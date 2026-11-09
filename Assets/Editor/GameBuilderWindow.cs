using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// One window for building the game: a tab for each part (Player, Level, Platforms, Creatures & Pickups,
/// Scenery, Signs & Story, Build). Open it with Game > Game Builder.
/// Most tabs show the same green buttons as the controllers, just gathered in one place.
/// </summary>
public class GameBuilderWindow : EditorWindow
{
    private enum Tab { Player, Level, Platforms, Creatures, Scenery, Signs, Build }

    private static readonly string[] TabNames =
    {
        "🧙 Player", "🏔 Level", "🪨 Platforms", "🕷 Creatures & Pickups", "🌲 Scenery", "💬 Signs & Story", "📦 Levels & Build"
    };

    private const string TabKey = "GameBuilder.Tab";

    private Tab tab;
    private Vector2 scroll;
    private readonly Dictionary<string, Vector2> listScrolls = new Dictionary<string, Vector2>();
    private readonly Dictionary<Object, Editor> editors = new Dictionary<Object, Editor>();
    private readonly Dictionary<string, bool> foldouts = new Dictionary<string, bool>();

    // Cached scene contents (refreshed when the Hierarchy changes)
    private List<PlatformLogic> platforms = new List<PlatformLogic>();
    private List<ObstacleBase> obstacles = new List<ObstacleBase>();
    private List<TriggerLogic> triggers = new List<TriggerLogic>();
    private List<Checkpoint> checkpoints = new List<Checkpoint>();
    private List<Material> skyMaterials;

    private GUIStyle bigButton, titleStyle, tabStyle, rowStyle, rowSelectedStyle;

    [MenuItem("Game/🎮 Game Builder", priority = -100)]
    public static void Open()
    {
        GameBuilderWindow window = GetWindow<GameBuilderWindow>("Game Builder");
        window.minSize = new Vector2(360, 400);
        window.Show();
    }

    // ------------------------------------------------------------------ Window events

    private void OnEnable()
    {
        tab = (Tab)EditorPrefs.GetInt(TabKey, 0);
        RefreshSceneLists();
    }

    private void OnDisable()
    {
        foreach (Editor ed in editors.Values)
        {
            if (ed != null) DestroyImmediate(ed);
        }
        editors.Clear();
    }

    private void OnHierarchyChange()
    {
        RefreshSceneLists();
        Repaint();
    }

    private void OnSelectionChange() => Repaint();

    private void RefreshSceneLists()
    {
        platforms = SortByName(FindAll<PlatformLogic>());
        obstacles = SortByName(FindAll<ObstacleBase>());
        triggers = SortByName(FindAll<TriggerLogic>());
        checkpoints = SortByName(FindAll<Checkpoint>());

        // Forget inspectors for things that were deleted
        foreach (Object key in editors.Keys.ToList())
        {
            if (key == null)
            {
                if (editors[key] != null) DestroyImmediate(editors[key]);
                editors.Remove(key);
            }
        }
    }

    private void OnGUI()
    {
        InitStyles();
        DrawTopBar();
        DrawTabs();

        scroll = EditorGUILayout.BeginScrollView(scroll);
        EditorGUILayout.Space(4);
        switch (tab)
        {
            case Tab.Player: DrawPlayerTab(); break;
            case Tab.Level: DrawLevelTab(); break;
            case Tab.Platforms: DrawPlatformsTab(); break;
            case Tab.Creatures: DrawCreaturesTab(); break;
            case Tab.Scenery: DrawSceneryTab(); break;
            case Tab.Signs: DrawSignsTab(); break;
            case Tab.Build: DrawBuildTab(); break;
        }
        EditorGUILayout.Space(20);
        EditorGUILayout.EndScrollView();
    }

