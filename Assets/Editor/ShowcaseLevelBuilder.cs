using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Builds Tutorial 2, "Adventure Park": the level after Tutorial Island. It shows every feature in action
/// (power-ups, moving platforms, scatter, creatures with models, a chaser, wandering pickups, glow & effects...),
/// made with the same tools as the Game Builder's buttons. Each sign's things are in a 📁 folder named after it,
/// so the Game Builder lists show what made what. Rebuild it any time from Game > Tutorial > Build Tutorial 2.
/// </summary>
public static class ShowcaseLevelBuilder
{
    public const string SceneName = "Tutorial2";
    public const string ScenePath = "Assets/Scenes/" + SceneName + ".unity";
    private const string TerrainPath = TutorialLevelBuilder.AssetFolder + "/Tutorial2Terrain.asset";
    private const string ModelFolder = "Assets/MyModels/";
    private const string SignFolder = "Park signs";

    // The park's main path runs along +Z from the start; exhibits are on both sides of it
    private static readonly Vector3 Start = new Vector3(0f, 0f, -80f);
    private const float ParkHeight = 1f;

    private static Terrain terrain;
    private static Transform signs;
    private static Material signMaterial;
    private static PlatformController platforms;
    private static readonly Dictionary<string, int> folderOrder = new Dictionary<string, int>();

    // ------------------------------------------------------------------ Menus

    [MenuItem("Game/Tutorial/Open Tutorial 2 (Adventure Park)", priority = 43)]
    public static void OpenMenu()
    {
        if (!File.Exists(ScenePath))
        {
            if (EditorUtility.DisplayDialog("No Adventure Park yet", "Tutorial 2 hasn't been built yet. Build it now?", "Build it", "Cancel")) BuildMenu();
            return;
        }
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(ScenePath);
    }

    [MenuItem("Game/Tutorial/Build Tutorial 2 (Adventure Park)", priority = 44)]
    public static void BuildMenu()
    {
        if (File.Exists(ScenePath) && !EditorUtility.DisplayDialog("Rebuild Adventure Park?",
                "This replaces Tutorial 2 with a fresh copy. Any changes made to it will be lost.", "Rebuild", "Cancel")) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        string result = Build();
        EditorUtility.DisplayDialog("Adventure Park", result + "\n\nPress Play and walk into the yellow signs!", "OK");
    }

    // ------------------------------------------------------------------ Build

