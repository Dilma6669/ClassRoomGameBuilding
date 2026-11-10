using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PlatformController))]
public class PlatformControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Hide the prefab reference fields from the kids in the Inspector
        serializedObject.Update();
        
        SerializedProperty customMeshProp = serializedObject.FindProperty("customMesh");
        SerializedProperty customMaterialProp = serializedObject.FindProperty("customMaterial");

        EditorGUILayout.PropertyField(customMeshProp);
        EditorGUILayout.PropertyField(customMaterialProp);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("customModel"), new GUIContent("🪨 Platform Model (3D model)"));

        serializedObject.ApplyModifiedProperties();

        PlatformController controller = (PlatformController)target;

        if (controller.customModel != null)
        {
            EditorGUILayout.HelpBox("New platforms will use '" + controller.customModel.name + "'.", MessageType.None);
        }
        if (GUILayout.Button(controller.customModel != null
                ? "🔁 Use '" + controller.customModel.name + "' on ALL platforms in this level"
                : "🔁 Put ALL platforms back to plain cylinders"))
        {
            controller.ApplyModelToAllPlatforms();
        }

        // The Game Builder's Platforms tab has its own Quick platform buttons, so these only show in Unity's Inspector
        if (GameBuilderWindow.DrawingInGameBuilder) return;

        EditorGUILayout.Space(15);

        GUIStyle setupButtonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            fixedHeight = 40
        };

        GUIStyle createButtonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            fixedHeight = 35
        };

        // Big Green Setup Platforms Button
        GUI.backgroundColor = new Color(0.2f, 0.8f, 0.3f); // Green tint
        if (GUILayout.Button("🛠️ Setup Platforms", setupButtonStyle))
        {
            controller.SetupPlatforms();
        }

        EditorGUILayout.Space(8);

        // Create Platform Button
        GUI.backgroundColor = new Color(0.2f, 0.8f, 0.3f); // Green = create
        if (GUILayout.Button("🏗️ Create New Platform", createButtonStyle))
        {
            controller.CreatePlatform();
        }

        GUI.backgroundColor = Color.white; // Reset color
    }
}