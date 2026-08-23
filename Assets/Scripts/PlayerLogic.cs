using UnityEngine;
using KinematicCharacterController.Examples;

[ExecuteAlways]
public class PlayerLogic : MonoBehaviour
{
    [Header("Player Controls")]
    [Range(1f, 30f)] public float moveSpeed = 10f;
    [Range(1f, 30f)] public float jumpHeight = 10f;

    [Header("Sprint Settings")]
    public bool enableSprint = true;
    public KeyCode sprintKey = KeyCode.LeftShift;
    [Range(1.1f, 3f)] public float sprintMultiplier = 1.8f;

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

            // Check if sprinting in Play mode
            if (Application.isPlaying && enableSprint && Input.GetKey(sprintKey))
            {
                targetSpeed *= sprintMultiplier;
            }

            // Apply base or boosted values
            characterController.MaxStableMoveSpeed = targetSpeed;
            characterController.JumpUpSpeed = jumpHeight;
        }
    }
}