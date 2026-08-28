using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyTerrainWanderer : MonoBehaviour
{
    [Header("Home Tether Setup")]
    [Tooltip("Maximum distance from spawn position the agent is allowed to wander.")]
    public float maxDistanceFromHome = 30f;

    [Header("Wander Range")]
    [Tooltip("Minimum distance for a single trek step.")]
    public float minWanderRadius = 5f;
    [Tooltip("Maximum distance for a single trek step.")]
    public float maxWanderRadius = 15f;

    [Header("Movement Speed Range")]
    public float minSpeed = 2f;
    public float maxSpeed = 6f;

    [Header("Dynamic Rotation Setup")]
    public float turnSpeedMultiplier = 60f;
    public float minAngularSpeed = 120f;

    private NavMeshAgent agent;
    private Vector3 homePosition;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        // Lock in spawn point as the tether center
        homePosition = transform.position; 
        
        SetNewRandomDestination();
    }

    private void Update()
    {
        if (!agent.isOnNavMesh) return;

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            SetNewRandomDestination();
        }
    }

    private void SetNewRandomDestination()
    {
        if (!agent.isOnNavMesh) return;

        float chosenSpeed = Random.Range(minSpeed, maxSpeed);
        agent.speed = chosenSpeed;
        agent.acceleration = chosenSpeed * 4f;

        float calculatedTurnSpeed = chosenSpeed * turnSpeedMultiplier;
        agent.angularSpeed = Mathf.Max(minAngularSpeed, calculatedTurnSpeed);

        for (int i = 0; i < 10; i++)
        {
            float chosenRadius = Random.Range(minWanderRadius, maxWanderRadius);
            Vector3 randomDirection = Random.insideUnitSphere.normalized * chosenRadius;
            
            // Pick next point relative to CURRENT position
            Vector3 candidatePoint = transform.position + randomDirection;

            // If candidate point drifts too far from HOME, pull target back toward Home
            if (Vector3.Distance(candidatePoint, homePosition) > maxDistanceFromHome)
            {
                candidatePoint = homePosition + (candidatePoint - homePosition).normalized * Random.Range(0f, maxDistanceFromHome * 0.5f);
            }

            if (NavMesh.SamplePosition(candidatePoint, out NavMeshHit hit, 10f, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
                return;
            }
        }
    }

    // Visualizes the tether radius in Scene view for easy debugging
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 center = Application.isPlaying ? homePosition : transform.position;
        Gizmos.DrawWireSphere(center, maxDistanceFromHome);
    }
}