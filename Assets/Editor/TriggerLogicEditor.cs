using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

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

        // 3. Scene Transition settings (conditional dropdown)
        SerializedProperty changeSceneProp = serializedObject.FindProperty("changeScene");
        EditorGUILayout.PropertyField(changeSceneProp, new GUIContent("Change Scene"));

        if (changeSceneProp.boolValue)
        {
            EditorGUI.indentLevel++;
            
            SerializedProperty sceneToLoadProp = serializedObject.FindProperty("sceneToLoad");
            string[] sceneNames = GetBuildSceneNames();

            if (sceneNames.Length > 0)
            {
                // Find currently selected scene index in the array
                int currentIndex = System.Array.IndexOf(sceneNames, sceneToLoadProp.stringValue);
                if (currentIndex < 0) currentIndex = 0;

                int selectedIndex = EditorGUILayout.Popup("Target Scene", currentIndex, sceneNames);
                sceneToLoadProp.stringValue = sceneNames[selectedIndex];
            }
            else
            {
                EditorGUILayout.HelpBox("No active scenes found in Build Settings! Go to File > Build Settings to add scenes.", MessageType.Warning);
                EditorGUILayout.PropertyField(sceneToLoadProp, new GUIContent("Scene Name"));
            }

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

    private string[] GetBuildSceneNames()
    {
        List<string> scenes = new List<string>();

        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (scene.enabled)
            {
                string name = Path.GetFileNameWithoutExtension(scene.path);
                if (!string.IsNullOrEmpty(name))
                {
                    scenes.Add(name);
                }
            }
        }

        return scenes.ToArray();
    }
}