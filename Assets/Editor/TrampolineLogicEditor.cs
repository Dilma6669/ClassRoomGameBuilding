using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TrampolineLogic))]
[CanEditMultipleObjects]
public class TrampolineLogicEditor : Editor
{
    private SerializedProperty isBouncyProp;
    private SerializedProperty launchForceProp;
    private SerializedProperty momentumTransferProp;

    private void OnEnable()
    {
        isBouncyProp = serializedObject.FindProperty("isBouncy");
        launchForceProp = serializedObject.FindProperty("launchForce");
        momentumTransferProp = serializedObject.FindProperty("momentumTransfer");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // 1. Draw Toggle
        EditorGUILayout.PropertyField(isBouncyProp);

        // 2. Draw Physics Sliders only when enabled
        if (isBouncyProp.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(launchForceProp);
            EditorGUILayout.PropertyField(momentumTransferProp);
            EditorGUI.indentLevel--;
        }

        serializedObject.ApplyModifiedProperties();
    }
}