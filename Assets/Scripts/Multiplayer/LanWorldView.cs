using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

/// <summary>Client renders passive sprites only: never instantiates combat scripts.
/// Full snapshots also repair missed spawns/despawns without replaying old events.</summary>
public sealed class LanWorldView
{
    readonly Sprite[] sprites;
    readonly Dictionary<Sprite, int> spriteIds = new Dictionary<Sprite, int>();
    readonly Dictionary<int, SpriteRenderer> replicas = new Dictionary<int, SpriteRenderer>();
    readonly Dictionary<int, Vector3> targets = new Dictionary<int, Vector3>();
    readonly Dictionary<SpriteRenderer, int> hostIds = new Dictionary<SpriteRenderer, int>();
    readonly List<SpriteRenderer> expired = new List<SpriteRenderer>();
    int nextId;
    public string Fingerprint { get; }

    public LanWorldView()
    {
        var catalog = Resources.Load<LanAssetCatalog>("LanAssetCatalog");
        if (catalog == null) throw new InvalidOperationException("缺少联机资源目录，请运行 Tools/Multiplayer/Rebuild Asset Catalog");
        sprites = catalog.sprites;
        for (int i = 0; i < sprites.Length; i++)
        {
            spriteIds[sprites[i]] = i;
        }
        string content = "lan-prototype-1|" + catalog.contentHash;
        foreach (var data in Resources.LoadAll<TextAsset>("Data").OrderBy(x => x.name, StringComparer.Ordinal)) content += data.text;
        using (var hash = SHA256.Create()) Fingerprint = BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(content)));
    }

    public LanVisualState[] Capture()
    {
        var states = new List<LanVisualState>();
        // Prototype cap is 50 enemies. Profile before replacing this scan with a registry.
        foreach (var renderer in UnityEngine.Object.FindObjectsOfType<SpriteRenderer>())
        {
            if (!renderer.enabled || renderer.sprite == null || !spriteIds.TryGetValue(renderer.sprite, out int sprite)) continue;
            if (!hostIds.TryGetValue(renderer, out int id)) hostIds[renderer] = id = ++nextId;
            var t = renderer.transform;
            Vector3 p = t.position, scale = t.lossyScale;
            states.Add(new LanVisualState { id = id, sprite = sprite, x = p.x, y = p.y, z = p.z,
                rotation = t.eulerAngles.z, sx = scale.x, sy = scale.y, color = renderer.color,
                flipX = renderer.flipX, flipY = renderer.flipY,
                layer = renderer.sortingLayerID, order = renderer.sortingOrder });
        }
        expired.Clear();
        foreach (var pair in hostIds) if (pair.Key == null || !pair.Key.gameObject.activeInHierarchy) expired.Add(pair.Key);
        foreach (var key in expired) hostIds.Remove(key);
        return states.ToArray();
    }

    public void Apply(LanVisualState[] states)
    {
        var alive = new HashSet<int>();
        foreach (var state in states)
        {
            if (state.sprite < 0 || state.sprite >= sprites.Length) continue;
            alive.Add(state.id);
            Vector3 position = new Vector3(state.x, state.y, state.z);
            if (!replicas.TryGetValue(state.id, out var renderer))
            {
                renderer = new GameObject("LAN View " + state.id, typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
                replicas.Add(state.id, renderer);
                renderer.transform.position = position;
            }
            if (Vector3.Distance(renderer.transform.position, position) > 4f) renderer.transform.position = position;
            targets[state.id] = position;
            renderer.sprite = sprites[state.sprite];
            renderer.color = state.color;
            renderer.flipX = state.flipX; renderer.flipY = state.flipY;
            renderer.sortingLayerID = state.layer; renderer.sortingOrder = state.order;
            renderer.transform.rotation = Quaternion.Euler(0, 0, state.rotation);
            renderer.transform.localScale = new Vector3(state.sx, state.sy, 1);
        }
        foreach (int id in replicas.Keys.Where(id => !alive.Contains(id)).ToArray())
        { UnityEngine.Object.Destroy(replicas[id].gameObject); replicas.Remove(id); targets.Remove(id); }
    }

    public void Interpolate(float dt)
    {
        float blend = 1f - Mathf.Exp(-30f * dt);
        foreach (var pair in replicas)
            if (pair.Value != null) pair.Value.transform.position = Vector3.Lerp(pair.Value.transform.position, targets[pair.Key], blend);
    }
}
