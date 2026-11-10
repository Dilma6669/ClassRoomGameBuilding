using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A big message in the middle-top of the screen that says what you just picked up ("+25 Health!"),
/// in the pickup's colour. It fades out after a couple of seconds; a new pickup replaces it.
/// Made in code (no scene setup needed).
/// </summary>
public class PickupMessage : MonoBehaviour
{
    private const float ShowFor = 1.6f;
    private const float FadeTime = 0.4f;

    private static PickupMessage instance;
    private TextMeshProUGUI text;
    private float shownAt = -100f;

    /// <summary>Shows the message (replacing any message already showing).</summary>
    public static void Show(string message, Color colour)
    {
        if (string.IsNullOrWhiteSpace(message)) return;
        if (instance == null) instance = Create();
        instance.text.text = message;
        colour.a = 1f;
        instance.text.color = colour;
        instance.shownAt = Time.unscaledTime;
        instance.transform.localScale = Vector3.one;
    }

    private static PickupMessage Create()
    {
        GameObject go = new GameObject("Pickup Message (auto)");
        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 450; // above the game and HUD, below the menus (500) and videos (600)
        CanvasScaler scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject label = new GameObject("Text");
        label.transform.SetParent(go.transform, false);
        TextMeshProUGUI tmp = label.AddComponent<TextMeshProUGUI>();
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 72f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.enableWordWrapping = true;
        tmp.raycastTarget = false;
        tmp.outlineWidth = 0.25f;
        tmp.outlineColor = new Color32(0, 0, 0, 200);
        GameSettings settings = GameSettings.Load();
        // The plain body font: the decorative title font goes blurry at this size
        if (settings != null && settings.bodyFont != null) tmp.font = settings.bodyFont;

        RectTransform rect = tmp.rectTransform;
        rect.anchorMin = new Vector2(0.15f, 0.62f);
        rect.anchorMax = new Vector2(0.85f, 0.8f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        PickupMessage message = go.AddComponent<PickupMessage>();
        message.text = tmp;
        tmp.text = "";
        return message;
    }

    private void Update()
    {
        float age = Time.unscaledTime - shownAt;
        float alpha = age < ShowFor ? 1f : Mathf.Clamp01(1f - (age - ShowFor) / FadeTime);
        Color c = text.color;
        if (!Mathf.Approximately(c.a, alpha))
        {
            c.a = alpha;
            text.color = c;
        }
        // A little 'pop' when it appears
        float pop = age < 0.15f ? Mathf.Lerp(1.25f, 1f, age / 0.15f) : 1f;
        transform.GetChild(0).localScale = Vector3.one * pop;
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }
}