    private void DrawTopBar()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            GUILayout.Label("Level: " + SceneManager.GetActiveScene().name, EditorStyles.toolbarButton);
            GUILayout.FlexibleSpace();
            bool teacher = StudentMode.TeacherMode;
            string label = teacher ? "👩‍🏫 Teacher mode: showing everything" : "🎒 Student mode: tidy";
            bool now = GUILayout.Toggle(teacher, label, EditorStyles.toolbarButton);
            if (now != teacher) StudentMode.TeacherMode = now;
        }
    }

    private void DrawTabs()
    {
        int columns = position.width > 900 ? 7 : 4;
        int chosen = GUILayout.SelectionGrid((int)tab, TabNames, columns, tabStyle);
        if (chosen != (int)tab)
        {
            tab = (Tab)chosen;
            EditorPrefs.SetInt(TabKey, chosen);
            scroll = Vector2.zero;
            GUI.FocusControl(null);
        }
    }

    // ------------------------------------------------------------------ 🧙 Player

    private void DrawPlayerTab()
    {
        Title("🧙 Player", "Make your hero, choose how they look, and set how fast and high they go.");

        PlayerController player = Find<PlayerController>();
        if (player == null)
        {
            NotSetUp();
            return;
        }

        DrawInspector(player); // Create Character + Player Model

        Health body = player.GetComponentInChildren<Health>(true);
        if (body == null) return;

        if (BigButton("🔍 Show me my player in the Scene"))
        {
            SelectAndFrame(body.gameObject);
        }

        Section("🏃 Moving, sprinting & jumping");
        DrawInspector(player.GetComponent<PlayerLogic>());

        Section("❤️ Health");
        DrawInspector(body);

        FallDamage fall = body.GetComponent<FallDamage>();
        if (fall != null)
        {
            Section("🪂 Fall damage");
            DrawInspector(fall);
        }

        CustomModel model = body.GetComponent<CustomModel>();
        if (model != null && model.model != null && Foldout("playerModel", "🎨 Model size, turn & animations"))
        {
            DrawInspector(model);
        }
    }

    // ------------------------------------------------------------------ 🏔 Level

    private void DrawLevelTab()
    {
        Title("🏔 Level", "Shape the ground, paint it, and choose the sky and sunlight.");

        Terrain terrain = Find<Terrain>();
        Section("⛰ The ground");
        if (terrain == null)
        {
            EnvironmentController env = Find<EnvironmentController>();
            if (env != null) DrawInspector(env);
            else NotSetUp();
        }
        else
        {
            EditorGUILayout.HelpBox(
                "Pick a brush, then click and drag on the ground in the Scene view.\n" +
                "• Hold Shift to lower instead of raise.\n" +
                "• Change Brush Size and Opacity (strength) in the Inspector.\n" +
                "• Trees, rocks and plants are in the 🌲 Scenery tab.", MessageType.Info);

            DrawToolGrid(terrain, new[]
            {
                ("⬆ Raise / Lower", "PaintHeightTool"),
                ("〰 Smooth", "SmoothHeightTool"),
                ("▬ Flatten", "SetHeightTool"),
                ("🎨 Paint Ground", "PaintTextureTool"),
                ("🔨 Stamp", "StampTool"),
                ("🌱 Paint Grass", "PaintDetailsTool"),
            });

            if (BigButton("✋ Stop sculpting (back to moving things)"))
            {
                Tools.current = Tool.Move;
            }
        }

        DrawSunAndSky();

        Section("🧱 World border");
        GameSettings settings = GameSettings.Load();
        if (settings != null)
        {
            EditorGUI.BeginChangeCheck();
            bool border = EditorGUILayout.ToggleLeft("Invisible walls stop players leaving the map (all levels)", settings.useWorldBorder);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(settings, "World Border");
                settings.useWorldBorder = border;
                EditorUtility.SetDirty(settings);
            }
        }
    }

    private void DrawToolGrid(Terrain terrain, (string label, string tool)[] tools)
    {
        int perRow = position.width > 600 ? 3 : 2;
        for (int i = 0; i < tools.Length; i += perRow)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                for (int j = i; j < Mathf.Min(i + perRow, tools.Length); j++)
                {
                    if (BigButton(tools[j].label, new Color(0.55f, 0.85f, 0.55f)))
                    {
                        UseTerrainTool(terrain, tools[j].tool);
                    }
                }
            }
        }
    }

    /// <summary>Selects the ground and switches the Scene view to one of Unity's terrain brushes.</summary>
    private static void UseTerrainTool(Terrain terrain, string toolName)
    {
        Selection.activeGameObject = terrain.gameObject;
        EditorApplication.delayCall += () =>
        {
            System.Type found = null;
            foreach (System.Type type in TypeCache.GetTypesDerivedFrom<EditorTool>())
            {
                if (type.IsAbstract) continue;
                if (type.Name == toolName + "Ovl") { found = type; break; } // Terrain Tools package version
                if (type.Name == toolName) found = type;
            }

            if (found != null) ToolManager.SetActiveTool(found);
            else Debug.LogWarning("Couldn't find the terrain brush '" + toolName + "'. Use the Terrain's Inspector instead.");

            if (SceneView.lastActiveSceneView != null) SceneView.lastActiveSceneView.Focus();
        };
    }

    private void DrawSunAndSky()
    {
        Section("☀️ Sun & sky");

        Light sun = RenderSettings.sun;
        if (sun == null) sun = FindAll<Light>().FirstOrDefault(l => l.type == LightType.Directional);
        if (sun != null)
        {
            Vector3 euler = sun.transform.eulerAngles;
            float height = euler.x > 180f ? euler.x - 360f : euler.x;

            EditorGUI.BeginChangeCheck();
            height = EditorGUILayout.Slider("Sun height", height, -10f, 90f);
            float turn = EditorGUILayout.Slider("Sun direction", euler.y, 0f, 360f);
            Color colour = EditorGUILayout.ColorField("Sunlight colour", sun.color);
            float brightness = EditorGUILayout.Slider("Brightness", sun.intensity, 0f, 3f);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObjects(new Object[] { sun.transform, sun }, "Change Sun");
                sun.transform.rotation = Quaternion.Euler(height, turn, 0f);
                sun.color = colour;
                sun.intensity = brightness;
            }
        }
        else
        {
            EditorGUILayout.HelpBox("This level has no sun (Directional Light).", MessageType.None);
        }

        // Sky
        if (skyMaterials == null)
        {
            skyMaterials = AssetDatabase.FindAssets("t:Material", new[] { "Assets" })
                .Select(g => AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(m => m != null && m.shader != null && m.shader.name.StartsWith("Skybox"))
                .ToList();
        }

        List<Material> choices = new List<Material>(skyMaterials);
        if (RenderSettings.skybox != null && !choices.Contains(RenderSettings.skybox)) choices.Insert(0, RenderSettings.skybox);
        if (choices.Count > 0)
        {
            string[] names = choices.Select(m => m.name).ToArray();
            int current = Mathf.Max(0, choices.IndexOf(RenderSettings.skybox));
            int picked = EditorGUILayout.Popup("Sky", current, names);
            if (picked != current)
            {
                RenderSettings.skybox = choices[picked];
                DynamicGI.UpdateEnvironment();
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            }
        }

        // Fog
        EditorGUI.BeginChangeCheck();
        bool fog = EditorGUILayout.Toggle("Fog", RenderSettings.fog);
        Color fogColour = RenderSettings.fogColor;
        float density = RenderSettings.fogDensity;
        if (fog)
        {
            fogColour = EditorGUILayout.ColorField("Fog colour", fogColour);
            density = EditorGUILayout.Slider("Fog thickness", density, 0f, 0.05f);
        }
        if (EditorGUI.EndChangeCheck())
        {
            RenderSettings.fog = fog;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = fogColour;
            RenderSettings.fogDensity = density;
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }
    }

    // ------------------------------------------------------------------ 🪨 Platforms

    private void DrawPlatformsTab()
    {
        Title("🪨 Platforms", "Floating, moving and spinning platforms to jump across.\nClick a platform in the list (or in the Scene) to change it.");

        PlatformController controller = Find<PlatformController>();
        if (controller == null)
        {
            NotSetUp();
            return;
        }

        DrawInspector(controller);

        Section("Your platforms");
        DrawList("platforms", platforms, "No platforms yet. Click 'Create New Platform' above.", p => "🪨 " + p.name);

        PlatformLogic selected = SelectedOf<PlatformLogic>();
        if (selected != null)
        {
            Section("✏️ " + selected.name);
            DrawInspector(selected);
            PlatformPopulator populator = selected.GetComponent<PlatformPopulator>();
            if (populator != null && Foldout("platformMore", "More: copy, delete, or scatter creatures on this platform"))
            {
                DrawInspector(populator);
            }
        }
    }

    // ------------------------------------------------------------------ 🕷 Creatures & Pickups

    private void DrawCreaturesTab()
    {
        Title("🕷 Creatures & Pickups", "Creatures hurt the player. Pickups help them.\nClick one and it appears in the middle of the Scene view, or on the platform you have selected.");

        PlatformLogic platform = SelectedOf<PlatformLogic>();
        TerrainPopulator ground = Find<TerrainPopulator>();
        if (platform == null && ground == null)
        {
            EditorGUILayout.HelpBox("Make the ground first (🏔 Level tab), or select a platform.", MessageType.Warning);
            return;
        }

        EditorGUILayout.HelpBox(platform != null
            ? "New things will go on the platform: " + platform.name
            : "New things will go on the ground where the Scene view is looking. Select a platform to put them on it instead.",
            MessageType.None);

        Section("🕷 Creatures (they hurt!)");
        ButtonRow(
            ("🕷 Wanderer\nwalks around", () => Spawn(ObstacleBase.PayloadType.Damage, ObstacleSpawnType.Wander, 10, 5f, false, false)),
            ("🧟 Patroller\nwalks back & forth", () => Spawn(ObstacleBase.PayloadType.Damage, ObstacleSpawnType.Patrol, 10, 5f, false, false)),
            ("🐍 Guard\nstays still", () => Spawn(ObstacleBase.PayloadType.Damage, ObstacleSpawnType.Static, 10, 5f, false, false)));

        Section("🎁 Pickups (they help!)");
        ButtonRow(
            ("❤️ Health\nheals you", () => Spawn(ObstacleBase.PayloadType.HealthBooster, ObstacleSpawnType.Static, 25, 5f, true, false)),
            ("⚡ Stamina\nrefills sprint", () => Spawn(ObstacleBase.PayloadType.StaminaBooster, ObstacleSpawnType.Static, 50, 5f, true, false)),
            ("🛡 Shield\ncan't be hurt", () => Spawn(ObstacleBase.PayloadType.InvincibilityBuff, ObstacleSpawnType.Static, 1, 8f, true, false)));
        ButtonRow(
            ("🦘 Super Jump\njump higher", () => Spawn(ObstacleBase.PayloadType.JumpBuff, ObstacleSpawnType.Static, 2, 8f, true, false)),
            ("💨 Speed Boost\nrun faster", () => Spawn(ObstacleBase.PayloadType.SprintBuff, ObstacleSpawnType.Static, 2, 8f, true, false)),
            ("🏀 Bounce Ball\nlaunches you up", () => Spawn(ObstacleBase.PayloadType.None, ObstacleSpawnType.Static, 10, 5f, false, true)));

        Section("Your creatures & pickups");
        DrawList("obstacles", obstacles, "None yet. Click a button above.", o => IconFor(o) + " " + o.name + "  (" + NiceName(o) + ")");

        ObstacleBase selected = SelectedOf<ObstacleBase>();
        if (selected != null)
        {
            Section("✏️ " + selected.name);
            DrawInspector(selected);

            CustomModel model = selected.GetComponent<CustomModel>();
            if (model != null) DrawInspector(model);
            else if (BigButton("🎨 Give it a 3D model"))
            {
                Undo.AddComponent<CustomModel>(selected.gameObject);
            }

            if (BigButton("🗑 Delete " + selected.name, new Color(0.95f, 0.5f, 0.5f)))
            {
                Undo.DestroyObjectImmediate(selected.gameObject);
                GUIUtility.ExitGUI();
            }
        }

        if (ground != null && Foldout("scatter", "🎲 Lots at once: scatter creatures over the ground"))
        {
            DrawInspector(ground);
        }
    }

    private void Spawn(ObstacleBase.PayloadType payload, ObstacleSpawnType movement, int amount, float duration, bool usedUp, bool bouncy)
    {
        HashSet<ObstacleBase> before = new HashSet<ObstacleBase>(FindAll<ObstacleBase>());
        PlatformLogic platform = SelectedOf<PlatformLogic>();
        PlatformPopulator onPlatform = platform != null ? platform.GetComponent<PlatformPopulator>() : null;

        if (onPlatform != null)
        {
            // Borrow the platform's settings for this one creation, then put them back
            string saved = JsonUtility.ToJson(onPlatform);
            onPlatform.scatterObstacleType = movement;
            onPlatform.baseSettings.payloadType = payload;
            onPlatform.baseSettings.payloadAmount = amount;
            onPlatform.baseSettings.buffDuration = duration;
            onPlatform.baseSettings.destroyOnTrigger = usedUp;
            onPlatform.baseSettings.isBouncy = bouncy;
            onPlatform.CreateSingleObstacle();
            JsonUtility.FromJsonOverwrite(saved, onPlatform);
        }
        else
        {
            TerrainPopulator ground = Find<TerrainPopulator>();
            if (ground == null) return;

            string saved = JsonUtility.ToJson(ground);
            ground.scatterObstacleType = movement;
            ground.baseSettings.payloadType = payload;
            ground.baseSettings.payloadAmount = amount;
            ground.baseSettings.buffDuration = duration;
            ground.baseSettings.destroyOnTrigger = usedUp;
            ground.baseSettings.isBouncy = bouncy;
            if (bouncy) { ground.baseSettings.launchForce = 30f; ground.baseSettings.objectScale = 1.4f; }
            ground.CreateSingleObstacle();
            JsonUtility.FromJsonOverwrite(saved, ground);
        }

        ObstacleBase created = FindAll<ObstacleBase>().FirstOrDefault(o => !before.Contains(o));
        if (created == null) return;

        created.name = NiceName(created);
        if (onPlatform == null) MoveToSceneViewGround(created.transform);

        RefreshSceneLists();
        SelectAndFrame(created.gameObject);
    }

    /// <summary>New ground creatures appear in the middle of the map; move them to where the student is looking.</summary>
    private static void MoveToSceneViewGround(Transform obj)
    {
        SceneView view = SceneView.lastActiveSceneView;
        Terrain terrain = obj.GetComponentInParent<Terrain>();
        if (terrain == null) terrain = Find<Terrain>();
        if (view == null || terrain == null) return;

        Vector3 target = view.pivot;
        Transform cam = view.camera.transform;
        if (Physics.Raycast(cam.position, cam.forward, out RaycastHit hit, 5000f)) target = hit.point;

        Vector3 tPos = terrain.transform.position;
        Vector3 size = terrain.terrainData.size;
        target.x = Mathf.Clamp(target.x, tPos.x + 2f, tPos.x + size.x - 2f);
        target.z = Mathf.Clamp(target.z, tPos.z + 2f, tPos.z + size.z - 2f);

        // Keep the same height above the ground it was given
        float lift = obj.position.y - (terrain.SampleHeight(obj.position) + tPos.y);
        float groundY = terrain.SampleHeight(target) + tPos.y;
        Undo.RecordObject(obj, "Move");
        obj.position = new Vector3(target.x, groundY + lift, target.z);
    }

    private static string NiceName(ObstacleBase o)
    {
        if (o.isBouncy) return "Bounce Ball";
        switch (o.payloadType)
        {
            case ObstacleBase.PayloadType.HealthBooster: return "Health Pickup";
            case ObstacleBase.PayloadType.StaminaBooster: return "Stamina Pickup";
            case ObstacleBase.PayloadType.InvincibilityBuff: return "Shield Pickup";
            case ObstacleBase.PayloadType.JumpBuff: return "Super Jump Pickup";
            case ObstacleBase.PayloadType.SprintBuff: return "Speed Boost Pickup";
            case ObstacleBase.PayloadType.None: return "Harmless Thing";
        }

        IObstacleMovement movement = o.GetComponent<IObstacleMovement>();
        string kind = movement == null ? "" : movement.GetType().Name;
        if (kind.Contains("Wander")) return "Wandering Creature";
        if (kind.Contains("Patrol")) return "Patrolling Creature";
        return "Guard Creature";
    }

    private static string IconFor(ObstacleBase o)
    {
        if (o.isBouncy) return "🏀";
        switch (o.payloadType)
        {
            case ObstacleBase.PayloadType.Damage: return "🕷";
            case ObstacleBase.PayloadType.HealthBooster: return "❤️";
            case ObstacleBase.PayloadType.StaminaBooster: return "⚡";
            case ObstacleBase.PayloadType.InvincibilityBuff: return "🛡";
            case ObstacleBase.PayloadType.JumpBuff: return "🦘";
            case ObstacleBase.PayloadType.SprintBuff: return "💨";
        }
        return "⚪";
    }

    // ------------------------------------------------------------------ 🌲 Scenery

    private void DrawSceneryTab()
    {
        Title("🌲 Scenery", "Trees, rocks, bushes and plants. Choose models for each group, then Scatter them, or Place One where the Scene view is looking.");

        SceneryPopulator scenery = Find<SceneryPopulator>();
        if (scenery != null)
        {
            DrawInspector(scenery);
            return;
        }

        TerrainPopulator ground = Find<TerrainPopulator>();
        if (ground == null)
        {
            EditorGUILayout.HelpBox("Make the ground first (🏔 Level tab).", MessageType.Warning);
            return;
        }

        if (BigButton("🌲 Add Scenery Tool", new Color(0.2f, 0.8f, 0.3f)))
        {
            Undo.AddComponent<SceneryPopulator>(ground.gameObject);
        }
    }

    // ------------------------------------------------------------------ 💬 Signs & Story

    private void DrawSignsTab()
    {
        Title("💬 Signs & Story", "Pop-up messages, checkpoint flags, and the story and Library your players can read.");

        Section("💬 Pop-up signs");
        TriggerController triggerController = Find<TriggerController>();
        if (triggerController != null) DrawInspector(triggerController);
        DrawList("triggers", triggers, "No pop-ups yet.", t => "💬 " + t.name);

        TriggerLogic trigger = SelectedOf<TriggerLogic>();
        if (trigger != null)
        {
            Section("✏️ " + trigger.name);
            DrawInspector(trigger);
        }

        Section("🚩 Checkpoints");
        EditorGUILayout.HelpBox("If the player dies after touching a flag, they start again from that flag.", MessageType.None);
        if (BigButton("🚩 Create Checkpoint", new Color(0.2f, 0.8f, 0.3f)))
        {
            GameMenuItems.CreateCheckpoint();
            RefreshSceneLists();
        }
        DrawList("checkpoints", checkpoints, "No checkpoints yet.", c => "🚩 " + c.name);

        Checkpoint flag = SelectedOf<Checkpoint>();
        if (flag != null)
        {
            Section("✏️ " + flag.name);
            DrawInspector(flag);
        }

        Section("📖 Story, Library & game title");
        GameSettings settings = GameSettings.Load();
        if (settings == null)
        {
            if (BigButton("⚙️ Make Game Settings")) GameMenuItems.OpenSettings();
        }
        else if (Foldout("settings", "Game title, main menu, death screen, default models and Library entries"))
        {
            DrawInspector(settings);
        }
    }

    // ------------------------------------------------------------------ 📦 Levels & Build

    private void DrawBuildTab()
    {
        Title("📦 Levels & Build", "Your levels, testing, and making the game for others to play.");

        Section("🗺 Levels in your game (played in this order)");
        EditorGUILayout.HelpBox("The game starts at level 1. Use ▲ ▼ to change the order, untick a level to leave it out of the game, or ✕ to remove it from the list (the level file isn't deleted).", MessageType.None);
        Scene active = SceneManager.GetActiveScene();
        List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
        bool activeInBuild = false;
        bool changed = false;
        int number = 0;
        GUI.enabled = !EditorApplication.isPlaying;
        for (int i = 0; i < scenes.Count; i++)
        {
            string path = scenes[i].path;
            bool isActive = path == active.path;
            activeInBuild |= isActive;
            bool included = scenes[i].enabled;
            if (included) number++;

            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                bool nowIncluded = EditorGUILayout.Toggle(included, GUILayout.Width(18));
                if (nowIncluded != included)
                {
                    scenes[i].enabled = nowIncluded;
                    changed = true;
                }

                string label = (included ? number + ".  " : "—  ") + Path.GetFileNameWithoutExtension(path);
                if (included && number == 1) label += "   ⭐ start";
                if (!included) label += "   (left out)";
                if (isActive) label += "   ← you are here";
                GUILayout.Label(label, isActive ? EditorStyles.boldLabel : EditorStyles.label);

                GUI.enabled = !EditorApplication.isPlaying && i > 0;
                if (GUILayout.Button("▲", GUILayout.Width(28))) { Swap(scenes, i, i - 1); changed = true; }
                GUI.enabled = !EditorApplication.isPlaying && i < scenes.Count - 1;
                if (GUILayout.Button("▼", GUILayout.Width(28))) { Swap(scenes, i, i + 1); changed = true; }
                GUI.enabled = !EditorApplication.isPlaying && !isActive;
                if (GUILayout.Button("Open", GUILayout.Width(55)) && EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    EditorSceneManager.OpenScene(path);
                    GUIUtility.ExitGUI();
                }
                GUI.enabled = !EditorApplication.isPlaying;
                if (GUILayout.Button("✕", GUILayout.Width(26)) &&
                    EditorUtility.DisplayDialog("Remove level", "Remove " + Path.GetFileNameWithoutExtension(path) + " from the game's level list?\n\nThe level itself is NOT deleted. You can add it back by opening it and clicking 'Add this level to the game'.", "Remove", "Cancel"))
                {
                    scenes.RemoveAt(i);
                    changed = true;
                    break;
                }
            }
        }
        GUI.enabled = true;

        if (changed)
        {
            EditorBuildSettings.scenes = scenes.ToArray();
            GUIUtility.ExitGUI();
        }

        if (number == 0)
        {
            EditorGUILayout.HelpBox("No levels are ticked, so the game has nothing to play!", MessageType.Error);
        }

        if (!activeInBuild)
        {
            EditorGUILayout.HelpBox("This level isn't in your game yet.", MessageType.Warning);
            if (BigButton("➕ Add this level to the game", new Color(0.2f, 0.8f, 0.3f)))
            {
                GameMenuItems.AddOpenSceneToBuild();
            }
        }

        Section("▶ Test");
        if (!EditorApplication.isPlaying)
        {
            if (BigButton("▶ Play this level", new Color(0.4f, 0.75f, 1f))) EditorApplication.EnterPlaymode();
        }
        else if (BigButton("■ Stop playing", new Color(0.95f, 0.5f, 0.5f)))
        {
            EditorApplication.ExitPlaymode();
        }

        Section("📦 Share your game");
        EditorGUILayout.HelpBox("Builds a Windows game into the Build/Windows folder. Copy that whole folder to share it.", MessageType.None);
        GUI.enabled = !EditorApplication.isPlaying;
        if (BigButton("📦 Build Game (Windows)", new Color(0.2f, 0.8f, 0.3f)))
        {
            GameMenuItems.BuildWindowsMenu();
            GUIUtility.ExitGUI();
        }
        GUI.enabled = true;
        if (Directory.Exists("Build/Windows") && GUILayout.Button("📂 Open the build folder"))
        {
            EditorUtility.RevealInFinder(Path.GetFullPath("Build/Windows"));
        }
    }

    // ------------------------------------------------------------------ Helpers

    private void InitStyles()
    {
        if (bigButton != null) return;
        bigButton = new GUIStyle(GUI.skin.button) { fontSize = 13, fontStyle = FontStyle.Bold, wordWrap = true, fixedHeight = 0, padding = new RectOffset(8, 8, 8, 8) };
        titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 20 };
        tabStyle = new GUIStyle(GUI.skin.button) { fontSize = 13, fontStyle = FontStyle.Bold, fixedHeight = 32 };
        rowStyle = new GUIStyle(EditorStyles.label) { fontSize = 12, padding = new RectOffset(8, 4, 4, 4) };
        rowSelectedStyle = new GUIStyle(rowStyle) { fontStyle = FontStyle.Bold };
        rowSelectedStyle.normal.textColor = EditorGUIUtility.isProSkin ? new Color(0.5f, 0.85f, 1f) : new Color(0.05f, 0.35f, 0.75f);
    }

    private void Title(string text, string help)
    {
        GUILayout.Label(text, titleStyle);
        if (!string.IsNullOrEmpty(help)) EditorGUILayout.HelpBox(help, MessageType.Info);
    }

    private static void Section(string text)
    {
        EditorGUILayout.Space(12);
        GUILayout.Label(text, EditorStyles.boldLabel);
    }

    private bool BigButton(string label, Color? colour = null)
    {
        Color old = GUI.backgroundColor;
        if (colour.HasValue) GUI.backgroundColor = colour.Value;
        bool pressed = GUILayout.Button(label, bigButton, GUILayout.MinHeight(label.Contains('\n') ? 54 : 38));
        GUI.backgroundColor = old;
        return pressed;
    }

    private void ButtonRow(params (string label, System.Action action)[] buttons)
    {
        System.Action clicked = null;
        using (new EditorGUILayout.HorizontalScope())
        {
            foreach ((string label, System.Action action) in buttons)
            {
                if (BigButton(label)) clicked = action;
            }
        }
        if (clicked != null)
        {
            clicked();
            GUIUtility.ExitGUI();
        }
    }

    private bool Foldout(string key, string label)
    {
        foldouts.TryGetValue(key, out bool open);
        EditorGUILayout.Space(8);
        open = EditorGUILayout.Foldout(open, label, true, EditorStyles.foldoutHeader);
        foldouts[key] = open;
        return open;
    }

    /// <summary>Draws a component's normal Inspector (with its green buttons) inside this window.</summary>
    private void DrawInspector(Object target)
    {
        if (target == null) return;
        if (!editors.TryGetValue(target, out Editor ed) || ed == null)
        {
            ed = Editor.CreateEditor(target);
            editors[target] = ed;
        }

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            ed.OnInspectorGUI();
        }
    }

    /// <summary>A clickable list; clicking a row selects that object and shows it in the Scene view.</summary>
    private void DrawList<T>(string key, List<T> items, string emptyMessage, System.Func<T, string> label) where T : Component
    {
        items.RemoveAll(i => i == null);
        if (items.Count == 0)
        {
            EditorGUILayout.HelpBox(emptyMessage, MessageType.None);
            return;
        }

        T selected = SelectedOf<T>();
        listScrolls.TryGetValue(key, out Vector2 listScroll);
        bool scrolls = items.Count > 8;
        if (scrolls) listScroll = EditorGUILayout.BeginScrollView(listScroll, EditorStyles.helpBox, GUILayout.Height(190));
        else EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        foreach (T item in items)
        {
            bool isSelected = item == selected;
            if (GUILayout.Button((isSelected ? "▶ " : "") + label(item), isSelected ? rowSelectedStyle : rowStyle))
            {
                SelectAndFrame(item.gameObject);
            }
        }

        if (scrolls) EditorGUILayout.EndScrollView();
        else EditorGUILayout.EndVertical();
        listScrolls[key] = listScroll;
    }

    private void NotSetUp()
    {
        EditorGUILayout.HelpBox("This level isn't set up for the Game Builder. Open one of your levels (📦 Levels & Build tab), or make a new level by copying Level01.", MessageType.Warning);
    }

    private static void SelectAndFrame(GameObject go)
    {
        Selection.activeGameObject = go;
        if (SceneView.lastActiveSceneView != null) SceneView.lastActiveSceneView.FrameSelected();
    }

    private static T SelectedOf<T>() where T : Component
    {
        GameObject go = Selection.activeGameObject;
        return go != null && go.scene.IsValid() ? go.GetComponentInParent<T>(true) : null;
    }

    private static void Swap<T>(List<T> list, int a, int b)
    {
        T temp = list[a];
        list[a] = list[b];
        list[b] = temp;
    }

    /// <summary>Sorts so "Sign 2" comes before "Sign 10".</summary>
    private static List<T> SortByName<T>(T[] items) where T : Object
    {
        List<T> list = items.ToList();
        list.Sort((a, b) => EditorUtility.NaturalCompare(a.name, b.name));
        return list;
    }

    private static T Find<T>() where T : Object => Object.FindAnyObjectByType<T>(FindObjectsInactive.Include);

    private static T[] FindAll<T>() where T : Object => Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
}
