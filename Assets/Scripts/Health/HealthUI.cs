using UnityEngine;

public class HealthUI : MonoBehaviour
{
    [SerializeField] private RectTransform healthBarFillRect;
    [SerializeField] private float maxBarWidth = 600f; // Matches your default Width

    private void Awake()
    {
        // Auto-cache RectTransform if not manually assigned
        if (healthBarFillRect == null)
        {
            healthBarFillRect = GetComponent<RectTransform>();
        }
    }

    public void UpdateHealthBar(int currentHealth, int maxHealth)
    {
        if (healthBarFillRect != null)
        {
            float fillRatio = (float)currentHealth / maxHealth;
            float newWidth = Mathf.Clamp(fillRatio * maxBarWidth, 0f, maxBarWidth);

            // Update the width while keeping the current height intact
            healthBarFillRect.sizeDelta = new Vector2(newWidth, healthBarFillRect.sizeDelta.y);
        }
    }
}