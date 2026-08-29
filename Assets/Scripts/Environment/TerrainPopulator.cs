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

    [Header("Random Scatter Setup")]
    [Tooltip("Drag rock, tree, enemy, or prop prefabs here to scatter across the terrain.")]
    public GameObject[] randomPrefabs;

    [Range(1, 1000)] public int scatterCount = 20;
    [Range(0f, 20f)] private float edgePadding = 5f;
    [Range(0f, 5f)] private float heightOffset = 0f;

    private bool randomYRotation = true;
    private bool alignWithTerrainSlope = true;

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
        if (terrainCollider != null)
        {
            terrainCollider.hideFlags = HideFlags.HideInInspector;
        }

        navMeshSurface = GetComponent<NavMeshSurface>();
        if (navMeshSurface != null)
        {
            navMeshSurface.hideFlags = HideFlags.HideInInspector;
        }
    }

    [ContextMenu("Bake NavMesh Surface")]
    public void BakeNavMeshSurface()
    {
        FetchTerrain();

        // 1. Ensure NavMeshSurface exists and hidden
        if (navMeshSurface == null)
        {
            navMeshSurface = GetComponent<NavMeshSurface>();
            if (navMeshSurface == null)
            {
                navMeshSurface = gameObject.AddComponent<NavMeshSurface>();
            }
        }
        EnsureComponentsHidden();

        // 2. Configure NavMeshSurface properties for Terrain & Children
        navMeshSurface.collectObjects = CollectObjects.Children; // Collect terrain and scattered objects under this root
        navMeshSurface.useGeometry = NavMeshCollectGeometry.PhysicsColliders; // Use Physics Colliders (TerrainCollider)

        // 3. Ensure target Terrain has an active TerrainCollider
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

        // 4. Build the NavMesh
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

    [ContextMenu("Scatter Objects On Terrain")]
    public void ScatterObjectsOnTerrain()
    {
        FetchTerrain();

        if (targetTerrain == null)
        {
            Debug.LogWarning("⚠️ No Terrain assigned or found in scene!");
            return;
        }

        if (randomPrefabs == null || randomPrefabs.Length == 0)
        {
            Debug.LogWarning("⚠️ Please assign at least one prefab to the 'Random Prefabs' array before scattering.");
            return;
        }

        Vector3 terrainPos = targetTerrain.transform.position;
        Vector3 terrainSize = targetTerrain.terrainData.size;

        for (int i = 0; i < scatterCount; i++)
        {
            GameObject selectedPrefab = randomPrefabs[Random.Range(0, randomPrefabs.Length)];
            if (selectedPrefab == null) continue;

            float randomX = Random.Range(terrainPos.x + edgePadding, terrainPos.x + terrainSize.x - edgePadding);
            float randomZ = Random.Range(terrainPos.z + edgePadding, terrainPos.z + terrainSize.z - edgePadding);

            float surfaceY = targetTerrain.SampleHeight(new Vector3(randomX, 0f, randomZ)) + terrainPos.y;
            Vector3 spawnWorldPos = new Vector3(randomX, surfaceY + heightOffset, randomZ);

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

            if (randomYRotation)
            {
                float randomAngle = Random.Range(0f, 360f);
                spawnRotation *= Quaternion.Euler(0f, randomAngle, 0f);
            }

            GameObject spawned = SpawnObject(selectedPrefab, spawnWorldPos, spawnRotation, "Scatter On Terrain");

            if (spawned != null)
            {
                EnemyTerrainLogic enemyTerrain = spawned.GetComponent<EnemyTerrainLogic>();
                if (enemyTerrain != null)
                {
                    enemyTerrain.SetInitialRotation(spawnRotation);
                }
            }
        }
    }

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
