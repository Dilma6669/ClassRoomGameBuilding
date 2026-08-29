using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class EnvironmentController : MonoBehaviour
{
    [Header("Target Terrain Settings")]
    private Vector3 terrainPositionOffset = new Vector3(-500f, -50f, -500f);

    private Terrain childTerrain;

    private void Awake()
    {
        EnsureParentScaleReset();
        FindAndAlignTerrain();
    }

    private void Update()
    {
        EnsureParentScaleReset();
        FindAndAlignTerrain();
    }

    public void CreateTerrain()
    {
#if UNITY_EDITOR
        // Search project for the TerrainLogicDefault prefab
        string[] guids = AssetDatabase.FindAssets("TerrainLogicDefault t:Prefab");

        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab != null)
            {
                // Instantiate as child of EnvironmentController
                GameObject spawnedTerrain = (GameObject)PrefabUtility.InstantiatePrefab(prefab, transform);
                
                Undo.RegisterCreatedObjectUndo(spawnedTerrain, "Create Terrain Default");

                // Immediately cache and align position using existing script rules
                childTerrain = spawnedTerrain.GetComponentInChildren<Terrain>();
                FindAndAlignTerrain();

                Debug.Log($"✅ Successfully created '{spawnedTerrain.name}' as child of '{gameObject.name}'.");
            }
        }
        else
        {
            Debug.LogWarning("⚠️ Could not find 'TerrainLogicDefault' prefab in the project!");
        }
#endif
    }

    private void EnsureParentScaleReset()
    {
        // Keep the parent GameObject scaled strictly at (1, 1, 1)
        if (transform.localScale != Vector3.one)
        {
            transform.localScale = Vector3.one;
        }
    }

    private void FindAndAlignTerrain()
    {
        // Grab child terrain component
        if (childTerrain == null)
        {
            childTerrain = GetComponentInChildren<Terrain>();
        }

        if (childTerrain != null)
        {
            Transform t = childTerrain.transform;

            // Snap position to target offsets if moved
            if (t.localPosition != terrainPositionOffset)
            {
                t.localPosition = terrainPositionOffset;
            }

            // Lock rotation to zero
            if (t.localRotation != Quaternion.identity)
            {
                t.localRotation = Quaternion.identity;
            }

            // Lock child terrain scale to uniform (1, 1, 1)
            if (t.localScale != Vector3.one)
            {
                t.localScale = Vector3.one;
            }
        }
    }
}