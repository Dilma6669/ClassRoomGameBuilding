using UnityEngine;

[RequireComponent(typeof(TerrainObstacle))]
public class TerrainStaticDriver : MonoBehaviour, IObstacleMovement
{
    private TerrainObstacle terrainHost;

    private void Awake()
    {
        terrainHost = GetComponent<TerrainObstacle>();

        // Disable NavMeshAgent if present since static obstacles don't pathfind
        if (terrainHost != null && terrainHost.Agent != null)
        {
            terrainHost.Agent.enabled = false;
        }
    }

    public void ProcessMovement(ObstacleBase host)
    {
        // No movement logic required - object remains stationary
    }
}