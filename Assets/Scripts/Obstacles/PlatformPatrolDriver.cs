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
    private Vector3 lastPatrolOffset;
    private int patrolDirection = 1;
    private float currentMoveSpeed;

    private void Start()
    {
        platformHost = GetComponent<PlatformObstacle>();

        // Exact anchor position capture from original ObstacleLogic
        Transform platformTransform = platformHost.GetPlatformTransform();
        if (platformTransform != null)
        {
            initialPlatformLocalPosition = platformTransform.InverseTransformPoint(transform.position);
        }
        else
        {
            initialPlatformLocalPosition = transform.localPosition;
        }

        lastPatrolOffset = Vector3.zero;
        RandomizeSpeed();
    }

    public void ProcessMovement(ObstacleBase host)
    {
        // Exact z-displacement patrol math from ObstacleLogic
        float currentZ = lastPatrolOffset.z + (patrolDirection * currentMoveSpeed * Time.deltaTime);

        if (currentZ >= moveDistance)
        {
            currentZ = moveDistance;
            patrolDirection = -1;
            RandomizeSpeed();
        }
        else if (currentZ <= -moveDistance)
        {
            currentZ = -moveDistance;
            patrolDirection = 1;
            RandomizeSpeed();
        }

        lastPatrolOffset = new Vector3(0f, 0f, currentZ);

        // Exact platform transformation sync from ObstacleLogic
        Transform platformTransform = platformHost.GetPlatformTransform();
        if (platformTransform != null)
        {
            transform.position = platformTransform.TransformPoint(initialPlatformLocalPosition + lastPatrolOffset);
        }
        else
        {
            transform.localPosition = initialPlatformLocalPosition + lastPatrolOffset;
        }
    }

    private void RandomizeSpeed()
    {
        currentMoveSpeed = Random.Range(minMoveSpeed, maxMoveSpeed);
    }
    
    private void OnDrawGizmosSelected()
    {
        if (platformHost == null) platformHost = GetComponent<PlatformObstacle>();

        Transform platformTransform = platformHost != null ? platformHost.GetPlatformTransform() : transform.parent;
        Vector3 centerPoint = transform.position;

        Vector3 startPos = centerPoint + transform.TransformDirection(new Vector3(0f, 0f, -moveDistance));
        Vector3 endPos = centerPoint + transform.TransformDirection(new Vector3(0f, 0f, moveDistance));

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(startPos, endPos);
        Gizmos.DrawWireSphere(startPos, 1f);
        Gizmos.DrawWireSphere(endPos, 1f);
    }
}