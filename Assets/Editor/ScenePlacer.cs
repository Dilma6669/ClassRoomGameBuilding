using UnityEditor;
using UnityEngine;

/// <summary>
/// "Click in the Scene to place it": after a Game Builder button is clicked, the Scene view shows a marker
/// under the mouse. A left-click places the thing there; Esc (or the Cancel button) stops.
/// Right-drag / Alt-drag still move the camera while placing.
/// </summary>
public static class ScenePlacer
{
    private static System.Action<RaycastHit> onPlace;
    private static string what;

    public static bool IsPlacing => onPlace != null;
    public static string What => what;

    /// <summary>Remembered for the Game Builder's tick box (on unless the student turns it off).</summary>
    public static bool ClickToPlace
    {
        get => EditorPrefs.GetBool("GameBuilder.ClickToPlace", true);
        set => EditorPrefs.SetBool("GameBuilder.ClickToPlace", value);
    }

    public static void Begin(string label, System.Action<RaycastHit> place)
    {
        Cancel();
        what = label;
        onPlace = place;
        SceneView.duringSceneGui += OnScene;
        if (SceneView.lastActiveSceneView != null) SceneView.lastActiveSceneView.Focus();
        SceneView.RepaintAll();
    }

    public static void Cancel()
    {
        SceneView.duringSceneGui -= OnScene;
        onPlace = null;
        what = null;
        SceneView.RepaintAll();
        RepaintBuilder();
    }

    private static void OnScene(SceneView view)
    {
        if (onPlace == null) { SceneView.duringSceneGui -= OnScene; return; }
        Event e = Event.current;

        // Take over left-clicks in the Scene view (so clicking doesn't select something else)
        int id = GUIUtility.GetControlID(FocusType.Passive);
        if (e.type == EventType.Layout) HandleUtility.AddDefaultControl(id);

        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
        {
            e.Use();
            Cancel();
            return;
        }

        bool hasSpot = FindSpot(e.mousePosition, out RaycastHit hit);

        if (e.type == EventType.Repaint && hasSpot)
        {
            PlatformLogic platform = hit.collider.GetComponentInParent<PlatformLogic>();
            Handles.color = platform != null ? new Color(0.4f, 0.8f, 1f) : new Color(0.3f, 1f, 0.4f);
            Handles.DrawWireDisc(hit.point, Vector3.up, 1f, 3f);
            Handles.DrawWireDisc(hit.point, Vector3.up, 0.15f, 3f);
            Handles.DrawLine(hit.point, hit.point + Vector3.up * 2f, 3f);
        }

        // The hint next to the mouse
        Handles.BeginGUI();
        string hint = "Click to place: " + what + (hasSpot && hit.collider.GetComponentInParent<PlatformLogic>() != null ? "  (on this platform)" : "") + "\nEsc to stop";
        GUIStyle style = new GUIStyle(EditorStyles.helpBox) { fontSize = 12, fontStyle = FontStyle.Bold };
        Vector2 size = style.CalcSize(new GUIContent(hint));
        GUI.Label(new Rect(e.mousePosition.x + 18f, e.mousePosition.y + 10f, size.x + 8f, size.y + 4f), hint, style);
        Handles.EndGUI();

        if (e.type == EventType.MouseMove || e.type == EventType.MouseDrag) view.Repaint();

        if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
        {
            e.Use();
            if (!hasSpot) return; // clicked the sky: keep waiting
            System.Action<RaycastHit> place = onPlace;
            Cancel();
            place(hit);
            RepaintBuilder();
        }
    }

    /// <summary>What's under the mouse: the ground, a platform, or anything else solid.</summary>
    private static bool FindSpot(Vector2 mouse, out RaycastHit hit)
    {
        Ray ray = HandleUtility.GUIPointToWorldRay(mouse);
        Physics.SyncTransforms();
        return Physics.Raycast(ray, out hit, 5000f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
    }

    private static void RepaintBuilder()
    {
        foreach (GameBuilderWindow w in Resources.FindObjectsOfTypeAll<GameBuilderWindow>()) w.Repaint();
    }
}
