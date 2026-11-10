using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Builds "Tutorial Island": a playable level where walking into each yellow sign pops up an
/// explanation of how that part of the level was made in the Editor (using the same buttons
/// students use). Rebuild it any time from Game > Tutorial > Build Tutorial Level.
/// </summary>
public static class TutorialLevelBuilder
{
    public const string ScenePath = "Assets/Scenes/Tutorial.unity";
    private const string TemplateScene = LevelMaker.TemplatePath; // the empty level
    internal const string AssetFolder = "Assets/Tutorial";
    internal const string NatureFolder = "Assets/MyModels/NatureModelsPackTest/FBX/";

    // Course runs along +Z from the start point, on a flattened meadow
    private static readonly Vector3 Start = new Vector3(20f, 0f, -40f);
    private const float MeadowHeight = 1f; // metres above the valley floor

    [MenuItem("Game/Tutorial/Open Tutorial Level", priority = 40)]
    public static void OpenTutorial()
    {
        if (!File.Exists(ScenePath))
        {
            if (EditorUtility.DisplayDialog("No tutorial yet", "The tutorial level hasn't been built yet. Build it now?", "Build it", "Cancel")) BuildMenu();
            return;
        }
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(ScenePath);
    }

    [MenuItem("Game/Tutorial/Build Tutorial Level", priority = 41)]
    public static void BuildMenu()
    {
        if (File.Exists(ScenePath) && !EditorUtility.DisplayDialog("Rebuild tutorial?",
                "This replaces the Tutorial level with a fresh copy. Any changes made to it will be lost.", "Rebuild", "Cancel")) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        string result = Build();
        EditorUtility.DisplayDialog("Tutorial Island", result + "\n\nPress Play and walk into the yellow signs!", "OK");
    }

    [MenuItem("Game/Tutorial/Build Tutorial Game (for the class)", priority = 42)]
    public static void BuildTutorialGameMenu()
    {
        if (!File.Exists(ScenePath) && !EditorUtility.DisplayDialog("No tutorial yet", "Build the Tutorial level first?", "Build it", "Cancel")) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (!File.Exists(ScenePath)) Build();

        UnityEditor.Build.Reporting.BuildReport report = BuildTutorialGame();
        bool ok = report != null && report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded;
        if (ok && EditorUtility.DisplayDialog("Tutorial game ready", "Tutorial Island is in Build/Tutorial.\nCopy that WHOLE folder to share it, then double-click TutorialIsland.exe.", "Show me", "OK"))
        {
            EditorUtility.RevealInFinder(Path.Combine(TutorialBuildFolder, "TutorialIsland.exe"));
        }
        else if (!ok)
        {
            EditorUtility.DisplayDialog("Build failed", "The tutorial game didn't build. Check the Console for red errors.", "OK");
        }
    }

    public static string TutorialBuildFolder => Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Build", "Tutorial");

    /// <summary>Builds a game with ONLY the tutorial level into Build/Tutorial (the class's level order isn't touched).</summary>
    public static UnityEditor.Build.Reporting.BuildReport BuildTutorialGame()
    {
        // Start clean, so no old files are left in the folder
        if (Directory.Exists(TutorialBuildFolder)) Directory.Delete(TutorialBuildFolder, true);

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            // Tutorial Island first, then Adventure Park (its Finish sign leads there) if it's been built
            scenes = File.Exists(ShowcaseLevelBuilder.ScenePath) ? new[] { ScenePath, ShowcaseLevelBuilder.ScenePath } : new[] { ScenePath },
            locationPathName = Path.Combine(TutorialBuildFolder, "TutorialIsland.exe"),
            target = BuildTarget.StandaloneWindows64,
            targetGroup = BuildTargetGroup.Standalone,
            options = BuildOptions.None
        };
        UnityEditor.Build.Reporting.BuildReport report = BuildPipeline.BuildPlayer(options);

