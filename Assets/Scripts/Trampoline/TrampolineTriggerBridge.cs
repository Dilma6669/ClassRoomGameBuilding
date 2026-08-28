using UnityEngine;

public class TrampolineTriggerBridge : MonoBehaviour
{
    private TrampolineLogic parentLogic;

    private void Awake()
    {
        // Automatically find the TrampolineLogic component on the parent object
        parentLogic = GetComponentInParent<TrampolineLogic>();

        if (parentLogic == null)
        {
            Debug.LogError($"[TrampolineTriggerBridge] Could not find TrampolineLogic script on parent of {gameObject.name}!");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (parentLogic != null)
        {
            parentLogic.HandlePlayerEnter(other);
        }
    }
}