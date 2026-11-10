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
        GUI.backgroundColor = new Color(0.4f, 0.75f, 1f); // Blue = open/use (green is for create)
        if (GUILayout.Button("⚙️ Game Settings & Library", buttonStyle))
        {
            controller.SetupProject();
        }

        EditorGUILayout.Space(6);
        if (GUILayout.Button("🚩 Create Checkpoint", buttonStyle))
        {
            GameMenuItems.CreateCheckpoint();
        }

        GUI.backgroundColor = Color.white;
    }
}