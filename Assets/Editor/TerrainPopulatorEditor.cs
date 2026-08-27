using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TerrainPopulator))]
public class TerrainPopulatorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        TerrainPopulator populator = (TerrainPopulator)target;

        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            fixedHeight = 35
        };

        EditorGUILayout.Space(15);
        EditorGUILayout.LabelField("Terrain Scatter Controls", EditorStyles.boldLabel);

        // Scatter Button
        GUI.backgroundColor = new Color(0.4f, 0.7f, 1f); // Sky blue
        if (GUILayout.Button("⛰️ Scatter Objects On Terrain", buttonStyle))
        {
            populator.ScatterObjectsOnTerrain();
        }

        EditorGUILayout.Space(6);

        // Clear Button
        GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f); // Red tint
        if (GUILayout.Button("🗑️ Clear All Terrain Spawns", buttonStyle))
        {
            populator.ClearTerrainSpawns();
        }

        GUI.backgroundColor = Color.white;
    }
}