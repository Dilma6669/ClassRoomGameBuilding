using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>Shared Inspector bits for the scatter tools.</summary>
public static class ScatterGUI
{
    public static GUIStyle Big => new GUIStyle(GUI.skin.button) { fontSize = 13, fontStyle = FontStyle.Bold, fixedHeight = 34 };

    /// <summary>Draws a min-max pair as two number boxes, e.g. "Size  from [1] to [3]".</summary>
    public static void Range(SerializedProperty prop, string label, float min, float max)
    {
        Vector2 v = prop.vector2Value;
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.PrefixLabel(new GUIContent(label, prop.tooltip));
            float oldWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 36;
            float a = EditorGUILayout.FloatField("from", v.x);
            EditorGUIUtility.labelWidth = 20;
            float b = EditorGUILayout.FloatField("to", v.y);
            EditorGUIUtility.labelWidth = oldWidth;
            a = Mathf.Clamp(a, min, max);
            b = Mathf.Clamp(b, min, max);
            if (a != v.x || b != v.y) prop.vector2Value = new Vector2(a, b);
        }
    }

    /// <summary>The "Where can they go?" settings, including tick boxes for each painted ground type.</summary>
    public static void Place(SerializedProperty place, bool forCreatures)
    {
        EditorGUILayout.LabelField("📍 Where", EditorStyles.boldLabel);
        SerializedProperty area = place.FindPropertyRelative("area");
        area.enumValueIndex = EditorGUILayout.Popup("Area", area.enumValueIndex, new[] { "The whole level", "Around where the Scene view looks" });
        if (area.enumValueIndex == (int)ScatterArea.AroundSceneView)
            EditorGUILayout.PropertyField(place.FindPropertyRelative("areaSize"), new GUIContent("Area size (metres)"));
        EditorGUILayout.PropertyField(place.FindPropertyRelative("edgePadding"), new GUIContent("Stay away from map edge"));
        EditorGUILayout.PropertyField(place.FindPropertyRelative("maxSlope"), new GUIContent("Steepest ground allowed"));
        EditorGUILayout.PropertyField(place.FindPropertyRelative("spacing"), new GUIContent("Space between them"));
        EditorGUILayout.PropertyField(place.FindPropertyRelative("keepAwayFromStart"), new GUIContent("Away from player start"));

        Terrain terrain = ScatterTools.FindTerrain();
        TerrainLayer[] layers = terrain != null && terrain.terrainData != null ? terrain.terrainData.terrainLayers : new TerrainLayer[0];
        if (layers.Length > 1)
        {
            SerializedProperty only = place.FindPropertyRelative("onlyOnGround");
            EditorGUILayout.LabelField(new GUIContent("Only on ground painted with:", "Tick the ground types to use. None ticked = any ground."));
            EditorGUI.indentLevel++;
            foreach (TerrainLayer layer in layers)
            {
                if (layer == null) continue;
                int index = -1;
                for (int i = 0; i < only.arraySize; i++) if (only.GetArrayElementAtIndex(i).objectReferenceValue == layer) index = i;
                string name = layer.diffuseTexture != null ? layer.diffuseTexture.name : layer.name;
                bool on = EditorGUILayout.ToggleLeft(name, index >= 0);
                if (on && index < 0)
                {
                    only.arraySize++;
                    only.GetArrayElementAtIndex(only.arraySize - 1).objectReferenceValue = layer;
                }
                else if (!on && index >= 0)
                {
                    only.GetArrayElementAtIndex(index).objectReferenceValue = null;
                    only.DeleteArrayElementAtIndex(index);
                }
            }
            if (only.arraySize == 0) EditorGUILayout.LabelField("(none ticked = any ground)", EditorStyles.miniLabel);
            EditorGUI.indentLevel--;
        }
    }
}

