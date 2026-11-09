using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SceneryPopulator))]
public class SceneryPopulatorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        SceneryPopulator scenery = (SceneryPopulator)target;

        EditorGUILayout.HelpBox(
            "🌲 Scenery: trees, rocks, bushes...\n" +
            "1. Drag models from Assets/MyModels into Scenery Models (you can add several).\n" +
            "2. Click Scatter to spread them over the terrain, or Place One to put one where the Scene view is looking.\n" +
            "Tip: you can also drag a model straight from the Project window into the Scene.",
            MessageType.Info);

        DrawDefaultInspector();

        EditorGUILayout.Space(10);
        GUIStyle big = new GUIStyle(GUI.skin.button) { fontSize = 14, fontStyle = FontStyle.Bold, fixedHeight = 40 };
        bool hasModels = scenery.sceneryModels.Exists(m => m != null);

        using (new EditorGUI.DisabledScope(!hasModels))
        {
            GUI.backgroundColor = new Color(0.2f, 0.8f, 0.3f);
            if (GUILayout.Button("🌲 Scatter " + scenery.howMany + " Across the Terrain", big))
            {
                int placed = scenery.Scatter();
                Debug.Log($"🌲 Scattered {placed} scenery objects.");
            }

            GUI.backgroundColor = new Color(0.2f, 0.6f, 0.9f);
            if (GUILayout.Button("📍 Place One (where the Scene view is looking)", GUILayout.Height(32)))
            {
                SceneView view = SceneView.lastActiveSceneView;
                Vector3 point = view != null ? view.pivot : scenery.transform.position;
                scenery.PlaceOne(point);
            }
        }

        GUI.backgroundColor = new Color(0.9f, 0.4f, 0.4f);
        if (GUILayout.Button("🗑️ Clear All Scenery", GUILayout.Height(28)))
        {
            if (EditorUtility.DisplayDialog("Clear scenery?", "Remove ALL scattered scenery from this terrain? (You can undo with Ctrl+Z.)", "Clear", "Cancel"))
            {
                Debug.Log($"🗑️ Removed {scenery.Clear()} scenery objects.");
            }
        }
        GUI.backgroundColor = Color.white;

        if (!hasModels)
        {
            EditorGUILayout.HelpBox("Add at least one model to Scenery Models to start.", MessageType.None);
        }
    }
}
