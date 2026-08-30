using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HealthHUD : MonoBehaviour
{
    [SerializeField] private RectTransform healthBarFillRect;
    [SerializeField] private Image healthBarImage; // The Image component on HealthBarFiller
    [SerializeField] private float maxBarWidth = 600f; // Set to your bar's full width

    [Header("Flash Effect")]
    [SerializeField] private Color normalColor = Color.green;
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField] private float flashDuration = 0.2f;

    private Coroutine flashCoroutine;

    private void Awake()
    {
        if (healthBarFillRect == null) healthBarFillRect = GetComponent<RectTransform>();
        if (healthBarImage == null) healthBarImage = healthBarFillRect.GetComponent<Image>();
    }

    public void UpdateHealthBar(int currentHealth, int maxHealth)
    {
        if (healthBarFillRect != null)
        {
            float fillRatio = (float)currentHealth / maxHealth;
            float newWidth = Mathf.Clamp(fillRatio * maxBarWidth, 0f, maxBarWidth);

            // Update parent filler width
            healthBarFillRect.sizeDelta = new Vector2(newWidth, healthBarFillRect.sizeDelta.y);

            // Trigger flash effect on hit
            TriggerFlash();
        }
    }

    private void TriggerFlash()
    {
        if (healthBarImage == null) return;

        if (flashCoroutine != null) StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        healthBarImage.color = flashColor;

        float elapsed = 0f;
        while (elapsed < flashDuration)
        {
            elapsed += Time.deltaTime;
            healthBarImage.color = Color.Lerp(flashColor, normalColor, elapsed / flashDuration);
            yield return null;
        }

        healthBarImage.color = normalColor;
    }
}