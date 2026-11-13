using UnityEditor;
using UnityEngine;

/// <summary>
/// The 🦊 Become a character box for "Become a character" pickups (inside the creature/pickup settings).
/// Picking a model finds its animations straight away (that only works in the Editor, so they are saved on the pickup).
/// </summary>
public static class BecomeEditor
{
    public static void Draw(SerializedObject so, ObstacleBase o)
    {
        SerializedProperty become = so.FindProperty("become");
        SerializedProperty look = become.FindPropertyRelative("look");

        // 🎨 Who you turn into
        EditorGUILayout.LabelField("🎨 Who you turn into", EditorStyles.boldLabel);
        SerializedProperty model = look.FindPropertyRelative("model");
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(model, new GUIContent("Model", "Drag in a model from Assets/MyModels (e.g. Roses Fox). Its animations are found for you."));
        if (EditorGUI.EndChangeCheck())
        {
            so.ApplyModifiedProperties();
            FindAnimations(o);
            so.Update();
        }
        if (o.become.look.model == null)
        {
            EditorGUILayout.HelpBox("Drag a model in here, e.g. Roses Fox from Assets/MyModels.", MessageType.Info);
            return;
        }

        Slider(look, "size", "Size", "Make the character bigger or smaller (1 = as tall as the normal player).", 0.1f, 50f);
        Slider(look, "turn", "Turn", "Turn the model if it faces the wrong way.", -180f, 180f);
        Slider(look, "raiseLower", "Raise / lower", "Move the model up or down if it floats or sinks into the ground.", -50f, 50f);
        Slider(look, "animationSpeed", "Animation speed", "Speed up or slow down its animations.", 0.1f, 10f);

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("🏃 Its animations (drag in any animation to change one)", EditorStyles.boldLabel);
        Clip(look, "idle", "Idle (standing still)");
        Clip(look, "walk", "Walk");
        Clip(look, "run", "Run (sprinting)");
        Clip(look, "jump", "Jump");
        Clip(look, "fall", "Fall");
        if (o.become.canFly) Clip(look, "fly", "Fly (while flying)");
        Clip(look, "hurt", "Hurt (when you get hurt)");
        if (GUILayout.Button(new GUIContent("🔍 Find its animations again", "Looks in the model file for idle, walk, run, jump... (replaces the ones above).")))
        {
            so.ApplyModifiedProperties();
            FindAnimations(o);
            so.Update();
        }

        // ⏱ Countdown
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("⏱ Countdown", EditorStyles.boldLabel);
        Toggle(become, "forever", "Stay the character for ever", "Ticked: no countdown, you stay the character until the level restarts.");
        if (!o.become.forever)
        {
            EditorGUILayout.PropertyField(so.FindProperty("buffDuration"), new GUIContent("Countdown (seconds)", "How long you stay the character. Then you're back to normal."));
            Toggle(become, "showCountdown", "Show the countdown on screen", "Shows the character's name and the time left at the top of the screen.");
        }

        // 🔧 What changes
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("🔧 While you are the character (tick what changes)", EditorStyles.boldLabel);
        Change(become, "changeSpeed", "moveSpeed", "Walking speed", "Normal player: about 10.", 1f, 1000f);
        Change(become, "changeSprint", "sprintMultiplier", "Sprint speed (× normal)", "How much faster sprinting is. Normal: 1.8.", 1f, 20f);
        Change(become, "changeJump", "jumpHeight", "Jump strength", "How high you jump. Normal: 10.", 1f, 1000f);
        Change(become, "changeAirSteer", "airSteerSpeed", "Steering in the air", "How well you can change direction in the air. Normal: 15.", 0f, 200f);
        Change(become, "changeStamina", "maxStamina", "Most stamina", "Your stamina bar is filled to this. Normal: 100.", 10f, 10000f);
        Change(become, "changeStaminaUse", "staminaDrainRate", "Stamina used per second", "How fast sprinting uses stamina. Normal: 12.5.", 1f, 100f);
        Change(become, "changeHealth", "maxHealth", "Most health", "Your health is filled to this. Afterwards it goes back to the normal most.", 10f, 10000f);
        Toggle(become, "invincible", "🛡 Nothing can hurt you", "Creatures and falls can't hurt you while you are the character.");
        Toggle(become, "noFallDamage", "🪂 Falling never hurts", "Landing from high up never hurts while you are the character.");

        // 🐉 Flying
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("🐉 Flying", EditorStyles.boldLabel);
        Toggle(become, "canFly", "Can fly", "Jump to take off, then hold Space to go up and Ctrl to come down. Let go to hover. Its Fly animation plays in the air (set it above).");
        if (o.become.canFly)
        {
            EditorGUI.indentLevel++;
            Slider(become, "flyUpSpeed", "Going up speed", "How fast Space takes you up. Normal: 10.", 1f, 200f);
            Slider(become, "flyDownSpeed", "Coming down speed", "How fast Ctrl takes you down. Normal: 10.", 1f, 200f);
            EditorGUI.indentLevel--;
            if (o.become.look.fly == null) EditorGUILayout.HelpBox("This model has no Fly animation, so it uses standing / walking / running in the air. Drag one into 'Fly' above, or press 🔍 Find its animations again.", MessageType.None);
        }

        // ⚔ Beating creatures
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("⚔ Beating creatures", EditorStyles.boldLabel);
        SerializedProperty hits = become.FindPropertyRelative("creatureHits");
        EditorGUILayout.PropertyField(hits, new GUIContent("Jumps on their head", "No Change: normal. Take Away: creatures need this many fewer jumps. Divide By: their jumps are divided by this (rounded up). Never fewer than 1."));
        if (o.become.creatureHits != CreatureHitsChange.NoChange)
        {
            SerializedProperty amount = become.FindPropertyRelative("creatureHitsAmount");
            string label = o.become.creatureHits == CreatureHitsChange.TakeAway ? "Take away" : "Divide by";
            amount.intValue = EditorGUILayout.IntSlider(label, amount.intValue, 1, 100);
            int example = 6;
            int n = o.become.creatureHits == CreatureHitsChange.TakeAway ? Mathf.Max(1, example - amount.intValue) : Mathf.Max(1, Mathf.CeilToInt(example / (float)Mathf.Max(1, amount.intValue)));
            EditorGUILayout.LabelField("e.g. a creature that needs " + example + " jumps needs only " + n + ".", EditorStyles.miniLabel);
        }
    }

