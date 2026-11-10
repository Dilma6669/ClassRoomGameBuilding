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
public partial class GameBuilderWindow : EditorWindow
{
    private enum Tab { Player, Level, Platforms, Creatures, Scenery, Signs, Sounds, Build }
    public const int SoundsTabIndex = (int)Tab.Sounds;

    private static readonly string[] TabNames =
    {
        "🧙 Player", "🏔 Level", "🪨 Platforms", "🕷 Creatures & Pickups", "🌲 Scenery", "💬 Signs & Story", "🔊 Sounds", "📦 Levels & Build"
    };

    private const string TabKey = "GameBuilder.Tab";

    private Tab tab;
    private Vector2 scroll;
    private string newLevelName;
    private readonly Dictionary<string, Vector2> listScrolls = new Dictionary<string, Vector2>();
    private readonly Dictionary<Object, Editor> editors = new Dictionary<Object, Editor>();
    private readonly Dictionary<string, bool> foldouts = new Dictionary<string, bool>();

    // Cached scene contents (refreshed when the Hierarchy changes)
    private List<PlatformLogic> platforms = new List<PlatformLogic>();
    private List<ObstacleBase> obstacles = new List<ObstacleBase>();
    private List<TriggerLogic> triggers = new List<TriggerLogic>();
    private List<Checkpoint> checkpoints = new List<Checkpoint>();
    private List<Material> skyMaterials;

