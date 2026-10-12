#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>一键构建背包系统预制体。菜单: Tools/Build Inventory Prefabs</summary>
public static class InventoryPrefabBuilder
{
    const string ITEM_PATH   = "Assets/Resources/Prefabs/UI/SubItem/InventoryItem.prefab";
    const string POPUP_PATH = "Assets/Resources/Prefabs/UI/Popup/UI_Inventory.prefab";
    const string PLAYER_PATH = "Assets/Resources/Prefabs/UI/Scene/UI_Player.prefab";
    const string FONT_PATH  = "Assets/TextMesh Pro/Fonts/simhei SDF.asset";
    const string ICON_PATH  = "Assets/Resources/Prefabs/SpriteIcon/InventoryBtn.png";

    static TMP_FontAsset _zhFont;
    static Sprite _btnIcon;
    static TMP_FontAsset ZhFont { get { if (_zhFont == null) _zhFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_PATH); return _zhFont; } }

    [MenuItem("Tools/Build Inventory Prefabs")]
    public static void BuildAll()
    {
        // 强制重新导入图标
        AssetDatabase.ImportAsset(ICON_PATH, ImportAssetOptions.ForceUpdate);
        _btnIcon = AssetDatabase.LoadAssetAtPath<Sprite>(ICON_PATH);
        Debug.Log(_btnIcon != null
            ? $"[Inventory] ✔ BtnIcon loaded: {_btnIcon.name}"
            : $"[Inventory] ❌ BtnIcon NULL at {ICON_PATH}");

        BuildItem();
        BuildPopup();
        RebuildPlayer();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Inventory] All prefabs built!");
    }

    static void BuildItem()
    {
        if (File.Exists(ITEM_PATH)) { File.Delete(ITEM_PATH); File.Delete(ITEM_PATH + ".meta"); }
        var root = new GameObject("InventoryItem", typeof(RectTransform));
        root.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 0.6f);
        root.AddComponent<InventoryItem>();
        var rt = root.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(60, 60);

        var iconGo = new GameObject("IconImage", typeof(RectTransform));
        iconGo.transform.SetParent(root.transform, false);
        var iconImg = iconGo.AddComponent<Image>();
        iconImg.raycastTarget = false;
        Stretch(iconGo.GetComponent<RectTransform>());

        var countGo = new GameObject("CountText", typeof(RectTransform));
        countGo.transform.SetParent(root.transform, false);
        var countTMP = countGo.AddComponent<TextMeshProUGUI>();
        countTMP.font = ZhFont;
        countTMP.text = "2"; countTMP.alignment = TextAlignmentOptions.BottomRight;
        countTMP.fontSize = 18; countTMP.color = Color.white; countTMP.raycastTarget = false;
        var countRT = countGo.GetComponent<RectTransform>();
        countRT.anchorMin = new Vector2(1, 0); countRT.anchorMax = new Vector2(1, 0);
        countRT.pivot = new Vector2(1, 0);
        countRT.anchoredPosition = new Vector2(-2, 2); countRT.sizeDelta = new Vector2(30, 24);

        var tipGo = new GameObject("TooltipPanel", typeof(RectTransform));
        tipGo.transform.SetParent(root.transform, false);
        tipGo.SetActive(false);
        tipGo.AddComponent<Image>().color = new Color(0, 0, 0, 0.85f);
        var tipRT = tipGo.GetComponent<RectTransform>();
        tipRT.anchorMin = new Vector2(0.5f, 1); tipRT.anchorMax = new Vector2(0.5f, 1);
        tipRT.pivot = new Vector2(0.5f, 0);
        tipRT.anchoredPosition = new Vector2(0, -4); tipRT.sizeDelta = new Vector2(200, 100);

        var nameGo = new GameObject("TooltipName", typeof(RectTransform));
        nameGo.transform.SetParent(tipGo.transform, false);
        var nameTMP = nameGo.AddComponent<TextMeshProUGUI>();
        nameTMP.font = ZhFont;
        nameTMP.text = "名称"; nameTMP.alignment = TextAlignmentOptions.TopLeft;
        nameTMP.fontSize = 16; nameTMP.fontStyle = FontStyles.Bold;
        nameTMP.color = new Color(1f, 0.85f, 0.3f); nameTMP.raycastTarget = false;
        var nameRT = nameGo.GetComponent<RectTransform>();
        nameRT.anchorMin = new Vector2(0, 1); nameRT.anchorMax = new Vector2(1, 1);
        nameRT.pivot = new Vector2(0.5f, 1);
        nameRT.anchoredPosition = new Vector2(0, -8); nameRT.sizeDelta = new Vector2(-16, 22);

        var descGo = new GameObject("TooltipDesc", typeof(RectTransform));
        descGo.transform.SetParent(tipGo.transform, false);
        var descTMP = descGo.AddComponent<TextMeshProUGUI>();
        descTMP.font = ZhFont;
        descTMP.text = "描述"; descTMP.alignment = TextAlignmentOptions.TopLeft;
        descTMP.fontSize = 13; descTMP.color = Color.white;
        descTMP.enableWordWrapping = true; descTMP.raycastTarget = false;
        var descRT = descGo.GetComponent<RectTransform>();
        descRT.anchorMin = Vector2.zero; descRT.anchorMax = Vector2.one;
        descRT.offsetMin = new Vector2(8, 8); descRT.offsetMax = new Vector2(-8, -30);

        SavePrefab(root, ITEM_PATH);
    }

    static void BuildPopup()
    {
        if (File.Exists(POPUP_PATH)) { File.Delete(POPUP_PATH); File.Delete(POPUP_PATH + ".meta"); }

        var root = new GameObject("UI_Inventory", typeof(RectTransform));
        var rootRT = root.GetComponent<RectTransform>();
        rootRT.anchorMin = Vector2.zero; rootRT.anchorMax = Vector2.one;
        rootRT.offsetMin = Vector2.zero; rootRT.offsetMax = Vector2.zero;
        root.AddComponent<UI_Inventory>();

        var panelGo = new GameObject("Panel", typeof(RectTransform));
        panelGo.transform.SetParent(root.transform, false);
        panelGo.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.3f);
        var panelRT = panelGo.GetComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.5f, 0.5f); panelRT.anchorMax = new Vector2(0.5f, 0.5f);
        panelRT.pivot = new Vector2(0.5f, 0.5f);
        panelRT.anchoredPosition = Vector2.zero; panelRT.sizeDelta = new Vector2(360, 180);

        var titleGo = new GameObject("Title", typeof(RectTransform));
        titleGo.transform.SetParent(panelGo.transform, false);
        var titleTMP = titleGo.AddComponent<TextMeshProUGUI>();
        titleTMP.font = ZhFont;
        titleTMP.text = "背包 (P 键关闭)"; titleTMP.alignment = TextAlignmentOptions.Center;
        titleTMP.fontSize = 16; titleTMP.color = new Color(0.1f, 0.1f, 0.1f); titleTMP.raycastTarget = false;
        var titleRT = titleGo.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0, 1); titleRT.anchorMax = new Vector2(1, 1);
        titleRT.pivot = new Vector2(0.5f, 1);
        titleRT.anchoredPosition = new Vector2(0, -8); titleRT.sizeDelta = new Vector2(0, 24);

        var gridGo = new GameObject("GridPanel", typeof(RectTransform));
        gridGo.transform.SetParent(panelGo.transform, false);
        var gridRT = gridGo.GetComponent<RectTransform>();
        gridRT.anchorMin = Vector2.zero; gridRT.anchorMax = Vector2.one;
        gridRT.offsetMin = new Vector2(12, 12); gridRT.offsetMax = new Vector2(-12, -40);

        var glg = gridGo.AddComponent<GridLayoutGroup>();
        glg.cellSize = new Vector2(60, 60); glg.spacing = new Vector2(8, 8);
        glg.startCorner = GridLayoutGroup.Corner.UpperLeft; glg.startAxis = GridLayoutGroup.Axis.Horizontal;
        glg.childAlignment = TextAnchor.UpperCenter; glg.constraint = GridLayoutGroup.Constraint.Flexible;
        var fitter = gridGo.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        SavePrefab(root, POPUP_PATH);
    }

    static void RebuildPlayer()
    {
        var prefabRoot = PrefabUtility.LoadPrefabContents(PLAYER_PATH);
        if (prefabRoot == null) { Debug.LogError("Cannot load " + PLAYER_PATH); return; }

        var oldBox = prefabRoot.transform.Find("WeaponImgList");
        if (oldBox != null) { Object.DestroyImmediate(oldBox.gameObject); Debug.Log("[Inventory] WeaponImgList removed"); }

        var oldBtn = prefabRoot.transform.Find("InventoryBtn");
        if (oldBtn != null) Object.DestroyImmediate(oldBtn.gameObject);

        var btnGo = new GameObject("InventoryBtn", typeof(RectTransform));
        btnGo.transform.SetParent(prefabRoot.transform, false);
        var img = btnGo.AddComponent<Image>();
        // RED = 可视诊断！如果看到红方块说明 Image 正常但 sprite 没加载
        // 如果看到 Enemy 图说明一切正常
        img.color = Color.red;
        if (_btnIcon != null) { img.sprite = _btnIcon; img.preserveAspect = true; }
        btnGo.AddComponent<Button>().targetGraphic = img;
        var btnRT = btnGo.GetComponent<RectTransform>();
        btnRT.anchorMin = new Vector2(0, 1); btnRT.anchorMax = new Vector2(0, 1);
        btnRT.pivot = new Vector2(0, 1);
        btnRT.anchoredPosition = new Vector2(20, -20); btnRT.sizeDelta = new Vector2(60, 60);

        PrefabUtility.SaveAsPrefabAsset(prefabRoot, PLAYER_PATH);
        PrefabUtility.UnloadPrefabContents(prefabRoot);
        Debug.Log(_btnIcon != null ? "[Inventory] ✔ InventoryBtn sprite assigned!" : "[Inventory] ⚠️ InventoryBtn sprite NULL — Image.color=red");
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

    static void SavePrefab(GameObject root, string path)
    {
        var dir = Path.GetDirectoryName(path);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        var p = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        if (p != null) Debug.Log("[Inventory] Created: " + path);
    }
}
#endif
