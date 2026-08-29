using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PlatformPopulator))]
public class PlatformPopulatorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        PlatformPopulator populator = (PlatformPopulator)target;

        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            fixedHeight = 30
        };

        EditorGUILayout.Space(15);
        EditorGUILayout.LabelField("Platform Management", EditorStyles.boldLabel);

        // Duplicate Platform Button
        GUI.backgroundColor = new Color(0.2f, 0.8f, 0.8f);
        if (GUILayout.Button("📋 Duplicate Platform Setup", buttonStyle))
        {
            populator.DuplicatePlatform();
        }

        EditorGUILayout.Space(4);

        // Delete Platform Button
        GUI.backgroundColor = new Color(0.9f, 0.2f, 0.2f);
        if (GUILayout.Button("❌ Delete Platform Setup", buttonStyle))
        {
            populator.DeletePlatform();
            return; // Exit GUI immediately since the target object has been deleted
        }

        EditorGUILayout.Space(15);
        EditorGUILayout.LabelField("Single Attachment Spawning", EditorStyles.boldLabel);

        // Enemy Button
        GUI.backgroundColor = new Color(0.3f, 0.8f, 0.4f);
        if (GUILayout.Button("➕ Create Enemy On Center", buttonStyle))
        {
            populator.CreateEnemyOnPlatform();
        }

        EditorGUILayout.Space(4);

        // Obstacle Button
        GUI.backgroundColor = new Color(0.9f, 0.6f, 0.2f);
        if (GUILayout.Button("📦 Create Obstacle On Center", buttonStyle))
        {
            populator.CreateObstacleOnPlatform();
        }

        EditorGUILayout.Space(4);

        // Trampoline Button
        GUI.backgroundColor = new Color(0.2f, 0.6f, 0.9f);
        if (GUILayout.Button("🌀 Create Trampoline On Center", buttonStyle))
        {
            populator.CreateTrampolineOnPlatform();
        }

        EditorGUILayout.Space(15);
        EditorGUILayout.LabelField("Random Scatter Controls", EditorStyles.boldLabel);

        // Scatter Button
        GUI.backgroundColor = new Color(0.7f, 0.4f, 0.9f);
        if (GUILayout.Button("🎲 Scatter Random Objects", buttonStyle))
        {
            populator.ScatterRandomObjects();
        }

        EditorGUILayout.Space(6);

        // Clear All Button
        GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f);
        if (GUILayout.Button("🗑️ Clear All Spawned Attachments", buttonStyle))
        {
            populator.ClearAllSpawnedAttachments();
        }

        GUI.backgroundColor = Color.white;
    }
}