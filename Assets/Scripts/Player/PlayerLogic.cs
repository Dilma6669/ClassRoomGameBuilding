using System.Collections;
using UnityEngine;

#if HAS_KINEMATIC_CC
using KinematicCharacterController.Examples;
#endif

[ExecuteAlways]
public class PlayerLogic : MonoBehaviour
{
    [Header("Movement Controls")]
    [Range(1f, 1000f)] public float moveSpeed = 10f;
    public bool enableSprint = true;
    public KeyCode sprintKey = KeyCode.LeftShift;
    [ShowAs("Sprint speed (× normal)"), Tooltip("How much faster you go while holding the sprint key. 2 = twice as fast.")]
    [Range(1f, 20f)] public float sprintMultiplier = 1.8f;

    [Header("Stamina Settings")]
    [Range(10f, 10000f)] public float maxStamina = 100f;
    [ShowAs("Stamina used per second"), Tooltip("How much stamina sprinting uses up each second.")]
    [Range(1f, 100f)] public float staminaDrainRate = 12.5f; // 100 stamina lasts 8 seconds of sprinting
    [ShowAs("Stamina refill speed"), Tooltip("How much stamina comes back each second after you stop sprinting.")]
    [Range(5f, 100f)] public float staminaRegenRate = 30f;
    [ShowAs("Wait before refilling"), Tooltip("Seconds to wait after sprinting before stamina starts coming back.")]
    [Range(0f, 5f)] public float regenDelay = 1f;

    [HideInInspector] public StaminaHUD staminaHUD;

    // Levels saved before 2026-11-13 used 25 stamina per second (4 s of sprinting): upgraded once to the new default
    [SerializeField, HideInInspector] private int settingsVersion;

    private void UpgradeOldSettings()
    {
        if (settingsVersion >= 2) return;
        if (settingsVersion < 1 && Mathf.Approximately(staminaDrainRate, 25f)) staminaDrainRate = 12.5f;
        // 2026-11-14: stamina refills twice as fast (15 -> 30 per second)
        if (Mathf.Approximately(staminaRegenRate, 15f)) staminaRegenRate = 30f;
        settingsVersion = 2;
    }

#if UNITY_EDITOR
    private void OnValidate() => UpgradeOldSettings();
#endif

    [Header("Jump Controls")]
    [Range(1f, 1000f)] public float jumpHeight = 10f;

    [ShowAs("Forward push when jumping"), Tooltip("An extra push forwards when you jump while moving.")]
    [Range(0f, 200f)] public float jumpForwardBoost = 2f;

    [ShowAs("Steering in the air"), Tooltip("How well you can change direction while you're in the air.")]
    [Range(0f, 200f)] public float airSteerSpeed = 15f;

#if HAS_KINEMATIC_CC
    private ExampleCharacterController characterController;
#else
    private Rigidbody fallbackRigidbody;
#endif

    /// <summary>True while the player is actually sprinting (used for the Sprint sound).</summary>
    public bool IsSprinting { get; private set; }

    private float currentStamina;
    private float regenTimer;
    private bool isExhausted = false;

    // Buff trackers
    private Coroutine jumpBuffCoroutine;
    private Coroutine sprintBuffCoroutine;
    private float activeSprintMultiplier;
    private float activeJumpHeight;

    private void Awake()
    {
        UpgradeOldSettings();
        FindCharacterController();
        FindStaminaHUD();
        currentStamina = maxStamina;
        ResetBuffValues();
    }

    private void FindCharacterController()
    {
#if HAS_KINEMATIC_CC
        if (characterController == null)
        {
            characterController = GetComponentInChildren<ExampleCharacterController>();
        }
#else
        if (fallbackRigidbody == null)
        {
            fallbackRigidbody = GetComponentInChildren<Rigidbody>();
        }
#endif
    }

    private void FindStaminaHUD()
    {
        if (staminaHUD == null)
        {
            staminaHUD = FindAnyObjectByType<StaminaHUD>();
        }
    }

    private void Start()
    {
        FindCharacterController();
        FindStaminaHUD();

        if (Application.isPlaying)
        {
            currentStamina = maxStamina;
        }
        ResetBuffValues();
        ApplyMovementSettings();
    }

    private void Update()
    {
        FindCharacterController();

        if (Application.isPlaying && staminaHUD == null)
        {
            FindStaminaHUD();
        }

        HandleStamina();
        ApplyMovementSettings();
    }

    /// <summary>Call after changing jumpHeight / sprintMultiplier from code (power-ups that are on keep going).</summary>
    public void SettingsChanged() => ResetBuffValues();

    private void ResetBuffValues()
    {
        if (sprintBuffCoroutine == null) activeSprintMultiplier = sprintMultiplier;
        if (jumpBuffCoroutine == null) activeJumpHeight = jumpHeight;
    }

    public void RestoreStamina(float amount)
    {
        currentStamina = Mathf.Min(currentStamina + amount, maxStamina);

        if (isExhausted && currentStamina >= maxStamina * 0.15f)
        {
            isExhausted = false;
        }

        if (staminaHUD != null)
        {
            staminaHUD.UpdateStaminaBar(currentStamina, maxStamina, isExhausted);
        }
    }

    /// <summary>Stamina is used up and still refilling (no sprinting, and no shooting for characters that shoot with stamina).</summary>
    public bool StaminaExhausted => isExhausted;

