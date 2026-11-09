using UnityEngine;
using System.Collections.Generic;

#if HAS_KINEMATIC_CC
using KinematicCharacterController.Examples;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class PlatformController : MonoBehaviour
{
    [HideInInspector]
    public GameObject platformLogicPrefab;

    [HideInInspector]
    public GameObject movingPlatformPrefab;

    [Header("Custom Platform Assets")]
    [Tooltip("Assign the custom mesh. This will automatically update both the visual mesh and the MeshCollider.")]
    public Mesh customMesh;

    [Tooltip("Assign an optional custom material (leave empty to keep default prefab material).")]
    public Material customMaterial;

    [Tooltip("3D model for NEW platforms (e.g. a floating rock from Assets/MyModels). Leave empty to use the default from Game Settings, or plain cylinders.")]
    public GameObject customModel;

    private bool isConvexCollider = true;

    private HashSet<int> knownChildIDs = new HashSet<int>();

    private void OnEnable()
    {
        CacheExistingChildren();
    }

    private void Update()
    {
        if (Application.isPlaying) return;

        if (transform.childCount > knownChildIDs.Count)
        {
            foreach (Transform child in transform)
            {
                int id = child.gameObject.GetInstanceID();

                if (!knownChildIDs.Contains(id))
                {
                    child.localPosition = Vector3.zero;
                    child.localRotation = Quaternion.identity;
                    child.localScale = Vector3.one;

                    knownChildIDs.Add(id);
                }
            }
        }
        else if (transform.childCount < knownChildIDs.Count)
        {
            CacheExistingChildren();
        }
    }

    private void CacheExistingChildren()
    {
        knownChildIDs.Clear();
        foreach (Transform child in transform)
        {
            knownChildIDs.Add(child.gameObject.GetInstanceID());
        }
    }

    public void SetupPlatforms()
    {
#if UNITY_EDITOR
        // Find PlatformLogicDefault prefab
        string[] logicGuids = AssetDatabase.FindAssets("PlatformLogicDefault t:Prefab");
        if (logicGuids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(logicGuids[0]);
            platformLogicPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Debug.Log("✅ Assigned 'PlatformLogicDefault' prefab.");
        }
        else
        {
            Debug.LogWarning("⚠️ Could not find 'PlatformLogicDefault' prefab in project.");
        }

        // Find MovingPlatform (3) prefab
        string[] movingGuids = AssetDatabase.FindAssets("\"MovingPlatform (3)\" t:Prefab");
        if (movingGuids.Length == 0)
        {
            // Fallback search in case exact quote matching misses
            movingGuids = AssetDatabase.FindAssets("MovingPlatform t:Prefab");
        }

        if (movingGuids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(movingGuids[0]);
            movingPlatformPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Debug.Log("✅ Assigned 'MovingPlatform (3)' prefab.");
        }
        else
        {
            Debug.LogWarning("⚠️ Could not find 'MovingPlatform (3)' prefab in project.");
        }

        EditorUtility.SetDirty(this);
#endif
    }

    [ContextMenu("Create Platform")]
    public void CreatePlatform()
    {
        if (platformLogicPrefab == null || movingPlatformPrefab == null)
        {
            Debug.LogWarning("⚠️ Cannot create platform! Please click 'Setup Platforms' first or ensure prefabs are present in project.");
            return;
        }

        GameObject platformParent;
        GameObject movingChild;

#if UNITY_EDITOR
        // 1. Instantiate outer PlatformLogic prefab
        platformParent = (GameObject)PrefabUtility.InstantiatePrefab(platformLogicPrefab, transform);
        platformParent.transform.localPosition = Vector3.zero;

        // 2. Find existing hidden/folder object, or fallback to creating one if missing
        Transform hiddenFolder = platformParent.transform.Find("⚠️ DO NOT TOUCH");
        if (hiddenFolder == null)
        {
            GameObject newFolder = new GameObject("⚠️ DO NOT TOUCH");
            newFolder.transform.SetParent(platformParent.transform, false);
            hiddenFolder = newFolder.transform;
        }

        // 3. Instantiate inner ExampleMovingPlatform inside the existing folder
        movingChild = (GameObject)PrefabUtility.InstantiatePrefab(movingPlatformPrefab, hiddenFolder);
        movingChild.transform.localPosition = Vector3.zero;

        Undo.RegisterCreatedObjectUndo(platformParent, "Create Platform Setup");
        Selection.activeGameObject = platformParent;
#else
        platformParent = Instantiate(platformLogicPrefab, transform);
        Transform hiddenFolder = platformParent.transform.Find("⚠️ DO NOT TOUCH");
        if (hiddenFolder == null)
        {
            GameObject newFolder = new GameObject("⚠️ DO NOT TOUCH");
            newFolder.transform.SetParent(platformParent.transform, false);
            hiddenFolder = newFolder.transform;
        }
        movingChild = Instantiate(movingPlatformPrefab, hiddenFolder);
        movingChild.transform.localPosition = Vector3.zero;
#endif

        // Apply custom Mesh & auto-generate MeshCollider
        ApplyCustomMeshAndCollider(movingChild);

        // Apply a 3D model: the one chosen here, otherwise the default from Game Settings
        // (unless a custom mesh was chosen - then keep that)
        GameObject platformModel = customModel != null ? customModel : (customMesh == null ? GameSettings.Load().defaultPlatformModel : null);
        ApplyPlatformModel(platformParent, platformModel);

        knownChildIDs.Add(platformParent.GetInstanceID());
    }

    /// <summary>Gives one platform a 3D model (or removes it when model is null).</summary>
    public static void ApplyPlatformModel(GameObject platform, GameObject model)
    {
        if (platform == null) return;
        PlatformModel pm = platform.GetComponent<PlatformModel>();
        if (model == null && pm == null) return;
        if (pm == null) pm = platform.AddComponent<PlatformModel>();
        pm.model = model;
        pm.Refresh();
    }

    /// <summary>Puts the Custom Model on EVERY platform in this level.</summary>
    public void ApplyModelToAllPlatforms()
    {
        foreach (PlatformLogic platform in FindObjectsByType<PlatformLogic>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (platform.gameObject.scene != gameObject.scene) continue;
#if UNITY_EDITOR
            Undo.RegisterFullObjectHierarchyUndo(platform.gameObject, "Change All Platform Models");
#endif
            ApplyPlatformModel(platform.gameObject, customModel);
        }
    }

    private void ApplyCustomMeshAndCollider(GameObject movingPlatformObj)
    {
        if (movingPlatformObj == null || customMesh == null) return;

        // 1. Disable existing default colliders on ExampleMovingPlatform
        Collider[] existingColliders = movingPlatformObj.GetComponents<Collider>();
        foreach (Collider col in existingColliders)
        {
            col.enabled = false;
        }

        // 2. Auto-generate and assign MeshCollider using customMesh
        MeshCollider meshCol = movingPlatformObj.GetComponent<MeshCollider>();
        if (meshCol == null)
        {
            meshCol = movingPlatformObj.AddComponent<MeshCollider>();
        }

        meshCol.enabled = true;
        meshCol.sharedMesh = customMesh;
        meshCol.convex = isConvexCollider;

        // 3. Traverse down to child to update MeshFilter and MeshRenderer
        if (movingPlatformObj.transform.childCount > 0)
        {
            Transform meshChild = movingPlatformObj.transform.GetChild(0);

            MeshFilter meshFilter = meshChild.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                meshFilter.sharedMesh = customMesh;
            }

            if (customMaterial != null)
            {
                MeshRenderer meshRenderer = meshChild.GetComponent<MeshRenderer>();
                if (meshRenderer != null)
                {
                    meshRenderer.sharedMaterial = customMaterial;
                }
            }
        }
    }
}