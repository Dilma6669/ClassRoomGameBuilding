using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class PlatformPopulator : MonoBehaviour
{
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

    [Header("Single Attachment Setup")]
    [Tooltip("Assign your base Obstacle prefab here.")]
    public GameObject obstaclePrefab;

    [Tooltip("Assign a custom mesh to override the obstacle visual and physics collision.")]
    public Mesh customMesh;

    [Tooltip("Assign an optional custom material (leave empty to keep default material).")]
    public Material customMaterial;

    [Range(0f, 5f)] private float obstacleSpawnHeightOffset = 0.5f;

    [Header("Random Scatter Setup")]
    [Tooltip("Select which type of obstacle to create when scattering across the platform.")]
    public ObstacleSpawnType scatterObstacleType = ObstacleSpawnType.Static;

    [Range(1, 50)] public int scatterCount = 5;
    [Range(0f, 2f)] private float scatterEdgePadding = 0.5f;
    [Range(0f, 5f)] private float scatterHeightOffset = 0.5f;
    private bool randomYRotation = true;

    #region Configurable Spawn Settings

    public BaseObstacleSettings baseSettings = new BaseObstacleSettings();
    public WandererSettings wanderSettings = new WandererSettings();
    public PatrolSettings patrolSettings = new PatrolSettings();

    #endregion

    private PlatformLogic platformLogic;

    private void FetchPlatformLogic()
    {
        if (platformLogic == null)
        {
            platformLogic = GetComponent<PlatformLogic>();
        }
    }

    private FollowPlatform EnsureBaseObstacleComponents(GameObject spawnedObject, Vector3 localOffset)
    {
        StripUnnecessaryComponents(spawnedObject);

        FollowPlatform followLogic = spawnedObject.GetComponent<FollowPlatform>();
        if (followLogic == null) followLogic = spawnedObject.AddComponent<FollowPlatform>();

        followLogic.targetPlatform = platformLogic.TargetChild;
        followLogic.offsetX = localOffset.x;
        followLogic.offsetY = localOffset.y;
        followLogic.offsetZ = localOffset.z;

        PlatformObstacle platformObstacle = spawnedObject.GetComponent<PlatformObstacle>();
        if (platformObstacle == null) platformObstacle = spawnedObject.AddComponent<PlatformObstacle>();

        ApplyBaseObstacleSettings(platformObstacle);
        ApplyCustomMeshAndCollider(spawnedObject);

        return followLogic;
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

    private void StripUnnecessaryComponents(GameObject obstacleObj)
    {
        if (obstacleObj == null) return;

        MonoBehaviour throttler = obstacleObj.GetComponent("AgentPerformanceThrottler") as MonoBehaviour;
        if (throttler != null)
        {
            DestroyComponentSafe(throttler);
        }

        NavMeshAgent agent = obstacleObj.GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            DestroyComponentSafe(agent);
        }

        Rigidbody[] rigidbodies = obstacleObj.GetComponentsInChildren<Rigidbody>(true);
        foreach (Rigidbody rb in rigidbodies)
        {
            DestroyComponentSafe(rb);
        }
    }

    private void DestroyComponentSafe(Component comp)
    {
        if (comp == null) return;
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            DestroyImmediate(comp);
            return;
        }
#endif
        Destroy(comp);
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
                if (obstacleObj.GetComponent<PlatformStaticDriver>() == null)
                    obstacleObj.AddComponent<PlatformStaticDriver>();
                break;

            case ObstacleSpawnType.Wander:
                PlatformWanderDriver wanderDriver = obstacleObj.GetComponent<PlatformWanderDriver>();
                if (wanderDriver == null) wanderDriver = obstacleObj.AddComponent<PlatformWanderDriver>();
                
                wanderDriver.wanderRadius = wanderSettings.wanderRadius;
                wanderDriver.minMoveSpeed = wanderSettings.minMoveSpeed;
                wanderDriver.maxMoveSpeed = wanderSettings.maxMoveSpeed;
                break;

            case ObstacleSpawnType.Patrol:
                PlatformPatrolDriver patrolDriver = obstacleObj.GetComponent<PlatformPatrolDriver>();
                if (patrolDriver == null) patrolDriver = obstacleObj.AddComponent<PlatformPatrolDriver>();

                patrolDriver.moveDistance = patrolSettings.moveDistance;
                patrolDriver.minMoveSpeed = patrolSettings.minMoveSpeed;
                patrolDriver.maxMoveSpeed = patrolSettings.maxMoveSpeed;
                break;
        }
    }

    #region Platform Management

    [ContextMenu("Duplicate Platform Setup")]
    public void DuplicatePlatform()
    {
#if UNITY_EDITOR
        FetchPlatformLogic();

        float xOffset = platformLogic != null ? platformLogic.widthOffset + 2f : 5f;
        Vector3 duplicatePosition = transform.position + new Vector3(xOffset, 0f, 0f);

        GameObject duplicateObj = Instantiate(gameObject, duplicatePosition, transform.rotation, transform.parent);
        duplicateObj.name = gameObject.name + "_Copy";

        Undo.RegisterCreatedObjectUndo(duplicateObj, "Duplicate Platform Setup");

        PlatformLogic newPlatformLogic = duplicateObj.GetComponent<PlatformLogic>();
        if (newPlatformLogic != null && newPlatformLogic.TargetChild != null)
        {
            FollowPlatform[] childFollowers = duplicateObj.GetComponentsInChildren<FollowPlatform>();
            foreach (FollowPlatform follower in childFollowers)
            {
                follower.targetPlatform = newPlatformLogic.TargetChild;
            }
        }

        Selection.activeGameObject = duplicateObj;
#endif
    }

    [ContextMenu("Delete Platform Setup")]
    public void DeletePlatform()
    {
#if UNITY_EDITOR
        Selection.activeGameObject = null;
        Undo.DestroyObjectImmediate(gameObject);
#else
        Destroy(gameObject);
#endif
    }

    #endregion

    #region Single Spawning Context Menus

    [ContextMenu("Create Single Obstacle")]
    public void CreateSingleObstacle()
    {
        GameObject obstacle = SpawnBaseObstacle("Create Single Obstacle", out Vector3 localOffset);
        if (obstacle == null) return;

        EnsureBaseObstacleComponents(obstacle, localOffset);
        AttachDriverBySpawnType(obstacle, scatterObstacleType);
    }

    private GameObject SpawnBaseObstacle(string undoName, out Vector3 localOffset)
    {
        localOffset = Vector3.zero;

        if (obstaclePrefab == null)
        {
            Debug.LogWarning("⚠️ Please assign an Obstacle Prefab first.");
            return null;
        }

        FetchPlatformLogic();
        if (platformLogic == null || platformLogic.TargetChild == null) return null;

        float yOffset = (platformLogic.heightOffset / 2f) + obstacleSpawnHeightOffset;
        localOffset = new Vector3(0f, yOffset, 0f);

        return SpawnObject(obstaclePrefab, localOffset, undoName);
    }

    #endregion

    #region Random Batch Spawning

    [ContextMenu("Scatter Random Objects")]
    public void ScatterRandomObjects()
    {
        if (obstaclePrefab == null)
        {
            Debug.LogWarning("⚠️ Please assign an Obstacle Prefab before scattering.");
            return;
        }

        FetchPlatformLogic();
        if (platformLogic == null || platformLogic.TargetChild == null) return;

        float halfWidth = Mathf.Max(0.1f, (platformLogic.widthOffset / 2f) - scatterEdgePadding);
        float halfDepth = Mathf.Max(0.1f, (platformLogic.depthOffset / 2f) - scatterEdgePadding);
        float yOffset = (platformLogic.heightOffset / 2f) + scatterHeightOffset;

        for (int i = 0; i < scatterCount; i++)
        {
            float randomX = Random.Range(-halfWidth, halfWidth);
            float randomZ = Random.Range(-halfDepth, halfDepth);
            Vector3 localOffset = new Vector3(randomX, yOffset, randomZ);

            GameObject spawned = SpawnObject(obstaclePrefab, localOffset, "Scatter Random Objects");

            if (spawned != null)
            {
                if (randomYRotation)
                {
                    float randomAngle = Random.Range(0f, 360f);

                    ObstacleLogic obstacleComponent = spawned.GetComponent<ObstacleLogic>();
                    if (obstacleComponent != null)
                    {
                        obstacleComponent.rotationAngle = randomAngle;
                    }
                    else
                    {
                        spawned.transform.rotation = Quaternion.Euler(0f, randomAngle, 0f);
                    }
                }

                EnsureBaseObstacleComponents(spawned, localOffset);
                AttachDriverBySpawnType(spawned, scatterObstacleType);
            }
        }
    }

    [ContextMenu("Clear All Spawned Attachments")]
    public void ClearAllSpawnedAttachments()
    {
        FollowPlatform[] followers = GetComponentsInChildren<FollowPlatform>();
        List<GameObject> objectsToDelete = new List<GameObject>();

        foreach (FollowPlatform follower in followers)
        {
            if (follower.gameObject != gameObject)
            {
                objectsToDelete.Add(follower.gameObject);
            }
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

    #endregion

    private GameObject SpawnObject(GameObject prefab, Vector3 localOffset, string undoName)
    {
        Vector3 spawnWorldPos = platformLogic.TargetChild.position + localOffset;

#if UNITY_EDITOR
        GameObject newObj = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        newObj.transform.position = spawnWorldPos;
        newObj.transform.SetParent(transform);
        Undo.RegisterCreatedObjectUndo(newObj, undoName);

        Selection.activeGameObject = newObj;

        return newObj;
#else
        return Instantiate(prefab, spawnWorldPos, Quaternion.identity, transform);
#endif
    }
}