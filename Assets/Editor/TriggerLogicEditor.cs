using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

[CustomEditor(typeof(TriggerLogic))]
public class TriggerLogicEditor : Editor
{
    private bool confirmClicked;
    private double savedAt = -10;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        confirmClicked = false;

        // 1. Transform settings at the top
        EditorGUILayout.PropertyField(serializedObject.FindProperty("size"));
        
        EditorGUILayout.PropertyField(serializedObject.FindProperty("offsetX"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("offsetY"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("offsetZ"));

        EditorGUILayout.Space(10);

        // 2. Message settings (conditional)
        // "Pop-up" choice: Nothing / Text message / Video (stored as displayText + popupType)
        SerializedProperty displayTextProp = serializedObject.FindProperty("displayText");
        SerializedProperty popupTypeProp = serializedObject.FindProperty("popupType");
        int popupChoice = !displayTextProp.boolValue ? 0 : (popupTypeProp.enumValueIndex == (int)TriggerLogic.PopupType.Video ? 2 : 1);
        int newChoice = EditorGUILayout.Popup(new GUIContent("Pop-up", "What happens on screen when the player walks in."),
            popupChoice, new[] { new GUIContent("Nothing"), new GUIContent("💬 Text message"), new GUIContent("🎬 Video") });
        if (newChoice != popupChoice)
        {
            displayTextProp.boolValue = newChoice != 0;
            if (newChoice != 0) popupTypeProp.enumValueIndex = newChoice == 2 ? (int)TriggerLogic.PopupType.Video : (int)TriggerLogic.PopupType.Text;
        }

        if (newChoice == 1)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("canvas"), new GUIContent("Dialog Canvas"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("message"));
            confirmClicked = DrawConfirmButton();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("clearOnExit"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("fadeDuration"), new GUIContent("Fade Duration (s)"));
            EditorGUI.indentLevel--;
        }
        else if (newChoice == 2)
        {
            EditorGUI.indentLevel++;
            SerializedProperty clipProp = serializedObject.FindProperty("videoClip");
            EditorGUILayout.PropertyField(clipProp, new GUIContent("🎬 Video"));
            if (clipProp.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("Put your video (.mp4) in the Assets/MyVideos folder, then drag it into the Video slot. Short videos work best (under a minute).", MessageType.Info);
                if (GUILayout.Button("📁 Show the MyVideos folder")) ShowVideosFolder();
            }
            EditorGUILayout.PropertyField(serializedObject.FindProperty("pauseGameWhileVideo"), new GUIContent("Full screen + pause game"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("playOnlyOnce"), new GUIContent("Play only once"));
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
        if (confirmClicked) SaveMessage();

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

    /// <summary>A button under the message box that finishes typing and saves the level.</summary>
    private bool DrawConfirmButton()
    {
        bool clicked;
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Space(EditorGUI.indentLevel * 15);
            GUI.backgroundColor = new Color(0.35f, 0.65f, 1f); // blue = "save/confirm" (green is for "create")
            clicked = GUILayout.Button(new GUIContent("✔ Confirm message", "Finish typing and save the level"), GUILayout.Height(26));
            GUI.backgroundColor = Color.white;
        }
        if (EditorApplication.timeSinceStartup - savedAt < 2.5)
        {
            EditorGUILayout.LabelField(Application.isPlaying ? "✔ Done (changes made while playing are lost when you stop)" : "✔ Saved!", EditorStyles.miniBoldLabel);
            Repaint();
        }
        if (clicked)
        {
            // Stop typing in the box, so the text is final
            GUI.FocusControl(null);
            GUIUtility.keyboardControl = 0;
            EditorGUIUtility.editingTextField = false;
        }
        return clicked;
    }

    private void SaveMessage()
    {
        savedAt = EditorApplication.timeSinceStartup;
        if (Application.isPlaying) return;
        TriggerLogic trigger = (TriggerLogic)target;
        EditorUtility.SetDirty(trigger);
        UnityEngine.SceneManagement.Scene scene = trigger.gameObject.scene;
        if (scene.IsValid())
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
        }
    }

    private static void ShowVideosFolder()
    {
        const string folder = "Assets/MyVideos";
        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets", "MyVideos");
        Object folderAsset = AssetDatabase.LoadAssetAtPath<Object>(folder);
        EditorUtility.FocusProjectWindow();
        EditorGUIUtility.PingObject(folderAsset);
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