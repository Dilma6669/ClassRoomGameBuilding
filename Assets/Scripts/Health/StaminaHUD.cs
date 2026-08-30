using System.Collections;
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

    [Header("Flash Effect")]
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField] private float flashDuration = 0.2f;
    [Tooltip("Minimum instant stamina jump required to trigger a flash (prevents passive regen from flashing).")]
    [SerializeField] private float minBoostThreshold = 1.0f;

    private Coroutine flashCoroutine;
    private float lastStamina;

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

        // Only trigger flash if the increase is a sudden boost larger than the threshold
        float staminaDelta = currentStamina - lastStamina;
        if (staminaDelta >= minBoostThreshold)
        {
            TriggerFlash(isExhausted);
        }
        else if (flashCoroutine == null && staminaBarImage != null)
        {
            staminaBarImage.color = isExhausted ? exhaustedColor : normalColor;
        }

        lastStamina = currentStamina;
    }

    private void TriggerFlash(bool isExhausted)
    {
        if (staminaBarImage == null) return;

        if (flashCoroutine != null) StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(FlashRoutine(isExhausted));
    }

    private IEnumerator FlashRoutine(bool isExhausted)
    {
        staminaBarImage.color = flashColor;

        Color targetColor = isExhausted ? exhaustedColor : normalColor;
        float elapsed = 0f;

        while (elapsed < flashDuration)
        {
            elapsed += Time.deltaTime;
            staminaBarImage.color = Color.Lerp(flashColor, targetColor, elapsed / flashDuration);
            yield return null;
        }

        staminaBarImage.color = targetColor;
        flashCoroutine = null;
    }
}