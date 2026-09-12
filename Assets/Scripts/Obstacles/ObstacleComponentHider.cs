using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

[ExecuteAlways]
public class ObstacleComponentHider : MonoBehaviour
{
    [Header("Inspector Visibility")]
    [Tooltip("Uncheck to collapse and hide advanced components from the Inspector UI.")]
    [SerializeField] private bool showAdvancedComponents = false;

    private void OnValidate()
    {
        ApplyVisibility(showAdvancedComponents);
    }

    private void Reset()
    {
        ApplyVisibility(showAdvancedComponents);
    }

    public void ApplyVisibility(bool visible)
    {
#if UNITY_EDITOR
        HideFlags flags = visible ? HideFlags.None : HideFlags.HideInInspector;

        // Fetch all targets across this root object and any child geometry/rigs
        Rigidbody[] rigidbodies = GetComponentsInChildren<Rigidbody>(true);
        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        UnityEngine.AI.NavMeshAgent[] agents = GetComponentsInChildren<UnityEngine.AI.NavMeshAgent>(true);
        AgentPerformanceThrottler[] throttlers = GetComponentsInChildren<AgentPerformanceThrottler>(true);

        foreach (var rb in rigidbodies) rb.hideFlags = flags;
        foreach (var col in colliders) col.hideFlags = flags;
        foreach (var agent in agents) agent.hideFlags = flags;
        foreach (var throttler in throttlers) throttler.hideFlags = flags;

        // Force Unity Inspector to instantly refresh views
        InternalEditorUtility.RepaintAllViews();
#endif
    }

    #region Context Menus (Right-Click Component Header)

    [ContextMenu("Hide Physics & Nav Components")]
    private void ContextHide()
    {
        showAdvancedComponents = false;
        ApplyVisibility(false);
    }

    [ContextMenu("Show Physics & Nav Components")]
    private void ContextShow()
    {
        showAdvancedComponents = true;
        ApplyVisibility(true);
    }

    #endregion
}