    /// <summary>Builds and saves the Adventure Park scene. Returns a short report.</summary>
    public static string Build()
    {
        if (!AssetDatabase.IsValidFolder(TutorialLevelBuilder.AssetFolder)) AssetDatabase.CreateFolder("Assets", "Tutorial");
        folderOrder.Clear();
        float oldHeight = ScatterTools.NewPlatformHeight;
        float oldCreatureSize = ScatterTools.NewCreatureSize;
        ScatterTools.NewCreatureSize = 1f; // the park sets its creatures' sizes itself

        // 1. Start from the empty template level, overwriting in place so the scene keeps its GUID
        if (File.Exists(ScenePath))
        {
            File.Copy(LevelMaker.TemplatePath, ScenePath, true);
            AssetDatabase.ImportAsset(ScenePath, ImportAssetOptions.ForceUpdate);
        }
        else
        {
            AssetDatabase.CopyAsset(LevelMaker.TemplatePath, ScenePath);
        }
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        try
        {
            // 2. Ground: its own copy, with a wide flat park (room for exhibits on both sides of the path)
            terrain = TutorialLevelBuilder.CreateTutorialTerrain(TerrainPath, new Rect(Start.x - 50f, Start.z - 25f, 100f, 250f), ParkHeight, newMountainsSeed: 7);
            signs = new GameObject("Park Signs").transform;
            signMaterial = TutorialLevelBuilder.GetSignMaterial();
            platforms = Object.FindAnyObjectByType<PlatformController>();
            Random.InitState(2026); // the same park every time it's rebuilt

            // 3. Player at the gate, facing down the path
            PlayerController player = Object.FindAnyObjectByType<PlayerController>();
            player.transform.SetPositionAndRotation(At(0f, -6f, 0.2f), Quaternion.identity);
            player.CreateCharacterMesh();

            // Scenery first (round the edges), so everything after it has a clear park
            int sceneryCount = BuildScenery();

            Welcome();
            PowerUps();
            MovingPlatforms();
            SteppingStones();
            SkyGarden();
            CreatureZoo();
            Checkpoint();
            Chaser();
            RunawayThings();
            GlowAndEffects();
            MakeItYours();
            Finish();

            // 4. Save and add to the build, right after Tutorial Island
            Selection.activeGameObject = null;
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            TutorialLevelBuilder.AddToBuild(ScenePath, TutorialLevelBuilder.ScenePath);

            int platformCount = Object.FindObjectsByType<PlatformLogic>(FindObjectsSortMode.None).Length;
            int thingCount = Object.FindObjectsByType<ObstacleBase>(FindObjectsSortMode.None).Length;
            return $"Built Adventure Park: {signs.childCount} signs, {platformCount} platforms, {thingCount} creatures & pickups and {sceneryCount} scenery pieces.";
        }
        finally
        {
            ScatterTools.NewPlatformHeight = oldHeight;
            ScatterTools.NewCreatureSize = oldCreatureSize;
            ScatterTools.PlaceAt = null;
            Random.InitState(System.Environment.TickCount);
        }
    }

    // ------------------------------------------------------------------ Stations

    private static void Welcome()
    {
        StationSign(0f, "1. Welcome!",
            "Welcome to ADVENTURE PARK: lots of Game Builder features, all in one place!\n" +
            "Open it in Unity (Game > Tutorial > Open Tutorial 2): the Game Builder lists have a folder for each sign.");
    }

    private static void PowerUps()
    {
        const string folder = "Power-ups";
        StationSign(10f, "2. Power-ups",
            "POWER-UPS! Health heals you, Stamina refills your sprint, Shield stops damage, Super Jump and Speed Boost last 8 seconds.\n" +
            "Game Builder > Creatures & Pickups: click one, then click where it goes. Each one glows in its own colour.");

        (ObstacleBase.PayloadType payload, int amount, float duration)[] row =
        {
            (ObstacleBase.PayloadType.HealthBooster, 25, 5f),
            (ObstacleBase.PayloadType.StaminaBooster, 50, 5f),
            (ObstacleBase.PayloadType.InvincibilityBuff, 1, 8f),
            (ObstacleBase.PayloadType.JumpBuff, 2, 8f),
            (ObstacleBase.PayloadType.SprintBuff, 2, 8f),
        };
        for (int i = 0; i < row.Length; i++)
        {
            ObstacleBase pickup = SpawnOnGround(At(-8f + i * 4f, 18f), row[i].payload, ObstacleSpawnType.Static, row[i].amount, row[i].duration, true);
            Pickup(pickup, folder);
        }
    }

    private static void MovingPlatforms()
    {
        const string folder = "Moving platforms";
        StationSign(28f, "3. Moving platforms",
            "MOVING platforms: side to side, spinning, growing, and an ELEVATOR up to a high ledge with a prize!\n" +
            "Click a platform, then 'Make it:' picks how it moves. Move Distance = how far, Move Speed = how fast.", 5f);

        Folder(Quick(ScatterTools.PlatformMove.SideToSide, 16f, 36f, 1.6f), folder);
        Folder(Quick(ScatterTools.PlatformMove.Spinning, 31f, 36f, 1.6f), folder);
        Folder(Quick(ScatterTools.PlatformMove.Growing, 16f, 50f, 1.6f), folder);

        // The elevator goes from just above the ground up to the ledge beside it
        PlatformLogic elevator = Quick(ScatterTools.PlatformMove.UpAndDown, 29f, 50f, 3f); // middle 4.5 m up, moves 8 m
        Folder(elevator, folder);
        PlatformLogic ledge = Quick(ScatterTools.PlatformMove.Still, 34.5f, 50f, 8.1f);    // top about 8.5 m up
        ledge.name = "High Ledge";
        Folder(ledge, folder);
        Pickup(SpawnOnPlatform(ledge, ObstacleBase.PayloadType.InvincibilityBuff, ObstacleSpawnType.Static, 1, 8f, true), folder, "Prize Shield");
    }

