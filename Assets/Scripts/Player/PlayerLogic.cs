using UnityEngine;
using KinematicCharacterController.Examples;

[ExecuteAlways]
public class PlayerLogic : MonoBehaviour
{
    [Header("Movement Controls")]
    [Range(1f, 30f)] public float moveSpeed = 10f;
    public bool enableSprint = true;
    public KeyCode sprintKey = KeyCode.LeftShift;
    [Range(1.1f, 3f)] public float sprintMultiplier = 1.8f;

    [Header("Stamina Settings")]
    [Range(10f, 200f)] public float maxStamina = 100f;
    [Tooltip("How fast stamina depletes per second while sprinting.")]
    [Range(5f, 100f)] public float staminaDrainRate = 25f;
    [Tooltip("How fast stamina regenerates per second while resting.")]
    [Range(5f, 100f)] public float staminaRegenRate = 15f;
    [Tooltip("Delay in seconds before stamina starts regenerating after sprinting.")]
    [Range(0f, 5f)] public float regenDelay = 1f;

    //[Header("UI Reference")]
    //[Tooltip("Will automatically find StaminaHUD in scene on game load if left empty.")]
   [HideInInspector] public StaminaHUD staminaHUD;

    [Header("Jump Controls")]
    [Range(1f, 30f)] public float jumpHeight = 10f;

    [Tooltip("Extra forward boost applied on jump.")]
    [Range(0f, 20f)] public float jumpForwardBoost = 2f;

    [Tooltip("Controls how fast you can steer or gain horizontal speed while mid-air.")]
    [Range(0f, 50f)] public float airSteerSpeed = 15f;

    private ExampleCharacterController characterController;
    private float currentStamina;
    private float regenTimer;
    private bool isExhausted = false;

    private void Awake()
    {
        FindCharacterController();
        FindStaminaHUD();
        currentStamina = maxStamina;
    }

    private void FindCharacterController()
    {
        if (characterController == null)
        {
            characterController = GetComponentInChildren<ExampleCharacterController>();
        }
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

    private void HandleStamina()
    {
        if (!Application.isPlaying) return;

        bool isTryingToSprint = enableSprint && Input.GetKey(sprintKey) && IsMoving();

        // Unlock exhausted lock once player recovers above 15% stamina
        if (isExhausted && currentStamina >= maxStamina * 0.15f)
        {
            isExhausted = false;
        }

        if (isTryingToSprint && !isExhausted)
        {
            // Drain stamina while sprinting
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
            // Regenerate stamina after delay
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

        // Send updates directly to StaminaHUD
        if (staminaHUD != null)
        {
            staminaHUD.UpdateStaminaBar(currentStamina, maxStamina, isExhausted);
        }
    }

    private bool IsMoving()
    {
        if (characterController == null) return false;
        Vector3 horizontalVelocity = characterController.Motor.BaseVelocity;
        horizontalVelocity.y = 0f;
        return horizontalVelocity.sqrMagnitude > 0.1f;
    }

    private void ApplyMovementSettings()
    {
        if (characterController != null)
        {
            float targetSpeed = moveSpeed;

            bool canSprint = Application.isPlaying && enableSprint && !isExhausted && Input.GetKey(sprintKey) && IsMoving() && currentStamina > 0f;

            if (canSprint)
            {
                targetSpeed *= sprintMultiplier;
            }

            characterController.MaxStableMoveSpeed = targetSpeed;
            characterController.JumpUpSpeed = jumpHeight;
            characterController.JumpScalableForwardSpeed = jumpForwardBoost;
            characterController.AirAccelerationSpeed = airSteerSpeed;
            characterController.MaxAirMoveSpeed = targetSpeed;
        }
    }
}