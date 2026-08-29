using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TriggerLogic))]
public class TriggerLogicEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // 1. Transform settings at the top
        EditorGUILayout.PropertyField(serializedObject.FindProperty("size"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("rotationY"));
        
        EditorGUILayout.PropertyField(serializedObject.FindProperty("offsetX"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("offsetY"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("offsetZ"));

        EditorGUILayout.Space(10);

        // 2. Message settings (conditional)
        SerializedProperty displayTextProp = serializedObject.FindProperty("displayText");
        EditorGUILayout.PropertyField(displayTextProp, new GUIContent("Display Text"));

        if (displayTextProp.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("message"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("clearOnExit"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("fadeDuration"), new GUIContent("Fade Duration (s)"));
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(10);

        // 3. Scene Transition settings (conditional)
        SerializedProperty changeSceneProp = serializedObject.FindProperty("changeScene");
        EditorGUILayout.PropertyField(changeSceneProp, new GUIContent("Change Scene"));

        if (changeSceneProp.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("sceneToLoad"), new GUIContent("Scene Name", "Exact name of the scene to load in Build Settings."));
            EditorGUI.indentLevel--;
        }

        serializedObject.ApplyModifiedProperties();

        GUILayout.Space(15);

        // Delete Button
        GUI.backgroundColor = new Color(1f, 0.3f, 0.3f);
        if (GUILayout.Button("🗑️ Delete Trigger", GUILayout.Height(30)))
        {
            TriggerLogic trigger = (TriggerLogic)target;
            trigger.DeleteTrigger();
        }
        GUI.backgroundColor = Color.white;
    }
}