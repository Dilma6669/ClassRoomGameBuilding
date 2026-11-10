using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Creature & pickup Inspector: the normal settings, but hides ones that don't apply
/// (jump-on settings on pickups, bounce settings when not bouncy, sparkle options when sparkles are off),
/// then the 🎯 Hit zone and 🏃 Chase sections.
/// </summary>
[CustomEditor(typeof(ObstacleBase), true)]
[CanEditMultipleObjects]
public class ObstacleBaseEditor : Editor
{
    private static readonly HashSet<string> DrawnLater = new HashSet<string>
    {
        "fitHitZoneToModel", "hitZoneWidth", "hitZoneHeight", "touchDistance",
        "chasePlayer", "chaseStartDistance", "chaseGiveUpDistance", "chaseSpeed",
        "triggerRadius", // not used: the hit zone is what touches the player
    };

    private static readonly Dictionary<string, GUIContent> Labels = new Dictionary<string, GUIContent>
    {
        { "destroyOnTrigger", new GUIContent("Disappears after touching", "Ticked: it vanishes once the player touches it (a pickup is used up; a creature hurts once, then disappears).") },
        { "sparkles", new GUIContent("Sparkle Burst", "A burst of sparkles when it's picked up or beaten.") },
        { "jumpsToDefeat", new GUIContent("Jumps On Head To Beat", "How many times the player must land on its head to beat it. 0 = it can't be beaten.") },
    };

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        ObstacleBase o = (ObstacleBase)target;
        bool many = targets.Length > 1; // with several selected, show everything

        SerializedProperty prop = serializedObject.GetIterator();
        bool enterChildren = true;
        while (prop.NextVisible(enterChildren))
        {
            enterChildren = false;
            if (prop.name == "m_Script" || DrawnLater.Contains(prop.name)) continue;

            if (!many)
            {
                if ((prop.name == "jumpsToDefeat" || prop.name == "stompBounce") && o.payloadType != ObstacleBase.PayloadType.Damage) continue;
                if ((prop.name == "launchForce" || prop.name == "upwardBias" || prop.name == "momentumTransfer") && !o.isBouncy) continue;
                if ((prop.name == "sparkleStyle" || prop.name == "customSparkleColour" || prop.name == "sparkleAmount" || prop.name == "sparkleSize") && !o.sparkles) continue;
                if (prop.name == "sparkleColour" && (!o.sparkles || !o.customSparkleColour)) continue;
            }

            if (Labels.TryGetValue(prop.name, out GUIContent label)) EditorGUILayout.PropertyField(prop, label);
            else EditorGUILayout.PropertyField(prop, true);
        }

        DrawHitZone(o, many);
        if (many || o.payloadType == ObstacleBase.PayloadType.Damage) DrawChase(o, many);

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawHitZone(ObstacleBase o, bool many)
    {
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField(new GUIContent("🎯 Hit zone (shown in the Scene view when selected)",
            "White cylinder: its solid body (land on its flat top to jump on it). " +
            "Coloured cylinder: walking into this hurts you (or picks it up). It only reaches out to the sides, never above."), EditorStyles.boldLabel);

        SerializedProperty fit = serializedObject.FindProperty("fitHitZoneToModel");
        EditorGUILayout.PropertyField(fit, new GUIContent("Fit to the model", fit.tooltip));

        // Width and height are shown in metres (they're stored for Object Scale 1)
        float scale = many ? 1f : Mathf.Max(0.01f, o.objectScale);
        SerializedProperty width = serializedObject.FindProperty("hitZoneWidth");
        SerializedProperty height = serializedObject.FindProperty("hitZoneHeight");
        using (new EditorGUI.DisabledScope(fit.boolValue && !fit.hasMultipleDifferentValues))
        {
            float maxSize = Mathf.Max(5f, 3f * Mathf.Max(width.floatValue, height.floatValue) * scale);
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = width.hasMultipleDifferentValues;
            float w = EditorGUILayout.Slider(new GUIContent("Width (metres)", width.tooltip), Mathf.Round(width.floatValue * scale * 100f) / 100f, 0.1f, maxSize);
            if (EditorGUI.EndChangeCheck()) width.floatValue = w / scale;
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = height.hasMultipleDifferentValues;
            float h = EditorGUILayout.Slider(new GUIContent("Height (metres)", height.tooltip), Mathf.Round(height.floatValue * scale * 100f) / 100f, 0.1f, maxSize);
            if (EditorGUI.EndChangeCheck()) height.floatValue = h / scale;
            EditorGUI.showMixedValue = false;
        }
        if (fit.boolValue) EditorGUILayout.LabelField("Untick 'Fit to the model' to change the width and height yourself.", EditorStyles.miniLabel);

        SerializedProperty touch = serializedObject.FindProperty("touchDistance");
        EditorGUILayout.PropertyField(touch, new GUIContent(
            o.payloadType == ObstacleBase.PayloadType.Damage ? "Hurts from (metres away)" : "Touch distance (metres)", touch.tooltip));

        if (!many && GUILayout.Button(new GUIContent("🎯 Fit it to the model again", "Measure the model now and fit the hit zone round it.")))
        {
            serializedObject.ApplyModifiedProperties();
            foreach (Object t in targets)
            {
                ObstacleBase ob = (ObstacleBase)t;
                Undo.RegisterFullObjectHierarchyUndo(ob.gameObject, "Fit Hit Zone");
                bool was = ob.fitHitZoneToModel;
                ob.fitHitZoneToModel = true;
                ob.UpdateHitZone(true);
                ob.fitHitZoneToModel = was;
                EditorUtility.SetDirty(ob);
            }
            serializedObject.Update();
        }
    }

    private void DrawChase(ObstacleBase o, bool many)
    {
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("🏃 Chase the player", EditorStyles.boldLabel);
        SerializedProperty chase = serializedObject.FindProperty("chasePlayer");
        EditorGUILayout.PropertyField(chase, new GUIContent("Chase the player", chase.tooltip));
        if (!chase.boolValue && !many) return;

        EditorGUI.indentLevel++;
        SerializedProperty start = serializedObject.FindProperty("chaseStartDistance");
        SerializedProperty giveUp = serializedObject.FindProperty("chaseGiveUpDistance");
        EditorGUILayout.PropertyField(start, new GUIContent("Starts chasing within", start.tooltip));
        EditorGUILayout.PropertyField(giveUp, new GUIContent("Gives up past (from home)", giveUp.tooltip));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("chaseSpeed"), new GUIContent("Chase speed", "How fast it runs while chasing."));
        if (giveUp.floatValue < start.floatValue)
            EditorGUILayout.HelpBox("'Gives up past' is smaller than 'Starts chasing within', so it may give up straight away.", MessageType.Warning);
        EditorGUILayout.LabelField("Orange circle: starts chasing. Red circle: gives up and walks home.", EditorStyles.miniLabel);
        EditorGUI.indentLevel--;
    }
}
