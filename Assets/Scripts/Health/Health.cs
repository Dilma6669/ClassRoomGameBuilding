using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

public class Health : MonoBehaviour, IDamagable
{
    [Range(10f, 10000f)][SerializeField] private int maxHealth = 100;
    private int currentHealth;
    
    private bool isInvincible = false;
    private bool isDead = false;
    public bool IsDead => isDead;
    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;

    /// <summary>Changes the most health you can have (fill = fill it right up). Used by 'become a character' pickups.</summary>
    public void SetMaxHealth(int newMax, bool fill)
    {
        maxHealth = Mathf.Max(1, newMax);
        currentHealth = fill ? maxHealth : Mathf.Min(currentHealth, maxHealth);
        if (healthHUD != null) healthHUD.UpdateHealthBar(currentHealth, maxHealth);
    }
    private Coroutine invincibilityCoroutine;

    [FormerlySerializedAs("healthUI")] [SerializeField, HideInInspector] private HealthHUD healthHUD;

    public enum FlashStyle { Colour, Blink }

    [Header("When you get hurt")]
    [ShowAs("Flash when hurt"), Tooltip("The player flashes for a moment when something hurts them.")]
    public bool flashWhenHurt = true;
    [ShowAs("Flash style"), Tooltip("Colour: turns the flash colour on and off. Blink: disappears and reappears.")]
    public FlashStyle flashStyle = FlashStyle.Colour;
    [ShowAs("Flash colour"), Tooltip("The colour it flashes (Colour style).")]
    public Color flashColour = new Color(1f, 0.25f, 0.25f);
    [ShowAs("Flash for (seconds)"), Tooltip("How long the flashing lasts.")]
    [Range(0.1f, 3f)] public float flashTime = 0.6f;
    [ShowAs("Flashes per second"), Tooltip("How fast it flashes.")]
    [Range(1f, 20f)] public float flashesPerSecond = 8f;

    private Coroutine flashCoroutine;
    private System.Collections.Generic.List<Renderer> flashRenderers;
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int GltfColorId = Shader.PropertyToID("baseColorFactor");

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
        if (isInvincible || isDead || BecomeCharacter.InvincibleNow) return;

        currentHealth = Mathf.Max(0, currentHealth - amount);

        // The player model's own "gets hurt" / "lands too hard" animation (and its sound, which replaces the general one)
        CustomModel ownModel = GetComponentInChildren<CustomModel>();
        bool ownSound = false;
        if (currentHealth > 0 && ownModel != null)
        {
            float length = ownModel.PlayMoment(fromFall ? AnimationMoment.LandsTooHard : AnimationMoment.GetsHurt, out ownSound);
            if (length <= 0f && fromFall) length = ownModel.PlayMoment(AnimationMoment.GetsHurt, out ownSound); // no fall animation: use the hurt one
            if (length <= 0f) ownModel.PlayHurt(); // none picked in Game Builder > Sounds: the model's own hurt animation
        }

        if (currentHealth > 0 && flashWhenHurt && amount > 0)
        {
            // Hit again mid-flash: keep flashing the same parts (some may be 'off' right now)
            if (flashCoroutine != null) StopCoroutine(flashCoroutine);
            else flashRenderers = null;
            flashCoroutine = StartCoroutine(FlashRoutine());
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

    /// <summary>Flashes the player's model (colour or blink) for flashTime seconds, then puts it back as it was.</summary>
    private IEnumerator FlashRoutine()
    {
        // Only the renderers showing now (a model hides the original capsule; that stays hidden)
        if (flashRenderers == null)
        {
            flashRenderers = new System.Collections.Generic.List<Renderer>();
            foreach (Renderer r in GetComponentsInChildren<Renderer>())
            {
                if (r.enabled && !(r is ParticleSystemRenderer)) flashRenderers.Add(r);
            }
        }
        System.Collections.Generic.List<Renderer> shown = flashRenderers;
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        block.SetColor(ColorId, flashColour);
        block.SetColor(BaseColorId, flashColour);
        block.SetColor(GltfColorId, flashColour);

        float t = 0f;
        while (t < flashTime)
        {
            bool on = Mathf.Repeat(t * flashesPerSecond, 1f) < 0.5f;
            foreach (Renderer r in shown)
            {
                if (r == null) continue;
                if (flashStyle == FlashStyle.Blink) r.enabled = !on;
                else r.SetPropertyBlock(on ? block : null);
            }
            yield return null;
            t += Time.deltaTime;
        }

        foreach (Renderer r in shown)
        {
            if (r == null) continue;
            r.enabled = true;
            r.SetPropertyBlock(null);
        }
        flashCoroutine = null;
        flashRenderers = null;
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