    private static void SteppingStones()
    {
        const string folder = "Stepping stones";
        StationSign(40f, "4. Stepping stones",
            "STEPPING STONES: Platforms > Stepping stones makes 6 in a row, each one a bit higher.\n" +
            "At the top there's a Speed Boost. Grab it, then hold Shift to sprint!", -5f);

        // The same sizes as the Stepping stones button, going down the park
        for (int i = 0; i < 6; i++)
        {
            Vector3 ground = At(-16f, 36f + i * 3.5f);
            PlatformLogic stone = ScatterTools.CreatePlatformAt(platforms, ground + Vector3.up * (1f + i * 0.7f), 2f, 2f, 0.6f, Random.Range(0f, 360f), null);
            ScatterTools.SetMovement(stone, ScatterTools.PlatformMove.Still, 0f, 2f);
            stone.name = "Stepping Stone " + (i + 1);
            Folder(stone, folder);
        }
        PlatformLogic top = Quick(ScatterTools.PlatformMove.Still, -16f, 57.5f, 5.6f); // top about 6 m up
        top.name = "Top Platform";
        Folder(top, folder);
        Pickup(SpawnOnPlatform(top, ObstacleBase.PayloadType.SprintBuff, ObstacleSpawnType.Static, 2, 8f, true), folder);
    }

    private static void SkyGarden()
    {
        const string folder = "Sky garden";
        StationSign(70f, "5. Sky garden",
            "This SKY GARDEN took 2 clicks: Platforms > Scatter tools (12 platforms, some crumbling or bouncy),\n" +
            "then Creatures & Pickups > Scatter tools, with 'Can go on: still / moving platforms' ticked.", -5f);

        Vector3 middle = At(-28f, 86f);
        HashSet<PlatformLogic> before = new HashSet<PlatformLogic>(Object.FindObjectsByType<PlatformLogic>(FindObjectsSortMode.None));

        // Platform scatter settings live on the PlatformController (students find them there)
        PlatformScatter ps = platforms.GetComponent<PlatformScatter>();
        if (ps == null) ps = platforms.gameObject.AddComponent<PlatformScatter>();
        ps.howMany = 12;
        ps.heightRange = new Vector2(1.5f, 5f);
        ps.sizeRange = new Vector2(3f, 5f);
        ps.still = true; ps.sideToSide = true; ps.upAndDown = true; ps.spinning = false; ps.growing = false;
        ps.moveDistanceRange = new Vector2(3f, 5f);
        ps.speedRange = new Vector2(1f, 2f);
        ps.crumblingPercent = 20;
        ps.bouncyPercent = 15;
        ps.place = new ScatterPlace { area = ScatterArea.AroundSceneView, areaSize = 30f, maxSlope = 90f, spacing = 2.5f, keepAwayFromStart = 5f, edgePadding = 15f };
        ScatterTools.PlaceAt = middle;
        ScatterTools.ScatterPlatforms(platforms, ps);

        List<PlatformLogic> garden = Object.FindObjectsByType<PlatformLogic>(FindObjectsSortMode.None).Where(p => !before.Contains(p)).ToList();
        foreach (PlatformLogic p in garden) Folder(p, folder);

        // Creature & pickup scatter settings live on the terrain
        HashSet<ObstacleBase> beforeThings = new HashSet<ObstacleBase>(Object.FindObjectsByType<ObstacleBase>(FindObjectsSortMode.None));
        CreatureScatter cs = terrain.GetComponent<CreatureScatter>();
        if (cs == null) cs = terrain.gameObject.AddComponent<CreatureScatter>();
        cs.place = new ScatterPlace { area = ScatterArea.AroundSceneView, areaSize = 30f, spacing = 2f, keepAwayFromStart = 5f };
        cs.groups = new List<CreatureScatter.Group>
        {
            new CreatureScatter.Group { name = "Sky garden spiders", kind = CreatureScatter.Kind.Wanderer, howMany = 4, sizeRange = new Vector2(1.4f, 1.7f),
                speedRange = new Vector2(1f, 1.5f), roamRange = new Vector2(1f, 1.5f), onGround = false, onStillPlatforms = true, onMovingPlatforms = true },
            new CreatureScatter.Group { name = "Sky garden health", kind = CreatureScatter.Kind.Health, howMany = 3, sizeRange = Vector2.one, glow = true,
                onGround = false, onStillPlatforms = true, onMovingPlatforms = true },
            new CreatureScatter.Group { name = "Sky garden super jumps", kind = CreatureScatter.Kind.SuperJump, howMany = 2, sizeRange = Vector2.one, glow = true,
                onGround = false, onStillPlatforms = true, onMovingPlatforms = false },
        };
        ScatterTools.PlaceAt = middle;
        ScatterTools.ScatterAllCreatures(cs);
        ScatterTools.PlaceAt = null;

        foreach (ObstacleBase o in Object.FindObjectsByType<ObstacleBase>(FindObjectsSortMode.None).Where(o => !beforeThings.Contains(o))) Folder(o, folder);
    }

