using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public sealed class LanBuildTools : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;
    public void OnPreprocessBuild(BuildReport report) { GenerateCatalog(); }

    [MenuItem("Tools/Multiplayer/Rebuild Asset Catalog")]
    public static void GenerateCatalog()
    {
        const string path = "Assets/Resources/LanAssetCatalog.asset";
        var catalog = AssetDatabase.LoadAssetAtPath<LanAssetCatalog>(path);
        if (catalog == null) { catalog = ScriptableObject.CreateInstance<LanAssetCatalog>(); AssetDatabase.CreateAsset(catalog, path); }
        var sprites = Resources.LoadAll<Sprite>("").Select(sprite =>
        {
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sprite, out string guid, out long id);
            return new { sprite, key = guid + ":" + id, path = AssetDatabase.GetAssetPath(sprite) };
        }).OrderBy(x => x.key, StringComparer.Ordinal).ToArray();
        catalog.sprites = sprites.Select(x => x.sprite).ToArray();
        string content = string.Join(";", sprites.Select(x => x.key + ":" + AssetDatabase.GetAssetDependencyHash(x.path)));
        using (var hash = SHA256.Create()) catalog.contentHash = BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(content)));
        catalog.font = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/TextMesh Pro/Fonts/simhei SDF.asset");
        EditorUtility.SetDirty(catalog); AssetDatabase.SaveAssets();
        Debug.Log("LAN catalog: " + catalog.sprites.Length + " sprites");
    }

    [MenuItem("Tools/Multiplayer/Build Windows Test Player")]
    public static void BuildWindows()
    {
        Directory.CreateDirectory("Builds/LanTest");
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = new[] { "Assets/Scenes/MainMenuScene.unity", "Assets/Scenes/GameScene.unity" },
            locationPathName = "Builds/LanTest/VampireCoop.exe", target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.Development });
        Debug.Log("LAN build: " + report.summary.result + " errors=" + report.summary.totalErrors);
    }
}
