using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class TerrainObstacle : ObstacleBase
{
    [Header("NavMesh Settings")]
    [Range(0.01f, 5f)] public float navMeshRadius = 0.5f;

    [Header("Height Alignment")]
    [Tooltip("Adjusts vertical offset of the agent above or below the NavMesh ground surface.")]
    [Range(-2f, 10f)] public float heightOffset = 0f;

    public NavMeshAgent Agent { get; private set; }
    public Vector3 SpawnCenterPosition { get; private set; }
    public bool IsFalling { get; set; }

    protected override void OnValidate()
    {
        base.OnValidate();
        if (Agent == null) Agent = GetComponent<NavMeshAgent>();
        if (Agent != null)
        {
            Agent.radius = navMeshRadius;
            Agent.baseOffset = heightOffset;
        }
    }

    protected override void Awake()
    {
        base.Awake();
        Agent = GetComponent<NavMeshAgent>();
        
        if (Agent != null)
        {
            Agent.radius = navMeshRadius;
            Agent.baseOffset = heightOffset;
            Agent.stoppingDistance = 0.2f;
            Agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
        }
    }

    private void Start()
    {
        SpawnCenterPosition = transform.position;

        if (Agent != null && NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 15f, NavMesh.AllAreas))
        {
            Agent.Warp(hit.position);
            Agent.baseOffset = heightOffset;
            SpawnCenterPosition = hit.position;
        }
    }

    public void SetKinematicState(bool enableKinematic)
    {
        if (Rb != null) Rb.isKinematic = enableKinematic;
        if (Agent != null) Agent.enabled = enableKinematic;
    }
}