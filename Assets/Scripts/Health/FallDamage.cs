using KinematicCharacterController;
using UnityEngine;

[RequireComponent(typeof(KinematicCharacterMotor))]
public class FallDamage : MonoBehaviour
{
    [Header("Fall Time Settings")]
    [Tooltip("How many seconds the player can fall before taking ANY damage.")]
    [Range(0.2f, 5f)] public float minFallTime = 1.0f;

    [Tooltip("How many seconds of falling deals MAXIMUM damage.")]
    [Range(0.5f, 10f)] public float maxFallTime = 10f;

    [Header("Damage Settings")]
    [Tooltip("Damage dealt when falling duration reaches minFallTime.")]
    [Range(1f, 100f)] public float minDamage = 1f;

    [Tooltip("Damage dealt when falling duration reaches maxFallTime.")]
    [Range(1f, 200f)] public float maxDamage = 200f;

    //[Header("References")]
   // [Tooltip("The Health script on this player object.")]
    private Health playerHealth;

    private KinematicCharacterMotor motor;
    private float currentFallTimer = 0f;
    private bool wasGrounded = true;

    private void Awake()
    {
        motor = GetComponent<KinematicCharacterMotor>();
        
        if (playerHealth == null)
        {
            playerHealth = GetComponent<Health>();
        }
    }

    private void Update()
    {
        if (motor == null) return;

        bool isGrounded = motor.GroundingStatus.IsStableOnGround;

        if (!isGrounded)
        {
            // Only count time spent actually moving DOWNWARD so jumping up doesn't add to fall time
            if (motor.BaseVelocity.y < 0f)
            {
                currentFallTimer += Time.deltaTime;
            }
        }
        else if (!wasGrounded && isGrounded)
        {
            // Landed! Evaluate total falling duration
            ProcessLandingDamage(currentFallTimer);
            
            // Reset timer
            currentFallTimer = 0f;
        }

        wasGrounded = isGrounded;
    }

    private void ProcessLandingDamage(float fallDuration)
    {
        if (fallDuration < minFallTime || playerHealth == null) return;

        // Calculate proportional damage between minDamage and maxDamage based on seconds in air
        float timePercent = Mathf.InverseLerp(minFallTime, maxFallTime, fallDuration);
        float calculatedDamage = Mathf.Lerp(minDamage, maxDamage, timePercent);

        playerHealth.TakeDamage(Mathf.RoundToInt(calculatedDamage));
    }
}