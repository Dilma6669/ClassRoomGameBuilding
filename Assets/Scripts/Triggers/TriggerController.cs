using UnityEngine;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class TriggerController : MonoBehaviour
{
    //[Header("Prefab Settings")]
    //[Tooltip("Drag the 'UITriggerLogic' prefab here, or leave empty to auto-find it in the Project.")]
    private GameObject uiTriggerPrefab;

    public void CreateTrigger()
    {
#if UNITY_EDITOR
        if (uiTriggerPrefab == null)
        {
            string[] guids = AssetDatabase.FindAssets("TriggerLogicDefault t:Prefab");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                uiTriggerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }
        }
#endif

        if (uiTriggerPrefab == null)
        {
            Debug.LogError("UIController: Could not find a prefab named 'UITriggerLogic' in your Project folder!", this);
            return;
        }

        GameObject newTrigger;

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            newTrigger = (GameObject)PrefabUtility.InstantiatePrefab(uiTriggerPrefab, transform);
        }
        else
        {
            newTrigger = Instantiate(uiTriggerPrefab, transform);
        }
#else
        newTrigger = Instantiate(uiTriggerPrefab, transform);
#endif

        newTrigger.name = "UITrigger";
        
        newTrigger.transform.localPosition = Vector3.zero;
        newTrigger.transform.localRotation = Quaternion.identity;

        // Hunt down the TextMeshProUGUI component in this GameObject's children (or parent Canvas)
        TextMeshProUGUI targetText = GetComponentInChildren<TextMeshProUGUI>(true);
        if (targetText == null)
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                targetText = canvas.GetComponentInChildren<TextMeshProUGUI>(true);
            }
        }

        // Assign the found TMP reference directly into the trigger logic component upon creation
        TriggerLogic triggerLogic = newTrigger.GetComponent<TriggerLogic>();
        if (triggerLogic != null)
        {
            if (targetText != null)
            {
                triggerLogic.uiTextObject = targetText;
            }
            else
            {
                Debug.LogWarning("UIController: Created trigger, but could not find any TextMeshProUGUI component in child objects or Canvas!", this);
            }
        }

#if UNITY_EDITOR
        Undo.RegisterCreatedObjectUndo(newTrigger, "Create UI Trigger");
        Selection.activeGameObject = newTrigger;
#endif
    }
}