using UnityEngine;
using UnityEngine.UI;

// Menu layers share a 1920x1080 design space, regardless of window aspect ratio.
[RequireComponent(typeof(Canvas))]
public sealed class FixedMenuViewport : MonoBehaviour
{
    private RectTransform content;
    private RectTransform canvasRect;

    public void Configure(bool drawBlackBackground)
    {
        if (content != null)
            return;

        Canvas canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = GetComponent<CanvasScaler>();
        if (scaler != null)
            scaler.enabled = false;
        canvas.scaleFactor = 1f;
        canvasRect = (RectTransform)transform;

        // Snapshot the original children before adding the wrapper. Keeping their
        // local layout preserves prefab anchors and animations below each child.
        Transform[] children = new Transform[transform.childCount];
        for (int i = 0; i < children.Length; i++)
            children[i] = transform.GetChild(i);

        if (drawBlackBackground)
        {
            GameObject backdrop = new GameObject("Letterbox", typeof(RectTransform), typeof(Image));
            RectTransform rect = (RectTransform)backdrop.transform;
            rect.SetParent(transform, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            Image image = backdrop.GetComponent<Image>();
            image.color = Color.black;
            image.raycastTarget = false;
        }

        GameObject root = new GameObject("MenuRoot_1920x1080", typeof(RectTransform), typeof(RectMask2D));
        content = (RectTransform)root.transform;
        content.SetParent(transform, false);
        content.anchorMin = content.anchorMax = content.pivot = new Vector2(0.5f, 0.5f);
        content.sizeDelta = new Vector2(1920f, 1080f);
        content.anchoredPosition = Vector2.zero;
        foreach (Transform child in children)
            child.SetParent(content, false);

        Canvas.ForceUpdateCanvases();
        Fit();
    }

    private void LateUpdate()
    {
        Fit();
    }

    private void Fit()
    {
        if (content == null)
            return;
        Vector2 available = canvasRect.rect.size;
        float scale = Mathf.Min(available.x / 1920f, available.y / 1080f);
        content.localScale = Vector3.one * Mathf.Max(scale, 0.0001f);
    }
}
