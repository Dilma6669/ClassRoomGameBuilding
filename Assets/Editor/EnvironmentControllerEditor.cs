using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(EnvironmentController))]
public class EnvironmentControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EnvironmentController controller = (EnvironmentController)target;

        EditorGUILayout.Space(15);

        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 15,
            fontStyle = FontStyle.Bold,
            fixedHeight = 45
        };

        // Big Green Create Terrain Button
        GUI.backgroundColor = new Color(0.2f, 0.8f, 0.3f); // Bright green tint
        if (GUILayout.Button("⛰️ Create Terrain", buttonStyle))
        {
            controller.CreateTerrain();
        }

        GUI.backgroundColor = Color.white; // Reset color
    }
}