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

        PlatformLogic platformTarget = (PlatformLogic)target;
        SerializedProperty prop = serializedObject.GetIterator();
        bool enterChildren = true;
        bool drewToggles = false;

        while (prop.NextVisible(enterChildren))
        {
            enterChildren = false;

            // Skip internal script property and custom toggles
            if (prop.name == "m_Script" || prop.name == "moveX" || prop.name == "moveY" || prop.name == "moveZ" || prop.name == "enableRotation")
                continue;

            // Hide rotation speed slider if enableRotation is unchecked
            if (prop.name == "rotationSpeedY" && !platformTarget.enableRotation)
                continue;

            // Hide movement properties if no movement axis is selected
            if ((prop.name == "initialDirection" || prop.name == "moveDistance" || prop.name == "moveSpeed") && !platformTarget.HasActiveAxis)
                continue;

            // Render Movement & Rotation Toggles block right before initialDirection or rotationSpeedY
            if (!drewToggles && (prop.name == "initialDirection" || prop.name == "rotationSpeedY"))
            {
                DrawToggleSection(platformTarget);
                drewToggles = true;
            }

            EditorGUILayout.PropertyField(prop, true);
        }

        // Render toggles at the bottom if neither movement nor rotation was active
        if (!drewToggles)
        {
            DrawToggleSection(platformTarget);
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawToggleSection(PlatformLogic platformTarget)
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Movement & Rotation Toggles", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        EditorGUI.BeginChangeCheck();
        bool x = EditorGUILayout.ToggleLeft("Left/Right", platformTarget.moveX, GUILayout.Width(85));
        bool y = EditorGUILayout.ToggleLeft("Up/Down", platformTarget.moveY, GUILayout.Width(80));
        bool z = EditorGUILayout.ToggleLeft("Forward/Back", platformTarget.moveZ, GUILayout.Width(100));
        bool rot = EditorGUILayout.ToggleLeft("Rotate", platformTarget.enableRotation, GUILayout.Width(90));

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(platformTarget, "Change Platform Toggles");
            platformTarget.moveX = x;
            platformTarget.moveY = y;
            platformTarget.moveZ = z;
            platformTarget.enableRotation = rot;
            EditorUtility.SetDirty(platformTarget);
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(5);
    }
}