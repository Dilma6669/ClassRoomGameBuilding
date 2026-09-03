using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ObstacleLogic))]
[CanEditMultipleObjects]
public class ObstacleLogicEditor : Editor
{
    private SerializedProperty objectScaleProp;

    private SerializedProperty movementSpaceProp;
    private SerializedProperty movementTypeProp;
    private SerializedProperty navMeshRadiusProp;

    private SerializedProperty payloadTypeProp;
    private SerializedProperty payloadAmountProp;
    private SerializedProperty buffDurationProp;
    private SerializedProperty destroyOnTriggerProp;

    private SerializedProperty minMoveSpeedProp;
    private SerializedProperty maxMoveSpeedProp;
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
        objectScaleProp = serializedObject.FindProperty("objectScale");

        movementSpaceProp = serializedObject.FindProperty("movementSpace");
        movementTypeProp = serializedObject.FindProperty("movementType");
        navMeshRadiusProp = serializedObject.FindProperty("navMeshRadius");

        payloadTypeProp = serializedObject.FindProperty("payloadType");
        payloadAmountProp = serializedObject.FindProperty("payloadAmount");
        buffDurationProp = serializedObject.FindProperty("buffDuration");
        destroyOnTriggerProp = serializedObject.FindProperty("destroyOnTrigger");

        minMoveSpeedProp = serializedObject.FindProperty("minMoveSpeed");
        maxMoveSpeedProp = serializedObject.FindProperty("maxMoveSpeed");
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

        // 0. Transform Settings
        EditorGUILayout.LabelField("Transform Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(objectScaleProp, new GUIContent("Uniform Scale"));

        EditorGUILayout.Space(5);

        // 1. Movement Settings
        EditorGUILayout.LabelField("Environment & Movement Space", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(movementSpaceProp);
        EditorGUILayout.PropertyField(movementTypeProp);

        ObstacleLogic.MovementSpace movementSpace = (ObstacleLogic.MovementSpace)movementSpaceProp.enumValueIndex;
        
        // Show NavMesh Radius slider unless explicitly set to MovingPlatform
        if (movementSpace != ObstacleLogic.MovementSpace.MovingPlatform)
        {
            EditorGUILayout.PropertyField(navMeshRadiusProp, new GUIContent("NavMesh Agent Radius"));
        }

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
            EditorGUILayout.PropertyField(minMoveSpeedProp, new GUIContent("Min Move Speed"));
            EditorGUILayout.PropertyField(maxMoveSpeedProp, new GUIContent("Max Move Speed"));
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