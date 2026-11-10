using UnityEngine;

[RequireComponent(typeof(PlatformObstacle))]
public class PlatformWanderDriver : MonoBehaviour, IObstacleMovement, IObstacleAnchor
{
    [Header("Wander Settings")]
    [ShowAs("How far it wanders"), Tooltip("It walks around inside a circle this big (in metres) round its starting spot.")]
    [Range(0.1f, 1000f)] public float wanderRadius = 6f;
    [ShowAs("Slowest speed"), Tooltip("Each walk picks a speed between Slowest and Fastest.")]
    [Range(0.1f, 500f)] public float minMoveSpeed = 2f;
    [ShowAs("Fastest speed"), Tooltip("Each walk picks a speed between Slowest and Fastest.")]
    [Range(0.1f, 500f)] public float maxMoveSpeed = 5f;

    [Header("Steering & Fluidity")]
    [Tooltip("Multiplier for turn speed. Higher values allow sharper turns without stopping.")]
    [Range(90f, 1080f)] private float turnSpeedMultiplier = 1000f;

    [Tooltip("Distance threshold to trigger target picking before reaching the exact point (prevents stopping).")]
    [Range(0.3f, 3f)] private float arrivalThreshold = 1.0f;

    private PlatformObstacle platformHost;
    private Vector3 currentWanderOffset;
    private float currentMoveSpeed;

    // Home spot: relative to the platform while riding one, or a fixed world point after landing on the ground
    private Vector3 localAnchorPosition;
    private Transform platformTransform;
    private bool anchorCached;

    private void Awake()
    {
        platformHost = GetComponent<PlatformObstacle>();
    }

    private void Start()
    {
        CachePlatformAnchor();
        PickNewWanderTarget();
    }

    /// <summary>Called after falling off and landing somewhere new.</summary>
    public void Reanchor()
    {
        anchorCached = false;
        CachePlatformAnchor();
        PickNewWanderTarget();
    }

    private void CachePlatformAnchor()
    {
        platformTransform = platformHost != null ? platformHost.GetPlatformTransform() : transform.parent;

        localAnchorPosition = platformTransform != null
            ? platformTransform.InverseTransformPoint(transform.position)
            : transform.position; // on the ground: a fixed world spot

        anchorCached = true;
    }

    private Vector3 AnchorWorld => platformTransform != null ? platformTransform.TransformPoint(localAnchorPosition) : localAnchorPosition;
    private Quaternion SurfaceRotation => platformTransform != null ? platformTransform.rotation : Quaternion.identity;
    private Vector3 SurfaceUp => platformTransform != null ? platformTransform.up : Vector3.up;

    public void ProcessMovement(ObstacleBase host)
    {
        if (!anchorCached) CachePlatformAnchor();

        Vector3 anchorWorld = AnchorWorld;
        Vector3 platformUp = SurfaceUp;
        Vector3 targetWorldPos = anchorWorld + (SurfaceRotation * currentWanderOffset);

        // Movement along the surface
        Vector3 worldDelta = Vector3.ProjectOnPlane(targetWorldPos - transform.position, platformUp);

        // Swap to the next target early so momentum is preserved
        if (worldDelta.magnitude <= arrivalThreshold)
        {
            PickNewWanderTarget();
            targetWorldPos = anchorWorld + (SurfaceRotation * currentWanderOffset);
            worldDelta = Vector3.ProjectOnPlane(targetWorldPos - transform.position, platformUp);
        }

        Vector3 moveDir = worldDelta.normalized;
        if (moveDir.sqrMagnitude <= 0.001f) return;

        // Smooth turning
        Quaternion targetRot = Quaternion.LookRotation(moveDir, platformUp);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, turnSpeedMultiplier * Time.deltaTime);

        // Smooth speed curve (no hard stops)
        float angleToTarget = Vector3.Angle(transform.forward, moveDir);
        float smoothSpeedFactor = Mathf.Lerp(0.4f, 1.0f, Mathf.Cos(angleToTarget * Mathf.Deg2Rad));
        float step = currentMoveSpeed * smoothSpeedFactor * Time.deltaTime;

        // Don't walk off the edge: if there's no ground just ahead, head somewhere else
        if (platformHost != null)
        {
            Vector3 ahead = transform.position + transform.forward * (step + platformHost.EdgeLookAhead);
            if (!platformHost.HasGroundAt(ahead))
            {
                // Heading straight at the edge? Pick somewhere else. Already turning away? Keep turning on the spot.
                if (Vector3.Dot(transform.forward, moveDir) > 0.7f) PickNewWanderTarget();
                return;
            }
        }

        transform.Translate(Vector3.forward * step, Space.Self);
    }

    public void PickNewWanderTarget()
    {
        currentMoveSpeed = Random.Range(minMoveSpeed, maxMoveSpeed);

        // Try a few random spots and keep one that has ground under it (so we don't aim at thin air)
        Vector3 anchorWorld = AnchorWorld;
        for (int attempt = 0; attempt < 10; attempt++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * wanderRadius;
            Vector3 offset = new Vector3(randomCircle.x, 0f, randomCircle.y);
            Vector3 candidate = anchorWorld + SurfaceRotation * offset;

            if (platformHost == null || platformHost.HasGroundAt(candidate, 3f))
            {
                currentWanderOffset = offset;
                return;
            }
        }

        currentWanderOffset = Vector3.zero; // nowhere good found: head home
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 centerWorld = Application.isPlaying && anchorCached ? AnchorWorld : transform.position;

        // Wander Area Sphere
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(centerWorld, wanderRadius);

        // Active Target Sphere & Path Line
        if (Application.isPlaying && anchorCached)
        {
            Vector3 targetWorld = centerWorld + (SurfaceRotation * currentWanderOffset);

            Gizmos.color = Color.green;
            Gizmos.DrawSphere(targetWorld, 0.35f);

            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, targetWorld);
        }
    }
}
