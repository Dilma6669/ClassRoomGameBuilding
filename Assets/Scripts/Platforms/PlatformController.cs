using UnityEngine;
using System.Collections.Generic;
using KinematicCharacterController.Examples;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class PlatformController : MonoBehaviour
{
    [Header("Platform Prefab References")]
    [Tooltip("Find this prefab in: Assets/Prefabs/Platforms/PlatformLogicDefault")]
    public GameObject platformLogicPrefab;

    [Tooltip("Find this prefab in: Assets/KinematicCharacterController/Examples/Prefabs/MovingPlatform (3)")]
    public GameObject movingPlatformPrefab;

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

    [ContextMenu("Create Platform")]
    public void CreatePlatform()
    {
        if (platformLogicPrefab == null || movingPlatformPrefab == null)
        {
            Debug.LogWarning("⚠️ Cannot create platform! Please ensure both 'Platform Logic Prefab' and 'Moving Platform Prefab' are assigned on PlatformController.");
            return;
        }

#if UNITY_EDITOR
        // 1. Instantiate outer PlatformLogic prefab
        GameObject platformParent = (GameObject)PrefabUtility.InstantiatePrefab(platformLogicPrefab, transform);
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
        GameObject movingChild = (GameObject)PrefabUtility.InstantiatePrefab(movingPlatformPrefab, hiddenFolder);
        movingChild.transform.localPosition = Vector3.zero;

        Undo.RegisterCreatedObjectUndo(platformParent, "Create Platform Setup");
        Selection.activeGameObject = platformParent;
#else
        GameObject platformParent = Instantiate(platformLogicPrefab, transform);
        Transform hiddenFolder = platformParent.transform.Find("⚠️ DO NOT TOUCH");
        if (hiddenFolder == null)
        {
            GameObject newFolder = new GameObject("⚠️ DO NOT TOUCH");
            newFolder.transform.SetParent(platformParent.transform, false);
            hiddenFolder = newFolder.transform;
        }
        GameObject movingChild = Instantiate(movingPlatformPrefab, hiddenFolder);
        movingChild.transform.localPosition = Vector3.zero;
#endif

        knownChildIDs.Add(platformParent.GetInstanceID());
    }
}