using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Small helper for building uGUI elements from code (used by GameMenuUI).
/// </summary>
public static class UIBuilder
{
    public static readonly Color TextColor = new Color(0.96f, 0.96f, 0.98f);
    public static readonly Color MutedTextColor = new Color(0.72f, 0.75f, 0.82f);
    public static readonly Color PanelColor = new Color(0.10f, 0.12f, 0.17f, 0.96f);
    public static readonly Color PanelLightColor = new Color(0.15f, 0.18f, 0.25f, 1f);
    public static readonly Color AccentColor = new Color(0.18f, 0.62f, 0.55f, 1f);
    public static readonly Color ButtonColor = new Color(0.24f, 0.27f, 0.35f, 1f);
    public static readonly Color DangerColor = new Color(0.72f, 0.24f, 0.24f, 1f);

    public static RectTransform Create(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = 5; // UI layer
        RectTransform rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        return rt;
    }

    /// <summary>Fill the parent, with optional insets (left, right, top, bottom).</summary>
    public static RectTransform Stretch(RectTransform rt, float left = 0, float right = 0, float top = 0, float bottom = 0)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(-right, -top);
        return rt;
    }

    public static Image Image(Transform parent, string name, Color color)
    {
        RectTransform rt = Create(name, parent);
        Image img = rt.gameObject.AddComponent<Image>();
        img.color = color;
        return img;
    }

    public static TextMeshProUGUI Text(Transform parent, string name, string text, float size, TMP_FontAsset font, Color color, TextAlignmentOptions align)
    {
        RectTransform rt = Create(name, parent);
        TextMeshProUGUI tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = align;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.raycastTarget = false;
        return tmp;
    }

    public static LayoutElement Layout(Component c, float preferredHeight = -1, float preferredWidth = -1, float flexibleHeight = -1)
    {
        LayoutElement le = c.gameObject.GetComponent<LayoutElement>();
        if (le == null) le = c.gameObject.AddComponent<LayoutElement>();
        le.preferredHeight = preferredHeight;
        le.preferredWidth = preferredWidth;
        le.flexibleHeight = flexibleHeight;
        return le;
    }

    public static Button Button(Transform parent, string label, TMP_FontAsset font, float fontSize, Color background, UnityAction onClick, TextAlignmentOptions align = TextAlignmentOptions.Center)
    {
        Image img = Image(parent, "Button - " + label, background);
        Button button = img.gameObject.AddComponent<Button>();
        button.targetGraphic = img;

        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.88f, 0.88f, 0.88f, 1f);
        colors.highlightedColor = Color.white;
        colors.selectedColor = new Color(0.88f, 0.88f, 0.88f, 1f);
        colors.pressedColor = new Color(0.65f, 0.65f, 0.65f, 1f);
        colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        TextMeshProUGUI text = Text(img.transform, "Label", label, fontSize, font, TextColor, align);
        Stretch(text.rectTransform, 24, 24, 0, 0);

        if (onClick != null) button.onClick.AddListener(onClick);
        return button;
    }

    /// <summary>Vertical scrolling area. Put children into the returned content transform.</summary>
    public static RectTransform ScrollArea(Transform parent, string name, out ScrollRect scroll, float spacing = 8f, int padding = 0)
    {
        Image root = Image(parent, name, new Color(0, 0, 0, 0)); // invisible, but catches the mouse wheel
        scroll = root.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 40f;

        RectTransform viewport = Stretch(Create("Viewport", root.transform));
        viewport.gameObject.AddComponent<RectMask2D>();

        RectTransform content = Create("Content", viewport);
        content.anchorMin = new Vector2(0, 1);
        content.anchorMax = new Vector2(1, 1);
        content.pivot = new Vector2(0.5f, 1);
        content.sizeDelta = Vector2.zero;
        content.anchoredPosition = Vector2.zero;

        VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = spacing;
        layout.padding = new RectOffset(padding, padding, padding, padding);
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.viewport = viewport;
        scroll.content = content;
        return content;
    }
}
