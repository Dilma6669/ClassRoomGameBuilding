using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>Creatures & Pickups tab: how the selected creature or pickup moves (stays still / wanders / patrols) and its settings.</summary>
public partial class GameBuilderWindow
{
    private enum Movement { Still, Wander, Patrol }

    private static Movement MovementOf(ObstacleBase o)
    {
        IObstacleMovement m = o.GetComponent<IObstacleMovement>();
        string name = m == null ? "" : m.GetType().Name;
        if (name.Contains("Wander")) return Movement.Wander;
        if (name.Contains("Patrol")) return Movement.Patrol;
        return Movement.Still;
    }

    private void DrawMovement(ObstacleBase o)
    {
        bool creature = o.payloadType == ObstacleBase.PayloadType.Damage;
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("🚶 How it moves", EditorStyles.boldLabel);
        Movement now = MovementOf(o);
        Movement[] all = { Movement.Still, Movement.Wander, Movement.Patrol };
        string[] labels = creature
            ? new[] { "🐍 Stays still (Guard)", "🕷 Wanders around", "🧟 Walks back & forth" }
            : new[] { "📍 Stays still", "🕷 Wanders around", "↔ Floats back & forth" };
        Color chosen = new Color(0.6f, 0.85f, 1f);
        int clicked = ButtonGrid(Columns(3), labels, all.Select(m => m == now ? chosen : (Color?)null).ToList());
        if (clicked >= 0 && all[clicked] != now)
        {
            SetMovement(o, all[clicked]);
            GUIUtility.ExitGUI();
        }

        MonoBehaviour driver = o.GetComponent<IObstacleMovement>() as MonoBehaviour;
        if (driver != null && now != Movement.Still) DrawInspector(driver);
    }

    /// <summary>Swaps the movement script (the ground and platform versions are different scripts).</summary>
    private static void SetMovement(ObstacleBase o, Movement movement)
    {
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Change how it moves");
        string oldKind = o.KindName();
        bool onPlatform = o is PlatformObstacle;

        foreach (Component c in o.GetComponents<Component>())
        {
            if (c is IObstacleMovement) Undo.DestroyObjectImmediate(c);
        }

        GameObject go = o.gameObject;
        switch (movement)
        {
            case Movement.Still:
                if (onPlatform) Undo.AddComponent<PlatformStaticDriver>(go);
                else Undo.AddComponent<TerrainStaticDriver>(go);
                break;
            case Movement.Wander:
                if (onPlatform) { PlatformWanderDriver d = Undo.AddComponent<PlatformWanderDriver>(go); d.wanderRadius = 4f; d.minMoveSpeed = 1.5f; d.maxMoveSpeed = 2.5f; }
                else { TerrainWanderDriver d = Undo.AddComponent<TerrainWanderDriver>(go); d.wanderRadius = 6f; d.minMoveSpeed = 1.5f; d.maxMoveSpeed = 3f; }
                break;
            case Movement.Patrol:
                if (onPlatform) { PlatformPatrolDriver d = Undo.AddComponent<PlatformPatrolDriver>(go); d.moveDistance = 3f; d.minMoveSpeed = 1.5f; d.maxMoveSpeed = 2f; }
                else { TerrainPatrolDriver d = Undo.AddComponent<TerrainPatrolDriver>(go); d.moveDistance = 6f; d.minMoveSpeed = 1.5f; d.maxMoveSpeed = 2.5f; }
                break;
        }

        // Still called by its old kind ('Wanderer')? Rename it to the new one ('Patroller')
        if (go.name == oldKind)
        {
            Undo.RecordObject(go, "Rename");
            go.name = o.KindName();
        }

        Undo.CollapseUndoOperations(group);
        EditorUtility.SetDirty(o);
    }
}

/// <summary>Platforms tab: one slider for how high the selected platform floats above the ground under it.</summary>
public partial class GameBuilderWindow
{
    private void DrawPlatformHeight(PlatformLogic p)
    {
        Transform moving = p.TargetChild;
        Terrain terrain = ScatterTools.FindTerrain();
        if (moving == null || terrain == null) return;

        float ground = terrain.SampleHeight(moving.position) + terrain.GetPosition().y;
        float height = Mathf.Round((moving.position.y - ground) * 10f) / 10f;

        EditorGUI.BeginChangeCheck();
        float wanted = EditorGUILayout.Slider(new GUIContent("⬆ Height (m)",
            "How high this platform floats above the ground under it. (A moving platform: where it starts.)"),
            height, 0f, Mathf.Max(40f, height));
        if (!EditorGUI.EndChangeCheck()) return;

        // Its height is 'Offset Y' (relative to the platform's holder); set it through Unity so Undo and OnValidate work
        SerializedObject so = new SerializedObject(p);
        SerializedProperty offsetY = so.FindProperty("offsetY");
        float up = (wanted - height) / Mathf.Max(0.0001f, p.transform.lossyScale.y);
        offsetY.floatValue += up;
        so.ApplyModifiedProperties();
        moving.localPosition = new Vector3(p.offsetX, p.offsetY, p.offsetZ); // PlatformLogic would only do this on its next update
        SceneView.RepaintAll();
    }
}

