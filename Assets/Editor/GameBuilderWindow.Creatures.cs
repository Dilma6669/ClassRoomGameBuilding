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
        Section("🚶 How it moves");
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
        if (creature) EditorGUILayout.LabelField("Want it to chase the player? Tick 'Chase the player' near the bottom of its settings above.", EditorStyles.wordWrappedMiniLabel);
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
