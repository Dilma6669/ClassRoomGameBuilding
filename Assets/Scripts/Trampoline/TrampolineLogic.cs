using UnityEngine;
using KinematicCharacterController;

public class TrampolineLogic : MonoBehaviour
{
    [Header("Trampoline Toggle")]
    [Tooltip("Enable or disable the trampoline bounce effect.")]
    public bool isBouncy = true;

    [Header("Launch Physics")]
    [Tooltip("How much force is applied to the player when touching the trampoline.")]
    [Range(0f, 100f)] public float launchForce = 25f;

    [Tooltip("How much of the players running or falling speed gets added to your jump!")]
    [Range(0f, 1f)] public float momentumTransfer = 0.3f;

    // Called directly from TrampolineTriggerBridge on the child object
    public void HandlePlayerEnter(Collider other)
    {
        // Early return if bounciness is disabled
        if (!isBouncy) return;

        KinematicCharacterMotor motor = other.GetComponentInParent<KinematicCharacterMotor>();

        if (motor != null)
        {
            // 1. Get player's contact position (feet)
            Vector3 playerFeet = motor.TransientPosition;

            // 2. Calculate radial outward vector from sphere center to player
            Vector3 sphereCenter = transform.position;
            Vector3 surfaceNormal = (playerFeet - sphereCenter).normalized;

            Debug.DrawRay(playerFeet, surfaceNormal * 4f, Color.red, 2.0f);

            // 3. Apply launch velocity along the surface normal
            motor.ForceUnground();
            float incomingSpeed = motor.BaseVelocity.magnitude;
            float totalLaunchSpeed = launchForce + (incomingSpeed * momentumTransfer);

            motor.BaseVelocity = surfaceNormal * totalLaunchSpeed;
        }
    }
}