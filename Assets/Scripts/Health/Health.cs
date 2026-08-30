using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

public class Health : MonoBehaviour, IDamagable
{
    [Range(10f, 500f)][SerializeField] private int maxHealth = 100;
    private int currentHealth;
    
    private bool isInvincible = false;
    private Coroutine invincibilityCoroutine;

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
        if (isInvincible) return;
        
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

    public void ApplyInvincibility(float duration)
    {
        if (invincibilityCoroutine != null) StopCoroutine(invincibilityCoroutine);
        invincibilityCoroutine = StartCoroutine(InvincibilityRoutine(duration));
    }

    private IEnumerator InvincibilityRoutine(float duration)
    {
        isInvincible = true;
        yield return new WaitForSeconds(duration);
        isInvincible = false;
        invincibilityCoroutine = null;
    }

    private void Die()
    {
        // Handle player death logic here
        gameObject.SetActive(false);
    }
}