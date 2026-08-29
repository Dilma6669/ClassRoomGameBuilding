using UnityEngine;
using UnityEngine.AI;

public class EnemyTerrainLogic : MonoBehaviour
{
    [Header("Wander Range")]
    [Tooltip("The shortest distance the agent will walk when wandering.")]
    public float minWanderRadius = 5f;

    [Tooltip("The farthest distance the agent will walk when wandering.")]
    public float maxWanderRadius = 15f;

    [Header("Movement Speed Range")]
    [Tooltip("The slowest speed the agent will move (like a slow stroll).")]
    public float minSpeed = 2f;

    [Tooltip("The fastest speed the agent will move (like a full sprint).")]
    public float maxSpeed = 6f;

    // "Dynamic Rotation Setup" 
    private float turnSpeedMultiplier = 60f;
    private float minAngularSpeed = 120f;
    
    [Header("Leash Constraint")]
    [Tooltip("Maximum distance from spawn position the agent is allowed to wander.")]
    public float maxDistanceFromHome = 30f;

    private NavMeshAgent agent;
    private Vector3 homePosition;

    private void Start()
    {
        agent = GetComponentInChildren<NavMeshAgent>();
        homePosition = transform.position; 

        SetNewRandomDestination();
    }

    public void SetInitialRotation(Quaternion rotation)
    {
        transform.rotation = rotation;

        if (agent == null)
        {
            agent = GetComponentInChildren<NavMeshAgent>();
        }

        if (agent != null)
        {
            agent.transform.rotation = rotation;
        }
    }

    private void Update()
    {
        if (agent == null || !agent.isOnNavMesh) return;

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            SetNewRandomDestination();
        }
    }

    private void SetNewRandomDestination()
    {
        if (agent == null || !agent.isOnNavMesh) return;

        float chosenSpeed = Random.Range(minSpeed, maxSpeed);
        agent.speed = chosenSpeed;
        agent.acceleration = chosenSpeed * 4f;

        float calculatedTurnSpeed = chosenSpeed * turnSpeedMultiplier;
        agent.angularSpeed = Mathf.Max(minAngularSpeed, calculatedTurnSpeed);

        Vector3 currentHome = Application.isPlaying ? homePosition : transform.position;

        for (int i = 0; i < 10; i++)
        {
            // Pick a distance between min and max radius
            float chosenRadius = Random.Range(minWanderRadius, maxWanderRadius);
            
            // Generate a random direction on a flat horizontal plane (XZ)
            Vector2 randomCircle = Random.insideUnitCircle.normalized * chosenRadius;
            Vector3 randomDirection = new Vector3(randomCircle.x, 0f, randomCircle.y);

            // Sample around current position
            Vector3 candidatePoint = transform.position + randomDirection;

            // Clamp point within max home leash distance
            if (Vector3.Distance(candidatePoint, currentHome) > maxDistanceFromHome)
            {
                candidatePoint = currentHome + (candidatePoint - currentHome).normalized * Random.Range(minWanderRadius, Mathf.Min(maxWanderRadius, maxDistanceFromHome));
            }

            if (NavMesh.SamplePosition(candidatePoint, out NavMeshHit hit, maxWanderRadius, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
                return;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 center = Application.isPlaying ? homePosition : transform.position;

        // 1. Draw Max Leash Boundary (Red Wireframe)
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.75f);
        Gizmos.DrawWireSphere(center, maxDistanceFromHome);

        // 2. Draw Min Wander Radius (Yellow Wireframe)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, minWanderRadius);

        // 3. Draw Max Wander Radius (Cyan Wireframe)
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, maxWanderRadius);
    }
}