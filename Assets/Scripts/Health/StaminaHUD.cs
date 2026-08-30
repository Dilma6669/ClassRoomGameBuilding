using UnityEngine;
using UnityEngine.UI;

public class StaminaHUD : MonoBehaviour
{
    [SerializeField] private RectTransform staminaBarFillRect;
    [SerializeField] private Image staminaBarImage;
    [SerializeField] private float maxBarWidth = 600f;

    [Header("Bar Colors")]
    [SerializeField] private Color normalColor = Color.yellow;
    [SerializeField] private Color exhaustedColor = Color.red;

    private void Awake()
    {
        if (staminaBarFillRect == null) staminaBarFillRect = GetComponent<RectTransform>();
        if (staminaBarImage == null && staminaBarFillRect != null) staminaBarImage = staminaBarFillRect.GetComponent<Image>();
    }

    public void UpdateStaminaBar(float currentStamina, float maxStamina, bool isExhausted)
    {
        if (staminaBarFillRect != null)
        {
            float fillRatio = Mathf.Clamp01(currentStamina / maxStamina);
            float newWidth = fillRatio * maxBarWidth;

            // Update filler rect width directly
            staminaBarFillRect.sizeDelta = new Vector2(newWidth, staminaBarFillRect.sizeDelta.y);
        }

        if (staminaBarImage != null)
        {
            staminaBarImage.color = isExhausted ? exhaustedColor : normalColor;
        }
    }
}