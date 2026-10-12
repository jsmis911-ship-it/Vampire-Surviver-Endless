using UnityEngine;
using UnityEditor;
using TMPro;

/// <summary>
/// 界面中文化工具：将包含中文文本的 UI 预制体的字体引用
/// 替换为已生成的中文字体资产（simhei SDF）。
/// 使用方式：Unity 菜单 → Tools → 中文化 → 应用中文字体
/// </summary>
public static class ChineseFontTool
{
    private static readonly string[] TargetPrefabs =
    {
        "Assets/Resources/Prefabs/UI/Popup/UI_GameOver.prefab",
        "Assets/Resources/Prefabs/UI/Popup/UI_GameVictory.prefab",
        "Assets/Resources/Prefabs/UI/Popup/UI_ItemBoxOpen.prefab",
        "Assets/Resources/Prefabs/UI/Scene/UI_MainMenu.prefab",
        "Assets/Resources/Prefabs/UI/SubItem/UpgdPanel.prefab",
    };

    /// <summary>
    /// 运行时动态填入中文、但预制体当前文本不含中文字符的预制体，
    /// 必须强制替换字体并做适配，否则运行时中文会以韩文字体渲染。
    /// </summary>
    private static readonly string[] ForcePrefabs =
    {
        "Assets/Resources/Prefabs/UI/SubItem/UpgdPanel.prefab",
        "Assets/Resources/Prefabs/UI/SubItem/StatData.prefab",
        "Assets/Resources/Prefabs/UI/SubItem/WeaponData.prefab",
    };

    [MenuItem("Tools/中文化/应用中文字体")]
    public static void ApplyChineseFont()
    {
        // 复用已生成的中文字体资产（simhei SDF，图集与材质均已就绪）
        string sdfPath = "Assets/TextMesh Pro/Fonts/simhei SDF.asset";
        TMP_FontAsset chineseFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(sdfPath);
        if (chineseFont == null)
        {
            Debug.LogError("[中文化] 未找到字体资产: " + sdfPath);
            return;
        }

        int total = 0;
        foreach (string path in TargetPrefabs)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogWarning("[中文化] 预制体不存在: " + path);
                continue;
            }

            TMP_Text[] texts = prefab.GetComponentsInChildren<TMP_Text>(true);
            foreach (TMP_Text t in texts)
            {
                // 仅替换包含中文字符的文本组件，英文/韩文文本保持原字体
                if (ContainsChinese(t.text))
                {
                    t.font = chineseFont;
                    EditorUtility.SetDirty(t);
                }
            }

            PrefabUtility.SavePrefabAsset(prefab);
            total += texts.Length;
            Debug.Log("[中文化] 已替换字体: " + path + "（" + texts.Length + " 个文本组件）");
        }

        foreach (string path in ForcePrefabs)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogWarning("[中文化] 预制体不存在: " + path);
                continue;
            }

            TMP_Text[] texts = prefab.GetComponentsInChildren<TMP_Text>(true);
            foreach (TMP_Text t in texts)
            {
                t.font = chineseFont;
                // 属性条目：125x50 区域放 5 个汉字需压缩字号（适配）
                if (t.gameObject.name == "TitleText" && t.fontSize > 20)
                    t.fontSize = 20;
                // 武器条目：等级前缀 "Lv" → "级"
                if (t.gameObject.name == "LvText")
                    t.text = "级";
                EditorUtility.SetDirty(t);
            }

            PrefabUtility.SavePrefabAsset(prefab);
            total += texts.Length;
            Debug.Log("[中文化] 已强制替换字体: " + path + "（" + texts.Length + " 个文本组件）");
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[中文化] 完成！共替换 " + total + " 个文本组件，字体资产: " + sdfPath);
    }

    /// <summary>判断字符串是否包含中文字符（CJK 统一表意文字）</summary>
    private static bool ContainsChinese(string text)
    {
        if (string.IsNullOrEmpty(text)) return false;
        foreach (char c in text)
        {
            if (c >= 0x4E00 && c <= 0x9FFF)
            {
                return true;
            }
        }
        return false;
    }
}
