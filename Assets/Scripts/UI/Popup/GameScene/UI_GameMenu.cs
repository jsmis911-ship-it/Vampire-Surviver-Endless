using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UI_GameMenu : UI_Popup
{
    public override Define.PopupUIGroup PopupID => Define.PopupUIGroup.UI_GameMenu;

    public override void Init()
    {
        base.Init();
        var font = GetComponentInChildren<TMP_Text>(true).font;
        foreach (Transform child in transform) child.gameObject.SetActive(false);
        transform.localScale = Vector3.one;
        var scaler = GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = .5f;
        SettingsControls.Build(transform, font, () => OnBackToGame(null), () => OnBackToMain(null), false);
    }

    public static void Toggle()
    {
        if (Managers.UI.IsPopupOpen(Define.PopupUIGroup.UI_GameMenu))
            Managers.UI.CloseAllGroupPopupUI(Define.PopupUIGroup.UI_GameMenu);
        else if (!Managers.gameStop)
        {
            Managers.UI.CloseAllGroupPopupUI(Define.PopupUIGroup.UI_Inventory);
            Managers.UI.ShowPopupUI<UI_GameMenu>();
            Managers.GamePause();
        }
    }

    public void OnBackToGame(PointerEventData data)
    {
        Managers.Sound.Play("Select", Define.Sound.Effect);
        Managers.UI.CloseAllGroupPopupUI(PopupID);
    }

    public void OnBackToMain(PointerEventData data)
    {
        Managers.Scene.LoadScene(Define.SceneType.MainMenuScene);
        Managers.GamePlay();
    }

    // Legacy events belong to inactive prefab controls.
    public void OnVolumnChanged() { }
    public void OnValueChanged() { }
}

// Both game modes use the same controls while retaining their own pause/exit flow.
public static class SettingsControls
{
    static readonly Color Panel = new Color(.055f, .075f, .115f, .98f);
    static readonly Color Accent = new Color(.24f, .74f, .62f);

    static Image Box(Transform parent, string name, Vector2 position, Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        var image = go.GetComponent<Image>();
        image.color = color;
        return image;
    }

    static TMP_Text Label(Transform parent, TMP_FontAsset font, string text, Vector2 position, Vector2 size, int fontSize)
    {
        var go = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        var label = go.GetComponent<TextMeshProUGUI>();
        label.font = font;
        label.text = text;
        label.fontSize = fontSize;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.raycastTarget = false;
        return label;
    }

    static void ActionButton(Transform parent, TMP_FontAsset font, string text, float y, Action action)
    {
        var image = Box(parent, text, new Vector2(0, y), new Vector2(520, 64), new Color(.12f, .25f, .29f));
        var button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(() => action());
        Label(image.transform, font, text, Vector2.zero, new Vector2(500, 60), 27).alignment = TextAlignmentOptions.Center;
    }

    static void Range(Transform parent, TMP_FontAsset font, string title, float y, float min, float max, float value, Action<float> changed, Func<float, string> format, bool wholeNumbers = false)
    {
        Label(parent, font, title, new Vector2(-130, y), new Vector2(260, 42), 27);
        var number = Label(parent, font, format(value), new Vector2(185, y), new Vector2(150, 42), 25);
        number.alignment = TextAlignmentOptions.MidlineRight;
        var track = Box(parent, title + " Slider", new Vector2(0, y - 46), new Vector2(520, 32), Color.clear);
        Box(track.transform, "Track", Vector2.zero, new Vector2(520, 8), new Color(.22f, .27f, .34f)).raycastTarget = false;
        var fillArea = Box(track.transform, "Fill Area", Vector2.zero, new Vector2(520, 8), Color.clear);
        fillArea.raycastTarget = false;
        var fill = Box(fillArea.transform, "Fill", Vector2.zero, Vector2.zero, Accent);
        fill.raycastTarget = false;
        var handleArea = Box(track.transform, "Handle Area", Vector2.zero, new Vector2(496, 32), Color.clear);
        handleArea.raycastTarget = false;
        var handle = Box(handleArea.transform, "Handle", Vector2.zero, new Vector2(24, 0), Color.white);
        var slider = track.gameObject.AddComponent<Slider>();
        slider.fillRect = fill.rectTransform;
        slider.handleRect = handle.rectTransform;
        slider.targetGraphic = handle;
        slider.minValue = min;
        slider.maxValue = max;
        slider.wholeNumbers = wholeNumbers;
        slider.SetValueWithoutNotify(value);
        slider.onValueChanged.AddListener(v => { changed(v); number.text = format(v); });
        // LAN creates its overlay in the lobby. Refresh when the battle menu opens.
        track.gameObject.AddComponent<SettingsRangeSync>().Configure(slider, number, format,
            title == "主音量" ? (Func<float>)(() => Managers.Sound._soundVolumn) : () => CameraController.ViewLevel);
    }

