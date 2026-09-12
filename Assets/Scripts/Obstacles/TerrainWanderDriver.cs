using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(TerrainObstacle))]
public class TerrainWanderDriver : MonoBehaviour, IObstacleMovement
{
    [Header("Wandering Settings")]
    [Range(0.1f, 1000f)] public float minWanderRadius = 2f;
    [Range(0.1f, 1000f)] public float maxWanderRadius = 6f;

    [Header("Speed Range")]
    [Range(0.1f, 1000f)] public float minMoveSpeed = 2f;
    [Range(0.1f, 1000f)] public float maxMoveSpeed = 5f;

    private TerrainObstacle terrainHost;
    private NavMeshAgent navMeshAgent;
    private float currentMoveSpeed;

    private void Start()
    {
        terrainHost = GetComponent<TerrainObstacle>();
        navMeshAgent = terrainHost.Agent;

        if (minWanderRadius > maxWanderRadius) minWanderRadius = maxWanderRadius;
        if (minMoveSpeed > maxMoveSpeed) minMoveSpeed = maxMoveSpeed;

        RandomizeSpeed();

        if (navMeshAgent != null && navMeshAgent.enabled && navMeshAgent.isOnNavMesh)
        {
            // Initial staggered start from ObstacleLogic
            Invoke(nameof(SetNewNavMeshDestination), Random.Range(0.05f, 0.5f));
        }
    }

    public void ProcessMovement(ObstacleBase host)
    {
        if (navMeshAgent == null || !navMeshAgent.enabled) return;

        // Exact falling recovery logic from original ObstacleLogic
        if (terrainHost.IsFalling)
        {
            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit groundHit, 1.2f, Physics.AllLayers, QueryTriggerInteraction.Ignore))
            {
                if (NavMesh.SamplePosition(groundHit.point, out NavMeshHit navHit, 1f, NavMesh.AllAreas))
                {
                    terrainHost.SetKinematicState(true);
                    navMeshAgent.Warp(navHit.position);
                    terrainHost.IsFalling = false;
                    SetNewNavMeshDestination();
                }
            }
            return;
        }

        if (!navMeshAgent.isOnNavMesh)
        {
            terrainHost.SetKinematicState(false);
            terrainHost.IsFalling = true;
            return;
        }

        // Apply speed & NavMesh parameters exactly as in ObstacleLogic
        navMeshAgent.speed = currentMoveSpeed;
        navMeshAgent.acceleration = currentMoveSpeed * 8f;
        navMeshAgent.angularSpeed = Mathf.Max(120f, currentMoveSpeed * 60f);

        // Path completion check from ObstacleLogic
        if (!navMeshAgent.pathPending)
        {
            if (!navMeshAgent.hasPath || navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance)
            {
                RandomizeSpeed();
                SetNewNavMeshDestination();
            }
        }
    }

    public void SetNewNavMeshDestination()
    {
        if (navMeshAgent == null || !navMeshAgent.enabled || !navMeshAgent.isOnNavMesh) return;

        Vector2 randomDirection = Random.insideUnitCircle.normalized;
        float randomDistance = Random.Range(minWanderRadius, maxWanderRadius);
        
        // Exact anchor math relative to spawnCenterPosition
        Vector3 candidatePoint = terrainHost.SpawnCenterPosition + new Vector3(randomDirection.x * randomDistance, 0f, randomDirection.y * randomDistance);

        if (NavMesh.SamplePosition(candidatePoint, out NavMeshHit hit, maxWanderRadius, NavMesh.AllAreas))
        {
            navMeshAgent.SetDestination(hit.position);
        }
    }

    private void RandomizeSpeed()
    {
        currentMoveSpeed = Random.Range(minMoveSpeed, maxMoveSpeed);
    }
    
    private void OnDrawGizmosSelected()
    {
        if (terrainHost == null) terrainHost = GetComponent<TerrainObstacle>();
        if (terrainHost == null) return;

        Vector3 centerPoint = Application.isPlaying ? terrainHost.SpawnCenterPosition : transform.position;

        // Outer boundary (Max Radius)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(centerPoint, maxWanderRadius);

        // Inner deadzone (Min Radius)
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(centerPoint, minWanderRadius);

        // Active path and destination waypoint visualization
        if (Application.isPlaying && navMeshAgent != null && navMeshAgent.enabled && navMeshAgent.hasPath)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(navMeshAgent.destination, 0.4f);

            Gizmos.color = Color.magenta;
            Vector3[] corners = navMeshAgent.path.corners;
            for (int i = 0; i < corners.Length - 1; i++)
            {
                Gizmos.DrawLine(corners[i], corners[i + 1]);
            }
        }
    }
}