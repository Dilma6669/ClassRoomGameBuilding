using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class TerrainObstacle : ObstacleBase
{
    [Header("NavMesh Settings")]
    [Range(0.01f, 5f)] public float navMeshRadius = 0.5f;

    public NavMeshAgent Agent { get; private set; }
    public Vector3 SpawnCenterPosition { get; private set; }
    public bool IsFalling { get; set; }

    protected override void OnValidate()
    {
        base.OnValidate();
        if (Agent == null) Agent = GetComponent<NavMeshAgent>();
        if (Agent != null) Agent.radius = navMeshRadius;
    }

    protected override void Awake()
    {
        base.Awake();
        Agent = GetComponent<NavMeshAgent>();
        
        if (Agent != null)
        {
            Agent.radius = navMeshRadius;
            Agent.stoppingDistance = 0.2f;
            Agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
        }
    }

    private void Start()
    {
        SpawnCenterPosition = transform.position;

        if (Agent != null && NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 15f, NavMesh.AllAreas))
        {
            transform.position = hit.position + Vector3.up * 0.1f;
            Agent.Warp(hit.position);
            SpawnCenterPosition = hit.position;
        }
    }

    public void SetKinematicState(bool enableKinematic)
    {
        if (Rb != null) Rb.isKinematic = enableKinematic;
        if (Agent != null) Agent.enabled = enableKinematic;
    }
}