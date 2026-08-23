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

        DrawPropertiesExcluding(serializedObject, "moveX", "moveY", "moveZ");

        PlatformLogic platform = (PlatformLogic)target;

        if (platform.enableMovement)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Movement Axis Controls", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            
            EditorGUI.BeginChangeCheck();
            bool x = EditorGUILayout.ToggleLeft("Left/Right", platform.moveX, GUILayout.Width(130));
            bool y = EditorGUILayout.ToggleLeft("Up/Down", platform.moveY, GUILayout.Width(130));
            bool z = EditorGUILayout.ToggleLeft("Forward/Backward", platform.moveZ, GUILayout.Width(130));

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(platform, "Change Movement Axis");
                platform.moveX = x;
                platform.moveY = y;
                platform.moveZ = z;
                EditorUtility.SetDirty(platform);
            }

            EditorGUILayout.EndHorizontal();
        }

        serializedObject.ApplyModifiedProperties();
    }
}