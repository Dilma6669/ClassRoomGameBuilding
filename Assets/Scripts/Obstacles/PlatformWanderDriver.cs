using UnityEngine;

[RequireComponent(typeof(PlatformObstacle))]
public class PlatformWanderDriver : MonoBehaviour, IObstacleMovement
{
    [Header("Wander Settings")]
    [Range(0.1f, 1000f)] public float minWanderRadius = 2f;
    [Range(0.1f, 1000f)] public float maxWanderRadius = 6f;
    [Range(0.1f, 1000f)] public float minMoveSpeed = 2f;
    [Range(0.1f, 1000f)] public float maxMoveSpeed = 5f;

    private PlatformObstacle platformHost;
    private Vector3 currentWanderOffset;
    private float currentMoveSpeed;
    private float turnSpeedMultiplier = 120f;
    
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

        // 2. Rotate offset by platform orientation ONLY (ignores parent scale)
        Quaternion platformRotation = platformTransform != null ? platformTransform.rotation : Quaternion.identity;
        Vector3 targetWorldPos = anchorWorld + (platformRotation * currentWanderOffset);

        // 3. Movement delta along the platform surface
        Vector3 worldDelta = targetWorldPos - transform.position;

        Vector3 platformUp = platformTransform != null ? platformTransform.up : Vector3.up;
        worldDelta = Vector3.ProjectOnPlane(worldDelta, platformUp);

        if (worldDelta.magnitude < 0.2f)
        {
            PickNewWanderTarget();
            return;
        }

        // 4. Rotate and translate towards target
        Vector3 moveDir = worldDelta.normalized;
        if (moveDir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir, platformUp);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, currentMoveSpeed * turnSpeedMultiplier * Time.deltaTime);
        }

        transform.Translate(Vector3.forward * (currentMoveSpeed * Time.deltaTime), Space.Self);
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