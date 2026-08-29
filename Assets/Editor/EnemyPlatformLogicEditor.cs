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

    private void OnEnable()
    {
        movementTypeProp = serializedObject.FindProperty("movementType");
        moveSpeedProp = serializedObject.FindProperty("moveSpeed");
        moveDistanceProp = serializedObject.FindProperty("moveDistance");
        rotationAngleProp = serializedObject.FindProperty("rotationAngle");
        wanderRadiusProp = serializedObject.FindProperty("wanderRadius");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // 1. Draw Movement Mode Dropdown
        EditorGUILayout.PropertyField(movementTypeProp);
        EditorGUILayout.Space(5);

        // 2. Draw Speed if not Idle
        EnemyPlatformLogic.MovementType currentMode = (EnemyPlatformLogic.MovementType)movementTypeProp.enumValueIndex;
        
        if (currentMode != EnemyPlatformLogic.MovementType.Idle)
        {
            EditorGUILayout.PropertyField(moveSpeedProp);
        }

        // 3. Contextual Settings display
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

        serializedObject.ApplyModifiedProperties();
    }
}