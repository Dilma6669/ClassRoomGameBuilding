using UnityEngine;
using KinematicCharacterController;

[ExecuteAlways]
public abstract class ObstacleBase : MonoBehaviour
{
    public enum PayloadType
    {
        None,
        Damage,
        HealthBooster,
        StaminaBooster,
        InvincibilityBuff,
        JumpBuff,
        SprintBuff
    }

    [Header("Transform Settings")] [Range(0.1f, 50f)]
    public float objectScale = 1f;
    [Tooltip("Initial rotation (Euler angles) applied to the obstacle.")]
    [Range(0f, 360f)] public float initialYRotation = 0f;
    
    [Header("Payload Settings")] public PayloadType payloadType = PayloadType.Damage;
    [Range(1, 100f)] public int payloadAmount = 10;
    [Range(1f, 60f)] public float buffDuration = 5f;
    public bool destroyOnTrigger = false;

    [Header("Bounce Settings")] public bool isBouncy = false;
    [Range(0.5f, 5f)] public float triggerRadius = 0.5f;
    [Range(5f, 50f)] public float launchForce = 25f;
    [Range(0f, 1f)] public float upwardBias = 0.5f;
    [Range(0f, 1f)] public float momentumTransfer = 0.3f;

    // Shared Components
    protected MeshCollider physicalMeshCollider;
    protected MeshCollider triggerMeshCollider;
    protected IObstacleMovement currentMovementStrategy;

    protected virtual void OnValidate()
    {
        transform.localScale = Vector3.one;
        for (int i = 0; i < transform.childCount; i++)
        {
            transform.GetChild(i).localScale = Vector3.one * objectScale;
        }
    
        // Lock X and Z to 0, applying rotation strictly around the Y-axis
        transform.localRotation = Quaternion.Euler(0f, initialYRotation, 0f);

        ApplyScale();
    }

    protected virtual void Awake()
    {
        InitializeCollidersAndPhysics();
        currentMovementStrategy = GetComponent<IObstacleMovement>();
    }

    protected virtual void Update()
    {
        if (Application.isPlaying && currentMovementStrategy != null)
        {
            currentMovementStrategy.ProcessMovement(this);
        }
    }

    private void InitializeCollidersAndPhysics()
    {
        transform.localScale = Vector3.one;

        // 1. Solid Collider on "⚠️ DO NOT TOUCH"
        Transform visualChild = transform.Find("⚠️ DO NOT TOUCH");
        if (visualChild != null)
        {
            physicalMeshCollider = visualChild.GetComponent<MeshCollider>();
        }

        // 2. Trigger Collider on "TriggerCollider"
        Transform triggerChild = transform.Find("⚠️ DO NOT TOUCH/TriggerCollider");
        if (triggerChild == null && visualChild != null) triggerChild = visualChild.Find("TriggerCollider");
    
        if (triggerChild != null)
        {
            triggerMeshCollider = triggerChild.GetComponent<MeshCollider>();
            if (triggerMeshCollider != null)
            {
                triggerMeshCollider.isTrigger = true;
                triggerMeshCollider.enabled = isBouncy || payloadType != PayloadType.None;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!Application.isPlaying) return;

        KinematicCharacterMotor motor = other.GetComponentInParent<KinematicCharacterMotor>();
        if (motor == null) motor = other.GetComponent<KinematicCharacterMotor>();

        if (motor != null)
        {
            if (isBouncy)
            {
                Vector3 surfaceNormal = (motor.TransientPosition - transform.position).normalized;
                if (surfaceNormal == Vector3.zero) surfaceNormal = Vector3.up;

                Vector3 launchDirection = Vector3.Lerp(surfaceNormal, Vector3.up, upwardBias).normalized;
                motor.ForceUnground();
                float totalLaunchSpeed = launchForce + (motor.BaseVelocity.magnitude * momentumTransfer);
                motor.BaseVelocity = launchDirection * totalLaunchSpeed;
            }

            var playerLogic = other.GetComponentInParent<PlayerLogic>();

            switch (payloadType)
            {
                case PayloadType.Damage:
                    var damagable = other.GetComponentInParent<IDamagable>();
                    if (damagable != null) damagable.TakeDamage(payloadAmount);
                    break;

                case PayloadType.HealthBooster:
                    var playerHealth = other.GetComponentInParent<Health>();
                    if (playerHealth != null) playerHealth.Heal(payloadAmount);
                    break;

                case PayloadType.StaminaBooster:
                    if (playerLogic != null) playerLogic.RestoreStamina(payloadAmount);
                    break;

                case PayloadType.InvincibilityBuff:
                    var healthComp = other.GetComponentInParent<Health>();
                    if (healthComp != null) healthComp.ApplyInvincibility(buffDuration);
                    break;

                case PayloadType.JumpBuff:
                    if (playerLogic != null) playerLogic.ApplyJumpBuff(payloadAmount, buffDuration);
                    break;

                case PayloadType.SprintBuff:
                    if (playerLogic != null) playerLogic.ApplySprintBuff(payloadAmount, buffDuration);
                    break;
            }

            if (destroyOnTrigger && payloadType != PayloadType.None)
            {
                Destroy(gameObject);
            }
        }
    }

    public void ApplyScale()
    {
        transform.localScale = Vector3.one;
        for (int i = 0; i < transform.childCount; i++)
        {
            transform.GetChild(i).localScale = Vector3.one * objectScale;
        }
    }

    protected virtual void OnDrawGizmosSelected()
    {
        if (isBouncy)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, triggerRadius * objectScale);
        }
    }
}