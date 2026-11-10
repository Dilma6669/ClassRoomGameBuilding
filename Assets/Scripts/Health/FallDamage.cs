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
    [ShowAs("Safe fall time"), Tooltip("Seconds you can fall without getting hurt at all.")]
    [Range(0.2f, 5f)] public float minFallTime = 1.0f;

    [ShowAs("Deadliest fall time"), Tooltip("Falling this many seconds (or longer) gives the Biggest hurt.")]
    [Range(0.5f, 50f)] public float maxFallTime = 10f;

    [Header("Damage Settings")]
    [ShowAs("Smallest hurt"), Tooltip("Health lost by a fall just longer than the Safe fall time.")]
    [Range(1f, 100f)] public float minDamage = 1f;

    [ShowAs("Biggest hurt"), Tooltip("Health lost by a fall as long as the Deadliest fall time (or longer).")]
    [Range(1f, 1000f)] public float maxDamage = 200f;

    [ShowAs("Hurt multiplier"), Tooltip("Multiplies all fall damage. 2 = falls hurt twice as much.")]
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

        playerHealth.TakeDamage(Mathf.RoundToInt(calculatedDamage) * damageMultiplier, true);
    }
}