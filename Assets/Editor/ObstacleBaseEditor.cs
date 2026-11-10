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
        "faceYouWhenAttacking",
        "shrinkAwayWhenBeaten", "waitBeforeShrinking", "shrinkTime", "deathSparkleWhen",
        "heightOffset", // drawn after Chase
        "jumpsToDefeat", "stompBounce", // drawn in the ⚔️ Battle settings box
    };

    [System.NonSerialized] private static GUIStyle boxTitle;

    /// <summary>Set by the Game Builder while it draws a creature: draws its 'How it moves' buttons inside the Movement box.</summary>
    public static System.Action<ObstacleBase> HowItMoves;

    /// <summary>Starts a bordered box whose title sits in its own coloured bar. End it with EndBox().</summary>
    private static void BeginBox(string title, string subtitle)
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        if (boxTitle == null)
        {
            boxTitle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 15, alignment = TextAnchor.MiddleLeft };
            boxTitle.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : new Color(0.1f, 0.15f, 0.25f);
        }
        Rect bar = GUILayoutUtility.GetRect(0f, 28f, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(bar, EditorGUIUtility.isProSkin ? new Color(0.2f, 0.34f, 0.55f) : new Color(0.62f, 0.76f, 0.93f));
        GUI.Label(new Rect(bar.x + 8f, bar.y, bar.width - 16f, bar.height), title, boxTitle);
        if (!string.IsNullOrEmpty(subtitle)) EditorGUILayout.LabelField(subtitle, EditorStyles.miniLabel);
    }

    private static void EndBox()
    {
        EditorGUILayout.Space(2);
        EditorGUILayout.EndVertical();
    }

    private static readonly HashSet<string> SparkleProps = new HashSet<string> { "sparkles", "sparkleStyle", "customSparkleColour", "sparkleColour", "sparkleAmount", "sparkleSize" };

    private static bool IsBeatable(ObstacleBase o) => o.payloadType == ObstacleBase.PayloadType.Damage && o.jumpsToDefeat > 0;

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

            // After the Bounce settings: the 🚶 Movement settings box (How it moves + Chase), then Height Alignment (ground creatures only)
            if (prop.name == "jumpsToDefeat")
            {
                bool creature = many || o.payloadType == ObstacleBase.PayloadType.Damage;
                bool howItMoves = HowItMoves != null && !many;
                if (creature || howItMoves)
                {
                    BeginBox("🚶 Movement settings", creature ? "How it moves around, and whether it chases you." : "How it moves around.");
                    if (howItMoves) HowItMoves(o);
                    if (creature) DrawChase(o, many);
                    EndBox();
                    EditorGUILayout.Space(4);
                }
                SerializedProperty height = serializedObject.FindProperty("heightOffset");
                if (height != null) EditorGUILayout.PropertyField(height, true);
            }
            if (prop.name == "m_Script" || DrawnLater.Contains(prop.name)) continue;

            if (!many)
            {
                if ((prop.name == "jumpsToDefeat" || prop.name == "stompBounce") && o.payloadType != ObstacleBase.PayloadType.Damage) continue;
                if ((prop.name == "launchForce" || prop.name == "upwardBias" || prop.name == "momentumTransfer") && !o.isBouncy) continue;
                if ((prop.name == "sparkleStyle" || prop.name == "customSparkleColour" || prop.name == "sparkleAmount" || prop.name == "sparkleSize") && !o.sparkles) continue;
                if (prop.name == "sparkleColour" && (!o.sparkles || !o.customSparkleColour)) continue;
            }

            // Creatures that can be beaten: the sparkle settings are shown in '💀 When it's beaten' instead
            if (!many && IsBeatable(o) && SparkleProps.Contains(prop.name)) continue;

            if (Labels.TryGetValue(prop.name, out GUIContent label)) EditorGUILayout.PropertyField(prop, label);
            else EditorGUILayout.PropertyField(prop, true);
        }

        if (many || o.payloadType == ObstacleBase.PayloadType.Damage)
        {
            // Everything about hurting and being beaten, in one box
            BeginBox("⚔️ Battle settings", "How it hurts you, and how you beat it.");

            EditorGUILayout.PropertyField(serializedObject.FindProperty("jumpsToDefeat"), Labels["jumpsToDefeat"]); // with its 🦶 Jump On Top title
            EditorGUILayout.PropertyField(serializedObject.FindProperty("stompBounce"));
            DrawHitZone(o, many);
            DrawAttack(o, many);
            if (many || o.jumpsToDefeat > 0) DrawBeaten(o, many);
            EndBox();
        }
        else
        {
            DrawHitZone(o, many); // pickups: just how close you have to be
        }

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

    /// <summary>⚔ When it hurts you: which animation it plays (any of its model's animations) and whether it turns to face you.</summary>
    private void DrawAttack(ObstacleBase o, bool many)
    {
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("⚔ When it hurts you", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("faceYouWhenAttacking"), new GUIContent("Turns to face you",
            "While its attack animation plays it quickly turns to face you (and stops walking), then turns back and carries on."));
        if (many) return;

        CustomModel cm = o.GetComponentInChildren<CustomModel>();
        if (cm == null || cm.model == null)
        {
            EditorGUILayout.LabelField("Give it a 3D model to choose an attack animation.", EditorStyles.wordWrappedMiniLabel);
            return;
        }

        CustomModelEditor.AnimationSlot(cm, new GUIContent("Attack animation",
            "Plays when it hurts you. Drag in any animation, e.g. a Jump if it has no attack animation."), cm.attack, c => cm.attack = c);
        if (cm.attack == null)
            EditorGUILayout.LabelField("No attack animation: drag any animation into the box above (a Jump works well).", EditorStyles.wordWrappedMiniLabel);

        // An 'it attacks you' animation chosen in the Sounds tab for this model plays instead
        foreach (ModelSounds ms in GameSounds.Load().modelSounds)
        {
            if (ms == null || ms.model != cm.model) continue;
            if (ms.animations.Exists(a => a != null && a.animation != null && a.playWhen == AnimationMoment.AttacksYou))
            {
                EditorGUILayout.LabelField("Game Builder > Sounds has an 'it attacks you' animation for this model, so that one plays instead.", EditorStyles.wordWrappedMiniLabel);
                break;
            }
        }
    }

    /// <summary>💀 When it's beaten: its death animation, then shrinking away.</summary>
    private void DrawBeaten(ObstacleBase o, bool many)
    {
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("💀 When it's beaten", EditorStyles.boldLabel);
        CustomModel cm = many ? null : o.GetComponentInChildren<CustomModel>();
        if (cm != null && cm.model != null)
        {
            CustomModelEditor.AnimationSlot(cm, new GUIContent("Death animation",
                "Plays when it's beaten; it stays in the last pose. Drag in any animation. Empty = it gets squashed flat instead."), cm.death, c => cm.death = c);
            if (cm.death == null)
                EditorGUILayout.LabelField("No death animation: it gets squashed flat and disappears.", EditorStyles.wordWrappedMiniLabel);
        }

        SerializedProperty shrink = serializedObject.FindProperty("shrinkAwayWhenBeaten");
        EditorGUILayout.PropertyField(shrink, new GUIContent("Shrinks away afterwards", shrink.tooltip));
        if (many || shrink.boolValue)
        {
            SerializedProperty wait = serializedObject.FindProperty("waitBeforeShrinking");
            EditorGUILayout.PropertyField(wait, new GUIContent("Lies there for (seconds)", wait.tooltip));
            SerializedProperty time = serializedObject.FindProperty("shrinkTime");
            EditorGUILayout.PropertyField(time, new GUIContent("Shrinks over (seconds)", time.tooltip));
        }

        if (many) return; // with several selected, the sparkle settings are in the main list above
        SerializedProperty sparkles = serializedObject.FindProperty("sparkles");
        EditorGUILayout.PropertyField(sparkles, new GUIContent("✨ Sparkle burst", "A burst of sparkles when it's beaten."));
        if (!sparkles.boolValue) return;
        EditorGUILayout.PropertyField(serializedObject.FindProperty("deathSparkleWhen"), new GUIContent("Sparkles when", "When it disappears at the end, as soon as it's beaten, or both."));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("sparkleStyle"), new GUIContent("Sparkle style"));
        SerializedProperty own = serializedObject.FindProperty("customSparkleColour");
        EditorGUILayout.PropertyField(own, new GUIContent("Pick my own colour", own.tooltip));
        if (own.boolValue) EditorGUILayout.PropertyField(serializedObject.FindProperty("sparkleColour"), new GUIContent("Sparkle colour"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("sparkleAmount"), new GUIContent("How many sparkles"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("sparkleSize"), new GUIContent("Sparkle size"));
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
