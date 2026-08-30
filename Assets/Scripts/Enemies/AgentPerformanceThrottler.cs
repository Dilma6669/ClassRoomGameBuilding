using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class AgentPerformanceThrottler : MonoBehaviour
{
    [Header("Distance Thresholds (from Main Camera)")]
    public float closeDistance = 30f;   
    public float mediumDistance = 70f;  

    [Header("Avoidance Quality Scaling")]
    public ObstacleAvoidanceType closeQuality = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
    public ObstacleAvoidanceType mediumQuality = ObstacleAvoidanceType.LowQualityObstacleAvoidance;

    private NavMeshAgent agent;
    private Camera mainCam;
    private float checkTimer;
    private const float checkInterval = 1f;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        mainCam = Camera.main;
        
        checkTimer = Random.Range(0f, checkInterval);
    }

    private void Update()
    {
        // Lazy-load camera if it wasn't ready at Start
        if (mainCam == null) mainCam = Camera.main;

        // Skip completely if camera missing, agent disabled (e.g. on Platform), or off NavMesh
        if (mainCam == null || agent == null || !agent.enabled || !agent.isOnNavMesh) return;

        checkTimer += Time.deltaTime;
        if (checkTimer >= checkInterval)
        {
            checkTimer = 0f;
            EvaluatePerformanceTier();
        }
    }

    private void EvaluatePerformanceTier()
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;

        float distSqr = (transform.position - mainCam.transform.position).sqrMagnitude;

        if (distSqr <= closeDistance * closeDistance)
        {
            agent.obstacleAvoidanceType = closeQuality;
            agent.isStopped = false;
        }
        else if (distSqr <= mediumDistance * mediumDistance)
        {
            agent.obstacleAvoidanceType = mediumQuality;
            agent.isStopped = false;
        }
        else
        {
            // Disables avoidance entirely and pauses distant agents
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
            agent.isStopped = true; 
        }
    }
}