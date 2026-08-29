using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GameController))]
public class GameControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GameController controller = (GameController)target;

        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 15,
            fontStyle = FontStyle.Bold,
            fixedHeight = 45
        };

        EditorGUILayout.Space(15);

        // Big Green Setup Button
        GUI.backgroundColor = new Color(0.2f, 0.8f, 0.3f); // Bright green tint
        if (GUILayout.Button("🚀 Setup Project", buttonStyle))
        {
            controller.SetupProject();
        }

        GUI.backgroundColor = Color.white;
    }
}