[CustomEditor(typeof(PlatformScatter))]
public class PlatformScatterEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        PlatformScatter s = (PlatformScatter)target;

        EditorGUILayout.PropertyField(serializedObject.FindProperty("howMany"), new GUIContent("How many"));
        ScatterGUI.Range(serializedObject.FindProperty("sizeRange"), "Size (metres)", 0.5f, 50f);
        ScatterGUI.Range(serializedObject.FindProperty("thicknessRange"), "Thickness", 0.2f, 10f);
        ScatterGUI.Range(serializedObject.FindProperty("heightRange"), "Height above ground", 0f, 100f);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("randomTurn"), new GUIContent("Random turn"));

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("🎢 Movement (each one picks from the ticked ones)", EditorStyles.boldLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            Tick("still", "Still");
            Tick("sideToSide", "Side to side");
            Tick("upAndDown", "Up & down");
        }
        using (new EditorGUILayout.HorizontalScope())
        {
            Tick("spinning", "Spinning");
            Tick("growing", "Growing");
            GUILayout.FlexibleSpace();
        }
        if (s.sideToSide || s.upAndDown) ScatterGUI.Range(serializedObject.FindProperty("moveDistanceRange"), "How far they move", 0.5f, 100f);
        if (s.sideToSide || s.upAndDown || s.spinning || s.growing) ScatterGUI.Range(serializedObject.FindProperty("speedRange"), "Speed", 0.1f, 20f);

        EditorGUILayout.Space(6);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("models"), new GUIContent("🪨 Models (random pick)", "Leave empty for the normal platform look."), true);

        EditorGUILayout.Space(6);
        ScatterGUI.Place(serializedObject.FindProperty("place"), false);
        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space(8);
        PlatformController controller = s.GetComponent<PlatformController>();
        using (new EditorGUILayout.HorizontalScope())
        {
            GUI.backgroundColor = new Color(0.2f, 0.8f, 0.3f);
            if (GUILayout.Button("🎲 Scatter " + s.howMany + " platforms", ScatterGUI.Big) && controller != null)
            {
                int made = ScatterTools.ScatterPlatforms(controller, s);
                Debug.Log(made < s.howMany
                    ? $"🎲 Scattered {made} of {s.howMany} platforms (there wasn't room for more: try a bigger area, less spacing, or a wider height range)."
                    : $"🎲 Scattered {made} platforms.");
                GUIUtility.ExitGUI();
            }
            GUI.backgroundColor = new Color(0.9f, 0.4f, 0.4f);
            int count = ScatterTools.Count(ScatterTools.PlatformGroup);
            using (new EditorGUI.DisabledScope(count == 0))
            {
                if (GUILayout.Button("🗑 Clear scattered (" + count + ")", ScatterGUI.Big, GUILayout.Width(190)))
                {
                    Debug.Log($"🗑 Removed {ScatterTools.Clear(ScatterTools.PlatformGroup)} scattered platforms (and anything on them).");
                    GUIUtility.ExitGUI();
                }
            }
            GUI.backgroundColor = Color.white;
        }
        EditorGUILayout.HelpBox("Clear only removes platforms made by Scatter, not ones you made yourself. Ctrl+Z undoes a whole scatter.", MessageType.None);
    }

    private void Tick(string property, string label)
    {
        SerializedProperty p = serializedObject.FindProperty(property);
        p.boolValue = EditorGUILayout.ToggleLeft(label, p.boolValue, GUILayout.Width(110));
    }
}

[CustomEditor(typeof(CreatureScatter))]
public class CreatureScatterEditor : Editor
{
    private static readonly string[] KindLabels = System.Enum.GetValues(typeof(CreatureScatter.Kind)).Cast<CreatureScatter.Kind>().Select(ScatterTools.KindLabel).ToArray();

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        CreatureScatter s = (CreatureScatter)target;
        SerializedProperty groups = serializedObject.FindProperty("groups");
        int removeIndex = -1;
        CreatureScatter.Group scatterNow = null, clearNow = null;

