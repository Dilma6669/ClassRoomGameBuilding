using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(EnemyPlatformLogic))]
[CanEditMultipleObjects]
public class EnemyPlatformLogicEditor : Editor
{
    private SerializedProperty movementTypeProp;
    private SerializedProperty moveSpeedProp;
    private SerializedProperty moveDistanceProp;
    private SerializedProperty rotationAngleProp;
    private SerializedProperty wanderRadiusProp;

    // Bounce Properties
    private SerializedProperty isBouncyProp;
    private SerializedProperty triggerRadiusProp;
    private SerializedProperty launchForceProp;
    private SerializedProperty upwardBiasProp;
    private SerializedProperty momentumTransferProp;

    private void OnEnable()
    {
        movementTypeProp = serializedObject.FindProperty("movementType");
        moveSpeedProp = serializedObject.FindProperty("moveSpeed");
        moveDistanceProp = serializedObject.FindProperty("moveDistance");
        rotationAngleProp = serializedObject.FindProperty("rotationAngle");
        wanderRadiusProp = serializedObject.FindProperty("wanderRadius");

        isBouncyProp = serializedObject.FindProperty("isBouncy");
        triggerRadiusProp = serializedObject.FindProperty("triggerRadius");
        launchForceProp = serializedObject.FindProperty("launchForce");
        upwardBiasProp = serializedObject.FindProperty("upwardBias");
        momentumTransferProp = serializedObject.FindProperty("momentumTransfer");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // 1. Movement Mode Selection
        EditorGUILayout.PropertyField(movementTypeProp);
        EditorGUILayout.Space(5);

        // 2. Movement Speed (Hidden in Idle)
        EnemyPlatformLogic.MovementType currentMode = (EnemyPlatformLogic.MovementType)movementTypeProp.enumValueIndex;
        if (currentMode != EnemyPlatformLogic.MovementType.Idle)
        {
            EditorGUILayout.PropertyField(moveSpeedProp);
        }

        // 3. Contextual Movement Settings
        switch (currentMode)
        {
            case EnemyPlatformLogic.MovementType.Idle:
                EditorGUILayout.PropertyField(rotationAngleProp);
                break;

            case EnemyPlatformLogic.MovementType.Patrol:
                EditorGUILayout.PropertyField(moveDistanceProp);
                break;

            case EnemyPlatformLogic.MovementType.RandomWander:
                EditorGUILayout.PropertyField(wanderRadiusProp);
                break;
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Bounce / Trampoline", EditorStyles.boldLabel);

        // 4. Draw Bounce Toggle & Conditional Launch Physics
        EditorGUILayout.PropertyField(isBouncyProp);

        if (isBouncyProp.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(triggerRadiusProp);
            EditorGUILayout.PropertyField(launchForceProp);
            EditorGUILayout.PropertyField(upwardBiasProp);
            EditorGUILayout.PropertyField(momentumTransferProp);
            EditorGUI.indentLevel--;
        }

        serializedObject.ApplyModifiedProperties();
    }
}