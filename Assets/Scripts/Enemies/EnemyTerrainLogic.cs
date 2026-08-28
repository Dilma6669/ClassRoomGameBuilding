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
    
    // "Maximum distance from spawn position the agent is allowed to wander."
    private float maxDistanceFromHome = 30f;

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

        for (int i = 0; i < 10; i++)
        {
            float chosenRadius = Random.Range(minWanderRadius, maxWanderRadius);
            Vector3 randomDirection = Random.insideUnitSphere.normalized * chosenRadius;
            
            Vector3 candidatePoint = transform.position + randomDirection;

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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 center = Application.isPlaying ? homePosition : transform.position;
        Gizmos.DrawWireSphere(center, maxDistanceFromHome);
    }
}