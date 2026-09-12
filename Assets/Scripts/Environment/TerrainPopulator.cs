using UnityEngine;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine.AI;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

[ExecuteAlways]
public class TerrainPopulator : MonoBehaviour
{
    [HideInInspector]
    public Terrain targetTerrain;

    [Header("Single Obstacle Setup")]
    [Tooltip("Assign your base Obstacle prefab here for spawning.")]
    public GameObject obstaclePrefab;

    [Tooltip("Assign a custom mesh to override the obstacle visual and physics collision.")]
    public Mesh customMesh;

    [Tooltip("Assign an optional custom material (leave empty to keep default material).")]
    public Material customMaterial;

    [Header("Random Scatter Setup")]
    [Tooltip("Select which type of obstacle to create when scattering across the terrain.")]
    public ObstacleSpawnType scatterObstacleType = ObstacleSpawnType.Static;

    [Range(1, 1000)] public int scatterCount = 20;
    [Range(0f, 20f)] private float edgePadding = 5f;
    [Range(0f, 5f)] private float heightOffset = 0.2f;

    private bool randomYRotation = true;
    private bool alignWithTerrainSlope = false;

    private TerrainCollider terrainCollider;
    private NavMeshSurface navMeshSurface;

    private void Start()
    {
        EnsureComponentsHidden();
    }

    private void OnValidate()
    {
        EnsureComponentsHidden();
    }

    private void EnsureComponentsHidden()
    {
        terrainCollider = GetComponent<TerrainCollider>();
        navMeshSurface = GetComponent<NavMeshSurface>();
    }

    [ContextMenu("Bake NavMesh Surface")]
    public void BakeNavMeshSurface()
    {
        FetchTerrain();

        if (navMeshSurface == null)
        {
            navMeshSurface = GetComponent<NavMeshSurface>();
            if (navMeshSurface == null)
            {
                navMeshSurface = gameObject.AddComponent<NavMeshSurface>();
            }
        }
        EnsureComponentsHidden();

        navMeshSurface.collectObjects = CollectObjects.Children;
        navMeshSurface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;

        if (targetTerrain != null)
        {
            TerrainCollider tCollider = targetTerrain.GetComponent<TerrainCollider>();
            if (tCollider == null)
            {
                tCollider = targetTerrain.gameObject.AddComponent<TerrainCollider>();
            }
            if (targetTerrain.terrainData != null)
            {
                tCollider.terrainData = targetTerrain.terrainData;
            }
            tCollider.enabled = true;
        }

        navMeshSurface.BuildNavMesh();

#if UNITY_EDITOR
        EditorUtility.SetDirty(gameObject);
        EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif

        Debug.Log("✅ NavMesh Surface baked successfully!");
    }

    private void FetchTerrain()
    {
        if (targetTerrain == null)
        {
            targetTerrain = GetComponent<Terrain>();
        }
        if (targetTerrain == null)
        {
            targetTerrain = Terrain.activeTerrain;
        }
    }

    private void EnsureBaseTerrainComponents(GameObject spawnedObject)
    {
        NavMeshAgent agent = spawnedObject.GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.enabled = true;
        }

        FollowPlatform follower = spawnedObject.GetComponent<FollowPlatform>();
        if (follower != null)
        {
#if UNITY_EDITOR
            Undo.DestroyObjectImmediate(follower);
#else
            Destroy(follower);
#endif
        }

        PlatformObstacle platformObstacle = spawnedObject.GetComponent<PlatformObstacle>();
        if (platformObstacle != null)
        {
#if UNITY_EDITOR
            Undo.DestroyObjectImmediate(platformObstacle);
#else
            Destroy(platformObstacle);
#endif
        }