    [System.NonSerialized] private GUIStyle bigButton, titleStyle, tabStyle, rowStyle, rowSelectedStyle;

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
        EditorSceneManager.sceneOpened -= OnLevelOpened;
        tab = (Tab)EditorPrefs.GetInt(TabKey, 0);
        RefreshSceneLists();
        EditorSceneManager.sceneOpened += OnLevelOpened;
    }

    private void OnLevelOpened(Scene scene, OpenSceneMode mode)
    {
        RefreshSceneLists();
        Repaint();
    }

    private void OnDisable()
    {
        EditorSceneManager.sceneOpened -= OnLevelOpened;
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
        NameUnnamedObstacles();

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
        BeginTheme();
        try
        {
            // Room for the plain-English labels ('Forward push when jumping')
            EditorGUIUtility.labelWidth = Mathf.Clamp(position.width * 0.32f, 150f, 230f);
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
                case Tab.Sounds: DrawSoundsTab(); break;
                case Tab.Build: DrawBuildTab(); break;
            }
            EditorGUILayout.Space(20);
            EditorGUILayout.EndScrollView();
        }
        finally
        {
            EditorGUIUtility.labelWidth = 0f; // back to Unity's default
            EndTheme();
        }
    }

    private void DrawTopBar()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            GUILayout.Label("Level: " + SceneManager.GetActiveScene().name, EditorStyles.toolbarButton);
            GUILayout.FlexibleSpace();
            GUILayout.Label(new GUIContent("↩ Made a mistake? Ctrl+Z undoes it", "Ctrl+Z undoes your last change. Ctrl+Y puts it back."), EditorStyles.toolbarButton);
            GUILayout.FlexibleSpace();
            bool teacher = StudentMode.TeacherMode;
            string label = teacher ? "👩‍🏫 Teacher mode: showing everything" : "🎒 Student mode: tidy";
            bool now = GUILayout.Toggle(teacher, label, EditorStyles.toolbarButton);
            if (now != teacher) StudentMode.TeacherMode = now;
        }
    }

    private void DrawTabs()
    {
        int columns = position.width > 900 ? 8 : 4;
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

        // ⚙️ The model a NEW character gets
        if (Box("⚙️ Default options for new characters", "The 3D model a NEW character gets when you click Create Character (e.g. in a new level).", true))
        {
            EditorGUILayout.LabelField("To change THIS player's look, use 'Player Model' in 'Your character' below instead.", EditorStyles.wordWrappedMiniLabel);
            SettingsField("defaultPlayerModel", "🧙 Default player model");
        }
        EndBox();

        // One box: Create Character + Player Model, the model's settings, and 'Show me my player'
        Health body = player.GetComponentInChildren<Health>(true);
        if (Box("🧙 Your character", "Make your hero and choose how they look."))
        {
            DrawInspector(player, false);

            if (body != null)
            {
                // Right under 'Player Model': the chosen model's size, turn and animations
                CustomModel model = body.GetComponent<CustomModel>();
                bool hasModel = model != null && model.model != null;
                if (hasModel)
                {
                    if (BeginFoldout("playerModel", "🎨 Model size, turn & animations"))
                    {
                        // 'Player Model' above already picks the model, and the Library box is below
                        CustomModelEditor.InPlayerTab = true;
                        try { DrawInspector(model, false); }
                        finally { CustomModelEditor.InPlayerTab = false; }
                    }
                    EndFoldout();
                }

                EditorGUILayout.Space(4);
                if (BigButton("🔍 Show me my player in the Scene"))
                {
                    SelectAndFrame(body.gameObject);
                }

                // The player's Library entry: picture, add / new picture / delete picture
                if (hasModel)
                {
                    EditorGUILayout.Space(4);
                    CustomModelEditor.DrawLibrarySection(model);
                    EditorGUILayout.Space(2);
                }
            }
        }
        EndBox();
        if (body == null) return;

        if (Box("🏃 Moving, sprinting & jumping", "How fast your hero walks and sprints, stamina, and how high they jump."))
            DrawInspector(player.GetComponent<PlayerLogic>(), false);
        EndBox();

        if (Box("❤️ Health", "How much health your hero has, and how they flash when hurt."))
            DrawInspector(body, false);
        EndBox();

        FallDamage fall = body.GetComponent<FallDamage>();
        if (fall != null)
        {
            if (Box("🪂 Fall damage", "How much falling from high up hurts."))
                DrawInspector(fall, false);
            EndBox();
        }
    }

    /// <summary>A folding box with its title in a coloured bar (blue; green = emerald for lists, tools and defaults). Draw its contents only if it returns true, then always call EndBox().</summary>
    private static bool Box(string title, string subtitle, bool green = false) => ObstacleBaseEditor.BeginBox(title, subtitle, green);

    private static void EndBox() => ObstacleBaseEditor.EndBox();

    [System.NonSerialized] private static GUIStyle groupTitle;

    /// <summary>
    /// A bordered group with a big title (not folding), a little in from both sides of the window, holding a whole
    /// topic: its Create button, its list and the selected thing's boxes (e.g. '💬 Pop-up signs'). End it with EndGroup().
    /// </summary>
    private static void BeginGroup(string title)
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(8);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        if (groupTitle == null) groupTitle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 17 };
        GUILayout.Label(title, groupTitle, GUILayout.Height(28));
    }

    private static void EndGroup()
    {
        EditorGUILayout.Space(5);
        EditorGUILayout.EndVertical();
        GUILayout.Space(8);
        EditorGUILayout.EndHorizontal();
    }

    // ------------------------------------------------------------------ 🏔 Level

    private void DrawLevelTab()
    {
        Title("🏔 Level", "Shape the ground, paint it, and choose the sky and sunlight.");

        Terrain terrain = Find<Terrain>();
        if (!Box("⛰ The ground", "Shape the ground with brushes, and paint it.", true))
        {
            // folded shut
        }
        else if (terrain == null)
        {
            EnvironmentController env = Find<EnvironmentController>();
            if (env != null)
            {
                EditorGUILayout.HelpBox("This level has no ground yet.\n1. Click 'Create Terrain' below.\n2. Then go to the 🧙 Player tab and click 'Create Character'.", MessageType.Info);
                DrawInspector(env);
            }
            else NotSetUp();
        }
        else
        {
            EditorGUILayout.HelpBox(
                "Pick a brush, then click and drag on the ground in the Scene view.\n" +
                "• Hold Shift to lower instead of raise.\n" +
                "• Change the brush's size and strength with the sliders under the buttons.\n" +
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

            DrawBrushSliders(terrain);

            if (BigButton("✋ Stop sculpting (back to moving things)"))
            {
                Tools.current = Tool.Move;
            }
        }
        EndBox();

        if (Box("☀️ Sun & sky", "Sunlight, the sky picture and fog.")) DrawSunAndSky();
        EndBox();

        GameSettings settings = GameSettings.Load();
        if (Box("🧱 World border", "Invisible walls round the map, and how far down you can fall (all levels).") && settings != null)
        {
            EditorGUI.BeginChangeCheck();
            bool border = EditorGUILayout.ToggleLeft("Invisible walls stop players leaving the map (all levels)", settings.useWorldBorder);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(settings, "World Border");
                settings.useWorldBorder = border;
                EditorUtility.SetDirty(settings);
            }
            if (settings.useWorldBorder)
            {
                EditorGUI.indentLevel++;
                SettingsField("borderPadding", "Gap from the edge");
                SettingsField("skyLimitHeight", "Ceiling height");
                EditorGUI.indentLevel--;
            }
            SettingsField("fallKillDepth", "Fall depth (you die)");
        }
        EndBox();
    }

    /// <summary>One Game Settings value (shared by every level), shown in whichever tab it belongs to.</summary>
    private static void SettingsField(string property, string label, string tooltip = null)
    {
        GameSettings settings = GameSettings.Load();
        if (settings == null) return;
        System.Reflection.FieldInfo field = typeof(GameSettings).GetField(property);
        if (field == null) return;
        TooltipAttribute tip = (TooltipAttribute)System.Attribute.GetCustomAttribute(field, typeof(TooltipAttribute));
        GUIContent content = new GUIContent(label, tooltip ?? tip?.tooltip);
        object value = field.GetValue(settings), changed = value;

        // Drawn by hand (not PropertyField) so the field's [Header] from Game Settings doesn't come with it
        EditorGUI.BeginChangeCheck();
        if (field.FieldType == typeof(bool)) changed = EditorGUILayout.ToggleLeft(content, (bool)value);
        else if (field.FieldType == typeof(float))
        {
            RangeAttribute range = (RangeAttribute)System.Attribute.GetCustomAttribute(field, typeof(RangeAttribute));
            changed = range != null ? EditorGUILayout.Slider(content, (float)value, range.min, range.max) : EditorGUILayout.FloatField(content, (float)value);
        }
        else if (typeof(Object).IsAssignableFrom(field.FieldType)) changed = EditorGUILayout.ObjectField(content, (Object)value, field.FieldType, false);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(settings, "Change " + label);
            field.SetValue(settings, changed);
            EditorUtility.SetDirty(settings);
        }
    }

    /// <summary>A closed-by-default fold-out at the bottom of a tab for the 'default model' settings.</summary>
    private void DefaultModelsFoldout(string key, string title, string help, params (string property, string label)[] fields)
    {
        EditorGUILayout.Space(8);
        if (BeginFoldout(key, title))
        {
            EditorGUILayout.LabelField(help, EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.Space(2);
            foreach ((string property, string label) in fields) SettingsField(property, label);
        }
        EndFoldout();
    }

    private void DrawToolGrid(Terrain terrain, (string label, string tool)[] tools)
    {
        Color? blue = new Color(0.4f, 0.75f, 1f);
        int clicked = ButtonGrid(Columns(3), tools.Select(t => t.label).ToList(), tools.Select(t => blue).ToList());
        if (clicked >= 0) UseTerrainTool(terrain, tools[clicked].tool);
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
            string[] names = choices.Select(FriendlySkyName).ToArray();
            // Two skies with the same friendly name would merge in the menu, so number them
            string[] plain = (string[])names.Clone();
            for (int i = 0; i < names.Length; i++)
            {
                if (plain.Count(n => n == plain[i]) > 1) names[i] += "  #" + (i + 1);
            }
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

        // ⚙️ Settings for the buttons below
        if (Box("⚙️ Default options for spawning platforms", "Where new platforms go, how high they float, and how they look.", true))
        {
            DrawClickToPlaceToggle();
            EditorGUILayout.Space(4);
            float newHeight = EditorGUILayout.Slider(new GUIContent("New platforms float this high (m)",
                "How far above the ground new platforms go. Change one afterwards with 'Height above the ground' when it's selected."),
                ScatterTools.NewPlatformHeight, 0.5f, 30f);
            if (!Mathf.Approximately(newHeight, ScatterTools.NewPlatformHeight)) ScatterTools.NewPlatformHeight = newHeight;

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("🧩 Look for ALL new platforms (3D model, mesh, material)", EditorStyles.boldLabel);
            DrawInspector(controller, false);
            // The model every level falls back on when this level's 3D model box is empty
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("For every level (used when 'Platform Model' above is empty):", EditorStyles.miniBoldLabel);
            SettingsField("defaultPlatformModel", "Default platform model");
        }
        EndBox();

        // Always shown (even with the box folded): 'Now click in the Scene view...' + Stop
        DrawPlacingStatus("New platforms appear where the Scene view is looking.");

        Section("⚡ Quick platforms");
        PlaceRow(4,
            ("🪨 Floating\nstays still", () => QuickPlatform(controller, ScatterTools.PlatformMove.Still)),
            ("↔ Moving\nside to side", () => QuickPlatform(controller, ScatterTools.PlatformMove.SideToSide)),
            ("🛗 Elevator\nup and down", () => QuickPlatform(controller, ScatterTools.PlatformMove.UpAndDown)),
            ("🔄 Spinning\nturns round", () => QuickPlatform(controller, ScatterTools.PlatformMove.Spinning)),
            ("🫧 Growing\ngets big & small", () => QuickPlatform(controller, ScatterTools.PlatformMove.Growing)),
            ("👣 Stepping stones\n6 in a row, going up", () => { ScatterTools.SteppingStones(controller); RefreshSceneLists(); }),
            ("💥 Crumbling\nfalls when you stand on it", () => QuickPlatform(controller, ScatterTools.PlatformMove.Still, PlatformLogic.PlatformType.Crumbling)),
            ("🟢 Bouncy\nlaunches you up high", () => QuickPlatform(controller, ScatterTools.PlatformMove.Still, PlatformLogic.PlatformType.Bouncy)));

        EditorGUILayout.Space(6);
        if (Box("📋 Platforms List", "Click one to change it. Drag them into folders to keep things tidy.", true))
        {
            InsetList(() => DrawList("platforms", platforms, "No platforms yet. Click a Quick platform button above.",
                p => IconFor(p.platformType) + IconFor(ScatterTools.GetMovement(p)) + " " + p.name));
        }
        EndBox();

        PlatformLogic selected = SelectedOf<PlatformLogic>();
        if (selected != null)
        {
            EditorGUILayout.Space(6);
            DrawNameAndFolder(selected, "platforms", platforms, "✏️ Name");

            if (Box("🚶 Movement & type", "How it moves, and whether it's normal, crumbling or bouncy."))
            {
            EditorGUILayout.LabelField("Make it:", EditorStyles.miniBoldLabel);
            ScatterTools.PlatformMove now = ScatterTools.GetMovement(selected);
            Color chosen = new Color(0.6f, 0.85f, 1f);
            ScatterTools.PlatformMove[] moves = (ScatterTools.PlatformMove[])System.Enum.GetValues(typeof(ScatterTools.PlatformMove));
            int moveClicked = ButtonGrid(Columns(moves.Length), moves.Select(m => IconFor(m) + " " + ScatterTools.MoveName(m)).ToList(),
                moves.Select(m => m == now ? chosen : (Color?)null).ToList());
            if (moveClicked >= 0 && moves[moveClicked] != now)
            {
                ScatterTools.PlatformMove move = moves[moveClicked];
                float distance = selected.moveDistance > 0f ? selected.moveDistance : (move == ScatterTools.PlatformMove.UpAndDown ? 8f : 6f);
                ScatterTools.SetMovement(selected, move, distance, Mathf.Max(1f, selected.moveSpeed));
                GUIUtility.ExitGUI();
            }
            EditorGUILayout.LabelField("Special:", EditorStyles.miniBoldLabel);
            PlatformLogic.PlatformType[] types = (PlatformLogic.PlatformType[])System.Enum.GetValues(typeof(PlatformLogic.PlatformType));
            int typeClicked = ButtonGrid(Columns(types.Length), types.Select(t => t == PlatformLogic.PlatformType.Normal ? "Normal" : IconFor(t) + " " + t).ToList(),
                types.Select(t => t == selected.platformType ? chosen : (Color?)null).ToList());
            if (typeClicked >= 0 && types[typeClicked] != selected.platformType)
            {
                ScatterTools.SetPlatformType(selected, types[typeClicked]);
                GUIUtility.ExitGUI();
            }
            }
            EndBox();

            if (Box("📐 Platform settings", "Where it is, its size and shape, its model, how it moves, and its effects."))
            {
                DrawPosition(selected.TargetChild, selected.name, hit => MovePlatformTo(selected, hit), null, () => DrawPlatformHeight(selected));
                DrawInspector(selected, false);
            }
            EndBox();

            if (Box("✨ Glow & sparkles", "Make it glow, or add sparkles around it."))
                DrawGlow(selected.TargetChild != null ? selected.TargetChild.gameObject : null, new Color(0.5f, 0.8f, 1f));
            EndBox();

            PlatformPopulator populator = selected.GetComponent<PlatformPopulator>();
            if (populator != null)
            {
                if (Box("🧩 More", "Copy or delete this platform, or scatter creatures on it."))
                    DrawInspector(populator, false);
                EndBox();
            }
        }

        // 🎲 Lots at once
        if (Box("🎲 Scatter tools", "Put lots of platforms down at once, with random sizes and movement.", true))
        {
            PlatformScatter scatter = controller.GetComponent<PlatformScatter>();
            if (scatter != null) DrawInspector(scatter, false);
            else if (BigButton("🎲 Start scattering platforms", new Color(0.2f, 0.8f, 0.3f)))
            {
                Undo.AddComponent<PlatformScatter>(controller.gameObject);
            }
        }
        EndBox();
    }

    /// <summary>Draws a list a little narrower than its box, so it sits nicely inside it.</summary>
    private static void InsetList(System.Action draw)
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(8);
        EditorGUILayout.BeginVertical();
        draw();
        EditorGUILayout.EndVertical();
        GUILayout.Space(8);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(4);
    }

    // ------------------------------------------------------------------ 🕷 Creatures & Pickups

    private void DrawCreaturesTab()
    {
        Title("🕷 Creatures & Pickups", "Creatures hurt the player. Pickups help them.\nClick a button, then click in the Scene where you want it (on the ground or on a platform).");

        PlatformLogic platform = SelectedOf<PlatformLogic>();
        TerrainPopulator ground = Find<TerrainPopulator>();
        if (platform == null && ground == null)
        {
            EditorGUILayout.HelpBox("Make the ground first (🏔 Level tab), or select a platform.", MessageType.Warning);
            return;
        }

        // ⚙️ Settings for the buttons below
        if (ObstacleBaseEditor.BeginBox("⚙️ Default options for spawning creatures", "Where new things go, how big new creatures are, and which models they get.", green: true))
        {
            DrawClickToPlaceToggle();
            EditorGUILayout.Space(4);
            float newSize = EditorGUILayout.Slider(new GUIContent("New creatures are this big",
                "Size of new creatures (and scattered ones). 2 = a zombie about as tall as the player. " +
                "Change one afterwards with its Object Scale. Pickups aren't changed."),
                ScatterTools.NewCreatureSize, 0.5f, 6f);
            if (!Mathf.Approximately(newSize, ScatterTools.NewCreatureSize)) ScatterTools.NewCreatureSize = newSize;
            EditorGUILayout.LabelField("Tip: the player jumps about 1.7 m, so very tall creatures can only be jumped on from something higher.", EditorStyles.wordWrappedMiniLabel);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("🧩 Default models for new creatures", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("The 3D model each NEW creature gets when you click Wanderer, Patroller or Guard (in every level). " +
                "To change one creature that's already made, click it in the 'Creatures and Pickups List' instead.", EditorStyles.wordWrappedMiniLabel);
            SettingsField("defaultWanderEnemy", "🕷 Wanderer");
            SettingsField("defaultPatrolEnemy", "🧟 Patroller");
            SettingsField("defaultStaticEnemy", "🐍 Guard");
        }
        ObstacleBaseEditor.EndBox();

        // Always shown (even with the box folded): 'Now click in the Scene view...' + Stop, or where new things go
        DrawPlacingStatus(platform != null
            ? "New things will go on the platform: " + platform.name
            : "New things will go on the ground where the Scene view is looking. Select a platform to put them on it instead.");

        Section("🕷 Creatures (they hurt!)");
        PlaceRow(3,
            ("🕷 Wanderer\nwalks around", () => Spawn(ObstacleBase.PayloadType.Damage, ObstacleSpawnType.Wander, 10, 5f, false, false)),
            ("🧟 Patroller\nwalks back & forth", () => Spawn(ObstacleBase.PayloadType.Damage, ObstacleSpawnType.Patrol, 10, 5f, false, false)),
            ("🐍 Guard\nstays still", () => Spawn(ObstacleBase.PayloadType.Damage, ObstacleSpawnType.Static, 10, 5f, false, false)));

        Section("🎁 Pickups (they help!)");
        PlaceRow(3,
            ("❤️ Health\nheals you", () => Spawn(ObstacleBase.PayloadType.HealthBooster, ObstacleSpawnType.Static, 25, 5f, true, false)),
            ("⚡ Stamina\nrefills sprint", () => Spawn(ObstacleBase.PayloadType.StaminaBooster, ObstacleSpawnType.Static, 50, 5f, true, false)),
            ("🛡 Shield\ncan't be hurt", () => Spawn(ObstacleBase.PayloadType.InvincibilityBuff, ObstacleSpawnType.Static, 1, 8f, true, false)),
            ("🦘 Super Jump\njump higher", () => Spawn(ObstacleBase.PayloadType.JumpBuff, ObstacleSpawnType.Static, 2, 8f, true, false)),
            ("💨 Speed Boost\nrun faster", () => Spawn(ObstacleBase.PayloadType.SprintBuff, ObstacleSpawnType.Static, 2, 8f, true, false)),
            ("🏀 Bounce Ball\nlaunches you up", () => Spawn(ObstacleBase.PayloadType.None, ObstacleSpawnType.Static, 10, 5f, false, true)));

        EditorGUILayout.Space(6); // the same gap as between the boxes below
        if (ObstacleBaseEditor.BeginBox("📋 Creatures and Pickups List", "Click one to change it. Drag them into folders to keep things tidy.", green: true))
        {
            // A little narrower than the box, so it sits nicely inside it
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(8);
            EditorGUILayout.BeginVertical();
            // Show what kind it is only when the name doesn't already say so (e.g. 'Jump On Me  (Patroller)')
            DrawList("obstacles", obstacles, "None yet. Click a button above.", o =>
            {
                string kind = NiceName(o);
                return IconFor(o) + " " + o.name + (o.name.StartsWith(kind) ? "" : "  (" + kind + ")");
            });
            EditorGUILayout.EndVertical();
            GUILayout.Space(8);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(4);
        }
        ObstacleBaseEditor.EndBox();

        ObstacleBase selected = SelectedOf<ObstacleBase>();
        if (selected != null)
        {
            EditorGUILayout.Space(6);
            // One outer box round everything about the selected one: Name, Model, its settings boxes and Delete.
            // A little narrower than the window on both sides.
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(8);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Space(-8); // top gap = the side gaps (the Name box adds its own gap above it)
            // Name and Model: the same width as the settings boxes below (Unity adds a small margin round those)
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical();
            DrawNameAndFolder(selected, "obstacles", obstacles, "✏️ Name");

            // 🎨 What it looks like
            if (ObstacleBaseEditor.BeginBox("🎨 Model", "What it looks like, and its animations."))
            {
                CustomModel model = selected.GetComponent<CustomModel>();
                if (model != null)
                {
                    CustomModelEditor.InModelBox = true;
                    try { DrawInspector(model, false); }
                    finally { CustomModelEditor.InModelBox = false; }
                }
                else if (BigButton("🎨 Give it a 3D model"))
                {
                    Undo.AddComponent<CustomModel>(selected.gameObject);
                }
                DrawGlow(selected.gameObject, Sparkles.ColourFor(selected.payloadType));
            }
            ObstacleBaseEditor.EndBox();
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            // Its settings: 📍 Move it… sits at the top of the 📐 Transform box, How it moves in the 🚶 Movement box
            ObstacleBaseEditor.MoveIt = o => DrawPosition(o.transform, o.name, hit => MoveObstacleTo(o, hit), () => PutObstacleOnGround(o));
            ObstacleBaseEditor.HowItMoves = DrawMovement;
            try { DrawInspector(selected, false); }
            finally { ObstacleBaseEditor.HowItMoves = null; ObstacleBaseEditor.MoveIt = null; }

            // Delete it: at the bottom of its box
            EditorGUILayout.Space(10);
            // The same width as the blue boxes above
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(3);
            EditorGUILayout.BeginVertical();
            bool delete = BigButton("🗑 Delete " + selected.name, new Color(0.95f, 0.5f, 0.5f));
            EditorGUILayout.EndVertical();
            GUILayout.Space(3);
            EditorGUILayout.EndHorizontal();
            if (delete)
            {
                Undo.DestroyObjectImmediate(selected.gameObject);
                GUIUtility.ExitGUI();
            }
            EditorGUILayout.Space(5); // bottom gap = the side gaps
            EditorGUILayout.EndVertical();
            GUILayout.Space(8);
            EditorGUILayout.EndHorizontal();
        }

        // 🎲 Lots at once
        if (ObstacleBaseEditor.BeginBox("🎲 Scatter tools", "Put lots of creatures and pickups down at once, on the ground and/or on platforms.", green: true))
        {
            Terrain terrain = Find<Terrain>();
            CreatureScatter scatter = terrain != null ? terrain.GetComponent<CreatureScatter>() : null;
            if (scatter != null) DrawInspector(scatter, false);
            else if (terrain == null) EditorGUILayout.HelpBox("Make the ground first (🏔 Level tab).", MessageType.Warning);
            else if (BigButton("🎲 Start scattering creatures & pickups", new Color(0.2f, 0.8f, 0.3f)))
            {
                Undo.AddComponent<CreatureScatter>(terrain.gameObject);
            }
        }
        ObstacleBaseEditor.EndBox();
    }

    private void QuickPlatform(PlatformController controller, ScatterTools.PlatformMove move, PlatformLogic.PlatformType type = PlatformLogic.PlatformType.Normal)
    {
        PlatformLogic p = ScatterTools.QuickPlatform(controller, move, type);
        if (p != null) p.name = UniqueName(p.name, p); // Floating Platform, Floating Platform 2...
        RefreshSceneLists();
        if (p == null) return;
        // Placed by clicking: the student is already looking there, so don't move the camera
        if (placeHit.HasValue) Selection.activeGameObject = p.gameObject;
        else SelectAndFrame(p.gameObject);
    }

    /// <summary>The spot clicked in the Scene, while a click-to-place button is doing its job (else null).</summary>
    [System.NonSerialized] private RaycastHit? placeHit;

    /// <summary>The 'click in the Scene to place' tick box, and a Stop button while placing.</summary>
    private void DrawPlacingOptions(string whenOff)
    {
        DrawClickToPlaceToggle();
        DrawPlacingStatus(whenOff);
    }

    /// <summary>The '📍 Click in the Scene to choose where it goes' tick box.</summary>
    private static void DrawClickToPlaceToggle()
    {
        bool click = EditorGUILayout.ToggleLeft(new GUIContent("📍 Click in the Scene to choose where it goes",
            "Ticked: click a button, then click the spot in the Scene view. Unticked: it appears straight away."), ScenePlacer.ClickToPlace);
        if (click != ScenePlacer.ClickToPlace)
        {
            ScenePlacer.ClickToPlace = click;
            if (!click) ScenePlacer.Cancel();
        }
    }

    /// <summary>'Now click in the Scene view...' with a Stop button while placing; otherwise (click to place off) where new things go.</summary>
    private void DrawPlacingStatus(string whenOff)
    {
        bool click = ScenePlacer.ClickToPlace;
        if (ScenePlacer.IsPlacing)
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                GUILayout.Label("👉 Now click in the Scene view to place: " + ScenePlacer.What + "   (Esc to stop)", EditorStyles.boldLabel);
                if (GUILayout.Button("Stop", GUILayout.Width(60))) ScenePlacer.Cancel();
            }
        }
        else if (!click)
        {
            EditorGUILayout.HelpBox(whenOff, MessageType.None);
        }
    }

    /// <summary>
    /// Like ButtonRow, but with 'click to place' on, the button waits for a click in the Scene view
    /// and then does its job at that spot (placeHit / ScatterTools.PlaceAt are set while it runs).
    /// </summary>
    private void PlaceRow(int columns, params (string label, System.Action action)[] buttons)
    {
        int clicked = ButtonGrid(Columns(columns), buttons.Select(b => b.label).ToList());
        if (clicked < 0) return;
        System.Action action = buttons[clicked].action;
        if (!ScenePlacer.ClickToPlace)
        {
            action();
            GUIUtility.ExitGUI();
        }

        // The button's first line, without its emoji: '🕷 Wanderer\nwalks around' -> 'Wanderer'
        string name = buttons[clicked].label.Split('\n')[0];
        int space = name.IndexOf(' ');
        if (space > 0 && space < 4) name = name.Substring(space + 1);

        ScenePlacer.Begin(name, hit =>
        {
            placeHit = hit;
            ScatterTools.PlaceAt = hit.point;
            try { action(); }
            finally
            {
                placeHit = null;
                ScatterTools.PlaceAt = null;
            }
            RefreshSceneLists();
            Repaint();
        });
        GUIUtility.ExitGUI();
    }

    /// <summary>Glow and floating-sparkle settings, or buttons to add them.</summary>
    private void DrawGlow(GameObject go, Color defaultColour)
    {
        if (go == null) return;
        Glow glow = go.GetComponent<Glow>();
        AmbientSparkles sparkles = go.GetComponent<AmbientSparkles>();

        if (glow == null || sparkles == null)
        {
            List<string> labels = new List<string>();
            if (glow == null) labels.Add("💡 Make it glow");
            if (sparkles == null) labels.Add("✨ Add sparkles around it");
            int clicked = ButtonGrid(labels.Count, labels);
            if (clicked >= 0 && labels[clicked].StartsWith("💡")) { ScatterTools.AddGlow(go, defaultColour); GUIUtility.ExitGUI(); }
            else if (clicked >= 0)
            {
                AmbientSparkles s = Undo.AddComponent<AmbientSparkles>(go);
                s.colour = defaultColour;
                GUIUtility.ExitGUI();
            }
        }

        if (glow != null)
        {
            EditorGUILayout.LabelField("💡 Glow", EditorStyles.boldLabel);
            DrawInspector(glow);
            if (GUILayout.Button("Stop glowing")) { Undo.DestroyObjectImmediate(glow); GUIUtility.ExitGUI(); }
        }
        if (sparkles != null)
        {
            EditorGUILayout.LabelField("✨ Sparkles around it (you'll see them when you press Play)", EditorStyles.boldLabel);
            DrawInspector(sparkles);
            if (GUILayout.Button("Remove sparkles")) { Undo.DestroyObjectImmediate(sparkles); GUIUtility.ExitGUI(); }
        }
    }

    /// <summary>The scattered scenery piece (tree, rock...) that's selected, if any.</summary>
    private static Transform SelectedSceneryPiece(SceneryPopulator scenery)
    {
        GameObject go = Selection.activeGameObject;
        Transform root = scenery.transform.Find(SceneryPopulator.ContainerName);
        if (go == null || root == null || !go.transform.IsChildOf(root) || go.transform == root) return null;

        // Walk up to the piece: the child of a group folder
        Transform t = go.transform;
        while (t.parent != null && t.parent.parent != root) t = t.parent;
        return t.parent != null && t.parent.parent == root ? t : null;
    }

    private static string IconFor(PlatformLogic.PlatformType type)
    {
        switch (type)
        {
            case PlatformLogic.PlatformType.Crumbling: return "💥";
            case PlatformLogic.PlatformType.Bouncy: return "🟢";
        }
        return "";
    }

    private static string IconFor(ScatterTools.PlatformMove move)
    {
        switch (move)
        {
            case ScatterTools.PlatformMove.SideToSide: return "↔";
            case ScatterTools.PlatformMove.UpAndDown: return "🛗";
            case ScatterTools.PlatformMove.Spinning: return "🔄";
            case ScatterTools.PlatformMove.Growing: return "🫧";
        }
        return "🪨";
    }

    private void Spawn(ObstacleBase.PayloadType payload, ObstacleSpawnType movement, int amount, float duration, bool usedUp, bool bouncy)
    {
        HashSet<ObstacleBase> before = new HashSet<ObstacleBase>(FindAll<ObstacleBase>());
        // Clicked in the Scene? Then the click decides: on the platform that was clicked, or on the ground
        PlatformLogic platform = placeHit.HasValue ? placeHit.Value.collider.GetComponentInParent<PlatformLogic>() : SelectedOf<PlatformLogic>();
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
            if (payload == ObstacleBase.PayloadType.Damage) onPlatform.baseSettings.objectScale = ScatterTools.NewCreatureSize;
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
            if (payload == ObstacleBase.PayloadType.Damage) ground.baseSettings.objectScale = ScatterTools.NewCreatureSize;
            ground.CreateSingleObstacle();
            JsonUtility.FromJsonOverwrite(saved, ground);
        }

        ObstacleBase created = FindAll<ObstacleBase>().FirstOrDefault(o => !before.Contains(o));
        if (created == null) return;

        created.name = UniqueName(NiceName(created), created); // Wanderer, Wanderer 2, Wanderer 3...
        if (onPlatform == null) MoveToSceneViewGround(created.transform, placeHit?.point);
        else if (placeHit.HasValue) MoveOnPlatform(created, placeHit.Value.point);

        // Pickups glow so players can spot them (click "Stop glowing" to turn it off)
        bool isPickup = payload != ObstacleBase.PayloadType.None && payload != ObstacleBase.PayloadType.Damage;
        if (isPickup) ScatterTools.AddGlow(created.gameObject, Sparkles.ColourFor(payload));

        RefreshSceneLists();
        if (placeHit.HasValue) Selection.activeGameObject = created.gameObject; // already looking there
        else SelectAndFrame(created.gameObject);
    }

    /// <summary>Moves a creature made on a platform from the platform's middle to the clicked spot on it.</summary>
    private static void MoveOnPlatform(ObstacleBase created, Vector3 point)
    {
        FollowPlatform follow = created.GetComponent<FollowPlatform>();
        if (follow == null || follow.targetPlatform == null) return;
        Transform platform = follow.targetPlatform;
        Vector3 offset = Quaternion.Inverse(platform.rotation) * (point - platform.position);
        Undo.RecordObject(follow, "Place");
        Undo.RecordObject(created.transform, "Place");
        follow.offsetX = offset.x;
        follow.offsetZ = offset.z;
        // FollowPlatform only moves it on the next editor update; move it now, so it sits on the right bit of the platform
        created.transform.position = platform.position + platform.rotation * new Vector3(follow.offsetX, follow.offsetY, follow.offsetZ);
        Physics.SyncTransforms(); // so the platform's collider is where the platform is now
        PlatformObstacle po = created as PlatformObstacle;
        if (po != null) po.SitOnPlatformInEditor();
        created.transform.position = platform.position + platform.rotation * new Vector3(follow.offsetX, follow.offsetY, follow.offsetZ);
    }

    /// <summary>New ground creatures appear in the middle of the map; move them to the clicked spot, or where the student is looking.</summary>
    private static void MoveToSceneViewGround(Transform obj, Vector3? clicked = null)
    {
        SceneView view = SceneView.lastActiveSceneView;
        Terrain terrain = obj.GetComponentInParent<Terrain>();
        if (terrain == null) terrain = Find<Terrain>();
        if (terrain == null || (view == null && !clicked.HasValue)) return;

        Vector3 target = clicked ?? view.pivot;
        Transform cam = view != null ? view.camera.transform : null;
        if (!clicked.HasValue && cam != null && Physics.Raycast(cam.position, cam.forward, out RaycastHit hit, 5000f)) target = hit.point;

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

    /// <summary>
    /// Creatures and pickups made by the older scatter buttons are called 'ObstacleLogic' (the prefab's name).
    /// Give them a name that says what they are, e.g. 'Health Pickup' or 'Wanderer'.
    /// Not recorded for Undo on purpose: an Undo step made while the Hierarchy changes would undo the rename,
    /// which renames it again, so Ctrl+Z would get stuck. The scene is marked as changed instead.
    /// </summary>
    private void NameUnnamedObstacles()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        foreach (ObstacleBase o in obstacles)
        {
            if (o == null || !o.name.StartsWith("ObstacleLogic") || !o.gameObject.scene.IsValid()) continue;
            if (PrefabUtility.IsPartOfPrefabAsset(o)) continue;
            o.name = NiceName(o);
            EditorSceneManager.MarkSceneDirty(o.gameObject.scene);
        }
    }

    private static string NiceName(ObstacleBase o)
    {
        return o.KindName(); // the same names as the buttons above the list
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
            // One piece of scenery selected? It can glow on its own
            // Always shown: 'Now click in the Scene view...' + Stop while placing
            DrawPlacingStatus("'📍 Place One' puts one where the Scene view is looking.");

            if (Box("🎲 Scenery tools", "Groups of trees, rocks and plants: pick their models, then Scatter them or Place One.", true))
            {
                DrawClickToPlaceToggle();
                EditorGUILayout.Space(4);
                DrawInspector(scenery, false);
            }
            EndBox();

            // One piece of scenery selected? It can glow on its own
            Transform piece = SelectedSceneryPiece(scenery);
            if (Box("✨ Glow & sparkles (one piece)", "Make one tree, rock or plant glow, or add sparkles around it."))
            {
                if (piece != null)
                {
                    EditorGUILayout.LabelField("Selected: " + piece.name, EditorStyles.boldLabel);
                    DrawGlow(piece.gameObject, new Color(0.5f, 0.9f, 1f));
                }
                else
                {
                    EditorGUILayout.LabelField("Click one tree or rock in the Scene to make just that one glow.", EditorStyles.wordWrappedMiniLabel);
                }
            }
            EndBox();
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

    [System.NonSerialized] private TriggerLogic lastTrigger;

    private void DrawSignsTab()
    {
        Title("💬 Signs & Story", "Pop-up messages, checkpoint flags, and the story and Library your players can read.");

        // 💬 Everything about pop-up signs, in one box
        BeginGroup("💬 Pop-up signs");
        TriggerController triggerController = Find<TriggerController>();
        if (triggerController != null) DrawInspector(triggerController, false);

        EditorGUILayout.Space(6);
        if (Box("📋 Pop-up Signs List", "Click one to change it. Drag them into folders to keep things tidy.", true))
            InsetList(() => DrawList("triggers", triggers, "No pop-ups yet.", t => "💬 " + t.name));
        EndBox();

        TriggerLogic trigger = SelectedOf<TriggerLogic>();
        if (trigger != lastTrigger)
        {
            // Picked a different sign: stop typing, so the old text can't end up in the new sign
            lastTrigger = trigger;
            GUIUtility.keyboardControl = 0;
            EditorGUIUtility.editingTextField = false;
        }
        if (trigger != null)
        {
            EditorGUILayout.Space(6);
            DrawNameAndFolder(trigger, "triggers", triggers, "✏️ Name");
            if (Box("💬 Sign settings", "Its message (or video), the size of its zone, and going to another level."))
                DrawInspector(trigger, false);
            EndBox();
        }
        EndGroup();

        // 🚩 Everything about checkpoints, in one box
        BeginGroup("🚩 Checkpoints");
        EditorGUILayout.HelpBox("If the player dies after touching a flag, they start again from that flag.", MessageType.None);
        SettingsField("useCheckpoints", "Use checkpoints (all levels)");
        if (BigButton("🚩 Create Checkpoint", new Color(0.2f, 0.8f, 0.3f)))
        {
            GameMenuItems.CreateCheckpoint();
            RefreshSceneLists();
        }

        EditorGUILayout.Space(6);
        if (Box("📋 Checkpoints List", "Click one to change it.", true))
            InsetList(() => DrawList("checkpoints", checkpoints, "No checkpoints yet.", c => "🚩 " + c.name));
        EndBox();

        Checkpoint flag = SelectedOf<Checkpoint>();
        if (flag != null)
        {
            EditorGUILayout.Space(6);
            DrawNameAndFolder(flag, "checkpoints", checkpoints, "✏️ Name");
            if (Box("🚩 Checkpoint settings", "This flag's settings."))
                DrawInspector(flag, false);
            EndBox();
        }
        EndGroup();

        GameSettings settings = GameSettings.Load();
        if (Box("📖 Story, Library & game title", "Fonts, the game's title, the death screen and the Library entries (all levels)."))
        {
            if (settings == null)
            {
                if (BigButton("⚙️ Make Game Settings", new Color(0.2f, 0.8f, 0.3f))) GameMenuItems.OpenSettings();
            }
            else DrawInspector(settings, false);
        }
        EndBox();
    }

    // ------------------------------------------------------------------ 📦 Levels & Build

    private void DrawBuildTab()
    {
        Title("📦 Levels & Build", "Your levels, testing, and making the game for others to play.");

        if (Box("🗺 Levels List", "The levels in your game, played in this order.", true)) DrawLevelsList();
        EndBox();
        DrawNewLevelTestShare();
    }

    /// <summary>The levels in the game: tick, ▲ ▼, Open, ✕, and 'Add this level to the game'.</summary>
    private void DrawLevelsList()
    {
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

    }

    /// <summary>Make a new level, Test (Play), and Share (Build Game), each in its own box.</summary>
    private void DrawNewLevelTestShare()
    {
        if (Box("➕ Make a new level", "A new empty level, or a copy of the one you're in."))
        {
        GUI.enabled = !EditorApplication.isPlaying;
        if (string.IsNullOrEmpty(newLevelName)) newLevelName = LevelMaker.NextFreeName();
        newLevelName = EditorGUILayout.TextField("New level's name", newLevelName);
        string nameProblem = LevelMaker.CheckName(newLevelName);
        if (nameProblem != null) EditorGUILayout.HelpBox(nameProblem, MessageType.Warning);
        GUI.enabled = !EditorApplication.isPlaying && nameProblem == null;
        int levelButton = ButtonGrid(2, new[] { "➕ New empty level", "📄 Copy this level" },
            new Color?[] { new Color(0.2f, 0.8f, 0.3f), new Color(0.55f, 0.75f, 1f) });
        if (levelButton == 0 && LevelMaker.CreateNewLevel(newLevelName)) AfterNewLevel();
        if (levelButton == 1 && LevelMaker.CopyCurrentLevel(newLevelName)) AfterNewLevel();
        GUI.enabled = true;
        EditorGUILayout.HelpBox("New empty level: a blank level to build from scratch.\nCopy this level: a copy of the level you're in now, to change into something new.", MessageType.None);

        }
        EndBox();

        if (Box("🎮 Test", "Play this level to try it out."))
        {
        if (!EditorApplication.isPlaying)
        {
            if (BigButton("▶ Play this level", new Color(0.4f, 0.75f, 1f))) EditorApplication.EnterPlaymode();
        }
        else if (BigButton("■ Stop playing", new Color(0.95f, 0.5f, 0.5f)))
        {
            EditorApplication.ExitPlaymode();
        }

        }
        EndBox();

        if (Box("📦 Share your game", "Turn your game into a program anyone can play."))
        {
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
        EndBox();
    }

    // ------------------------------------------------------------------ Helpers

    private void InitStyles()
    {
        if (bigButton != null) return;
        bigButton = new GUIStyle(GUI.skin.button) { fontSize = 14, fontStyle = FontStyle.Bold, wordWrap = true, fixedHeight = 0, padding = new RectOffset(8, 8, 8, 8) };
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
        bool pressed = GUILayout.Button(label, bigButton, GUILayout.MinHeight(ButtonHeight(label)));
        GUI.backgroundColor = old;
        return pressed;
    }

    private static float ButtonHeight(string label) => label.Contains('\n') ? 60 : 46;

    /// <summary>
    /// Big buttons in equal columns, so every row lines up with the one above it and with the
    /// full-width buttons. Returns the index of the button clicked, or -1.
    /// </summary>
    private int ButtonGrid(int columns, IList<string> labels, IList<Color?> colours = null)
    {
        int clicked = -1;
        if (labels.Count == 0) return clicked;
        columns = Mathf.Max(1, columns);
        float height = labels.Any(l => l.Contains('\n')) ? ButtonHeight("\n") : ButtonHeight("");
        const float Gap = 4f;
        for (int start = 0; start < labels.Count; start += columns)
        {
            Rect row = GUILayoutUtility.GetRect(0, height + Gap, GUILayout.ExpandWidth(true));
            // Same edges as a normal full-width button (its margins), and the same gap between rows
            row = new Rect(row.x + bigButton.margin.left, row.y + Gap / 2, row.width - bigButton.margin.horizontal, height);
            float cell = (row.width - Gap * (columns - 1)) / columns;
            for (int i = start; i < Mathf.Min(start + columns, labels.Count); i++)
            {
                Rect r = new Rect(row.x + (i - start) * (cell + Gap), row.y, cell, row.height);
                Color old = GUI.backgroundColor;
                if (colours != null && i < colours.Count && colours[i].HasValue) GUI.backgroundColor = colours[i].Value;
                if (GUI.Button(r, labels[i], bigButton)) clicked = i;
                GUI.backgroundColor = old;
            }
        }
        return clicked;
    }

    /// <summary>Columns for a grid: fewer when the window is narrow.</summary>
    private int Columns(int wanted) => position.width > 700 ? wanted : Mathf.Min(wanted, 2);

    private void ButtonRow(int columns, params (string label, System.Action action)[] buttons)
    {
        int clicked = ButtonGrid(Columns(columns), buttons.Select(b => b.label).ToList());
        if (clicked >= 0)
        {
            buttons[clicked].action();
            GUIUtility.ExitGUI();
        }
    }

    /// <summary>
    /// A fold-out in its own box (Editor/FoldoutBar.cs): the teal bar on top, the contents inside.
    /// Always call EndFoldout() afterwards, open or not.
    /// </summary>
    private bool BeginFoldout(string key, string label, bool startOpen = false)
    {
        if (!foldouts.ContainsKey(key)) foldouts[key] = startOpen;
        bool open = foldouts[key];
        EditorGUILayout.Space(8);
        open = FoldoutBar.Begin(open, label);
        foldouts[key] = open;
        return open;
    }

    private static void EndFoldout()
    {
        FoldoutBar.End();
    }

    /// <summary>True while a component's Inspector is being drawn inside the Game Builder (not in Unity's Inspector).</summary>
    public static bool DrawingInGameBuilder { get; private set; }

    /// <summary>Draws a component's normal Inspector (with its green buttons) inside this window.</summary>
    /// <param name="boxed">False when it's already inside a fold-out's box.</param>
    private void DrawInspector(Object target, bool boxed = true)
    {
        if (target == null) return;
        if (!editors.TryGetValue(target, out Editor ed) || ed == null || ed.target == null)
        {
            ed = Editor.CreateEditor(target);
            editors[target] = ed;
        }

        using (new EditorGUILayout.VerticalScope(boxed ? EditorStyles.helpBox : GUIStyle.none))
        {
            // Lets a panel hide things the Game Builder already shows another way
            bool wasDrawing = DrawingInGameBuilder; // one inspector can be drawn inside another (How it moves)
            DrawingInGameBuilder = true;
            try
            {
                // Components without their own editor (PlayerLogic, Health, FallDamage...) use Unity's generic
                // Inspector, which always shows the greyed 'Script' box. Draw those ourselves without it.
                if (ed.GetType().Name == "GenericInspector") KidInspector.DrawDefault(ed.serializedObject);
                else ed.OnInspectorGUI();
            }
            finally { DrawingInGameBuilder = wasDrawing; }
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

        HashSet<T> selected = SelectedAll<T>();
        List<Component> all = items.Cast<Component>().ToList();
        List<Component> chosen = selected.Cast<Component>().ToList();
        if (Event.current.type == EventType.DragExited) { dragItems = null; dropTarget = null; }

        // Things without a folder first, then each folder (📁) A to Z. Inside each: your order (▲ ▼ / drag), then by name.
        List<Component> loose = Ordered(all.Where(i => FolderOf(i) == ""));
        List<string> folders = all.Select(FolderOf).Where(f => f != "").Distinct()
            .OrderBy(f => f, System.StringComparer.OrdinalIgnoreCase).ToList();

        int rows = loose.Count + folders.Sum(f => 1 + (FolderOpen(key, f) ? all.Count(i => FolderOf(i) == f) : 0));
        listScrolls.TryGetValue(key, out Vector2 listScroll);
        bool scrolls = rows > 9;
        if (scrolls) listScroll = EditorGUILayout.BeginScrollView(listScroll, EditorStyles.helpBox, GUILayout.Height(230));
        else EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        // While dragging something that's in a folder: a strip to drop it back out
        if (dragItems != null && dragKey == key && dragItems.Any(d => FolderOf(d) != ""))
        {
            Rect strip = GUILayoutUtility.GetRect(new GUIContent("x"), rowStyle, GUILayout.ExpandWidth(true));
            GUI.Box(strip, "⬆ Drop here to take it out of its folder", EditorStyles.helpBox);
            HandleDrop(key, strip, "", null, all, true);
        }

        foreach (Component item in loose) DrawListRow(key, item, loose, "", selected.Contains((T)item), label((T)item), 0, chosen, all);

        foreach (string folder in folders)
        {
            List<Component> inside = Ordered(all.Where(i => FolderOf(i) == folder));
            if (DrawFolderRow(key, folder, inside.Count, inside, all))
            {
                foreach (Component item in inside) DrawListRow(key, item, inside, folder, selected.Contains((T)item), label((T)item), 18, chosen, all);
            }
        }

        if (scrolls) EditorGUILayout.EndScrollView();
        else EditorGUILayout.EndVertical();
        listScrolls[key] = listScroll;
        if (dragItems != null) EditorGUILayout.LabelField("Drop it on a folder, or on a row to put it just above that row.", EditorStyles.miniLabel);
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

    private void AfterNewLevel()
    {
        newLevelName = null; // suggest the next free name next time
        tab = Tab.Level;
        EditorPrefs.SetInt(TabKey, (int)tab);
        scroll = Vector2.zero;
        RefreshSceneLists();
        GUIUtility.ExitGUI();
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
