using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
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

        GUI.backgroundColor = new Color(0.2f, 0.8f, 0.8f);
        if (GUILayout.Button("📋 Duplicate Platform Setup", buttonStyle))
        {
            populator.DuplicatePlatform();
        }

        EditorGUILayout.Space(4);

        GUI.backgroundColor = new Color(0.9f, 0.2f, 0.2f);
        if (GUILayout.Button("❌ Delete Platform Setup", buttonStyle))
        {
            populator.DeletePlatform();
            return;
        }

        EditorGUILayout.Space(15);
        EditorGUILayout.LabelField("Single Attachment Spawning", EditorStyles.boldLabel);

        GUI.backgroundColor = new Color(0.9f, 0.6f, 0.2f);
        if (GUILayout.Button("📦 Create Obstacle On Center", buttonStyle))
        {
            populator.CreateObstacleOnPlatform();
        }

        EditorGUILayout.Space(15);
        EditorGUILayout.LabelField("Random Scatter Controls", EditorStyles.boldLabel);

        GUI.backgroundColor = new Color(0.7f, 0.4f, 0.9f);
        if (GUILayout.Button("🎲 Scatter Random Objects", buttonStyle))
        {
            populator.ScatterRandomObjects();
        }

        EditorGUILayout.Space(6);

        GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f);
        if (GUILayout.Button("🗑️ Clear All Spawned Attachments", buttonStyle))
        {
            populator.ClearAllSpawnedAttachments();
        }

        GUI.backgroundColor = Color.white;
    }
}
#endif