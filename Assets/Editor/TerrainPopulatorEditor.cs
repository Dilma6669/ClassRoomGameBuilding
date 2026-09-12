using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TerrainPopulator))]
public class TerrainPopulatorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        TerrainPopulator populator = (TerrainPopulator)target;

        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            fixedHeight = 30
        };

        EditorGUILayout.Space(15);
        EditorGUILayout.LabelField("Terrain Scatter Controls", EditorStyles.boldLabel);

        // Patrol Scatter Button
        GUI.backgroundColor = new Color(0.9f, 0.6f, 0.2f);
        if (GUILayout.Button("🚶 Scatter Patrol Obstacles", buttonStyle))
        {
            populator.ScatterPatrolObstacles();
        }

        EditorGUILayout.Space(4);

        // Wander Scatter Button
        GUI.backgroundColor = new Color(0.3f, 0.7f, 0.9f);
        if (GUILayout.Button("🎲 Scatter Wander Obstacles", buttonStyle))
        {
            populator.ScatterWanderObstacles();
        }

        EditorGUILayout.Space(4);

        // Static Scatter Button
        GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
        if (GUILayout.Button("🛑 Scatter Static Obstacles", buttonStyle))
        {
            populator.ScatterStaticObstacles();
        }

        EditorGUILayout.Space(15);
        EditorGUILayout.LabelField("NavMesh & Management", EditorStyles.boldLabel);

        // Bake NavMesh Button
        GUI.backgroundColor = new Color(0.2f, 0.8f, 0.8f);
        if (GUILayout.Button("🧠 Bake NavMesh Surface", buttonStyle))
        {
            populator.BakeNavMeshSurface();
        }

        EditorGUILayout.Space(6);

        // Clear Button
        GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f);
        if (GUILayout.Button("🗑️ Clear All Terrain Spawns", buttonStyle))
        {
            populator.ClearTerrainSpawns();
        }

        GUI.backgroundColor = Color.white;
    }
}