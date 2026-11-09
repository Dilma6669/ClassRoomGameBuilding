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

    public void TakeDamage(int amount) => TakeDamage(amount, false);

    /// <summary>fromFall: landed too hard (plays the Fall damage sound instead of Damage, if there is one).</summary>
    public void TakeDamage(int amount, bool fromFall)
    {
        if (isInvincible || isDead) return;

        currentHealth = Mathf.Max(0, currentHealth - amount);

        // The player model's own "gets hurt" / "lands too hard" animation (and its sound, which replaces the general one)
        CustomModel ownModel = GetComponentInChildren<CustomModel>();
        bool ownSound = false;
        if (currentHealth > 0 && ownModel != null)
        {
            float length = ownModel.PlayMoment(fromFall ? AnimationMoment.LandsTooHard : AnimationMoment.GetsHurt, out ownSound);
            if (length <= 0f && fromFall) ownModel.PlayMoment(AnimationMoment.GetsHurt, out ownSound); // no fall animation: use the hurt one
        }

        if (currentHealth > 0 && !ownSound)
        {
            bool fallSound = fromFall && SoundManager.Find(SoundEvent.FallDamage, gameObject) != null;
            SoundManager.Play(fallSound ? SoundEvent.FallDamage : SoundEvent.Damage, transform.position, gameObject);
        }

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
        SoundManager.Play(SoundEvent.PlayerDeath, transform.position, gameObject);

        // Hide the player, then show the death screen (which restarts the level)
        gameObject.SetActive(false);
        GameManager.NotifyPlayerDied();
    }
}