    private static void Slider(SerializedProperty parent, string name, string label, string tip, float min, float max)
    {
        SerializedProperty p = parent.FindPropertyRelative(name);
        p.floatValue = EditorGUILayout.Slider(new GUIContent(label, tip), p.floatValue, min, max);
    }

    private static void Clip(SerializedProperty parent, string name, string label)
    {
        EditorGUILayout.PropertyField(parent.FindPropertyRelative(name), new GUIContent(label));
    }

    private static void Toggle(SerializedProperty parent, string name, string label, string tip)
    {
        SerializedProperty p = parent.FindPropertyRelative(name);
        p.boolValue = EditorGUILayout.Toggle(new GUIContent(label, tip), p.boolValue);
    }

    /// <summary>A tick box, and the value next to it when it's ticked.</summary>
    private static void Change(SerializedProperty parent, string tick, string value, string label, string tip, float min, float max)
    {
        SerializedProperty t = parent.FindPropertyRelative(tick);
        t.boolValue = EditorGUILayout.ToggleLeft(new GUIContent("Change: " + label, tip), t.boolValue);
        if (!t.boolValue) return;
        SerializedProperty v = parent.FindPropertyRelative(value);
        EditorGUI.indentLevel++;
        if (v.propertyType == SerializedPropertyType.Integer) v.intValue = EditorGUILayout.IntSlider(new GUIContent(label, tip), v.intValue, (int)min, (int)max);
        else v.floatValue = EditorGUILayout.Slider(new GUIContent(label, tip), v.floatValue, min, max);
        EditorGUI.indentLevel--;
    }

    /// <summary>Finds the model's own animations (idle, walk, run...) and saves them on the pickup.</summary>
    public static void FindAnimations(ObstacleBase o)
    {
        CustomModel.Look look = o.become.look;
        Undo.RecordObject(o, "Find animations");
        look.idle = look.walk = look.run = look.jump = look.fall = look.attack = look.hurt = look.death = look.fly = null;
        if (look.model != null)
        {
            GameObject temp = new GameObject("temp") { hideFlags = HideFlags.HideAndDontSave };
            try
            {
                CustomModel probe = temp.AddComponent<CustomModel>();
                probe.model = look.model;
                probe.animationsFrom = look.animationsFrom;
                probe.AutoFindClips();
                CustomModel.Look found = probe.GetLook();
                look.idle = found.idle; look.walk = found.walk; look.run = found.run; look.jump = found.jump;
                look.fall = found.fall; look.attack = found.attack; look.hurt = found.hurt; look.death = found.death; look.fly = found.fly;
            }
            finally { Object.DestroyImmediate(temp); }
        }
        EditorUtility.SetDirty(o);
    }
}
