using UnityEngine;

[RequireComponent(typeof(TerrainObstacle))]
public class TerrainStaticDriver : MonoBehaviour, IObstacleMovement
{
    private TerrainObstacle terrainHost;

    private void Awake()
    {
        terrainHost = GetComponent<TerrainObstacle>();
    }

    public void ProcessMovement(ObstacleBase host)
    {
        // No movement logic required - object remains stationary
    }
}