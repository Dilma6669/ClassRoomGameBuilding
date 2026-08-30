using UnityEngine;
using UnityEngine.Serialization;

public class Health : MonoBehaviour, IDamagable
{
    [Range(10f, 500f)][SerializeField] private int maxHealth = 100;
    private int currentHealth;

    [FormerlySerializedAs("healthUI")] [SerializeField, HideInInspector] private HealthHUD healthHUD;

    private void Start()
    {
        currentHealth = maxHealth;

        // Auto-find HealthUI if not assigned manually in Inspector
        if (healthHUD == null)
        {
            healthHUD = FindFirstObjectByType<HealthHUD>();
        }

        // Initialize UI at full health on start
        if (healthHUD != null)
        {
            healthHUD.UpdateHealthBar(currentHealth, maxHealth);
        }
    }

    public void TakeDamage(int amount)
    {
        currentHealth = Mathf.Max(0, currentHealth - amount);

        // Update the health bar UI
        if (healthHUD != null)
        {
            healthHUD.UpdateHealthBar(currentHealth, maxHealth);
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(int amount)
    {
        // Increase health up to maxHealth ceiling
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);

        // Update the health bar UI
        if (healthHUD != null)
        {
            healthHUD.UpdateHealthBar(currentHealth, maxHealth);
        }
    }

    private void Die()
    {
        // Handle player death logic here
        gameObject.SetActive(false);
    }
}