    private static void CreatureZoo()
    {
        const string folder = "Creature zoo";
        StationSign(78f, "6. Creature zoo",
            "CREATURE ZOO: any model in Assets/MyModels can be a creature: drag it onto one. Here: a wandering Fox, a Horse that\n" +
            "walks back & forth, a Snake guard and a BOUNCY spider. The GIANT Dinosaur needs 3 jumps: jump on it from the platform!", 5f);

        ObstacleBase fox = SpawnOnGround(At(20f, 76f), ObstacleBase.PayloadType.Damage, ObstacleSpawnType.Wander, 10, 5f, false, Model("Fox"), 2f);
        SetWander(fox, 5f, 1.5f, 2.5f);
        Folder(fox, folder, "Fox");

        ObstacleBase horse = SpawnOnGround(At(30f, 82f), ObstacleBase.PayloadType.Damage, ObstacleSpawnType.Patrol, 10, 5f, false, Model("Horse"), 3f);
        SetPatrol(horse, 8f, 1.5f, 2.5f);
        Turn(horse, 0f);
        Folder(horse, folder, "Horse");

        ObstacleBase snake = SpawnOnGround(At(16f, 90f), ObstacleBase.PayloadType.Damage, ObstacleSpawnType.Static, 10, 5f, false, Model("Snake"), 4f);
        Turn(snake, 270f); // looking at the path
        Folder(snake, folder, "Snake");

        // A giant: 9 times bigger (about 5 m tall), 3 jumps to beat, with a platform beside it to jump from
        ObstacleBase dino = SpawnOnGround(At(28f, 100f), ObstacleBase.PayloadType.Damage, ObstacleSpawnType.Static, 15, 5f, false, Model("Dinosaur"), 9f);
        if (dino != null)
        {
            dino.jumpsToDefeat = 3;
            dino.sparkleStyle = ObstacleBase.SparkleStyle.Explosion;
            Turn(dino, 270f);
            dino.UpdateHitZone(true);
        }
        Folder(dino, folder, "Giant Dinosaur");
        PlatformLogic jumpFrom = Quick(ScatterTools.PlatformMove.Still, 28f, 95f, 6.2f);
        jumpFrom.name = "Jump From Here";
        Folder(jumpFrom, folder);

        // Runs into you and throws you back ('Is Bouncy' on a creature)
        ObstacleBase bouncy = SpawnOnGround(At(18f, 110f), ObstacleBase.PayloadType.Damage, ObstacleSpawnType.Wander, 5, 5f, false, Model("Spider"), 2f);
        if (bouncy != null)
        {
            bouncy.isBouncy = true;
            bouncy.launchForce = 15f;
            SetWander(bouncy, 3f, 1f, 1.5f);
        }
        Folder(bouncy, folder, "Bouncy Spider");
    }

