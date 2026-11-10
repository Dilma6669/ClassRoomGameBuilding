using UnityEditor;
using UnityEngine;

/// <summary>
/// One 'Move it' panel for everything (creatures, pickups, platforms), shown in a corner of the Scene view.
/// Opened with a '📍 Move it…' button in the Game Builder; closed with ✔ OK (or Esc-free: just click OK).
/// Left-right / up-down / forward-back with − and +, a typed number, 'Click a new spot', and extras
/// (back on the ground, platform height). The Move tool's arrows are shown on the object too.
/// </summary>
public static class MovePanel
{
    private const float Step = 0.5f;
    private const int WindowId = 0x4D4F5645; // 'MOVE'

    private static Transform target;
    private static string label;
    private static System.Action<RaycastHit> moveTo;
    private static System.Action putOnGround;
    private static System.Action extraGUI;
    private static Rect window;
    private static bool placed;

    public static bool IsOpenFor(Transform t) => target != null && target == t;

    /// <param name="t">What moves (for a platform: its moving part).</param>
    /// <param name="what">Its name, for the title.</param>
    /// <param name="newSpot">What happens when a new spot is clicked in the Scene.</param>
    /// <param name="ground">'Back on the ground', or null to hide that button.</param>
    /// <param name="extra">Extra controls (e.g. a platform's height slider), or null.</param>
    public static void Open(Transform t, string what, System.Action<RaycastHit> newSpot, System.Action ground = null, System.Action extra = null)
    {
        SceneView.duringSceneGui -= OnScene;
        target = t;
        label = what;
        moveTo = newSpot;
        putOnGround = ground;
        extraGUI = extra;
        placed = false;
        SceneView.duringSceneGui += OnScene;
        Selection.selectionChanged -= OnSelectionChanged;
        Selection.selectionChanged += OnSelectionChanged;

        // Show the Move tool's arrows on it, so it can be dragged too
        Tools.current = Tool.Move;
        SceneView.RepaintAll();
    }

    /// <summary>Picked something else? Close, so the panel never moves the wrong thing.</summary>
    private static void OnSelectionChanged()
    {
        if (target == null) { Close(); return; }
        Transform picked = Selection.activeTransform;
        if (picked == null) return;
        if (OwnerOf(picked) != OwnerOf(target)) Close();
    }

    /// <summary>The creature / pickup / platform a transform belongs to (clicking its model counts as the same thing).</summary>
    private static Transform OwnerOf(Transform t)
    {
        ObstacleBase o = t.GetComponentInParent<ObstacleBase>();
        if (o != null) return o.transform;
        PlatformLogic p = t.GetComponentInParent<PlatformLogic>();
        return p != null ? p.transform : t;
    }

    public static void Close()
    {
        Selection.selectionChanged -= OnSelectionChanged;
        SceneView.duringSceneGui -= OnScene;
        if (ScenePlacer.IsPlacing && ScenePlacer.What == "new spot for " + label) ScenePlacer.Cancel();
        target = null;
        moveTo = null;
        putOnGround = null;
        extraGUI = null;
        SceneView.RepaintAll();
        foreach (GameBuilderWindow w in Resources.FindObjectsOfTypeAll<GameBuilderWindow>()) w.Repaint();
    }

    private static void OnScene(SceneView view)
    {
        if (target == null) { Close(); return; } // deleted, or another level opened

        if (!placed)
        {
            // Bottom-left corner of the Scene view to start with (it can be dragged by its title)
            window = new Rect(10f, Mathf.Max(30f, view.position.height - 300f), 330f, 10f);
            placed = true;
        }

        Handles.BeginGUI();
        window = GUILayout.Window(WindowId, window, DrawWindow, "📍 Move: " + label);
        Handles.EndGUI();
    }

    private static void DrawWindow(int id)
    {
        if (target == null) return;
        float oldWidth = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = 120f;

        Vector3 pos = target.position;
        Vector3 wanted = pos;
        wanted.x = Row("↔ Left / right", pos.x);
        wanted.y = Row("↕ Up / down", pos.y);
        wanted.z = Row("↗ Forward / back", pos.z);
        if ((wanted - pos).sqrMagnitude > 0.0000001f)
        {
            Undo.RecordObject(target, "Move " + label);
            target.position = wanted;
            SceneView.RepaintAll();
        }

        if (extraGUI != null) extraGUI();

        EditorGUILayout.Space(2);
        bool placing = ScenePlacer.IsPlacing && ScenePlacer.What == "new spot for " + label;
        if (GUILayout.Button(placing ? "👉 Now click the new spot (Esc to stop)" : "📍 Click a new spot for it", GUILayout.Height(24)))
        {
            if (placing) ScenePlacer.Cancel();
            else
            {
                System.Action<RaycastHit> move = moveTo;
                ScenePlacer.Begin("new spot for " + label, hit => { move?.Invoke(hit); SceneView.RepaintAll(); });
            }
        }
        if (putOnGround != null && GUILayout.Button(new GUIContent("⬇ Back on the ground (automatic height)", "Drops it onto the ground or platform under it."), GUILayout.Height(24)))
        {
            putOnGround();
            SceneView.RepaintAll();
        }

        EditorGUILayout.Space(4);
        Color old = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.2f, 0.8f, 0.3f);
        if (GUILayout.Button("✔ OK", new GUIStyle(GUI.skin.button) { fontStyle = FontStyle.Bold, fontSize = 13 }, GUILayout.Height(30)))
        {
            GUI.backgroundColor = old;
            EditorGUIUtility.labelWidth = oldWidth;
            Close();
            GUIUtility.ExitGUI();
        }
        GUI.backgroundColor = old;
        EditorGUILayout.LabelField("Tip: you can also drag the arrows in the Scene.", EditorStyles.centeredGreyMiniLabel);

        EditorGUIUtility.labelWidth = oldWidth;
        GUI.DragWindow(new Rect(0, 0, 10000, 20));
    }

    private static float Row(string text, float value)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField(text, GUILayout.Width(118f));
            if (GUILayout.Button("−", GUILayout.Width(26))) value -= Step;
            value = EditorGUILayout.FloatField(Mathf.Round(value * 100f) / 100f, GUILayout.MinWidth(60));
            if (GUILayout.Button("+", GUILayout.Width(26))) value += Step;
        }
        return value;
    }
}