/// <summary>The '📍 Move it…' button: opens the one Move panel (Editor/MovePanel.cs) in the Scene view.</summary>
public partial class GameBuilderWindow
{
    private void DrawPosition(Transform t, string what, System.Action<RaycastHit> moveTo, System.Action putOnGround = null, System.Action extra = null)
    {
        if (t == null) return;
        bool open = MovePanel.IsOpenFor(t);
        Color old = GUI.backgroundColor;
        if (open) GUI.backgroundColor = new Color(0.6f, 0.85f, 1f); // highlighted while the panel is open
        if (GUILayout.Button(new GUIContent(open ? "📍 Moving it… (the Move panel is in the Scene view; click ✔ OK there)" : "📍 Move it…  (left-right, up-down, new spot)",
                "Opens the Move panel in the corner of the Scene view. Click ✔ OK there when it's in the right spot."), GUILayout.Height(28)))
        {
            if (open) MovePanel.Close();
            else MovePanel.Open(t, what, moveTo, putOnGround, extra);
        }
        GUI.backgroundColor = old;
    }


    /// <summary>Move a creature or pickup to a clicked spot (along its own platform, or on the ground).</summary>
    private void MoveObstacleTo(ObstacleBase o, RaycastHit hit)
    {
        PlatformLogic clickedPlatform = hit.collider.GetComponentInParent<PlatformLogic>();
        FollowPlatform follow = o.GetComponent<FollowPlatform>();
        bool onPlatform = o is PlatformObstacle && follow != null && follow.targetPlatform != null;

        if (onPlatform)
        {
            PlatformLogic home = follow.targetPlatform.GetComponentInParent<PlatformLogic>();
            if (clickedPlatform != null && clickedPlatform == home) { MoveOnPlatform(o, hit.point); return; }
            EditorUtility.DisplayDialog("Move " + o.name, o.name + " lives on the platform '" + (home != null ? home.name : "?") +
                "', so it can only move around on that platform.\n\nTo have one somewhere else, delete this one and place a new one there.", "OK");
            return;
        }

        if (clickedPlatform != null)
        {
            EditorUtility.DisplayDialog("Move " + o.name, o.name + " lives on the ground, so it can't go onto a platform.\n\nTo have one on a platform, place a new one there.", "OK");
            return;
        }
        MoveToSceneViewGround(o.transform, hit.point);
    }

    /// <summary>Move a platform to a clicked spot, keeping how high it floats.</summary>
    private static void MovePlatformTo(PlatformLogic p, RaycastHit hit)
    {
        Transform moving = p.TargetChild;
        Terrain terrain = ScatterTools.FindTerrain();
        if (moving == null) return;
        float height = terrain != null ? moving.position.y - (terrain.SampleHeight(moving.position) + terrain.GetPosition().y) : 0f;
        float groundY = terrain != null ? terrain.SampleHeight(hit.point) + terrain.GetPosition().y : hit.point.y;
        Undo.RecordObject(moving, "Move platform");
        moving.position = new Vector3(hit.point.x, groundY + height, hit.point.z);
    }
}

public partial class GameBuilderWindow
{
    /// <summary>Automatic height: drop a creature or pickup onto its platform, or onto the ground under it.</summary>
    private static void PutObstacleOnGround(ObstacleBase o)
    {
        FollowPlatform follow = o.GetComponent<FollowPlatform>();
        if (o is PlatformObstacle po && follow != null && follow.targetPlatform != null)
        {
            MoveOnPlatform(o, o.transform.position); // works out the platform's top under it and sits it there
            return;
        }
        Terrain terrain = ScatterTools.FindTerrain();
        if (terrain == null) return;
        Vector3 p = o.transform.position;
        Undo.RecordObject(o.transform, "Back on the ground");
        // The shape is lifted inside (Sit On Ground), so the creature's own spot is the ground itself, like when it walks
        o.transform.position = new Vector3(p.x, terrain.SampleHeight(p) + terrain.GetPosition().y, p.z);
    }
}