    private static void Checkpoint()
    {
        StationSign(118f, "7. Checkpoint",
            "CHECKPOINT: touch the flag. If you die after this, you start again from here.\n" +
            "Game Builder > Signs & Story > Create Checkpoint.");
        GameMenuItems.CreateCheckpoint();
        GameObject flag = Selection.activeGameObject;
        flag.transform.position = At(3f, 118f);
        flag.name = "Park Checkpoint";
    }

    private static void Chaser()
    {
        const string folder = "Chaser";
        StationSign(128f, "8. Chaser",
            "Look out: that zombie CHASES you if you go near it! Click a creature > Movement settings > tick 'Chase the player'.\n" +
            "'Starts chasing within' and 'Gives up past' are its orange and red circles in the Scene. Run away to escape!", 5f);

        ObstacleBase zombie = SpawnOnGround(At(11f, 134f), ObstacleBase.PayloadType.Damage, ObstacleSpawnType.Patrol, 10, 5f, false, Model("Zombie"), 2f);
        if (zombie != null)
        {
            SetPatrol(zombie, 3f, 1f, 1.5f);
            Turn(zombie, 0f);
            zombie.chasePlayer = true;
            zombie.chaseStartDistance = 8f;
            zombie.chaseGiveUpDistance = 15f;
            zombie.chaseSpeed = 2.5f; // slower than the player, so it can be escaped
            zombie.jumpsToDefeat = 2;
        }
        Folder(zombie, folder, "Chasing Zombie");
    }

    private static void RunawayThings()
    {
        const string folder = "Runaway things";
        StationSign(142f, "9. Runaway things",
            "Pickups can move too! This Health pickup WANDERS around (catch it!) and so does the bounce ball.\n" +
            "Click a pickup > Movement settings > Wanders around, or Floats back & forth.");

        ObstacleBase health = SpawnOnGround(At(-11f, 146f), ObstacleBase.PayloadType.HealthBooster, ObstacleSpawnType.Wander, 25, 5f, true);
        SetWander(health, 5f, 1.5f, 2f);
        Pickup(health, folder, "Runaway Health");

        ObstacleBase ball = SpawnOnGround(At(11f, 148f), ObstacleBase.PayloadType.None, ObstacleSpawnType.Wander, 10, 5f, false, null, 1.4f, bouncy: true);
        SetWander(ball, 4f, 1f, 1.5f);
        if (ball != null) ball.upwardBias = 0.2f; // round, so you fly off at wild angles depending on where you hit it
        Folder(ball, folder, "Wandering Bounce Ball");
    }

