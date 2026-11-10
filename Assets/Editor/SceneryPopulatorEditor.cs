using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SceneryPopulator))]
public class SceneryPopulatorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        SceneryPopulator scenery = (SceneryPopulator)target;

        EditorGUILayout.HelpBox(
            "🌲 Scenery: trees, rocks, grass...\n" +
            "Each group below has its own models, amount and size.\n" +
            "Fastest way: type words (e.g. Tree) and click 'Add matching models', then Scatter.\n" +
            "Tip: you can also drag any model straight from the Project window into the Scene.",
            MessageType.Info);

        SerializedProperty groups = serializedObject.FindProperty("groups");
        GUIStyle big = new GUIStyle(GUI.skin.button) { fontSize = 13, fontStyle = FontStyle.Bold, fixedHeight = 34 };
        int removeIndex = -1;

        for (int i = 0; i < groups.arraySize; i++)
        {
            SerializedProperty g = groups.GetArrayElementAtIndex(i);
            SceneryPopulator.SceneryGroup group = scenery.groups[i];

            EditorGUILayout.Space(6);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            g.isExpanded = FoldoutBar.Draw(g.isExpanded, "  " + group.name + "  (" + group.models.FindAll(m => m != null).Count + " models)");
            EditorGUILayout.PropertyField(g.FindPropertyRelative("include"), GUIContent.none, GUILayout.Width(18));
            if (GUILayout.Button("✕", GUILayout.Width(24))) removeIndex = i;
            EditorGUILayout.EndHorizontal();

            if (g.isExpanded)
            {
                EditorGUILayout.PropertyField(g.FindPropertyRelative("name"));
                EditorGUILayout.PropertyField(g.FindPropertyRelative("howMany"), new GUIContent("How Many"));
                EditorGUILayout.PropertyField(g.FindPropertyRelative("sizeRange"), new GUIContent("Size (min, max)"));
                EditorGUILayout.PropertyField(g.FindPropertyRelative("tiltWithGround"));
                EditorGUILayout.PropertyField(g.FindPropertyRelative("solid"), new GUIContent("Solid (bump into)"));
                EditorGUILayout.PropertyField(g.FindPropertyRelative("safeLanding"), new GUIContent("🪂 Landing on them never hurts"));
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PropertyField(g.FindPropertyRelative("glow"), new GUIContent("💡 Glow"));
                    if (group.glow) EditorGUILayout.PropertyField(g.FindPropertyRelative("glowColour"), GUIContent.none, GUILayout.Width(80));
                }
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PropertyField(g.FindPropertyRelative("sparkles"), new GUIContent("✨ Sparkles around them"));
                    if (group.sparkles)
                    {
                        EditorGUILayout.PropertyField(g.FindPropertyRelative("sparkleStyle"), GUIContent.none, GUILayout.Width(90));
                        EditorGUILayout.PropertyField(g.FindPropertyRelative("sparkleColour"), GUIContent.none, GUILayout.Width(80));
                    }
                }
                if (GUILayout.Button("Use these glow & sparkle settings on the " + group.name + " already placed"))
                {
                    serializedObject.ApplyModifiedProperties();
                    scenery.ApplyGlowToPlaced(group);
                    Debug.Log($"✨ Updated glow & sparkles on '{group.name}'.");
                    serializedObject.Update();
                }

                // Quick fill from Assets/MyModels by name
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(g.FindPropertyRelative("quickFillWords"), new GUIContent("Find models named"));
                if (GUILayout.Button("Add matching models", GUILayout.Width(150)))
                {
                    serializedObject.ApplyModifiedProperties();
                    Undo.RecordObject(scenery, "Add Scenery Models");
                    int added = scenery.QuickFill(group);
                    EditorUtility.SetDirty(scenery);
                    Debug.Log($"🌲 Added {added} models to '{group.name}'.");
                    serializedObject.Update();
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.PropertyField(g.FindPropertyRelative("models"), new GUIContent("Models"), true);

                bool hasModels = group.models.Exists(m => m != null);
                using (new EditorGUI.DisabledScope(!hasModels))
                {
                    EditorGUILayout.BeginHorizontal();
                    GUI.backgroundColor = new Color(0.2f, 0.8f, 0.3f);
                    if (GUILayout.Button("🌲 Scatter " + group.howMany)) Debug.Log($"🌲 Scattered {scenery.Scatter(group)} '{group.name}'.");
                    GUI.backgroundColor = new Color(0.2f, 0.8f, 0.3f);
                    bool placingThis = ScenePlacer.IsPlacing && ScenePlacer.What == group.name;
                    if (placingThis) GUI.backgroundColor = new Color(0.6f, 0.85f, 1f); // highlighted while placing
                    if (GUILayout.Button(new GUIContent(placingThis ? "📍 Placing… (Esc)" : "📍 Place One",
                            ScenePlacer.ClickToPlace ? "Then click in the Scene: one goes down with each click, until you press Esc." : "Puts one where the Scene view is looking.")))
                    {
                        if (placingThis) ScenePlacer.Cancel();
                        else if (ScenePlacer.ClickToPlace)
                        {
                            // Keep placing (a random model from the group each click) until Esc
                            SceneryPopulator target = scenery;
                            SceneryPopulator.SceneryGroup chosen = group;
                            ScenePlacer.Begin(group.name, hit => target.PlaceOneAt(chosen, hit), repeat: true);
                        }
                        else scenery.PlaceOne(group, SceneView.lastActiveSceneView != null ? SceneView.lastActiveSceneView.pivot : scenery.transform.position);
                    }
                    GUI.backgroundColor = new Color(0.9f, 0.4f, 0.4f);
                    if (GUILayout.Button("🗑️ Clear")) Debug.Log($"🗑️ Removed {scenery.Clear(group)} '{group.name}'.");
                    GUI.backgroundColor = Color.white;
                    EditorGUILayout.EndHorizontal();
                }
            }
            EditorGUILayout.EndVertical();
        }

        if (removeIndex >= 0 && EditorUtility.DisplayDialog("Remove group?", "Remove the group '" + scenery.groups[removeIndex].name + "' from the list? (Scenery already placed stays.)", "Remove", "Cancel"))
        {
            groups.DeleteArrayElementAtIndex(removeIndex);
        }

        EditorGUILayout.Space(4);
        if (GUILayout.Button("+ Add Group"))
        {
            groups.arraySize++;
            SerializedProperty g = groups.GetArrayElementAtIndex(groups.arraySize - 1);
            g.FindPropertyRelative("name").stringValue = "New Group";
            g.FindPropertyRelative("models").arraySize = 0;
            g.FindPropertyRelative("include").boolValue = true;
            g.isExpanded = true;
        }

        EditorGUILayout.Space(8);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("randomTurn"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("maxSlope"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("edgePadding"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("sinkIntoGround"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("placeOnPlatforms"));
        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space(10);
        GUI.backgroundColor = new Color(0.2f, 0.8f, 0.3f);
        if (GUILayout.Button("🌲 Scatter All Ticked Groups", big))
        {
            Debug.Log($"🌲 Scattered {scenery.ScatterAll()} scenery objects.");
        }
        GUI.backgroundColor = new Color(0.9f, 0.4f, 0.4f);
        if (GUILayout.Button("🗑️ Clear All Scenery", GUILayout.Height(28)) &&
            EditorUtility.DisplayDialog("Clear scenery?", "Remove ALL scattered scenery from this terrain? (You can undo with Ctrl+Z.)", "Clear", "Cancel"))
        {
            Debug.Log($"🗑️ Removed {scenery.Clear()} scenery objects.");
        }
        GUI.backgroundColor = Color.white;
    }
}
