using UnityEngine;

#if HAS_KINEMATIC_CC
using KinematicCharacterController;
#endif

#if HAS_KINEMATIC_CC
[RequireComponent(typeof(KinematicCharacterMotor))]
#endif
public class FallDamage : MonoBehaviour
{
    public enum FallMeasure { Time, Height }

    [ShowAs("Measure falls by"), Tooltip("Time: how many seconds you fall. Height: how many metres you drop (the further you fall, the more it hurts).")]
    public FallMeasure measureBy = FallMeasure.Time;

    [Header("If measuring by Height (metres)")]
    [ShowAs("Safe drop (m)"), Tooltip("You can drop this many metres without getting hurt at all.")]
    [Range(1f, 200f)] public float safeFallHeight = 8f;
    [ShowAs("Deadliest drop (m)"), Tooltip("Dropping this many metres (or more) gives the Biggest hurt.")]
    [Range(2f, 2000f)] public float deadliestFallHeight = 100f;

    [Header("If measuring by Time (seconds)")]
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
    private KinematicCharacterController.Examples.ExampleCharacterController flyer;
#endif

    private float currentFallTimer = 0f;
    private float fallTopY = float.NegativeInfinity; // the highest point since you left the ground
    private bool wasGrounded = true;

    // A Super Jump that protects you: a fall that happens (or starts) while it's on doesn't hurt
    private PlayerLogic playerLogic;
    private bool protectedFall;

    private void Awake()
    {
        playerHealth = GetComponent<Health>();
        playerLogic = GetComponentInParent<PlayerLogic>();
        if (playerLogic == null) playerLogic = FindAnyObjectByType<PlayerLogic>();

#if HAS_KINEMATIC_CC
        motor = GetComponent<KinematicCharacterMotor>();
        flyer = GetComponent<KinematicCharacterController.Examples.ExampleCharacterController>();
#endif
    }

    private void Update()
    {
#if HAS_KINEMATIC_CC
        if (motor == null) return;

        bool isGrounded = motor.GroundingStatus.IsStableOnGround;

        if (!isGrounded)
        {
            // Flying: coming down on purpose never counts as falling (if the flying ends in the air, the fall counts from then)
            if (flyer != null && flyer.IsFlying) { currentFallTimer = 0f; fallTopY = motor.TransientPosition.y; }
            else
            {
                fallTopY = Mathf.Max(fallTopY, motor.TransientPosition.y);
                if (motor.BaseVelocity.y < 0f) currentFallTimer += Time.deltaTime;
            }
            if ((playerLogic != null && playerLogic.NoFallDamageNow) || BecomeCharacter.NoFallDamageNow) protectedFall = true;
        }
        else if (!wasGrounded && isGrounded)
        {
            float drop = float.IsNegativeInfinity(fallTopY) ? 0f : fallTopY - motor.TransientPosition.y;
            if (!protectedFall && !LandedSafely()) ProcessLandingDamage(currentFallTimer, drop);
            currentFallTimer = 0f;
            fallTopY = float.NegativeInfinity;
            protectedFall = false;
        }

        wasGrounded = isGrounded;
#endif
    }

#if HAS_KINEMATIC_CC
    /// <summary>Landed on a platform, creature, pickup or scenery with 'Landing on it never hurts' ticked?</summary>
    private bool LandedSafely()
    {
        return SafeLanding.IsSafe(motor.GroundingStatus.GroundCollider);
    }
#endif

    private void ProcessLandingDamage(float fallDuration, float dropMetres)
    {
        if (playerHealth == null) return;
        float timePercent;
        if (measureBy == FallMeasure.Height)
        {
            if (dropMetres < safeFallHeight) return;
            timePercent = Mathf.InverseLerp(safeFallHeight, Mathf.Max(safeFallHeight + 0.1f, deadliestFallHeight), dropMetres);
        }
        else
        {
            if (fallDuration < minFallTime) return;
            timePercent = Mathf.InverseLerp(minFallTime, maxFallTime, fallDuration);
        }
        float calculatedDamage = Mathf.Lerp(minDamage, maxDamage, timePercent);

        playerHealth.TakeDamage(Mathf.RoundToInt(calculatedDamage) * damageMultiplier, true);
    }
}