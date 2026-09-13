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

        // Fetch ONLY components that ALREADY exist on this GameObject or its children
        Rigidbody[] rigidbodies = GetComponentsInChildren<Rigidbody>(true);
        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        UnityEngine.AI.NavMeshAgent[] agents = GetComponentsInChildren<UnityEngine.AI.NavMeshAgent>(true);
        AgentPerformanceThrottler[] throttlers = GetComponentsInChildren<AgentPerformanceThrottler>(true);

        // Apply hideFlags ONLY to existing, non-null components
        foreach (var rb in rigidbodies) if (rb != null) rb.hideFlags = flags;
        foreach (var col in colliders) if (col != null) col.hideFlags = flags;
        foreach (var agent in agents) if (agent != null) agent.hideFlags = flags;
        foreach (var throttler in throttlers) if (throttler != null) throttler.hideFlags = flags;

        // Refresh Inspector windows safely
        if (!Application.isPlaying)
        {
            EditorUtility.SetDirty(this);
            InternalEditorUtility.RepaintAllViews();
        }
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