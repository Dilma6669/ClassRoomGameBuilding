#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TerrainPopulator))]
public class TerrainPopulatorEditor : Editor
{
    private SerializedProperty obstaclePrefabProp;
    private SerializedProperty customMeshProp;
    private SerializedProperty customMaterialProp;

    private SerializedProperty scatterObstacleTypeProp;
    private SerializedProperty scatterCountProp;
    private SerializedProperty edgePaddingProp;
    private SerializedProperty heightOffsetProp;
    private SerializedProperty randomYRotationProp;
    private SerializedProperty alignWithTerrainSlopeProp;

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
        edgePaddingProp = serializedObject.FindProperty("edgePadding");
        heightOffsetProp = serializedObject.FindProperty("heightOffset");
        randomYRotationProp = serializedObject.FindProperty("randomYRotation");
        alignWithTerrainSlopeProp = serializedObject.FindProperty("alignWithTerrainSlope");

        baseSettingsProp = serializedObject.FindProperty("baseSettings");
        wanderSettingsProp = serializedObject.FindProperty("wanderSettings");
        patrolSettingsProp = serializedObject.FindProperty("patrolSettings");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Single Obstacle Setup
        EditorGUILayout.LabelField("Single Obstacle Setup", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(obstaclePrefabProp);
        EditorGUILayout.PropertyField(customMeshProp);
        EditorGUILayout.PropertyField(customMaterialProp);

        EditorGUILayout.Space(10);

        // Scatter Setup Header & Dropdown
        EditorGUILayout.LabelField("Random Scatter Setup", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(scatterObstacleTypeProp);
        EditorGUILayout.PropertyField(scatterCountProp);
        EditorGUILayout.PropertyField(edgePaddingProp);
        EditorGUILayout.PropertyField(heightOffsetProp);
        EditorGUILayout.PropertyField(randomYRotationProp);

        EditorGUILayout.Space(10);

        // Shared Base Settings Section
        EditorGUILayout.LabelField("Shared Obstacle Properties (Scale, Payload, Bounce)", EditorStyles.boldLabel);
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

        // Custom Buttons
        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            fixedHeight = 30
        };

        EditorGUILayout.Space(15);
        EditorGUILayout.LabelField("Terrain Scatter Controls", EditorStyles.boldLabel);

        GUI.backgroundColor = new Color(0.7f, 0.4f, 0.9f);
        if (GUILayout.Button("🎲 Scatter Obstacles", buttonStyle))
        {
            TerrainPopulator populator = (TerrainPopulator)target;
            populator.ScatterObstacles();
        }

        EditorGUILayout.Space(15);
        EditorGUILayout.LabelField("NavMesh & Management", EditorStyles.boldLabel);

        GUI.backgroundColor = new Color(0.2f, 0.8f, 0.8f);
        if (GUILayout.Button("🧠 Bake NavMesh Surface", buttonStyle))
        {
            TerrainPopulator populator = (TerrainPopulator)target;
            populator.BakeNavMeshSurface();
        }

        EditorGUILayout.Space(6);

        GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f);
        if (GUILayout.Button("🗑️ Clear All Terrain Spawns", buttonStyle))
        {
            TerrainPopulator populator = (TerrainPopulator)target;
            populator.ClearTerrainSpawns();
        }

        GUI.backgroundColor = Color.white;
    }
}
#endif