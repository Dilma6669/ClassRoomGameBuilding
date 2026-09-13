using UnityEngine;
using UnityEngine.AI;

public class TerrainObstacle : ObstacleBase
{
    [Header("NavMesh Settings")]
    [Range(0.01f, 5f)] public float navMeshRadius = 0.5f;

    [Header("Height Alignment")]
    [Tooltip("Adjusts vertical offset of the visual model and colliders above the terrain ground surface.")]
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
        }

        ApplyVisualHeightOffset();
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

        ApplyVisualHeightOffset();
    }

    private void Start()
    {
        SpawnCenterPosition = transform.position;

        if (Agent != null && NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 15f, NavMesh.AllAreas))
        {
            Agent.Warp(hit.position);
            SpawnCenterPosition = hit.position;
        }

        ApplyVisualHeightOffset();
    }

    private void ApplyVisualHeightOffset()
    {
        // Target the child visual container
        Transform visualChild = transform.Find("⚠️ DO NOT TOUCH");
        if (visualChild != null)
        {
            // Move the visual mesh and BOTH attached colliders up relative to the NavMesh root
            visualChild.localPosition = new Vector3(0f, heightOffset, 0f);
        }
    }

    public void SetKinematicState(bool enableKinematic)
    {
        // Safely check for an optional Rigidbody locally if this specific terrain obstacle uses gravity/falling
        Rigidbody localRb = GetComponent<Rigidbody>();
        if (localRb != null) 
        {
            localRb.isKinematic = enableKinematic;
        }

        if (Agent != null) 
        {
            Agent.enabled = enableKinematic;
        }
    }
}