        // Unity's debugging folder isn't needed to play (and says "DoNotShip")
        foreach (string dir in Directory.GetDirectories(TutorialBuildFolder, "*DoNotShip"))
        {
            Directory.Delete(dir, true);
        }
        Debug.Log("Tutorial game build " + report.summary.result + ": " + TutorialBuildFolder);
        return report;
    }

    /// <summary>Builds and saves the tutorial scene. Returns a short report.</summary>
    public static string Build()
    {
        if (!AssetDatabase.IsValidFolder(AssetFolder)) AssetDatabase.CreateFolder("Assets", "Tutorial");

        // 1. Start from the empty template level (it has all the controllers, HUD and pop-up dialog)
        // Overwrite the file in place (rather than delete + copy) so the scene keeps its GUID
        if (File.Exists(ScenePath))
        {
            File.Copy(TemplateScene, ScenePath, true);
            AssetDatabase.ImportAsset(ScenePath, ImportAssetOptions.ForceUpdate);
        }
        else
        {
            AssetDatabase.CopyAsset(TemplateScene, ScenePath);
        }
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        // 2. Terrain: its own copy (so Level02's terrain isn't touched), with a meadow for the course
        // Flatten a meadow for the course, blending smoothly into the mountains
        Terrain terrain = CreateTutorialTerrain(AssetFolder + "/TutorialTerrain.asset", new Rect(Start.x - 35f, Start.z - 25f, 70f, 295f), MeadowHeight);
        float G(float x, float z) => terrain.SampleHeight(new Vector3(x, 0f, z)) + terrain.GetPosition().y;
        Vector3 At(float side, float forward, float up = 0f) => new Vector3(Start.x + side, G(Start.x + side, Start.z + forward) + up, Start.z + forward);

        Transform signs = new GameObject("Tutorial Signs").transform;
        Material signMaterial = GetSignMaterial();

        // 3. Player (the default model, the Old Man), facing down the course
        PlayerController player = Object.FindAnyObjectByType<PlayerController>();
        player.transform.SetPositionAndRotation(At(0f, -4f, 0.2f), Quaternion.identity);
        player.CreateCharacterMesh();

        // ---- Stations ----------------------------------------------------------------------
        Sign(signs, signMaterial, At(-4f, 0f), At(0f, 0f, 1.5f), "1. Welcome!",
            "Welcome to Tutorial Island! Each yellow sign shows how this level was made with Game > Game Builder.\nWASD = move,  Space = jump,  Shift = sprint,  Esc = menu");

        Sign(signs, signMaterial, At(-4f, 8f), At(0f, 8f, 1.5f), "2. Pop-up signs",
            "Pop-ups like this: Game Builder > Signs & Story > Create Pop-up Sign, then type your Message.\nTick 'Go to another level when touched' to send players somewhere new.");

        // Videos: one sign explains, the next one plays a video
        Sign(signs, signMaterial, At(-4f, 15f), At(0f, 15f, 1.5f), "3. Videos",
            "Signs can play a VIDEO too: walk into the 'Watch me!' sign next. To make one, set a sign's Pop-up to Video,\nput an .mp4 in Assets/MyVideos and drag it in. Full screen pauses the game; untick it for a small corner video.");
        GameObject videoSign = Sign(signs, signMaterial, At(-4f, 22f), At(0f, 22f, 1.5f), "Watch me!", "");
        TriggerLogic video = videoSign.GetComponent<TriggerLogic>();
        video.displayText = true;
        video.popupType = TriggerLogic.PopupType.Video;
        video.videoClip = AssetDatabase.LoadAssetAtPath<UnityEngine.Video.VideoClip>("Assets/MyVideos/TestVideo.mp4");
        video.pauseGameWhileVideo = true;
        video.playOnlyOnce = true;

        Sign(signs, signMaterial, At(-4f, 30f), At(0f, 30f, 1.5f), "4. This is you",
            "You are the Old Man. To change your look: Game Builder > Player, drag a model\ninto 'Player Model'. Green bar = health (falls and creatures hurt), yellow = stamina.");

        // Scenery grove around station 5
        SceneryPopulator scenery = BuildScenery(terrain, At);
        Sign(signs, signMaterial, At(-4f, 42f), At(0f, 42f, 1.5f), "5. Scenery",
            "These trees and rocks came from Game Builder > Scenery:\ntype words like Tree into 'Find models named', click Add, then Scatter.");

        // Glow & sparkles corner
        BuildGlowCorner(scenery, At);
        Sign(signs, signMaterial, At(-4f, 60f), At(0f, 60f, 1.5f), "6. Glow & sparkles",
            "That rock GLOWS and sparkles! Select anything in the Game Builder and click 'Make it glow' or 'Add sparkles around it'.\nPick a style (Swirl, Fireflies, Sparks, Shimmer...) and where they come from. Scenery groups can glow too.");

        // Platforms: plain, moving, rock
        PlatformController platforms = Object.FindAnyObjectByType<PlatformController>();
        Sign(signs, signMaterial, At(-6f, 72f), At(0f, 72f, 1.5f), "7. Platforms",
            "Game Builder > Platforms: click a Quick platform (Floating, Moving, Elevator...), then click in the Scene where it goes.\n'New platforms float this high' sets how high. 'Move it...' moves one later. 'Scatter' makes lots at once.");
        GameObject p1 = MakePlatform(platforms, At(0f, 82f), 1.2f, 6f);
        GameObject p2 = MakePlatform(platforms, At(0f, 91f), 2.4f, 5f);
        PlatformLogic moving = p2.GetComponent<PlatformLogic>();
        moving.moveX = true; moving.moveZ = false; moving.moveDistance = 6f; moving.moveSpeed = 1.5f;
        GameObject p3 = MakePlatform(platforms, At(0f, 100f), 3.4f, 6f);
        GameObject rock = AssetDatabase.LoadAssetAtPath<GameObject>(NatureFolder + "Rock_Moss_6.fbx");
        if (rock != null) PlatformController.ApplyPlatformModel(p3, rock);
        float rockTop = TopOf(p3);
        Sign(signs, signMaterial, At(-6f, 100f), new Vector3(Start.x, rockTop + 1.5f, Start.z + 100f), "8. Rock platform",
            "This platform is a ROCK! Game Builder > Platforms: click a platform, drag a model\ninto 'Platform Model'. You stand on its real shape, bumps and all.");

        // Bouncy platform (low, so it's easy to step onto)
        GameObject bouncy = MakePlatform(platforms, At(0f, 116f), 0.6f, 5f);
        PlatformLogic bouncyLogic = bouncy.GetComponent<PlatformLogic>();
        ScatterTools.SetPlatformType(bouncyLogic, PlatformLogic.PlatformType.Bouncy);
        bouncyLogic.bounceStrength = 22f;
        bouncy.name = "Bouncy Platform";
        Sign(signs, signMaterial, At(-6f, 111f), At(0f, 111f, 1.5f), "9. Bouncy",
            "BOUNCY platform: stand on the green one! Make one: click a platform, then Special > Bouncy.\nChange Bounce Strength, and pick a Land Effect for sparkles when you land (any platform can have one).");

        // Crumbling path: low, so falling off doesn't hurt
        for (int i = 0; i < 3; i++)
        {
            GameObject crumble = MakePlatform(platforms, At(0f, 131f + i * 6f), 1.5f, 3.5f);
            PlatformLogic crumbleLogic = crumble.GetComponent<PlatformLogic>();
            ScatterTools.SetPlatformType(crumbleLogic, PlatformLogic.PlatformType.Crumbling);
            crumbleLogic.comeBackAfter = 3f;
            crumble.name = "Crumbling Platform " + (i + 1);
        }
        Sign(signs, signMaterial, At(-6f, 125f), At(0f, 125f, 1.5f), "10. Crumbling",
            "CRUMBLING platforms: they flash once a second, then fall... then come back! Make one: Special > Crumbling.\nSet Crumble After, Come Back After (0 = never), Warning Flash and a Break Effect. Be quick!");

        // Creature arena
        GameObject arena = MakePlatform(platforms, At(0f, 158f), 3.4f, 12f);
        float arenaTop = TopOf(arena);
        AddCreatures(arena);
        Sign(signs, signMaterial, At(-9f, 158f), new Vector3(Start.x, arenaTop + 1.5f, Start.z + 155f), "11. Creatures",
            "Careful, creatures hurt if you run into them! Game Builder > Creatures & Pickups: click a creature, then click where it goes.\n'Scatter lots at once' puts many on the ground or on platforms. The glowing ball is Health.");

        // Stomp practice: one creature standing still on the ground
        AddStompCreature(At(0f, 180f));
        Sign(signs, signMaterial, At(-5f, 174f), At(0f, 174f, 1.5f), "12. Jump on it!",
            "JUMP ON ITS HEAD to beat it! This one needs 2 jumps. Click a creature and set 'Jumps On Head To Beat'\n(0 = it can't be beaten). 'Sparkle Burst' picks the effect when it's beaten or picked up.\n" +
            "Its Hit zone hurts from the sides only, so land on top! 'How it moves' and 'Chase the player' make creatures walk, patrol or chase you.");

        // Bounce ball on the ground
        AddBouncePad(terrain, At(0f, 196f));
        Sign(signs, signMaterial, At(-5f, 190f), At(0f, 190f, 1.5f), "13. Bounce!",
            "Jump onto the ball to get launched sky-high! Creatures & Pickups > Bounce Ball.\nAny creature can bounce too: tick 'Is Bouncy', and raise Launch Force to go higher.");

        // Checkpoint
        GameMenuItems.CreateCheckpoint();
        GameObject flag = Selection.activeGameObject;
        flag.transform.position = At(3f, 206f);
        Sign(signs, signMaterial, At(-4f, 206f), At(0f, 206f, 1.5f), "14. Checkpoint",
            "Checkpoint flags save your place: Game Builder > Signs & Story > Create Checkpoint.\nIf you die after touching a flag, you start again from there.");

        Sign(signs, signMaterial, At(-4f, 218f), At(0f, 218f, 1.5f), "15. Library & Sounds",
            "Esc > Library: the Story. Esc > Sound & Music: volume and songs.\nGame Builder > Sounds: drag in sounds for jumping, creatures, pickups and music.");

        Sign(signs, signMaterial, At(-4f, 230f), At(0f, 230f, 1.5f), "16. Levels",
            "Game Builder > Levels & Build: 'New empty level' or 'Copy this level' makes a new level.\nUse the arrows to change the order, untick one to leave it out. The star level is where the game starts.");

        GameObject finish = Sign(signs, signMaterial, At(-4f, 244f), At(0f, 244f, 1.5f), "17. Finish!",
            "You finished Tutorial Island! Next stop: ADVENTURE PARK, where you'll see every feature in action...\n(This sign uses 'Go to another level when touched'.)");
        TriggerLogic finishTrigger = finish.GetComponent<TriggerLogic>();
        finishTrigger.changeScene = true;
        finishTrigger.sceneToLoad = ShowcaseLevelBuilder.SceneName;

        // 4. Save and add to the build (after the student's levels)
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        AddToBuild();

        int sceneryCount = scenery.transform.Find(SceneryPopulator.ContainerName)?.GetComponentsInChildren<Renderer>().Length ?? 0;
        return $"Built Tutorial Island: 17 signs (one plays a video), 8 platforms (bouncy and crumbling too), a glow & sparkles corner, creatures, a bounce pad, a checkpoint and {sceneryCount} scenery pieces.";
    }

    // ------------------------------------------------------------------ Terrain

    /// <summary>Creates the level's terrain with its own copy of the ground (dataPath) and a flat area for the course.</summary>
    internal static Terrain CreateTutorialTerrain(string dataPath, Rect flatArea, float flatHeight)
    {
        EnvironmentController env = Object.FindAnyObjectByType<EnvironmentController>();
        env.CreateTerrain();
        Terrain terrain = env.GetComponentInChildren<Terrain>();

        // Own copy of the terrain data, so sculpting here never changes other levels
        AssetDatabase.DeleteAsset(dataPath);
        TerrainData copy = Object.Instantiate(terrain.terrainData);
        AssetDatabase.CreateAsset(copy, dataPath);
        terrain.terrainData = copy;
        TerrainCollider tc = terrain.GetComponent<TerrainCollider>();
        if (tc != null) tc.terrainData = copy;

        FlattenArea(terrain, flatArea, flatHeight, 30f);
        return terrain;
    }

    internal static void FlattenArea(Terrain terrain, Rect area, float height, float blend)
    {
        TerrainData data = terrain.terrainData;
        Vector3 origin = terrain.GetPosition();
        int res = data.heightmapResolution;
        float[,] heights = data.GetHeights(0, 0, res, res);
        float target = height / data.size.y;

        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                float worldX = origin.x + x / (float)(res - 1) * data.size.x;
                float worldZ = origin.z + y / (float)(res - 1) * data.size.z;

                // Distance outside the rectangle (0 inside)
                float dx = Mathf.Max(area.xMin - worldX, 0f, worldX - area.xMax);
                float dz = Mathf.Max(area.yMin - worldZ, 0f, worldZ - area.yMax);
                float distance = Mathf.Sqrt(dx * dx + dz * dz);
                if (distance >= blend) continue;

                float t = Mathf.SmoothStep(0f, 1f, distance / blend); // 0 = fully flat, 1 = untouched
                heights[y, x] = Mathf.Lerp(target, heights[y, x], t);
            }
        }
        data.SetHeights(0, 0, heights);
    }

    // ------------------------------------------------------------------ Scenery

    private static SceneryPopulator BuildScenery(Terrain terrain, System.Func<float, float, float, Vector3> at)
    {
        TerrainPopulator populator = Object.FindAnyObjectByType<TerrainPopulator>();
        SceneryPopulator scenery = populator.GetComponent<SceneryPopulator>();
        if (scenery == null) scenery = Undo.AddComponent<SceneryPopulator>(populator.gameObject);
        if (scenery.groups.Count == 0) typeof(SceneryPopulator).GetMethod("Reset", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(scenery, null);
        foreach (SceneryPopulator.SceneryGroup group in scenery.groups) scenery.QuickFill(group);

        // A little grove around the scenery station (both sides of the path, never on it)
        SceneryPopulator.SceneryGroup trees = scenery.groups.Find(g => g.name == "Trees");
        SceneryPopulator.SceneryGroup rocks = scenery.groups.Find(g => g.name == "Rocks");
        SceneryPopulator.SceneryGroup plants = scenery.groups.Find(g => g.name == "Plants & Grass");
        Random.InitState(42); // same layout every time it's rebuilt
        float[] treeSpots = { 34f, 39f, 45f, 50f, 55f };
        foreach (float z in treeSpots)
        {
            if (trees != null) { scenery.PlaceOne(trees, at(-12f + Random.Range(-2f, 2f), z, 0f)); scenery.PlaceOne(trees, at(12f + Random.Range(-2f, 2f), z + 2f, 0f)); }
            if (rocks != null) scenery.PlaceOne(rocks, at(Random.value < 0.5f ? -8f : 8f, z + 1f, 0f));
            if (plants != null) { scenery.PlaceOne(plants, at(-6f, z, 0f)); scenery.PlaceOne(plants, at(6f, z + 3f, 0f)); }
        }

        // Scatter the rest of the island, then keep the course itself clear
        if (trees != null) { trees.howMany = 120; scenery.Scatter(trees); }
        if (rocks != null) { rocks.howMany = 60; scenery.Scatter(rocks); }
        if (plants != null) { plants.howMany = 250; scenery.Scatter(plants); }
        ClearCourse(scenery.transform.Find(SceneryPopulator.ContainerName), new Rect(Start.x - 7f, Start.z - 10f, 14f, 265f), new Rect(Start.x - 16f, Start.z + 30f, 32f, 32f));
        Random.InitState(System.Environment.TickCount);
        return scenery;
    }

    /// <summary>A glowing "crystal" rock and a few sparkly things to show off the glow & sparkle options.</summary>
    private static void BuildGlowCorner(SceneryPopulator scenery, System.Func<float, float, float, Vector3> at)
    {
        SceneryPopulator.SceneryGroup corner = new SceneryPopulator.SceneryGroup { name = "Glow & Sparkles Corner", solid = true };

        GameObject Piece(string model, float side, float forward, float size)
        {
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(NatureFolder + model + ".fbx");
            if (asset == null) return null;
            corner.models = new List<GameObject> { asset };
            corner.sizeRange = new Vector2(size, size);
            GameObject go = scenery.PlaceOne(corner, at(side, forward, 0f));
            // Glowing things can't be merged by static batching
            if (go != null) GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
            return go;
        }

        AmbientSparkles Sparkle(GameObject go, AmbientSparkles.Style style, Color colour, bool top, bool sides, bool centre, float amount)
        {
            if (go == null) return null;
            AmbientSparkles s = go.AddComponent<AmbientSparkles>();
            s.style = style; s.colour = colour; s.fromTop = top; s.fromSides = sides; s.fromBottom = false; s.fromCentre = centre; s.amount = amount;
            return s;
        }

        // The "crystal": a snowy rock that glows cyan, with sparkles swirling round it
        GameObject crystal = Piece("Rock_Snow_3", 9f, 62f, 2.2f);
        if (crystal != null)
        {
            Glow glow = crystal.AddComponent<Glow>();
            glow.colour = new Color(0.4f, 0.9f, 1f);
            glow.strength = 2f;
            glow.lightRange = 7f;
            glow.pulsesPerSecond = 0.5f;
            Sparkle(crystal, AmbientSparkles.Style.Swirl, glow.colour, false, true, false, 25f);
        }

        // A tree full of fireflies, a shimmering stump and a flower fountain
        Sparkle(Piece("CommonTree_Autumn_2", 15f, 66f, 1.8f), AmbientSparkles.Style.Fireflies, new Color(1f, 0.9f, 0.4f), true, true, false, 20f);
        Sparkle(Piece("TreeStump_Moss", 8f, 68f, 1.6f), AmbientSparkles.Style.Shimmer, new Color(0.5f, 1f, 0.5f), true, false, false, 15f);
        Sparkle(Piece("Flowers", 11f, 57f, 1.8f), AmbientSparkles.Style.Fountain, new Color(1f, 0.5f, 0.8f), false, false, true, 15f);
    }

    /// <summary>Removes scattered scenery that landed on the path (but keeps the hand-placed grove).</summary>
    private static void ClearCourse(Transform sceneryRoot, Rect path, Rect grove)
    {
        if (sceneryRoot == null) return;
        List<GameObject> remove = new List<GameObject>();
        foreach (Transform group in sceneryRoot)
        {
            foreach (Transform piece in group)
            {
                Vector2 p = new Vector2(piece.position.x, piece.position.z);
                bool onPath = path.Contains(p);
                bool inGrove = grove.Contains(p);
                if (onPath && !(inGrove && Mathf.Abs(p.x - Start.x) > 5f)) remove.Add(piece.gameObject);
            }
        }
        foreach (GameObject go in remove) Object.DestroyImmediate(go);
    }

    // ------------------------------------------------------------------ Platforms & obstacles

    /// <summary>Creates a platform whose top is 'topHeight' above the ground point, about 'width' metres across.</summary>
    internal static GameObject MakePlatform(PlatformController controller, Vector3 groundPoint, float topHeight, float width)
    {
        controller.CreatePlatform();
        GameObject root = Selection.activeGameObject;
        PlatformLogic logic = root.GetComponent<PlatformLogic>();
        Transform target = logic.TargetChild != null ? logic.TargetChild : root.transform.Find("⚠️ DO NOT TOUCH").GetChild(0);

        // Measure how big the platform shape is per unit of Width, then size it
        target.localScale = Vector3.one;
        Bounds unit = RendererBounds(target);
        float perUnitWidth = Mathf.Max(unit.size.x, 0.01f);
        float perUnitHeight = Mathf.Max(unit.size.y, 0.01f);
        logic.widthOffset = logic.depthOffset = width / perUnitWidth;
        logic.heightOffset = Mathf.Max(0.2f, 0.6f / perUnitHeight);
        target.localScale = new Vector3(logic.widthOffset, logic.heightOffset, logic.depthOffset);
        target.localPosition = new Vector3(logic.offsetX, logic.offsetY, logic.offsetZ);

        // Place it so its top is at the wanted height
        root.transform.position = groundPoint;
        float top = RendererBounds(target).max.y;
        root.transform.position += Vector3.up * (groundPoint.y + topHeight - top);
        root.name = "Tutorial Platform";
        return root;
    }

    internal static float TopOf(GameObject platform)
    {
        PlatformLogic logic = platform.GetComponent<PlatformLogic>();
        Transform target = logic.TargetChild != null ? logic.TargetChild : platform.transform.Find("⚠️ DO NOT TOUCH").GetChild(0);
        return RendererBounds(target).max.y;
    }

    internal static Bounds RendererBounds(Transform root)
    {
        Bounds b = new Bounds(root.position, Vector3.zero);
        bool found = false;
        foreach (Renderer r in root.GetComponentsInChildren<Renderer>())
        {
            if (!r.enabled) continue;
            if (!found) { b = r.bounds; found = true; } else b.Encapsulate(r.bounds);
        }
        return b;
    }

    private static void AddCreatures(GameObject arena)
    {
        PlatformPopulator populator = arena.GetComponent<PlatformPopulator>();
        if (populator == null) return;
        populator.customModel = null;
        populator.customMesh = null;

        // A wandering spider and a patrolling zombie (default enemy models)
        populator.baseSettings.payloadType = ObstacleBase.PayloadType.Damage;
        populator.baseSettings.payloadAmount = 10;
        populator.baseSettings.isBouncy = false;
        populator.baseSettings.destroyOnTrigger = false;
        populator.baseSettings.objectScale = 1.2f;
        populator.wanderSettings.wanderRadius = 4f;
        populator.wanderSettings.minMoveSpeed = 1.5f;
        populator.wanderSettings.maxMoveSpeed = 2.5f;
        populator.patrolSettings.moveDistance = 6f;
        populator.patrolSettings.minMoveSpeed = 1.5f;
        populator.patrolSettings.maxMoveSpeed = 2f;

        populator.scatterObstacleType = ObstacleSpawnType.Wander;
        populator.CreateSingleObstacle();
        Nudge(Selection.activeGameObject, 2.5f, 1f);

        populator.scatterObstacleType = ObstacleSpawnType.Patrol;
        populator.CreateSingleObstacle();
        Nudge(Selection.activeGameObject, -2.5f, -2f);

        // A healing pickup (keeps its glowing ball shape)
        populator.baseSettings.payloadType = ObstacleBase.PayloadType.HealthBooster;
        populator.baseSettings.payloadAmount = 30;
        populator.baseSettings.destroyOnTrigger = true;
        populator.baseSettings.objectScale = 0.7f;
        populator.scatterObstacleType = ObstacleSpawnType.Static;
        populator.CreateSingleObstacle();
        Nudge(Selection.activeGameObject, 0f, 4f);
        if (Selection.activeGameObject != null)
        {
            // It glows, with sparkles rising from it, so players spot it
            Glow glow = Selection.activeGameObject.AddComponent<Glow>();
            glow.colour = Sparkles.ColourFor(ObstacleBase.PayloadType.HealthBooster);
            AmbientSparkles sparkle = Selection.activeGameObject.AddComponent<AmbientSparkles>();
            sparkle.colour = glow.colour;
            sparkle.fromTop = false;
            sparkle.fromSides = false;
            sparkle.fromCentre = true;
            sparkle.amount = 12f;
        }
    }

    internal static void Nudge(GameObject obstacle, float x, float z)
    {
        if (obstacle == null) return;
        FollowPlatform follow = obstacle.GetComponent<FollowPlatform>();
        if (follow != null) { follow.offsetX += x; follow.offsetZ += z; }
        PlatformObstacle po = obstacle.GetComponent<PlatformObstacle>();
        if (po != null) po.SitOnPlatformInEditor();
    }

    /// <summary>A creature that stands still on the ground and takes 2 jumps on its head to beat.</summary>
    private static void AddStompCreature(Vector3 groundPoint)
    {
        TerrainPopulator populator = Object.FindAnyObjectByType<TerrainPopulator>();
        Terrain terrain = Object.FindAnyObjectByType<Terrain>();
        // A zombie: tall, so it's easy to land on (the default still creature is a flat snake)
        populator.customModel = GameSettings.Load().DefaultObstacleModel(ObstacleBase.PayloadType.Damage, ObstacleSpawnType.Patrol);
        populator.customMesh = null;
        populator.scatterObstacleType = ObstacleSpawnType.Static;
        populator.baseSettings.payloadType = ObstacleBase.PayloadType.Damage;
        populator.baseSettings.payloadAmount = 10;
        populator.baseSettings.isBouncy = false;
        populator.baseSettings.destroyOnTrigger = false;
        populator.baseSettings.objectScale = 1.2f;
        populator.CreateSingleObstacle();

        GameObject creature = Selection.activeGameObject;
        if (creature != null)
        {
            creature.name = "Jump On Me";
            // It was made in the middle of the map: move it, keeping its height above the ground
            float lift = creature.transform.position.y - (terrain.SampleHeight(creature.transform.position) + terrain.GetPosition().y);
            creature.transform.position = groundPoint + Vector3.up * lift;
            ObstacleBase obstacle = creature.GetComponent<ObstacleBase>();
            obstacle.jumpsToDefeat = 2;
            obstacle.sparkleStyle = ObstacleBase.SparkleStyle.Explosion;
            obstacle.initialYRotation = 180f; // facing the player coming down the path
            creature.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        }
        populator.customModel = null;
        populator.baseSettings.objectScale = 1f;
    }

    private static void AddBouncePad(Terrain terrain, Vector3 groundPoint)
    {
        TerrainPopulator populator = Object.FindAnyObjectByType<TerrainPopulator>();
        populator.customModel = null;
        populator.customMesh = null;
        populator.scatterObstacleType = ObstacleSpawnType.Static;
        populator.baseSettings.payloadType = ObstacleBase.PayloadType.None;
        populator.baseSettings.isBouncy = true;
        populator.baseSettings.launchForce = 30f;
        populator.baseSettings.objectScale = 1.4f; // low enough to jump onto
        populator.CreateSingleObstacle();
        GameObject pad = Selection.activeGameObject;
        if (pad != null)
        {
            pad.name = "Bounce Pad";
            pad.transform.position = groundPoint;
        }
        populator.baseSettings.isBouncy = false;
        populator.baseSettings.payloadType = ObstacleBase.PayloadType.Damage;
        populator.baseSettings.objectScale = 1f;
    }

    // ------------------------------------------------------------------ Signs

    /// <summary>A yellow signpost with a title, plus a pop-up trigger with the explanation.</summary>
    internal static GameObject Sign(Transform parent, Material material, Vector3 postGround, Vector3 triggerCentre, string title, string message)
    {
        // Signpost
        GameObject post = new GameObject("Sign - " + title);
        post.transform.SetParent(parent, false);
        post.transform.position = postGround;
        AddPart(post.transform, PrimitiveType.Cylinder, new Vector3(0f, 1.2f, 0f), new Vector3(0.15f, 1.2f, 0.15f), material);
        AddPart(post.transform, PrimitiveType.Cube, new Vector3(0f, 2.7f, 0f), new Vector3(2.8f, 1.2f, 0.12f), material);

        GameObject label = new GameObject("Title");
        label.transform.SetParent(post.transform, false);
        label.transform.localPosition = new Vector3(0f, 2.7f, -0.08f);
        TextMeshPro text = label.AddComponent<TextMeshPro>();
        text.text = title;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(0.15f, 0.12f, 0.08f);
        text.enableAutoSizing = true;
        text.fontSizeMin = 1f;
        text.fontSizeMax = 6f;
        text.rectTransform.sizeDelta = new Vector2(2.6f, 1f);
        TMP_FontAsset font = GameSettings.Load().titleFont;
        if (font != null) text.font = font;

        // Pop-up sign (same as TriggerController > Create Pop-up Sign)
        TriggerController triggers = Object.FindAnyObjectByType<TriggerController>();
        triggers.CreateTrigger();
        GameObject triggerGO = Selection.activeGameObject;
        TriggerLogic trigger = triggerGO.GetComponent<TriggerLogic>();
        trigger.message = message;
        trigger.size = 5f;
        trigger.clearOnExit = true;
        Vector3 local = triggers.transform.InverseTransformPoint(triggerCentre);
        trigger.offsetX = local.x;
        trigger.offsetY = local.y;
        trigger.offsetZ = local.z;
        triggerGO.transform.localPosition = local;
        triggerGO.name = "Pop-up - " + title;
        return triggerGO;
    }

    internal static void AddPart(Transform parent, PrimitiveType type, Vector3 localPosition, Vector3 localScale, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        Object.DestroyImmediate(part.GetComponent<Collider>());
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localScale = localScale;
        part.GetComponent<Renderer>().sharedMaterial = material;
    }

    internal static Material GetSignMaterial()
    {
        string path = AssetFolder + "/SignYellow.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null) return mat;
        mat = new Material(Shader.Find("Standard")) { color = new Color(1f, 0.82f, 0.2f) };
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", new Color(0.35f, 0.25f, 0.02f));
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    private static void AddToBuild() => AddToBuild(ScenePath, null);

    /// <summary>Adds the scene to Build Settings (or fixes its entry). New entries go right after 'after' if it's listed, else at the end.</summary>
    internal static void AddToBuild(string scenePath, string after)
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        // Replace any old entry too, so its GUID matches the scene file
        // (Unity keeps a stale GUID if the path is already listed, so remove the entry first, then add it back)
        GUID guid = new GUID(AssetDatabase.AssetPathToGUID(scenePath));
        int index = scenes.FindIndex(s => s.path == scenePath);
        if (index >= 0 && scenes[index].guid == guid) return;

        bool enabled = index < 0 || scenes[index].enabled;
        if (index >= 0)
        {
            scenes.RemoveAt(index);
            EditorBuildSettings.scenes = scenes.ToArray();
        }
        else
        {
            int afterIndex = after != null ? scenes.FindIndex(s => s.path == after) : -1;
            index = afterIndex >= 0 ? afterIndex + 1 : scenes.Count;
        }
        scenes.Insert(index, new EditorBuildSettingsScene(guid, enabled));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
