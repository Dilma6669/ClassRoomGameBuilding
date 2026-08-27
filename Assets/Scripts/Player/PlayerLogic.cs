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

    [Header("Jump Controls")]
    [Range(1f, 30f)] public float jumpHeight = 10f;

    [Tooltip("Extra forward boost applied on jump. Set lower (e.g. 0 to 2) to prevent forward jumps from overshooting.")]
    [Range(0f, 20f)] public float jumpForwardBoost = 2f;

    [Tooltip("Controls how fast you can steer or gain horizontal speed while mid-air.")]
    [Range(0f, 50f)] public float airSteerSpeed = 15f;

    private ExampleCharacterController characterController;

    private void Awake()
    {
        FindCharacterController();
    }

    private void FindCharacterController()
    {
        if (characterController == null)
        {
            characterController = GetComponentInChildren<ExampleCharacterController>();
        }
    }

    private void Start()
    {
        FindCharacterController();
        ApplyMovementSettings();
    }

    private void Update()
    {
        FindCharacterController();
        ApplyMovementSettings();
    }

    private void ApplyMovementSettings()
    {
        if (characterController != null)
        {
            float targetSpeed = moveSpeed;

            if (Application.isPlaying && enableSprint && Input.GetKey(sprintKey))
            {
                targetSpeed *= sprintMultiplier;
            }

            characterController.MaxStableMoveSpeed = targetSpeed;
            characterController.JumpUpSpeed = jumpHeight;

            // Target the internal variables driving jump momentum
            characterController.JumpScalableForwardSpeed = jumpForwardBoost;
            characterController.AirAccelerationSpeed = airSteerSpeed;
            characterController.MaxAirMoveSpeed = targetSpeed;
        }
    }
}