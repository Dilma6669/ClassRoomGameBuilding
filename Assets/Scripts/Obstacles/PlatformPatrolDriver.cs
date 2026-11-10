using UnityEngine;

[RequireComponent(typeof(PlatformObstacle))]
public class PlatformPatrolDriver : MonoBehaviour, IObstacleMovement, IObstacleAnchor
{
    [Header("Patrol Settings")]
    [ShowAs("Patrol distance"), Tooltip("How far it walks back and forth (in metres).")]
    [Range(0.1f, 100f)] public float moveDistance = 3f;

    [Header("Speed Settings")]
    [ShowAs("Slowest speed"), Tooltip("Each walk picks a speed between Slowest and Fastest.")]
    [Range(0.1f, 20f)] public float minMoveSpeed = 2f;
    [ShowAs("Fastest speed"), Tooltip("Each walk picks a speed between Slowest and Fastest.")]
    [Range(0.1f, 20f)] public float maxMoveSpeed = 5f;

    private PlatformObstacle platformHost;
    private Vector3 initialPlatformLocalPosition;
    private Quaternion initialLocalRotation;
    private int patrolDirection = 1;
    private float currentMoveSpeed;
    private float turnSpeedMultiplier = 120f;

    private Transform platformTransform;
    private bool isInitialized = false;
    private bool anchorCached;

    private void Awake()
    {
        platformHost = GetComponent<PlatformObstacle>();
    }

    private void Start()
    {
        CachePlatformAnchor();
        RandomizeSpeed();
        isInitialized = true;
    }

    /// <summary>Called after falling off and landing somewhere new: patrol around the landing spot.</summary>
    public void Reanchor()
    {
        anchorCached = false;
        CachePlatformAnchor();
    }

    private void CachePlatformAnchor()
    {
        platformTransform = platformHost != null ? platformHost.GetPlatformTransform() : transform.parent;

        if (platformTransform != null)
        {
            initialPlatformLocalPosition = platformTransform.InverseTransformPoint(transform.position);
            initialLocalRotation = Quaternion.Inverse(platformTransform.rotation) * transform.rotation;
        }
        else
        {
            // On the ground: a fixed world spot and facing
            initialPlatformLocalPosition = transform.position;
            initialLocalRotation = transform.rotation;
        }
        anchorCached = true;
    }

    public void ProcessMovement(ObstacleBase host)
    {
        if (!anchorCached) CachePlatformAnchor();

        // 1. Calculate anchor center on the platform (or the fixed world spot after landing on the ground)
        Vector3 anchorWorld = platformTransform != null
            ? platformTransform.TransformPoint(initialPlatformLocalPosition)
            : initialPlatformLocalPosition;

        // 2. Combine Platform Rotation with Obstacle's Initial Facing Direction
        Quaternion platformRotation = platformTransform != null ? platformTransform.rotation : Quaternion.identity;
        Quaternion worldFacing = platformRotation * initialLocalRotation;
        Vector3 patrolForward = worldFacing * Vector3.forward;

        // 3. Determine target waypoint along the obstacle's facing axis
        Vector3 targetWorldPos = anchorWorld + (patrolForward * (patrolDirection * moveDistance * 0.5f));

        // 4. Movement delta along surface plane
        Vector3 worldDelta = targetWorldPos - transform.position;
        Vector3 platformUp = platformTransform != null ? platformTransform.up : Vector3.up;
        worldDelta = Vector3.ProjectOnPlane(worldDelta, platformUp);

        // Flip direction when reaching endpoint
        if (worldDelta.magnitude < 0.2f)
        {
            patrolDirection *= -1;
            RandomizeSpeed();
            return;
        }

        // 5. Steer and translate
        Vector3 moveDir = worldDelta.normalized;
        if (moveDir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir, platformUp);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, currentMoveSpeed * turnSpeedMultiplier * Time.deltaTime);
        }

        // 6. Don't walk off the edge: turn around early instead
        float step = currentMoveSpeed * Time.deltaTime;
        // (check where we're FACING - that's the way Translate moves us, even mid-turn)
        if (platformHost != null && !platformHost.HasGroundAt(transform.position + transform.forward * (step + platformHost.EdgeLookAhead)))
        {
            // Heading straight at the edge? Turn around. Already turning away? Keep turning on the spot.
            if (Vector3.Dot(transform.forward, moveDir) > 0.7f) patrolDirection *= -1;
            return;
        }

        transform.Translate(Vector3.forward * step, Space.Self);
    }

    private void RandomizeSpeed()
    {
        currentMoveSpeed = Random.Range(minMoveSpeed, maxMoveSpeed);
    }

    private void OnDrawGizmosSelected()
    {
        // 1. Determine platform transform reference
        Transform pTransform = platformTransform;
        if (pTransform == null)
        {
            if (platformHost == null) platformHost = GetComponent<PlatformObstacle>();
            if (platformHost != null) pTransform = platformHost.GetPlatformTransform();
            if (pTransform == null) pTransform = transform.parent;
        }

        // 2. Determine anchor center and rotation relative to platform
        Vector3 center;
        Vector3 dir;

        if (Application.isPlaying && isInitialized)
        {
            center = pTransform != null 
                ? pTransform.TransformPoint(initialPlatformLocalPosition) 
                : transform.position;

            Quaternion platformRotation = pTransform != null ? pTransform.rotation : Quaternion.identity;
            dir = (platformRotation * initialLocalRotation) * Vector3.forward;
        }
        else
        {
            center = transform.position;
            dir = transform.forward;
        }

        // 3. Compute static end points anchored on the platform path
        Vector3 startGizmo = center - (dir * (moveDistance * 0.5f));
        Vector3 endGizmo = center + (dir * (moveDistance * 0.5f));

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(startGizmo, endGizmo);
        Gizmos.DrawWireSphere(startGizmo, 0.4f);
        Gizmos.DrawWireSphere(endGizmo, 0.4f);
    }
}