        ApplyCustomMeshAndCollider(spawnedObject);
    }

    private void ApplyCustomMeshAndCollider(GameObject obstacleObj)
    {
        if (obstacleObj == null || customMesh == null) return;

        MeshFilter filter = obstacleObj.GetComponentInChildren<MeshFilter>();
        if (filter != null)
        {
            filter.sharedMesh = customMesh;
        }

        if (customMaterial != null)
        {
            MeshRenderer renderer = obstacleObj.GetComponentInChildren<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = customMaterial;
            }
        }

        MeshCollider[] childMeshColliders = obstacleObj.GetComponentsInChildren<MeshCollider>(true);
        foreach (MeshCollider col in childMeshColliders)
        {
            if (col.gameObject == obstacleObj) continue;

            col.sharedMesh = customMesh;
            col.convex = true;
        }
    }

    private void AttachDriverBySpawnType(GameObject obstacleObj, ObstacleSpawnType spawnType)
    {
        switch (spawnType)
        {
            case ObstacleSpawnType.Static:
                if (obstacleObj.GetComponent<TerrainStaticDriver>() == null)
                    obstacleObj.AddComponent<TerrainStaticDriver>();
                break;

            case ObstacleSpawnType.Wander:
                if (obstacleObj.GetComponent<TerrainWanderDriver>() == null)
                    obstacleObj.AddComponent<TerrainWanderDriver>();
                break;

            case ObstacleSpawnType.Patrol:
                if (obstacleObj.GetComponent<TerrainPatrolDriver>() == null)
                    obstacleObj.AddComponent<TerrainPatrolDriver>();
                break;
        }
    }

    #region Terrain Obstacle Spawning

    [ContextMenu("Scatter Obstacles")]
    public void ScatterObstacles()
    {
        if (obstaclePrefab == null)
        {
            Debug.LogWarning("⚠️ Please assign an Obstacle Prefab before scattering.");
            return;
        }

        FetchTerrain();
        if (targetTerrain == null)
        {
            Debug.LogWarning("⚠️ No Terrain assigned or found in scene!");
            return;
        }

        Vector3 terrainPos = targetTerrain.transform.position;
        Vector3 terrainSize = targetTerrain.terrainData.size;

        for (int i = 0; i < scatterCount; i++)
        {
            float randomX = Random.Range(terrainPos.x + edgePadding, terrainPos.x + terrainSize.x - edgePadding);
            float randomZ = Random.Range(terrainPos.z + edgePadding, terrainPos.z + terrainSize.z - edgePadding);

            float surfaceY = targetTerrain.SampleHeight(new Vector3(randomX, 0f, randomZ)) + terrainPos.y;
            Vector3 spawnWorldPos = new Vector3(randomX, surfaceY + Mathf.Max(0.2f, heightOffset), randomZ);

            Quaternion spawnRotation = Quaternion.identity;

            if (alignWithTerrainSlope)
            {
                float normX = (randomX - terrainPos.x) / terrainSize.x;
                float normZ = (randomZ - terrainPos.z) / terrainSize.z;

                int sampleX = Mathf.Clamp((int)(normX * targetTerrain.terrainData.heightmapResolution), 0, targetTerrain.terrainData.heightmapResolution - 1);
                int sampleZ = Mathf.Clamp((int)(normZ * targetTerrain.terrainData.heightmapResolution), 0, targetTerrain.terrainData.heightmapResolution - 1);

                Vector3 terrainNormal = targetTerrain.terrainData.GetInterpolatedNormal(sampleX, sampleZ);
                spawnRotation = Quaternion.FromToRotation(Vector3.up, terrainNormal);
            }

            float randomAngle = 0f;
            if (randomYRotation)
            {
                randomAngle = Random.Range(0f, 360f);
                spawnRotation *= Quaternion.Euler(0f, randomAngle, 0f);
            }

            GameObject spawned = SpawnObject(obstaclePrefab, spawnWorldPos, spawnRotation, "Scatter Obstacles");

            if (spawned != null)
            {
                ObstacleLogic obstacle = spawned.GetComponent<ObstacleLogic>();
                if (obstacle != null && randomYRotation)
                {
                    obstacle.rotationAngle = randomAngle;
                }

                EnsureBaseTerrainComponents(spawned);
                AttachDriverBySpawnType(spawned, scatterObstacleType);
            }
        }
    }

    #endregion

    [ContextMenu("Clear Terrain Spawns")]
    public void ClearTerrainSpawns()
    {
        List<GameObject> objectsToDelete = new List<GameObject>();

        foreach (Transform child in transform)
        {
            objectsToDelete.Add(child.gameObject);
        }

        for (int i = objectsToDelete.Count - 1; i >= 0; i--)
        {
#if UNITY_EDITOR
            Undo.DestroyObjectImmediate(objectsToDelete[i]);
#else
            Destroy(objectsToDelete[i]);
#endif
        }
    }

    private GameObject SpawnObject(GameObject prefab, Vector3 worldPos, Quaternion rotation, string undoName)
    {
#if UNITY_EDITOR
        GameObject newObj = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        newObj.transform.position = worldPos;
        newObj.transform.rotation = rotation;
        newObj.transform.SetParent(transform);
        Undo.RegisterCreatedObjectUndo(newObj, undoName);
        return newObj;
#else
        return Instantiate(prefab, worldPos, rotation, transform);
#endif
    }
}