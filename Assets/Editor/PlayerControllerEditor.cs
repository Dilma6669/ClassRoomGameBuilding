using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(CharacterController))]
public class CharacterControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        CharacterController controller = (CharacterController)target;

        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 15,
            fontStyle = FontStyle.Bold,
            fixedHeight = 45
        };

        EditorGUILayout.Space(15);

        // Big Green Create Character Button
        GUI.backgroundColor = new Color(0.2f, 0.8f, 0.3f); // Bright green
        if (GUILayout.Button("🧙‍♂️ Create Character", buttonStyle))
        {
            controller.CreateCharacterMesh();
        }

        GUI.backgroundColor = Color.white;
    }
}