    private static void GlowAndEffects()
    {
        const string folder = "Glow & effects";
        StationSign(156f, "10. Glow & effects",
            "GLOW & LANDING EFFECTS: jump onto these platforms! Click a platform: 'Make it glow', 'Add sparkles around it',\n" +
            "and pick a Land Effect (Sparks, Ring, Fountain...). The last one is a ROCK: drag a model into 'Platform Model'.");

        PlatformLogic Low(float side, float forward, Sparkles.Effect effect, Color colour, string name)
        {
            PlatformLogic p = Quick(ScatterTools.PlatformMove.Still, side, forward, 0.6f);
            p.landEffect = effect;
            p.effectColour = colour;
            p.name = name;
            Folder(p, folder);
            return p;
        }

        PlatformLogic sparks = Low(-8f, 162f, Sparkles.Effect.Sparks, new Color(0.4f, 0.9f, 1f), "Glowing Sparks Platform");
        Glow g1 = ScatterTools.AddGlow(sparks.TargetChild.gameObject, sparks.effectColour);
        g1.strength = 2f;

        PlatformLogic ring = Low(8f, 162f, Sparkles.Effect.Ring, new Color(1f, 0.5f, 0.85f), "Fireflies Ring Platform");
        Glow g2 = ScatterTools.AddGlow(ring.TargetChild.gameObject, ring.effectColour);
        g2.pulsesPerSecond = 1f;
        AddSparkles(ring.TargetChild.gameObject, AmbientSparkles.Style.Fireflies, new Color(1f, 0.9f, 0.4f));

        PlatformLogic fountain = Low(-8f, 172f, Sparkles.Effect.Fountain, new Color(0.6f, 1f, 0.5f), "Fountain Platform");
        AddSparkles(fountain.TargetChild.gameObject, AmbientSparkles.Style.FloatUp, fountain.effectColour);

        PlatformLogic rock = Low(8f, 172f, Sparkles.Effect.Explosion, new Color(1f, 0.6f, 0.2f), "Rock Platform");
        GameObject rockModel = AssetDatabase.LoadAssetAtPath<GameObject>(TutorialLevelBuilder.NatureFolder + "Rock_Moss_6.fbx");
        if (rockModel != null) PlatformController.ApplyPlatformModel(rock.gameObject, rockModel);
    }

    private static void MakeItYours()
    {
        StationSign(184f, "11. Make it yours",
            "MAKE IT YOURS: with 'Click in the Scene' ticked (in Default options), buttons wait for you to click where things go. 'Move it...' moves things later.\n" +
            "Give things a Name and put them in a Folder, then drag them in the lists to sort them. Made a mistake? Ctrl+Z!");
    }

    private static void Finish()
    {
        StationSign(196f, "12. Finish!",
            "You finished ADVENTURE PARK! Now make your OWN level: Game Builder > Levels & Build > New empty level.\n" +
            "Want another go? Esc > Restart Level.");
    }

    // ------------------------------------------------------------------ Scenery

