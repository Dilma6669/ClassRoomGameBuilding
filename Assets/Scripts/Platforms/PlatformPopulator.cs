using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class PlatformPopulator : MonoBehaviour
{
    [Header("Single Attachment Setup")]
    [Tooltip("Assign your consolidated Obstacle prefab here (handles Enemies, Hazards, Trampolines, and Pickups).")]
    public GameObject obstaclePrefab;
    [Range(0f, 5f)] private float obstacleSpawnHeightOffset = 0.5f;

    [Header("Random Scatter Setup")]
    [Tooltip("Drag rock, tree, or prop prefabs here to scatter randomly across the platform surface.")]
    public GameObject[] randomPrefabs;

    [Range(1, 50)] public int scatterCount = 5;
    [Range(0f, 2f)] private float scatterEdgePadding = 0.5f;
    [Range(0f, 5f)] private float scatterHeightOffset = 0.5f;
    private bool randomYRotation = true;

    private PlatformLogic platformLogic;

    private void FetchPlatformLogic()
    {
        if (platformLogic == null)
        {
            platformLogic = GetComponent<PlatformLogic>();
        }
    }

    private void SetupFollower(GameObject spawnedObject, Vector3 localOffset)
    {
        FollowPlatform followLogic = spawnedObject.GetComponent<FollowPlatform>();
        if (followLogic == null) followLogic = spawnedObject.AddComponent<FollowPlatform>();

        followLogic.targetPlatform = platformLogic.TargetChild;
        followLogic.offsetX = localOffset.x;
        followLogic.offsetY = localOffset.y;
        followLogic.offsetZ = localOffset.z;
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

    [ContextMenu("Create Obstacle On Center")]
    public void CreateObstacleOnPlatform()
    {
        if (obstaclePrefab == null)
        {
            Debug.LogWarning("⚠️ Please assign an Obstacle Prefab first.");
            return;
        }

        FetchPlatformLogic();
        if (platformLogic == null || platformLogic.TargetChild == null) return;

        float yOffset = (platformLogic.heightOffset / 2f) + obstacleSpawnHeightOffset;
        GameObject obstacle = SpawnObject(obstaclePrefab, new Vector3(0f, yOffset, 0f), "Create Obstacle On Platform");

        if (obstacle != null)
        {
            SetupFollower(obstacle, new Vector3(0f, yOffset, 0f));
        }
    }

    #endregion

    #region Random Batch Spawning

    [ContextMenu("Scatter Random Objects")]
    public void ScatterRandomObjects()
    {
        if (randomPrefabs == null || randomPrefabs.Length == 0)
        {
            Debug.LogWarning("⚠️ Please assign at least one prefab to the 'Random Prefabs' array before scattering.");
            return;
        }

        FetchPlatformLogic();
        if (platformLogic == null || platformLogic.TargetChild == null) return;

        float halfWidth = Mathf.Max(0.1f, (platformLogic.widthOffset / 2f) - scatterEdgePadding);
        float halfDepth = Mathf.Max(0.1f, (platformLogic.depthOffset / 2f) - scatterEdgePadding);
        float yOffset = (platformLogic.heightOffset / 2f) + scatterHeightOffset;

        for (int i = 0; i < scatterCount; i++)
        {
            GameObject selectedPrefab = randomPrefabs[Random.Range(0, randomPrefabs.Length)];
            if (selectedPrefab == null) continue;

            float randomX = Random.Range(-halfWidth, halfWidth);
            float randomZ = Random.Range(-halfDepth, halfDepth);
            Vector3 localOffset = new Vector3(randomX, yOffset, randomZ);

            GameObject spawned = SpawnObject(selectedPrefab, localOffset, "Scatter Random Objects");

            if (spawned != null)
            {
                if (randomYRotation)
                {
                    float randomAngle = Random.Range(0f, 360f);

                    Obstacle obstacleComponent = spawned.GetComponent<Obstacle>();
                    if (obstacleComponent != null)
                    {
                        obstacleComponent.rotationAngle = randomAngle;
                    }
                    else
                    {
                        spawned.transform.rotation = Quaternion.Euler(0f, randomAngle, 0f);
                    }
                }

                SetupFollower(spawned, localOffset);
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