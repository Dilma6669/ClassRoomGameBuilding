using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ObstacleLogic))]
[CanEditMultipleObjects]
public class ObstacleLogicEditor : Editor
{
    private SerializedProperty movementSpaceProp;
    private SerializedProperty movementTypeProp;

    private SerializedProperty payloadTypeProp;
    private SerializedProperty payloadAmountProp;
    private SerializedProperty buffDurationProp;
    private SerializedProperty destroyOnTriggerProp;

    private SerializedProperty moveSpeedProp;
    private SerializedProperty moveDistanceProp;
    private SerializedProperty minWanderRadiusProp;
    private SerializedProperty maxWanderRadiusProp;
    private SerializedProperty rotationAngleProp;

    private SerializedProperty isBouncyProp;
    private SerializedProperty triggerRadiusProp;
    private SerializedProperty launchForceProp;
    private SerializedProperty upwardBiasProp;
    private SerializedProperty momentumTransferProp;

    private void OnEnable()
    {
        movementSpaceProp = serializedObject.FindProperty("movementSpace");
        movementTypeProp = serializedObject.FindProperty("movementType");

        payloadTypeProp = serializedObject.FindProperty("payloadType");
        payloadAmountProp = serializedObject.FindProperty("payloadAmount");
        buffDurationProp = serializedObject.FindProperty("buffDuration");
        destroyOnTriggerProp = serializedObject.FindProperty("destroyOnTrigger");

        moveSpeedProp = serializedObject.FindProperty("moveSpeed");
        moveDistanceProp = serializedObject.FindProperty("moveDistance");
        minWanderRadiusProp = serializedObject.FindProperty("minWanderRadius");
        maxWanderRadiusProp = serializedObject.FindProperty("maxWanderRadius");
        rotationAngleProp = serializedObject.FindProperty("rotationAngle");

        isBouncyProp = serializedObject.FindProperty("isBouncy");
        triggerRadiusProp = serializedObject.FindProperty("triggerRadius");
        launchForceProp = serializedObject.FindProperty("launchForce");
        upwardBiasProp = serializedObject.FindProperty("upwardBias");
        momentumTransferProp = serializedObject.FindProperty("momentumTransfer");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // 1. Movement Settings
        EditorGUILayout.LabelField("Environment & Movement Space", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(movementSpaceProp);
        EditorGUILayout.PropertyField(movementTypeProp);

        EditorGUILayout.Space(5);

        // 2. Dynamic Payload Settings
        EditorGUILayout.LabelField("Payload Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(payloadTypeProp);

        ObstacleLogic.PayloadType payloadType = (ObstacleLogic.PayloadType)payloadTypeProp.enumValueIndex;

        switch (payloadType)
        {
            case ObstacleLogic.PayloadType.Damage:
            case ObstacleLogic.PayloadType.HealthBooster:
            case ObstacleLogic.PayloadType.StaminaBooster:
                EditorGUILayout.PropertyField(payloadAmountProp, new GUIContent("Amount"));
                EditorGUILayout.PropertyField(destroyOnTriggerProp);
                break;
            case ObstacleLogic.PayloadType.SprintBuff:
                EditorGUILayout.PropertyField(payloadAmountProp, new GUIContent("Sprint Multiplier"));
                EditorGUILayout.PropertyField(buffDurationProp);
                EditorGUILayout.PropertyField(destroyOnTriggerProp);
                break;

            case ObstacleLogic.PayloadType.JumpBuff:
                EditorGUILayout.PropertyField(payloadAmountProp, new GUIContent("Jump Multiplier"));
                EditorGUILayout.PropertyField(buffDurationProp);
                EditorGUILayout.PropertyField(destroyOnTriggerProp);
                break;

            case ObstacleLogic.PayloadType.InvincibilityBuff:
                EditorGUILayout.PropertyField(buffDurationProp);
                EditorGUILayout.PropertyField(destroyOnTriggerProp);
                break;

            case ObstacleLogic.PayloadType.None:
                // Hide all payload fields when set to None
                break;
        }

        EditorGUILayout.Space(5);

        // 3. Movement Controls
        EditorGUILayout.LabelField("Movement Parameters", EditorStyles.boldLabel);
        ObstacleLogic.MovementType movementType = (ObstacleLogic.MovementType)movementTypeProp.enumValueIndex;

        if (movementType != ObstacleLogic.MovementType.Static)
        {
            EditorGUILayout.PropertyField(moveSpeedProp);
        }

        if (movementType == ObstacleLogic.MovementType.Patrol)
        {
            EditorGUILayout.PropertyField(moveDistanceProp);
        }
        else if (movementType == ObstacleLogic.MovementType.RandomWander)
        {
            EditorGUILayout.PropertyField(minWanderRadiusProp);
            EditorGUILayout.PropertyField(maxWanderRadiusProp);
        }
        else if (movementType == ObstacleLogic.MovementType.Static)
        {
            EditorGUILayout.PropertyField(rotationAngleProp);
        }

        EditorGUILayout.Space(5);

        // 4. Bounce Settings
        EditorGUILayout.LabelField("Bounce / Trampoline Settings", EditorStyles.boldLabel);
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