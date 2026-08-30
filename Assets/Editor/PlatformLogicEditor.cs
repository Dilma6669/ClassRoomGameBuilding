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

        Vector3 currentSize = new Vector3(platform.widthOffset, platform.heightOffset, platform.depthOffset);
        Vector3 newSize = Handles.ScaleHandle(
            currentSize, 
            t.position, 
            t.rotation, 
            HandleUtility.GetHandleSize(t.position) * 1.2f
        );

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(platform, "Resize Platform");
            
            platform.widthOffset = Mathf.Max(0.5f, newSize.x);
            platform.heightOffset = Mathf.Max(0.2f, newSize.y);
            platform.depthOffset = Mathf.Max(0.5f, newSize.z);
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
    }
}