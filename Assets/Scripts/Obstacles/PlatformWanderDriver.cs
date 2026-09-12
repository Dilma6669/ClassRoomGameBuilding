using UnityEngine;

[RequireComponent(typeof(PlatformObstacle))]
public class PlatformWanderDriver : MonoBehaviour, IObstacleMovement
{
    [Header("Wander Settings")]
    [Range(0.1f, 1000f)] public float minWanderRadius = 2f;
    [Range(0.1f, 1000f)] public float maxWanderRadius = 6f;
    [Range(0.1f, 1000f)] public float minMoveSpeed = 2f;
    [Range(0.1f, 1000f)] public float maxMoveSpeed = 5f;

    [Header("Steering & Fluidity")]
    [Tooltip("Multiplier for turn speed. Higher values allow sharper turns without stopping.")]
    [Range(90f, 1080f)] private float turnSpeedMultiplier = 1000f;

    [Tooltip("Distance threshold to trigger target picking before reaching the exact point (prevents stopping).")]
    [Range(0.3f, 3f)] private float arrivalThreshold = 1.0f;

    private PlatformObstacle platformHost;
    private Vector3 currentWanderOffset;
    private float currentMoveSpeed;
    
    private Vector3 localAnchorPosition;
    private Transform platformTransform;

    private void Awake()
    {
        platformHost = GetComponent<PlatformObstacle>();
    }

    private void Start()
    {
        CachePlatformAnchor();
        PickNewWanderTarget();
    }

    private void CachePlatformAnchor()
    {
        if (platformHost != null && platformHost.GetPlatformTransform() != null)
        {
            platformTransform = platformHost.GetPlatformTransform();
        }
        else
        {
            platformTransform = transform.parent;
        }

        if (platformTransform != null)
        {
            localAnchorPosition = platformTransform.InverseTransformPoint(transform.position);
        }
        else
        {
            localAnchorPosition = transform.localPosition;
        }
    }

    public void ProcessMovement(ObstacleBase host)
    {
        if (platformTransform == null) CachePlatformAnchor();

        // 1. Calculate unscaled world center anchor
        Vector3 anchorWorld = platformTransform != null 
            ? platformTransform.TransformPoint(localAnchorPosition) 
            : transform.position;

        // 2. Rotate offset by platform orientation ONLY
        Quaternion platformRotation = platformTransform != null ? platformTransform.rotation : Quaternion.identity;
        Vector3 targetWorldPos = anchorWorld + (platformRotation * currentWanderOffset);

        // 3. Movement delta along the platform surface
        Vector3 worldDelta = targetWorldPos - transform.position;

        Vector3 platformUp = platformTransform != null ? platformTransform.up : Vector3.up;
        worldDelta = Vector3.ProjectOnPlane(worldDelta, platformUp);

        float distanceToTarget = worldDelta.magnitude;

        // 4. Smooth Transition: Swap to next target early so momentum is preserved
        if (distanceToTarget <= arrivalThreshold)
        {
            PickNewWanderTarget();
            
            // Re-evaluate delta for new target immediately
            targetWorldPos = anchorWorld + (platformRotation * currentWanderOffset);
            worldDelta = Vector3.ProjectOnPlane(targetWorldPos - transform.position, platformUp);
        }

        Vector3 moveDir = worldDelta.normalized;
        if (moveDir.sqrMagnitude > 0.001f)
        {
            // 5. High-speed smooth turning
            Quaternion targetRot = Quaternion.LookRotation(moveDir, platformUp);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, turnSpeedMultiplier * Time.deltaTime);

            // 6. Smooth Speed Curve (No hard stops—maintains continuous forward motion)
            float angleToTarget = Vector3.Angle(transform.forward, moveDir);
            float smoothSpeedFactor = Mathf.Lerp(0.4f, 1.0f, Mathf.Cos(angleToTarget * Mathf.Deg2Rad));

            transform.Translate(Vector3.forward * (currentMoveSpeed * smoothSpeedFactor * Time.deltaTime), Space.Self);
        }
    }

    public void PickNewWanderTarget()
    {
        currentMoveSpeed = Random.Range(minMoveSpeed, maxMoveSpeed);
        
        // Pick a random direction and exact distance in unscaled local space
        Vector2 randomCircle = Random.insideUnitCircle.normalized;
        float dist = Random.Range(minWanderRadius, maxWanderRadius);
        
        currentWanderOffset = new Vector3(randomCircle.x * dist, 0f, randomCircle.y * dist);
    }

    private void OnDrawGizmosSelected()
    {
        Transform activePlatform = platformTransform;
        if (activePlatform == null)
        {
            PlatformObstacle host = GetComponent<PlatformObstacle>();
            activePlatform = (host != null && host.GetPlatformTransform() != null) ? host.GetPlatformTransform() : transform.parent;
        }

        Vector3 centerWorld;

        if (Application.isPlaying && activePlatform != null)
        {
            centerWorld = activePlatform.TransformPoint(localAnchorPosition);
        }
        else
        {
            centerWorld = transform.position;
        }

        // Outer & Inner Wander Spheres
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(centerWorld, maxWanderRadius);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(centerWorld, minWanderRadius);

        // Active Target Sphere & Path Line
        if (Application.isPlaying && activePlatform != null)
        {
            Quaternion platformRotation = activePlatform.rotation;
            Vector3 targetWorld = centerWorld + (platformRotation * currentWanderOffset);

            Gizmos.color = Color.green;
            Gizmos.DrawSphere(targetWorld, 0.35f);

            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, targetWorld);
        }
    }
}