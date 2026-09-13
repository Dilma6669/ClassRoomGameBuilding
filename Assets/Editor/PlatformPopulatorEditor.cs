#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PlatformPopulator))]
public class PlatformPopulatorEditor : Editor
{
    private SerializedProperty obstaclePrefabProp;
    private SerializedProperty customMeshProp;
    private SerializedProperty customMaterialProp;

    private SerializedProperty scatterObstacleTypeProp;
    private SerializedProperty scatterCountProp;
    private SerializedProperty randomYRotationProp;

    private SerializedProperty baseSettingsProp;
    private SerializedProperty wanderSettingsProp;
    private SerializedProperty patrolSettingsProp;

    private void OnEnable()
    {
        obstaclePrefabProp = serializedObject.FindProperty("obstaclePrefab");
        customMeshProp = serializedObject.FindProperty("customMesh");
        customMaterialProp = serializedObject.FindProperty("customMaterial");

        scatterObstacleTypeProp = serializedObject.FindProperty("scatterObstacleType");
        scatterCountProp = serializedObject.FindProperty("scatterCount");
        randomYRotationProp = serializedObject.FindProperty("randomYRotation");

        baseSettingsProp = serializedObject.FindProperty("baseSettings");
        wanderSettingsProp = serializedObject.FindProperty("wanderSettings");
        patrolSettingsProp = serializedObject.FindProperty("patrolSettings");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Single Obstacle Setup
        EditorGUILayout.LabelField("Single Attachment & Prefab Setup", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(obstaclePrefabProp);
        EditorGUILayout.PropertyField(customMeshProp);
        EditorGUILayout.PropertyField(customMaterialProp);

        EditorGUILayout.Space(10);

        // Scatter Setup Header & Dropdown
        EditorGUILayout.LabelField("Random Scatter Setup", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(scatterObstacleTypeProp);
        EditorGUILayout.PropertyField(scatterCountProp);
        EditorGUILayout.PropertyField(randomYRotationProp);

        EditorGUILayout.Space(10);

        // Shared Base Settings Section
        EditorGUILayout.LabelField("Obstacle Properties", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        EditorGUILayout.PropertyField(baseSettingsProp, true);
        EditorGUI.indentLevel--;

        // Dynamic Driver Settings depending on Dropdown selection
        ObstacleSpawnType currentType = (ObstacleSpawnType)scatterObstacleTypeProp.enumValueIndex;

        if (currentType == ObstacleSpawnType.Wander)
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Wanderer Movement Settings", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(wanderSettingsProp, true);
            EditorGUI.indentLevel--;
        }
        else if (currentType == ObstacleSpawnType.Patrol)
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Patrol Movement Settings", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(patrolSettingsProp, true);
            EditorGUI.indentLevel--;
        }

        serializedObject.ApplyModifiedProperties();

        // Custom Buttons Setup
        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            fixedHeight = 30
        };

        // 1. Obstacle Spawning Section
        EditorGUILayout.Space(15);
        EditorGUILayout.LabelField("Obstacle Spawning", EditorStyles.boldLabel);

        GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
        if (GUILayout.Button("➕ Create Single Obstacle", buttonStyle))
        {
            PlatformPopulator populator = (PlatformPopulator)target;
            populator.CreateSingleObstacle();
        }

        EditorGUILayout.Space(4);

        GUI.backgroundColor = new Color(0.7f, 0.4f, 0.9f);
        if (GUILayout.Button("🎲 Scatter Obstacles", buttonStyle))
        {
            PlatformPopulator populator = (PlatformPopulator)target;
            populator.ScatterRandomObjects();
        }

        EditorGUILayout.Space(4);

        GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f);
        if (GUILayout.Button("🗑️ Clear All Obstacles", buttonStyle))
        {
            PlatformPopulator populator = (PlatformPopulator)target;
            populator.ClearAllSpawnedAttachments();
        }

        // 2. Platform Management Section
        EditorGUILayout.Space(15);
        EditorGUILayout.LabelField("Platform Management", EditorStyles.boldLabel);

        GUI.backgroundColor = new Color(0.2f, 0.8f, 0.8f);
        if (GUILayout.Button("📋 Duplicate Platform Setup", buttonStyle))
        {
            PlatformPopulator populator = (PlatformPopulator)target;
            populator.DuplicatePlatform();
        }

        EditorGUILayout.Space(4);

        GUI.backgroundColor = new Color(0.9f, 0.2f, 0.2f);
        if (GUILayout.Button("❌ Delete Platform Setup", buttonStyle))
        {
            PlatformPopulator populator = (PlatformPopulator)target;
            populator.DeletePlatform();
            return;
        }

        GUI.backgroundColor = Color.white;
    }
}
#endif