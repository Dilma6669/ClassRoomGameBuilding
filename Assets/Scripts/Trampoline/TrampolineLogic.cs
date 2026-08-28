using UnityEngine;
using KinematicCharacterController;

public class TrampolineLogic : MonoBehaviour
{
    [Header("Bounce Settings")]
    [Tooltip("Multiplier applied to the incoming speed (e.g., 1 = maintain speed, 1.5 = boost speed).")]
    public float bounceMultiplier = 1.2f;

    [Tooltip("Minimum horizontal force applied so the player still bounces back if walking slowly.")]
    public float minimumBounceForce = 10f;

    [Tooltip("Extra upward force to launch the character clear of the trampoline pad.")]
    public float upwardPopForce = 5f;

    private void OnTriggerEnter(Collider other)
    {
        KinematicCharacterMotor motor = other.GetComponent<KinematicCharacterMotor>();

        if (motor != null)
        {
            motor.ForceUnground();

            Vector3 incomingVelocity = motor.BaseVelocity;

            // Direct inversion: Flip horizontal velocity completely backwards
            Vector3 reversedDirection = -incomingVelocity;

            // Calculate return speed based on incoming velocity or minimum floor
            float currentSpeed = incomingVelocity.magnitude;
            float targetSpeed = Mathf.Max(currentSpeed * bounceMultiplier, minimumBounceForce);

            // Blend inverted momentum with the trampoline's facing vector (upward pop)
            Vector3 finalVelocity = (reversedDirection.normalized * targetSpeed) + (transform.up * upwardPopForce);

            motor.BaseVelocity = finalVelocity;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, transform.up * 1.5f);
    }
}