        for (int i = 0; i < groups.arraySize; i++)
        {
            SerializedProperty g = groups.GetArrayElementAtIndex(i);
            CreatureScatter.Group group = s.groups[i];
            CreatureScatter.Kind kind = (CreatureScatter.Kind)g.FindPropertyRelative("kind").enumValueIndex;

            EditorGUILayout.Space(4);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            using (new EditorGUILayout.HorizontalScope())
            {
                g.isExpanded = EditorGUILayout.Foldout(g.isExpanded, "  " + group.name + "  (" + group.howMany + ")", true,
                    new GUIStyle(EditorStyles.foldout) { fontStyle = FontStyle.Bold, fontSize = 13 });
                EditorGUILayout.PropertyField(g.FindPropertyRelative("include"), GUIContent.none, GUILayout.Width(18));
                if (GUILayout.Button("✕", GUILayout.Width(24))) removeIndex = i;
            }

            if (g.isExpanded)
            {
                EditorGUILayout.PropertyField(g.FindPropertyRelative("name"));
                SerializedProperty kindProp = g.FindPropertyRelative("kind");
                kindProp.enumValueIndex = EditorGUILayout.Popup("What", kindProp.enumValueIndex, KindLabels);
                EditorGUILayout.PropertyField(g.FindPropertyRelative("howMany"), new GUIContent("How many"));
                ScatterGUI.Range(g.FindPropertyRelative("sizeRange"), "Size (1 = normal)", 0.1f, 20f);
                if (ScatterTools.Moves(kind))
                {
                    ScatterGUI.Range(g.FindPropertyRelative("speedRange"), "Speed", 0.1f, 50f);
                    ScatterGUI.Range(g.FindPropertyRelative("roamRange"), kind == CreatureScatter.Kind.Wanderer ? "How far they roam" : "How far they walk", 0.5f, 100f);
                }
                EditorGUILayout.PropertyField(g.FindPropertyRelative("models"), new GUIContent("🎨 Models (random pick)", "Leave empty for the normal look."), true);

                EditorGUILayout.LabelField("Can go on:");
                using (new EditorGUILayout.HorizontalScope())
                {
                    Tick(g, "onGround", "The ground");
                    Tick(g, "onStillPlatforms", "Still platforms");
                    Tick(g, "onMovingPlatforms", "Moving platforms");
                }
                if (!group.onGround && !group.onStillPlatforms && !group.onMovingPlatforms)
                    EditorGUILayout.HelpBox("Tick at least one place for them to go.", MessageType.Warning);

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUI.backgroundColor = new Color(0.2f, 0.8f, 0.3f);
                    if (GUILayout.Button("🎲 Scatter " + group.howMany)) scatterNow = group;
                    GUI.backgroundColor = new Color(0.9f, 0.4f, 0.4f);
                    if (GUILayout.Button("🗑 Clear (" + ScatterTools.Count(ScatterTools.CreatureGroupId(group)) + ")", GUILayout.Width(110))) clearNow = group;
                    GUI.backgroundColor = Color.white;
                }
            }
            EditorGUILayout.EndVertical();
        }

        if (removeIndex >= 0 && EditorUtility.DisplayDialog("Remove group?", "Remove the group '" + s.groups[removeIndex].name + "' from the list? (Things already scattered stay.)", "Remove", "Cancel"))
        {
            groups.DeleteArrayElementAtIndex(removeIndex);
        }

        if (GUILayout.Button("+ Add Group"))
        {
            groups.arraySize++;
            SerializedProperty g = groups.GetArrayElementAtIndex(groups.arraySize - 1);
            g.FindPropertyRelative("name").stringValue = "New Group " + groups.arraySize;
            g.FindPropertyRelative("include").boolValue = true;
            g.FindPropertyRelative("models").arraySize = 0;
            g.isExpanded = true;
        }

        EditorGUILayout.Space(8);
        ScatterGUI.Place(serializedObject.FindProperty("place"), true);
        serializedObject.ApplyModifiedProperties();

        if (scatterNow != null)
        {
            int made = ScatterTools.ScatterCreatures(s, scatterNow);
            Report(made, scatterNow.howMany, scatterNow.name);
            GUIUtility.ExitGUI();
        }
        if (clearNow != null)
        {
            Debug.Log($"🗑 Removed {ScatterTools.Clear(ScatterTools.CreatureGroupId(clearNow))} '{clearNow.name}'.");
            GUIUtility.ExitGUI();
        }

        EditorGUILayout.Space(8);
        using (new EditorGUILayout.HorizontalScope())
        {
            GUI.backgroundColor = new Color(0.2f, 0.8f, 0.3f);
            if (GUILayout.Button("🎲 Scatter All Ticked Groups", ScatterGUI.Big))
            {
                int wanted = s.groups.Where(x => x.include).Sum(x => x.howMany);
                Report(ScatterTools.ScatterAllCreatures(s), wanted, "creatures & pickups");
                GUIUtility.ExitGUI();
            }
            GUI.backgroundColor = new Color(0.9f, 0.4f, 0.4f);
            if (GUILayout.Button("🗑 Clear All Scattered", ScatterGUI.Big, GUILayout.Width(190)))
            {
                Debug.Log($"🗑 Removed {ScatterTools.Clear(null)} scattered creatures & pickups.");
                GUIUtility.ExitGUI();
            }
            GUI.backgroundColor = Color.white;
        }
        EditorGUILayout.HelpBox("Clear only removes things made by Scatter, not ones you placed yourself. Ctrl+Z undoes a whole scatter.", MessageType.None);
    }

    private static void Report(int made, int wanted, string what)
    {
        Debug.Log(made < wanted
            ? $"🎲 Scattered {made} of {wanted} {what} (there wasn't room for more: try less spacing, a bigger area, or more places they can go)."
            : $"🎲 Scattered {made} {what}.");
    }

    private static void Tick(SerializedProperty g, string property, string label)
    {
        SerializedProperty p = g.FindPropertyRelative(property);
        p.boolValue = EditorGUILayout.ToggleLeft(label, p.boolValue, GUILayout.Width(125));
    }
}
