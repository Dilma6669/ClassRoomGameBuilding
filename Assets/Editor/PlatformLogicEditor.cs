using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PlatformLogic))]
public class PlatformLogicEditor : Editor
{
    private void OnSceneGUI()
    {
        PlatformLogic platform = (PlatformLogic)target;
        
        // Grab the child transform if available, otherwise fallback to parent
        Transform t = platform.TargetChild != null ? platform.TargetChild : platform.transform;

        EditorGUI.BeginChangeCheck();

        Vector3 currentSize = new Vector3(platform.width, platform.height, platform.depth);
        Vector3 newSize = Handles.ScaleHandle(
            currentSize, 
            t.position, 
            t.rotation, 
            HandleUtility.GetHandleSize(t.position) * 1.2f
        );

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(platform, "Resize Platform");
            
            platform.width = Mathf.Max(0.5f, newSize.x);
            platform.height = Mathf.Max(0.2f, newSize.y);
            platform.depth = Mathf.Max(0.5f, newSize.z);
        }
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SerializedProperty prop = serializedObject.GetIterator();
        bool enterChildren = true;

        while (prop.NextVisible(enterChildren))
        {
            enterChildren = false;

            // Skip internal script property and custom axis toggles
            if (prop.name == "m_Script" || prop.name == "moveX" || prop.name == "moveY" || prop.name == "moveZ")
                continue;

            // Inject Movement Settings header & toggles right before initialDirection renders
            if (prop.name == "initialDirection")
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField("Movement Settings", EditorStyles.boldLabel);

                EditorGUILayout.BeginHorizontal();
                
                PlatformLogic platformTarget = (PlatformLogic)target;

                EditorGUI.BeginChangeCheck();
                bool x = EditorGUILayout.ToggleLeft("Left/Right", platformTarget.moveX, GUILayout.Width(130));
                bool y = EditorGUILayout.ToggleLeft("Up/Down", platformTarget.moveY, GUILayout.Width(130));
                bool z = EditorGUILayout.ToggleLeft("Forward/Backward", platformTarget.moveZ, GUILayout.Width(130));

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(platformTarget, "Change Movement Axis");
                    platformTarget.moveX = x;
                    platformTarget.moveY = y;
                    platformTarget.moveZ = z;
                    EditorUtility.SetDirty(platformTarget);
                }

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space(5);
            }

            EditorGUILayout.PropertyField(prop, true);
        }

        serializedObject.ApplyModifiedProperties();

        // --- BUTTON WORKFLOW ---
        EditorGUILayout.Space(15);
        
        PlatformLogic platform = (PlatformLogic)target;

        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            fixedHeight = 35
        };

        // Enemy Button
        GUI.backgroundColor = new Color(0.3f, 0.8f, 0.4f); // Friendly green tint
        if (GUILayout.Button("➕ Create Enemy On Platform", buttonStyle))
        {
            platform.CreateEnemyOnPlatform();
        }

        EditorGUILayout.Space(5);

        // Obstacle Button
        GUI.backgroundColor = new Color(0.9f, 0.6f, 0.2f); // Orange tint
        if (GUILayout.Button("📦 Create Obstacle On Platform", buttonStyle))
        {
            platform.CreateObstacleOnPlatform();
        }

        EditorGUILayout.Space(5);

        // Trampoline Button
        GUI.backgroundColor = new Color(0.2f, 0.6f, 0.9f); // Cyan/blue tint
        if (GUILayout.Button("🌀 Create Trampoline On Platform", buttonStyle))
        {
            platform.CreateTrampolineOnPlatform();
        }

        GUI.backgroundColor = Color.white; // Reset color
    }
}