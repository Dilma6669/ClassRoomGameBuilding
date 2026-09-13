using UnityEngine;

[RequireComponent(typeof(TerrainObstacle))]
public class TerrainPatrolDriver : MonoBehaviour, IObstacleMovement
{
    [Header("Patrol Settings")]
    [Tooltip("Total distance covered along the patrol line.")]
    [Range(0.1f, 500f)] public float moveDistance = 5f;

    [Header("Speed Settings")]
    [Range(0.1f, 1000f)] public float minMoveSpeed = 2f;
    [Range(0.1f, 1000f)] public float maxMoveSpeed = 5f;

    private TerrainObstacle terrainHost;
    private Rigidbody rb;
    private float currentMoveSpeed;

    // Fixed World Endpoints
    private Vector3 pointA;
    private Vector3 pointB;
    private bool headingToB = true;
    private bool isInitialized = false;

    private void Awake()
    {
        terrainHost = GetComponent<TerrainObstacle>();
        rb = GetComponent<Rigidbody>();

        // Kinematic setup
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        InitializePatrolEndpoints();
    }

    private void Start()
    {
        if (minMoveSpeed > maxMoveSpeed) minMoveSpeed = maxMoveSpeed;
        RandomizeSpeed();
    }

    private void InitializePatrolEndpoints()
    {
        Vector3 origin = transform.position;
        Vector3 forwardDir = transform.forward;

        pointA = origin - (forwardDir * (moveDistance * 0.5f));
        pointB = origin + (forwardDir * (moveDistance * 0.5f));

        isInitialized = true;
    }

    public void ProcessMovement(ObstacleBase host)
    {
        Vector3 currentTargetPoint = headingToB ? pointB : pointA;

        // 1. Horizontal movement calculation (XZ plane)
        Vector3 currentXZ = new Vector3(transform.position.x, 0f, transform.position.z);
        Vector3 targetXZ = new Vector3(currentTargetPoint.x, 0f, currentTargetPoint.z);

        Vector3 moveDir = (targetXZ - currentXZ).normalized;

        if (moveDir != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
        }

        Vector3 nextXZ = Vector3.MoveTowards(currentXZ, targetXZ, currentMoveSpeed * Time.deltaTime);

        // 2. Direct Terrain Height Lookup (matches TerrainWanderDriver)
        Terrain terrain = Terrain.activeTerrain;
        float targetY = transform.position.y;

        if (terrain != null)
        {
            targetY = terrain.SampleHeight(new Vector3(nextXZ.x, 0f, nextXZ.z)) + terrain.transform.position.y;
        }

        // 3. Apply position
        transform.position = new Vector3(nextXZ.x, targetY, nextXZ.z);

        // 4. Switch direction upon reaching endpoint
        if (Vector3.Distance(nextXZ, targetXZ) <= 0.01f)
        {
            headingToB = !headingToB;
            RandomizeSpeed();
        }
    }

    private void RandomizeSpeed()
    {
        currentMoveSpeed = Random.Range(minMoveSpeed, maxMoveSpeed);
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 startGizmo;
        Vector3 endGizmo;

        if (Application.isPlaying && isInitialized)
        {
            startGizmo = pointA;
            endGizmo = pointB;
        }
        else
        {
            Vector3 center = transform.position;
            Vector3 dir = transform.forward; 
        
            startGizmo = center - (dir * (moveDistance * 0.5f));
            endGizmo = center + (dir * (moveDistance * 0.5f));
        }

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(startGizmo, endGizmo);
        Gizmos.DrawWireSphere(startGizmo, 0.5f);
        Gizmos.DrawWireSphere(endGizmo, 0.5f);
    }
}