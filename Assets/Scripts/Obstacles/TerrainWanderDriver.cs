using UnityEngine;

[RequireComponent(typeof(TerrainObstacle))]
public class TerrainWanderDriver : MonoBehaviour, IObstacleMovement
{
    [Header("Wandering Settings")]
    [Range(0.1f, 1000f)] public float wanderRadius = 6f;

    [Header("Speed Settings")]
    [Range(0.1f, 1000f)] public float minMoveSpeed = 2f;
    [Range(0.1f, 1000f)] public float maxMoveSpeed = 5f;

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

        // 2. Hardcoded Terrain Height Surface Lookup (Raycasts from 10m above down to 20m below)
        Terrain terrain = Terrain.activeTerrain;
        float targetY = transform.position.y;

        if (terrain != null)
        {
            targetY = terrain.SampleHeight(new Vector3(nextXZ.x, 0f, nextXZ.z)) + terrain.transform.position.y;
        }

// 3. Apply position
        transform.position = new Vector3(nextXZ.x, targetY, nextXZ.z);

        // 3. Apply position
        transform.position = new Vector3(nextXZ.x, targetY, nextXZ.z);

        // 4. Target Arrival Check
        if (Vector3.Distance(nextXZ, targetXZ) <= 0.1f)
        {
            PickNewWanderTarget();
        }
    }

    public void PickNewWanderTarget()
    {
        currentMoveSpeed = Random.Range(minMoveSpeed, maxMoveSpeed);

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

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(centerPoint, wanderRadius);

        if (Application.isPlaying)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(currentTargetPoint, 0.4f);

            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, currentTargetPoint);
        }
    }
}