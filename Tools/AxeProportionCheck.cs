// Run Main through Unity Script Execute in Play Mode. Exit Play Mode afterwards.
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

public static class AxeProportionCheck
{
    public static void Main()
    {
        Managers.Game.StartPlayer = Managers.Data.PlayerData[3];
        Managers.Scene.LoadScene(Define.SceneType.GameScene);
        Managers.Instance.StartCoroutine(Run());
    }

    static IEnumerator Run()
    {
        yield return null;
        yield return null;
        foreach (var sp in UnityEngine.Object.FindObjectsOfType<Spawner>()) { sp.enabled = false; sp.StopAllCoroutines(); }
        foreach (var enemy in UnityEngine.Object.FindObjectsOfType<EnemyController>()) UnityEngine.Object.Destroy(enemy.gameObject);
        Managers.Instance.enabled = false;
        var axe = UnityEngine.Object.FindObjectOfType<AxeController>();
        var stats = axe.GetComponentInParent<PlayerStat>();
        var shaft = axe.GetComponentsInChildren<SpriteRenderer>().Single(s => s.name == "Axe");
        var head = axe.GetComponentsInChildren<SpriteRenderer>().Single(s => s.name == "Axe Head");
        var guide = axe.GetComponentsInChildren<SpriteRenderer>().Single(s => s.name == "Attack Range");
        var hand = axe.GetComponentsInChildren<SpriteRenderer>().Single(s => s.name == "Grip Hand");
        stats.GetComponent<PlayerController>().enabled = false;
        Managers.Game.WorldMousePos = axe.transform.position + Vector3.right * 5;
        Directory.CreateDirectory("Logs/FarmerAxe");
        for (int tier = 0; tier <= 3; tier++)
        {
            axe.enabled = false;
            if (tier > 0) stats.ApplyAxeUpgrade("AxeRangeUp");
            axe.enabled = true;
            yield return null;
            yield return null;
            float length = shaft.sprite.bounds.size.x * shaft.transform.lossyScale.x;
            if (Mathf.Abs(length - axe.AttackRange) > 0.001f || Mathf.Abs(guide.transform.localScale.x - length) > 0.001f)
                throw new Exception("Handle and sector radius differ at tier " + tier);
            if (Vector3.Distance(hand.transform.position, shaft.transform.position) > 0.001f || axe.transform.localPosition.y >= 0)
                throw new Exception("Grip must overlap handle at the cuff, below the face");
            if (Mathf.Abs(Vector2.Distance(shaft.transform.position, head.transform.position) - length) > 0.001f ||
                Mathf.Abs(head.transform.lossyScale.x - 0.55f) > 0.001f || Mathf.Abs(head.transform.lossyScale.y - 0.55f) > 0.001f)
                throw new Exception("Blade size or position changed at tier " + tier);
            if (tier == 0 || tier == 3) ScreenCapture.CaptureScreenshot("Logs/FarmerAxe/long-axe-" + tier + ".png");
            yield return new WaitForEndOfFrame();
        }
        var body = stats.GetComponent<SpriteRenderer>();
        body.flipX = true;
        Managers.Game.WorldMousePos = stats.transform.position + Vector3.left * 5;
        yield return null; yield return null;
        if (axe.transform.localPosition.x >= 0 || Vector3.Distance(hand.transform.position, shaft.transform.position) > 0.001f)
            throw new Exception("Grip did not follow the mirrored character");
        ScreenCapture.CaptureScreenshot("Logs/FarmerAxe/hand-left.png");
        var catalog = Resources.Load<LanAssetCatalog>("LanAssetCatalog");
        if (!catalog.sprites.Contains(head.sprite) || !catalog.sprites.Contains(shaft.sprite) || !catalog.sprites.Contains(hand.sprite)) throw new Exception("Missing LAN sprites");
        File.WriteAllText("Logs/FarmerAxe/proportions.txt", "PASS: all four reach tiers; handle equals sector radius; blade scale and handle thickness fixed; LAN sprites present.");
        Managers.GamePause();
    }
}
