using System.Collections;
using UnityEngine;

#if HAS_KINEMATIC_CC
using KinematicCharacterController.Examples;
#endif

[ExecuteAlways]
public class PlayerLogic : MonoBehaviour
{
    [Header("Movement Controls")]
    [Range(1f, 300f)] public float moveSpeed = 10f;
    public bool enableSprint = true;
    public KeyCode sprintKey = KeyCode.LeftShift;
    [ShowAs("Sprint speed (× normal)"), Tooltip("How much faster you go while holding the sprint key. 2 = twice as fast.")]
    [Range(1f, 4f)] public float sprintMultiplier = 1.8f;

    [Header("Stamina Settings")]
    [Range(10f, 200f)] public float maxStamina = 100f;
    [ShowAs("Stamina used per second"), Tooltip("How much stamina sprinting uses up each second.")]
    [Range(5f, 100f)] public float staminaDrainRate = 25f;
    [ShowAs("Stamina refill speed"), Tooltip("How much stamina comes back each second after you stop sprinting.")]
    [Range(5f, 100f)] public float staminaRegenRate = 15f;
    [ShowAs("Wait before refilling"), Tooltip("Seconds to wait after sprinting before stamina starts coming back.")]
    [Range(0f, 5f)] public float regenDelay = 1f;

    [HideInInspector] public StaminaHUD staminaHUD;

    [Header("Jump Controls")]
    [Range(1f, 300f)] public float jumpHeight = 10f;

    [ShowAs("Forward push when jumping"), Tooltip("An extra push forwards when you jump while moving.")]
    [Range(0f, 20f)] public float jumpForwardBoost = 2f;

    [ShowAs("Steering in the air"), Tooltip("How well you can change direction while you're in the air.")]
    [Range(0f, 50f)] public float airSteerSpeed = 15f;

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