using UnityEngine;

public class Health : MonoBehaviour, IDamagable
{
    [Range(10f, 500f)][SerializeField] private int maxHealth = 100;
    private int currentHealth;

    [SerializeField, HideInInspector] private HealthUI healthUI;

    private void Start()
    {
        currentHealth = maxHealth;

        // Auto-find HealthUI if not assigned manually in Inspector
        if (healthUI == null)
        {
            healthUI = FindFirstObjectByType<HealthUI>();
        }

        // Initialize UI at full health on start
        if (healthUI != null)
        {
            healthUI.UpdateHealthBar(currentHealth, maxHealth);
        }
    }

    public void TakeDamage(int amount)
    {
        currentHealth = Mathf.Max(0, currentHealth - amount);

        // Update the health bar UI
        if (healthUI != null)
        {
            healthUI.UpdateHealthBar(currentHealth, maxHealth);
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        // Handle player death logic here
        gameObject.SetActive(false);
    }
}