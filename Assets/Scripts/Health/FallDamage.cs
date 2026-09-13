using UnityEngine;

#if HAS_KINEMATIC_CC
using KinematicCharacterController;
#endif

#if HAS_KINEMATIC_CC
[RequireComponent(typeof(KinematicCharacterMotor))]
#endif
public class FallDamage : MonoBehaviour
{
    [Header("Fall Time Settings")]
    [Tooltip("How many seconds the player can fall before taking ANY damage.")]
    [Range(0.2f, 5f)] public float minFallTime = 1.0f;

    [Tooltip("How many seconds of falling deals MAXIMUM damage.")]
    [Range(0.5f, 50f)] public float maxFallTime = 10f;

    [Header("Damage Settings")]
    [Tooltip("Damage dealt when falling duration reaches minFallTime.")]
    [Range(1f, 100f)] public float minDamage = 1f;

    [Tooltip("Damage dealt when falling duration reaches maxFallTime.")]
    [Range(1f, 1000f)] public float maxDamage = 200f;

    public int damageMultiplier = 1;

    private Health playerHealth;

#if HAS_KINEMATIC_CC
    private KinematicCharacterMotor motor;
#endif

    private float currentFallTimer = 0f;
    private bool wasGrounded = true;

    private void Awake()
    {
        playerHealth = GetComponent<Health>();

#if HAS_KINEMATIC_CC
        motor = GetComponent<KinematicCharacterMotor>();
#endif
    }

    private void Update()
    {
#if HAS_KINEMATIC_CC
        if (motor == null) return;

        bool isGrounded = motor.GroundingStatus.IsStableOnGround;

        if (!isGrounded)
        {
            if (motor.BaseVelocity.y < 0f)
            {
                currentFallTimer += Time.deltaTime;
            }
        }
        else if (!wasGrounded && isGrounded)
        {
            ProcessLandingDamage(currentFallTimer);
            currentFallTimer = 0f;
        }

        wasGrounded = isGrounded;
#endif
    }

    private void ProcessLandingDamage(float fallDuration)
    {
        if (fallDuration < minFallTime || playerHealth == null) return;

        float timePercent = Mathf.InverseLerp(minFallTime, maxFallTime, fallDuration);
        float calculatedDamage = Mathf.Lerp(minDamage, maxDamage, timePercent);

        playerHealth.TakeDamage(Mathf.RoundToInt(calculatedDamage) * damageMultiplier);
    }
}