    /// <summary>Uses some stamina (a shooting character's shot). Returns false (and uses none) while it's run out.</summary>
    public bool UseStamina(float amount)
    {
        if (isExhausted) return false;
        currentStamina -= Mathf.Max(0f, amount);
        regenTimer = regenDelay;
        if (currentStamina <= 0f) { currentStamina = 0f; isExhausted = true; }
        if (staminaHUD != null) staminaHUD.UpdateStaminaBar(currentStamina, maxStamina, isExhausted);
        return true;
    }

    /// <summary>While a Super Jump that protects you is on, falling doesn't hurt (FallDamage checks this).</summary>
    public bool NoFallDamageNow { get; private set; }

    /// <summary>While a Speed Boost that gives free sprinting is on, sprinting doesn't use stamina.</summary>
    public bool FreeSprintNow { get; private set; }

    public void ApplyJumpBuff(float multiplier, float duration) => ApplyJumpBuff(multiplier, duration, false);

    /// <param name="noFallDamage">Falling doesn't hurt while it lasts.</param>
    public void ApplyJumpBuff(float multiplier, float duration, bool noFallDamage)
    {
        if (jumpBuffCoroutine != null) StopCoroutine(jumpBuffCoroutine);
        jumpBuffCoroutine = StartCoroutine(JumpBuffRoutine(multiplier, duration, noFallDamage));
    }

    private IEnumerator JumpBuffRoutine(float multiplier, float duration, bool noFallDamage)
    {
        activeJumpHeight = jumpHeight * multiplier;
        NoFallDamageNow = noFallDamage;
        yield return new WaitForSeconds(duration);
        activeJumpHeight = jumpHeight;
        NoFallDamageNow = false;
        jumpBuffCoroutine = null;
    }

    public void ApplySprintBuff(float multiplier, float duration) => ApplySprintBuff(multiplier, duration, false);

    /// <param name="freeSprint">Sprinting doesn't use stamina while it lasts.</param>
    public void ApplySprintBuff(float multiplier, float duration, bool freeSprint)
    {
        if (sprintBuffCoroutine != null) StopCoroutine(sprintBuffCoroutine);
        sprintBuffCoroutine = StartCoroutine(SprintBuffRoutine(multiplier, duration, freeSprint));
    }

    private IEnumerator SprintBuffRoutine(float multiplier, float duration, bool freeSprint)
    {
        activeSprintMultiplier = multiplier;
        FreeSprintNow = freeSprint;
        if (freeSprint) isExhausted = false; // you can sprint straight away
        yield return new WaitForSeconds(duration);
        activeSprintMultiplier = sprintMultiplier;
        FreeSprintNow = false;
        sprintBuffCoroutine = null;
    }

    private void HandleStamina()
    {
        if (!Application.isPlaying) return;

        bool isTryingToSprint = enableSprint && Input.GetKey(sprintKey) && IsMoving();

        if (isExhausted && currentStamina >= maxStamina * 0.15f)
        {
            isExhausted = false;
        }

        if (isTryingToSprint && FreeSprintNow)
        {
            // Speed Boost with free sprinting: no stamina used (and none comes back either)
        }
        else if (isTryingToSprint && !isExhausted)
        {
            currentStamina -= staminaDrainRate * Time.deltaTime;
            regenTimer = regenDelay;

            if (currentStamina <= 0f)
            {
                currentStamina = 0f;
                isExhausted = true;
            }
        }
        else
        {
            if (regenTimer > 0f)
            {
                regenTimer -= Time.deltaTime;
            }
            else if (currentStamina < maxStamina)
            {
                currentStamina += staminaRegenRate * Time.deltaTime;
                currentStamina = Mathf.Min(currentStamina, maxStamina);
            }
        }

        if (staminaHUD != null)
        {
            staminaHUD.UpdateStaminaBar(currentStamina, maxStamina, isExhausted);
        }
    }

    private bool IsMoving()
    {
#if HAS_KINEMATIC_CC
        if (characterController == null) return false;
        Vector3 horizontalVelocity = characterController.Motor.BaseVelocity;
        horizontalVelocity.y = 0f;
        return horizontalVelocity.sqrMagnitude > 0.1f;
#else
        if (fallbackRigidbody == null) return false;
        Vector3 horizontalVelocity = fallbackRigidbody.linearVelocity;
        horizontalVelocity.y = 0f;
        return horizontalVelocity.sqrMagnitude > 0.1f;
#endif
    }

    private void ApplyMovementSettings()
    {
#if HAS_KINEMATIC_CC
        if (characterController != null)
        {
            float targetSpeed = moveSpeed;

            bool canSprint = Application.isPlaying && enableSprint && !isExhausted && Input.GetKey(sprintKey) && IsMoving() && currentStamina > 0f;

            IsSprinting = canSprint;
            if (canSprint)
            {
                targetSpeed *= activeSprintMultiplier;
            }

            characterController.MaxStableMoveSpeed = targetSpeed;
            characterController.JumpUpSpeed = activeJumpHeight;
            characterController.JumpScalableForwardSpeed = jumpForwardBoost;
            characterController.AirAccelerationSpeed = airSteerSpeed;
            characterController.MaxAirMoveSpeed = targetSpeed;
        }
#endif
    }
}