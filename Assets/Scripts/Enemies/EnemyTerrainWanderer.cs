using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyTerrainWanderer : MonoBehaviour
{
    [Header("Wander Range")]
    public float minWanderRadius = 15f;
    public float maxWanderRadius = 40f;

    [Header("Movement Speed Range")]
    public float minSpeed = 2f;
    public float maxSpeed = 6f;

    [Header("Dynamic Rotation Setup")]
    public float turnSpeedMultiplier = 60f;
    public float minAngularSpeed = 120f;

    private NavMeshAgent agent;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        SetNewRandomDestination();
    }

    private void Update()
    {
        if (!agent.isOnNavMesh) return;

        // Immediately pick a new destination the second they reach the target
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
            randomDirection += transform.position;

            if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, 10f, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
                return;
            }
        }
    }
}