    /// <summary>Trees, rocks and plants round the park (none inside it), plus a row of trees along each side.</summary>
    private static int BuildScenery()
    {
        TerrainPopulator populator = Object.FindAnyObjectByType<TerrainPopulator>();
        SceneryPopulator scenery = populator.GetComponent<SceneryPopulator>();
        if (scenery == null) scenery = Undo.AddComponent<SceneryPopulator>(populator.gameObject);
        if (scenery.groups.Count == 0) typeof(SceneryPopulator).GetMethod("Reset", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(scenery, null);
        foreach (SceneryPopulator.SceneryGroup group in scenery.groups) scenery.QuickFill(group);

        SceneryPopulator.SceneryGroup trees = scenery.groups.Find(g => g.name == "Trees");
        SceneryPopulator.SceneryGroup rocks = scenery.groups.Find(g => g.name == "Rocks");
        SceneryPopulator.SceneryGroup plants = scenery.groups.Find(g => g.name == "Plants & Grass");
        if (trees != null) { trees.howMany = 120; scenery.Scatter(trees); }
        if (rocks != null) { rocks.howMany = 60; scenery.Scatter(rocks); }
        if (plants != null) { plants.howMany = 250; scenery.Scatter(plants); }

        // Keep the park itself clear
        Transform root = scenery.transform.Find(SceneryPopulator.ContainerName);
        Rect park = new Rect(Start.x - 46f, Start.z - 18f, 92f, 238f);
        if (root != null)
        {
            List<GameObject> remove = new List<GameObject>();
            foreach (Transform group in root)
                foreach (Transform piece in group)
                    if (park.Contains(new Vector2(piece.position.x, piece.position.z))) remove.Add(piece.gameObject);
            foreach (GameObject go in remove) Object.DestroyImmediate(go);
        }

        // A row of trees down each side, and flowers by the gate
        for (float z = -10f; z <= 205f; z += 12f)
        {
            if (trees != null)
            {
                scenery.PlaceOne(trees, At(-44f + Random.Range(-1.5f, 1.5f), z + Random.Range(-2f, 2f)));
                scenery.PlaceOne(trees, At(44f + Random.Range(-1.5f, 1.5f), z + Random.Range(-2f, 2f)));
            }
        }
        if (plants != null)
        {
            for (int i = 0; i < 6; i++) scenery.PlaceOne(plants, At(i % 2 == 0 ? -6f - i : 6f + i, -4f + i * 1.5f));
        }
        return root != null ? root.GetComponentsInChildren<Renderer>().Length : 0;
    }

    // ------------------------------------------------------------------ Helpers

    private static float Ground(float x, float z) => terrain.SampleHeight(new Vector3(x, 0f, z)) + terrain.GetPosition().y;

    /// <summary>A point on the ground: 'side' metres right of the path (negative = left), 'forward' metres from the start.</summary>
    private static Vector3 At(float side, float forward, float up = 0f) =>
        new Vector3(Start.x + side, Ground(Start.x + side, Start.z + forward) + up, Start.z + forward);

    /// <summary>A yellow sign beside the path ('postSide' &lt; 0 = left of the path) with a pop-up over the path.</summary>
    private static void StationSign(float forward, string title, string message, float postSide = -4f)
    {
        GameObject trigger = TutorialLevelBuilder.Sign(signs, signMaterial, At(postSide, forward), At(0f, forward, 1.5f), title, message);
        Folder(trigger.GetComponent<TriggerLogic>(), SignFolder);
    }

    /// <summary>The Game Builder's Quick platform button, with the click on this spot. height = 'New platforms float this high'.</summary>
    private static PlatformLogic Quick(ScatterTools.PlatformMove move, float side, float forward, float height, PlatformLogic.PlatformType type = PlatformLogic.PlatformType.Normal)
    {
        ScatterTools.NewPlatformHeight = height;
        ScatterTools.PlaceAt = At(side, forward);
        try { return ScatterTools.QuickPlatform(platforms, move, type); }
        finally { ScatterTools.PlaceAt = null; }
    }

    private static GameObject Model(string name) => AssetDatabase.LoadAssetAtPath<GameObject>(ModelFolder + name + "-model.glb");

    /// <summary>Like the Game Builder's creature/pickup buttons, on the ground at 'point'. model null = the normal look.</summary>
    private static ObstacleBase SpawnOnGround(Vector3 point, ObstacleBase.PayloadType payload, ObstacleSpawnType movement, int amount, float duration, bool usedUp,
        GameObject model = null, float scale = 1f, bool bouncy = false)
    {
        TerrainPopulator spawner = Object.FindAnyObjectByType<TerrainPopulator>();
        HashSet<ObstacleBase> before = new HashSet<ObstacleBase>(Object.FindObjectsByType<ObstacleBase>(FindObjectsInactive.Include, FindObjectsSortMode.None));

        // Borrow the spawner's settings for this one, then put them back
        string saved = JsonUtility.ToJson(spawner);
        spawner.scatterObstacleType = movement;
        spawner.baseSettings.payloadType = payload;
        spawner.baseSettings.payloadAmount = amount;
        spawner.baseSettings.buffDuration = duration;
        spawner.baseSettings.destroyOnTrigger = usedUp;
        spawner.baseSettings.isBouncy = bouncy;
        spawner.baseSettings.objectScale = scale;
        if (bouncy) spawner.baseSettings.launchForce = 30f;
        spawner.customModel = model;
        spawner.customMesh = null;
        spawner.CreateSingleObstacle();
        JsonUtility.FromJsonOverwrite(saved, spawner);

        ObstacleBase made = Object.FindObjectsByType<ObstacleBase>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(o => !before.Contains(o));
        if (made == null) return null;

        // It was made in the middle of the map: move it, keeping its height above the ground
        Transform t = made.transform;
        float lift = t.position.y - (terrain.SampleHeight(t.position) + terrain.GetPosition().y);
        t.position = new Vector3(point.x, point.y + lift, point.z);
        return made;
    }

    /// <summary>Like the Game Builder's buttons with a platform selected: made in the middle of the platform.</summary>
    private static ObstacleBase SpawnOnPlatform(PlatformLogic platform, ObstacleBase.PayloadType payload, ObstacleSpawnType movement, int amount, float duration, bool usedUp)
    {
        PlatformPopulator spawner = platform.GetComponent<PlatformPopulator>();
        if (spawner == null) return null;
        HashSet<ObstacleBase> before = new HashSet<ObstacleBase>(Object.FindObjectsByType<ObstacleBase>(FindObjectsInactive.Include, FindObjectsSortMode.None));

        string saved = JsonUtility.ToJson(spawner);
        spawner.scatterObstacleType = movement;
        spawner.baseSettings.payloadType = payload;
        spawner.baseSettings.payloadAmount = amount;
        spawner.baseSettings.buffDuration = duration;
        spawner.baseSettings.destroyOnTrigger = usedUp;
        spawner.baseSettings.isBouncy = false;
        spawner.CreateSingleObstacle();
        JsonUtility.FromJsonOverwrite(saved, spawner);

        return Object.FindObjectsByType<ObstacleBase>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(o => !before.Contains(o));
    }

    /// <summary>Pickups glow in their own colour, the same as the Game Builder's buttons.</summary>
    private static void Pickup(ObstacleBase pickup, string folder, string name = null)
    {
        if (pickup == null) return;
        ScatterTools.AddGlow(pickup.gameObject, Sparkles.ColourFor(pickup.payloadType));
        Folder(pickup, folder, name);
    }

    private static void SetWander(ObstacleBase o, float radius, float slowest, float fastest)
    {
        if (o == null) return;
        TerrainWanderDriver d = o.GetComponent<TerrainWanderDriver>();
        if (d != null) { d.wanderRadius = radius; d.minMoveSpeed = slowest; d.maxMoveSpeed = fastest; }
    }

    private static void SetPatrol(ObstacleBase o, float distance, float slowest, float fastest)
    {
        if (o == null) return;
        TerrainPatrolDriver d = o.GetComponent<TerrainPatrolDriver>();
        if (d != null) { d.moveDistance = distance; d.minMoveSpeed = slowest; d.maxMoveSpeed = fastest; }
    }

    private static void Turn(ObstacleBase o, float yRotation)
    {
        if (o == null) return;
        o.initialYRotation = yRotation;
        o.transform.rotation = Quaternion.Euler(0f, yRotation, 0f);
    }

    private static void AddSparkles(GameObject go, AmbientSparkles.Style style, Color colour)
    {
        AmbientSparkles s = go.AddComponent<AmbientSparkles>();
        s.style = style;
        s.colour = colour;
        s.fromTop = true; s.fromSides = true; s.fromBottom = false; s.fromCentre = false;
        s.amount = 15f;
    }

    /// <summary>Puts it in a Game Builder list folder (in the order things were made), and optionally renames it.</summary>
    private static void Folder(Component c, string folder, string name = null)
    {
        if (c == null) return;
        if (name != null) c.gameObject.name = name;
        ListFolder f = c.GetComponent<ListFolder>();
        if (f == null) f = c.gameObject.AddComponent<ListFolder>();
        f.hideFlags = HideFlags.HideInInspector;
        f.folder = folder;
        folderOrder.TryGetValue(folder, out int n);
        f.order = (n + 1) * 10;
        folderOrder[folder] = n + 1;
    }
}
