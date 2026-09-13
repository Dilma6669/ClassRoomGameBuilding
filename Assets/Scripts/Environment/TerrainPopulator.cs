using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AI;
using UnityEngine.Serialization;
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
    [Range(0f, 20f)] public float edgePadding = 5f;
    [Range(0f, 5f)] public float heightOffset = 0.2f;

    public bool randomYRotation = true;

    #region Configurable Spawn Settings

    [System.Serializable]
    public class BaseObstacleSettings
    {
        [Range(0.1f, 50f)] public float objectScale = 1f;
        [Range(0f, 360f)] public float initialYRotation = 0f;
        public ObstacleBase.PayloadType payloadType = ObstacleBase.PayloadType.Damage;
        [Range(1, 100f)] public int payloadAmount = 10;
        [Range(1f, 60f)] public float buffDuration = 5f;
        public bool destroyOnTrigger = false;

        [Header("Bounce Settings")]
        public bool isBouncy = false;
        [Range(0.5f, 5f)] public float triggerRadius = 0.5f;
        [Range(5f, 50f)] public float launchForce = 25f;
        [Range(0f, 1f)] public float upwardBias = 0.5f;
        [Range(0f, 1f)] public float momentumTransfer = 0.3f;
    }

    [System.Serializable]
    public class WandererSettings
    {
        [Range(0.1f, 1000f)] public float wanderRadius = 6f;
        [Range(0.1f, 1000f)] public float minMoveSpeed = 2f;
        [Range(0.1f, 1000f)] public float maxMoveSpeed = 5f;
    }

    [System.Serializable]
    public class PatrolSettings
    {
        [Range(0.1f, 500f)] public float moveDistance = 5f;
        [Range(0.1f, 1000f)] public float minMoveSpeed = 2f;
        [Range(0.1f, 1000f)] public float maxMoveSpeed = 5f;
    }

    public BaseObstacleSettings baseSettings = new BaseObstacleSettings();
    public WandererSettings wanderSettings = new WandererSettings();
    public PatrolSettings patrolSettings = new PatrolSettings();

    #endregion

    private TerrainCollider terrainCollider;

    private void Start()
    {
        EnsureComponentsHidden();
        AutoFindObstaclePrefab();
    }

    private void OnValidate()
    {
        EnsureComponentsHidden();
        AutoFindObstaclePrefab();
    }

    private void Reset()
    {
        AutoFindObstaclePrefab();
    }

    private void AutoFindObstaclePrefab()
    {
#if UNITY_EDITOR
        if (obstaclePrefab == null)
        {
            // Search Project window for the 'ObstacleLogic' prefab
            string[] guids = AssetDatabase.FindAssets("ObstacleLogic t:Prefab");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                obstaclePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }
        }
#endif
    }

    private void EnsureComponentsHidden()
    {
        terrainCollider = GetComponent<TerrainCollider>();
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

        TerrainObstacle terrainObstacle = spawnedObject.GetComponent<TerrainObstacle>();
        if (terrainObstacle == null)
        {
            terrainObstacle = spawnedObject.AddComponent<TerrainObstacle>();
        }

        ApplyBaseObstacleSettings(terrainObstacle);
        ApplyCustomMeshAndCollider(spawnedObject);
    }

    private void ApplyBaseObstacleSettings(ObstacleBase obstacle)
    {
        if (obstacle == null) return;

        obstacle.objectScale = baseSettings.objectScale;
        obstacle.initialYRotation = baseSettings.initialYRotation;
        obstacle.payloadType = baseSettings.payloadType;
        obstacle.payloadAmount = baseSettings.payloadAmount;
        obstacle.buffDuration = baseSettings.buffDuration;
        obstacle.destroyOnTrigger = baseSettings.destroyOnTrigger;

        obstacle.isBouncy = baseSettings.isBouncy;
        obstacle.triggerRadius = baseSettings.triggerRadius;
        obstacle.launchForce = baseSettings.launchForce;
        obstacle.upwardBias = baseSettings.upwardBias;
        obstacle.momentumTransfer = baseSettings.momentumTransfer;

        obstacle.ApplyScale();
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
                TerrainWanderDriver wanderDriver = obstacleObj.GetComponent<TerrainWanderDriver>();
                if (wanderDriver == null) wanderDriver = obstacleObj.AddComponent<TerrainWanderDriver>();
                
                wanderDriver.wanderRadius = wanderSettings.wanderRadius;
                wanderDriver.minMoveSpeed = wanderSettings.minMoveSpeed;
                wanderDriver.maxMoveSpeed = wanderSettings.maxMoveSpeed;
                break;

            case ObstacleSpawnType.Patrol:
                TerrainPatrolDriver patrolDriver = obstacleObj.GetComponent<TerrainPatrolDriver>();
                if (patrolDriver == null) patrolDriver = obstacleObj.AddComponent<TerrainPatrolDriver>();

                patrolDriver.moveDistance = patrolSettings.moveDistance;
                patrolDriver.minMoveSpeed = patrolSettings.minMoveSpeed;
                patrolDriver.maxMoveSpeed = patrolSettings.maxMoveSpeed;
                break;
        }
    }

    #region Single Terrain Obstacle Spawning

    [ContextMenu("Create Single Obstacle")]
    public void CreateSingleObstacle()
    {
        AutoFindObstaclePrefab();
        if (obstaclePrefab == null)
        {
            Debug.LogWarning("⚠️ Please assign an Obstacle Prefab first.");
            return;
        }

        FetchTerrain();
        if (targetTerrain == null)
        {
            Debug.LogWarning("⚠️ No Terrain assigned or found in scene!");
            return;
        }

        // Spawn at terrain center XZ
        Vector3 terrainPos = targetTerrain.transform.position;
        Vector3 terrainSize = targetTerrain.terrainData.size;

        float centerX = terrainPos.x + (terrainSize.x * 0.5f);
        float centerZ = terrainPos.z + (terrainSize.z * 0.5f);
        float surfaceY = targetTerrain.SampleHeight(new Vector3(centerX, 0f, centerZ)) + terrainPos.y;

        Vector3 spawnWorldPos = new Vector3(centerX, surfaceY + Mathf.Max(0.2f, heightOffset), centerZ);
        Quaternion spawnRotation = Quaternion.Euler(0f, baseSettings.initialYRotation, 0f);

        GameObject spawned = SpawnObject(obstaclePrefab, spawnWorldPos, spawnRotation, "Create Single Obstacle");

        if (spawned != null)
        {
            EnsureBaseTerrainComponents(spawned);
            AttachDriverBySpawnType(spawned, scatterObstacleType);
            
#if UNITY_EDITOR
            Selection.activeGameObject = spawned;
#endif
        }
    }

    #endregion

    #region Terrain Obstacle Spattering

    [ContextMenu("Scatter Obstacles")]
    public void ScatterObstacles()
    {
        AutoFindObstaclePrefab();
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

            float yAngle = randomYRotation ? Random.Range(0f, 360f) : baseSettings.initialYRotation;
            Quaternion spawnRotation = Quaternion.Euler(0f, yAngle, 0f);

            GameObject spawned = SpawnObject(obstaclePrefab, spawnWorldPos, spawnRotation, "Scatter Obstacles");

            if (spawned != null)
            {
                EnsureBaseTerrainComponents(spawned);
                AttachDriverBySpawnType(spawned, scatterObstacleType);

                ObstacleBase obstacleComp = spawned.GetComponent<ObstacleBase>();
                if (obstacleComp != null)
                {
                    obstacleComp.initialYRotation = yAngle;
                    spawned.transform.rotation = spawnRotation;
                }
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