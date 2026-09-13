using UnityEngine;

[RequireComponent(typeof(TerrainObstacle))]
public class TerrainWanderDriver : MonoBehaviour, IObstacleMovement
{
    [Header("Wandering Settings")]
    [Range(0.1f, 1000f)] public float wanderRadius = 6f;

    [Header("Speed Settings")]
    [Range(0.1f, 1000f)] public float minMoveSpeed = 2f;
    [Range(0.1f, 1000f)] public float maxMoveSpeed = 5f;

    [Header("Terrain Surface Snapping")]
    [Tooltip("Layer mask containing your Terrain layer.")]
    public LayerMask terrainLayer = ~0;
    [Tooltip("Height offset from the terrain surface (e.g., half the obstacle's height).")]
    public float yOffset = 0f;
    [Tooltip("How high above the obstacle to cast the downward ray.")]
    public float raycastHeightOffset = 5f;

    private TerrainObstacle terrainHost;
    private Rigidbody rb;
    private float currentMoveSpeed;

    // Movement & Anchor Tracking
    private Vector3 spawnAnchorPoint;
    private Vector3 currentTargetPoint;
    private bool isInitialized = false;

    private void Awake()
    {
        terrainHost = GetComponent<TerrainObstacle>();
        rb = GetComponent<Rigidbody>();

        // Completely disable NavMeshAgent if present on component
        if (terrainHost != null && terrainHost.Agent != null)
        {
            terrainHost.Agent.enabled = false;
        }

        // Setup direct transform control via Kinematic Rigidbody
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        spawnAnchorPoint = transform.position;
        isInitialized = true;
    }

    private void Start()
    {
        if (minMoveSpeed > maxMoveSpeed) minMoveSpeed = maxMoveSpeed;
        
        // Pick initial target immediately
        PickNewWanderTarget();
    }

    public void ProcessMovement(ObstacleBase host)
    {
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

        // 2. Terrain Height Surface Lookup (Self-Hit Protected Raycast)
        float targetY = transform.position.y;
        Vector3 rayOrigin = new Vector3(nextXZ.x, transform.position.y + raycastHeightOffset, nextXZ.z);

        RaycastHit[] hits = Physics.RaycastAll(rayOrigin, Vector3.down, raycastHeightOffset * 3f, terrainLayer, QueryTriggerInteraction.Ignore);

        foreach (RaycastHit hit in hits)
        {
            // Ignore hits on self or children
            if (hit.transform == transform || hit.transform.IsChildOf(transform)) 
                continue;

            targetY = hit.point.y + yOffset;
            break;
        }

        // 3. Apply position
        transform.position = new Vector3(nextXZ.x, targetY, nextXZ.z);

        // 4. Target Arrival Check (Switch destination smoothly)
        if (Vector3.Distance(nextXZ, targetXZ) <= 0.1f)
        {
            PickNewWanderTarget();
        }
    }

    public void PickNewWanderTarget()
    {
        currentMoveSpeed = Random.Range(minMoveSpeed, maxMoveSpeed);

        // Pick a random XZ position within wanderRadius around the initial spawn location
        Vector2 randomCircle = Random.insideUnitCircle * wanderRadius;
        
        Vector3 anchor = (terrainHost != null && terrainHost.SpawnCenterPosition != Vector3.zero) 
            ? terrainHost.SpawnCenterPosition 
            : spawnAnchorPoint;

        currentTargetPoint = anchor + new Vector3(randomCircle.x, 0f, randomCircle.y);
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 centerPoint = (Application.isPlaying && isInitialized) 
            ? spawnAnchorPoint 
            : transform.position;

        // Wander Area Sphere
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(centerPoint, wanderRadius);

        // Active target point visualization
        if (Application.isPlaying)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(currentTargetPoint, 0.4f);

            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, currentTargetPoint);
        }
    }
}