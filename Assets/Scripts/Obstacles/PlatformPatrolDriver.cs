using UnityEngine;

[RequireComponent(typeof(PlatformObstacle))]
public class PlatformPatrolDriver : MonoBehaviour, IObstacleMovement
{
    [Header("Patrol Settings")]
    [Range(0.1f, 500f)] public float moveDistance = 3f;

    [Header("Speed Settings")]
    [Range(0.1f, 1000f)] public float minMoveSpeed = 2f;
    [Range(0.1f, 1000f)] public float maxMoveSpeed = 5f;

    private PlatformObstacle platformHost;
    private Vector3 initialPlatformLocalPosition;
    private Quaternion initialLocalRotation;
    private int patrolDirection = 1;
    private float currentMoveSpeed;
    private float turnSpeedMultiplier = 120f;

    private Transform platformTransform;
    private bool isInitialized = false;

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
            initialPlatformLocalPosition = platformTransform.InverseTransformPoint(transform.position);
            initialLocalRotation = Quaternion.Inverse(platformTransform.rotation) * transform.rotation;
        }
        else
        {
            initialPlatformLocalPosition = transform.localPosition;
            initialLocalRotation = transform.localRotation;
        }
    }

    public void ProcessMovement(ObstacleBase host)
    {
        if (platformTransform == null) CachePlatformAnchor();

        // 1. Calculate anchor center on the platform
        Vector3 anchorWorld = platformTransform != null 
            ? platformTransform.TransformPoint(initialPlatformLocalPosition) 
            : transform.position;

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

        transform.Translate(Vector3.forward * (currentMoveSpeed * Time.deltaTime), Space.Self);
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