    public static void Build(Transform parent, TMP_FontAsset font, Action resume, Action leave, bool multiplayer)
    {
        var shade = Box(parent, "Settings", Vector2.zero, Vector2.zero, new Color(0, 0, 0, .55f));
        shade.rectTransform.anchorMin = Vector2.zero;
        shade.rectTransform.anchorMax = Vector2.one;
        shade.rectTransform.offsetMin = shade.rectTransform.offsetMax = Vector2.zero;
        var dismiss = shade.gameObject.AddComponent<Button>();
        dismiss.transition = Selectable.Transition.None;
        dismiss.onClick.AddListener(() => resume());
        var panel = Box(shade.transform, "Settings Panel", Vector2.zero, new Vector2(640, 590), Panel);
        panel.gameObject.AddComponent<Button>().transition = Selectable.Transition.None;
        Label(panel.transform, font, "设置", new Vector2(0, 230), new Vector2(520, 55), 40).alignment = TextAlignmentOptions.Center;
        Range(panel.transform, font, "主音量", 146, 0, 1, Managers.Sound._soundVolumn,
            Managers.Sound.SetMasterVolume, v => Mathf.RoundToInt(v * 100) + "%");
        Range(panel.transform, font, "视角高度", 32, CameraController.MinViewLevel, CameraController.MaxViewLevel,
            CameraController.ViewLevel, CameraController.SetViewLevel, v => CameraController.HeightForLevel(v).ToString("F1"), true);
        ActionButton(panel.transform, font, "返回主菜单", -100, leave);
        ActionButton(panel.transform, font, "继续游戏", -178, resume);
        if (multiplayer)
            Label(panel.transform, font, "设置期间战斗继续", new Vector2(0, -248), new Vector2(520, 35), 21).alignment = TextAlignmentOptions.Center;
    }

    public static void Gear(Transform parent, Action open)
    {
        var image = Box(parent, "Settings Button", new Vector2(58, -58), new Vector2(56, 56), Color.clear);
        image.rectTransform.anchorMin = image.rectTransform.anchorMax = new Vector2(0, 1);
        var button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(() => open());
        // Draw the gear with UI geometry; no font glyph or bitmap dependency.
        for (int i = 0; i < 16; i++)
        {
            float angle = i * Mathf.PI / 8;
            var segment = Box(image.transform, "Gear", new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 13,
                new Vector2(i % 2 == 0 ? 15 : 7, 7), new Color(.65f, .65f, .65f, 1f));
            segment.rectTransform.localRotation = Quaternion.Euler(0, 0, angle * Mathf.Rad2Deg);
            segment.raycastTarget = false;
        }
    }
}

public sealed class SettingsRangeSync : MonoBehaviour
{
    Slider slider;
    TMP_Text label;
    Func<float> read;
    Func<float, string> format;
    public void Configure(Slider control, TMP_Text text, Func<float, string> display, Func<float> current)
    {
        slider = control; label = text; format = display; read = current;
        OnEnable();
    }
    void OnEnable()
    {
        if (read == null) return;
        float value = read();
        slider.SetValueWithoutNotify(value);
        label.text = format(value);
    }
}
