using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PlatformController))]
public class PlatformControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw standard fields (the two prefab slots)
        DrawDefaultInspector();

        PlatformController controller = (PlatformController)target;

        EditorGUILayout.Space(15);

        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            fixedHeight = 35
        };

        GUI.backgroundColor = new Color(0.2f, 0.6f, 0.9f); // Friendly blue tint

        if (GUILayout.Button("🏗️ Create New Platform", buttonStyle))
        {
            controller.CreatePlatform();
        }

        GUI.backgroundColor = Color.white; // Reset color
    }
}