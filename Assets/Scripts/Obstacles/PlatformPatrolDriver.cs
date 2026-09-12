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
    private int patrolDirection = 1;
    private float currentMoveSpeed;
    private float turnSpeedMultiplier = 120f;

    private Transform platformTransform;

    private void Awake()
    {
        platformHost = GetComponent<PlatformObstacle>();
    }

    private void Start()
    {
        CachePlatformAnchor();
        RandomizeSpeed();
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
        }
        else
        {
            initialPlatformLocalPosition = transform.localPosition;
        }
    }

    public void ProcessMovement(ObstacleBase host)
    {
        if (platformTransform == null) CachePlatformAnchor();

        // 1. Calculate world anchor center on the platform
        Vector3 anchorWorld = platformTransform != null 
            ? platformTransform.TransformPoint(initialPlatformLocalPosition) 
            : transform.position;

        Quaternion platformRotation = platformTransform != null ? platformTransform.rotation : Quaternion.identity;

        // 2. Determine target waypoint on the patrol axis (+moveDistance or -moveDistance)
        Vector3 targetLocalOffset = new Vector3(0f, 0f, patrolDirection * moveDistance);
        Vector3 targetWorldPos = anchorWorld + (platformRotation * targetLocalOffset);

        // 3. Movement delta along the platform surface plane
        Vector3 worldDelta = targetWorldPos - transform.position;

        Vector3 platformUp = platformTransform != null ? platformTransform.up : Vector3.up;
        worldDelta = Vector3.ProjectOnPlane(worldDelta, platformUp);

        // Flip direction when reaching the endpoint sphere
        if (worldDelta.magnitude < 0.2f)
        {
            patrolDirection *= -1;
            RandomizeSpeed();
            return;
        }

        // 4. Steer and translate (Exact match to Wanderer motor)
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
        Transform activePlatform = platformTransform;
        if (activePlatform == null)
        {
            PlatformObstacle host = GetComponent<PlatformObstacle>();
            activePlatform = (host != null && host.GetPlatformTransform() != null) ? host.GetPlatformTransform() : transform.parent;
        }

        Vector3 centerWorld;

        if (Application.isPlaying && activePlatform != null)
        {
            centerWorld = activePlatform.TransformPoint(initialPlatformLocalPosition);
        }
        else
        {
            centerWorld = transform.position;
        }

        Quaternion platformRotation = activePlatform != null ? activePlatform.rotation : Quaternion.identity;
        Vector3 forwardDir = platformRotation * Vector3.forward;

        Vector3 startPos = centerWorld - (forwardDir * moveDistance);
        Vector3 endPos = centerWorld + (forwardDir * moveDistance);

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(startPos, endPos);
        Gizmos.DrawWireSphere(startPos, 1f);
        Gizmos.DrawWireSphere(endPos, 1f);
    }
}