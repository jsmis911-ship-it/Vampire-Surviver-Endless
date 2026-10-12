using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>Rebuilds the farmer assets from the existing character and axe sprite sheets.</summary>
public static class FarmerAxeSetup
{
    const string AnimationFolder = "Assets/Resources/Animations/Farmer/";
    const string IconFolder = "Assets/Resources/Prefabs/SpriteIcon/";

    [MenuItem("Tools/Farmer Axe/Rebuild Assets")]
    public static void Rebuild()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode before rebuilding assets.");
        var farmer = AssetDatabase.LoadAllAssetsAtPath("Assets/Resources/Sprites/Farmer 2.png").OfType<Sprite>().ToDictionary(s => s.name);
        var axe = AssetDatabase.LoadAllAssetsAtPath("Assets/Resources/Sprites/FarmerAxe.png").OfType<Sprite>().ToDictionary(s => s.name);
        var controller = new AnimatorOverrideController(AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AnimationFolder + "AcPlayer0.controller"));
        var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        foreach (var original in controller.animationClips)
        {
            var clip = UnityEngine.Object.Instantiate(original);
            clip.name = original.name.Replace("Player0", "Player2");
            foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
            {
                var frames = AnimationUtility.GetObjectReferenceCurve(clip, binding);
                for (int i = 0; i < frames.Length; i++)
                    if (frames[i].value is Sprite sprite) frames[i].value = farmer[sprite.name];
                AnimationUtility.SetObjectReferenceCurve(clip, binding, frames);
            }
            string path = AnimationFolder + clip.name + ".anim";
            var saved = SaveAsset(clip, path);
            overrides.Add(new KeyValuePair<AnimationClip, AnimationClip>(original, saved));
        }
        controller.ApplyOverrides(overrides);
        controller.name = "AcPlayer2";
        var savedController = SaveAsset(controller, AnimationFolder + "AcPlayer2.overrideController");

        string playerPath = "Assets/Resources/Prefabs/Player/Player.prefab";
        var player = PrefabUtility.LoadPrefabContents(playerPath);
        Material spriteMaterial;
        try
        {
            var pc = player.GetComponent<PlayerController>();
            var controllers = pc.animeCon.ToList();
            while (controllers.Count < 3) controllers.Add(null);
            controllers[2] = savedController;
            pc.animeCon = controllers.ToArray();
            spriteMaterial = player.GetComponent<SpriteRenderer>().sharedMaterial;
            PrefabUtility.SaveAsPrefabAsset(player, playerPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(player); }

        CreateIcon(farmer["Run 0"], "player2");
        foreach (var name in new[] { "Axe", "AxeRangeUp", "AxeAngleUp", "AxeDamageUp" }) CreateIcon(axe[name], name);
        var ranges = CreateRangeSprites();
        // Reuse the sheet's wood and blade pixels with independent proportions.
        var handle = Sprite.Create(axe["Axe"].texture, new Rect(22, 94, 12, 4), new Vector2(0, 0.5f), 18, 0, SpriteMeshType.FullRect);
        handle.name = "AxeHandle";
        handle = SaveAsset(handle, IconFolder + "AxeHandle.asset");
        var head = Sprite.Create(axe["Axe"].texture, new Rect(37, 83, 16, 20), new Vector2(7f / 16f, 13f / 20f), 18, 0, SpriteMeshType.FullRect);
        head.name = "AxeHead";
        head = SaveAsset(head, IconFolder + "AxeHead.asset");
        var hand = Sprite.Create(farmer["Run 0"].texture, new Rect(6, 71, 2, 2), new Vector2(0.5f, 0.5f), 18, 0, SpriteMeshType.FullRect);
        hand.name = "AxeHand";
        hand = SaveAsset(hand, IconFolder + "AxeHand.asset");
        CreateLongAxeIcon(axe["Axe"].texture);

        var root = new GameObject("AxeSpawningPool");
        try
        {
            root.transform.localPosition = new Vector3(4f / 18f, -5f / 18f, 0);
            var weapon = root.AddComponent<AxeController>();
            var renderer = MakeRenderer(root.transform, "Axe", handle, spriteMaterial, 2);
            var headRenderer = MakeRenderer(renderer.transform, "Axe Head", head, spriteMaterial, 2);
            var handRenderer = MakeRenderer(root.transform, "Grip Hand", hand, spriteMaterial, 3);
            var rangeRenderer = MakeRenderer(root.transform, "Attack Range", ranges[0], spriteMaterial, 0);
            var slashes = new[] { MakeRenderer(root.transform, "Slash A", axe["Slash_0"], spriteMaterial, 1),
                MakeRenderer(root.transform, "Slash B", axe["Slash_0"], spriteMaterial, 1) };
            foreach (var slash in slashes) slash.enabled = false;
            var so = new SerializedObject(weapon);
            so.FindProperty("axeRenderer").objectReferenceValue = renderer;
            so.FindProperty("axeHeadRenderer").objectReferenceValue = headRenderer;
            so.FindProperty("handRenderer").objectReferenceValue = handRenderer;
            so.FindProperty("rangeRenderer").objectReferenceValue = rangeRenderer;
            var rangeFrames = so.FindProperty("rangeFrames"); rangeFrames.arraySize = ranges.Length;
            for (int i = 0; i < ranges.Length; i++) rangeFrames.GetArrayElementAtIndex(i).objectReferenceValue = ranges[i];
            var renderers = so.FindProperty("slashRenderers"); renderers.arraySize = 2;
            for (int i = 0; i < 2; i++) renderers.GetArrayElementAtIndex(i).objectReferenceValue = slashes[i];
            var frames = so.FindProperty("slashFrames"); frames.arraySize = 4;
            for (int i = 0; i < 4; i++) frames.GetArrayElementAtIndex(i).objectReferenceValue = axe["Slash_" + i];
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, "Assets/Resources/Prefabs/Weapon/SpawningPool/AxeSpawningPool.prefab");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
        AssetDatabase.SaveAssets();
        LanBuildTools.GenerateCatalog();
        Debug.Log("Farmer axe ready: character animation, portrait, four icons, weapon prefab and LAN catalog.");
    }

    static SpriteRenderer MakeRenderer(Transform parent, string name, Sprite sprite, Material material, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sharedMaterial = material;
        renderer.sortingOrder = order;
        return renderer;
    }

    static void CreateLongAxeIcon(Texture2D source)
    {
        const string path = "Assets/Resources/Sprites/LongAxeIcon.png";
        var readable = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        readable.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(source)));
        var icon = new Texture2D(48, 16, TextureFormat.RGBA32, false);
        icon.SetPixels32(new Color32[48 * 16]);
        // Same thin wooden shaft and small blade proportions as the world weapon.
        for (int x = 2; x < 41; x++)
            for (int y = 0; y < 2; y++) icon.SetPixel(x, 8 + y, readable.GetPixel(26, 95 + y));
        for (int x = 0; x < 10; x++)
            for (int y = 0; y < 12; y++)
            {
                var pixel = readable.GetPixel(37 + x * 16 / 10, 83 + y * 20 / 12);
                if (pixel.a > 0) icon.SetPixel(36 + x, 1 + y, pixel);
            }
        icon.Apply(); File.WriteAllBytes(path, icon.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(readable); UnityEngine.Object.DestroyImmediate(icon);
        AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 18; importer.alphaIsTransparency = true; importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Point; importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        CreateIcon(AssetDatabase.LoadAssetAtPath<Sprite>(path), "Axe");
    }

    static Sprite[] CreateRangeSprites()
    {
        // A geometric guide, with a shared center and a one-unit radius.
        // Imported sprites also travel through the existing LAN sprite replication.
        const int cell = 256;
        const float radius = 124f;
        const string path = "Assets/Resources/Sprites/AxeAttackRange.png";
        var texture = new Texture2D(cell * 4, cell, TextureFormat.RGBA32, false);
        var pixels = new Color32[cell * cell * 4];
        var slices = new SpriteMetaData[4];
        for (int i = 0; i < 4; i++)
        {
            float half = (80f + i * 15f) * 0.5f;
            for (int y = 0; y < cell; y++)
                for (int x = 0; x < cell; x++)
                {
                    var offset = new Vector2(x + 0.5f - cell / 2f, y + 0.5f - cell / 2f);
                    float distance = offset.magnitude;
                    float angle = Mathf.Abs(Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg);
                    if (distance > radius || angle > half) continue;
                    bool edge = radius - distance < 3f || distance * Mathf.Sin((half - angle) * Mathf.Deg2Rad) < 1.8f;
                    // A short central tick makes the exact mouse direction readable.
                    bool tick = offset.x > radius - 10f && Mathf.Abs(offset.y) < 1.2f;
                    pixels[y * cell * 4 + i * cell + x] = new Color32(255, 255, 255, (byte)(edge || tick ? 245 : 45));
                }
            slices[i] = new SpriteMetaData { name = "AxeRange_" + (80 + i * 15), rect = new Rect(i * cell, 0, cell, cell), alignment = 9, pivot = new Vector2(0.5f, 0.5f) };
        }
        texture.SetPixels32(pixels); texture.Apply();
        File.WriteAllBytes(path, texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = radius;
        importer.spritesheet = slices;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s => int.Parse(s.name.Substring(9))).ToArray();
    }

    static T SaveAsset<T>(T generated, string path) where T : UnityEngine.Object
    {
        var existing = AssetDatabase.LoadAssetAtPath<T>(path);
        if (existing == null) { AssetDatabase.CreateAsset(generated, path); return generated; }
        EditorUtility.CopySerialized(generated, existing);
        UnityEngine.Object.DestroyImmediate(generated);
        EditorUtility.SetDirty(existing);
        return existing;
    }

    static void CreateIcon(Sprite source, string name)
    {
        // UI icons use a tight rect; combat sprites retain their padded rotation pivots.
        var readable = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        try
        {
            readable.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(source.texture)));
            Rect r = source.rect;
            int minX = (int)r.xMax, minY = (int)r.yMax, maxX = (int)r.x, maxY = (int)r.y;
            for (int y = (int)r.y; y < r.yMax; y++)
                for (int x = (int)r.x; x < r.xMax; x++)
                    if (readable.GetPixel(x, y).a > 0.1f)
                    { minX = Math.Min(minX, x); minY = Math.Min(minY, y); maxX = Math.Max(maxX, x); maxY = Math.Max(maxY, y); }
            var icon = Sprite.Create(source.texture, new Rect(minX, minY, maxX - minX + 1, maxY - minY + 1), new Vector2(0.5f, 0.5f), 18, 0, SpriteMeshType.FullRect);
            icon.name = name;
            SaveAsset(icon, IconFolder + name + ".asset");
        }
        finally { UnityEngine.Object.DestroyImmediate(readable); }
    }
}
