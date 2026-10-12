using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 简单屏幕提示系统：动态创建 Canvas + TMP_Text，淡入→停留→淡出。
/// 支持同时显示多条，垂直堆叠。
/// </summary>
public class ToastManager : MonoBehaviour
{
    static ToastManager _instance;
    static ToastManager Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("ToastManager");
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<ToastManager>();
                _instance.Init();
            }
            return _instance;
        }
    }

    Canvas _canvas;
    Transform _toastRoot;
    static TMP_FontAsset _font;

    void Init()
    {
        // 加载 simhei SDF 字体（中文支持）
        _font = Resources.Load<TMP_FontAsset>("Fonts/simhei SDF");
        if (_font == null) _font = TMP_Settings.defaultFontAsset;

        // Canvas
        var canvasGO = new GameObject("ToastCanvas");
        canvasGO.transform.SetParent(transform);
        _canvas = canvasGO.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 100; // 最顶层
        canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGO.AddComponent<GraphicRaycaster>();
        // EventSystem
        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
        // Toast 容器（底部中央）
        var rootGO = new GameObject("ToastRoot");
        rootGO.transform.SetParent(canvasGO.transform, false);
        var rt = rootGO.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        _toastRoot = rootGO.transform;
    }

    /// <summary>显示一条 Toast。duration 总时长（淡入0.3s+停留+淡出0.5s）。</summary>
    public static void Show(string text, float duration = 2.2f)
    {
        Instance.StartCoroutine(Instance.ShowRoutine(text, duration));
    }

    IEnumerator ShowRoutine(string text, float duration)
    {
        // 创建 Toast 背景
        var toastGO = new GameObject("Toast");
        toastGO.transform.SetParent(_toastRoot, false);
        var bg = toastGO.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.75f);
        var bgRT = bg.rectTransform;
        bgRT.sizeDelta = new Vector2(380, 44);

        // 创建文本
        var txtGO = new GameObject("Text");
        txtGO.transform.SetParent(toastGO.transform, false);
        var txt = txtGO.AddComponent<TextMeshProUGUI>();
        txt.font = _font; // 关键！simhei SDF 字体支持中文
        txt.text = text;
        txt.color = Color.white;
        txt.fontSize = 18;
        txt.alignment = TextAlignmentOptions.Center;
        txt.fontStyle = FontStyles.Bold;
        var txtRT = txt.rectTransform;
        txtRT.anchorMin = Vector2.zero; txtRT.anchorMax = Vector2.one;
        txtRT.offsetMin = new Vector2(10, 0); txtRT.offsetMax = new Vector2(-10, 0);

        // 位置：底部向上堆叠（y 偏移由 siblingsCount 决定）
        int siblingsCount = _toastRoot.childCount;
        var toastRT = bgRT;
        toastRT.anchoredPosition = new Vector2(0, -100 - (siblingsCount - 1) * 50);

        // 淡入
        float fadeIn = 0.3f;
        for (float t = 0; t < fadeIn; t += Time.deltaTime)
        {
            float a = t / fadeIn;
            toastRT.localScale = new Vector3(1f, 0f, 1f);
            toastRT.localScale = Vector3.Lerp(new Vector3(1f, 0f, 1f), Vector3.one, a);
            bg.color = new Color(0, 0, 0, 0.75f * a);
            txt.color = new Color(1, 1, 1, a);
            yield return null;
        }
        toastRT.localScale = Vector3.one;
        bg.color = new Color(0, 0, 0, 0.75f);
        txt.color = Color.white;

        // 停留
        float stay = Mathf.Max(0, duration - 0.3f - 0.5f);
        yield return new WaitForSeconds(stay);

        // 淡出
        float fadeOut = 0.5f;
        for (float t = 0; t < fadeOut; t += Time.deltaTime)
        {
            float a = 1f - t / fadeOut;
            bg.color = new Color(0, 0, 0, 0.75f * a);
            txt.color = new Color(1, 1, 1, a);
            toastRT.localScale = new Vector3(1f, a, 1f);
            yield return null;
        }

        Destroy(toastGO);
    }
}
