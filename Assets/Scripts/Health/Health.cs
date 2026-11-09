using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

public class Health : MonoBehaviour, IDamagable
{
    [Range(10f, 500f)][SerializeField] private int maxHealth = 100;
    private int currentHealth;
    
    private bool isInvincible = false;
    private bool isDead = false;
    public bool IsDead => isDead;
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
        if (isInvincible || isDead) return;
        
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

    /// <summary>Instantly kills the player, even if invincible (e.g. falling out of the world).</summary>
    public void Kill()
    {
        if (isDead) return;
        currentHealth = 0;
        if (healthHUD != null) healthHUD.UpdateHealthBar(currentHealth, maxHealth);
        Die();
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        // Hide the player, then show the death screen (which restarts the level)
        gameObject.SetActive(false);
        GameManager.NotifyPlayerDied();
    }
}