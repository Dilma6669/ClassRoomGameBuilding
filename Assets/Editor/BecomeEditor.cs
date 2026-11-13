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
        Toggle(become, "pickupSameSize", "📏 Pickup is as big as the character", "The pickup itself is made the same size as the character you turn into, so you can see how big you'll be.");
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
        if (o.become.canShoot) Clip(look, "attack", "Attack (when shooting)");
        Clip(look, "hurt", "Hurt (when you get hurt)");
        if (GUILayout.Button(new GUIContent("🔍 Find its animations again", "Looks in the model file for idle, walk, run, jump... (replaces the ones above).")))
        {
            so.ApplyModifiedProperties();
            FindAnimations(o);
            so.Update();
        }

        // ✨ Turning-into effect
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("✨ Turning-into effect", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(become.FindPropertyRelative("transformEffect"), new GUIContent("Sparkles", "The sparkles when you turn into the character. None = no sparkles."));
        if (o.become.transformEffect != Sparkles.Effect.None)
        {
            EditorGUI.indentLevel++;
            Toggle(become, "transformCustomColour", "Pick my own colour", "Otherwise the sparkles are pink.");
            if (o.become.transformCustomColour) EditorGUILayout.PropertyField(become.FindPropertyRelative("transformColour"), new GUIContent("Colour"));
            Slider(become, "transformAmount", "How many sparkles", "1 = normal.", 0.2f, 20f);
            Slider(become, "transformSize", "Sparkle size", "1 = normal (bigger characters get bigger sparkles too).", 0.3f, 10f);
            EditorGUI.indentLevel--;
        }
        Toggle(become, "transformGrow", "Pops in (grows from tiny)", "The new character grows from tiny to full size.");
        Toggle(become, "transformSpin", "Spins round once", "The new character spins round once as it appears.");
        if (o.become.transformGrow || o.become.transformSpin) Slider(become, "transformTime", "Pop / spin time (seconds)", "How long the pop-in and spin take.", 0.1f, 3f);
        Toggle(become, "transformEffectWhenBack", "Same effect when you turn back", "The same sparkles, pop and spin when you go back to normal.");

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
            Slider(become, "flyTilt", "Tilts up / down (degrees)", "It tips its nose up when going up and down when coming down. 0 = stays level.", 0f, 60f);
            Slider(become, "flyCameraLift", "📷 Camera higher (metres)", "The camera sits higher while you're this character, so it's lower on the screen and you can see ahead. 0 = normal.", 0f, 30f);
            EditorGUI.indentLevel--;
            if (o.become.look.fly == null) EditorGUILayout.HelpBox("This model has no Fly animation, so it uses standing / walking / running in the air. Drag one into 'Fly' above, or press 🔍 Find its animations again.", MessageType.None);
        }

        // 🏹 Shooting
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("🏹 Shooting (left click)", EditorStyles.boldLabel);
        Toggle(become, "canShoot", "Can shoot", "Hold the left mouse button to shoot the way the character faces. A flying character tipped up or down shoots up or down.");
        if (o.become.canShoot)
        {
            EditorGUI.indentLevel++;
            SerializedProperty dmg = become.FindPropertyRelative("shootDamage");
            dmg.intValue = EditorGUILayout.IntSlider(new GUIContent("Damage", "How much each shot hurts a creature. 10 damage = one jump on its head (a creature that needs 3 jumps is beaten by 30)."), dmg.intValue, 1, 1000);
            Slider(become, "shootEveryMin", "Seconds between shots (shortest)", "While you hold the button, the next shot comes after a random time between the shortest and longest. Make them the same for steady shooting.", 0.03f, 10f);
            Slider(become, "shootEveryMax", "Seconds between shots (longest)", "Make it the same as the shortest for steady shooting.", 0.03f, 10f);
            Slider(become, "shootSpeed", "Shot speed", "How fast the shots fly (metres per second).", 1f, 300f);
            SerializedProperty many = become.FindPropertyRelative("shotsAtOnce");
            many.intValue = EditorGUILayout.IntSlider(new GUIContent("Shots at once", "Several shots spread out like a fan."), many.intValue, 1, 12);
            if (o.become.shotsAtOnce > 1) Slider(become, "shootSpread", "Fan width (degrees between shots)", "How spread out the shots are.", 0f, 60f);
            Slider(become, "shootSize", "Shot size (metres)", "How big each shot is.", 0.1f, 20f);
            EditorGUILayout.PropertyField(become.FindPropertyRelative("shootColour"), new GUIContent("Colour", "The glowing ball, its trail and the burst when it hits."));
            EditorGUILayout.PropertyField(become.FindPropertyRelative("shootModel"), new GUIContent("Shoot a model", "Drag in a model (e.g. a rock) to shoot instead of a glowing ball. Empty = a glowing ball."));
            Toggle(become, "shootTrail", "Glowing trail", "A glowing trail behind each shot.");
            EditorGUILayout.PropertyField(become.FindPropertyRelative("shootHitEffect"), new GUIContent("Sparkles when it hits"));
            Slider(become, "shootLasts", "A miss disappears after (seconds)", "How long a shot flies before it vanishes.", 0.5f, 30f);
            Slider(become, "shootHoming", "Homing (curves to creatures)", "The shots curve towards the nearest creature in front of you. 0 = straight, 1 = turns very hard.", 0f, 1f);
            Toggle(become, "shootDrops", "Drops like a thrown ball", "The shots fall as they fly.");
            Slider(become, "shootAimUpDown", "Aim up / down (degrees)", "Shoots a little higher (+) or lower (-) than straight ahead.", -45f, 45f);
            Slider(become, "shootFromHeight", "Shoots from (0 feet, 1 head)", "Where the shots come out of the character.", 0f, 1f);
            EditorGUILayout.PropertyField(become.FindPropertyRelative("shootAnimation"), new GUIContent("Shooting animation", "Plays when you shoot. Empty = its attack animation (if it has one)."));
            Slider(become, "shootDelay", "Shot leaves after (seconds)", "Wait this long after the animation starts, so the shot matches it. 0 = straight away.", 0f, 3f);

            EditorGUILayout.Space(2);
            EditorGUILayout.PropertyField(become.FindPropertyRelative("shootUses"), new GUIContent("Each shot uses", "Stamina Bar: shots use stamina, so you juggle sprinting and shooting. Own Bar: a yellow bar at the top fills up with each shot; when full, wait for it to go down. No Limit: shoot for ever."));
            switch (o.become.shootUses)
            {
                case BecomeSettings.ShootBarKind.StaminaBar:
                    Slider(become, "shootBarPerShot", "Stamina per shot", "How much stamina each shot uses. Normal most stamina: 100 (change it above with 'Most stamina').", 0f, 500f);
                    break;
                case BecomeSettings.ShootBarKind.OwnBar:
                    Slider(become, "shootBarSize", "Bar size", "How full the yellow bar can get. Bigger = more shots before it's full.", 1f, 1000f);
                    Slider(become, "shootBarPerShot", "Each shot fills", "How much each shot fills the bar.", 0f, 500f);
                    Slider(become, "shootBarRefill", "Goes down per second", "How fast the bar empties again when you stop shooting.", 1f, 500f);
                    Slider(become, "shootBarWait", "Wait before going down (seconds)", "After a shot, the bar waits this long before it starts emptying.", 0f, 5f);
                    Slider(become, "shootBarReadyAt", "Full? Shoot again at (%)", "When the bar gets full, you can shoot again once it's down to this % full.", 0f, 95f);
                    int shots = o.become.shootBarPerShot > 0f ? Mathf.CeilToInt(o.become.shootBarSize / o.become.shootBarPerShot) : 0;
                    EditorGUILayout.LabelField(shots > 0 ? "About " + shots + " shots in a row before it's full." : "Shots don't fill it: no limit.", EditorStyles.miniLabel);
                    break;
            }
            EditorGUI.indentLevel--;
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
