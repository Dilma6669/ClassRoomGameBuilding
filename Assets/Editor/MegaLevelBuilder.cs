using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Builds the BONUS level, Tutorial 3 "Playground Madness": every slider turned up to the max, and lots of wacky combinations
/// (a bouncy dinosaur that throws you onto a sky island, pickups on glowing giant crystals, spinning stump carousels with
/// trees and spiders on them, ghost dragons, an invisible bridge, trampoline spiders...). It's also a stress test:
/// hundreds of creatures, pickups, platforms and scenery pieces. The goal: a finish platform 300 m up, reached by a
/// chain of boost pads. Rebuild it any time from Game > Tutorial > Build Tutorial 3 (Playground Madness).
/// Made with the students' own tools (scatter groups, quick platforms, platform models, scenery on platforms, teleporters),
/// so the Game Builder shows the maxed-out settings that made each thing.
/// </summary>
public static class MegaLevelBuilder
{
    public const string SceneName = "Tutorial3";
    public const string ScenePath = "Assets/Scenes/" + SceneName + ".unity";
    private const string TerrainPath = TutorialLevelBuilder.AssetFolder + "/Tutorial3Terrain.asset";
    private const string ModelFolder = "Assets/MyModels/";
    private const string SignFolder = "Madness signs";

    private static readonly Vector3 Start = new Vector3(0f, 0f, -200f);
    private const float Flat = 1f;

    // The boost pad tower: pad i is at TowerZ + i * PadStep (going north), each PadRise higher
    private const int PadCount = 8;
    private const float TowerZ = -175f;
    private const float PadStep = 13f;
    private const float PadRise = 36f;
    private const float PadBounce = 57f; // apex about 54 m above the pad: 18 m of spare height to steer onto the next one

    private static Terrain terrain;
    private static Transform signs;
    private static Material signMaterial;
    private static PlatformController platforms;
    private static SceneryPopulator scenery;
    private static CreatureScatter creatureScatter;
    private static readonly Dictionary<string, int> folderOrder = new Dictionary<string, int>();
    private static readonly List<(Vector3 pos, float radius)> keepClear = new List<(Vector3, float)>();

    // ------------------------------------------------------------------ Menus

    [MenuItem("Game/Tutorial/Open Tutorial 3 (Playground Madness)", priority = 45)]
    public static void OpenMenu()
    {
        if (!File.Exists(ScenePath))
        {
            if (EditorUtility.DisplayDialog("No Playground Madness yet", "Tutorial 3 hasn't been built yet. Build it now? (It takes a minute or two.)", "Build it", "Cancel")) BuildMenu();
            return;
        }
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(ScenePath);
    }

    [MenuItem("Game/Tutorial/Build Tutorial 3 (Playground Madness)", priority = 46)]
    public static void BuildMenu()
    {
        if (File.Exists(ScenePath) && !EditorUtility.DisplayDialog("Rebuild Playground Madness?",
                "This replaces Tutorial 3 with a fresh copy. Any changes made to it will be lost. It takes a minute or two.", "Rebuild", "Cancel")) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        string result = Build();
        EditorUtility.DisplayDialog("Playground Madness", result + "\n\nPress Play. Good luck getting to the top!", "OK");
    }

    // ------------------------------------------------------------------ Build

    public static string Build()
    {
        if (!AssetDatabase.IsValidFolder(TutorialLevelBuilder.AssetFolder)) AssetDatabase.CreateFolder("Assets", "Tutorial");
        folderOrder.Clear();
        keepClear.Clear();
        float oldHeight = ScatterTools.NewPlatformHeight;
        float oldCreatureSize = ScatterTools.NewCreatureSize;
        ScatterTools.NewCreatureSize = 1f; // sizes are set here

        if (File.Exists(ScenePath))
        {
            File.Copy(LevelMaker.TemplatePath, ScenePath, true);
            AssetDatabase.ImportAsset(ScenePath, ImportAssetOptions.ForceUpdate);
        }
        else AssetDatabase.CopyAsset(LevelMaker.TemplatePath, ScenePath);
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        try
        {
            // A big flat valley (500 x 500 m) for the racers, with the normal mountains round it
            terrain = TutorialLevelBuilder.CreateTutorialTerrain(TerrainPath, new Rect(-250f, -250f, 500f, 500f), Flat);
            signs = new GameObject("Madness Signs").transform;
            signMaterial = TutorialLevelBuilder.GetSignMaterial();
            platforms = Object.FindAnyObjectByType<PlatformController>();
            TerrainPopulator populator = Object.FindAnyObjectByType<TerrainPopulator>();
            scenery = populator.GetComponent<SceneryPopulator>();
            if (scenery == null) scenery = Undo.AddComponent<SceneryPopulator>(populator.gameObject);
            creatureScatter = terrain.GetComponent<CreatureScatter>();
            if (creatureScatter == null) creatureScatter = terrain.gameObject.AddComponent<CreatureScatter>();
            creatureScatter.groups = new List<CreatureScatter.Group>();
            Random.InitState(3003);

            // Player: start of the valley, facing the tower. 1000 health, because everything here is MEGA.
            PlayerController player = Object.FindAnyObjectByType<PlayerController>();
            player.transform.SetPositionAndRotation(G(0f, -205f, 0.2f), Quaternion.identity);
            player.CreateCharacterMesh();
            TutorialLevelBuilder.SetTutorialPlayer(player);
            Health health = player.GetComponentInChildren<Health>();
            if (health != null)
            {
                SerializedObject so = new SerializedObject(health);
                SerializedProperty max = so.FindProperty("maxHealth");
                if (max != null) { max.intValue = 1000; so.ApplyModifiedPropertiesWithoutUndo(); }
            }
            KeepClear(G(0f, -200f), 30f);

            Welcome();
            BoostTower();          // first, so nothing else lands in its way
            WelcomeGifts();
            DinoLauncher();
            CrystalField();
            Carousel();
            InvisibleBridge();
            SkyDrop();
            TrampolineSpiders();
            SkyIslands();
            ScatteredPlatforms();
            int sceneryCount = Scenery();
            Stampedes();
            Creatures();
            PlatformCrowds();
            BecomeCharacters();

            Selection.activeGameObject = null;
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            TutorialLevelBuilder.SetMoodyLook();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            TutorialLevelBuilder.AddToBuild(ScenePath, ShowcaseLevelBuilder.ScenePath);

            int platformCount = Object.FindObjectsByType<PlatformLogic>(FindObjectsSortMode.None).Length;
            ObstacleBase[] things = Object.FindObjectsByType<ObstacleBase>(FindObjectsSortMode.None);
            int creatures = things.Count(o => o.payloadType == ObstacleBase.PayloadType.Damage);
            return $"Built Playground Madness: {platformCount} platforms, {creatures} creatures, {things.Length - creatures} pickups & bounce balls, " +
                   $"{sceneryCount} scenery pieces, {Object.FindObjectsByType<Teleporter>(FindObjectsSortMode.None).Length} teleporter pads.";
        }
        finally
        {
            ScatterTools.NewPlatformHeight = oldHeight;
            ScatterTools.NewCreatureSize = oldCreatureSize;
            ScatterTools.PlaceAt = null;
            Random.InitState(System.Environment.TickCount);
        }
    }

    // ------------------------------------------------------------------ The way up

    private static void Welcome()
    {
        Sign(G(-5f, -195f), "1. PLAYGROUND MADNESS",
            "BONUS LEVEL! Every slider turned up to the MAX: giant spiders, huge dinosaurs, dragons, things racing at 150 m/s...\n" +
            "Your goal: the FINISH 300 m up in the sky. Bounce up the green BOOST PADS one after another. You have 1000 health!");
    }

    /// <summary>A zig-zag chain of bouncy boost pads to the finish, 36 m higher each time. Landing on them never hurts.</summary>
    private static void BoostTower()
    {
        const string folder = "Boost tower";
        Sign(G(6f, -185f), "2. Boost pads",
            "BOOST PADS: bouncy platforms with Bounce Strength 57 (the slider goes to 500!). Land on one and you fly 50 m up.\n" +
            "Steer in the air onto the next one. 'Landing on it never hurts' is ticked, so falls onto them are safe. Checkpoint halfway!");

        PlatformLogic last = null;
        for (int i = 0; i < PadCount; i++)
        {
            float x = (i % 2 == 0 ? -1f : 1f) * 5f;
            float z = TowerZ + i * PadStep;
            float padTop = 2f + i * PadRise;
            PlatformLogic pad = ScatterTools.CreatePlatformAt(platforms, G(x, z, padTop - 0.5f), 9f, 9f, 1f, 0f, null);
            bool moves = i == 5 || i == 7;
            ScatterTools.SetMovement(pad, moves ? ScatterTools.PlatformMove.SideToSide : ScatterTools.PlatformMove.Still, 8f, 2f);
            ScatterTools.SetPlatformType(pad, PlatformLogic.PlatformType.Bouncy);
            pad.bounceStrength = PadBounce;
            pad.landEffect = Sparkles.Effect.Explosion;
            pad.effectAmount = 3f;
            pad.effectSize = 2f;
            ScatterTools.AddGlow(pad.TargetChild.gameObject, PlatformLogic.BouncyColour).strength = 3f;
            Folder(pad, folder, $"Boost Pad {i + 1}" + (moves ? " (moving)" : ""));
            KeepClear(pad.TargetChild.position, 14f);
            last = pad;

            // Halfway: a still rest stop with a checkpoint and a mega health
            if (i == 3)
            {
                PlatformLogic rest = ScatterTools.CreatePlatformAt(platforms, G(x - 12f, z, padTop - 0.5f), 7f, 7f, 1f, 0f, null);
                ScatterTools.SetMovement(rest, ScatterTools.PlatformMove.Still, 0f, 1f);
                rest.safeLanding = true;
                Folder(rest, folder, "Rest Stop");
                Physics.SyncTransforms();
                GameMenuItems.CreateCheckpoint();
                GameObject flag = Selection.activeGameObject;
                flag.transform.position = new Vector3(rest.TargetChild.position.x + 2f, Surface(rest, rest.TargetChild.position + Vector3.right * 2f), rest.TargetChild.position.z);
                flag.name = "Halfway Checkpoint";
                ObstacleBase h = OnPlatform(rest, CreatureScatter.Kind.Health, folder);
                Mega(h, 1000, 5f, "MEGA HEALTH +1000!");
            }
        }

        // The finish platform, a bit past and above the last pad
        float finishTop = 2f + PadCount * PadRise - 8f;
        PlatformLogic finish = ScatterTools.CreatePlatformAt(platforms, G(0f, TowerZ + PadCount * PadStep + 4f, finishTop - 0.6f), 16f, 16f, 1.2f, 0f, null);
        ScatterTools.SetMovement(finish, ScatterTools.PlatformMove.Still, 0f, 1f);
        finish.safeLanding = true;
        Folder(finish, folder, "FINISH Platform");
        ScatterTools.AddGlow(finish.TargetChild.gameObject, new Color(1f, 0.85f, 0.2f)).strength = 4f;
        AddSparkles(finish.TargetChild.gameObject, AmbientSparkles.Style.Fountain, new Color(1f, 0.85f, 0.2f), 60f);
        Physics.SyncTransforms();
        float top = Surface(finish, finish.TargetChild.position);
        Vector3 c = finish.TargetChild.position;

        GameObject sign = TutorialLevelBuilder.Sign(signs, signMaterial, new Vector3(c.x - 3f, top, c.z + 3f), new Vector3(c.x, top + 1.5f, c.z), "FINISH!",
            "YOU BEAT PLAYGROUND MADNESS!!! You climbed 300 m through spiders, dinosaurs and dragons. You're a legend.\n" +
            "Now go and turn YOUR sliders up to the max. (Take the teleporter to go back down.)");
        Folder(sign.GetComponent<TriggerLogic>(), SignFolder);
        sign.GetComponent<TriggerLogic>().size = 8f;

        // One-way teleporter home (the start pad doesn't send you up: no cheating!)
        Teleporter home = TeleporterTools.CreatePair(new Vector3(c.x + 4f, top, c.z - 4f), finish, G(8f, -205f), null, new Color(1f, 0.85f, 0.2f));
        home.name = "Teleporter Home";
        home.sendsYouTo.name = "Home Landing (one way)";
        home.sendsYouTo.GetComponent<Teleporter>().sendsYouTo = null;
        home.message = "Back to the start!";
        Folder(home, folder);
        Folder(home.sendsYouTo, folder);
        KeepClear(G(8f, -205f), 6f);
    }

    /// <summary>Ridiculous presents round the start: giant pickups worth 1000, lasting minutes.</summary>
    private static void WelcomeGifts()
    {
        const string folder = "Welcome gifts";
        Sign(G(-8f, -210f), "3. Welcome gifts",
            "PRESENTS! Pickups with the sliders maxed: Health +1000, Shield for 2 minutes, Super Jump x3 and Speed Boost x3 for a whole minute.\n" +
            "Click one > When you touch it: How much, Power-up lasts, Message. Objects can be 200 times bigger too!");

        (CreatureScatter.Kind kind, int amount, float time, string msg)[] gifts =
        {
            (CreatureScatter.Kind.Health, 1000, 5f, "MEGA HEALTH +1000!"),
            (CreatureScatter.Kind.Shield, 1, 120f, "SHIELD FOR 2 MINUTES!"),
            (CreatureScatter.Kind.SuperJump, 3, 60f, "SUPER JUMP x3!"),
            (CreatureScatter.Kind.SpeedBoost, 3, 60f, "ZOOOOOM x3!"),
            (CreatureScatter.Kind.Stamina, 1000, 5f, "ENDLESS STAMINA!"),
        };
        // A ring of 20 giant gifts round the start
        for (int i = 0; i < 20; i++)
        {
            float a = i / 20f * Mathf.PI * 2f;
            var g = gifts[i % gifts.Length];
            ObstacleBase o = Spawn(G(Mathf.Sin(a) * 16f, -200f + Mathf.Cos(a) * 16f), g.kind, ObstacleSpawnType.Static, null, 2.5f);
            Mega(o, g.amount, g.time, g.msg);
            Folder(o, folder, "Giant " + ScatterTools.KindName(g.kind));
        }
    }

    // ------------------------------------------------------------------ Wacky combinations

    /// <summary>A giant BOUNCY dinosaur: run into it and it throws you up onto an island in the sky.</summary>
    private static void DinoLauncher()
    {
        const string folder = "Dino launcher";
        Vector3 at = G(-60f, -150f);
        Sign(G(-48f, -170f), "4. Dino launcher",
            "A GIANT BOUNCY DINOSAUR (size 30, Launch force 80, Straight up 1). Run into it and it throws you 100 m up!\n" +
            "Run at it from the island side, then steer onto the sky island as you fall. Prizes up there! A creature can be bouncy instead of hurting you.");

        ObstacleBase dino = Spawn(at, CreatureScatter.Kind.BounceBall, ObstacleSpawnType.Static, Model("Dinosaur"), 30f);
        if (dino != null)
        {
            dino.launchForce = 80f;
            dino.upwardBias = 1f; // straight up, beside the island (not into it)
            dino.hitZoneShape = ObstacleBase.HitZoneShape.Cylinder;
            dino.momentumTransfer = 0f;
            dino.sparkles = true;
            Turn(dino, 90f);
            dino.UpdateHitZone(true);
        }
        Folder(dino, folder, "Bouncy Giant Dinosaur");
        KeepClear(at, 30f);

        // The island: a huge mossy rock 70 m up, a bit to the side of the dinosaur
        PlatformLogic island = BigPlatform(at + new Vector3(27f, 59f, 0f), 26f, "Rock_Moss_2", ScatterTools.PlatformMove.Still, 0f, 0f, folder, "Dino Sky Island");
        island.safeLanding = true;
        TreesOn(island, 3, "CommonTree_Autumn_", 2.5f, 3.5f);
        for (int i = 0; i < 4; i++) Mega(OnPlatform(island, i % 2 == 0 ? CreatureScatter.Kind.Shield : CreatureScatter.Kind.SuperJump, folder), i % 2 == 0 ? 1 : 3, 90f, "SKY PRIZE!");
    }

    /// <summary>Giant glowing crystals (rocks) with pickups on TOP of them, and sharks swimming round them... in the air.</summary>
    private static void CrystalField()
    {
        const string folder = "Crystal field";
        Vector3 middle = G(70f, -140f);
        Sign(G(48f, -170f), "5. Crystal field",
            "GIANT GLOWING CRYSTALS (snowy rocks, size 6-10, glow + sparkles) with pickups on TOP. Sharks circle them... in the AIR\n" +
            "(Creatures > Scatter tools > Fly this high). 'Landing on them never hurts' is ticked on the crystals.");

        GameObject[] crystals = Models("Rock_Snow_");
        SceneryPopulator.SceneryGroup group = Group("Glowing Crystals", "Rock_Snow", new Vector2(6f, 10f), solid: true);
        group.glow = true; group.glowColour = new Color(0.4f, 0.95f, 1f);
        group.sparkles = true; group.sparkleStyle = AmbientSparkles.Style.Shimmer; group.sparkleColour = new Color(0.6f, 1f, 1f);
        group.safeLanding = true;
        group.include = false; // placed by hand here

        for (int i = 0; i < 10; i++)
        {
            float a = i / 10f * Mathf.PI * 2f;
            Vector3 p = middle + new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * (i % 2 == 0 ? 22f : 12f);
            p.y = GroundY(p.x, p.z);
            GameObject rock = scenery.PlaceOne(group, p);
            if (rock == null) continue;
            Glow glow = rock.GetComponent<Glow>();
            if (glow != null) { glow.strength = 3f; glow.lightRange = 25f; }
            Physics.SyncTransforms();
            Bounds b = TutorialLevelBuilder.RendererBounds(rock.transform);

            // A pickup sitting on the top of the crystal
            CreatureScatter.Kind kind = i % 3 == 0 ? CreatureScatter.Kind.Health : i % 3 == 1 ? CreatureScatter.Kind.SpeedBoost : CreatureScatter.Kind.SuperJump;
            ObstacleBase prize = Spawn(new Vector3(b.center.x, GroundY(b.center.x, b.center.z), b.center.z), kind, ObstacleSpawnType.Static, null, 1.5f);
            if (prize is TerrainObstacle t) ScatterTools.SetFlyHeight(t, b.max.y - GroundY(b.center.x, b.center.z) - 0.3f);
            Mega(prize, kind == CreatureScatter.Kind.Health ? 500 : 3, 45f, "CRYSTAL POWER!");
            Folder(prize, folder, "Crystal-top " + ScatterTools.KindName(kind));
        }
        KeepClear(middle, 30f);

        // Flying sharks round the crystals
        ScatterGroup(new CreatureScatter.Group { name = "Air sharks", kind = CreatureScatter.Kind.Wanderer, howMany = 8, sizeRange = new Vector2(6f, 9f),
            speedRange = new Vector2(10f, 18f), roamRange = new Vector2(25f, 30f), flyHeightRange = new Vector2(6f, 14f), jumpsToDefeat = 3,
            models = { Model("Shark") } }, middle, 60f, folder);
    }

    /// <summary>A GIANT tree stump spinning in the sky like a merry-go-round, with trees, spiders, a ring of power-ups and a teleporter on it.</summary>
    private static void Carousel()
    {
        const string folder = "Power-up carousel";
        Vector3 at = G(60f, -60f, 45f);
        Sign(G(20f, -150f), "6. Power-up carousel",
            "Over there: a GIANT TREE STUMP spinning in the sky with trees, spiders and a ring of power-ups ON it!\n" +
            "Take the purple teleporter by the start to get on. Spin speed 90 degrees a second (the slider goes to 3600!).");

        PlatformLogic stump = BigPlatform(at, 40f, "TreeStump_Moss", ScatterTools.PlatformMove.Spinning, 0f, 0f, folder, "Spinning Stump Carousel");
        stump.rotationSpeedY = 90f;
        stump.safeLanding = true;
        TreesOn(stump, 4, "PineTree_Autumn_", 2f, 3f, ring: 14f);
        Physics.SyncTransforms();

        // A ring of power-ups round the edge (they ride the spin)
        ScatterOnto(stump, folder, new CreatureScatter.Group { name = "Carousel prizes", kind = CreatureScatter.Kind.SpeedBoost, howMany = 6, sizeRange = new Vector2(2f, 2f),
            glow = true, onGround = false, onMovingPlatforms = true });
        ScatterOnto(stump, folder, new CreatureScatter.Group { name = "Carousel spiders", kind = CreatureScatter.Kind.Wanderer, howMany = 5, sizeRange = new Vector2(2.5f, 4f),
            speedRange = new Vector2(3f, 6f), roamRange = new Vector2(6f, 10f), models = { Model("Spider") }, onGround = false, onMovingPlatforms = true });

        Vector3 top = stump.TargetChild.position;
        top += new Vector3(0f, 0f, 3f);
        top.y = Surface(stump, top);
        Teleporter pad = TeleporterTools.CreatePair(G(-10f, -192f), null, top, stump, new Color(0.6f, 0.35f, 1f));
        pad.name = "Teleporter to the Carousel";
        pad.sendsYouTo.name = "Carousel Teleporter";
        Folder(pad, folder);
        Folder(pad.sendsYouTo, folder);
        KeepClear(G(-10f, -192f), 4f);
    }

    /// <summary>An INVISIBLE bridge (100% see-through platforms) across a gap, marked only by fireflies, with a glass ghost house of crumbling platforms.</summary>
    private static void InvisibleBridge()
    {
        const string folder = "Invisible bridge";
        Sign(G(-20f, -150f), "7. Invisible bridge",
            "An INVISIBLE BRIDGE: platforms 100% see-through (the 👻 See-through slider), shown only by their fireflies.\n" +
            "It leads to a GHOST DRAGON's platform. Find your way across... and don't fall!");

        Vector3 from = G(-30f, -140f);
        for (int i = 0; i < 8; i++)
        {
            Vector3 p = from + new Vector3(-6f * i, 3f + i * 1.2f, 4f * Mathf.Sin(i));
            PlatformLogic step = ScatterTools.CreatePlatformAt(platforms, p, 3.5f, 3.5f, 0.6f, 0f, null);
            ScatterTools.SetMovement(step, ScatterTools.PlatformMove.Still, 0f, 1f);
            if (i % 3 == 2) ScatterTools.SetPlatformType(step, PlatformLogic.PlatformType.Crumbling);
            SeeThrough s = step.TargetChild.gameObject.AddComponent<SeeThrough>();
            s.howSeeThrough = 100f;
            AddSparkles(step.TargetChild.gameObject, AmbientSparkles.Style.Fireflies, new Color(0.7f, 1f, 0.6f), 12f);
            Folder(step, folder, "Invisible Step " + (i + 1));
            KeepClear(p, 5f);
        }

        // At the end: a glass platform with a see-through GHOST DRAGON guarding a prize
        Vector3 end = from + new Vector3(-56f, 14f, 0f);
        PlatformLogic glass = ScatterTools.CreatePlatformAt(platforms, end, 16f, 16f, 1f, 0f, null);
        ScatterTools.SetMovement(glass, ScatterTools.PlatformMove.Still, 0f, 1f);
        glass.TargetChild.gameObject.AddComponent<SeeThrough>().howSeeThrough = 60f;
        ScatterTools.AddGlow(glass.TargetChild.gameObject, new Color(0.6f, 0.9f, 1f));
        Folder(glass, folder, "Glass Platform");
        KeepClear(end, 12f);
        Physics.SyncTransforms();
        ObstacleBase ghost = null;
        foreach (ObstacleBase o in ScatterOnto(glass, folder, new CreatureScatter.Group { name = "Ghost dragon", kind = CreatureScatter.Kind.Patroller, howMany = 1,
                     sizeRange = new Vector2(6f, 6f), speedRange = new Vector2(2f, 3f), roamRange = new Vector2(7f, 7f), jumpsToDefeat = 5,
                     models = { Model("Dragon") }, onGround = false, onStillPlatforms = true }))
        {
            ghost = o;
            o.name = "Ghost Dragon";
            o.gameObject.AddComponent<SeeThrough>().howSeeThrough = 65f;
            AddSparkles(o.gameObject, AmbientSparkles.Style.Shimmer, new Color(0.7f, 0.9f, 1f), 30f);
        }
        Mega(OnPlatform(glass, CreatureScatter.Kind.Shield, folder), 1, 180f, "GHOST SHIELD: 3 MINUTES!");
    }

    /// <summary>A teleporter that drops you from 150 m up onto a bouncy pad (arrive above = 150).</summary>
    private static void SkyDrop()
    {
        const string folder = "Sky drop";
        Vector3 pad = G(110f, -40f);
        Sign(G(14f, -210f), "8. Sky drop",
            "The ORANGE teleporter drops you from 150 m up ('Arrive this high above')!\n" +
            "Aim for the giant bouncy pad far below, which throws you back up. Landing on it never hurts.");

        PlatformLogic trampoline = ScatterTools.CreatePlatformAt(platforms, pad + Vector3.up * 1.5f, 20f, 20f, 1f, 0f, null);
        ScatterTools.SetMovement(trampoline, ScatterTools.PlatformMove.Still, 0f, 1f);
        ScatterTools.SetPlatformType(trampoline, PlatformLogic.PlatformType.Bouncy);
        trampoline.bounceStrength = 45f;
        trampoline.landEffect = Sparkles.Effect.Explosion;
        trampoline.effectAmount = 5f;
        trampoline.effectSize = 3f;
        Folder(trampoline, folder, "Giant Trampoline");
        KeepClear(pad, 20f);
        Physics.SyncTransforms();

        Vector3 top = trampoline.TargetChild.position;
        top.y = Surface(trampoline, top);
        Teleporter up = TeleporterTools.CreatePair(G(14f, -198f), null, top, trampoline, new Color(1f, 0.55f, 0.15f));
        up.name = "Sky Drop Teleporter";
        Teleporter landing = up.sendsYouTo.GetComponent<Teleporter>();
        landing.name = "Sky Drop Landing";
        landing.arriveAbove = 150f;
        landing.message = "SKY DROP!!!";
        up.message = "SKY DROP!!!";
        up.arriveAbove = 0.5f;
        Folder(up, folder);
        Folder(landing, folder);
        KeepClear(G(14f, -198f), 4f);
    }

    /// <summary>Spiders you jump on send you flying 70 m up (Bounce off its head = 70).</summary>
    private static void TrampolineSpiders()
    {
        const string folder = "Trampoline spiders";
        Vector3 at = G(-70f, -60f);
        Sign(G(-14f, -150f), "9. Trampoline spiders",
            "TRAMPOLINE SPIDERS: jump on their heads and you fly 70 m up ('Bounce off its head' slider). They take 50 jumps to beat!\n" +
            "Jump on one from the platform beside them.");
        foreach (ObstacleBase o in ScatterGroup(new CreatureScatter.Group { name = "Trampoline spiders", kind = CreatureScatter.Kind.Wanderer, howMany = 6,
                     sizeRange = new Vector2(2.5f, 3f), speedRange = new Vector2(1f, 2f), roamRange = new Vector2(8f, 10f), jumpsToDefeat = 50,
                     models = { Model("Spider") } }, at, 30f, folder))
        {
            o.stompBounce = 70f;
            o.safeLanding = true;
            o.payloadAmount = 1;
        }
        PlatformLogic jumpFrom = ScatterTools.CreatePlatformAt(platforms, at + new Vector3(0f, 4f, -16f), 5f, 5f, 0.6f, 0f, null);
        ScatterTools.SetMovement(jumpFrom, ScatterTools.PlatformMove.Still, 0f, 1f);
        Folder(jumpFrom, folder, "Spider Jump-off");
        KeepClear(at, 20f);
    }

    // ------------------------------------------------------------------ Huge sky islands (scenery models as platforms)

    /// <summary>
    /// Huge rocks, stumps and logs floating in the sky as platforms: spinning (some ultra fast), flying side to side 150 m,
    /// elevators going 150 m up. Many have trees, creatures and pickups ON them.
    /// </summary>
    private static void SkyIslands()
    {
        const string folder = "Sky islands";
        string[] shapes = { "Rock_Moss_1", "Rock_Moss_3", "Rock_Moss_4", "Rock_Moss_6", "Rock_2", "Rock_5", "Rock_7", "TreeStump", "TreeStump_Moss", "WoodLog_Moss", "Rock_Snow_4" };
        int made = 0;
        for (int attempt = 0; attempt < 400 && made < 34; attempt++)
        {
            Vector3 p = new Vector3(Random.Range(-320f, 320f), 0f, Random.Range(-320f, 320f));
            if (Mathf.Abs(p.x) < 45f && p.z > -230f && p.z < -40f) continue; // the start and the boost tower
            float size = made < 12 ? Random.Range(40f, 80f) : Random.Range(14f, 30f);
            if (keepClear.Any(k => Vector2.Distance(new Vector2(k.pos.x, k.pos.z), new Vector2(p.x, p.z)) < k.radius + size * 0.6f)) continue;
            p.y = GroundY(p.x, p.z) + Random.Range(35f, 150f);

            ScatterTools.PlatformMove move;
            float distance = 0f, speed = 0f;
            int roll = made % 6;
            if (roll <= 1) { move = ScatterTools.PlatformMove.Spinning; speed = made % 4 == 0 ? Random.Range(360f, 720f) : Random.Range(30f, 120f); }
            else if (roll == 2) { move = ScatterTools.PlatformMove.SideToSide; distance = Random.Range(60f, 150f); speed = Random.Range(10f, 40f); }
            else if (roll == 3) { move = ScatterTools.PlatformMove.UpAndDown; distance = Random.Range(60f, 150f); speed = Random.Range(8f, 25f); }
            else if (roll == 4) { move = ScatterTools.PlatformMove.Growing; speed = Random.Range(1f, 3f); }
            else move = ScatterTools.PlatformMove.Still;

            string shape = shapes[Random.Range(0, shapes.Length)];
            PlatformLogic island = BigPlatform(p, size, shape, move, distance, speed, folder, null);
            if (island == null) continue;
            if (move == ScatterTools.PlatformMove.Growing) { island.minScale = 0.4f; island.maxScale = 2.5f; }
            island.name = (size >= 40f ? "Huge " : "") + ScatterTools.MoveName(move) + " " + shape.Replace("_", " ") + " Island";
            KeepClear(p, size * 0.6f + distance * 0.5f);

            // Trees on most of them, giant ones on the biggest
            if (made % 3 != 2 && move != ScatterTools.PlatformMove.Growing) TreesOn(island, size >= 40f ? 5 : 2, made % 2 == 0 ? "CommonTree_" : "PineTree_", size >= 40f ? 3f : 1.5f, size >= 40f ? 6f : 2.5f);
            made++;
        }
    }

    /// <summary>Lots of ordinary platforms, scattered with the Platforms tab's Scatter tool and every slider maxed.</summary>
    private static void ScatteredPlatforms()
    {
        PlatformScatter ps = platforms.GetComponent<PlatformScatter>();
        if (ps == null) ps = platforms.gameObject.AddComponent<PlatformScatter>();
        HashSet<PlatformLogic> before = new HashSet<PlatformLogic>(Object.FindObjectsByType<PlatformLogic>(FindObjectsSortMode.None));

        ps.howMany = 90;
        ps.sizeRange = new Vector2(4f, 30f);
        ps.thicknessRange = new Vector2(0.6f, 4f);
        ps.heightRange = new Vector2(5f, 120f);
        ps.still = true; ps.sideToSide = true; ps.upAndDown = true; ps.spinning = true; ps.growing = true;
        ps.moveDistanceRange = new Vector2(10f, 200f);
        ps.speedRange = new Vector2(2f, 40f);
        ps.crumblingPercent = 10;
        ps.bouncyPercent = 20;
        ps.models = Models("Rock_Moss_").ToList();
        ps.place = new ScatterPlace { area = ScatterArea.WholeLevel, maxSlope = 90f, spacing = 6f, keepAwayFromStart = 40f, edgePadding = 120f };
        ScatterTools.ScatterPlatforms(platforms, ps);

        ps.howMany = 60;
        ps.models = new List<GameObject>();
        ps.sizeRange = new Vector2(3f, 12f);
        ScatterTools.ScatterPlatforms(platforms, ps);

        foreach (PlatformLogic p in Object.FindObjectsByType<PlatformLogic>(FindObjectsSortMode.None).Where(p => !before.Contains(p)))
        {
            // Away from the boost tower
            if (keepClear.Take(PadCount + 3).Any(k => Vector3.Distance(k.pos, p.TargetChild.position) < k.radius + Mathf.Max(p.widthOffset, p.depthOffset)))
            { Object.DestroyImmediate(p.gameObject); continue; }
            if (ScatterTools.GetMovement(p) == ScatterTools.PlatformMove.Spinning) p.rotationSpeedY = Mathf.Clamp(p.rotationSpeedY, -360f, 360f);
            Folder(p, "Scattered platforms");
        }
    }

    // ------------------------------------------------------------------ Scenery: hundreds, lots of them enormous

    private static int Scenery()
    {
        if (scenery.groups.Count == 0) typeof(SceneryPopulator).GetMethod("Reset", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(scenery, null);
        scenery.edgePadding = 20f;
        scenery.maxSlope = 40f;
        SceneryPopulator.SceneryGroup trees = scenery.groups.Find(g => g.name == "Trees");
        SceneryPopulator.SceneryGroup rocks = scenery.groups.Find(g => g.name == "Rocks");
        SceneryPopulator.SceneryGroup plants = scenery.groups.Find(g => g.name == "Plants & Grass");
        foreach (SceneryPopulator.SceneryGroup g in new[] { trees, rocks, plants }) if (g != null) scenery.QuickFill(g);

        SceneryPopulator.SceneryGroup giantTrees = Group("Giant Trees", "Tree, Willow -Stump", new Vector2(6f, 15f), solid: true);
        SceneryPopulator.SceneryGroup giantRocks = Group("Giant Rocks", "Rock -Snow", new Vector2(6f, 25f), solid: true);
        giantRocks.tiltWithGround = true;
        SceneryPopulator.SceneryGroup glowTrees = Group("Glowing Firefly Trees", "Willow_Autumn, BirchTree_Autumn", new Vector2(5f, 9f), solid: true);
        glowTrees.glow = true; glowTrees.glowColour = new Color(1f, 0.6f, 0.2f);
        glowTrees.sparkles = true; glowTrees.sparkleStyle = AmbientSparkles.Style.Fireflies;

        if (trees != null) { trees.howMany = 350; scenery.Scatter(trees); }
        if (rocks != null) { rocks.howMany = 120; scenery.Scatter(rocks); }
        if (plants != null) { plants.howMany = 500; scenery.Scatter(plants); }
        giantTrees.howMany = 180; scenery.Scatter(giantTrees);
        giantRocks.howMany = 90; scenery.Scatter(giantRocks);
        glowTrees.howMany = 25; scenery.Scatter(glowTrees);

        // Keep the start, the tower, and the wacky stations clear
        Transform root = scenery.transform.Find(SceneryPopulator.ContainerName);
        if (root != null)
        {
            List<GameObject> remove = new List<GameObject>();
            foreach (Transform group in root)
            {
                if (group.name == "Glowing Crystals") continue;
                foreach (Transform piece in group)
                {
                    Vector3 p = piece.position;
                    bool giant = piece.localScale.x > 5f;
                    bool inTower = Mathf.Abs(p.x) < (giant ? 50f : 25f) && p.z > -235f && p.z < -50f;
                    if (inTower || keepClear.Any(k => Vector2.Distance(new Vector2(k.pos.x, k.pos.z), new Vector2(p.x, p.z)) < k.radius + (giant ? 20f : 4f)))
                        remove.Add(piece.gameObject);
                }
            }
            foreach (GameObject go in remove) Object.DestroyImmediate(go);
        }

        // Health hidden in the tops of some glowing trees (they float up there)
        Transform glowing = root != null ? root.Find(glowTrees.name) : null;
        int n = 0;
        if (glowing != null)
        {
            foreach (Transform tree in glowing.Cast<Transform>().Take(10).ToList())
            {
                Physics.SyncTransforms();
                Bounds b = TutorialLevelBuilder.RendererBounds(tree);
                float ground = GroundY(tree.position.x, tree.position.z);
                ObstacleBase prize = Spawn(new Vector3(tree.position.x, ground, tree.position.z), CreatureScatter.Kind.Health, ObstacleSpawnType.Static, null, 2f);
                if (prize is TerrainObstacle t) ScatterTools.SetFlyHeight(t, b.max.y - ground + 1f);
                Mega(prize, 300, 5f, "TREETOP HEALTH!");
                Folder(prize, "Treetop prizes", "Treetop Health " + (++n));
            }
        }

        return root != null ? root.Cast<Transform>().Sum(g => g.childCount) : 0;
    }

    // ------------------------------------------------------------------ Racers and stampedes

    /// <summary>Power-ups and bounce balls racing round the valley: catch them if you can.</summary>
    private static void Stampedes()
    {
        const string folder = "Stampedes";
        Sign(G(-14f, -205f), "10. Catch them!",
            "Pickups that RUN: 'They run around' in the scatter tool, speed up to 90 m/s.\n" +
            "Grab a Speed Boost (x3!) and chase them. Watch out for the giant bounce balls...");

        ScatterGroup(new CreatureScatter.Group { name = "Power-up stampede", kind = CreatureScatter.Kind.SpeedBoost, howMany = 40, sizeRange = new Vector2(2f, 4f),
            pickupsWander = true, speedRange = new Vector2(30f, 90f), roamRange = new Vector2(100f, 200f), glow = true }, null, 0f, folder,
            o => Mega(o, 3, 45f, "CAUGHT ONE! x3 SPEED!"));
        ScatterGroup(new CreatureScatter.Group { name = "Runaway health", kind = CreatureScatter.Kind.Health, howMany = 40, sizeRange = new Vector2(2f, 3f),
            pickupsWander = true, speedRange = new Vector2(20f, 60f), roamRange = new Vector2(80f, 150f), glow = true }, null, 0f, folder,
            o => Mega(o, 250, 5f, "+250 HEALTH!"));
        ScatterGroup(new CreatureScatter.Group { name = "Floating super jumps", kind = CreatureScatter.Kind.SuperJump, howMany = 30, sizeRange = new Vector2(2f, 3f),
            pickupsWander = true, speedRange = new Vector2(10f, 30f), roamRange = new Vector2(50f, 100f), flyHeightRange = new Vector2(2f, 8f), glow = true }, null, 0f, folder,
            o => Mega(o, 3, 60f, "SUPER JUMP x3!"));
        ScatterGroup(new CreatureScatter.Group { name = "Giant bounce balls", kind = CreatureScatter.Kind.BounceBall, howMany = 25, sizeRange = new Vector2(4f, 10f),
            pickupsWander = true, speedRange = new Vector2(15f, 45f), roamRange = new Vector2(60f, 150f) }, null, 0f, folder,
            o => { o.launchForce = Random.Range(40f, 90f); o.upwardBias = 0.2f; });

        // A conveyor of shields racing back and forth at 150 m/s
        for (int i = 0; i < 20; i++)
        {
            ObstacleBase o = Spawn(G(-150f + i * 3f, 60f), CreatureScatter.Kind.Shield, ObstacleSpawnType.Patrol, null, 2f, speed: new Vector2(150f, 150f), roam: 300f);
            Turn(o, 90f);
            Mega(o, 1, 120f, "CONVEYOR SHIELD!");
            Folder(o, folder, "Conveyor Shield " + (i + 1));
        }
    }

    /// <summary>Hundreds of creatures: giant spiders, huge dinosaurs, flying dragons, racing foxes and horses, giant snakes.</summary>
    private static void Creatures()
    {
        const string folder = "Mega creatures";
        Sign(G(5f, -215f), "11. Mega creatures",
            "Every creature here was made with Creatures > Scatter tools and the sliders maxed: spiders size 10-25,\n" +
            "dinosaurs 25-50, dragons FLYING 15-60 m up, foxes and horses racing at 150 m/s. They're in the Creatures list!");

        CreatureScatter.Group[] groups =
        {
            new CreatureScatter.Group { name = "Enormous spiders", kind = CreatureScatter.Kind.Wanderer, howMany = 40, sizeRange = new Vector2(10f, 25f),
                speedRange = new Vector2(15f, 40f), roamRange = new Vector2(60f, 150f), jumpsToDefeat = 8, models = { Model("Spider") } },
            new CreatureScatter.Group { name = "Huge dinosaurs", kind = CreatureScatter.Kind.Patroller, howMany = 16, sizeRange = new Vector2(25f, 50f),
                speedRange = new Vector2(10f, 30f), roamRange = new Vector2(100f, 250f), jumpsToDefeat = 20, models = { Model("Dinosaur") } },
            new CreatureScatter.Group { name = "Dragons", kind = CreatureScatter.Kind.Wanderer, howMany = 24, sizeRange = new Vector2(15f, 40f),
                speedRange = new Vector2(30f, 80f), roamRange = new Vector2(150f, 300f), flyHeightRange = new Vector2(15f, 60f), jumpsToDefeat = 10, models = { Model("Dragon") } },
            new CreatureScatter.Group { name = "Racers", kind = CreatureScatter.Kind.Patroller, howMany = 70, sizeRange = new Vector2(2f, 4f),
                speedRange = new Vector2(60f, 150f), roamRange = new Vector2(200f, 400f), jumpsToDefeat = 1, models = { Model("Fox"), Model("Horse"), Model("Zombie") } },
            new CreatureScatter.Group { name = "Seagull flock", kind = CreatureScatter.Kind.Wanderer, howMany = 30, sizeRange = new Vector2(3f, 8f),
                speedRange = new Vector2(40f, 100f), roamRange = new Vector2(100f, 250f), flyHeightRange = new Vector2(5f, 30f), models = { Model("Seagull") } },
            new CreatureScatter.Group { name = "Giant snake guards", kind = CreatureScatter.Kind.Guard, howMany = 8, sizeRange = new Vector2(30f, 50f),
                jumpsToDefeat = 15, models = { Model("Snake") } },
            new CreatureScatter.Group { name = "Tiny spider swarm", kind = CreatureScatter.Kind.Wanderer, howMany = 50, sizeRange = new Vector2(0.4f, 0.7f),
                speedRange = new Vector2(4f, 8f), roamRange = new Vector2(10f, 20f), models = { Model("Spider") } },
        };
        foreach (CreatureScatter.Group g in groups)
        {
            ScatterGroup(g, null, 0f, folder, o =>
            {
                if (g.name == "Tiny spider swarm") o.payloadAmount = 1;
                if (g.name == "Huge dinosaurs" || g.name == "Giant snake guards") o.payloadAmount = 40;
            });
        }

        // One dinosaur CHASES you at 40 m/s from 150 m away
        ObstacleBase chaser = Spawn(G(120f, 80f), CreatureScatter.Kind.Patroller, ObstacleSpawnType.Patrol, Model("Dinosaur"), 20f, speed: new Vector2(5f, 8f), roam: 30f);
        if (chaser != null)
        {
            chaser.chasePlayer = true;
            chaser.chaseStartDistance = 150f;
            chaser.chaseGiveUpDistance = 400f;
            chaser.chaseSpeed = 40f;
            chaser.jumpsToDefeat = 10;
            chaser.payloadAmount = 50;
        }
        Folder(chaser, folder, "Chasing T-Rex");
    }

    /// <summary>Two 'become a character' pickups either side of the start: Roses Fox (a student's) and a flying dragon.</summary>
    private static void BecomeCharacters()
    {
        const string folder = "Become a character";
        Sign(G(-1f, -191f), "12. Become a character",
            "Touch the FOX to turn into Roses Fox (a student's model!): super fast for 30 seconds.\n" +
            "Touch the DRAGON to become a dragon that FLIES: jump, then hold Space to go up and Ctrl to come down. Creatures need half the jumps!\n" +
            "Make one: Creatures & Pickups > Become a Character (or Become a Flyer), then pick any model.");

        BecomePickup(G(-10f, -196f), "Roses Fox - Rigged & Animated (C)", false, folder, "Become Roses Fox", 1.8f, s =>
        {
            s.changeSpeed = true; s.moveSpeed = 25f;
            s.changeJump = true; s.jumpHeight = 16f;
            s.noFallDamage = true;
        });
        BecomePickup(G(10f, -196f), "Dragon-model", true, folder, "Become a Dragon", 2.5f, s =>
        {
            s.look.size = 2f;
            s.flyUpSpeed = 15f; s.flyDownSpeed = 15f;
            s.noFallDamage = true;
            s.creatureHits = CreatureHitsChange.DivideBy; s.creatureHitsAmount = 2;
        });
    }

    private static void BecomePickup(Vector3 at, string modelName, bool canFly, string folder, string name, float scale, System.Action<BecomeSettings> settings)
    {
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelFolder + modelName + ".glb");
        ObstacleBase o = Spawn(at, CreatureScatter.Kind.Health, ObstacleSpawnType.Static, model, scale); // the pickup looks like who you become
        if (o == null) return;
        o.payloadType = ObstacleBase.PayloadType.BecomeCharacter;
        o.payloadAmount = 1;
        o.buffDuration = 30f;
        o.destroyOnTrigger = false; // touch it again any time
        o.showPickupMessage = true; o.pickupMessage = ""; // the automatic message says who you are (and how to fly)
        o.become = new BecomeSettings { canFly = canFly };
        o.become.look.model = model;
        BecomeEditor.FindAnimations(o);
        settings(o.become);
        Turn(o, 180f); // facing the start
        Glow glow = ScatterTools.AddGlow(o.gameObject, Sparkles.ColourFor(o.payloadType));
        glow.colour = Sparkles.ColourFor(o.payloadType);
        glow.strength = 2f;
        AddSparkles(o.gameObject, AmbientSparkles.Style.Fireflies, Sparkles.ColourFor(o.payloadType), 20f);
        EditorUtility.SetDirty(o);
        Folder(o, folder, name);
    }

    /// <summary>Creatures and pickups ON the platforms: spiders, dinosaurs on sky islands, pickups riding spinners.</summary>
    private static void PlatformCrowds()
    {
        const string folder = "Platform crowds";
        ScatterGroup(new CreatureScatter.Group { name = "Sky spiders", kind = CreatureScatter.Kind.Wanderer, howMany = 50, sizeRange = new Vector2(2f, 6f),
            speedRange = new Vector2(2f, 8f), roamRange = new Vector2(4f, 20f), jumpsToDefeat = 2, models = { Model("Spider") },
            onGround = false, onStillPlatforms = true, onMovingPlatforms = true }, null, 0f, folder);
        ScatterGroup(new CreatureScatter.Group { name = "Sky dinosaurs", kind = CreatureScatter.Kind.Guard, howMany = 10, sizeRange = new Vector2(8f, 14f),
            jumpsToDefeat = 4, models = { Model("Dinosaur") }, onGround = false, onStillPlatforms = true, onMovingPlatforms = true }, null, 0f, folder);
        ScatterGroup(new CreatureScatter.Group { name = "Sky pickups", kind = CreatureScatter.Kind.Health, howMany = 40, sizeRange = new Vector2(1.5f, 3f), glow = true,
            onGround = false, onStillPlatforms = true, onMovingPlatforms = true }, null, 0f, folder, o => Mega(o, 200, 5f, "SKY HEALTH +200!"));
        ScatterGroup(new CreatureScatter.Group { name = "Spinner shields", kind = CreatureScatter.Kind.Shield, howMany = 30, sizeRange = new Vector2(1.5f, 3f), glow = true,
            onGround = false, onMovingPlatforms = true }, null, 0f, folder, o => Mega(o, 1, 60f, "SPINNER SHIELD!"));
    }

    // ------------------------------------------------------------------ Helpers

    /// <summary>The height of a platform's own surface at a spot (its model or cylinder; not trees standing on it).</summary>
    private static float Surface(PlatformLogic p, Vector3 at)
    {
        Physics.SyncTransforms();
        float best = float.NegativeInfinity;
        Transform holder = p.TargetChild.Find(PlatformSceneryHolder.HolderName);
        foreach (RaycastHit h in Physics.RaycastAll(new Vector3(at.x, at.y + 300f, at.z), Vector3.down, 600f, Physics.AllLayers, QueryTriggerInteraction.Ignore))
        {
            if (!h.transform.IsChildOf(p.TargetChild) || (holder != null && h.transform.IsChildOf(holder))) continue;
            best = Mathf.Max(best, h.point.y);
        }
        return float.IsNegativeInfinity(best) ? TutorialLevelBuilder.TopOf(p.gameObject) : best;
    }

    private static float GroundY(float x, float z) => terrain.SampleHeight(new Vector3(x, 0f, z)) + terrain.GetPosition().y;
    private static Vector3 G(float x, float z, float up = 0f) => new Vector3(x, GroundY(x, z) + up, z);
    private static void KeepClear(Vector3 p, float radius) => keepClear.Add((p, radius));

    private static GameObject Model(string name) => AssetDatabase.LoadAssetAtPath<GameObject>(ModelFolder + name + "-model.glb");
    private static GameObject Nature(string name) => AssetDatabase.LoadAssetAtPath<GameObject>(TutorialLevelBuilder.NatureFolder + name + ".fbx");
    private static GameObject[] Models(string prefix) =>
        AssetDatabase.FindAssets(prefix + " t:Model", new[] { TutorialLevelBuilder.NatureFolder.TrimEnd('/') })
            .Select(AssetDatabase.GUIDToAssetPath).Where(p => Path.GetFileName(p).StartsWith(prefix))
            .Select(AssetDatabase.LoadAssetAtPath<GameObject>).Where(m => m != null).ToArray();

    private static void Sign(Vector3 post, string title, string message)
    {
        GameObject trigger = TutorialLevelBuilder.Sign(signs, signMaterial, post, post + new Vector3(post.x < 0f ? 4f : -4f, 1.5f, 0f), title, message);
        Folder(trigger.GetComponent<TriggerLogic>(), SignFolder);
        KeepClear(post, 4f);
    }

    /// <summary>A scenery group (its settings stay in the Scenery tab, so students can see the big sizes).</summary>
    private static SceneryPopulator.SceneryGroup Group(string name, string words, Vector2 size, bool solid)
    {
        SceneryPopulator.SceneryGroup g = scenery.groups.Find(x => x.name == name);
        if (g == null) { g = new SceneryPopulator.SceneryGroup { name = name }; scenery.groups.Add(g); }
        g.quickFillWords = words;
        g.sizeRange = size;
        g.solid = solid;
        g.models.Clear();
        scenery.QuickFill(g);
        return g;
    }

    /// <summary>A platform with a scenery model (rock, stump, log), any size, moving any way.</summary>
    private static PlatformLogic BigPlatform(Vector3 centre, float size, string model, ScatterTools.PlatformMove move, float distance, float speed, string folder, string name)
    {
        PlatformLogic p = ScatterTools.CreatePlatformAt(platforms, centre, size, size, Mathf.Max(1f, size * 0.08f), Random.Range(0f, 360f), Nature(model));
        if (p == null) return null;
        ScatterTools.SetMovement(p, move, distance, Mathf.Max(0.1f, speed));
        if (move == ScatterTools.PlatformMove.Spinning) p.rotationSpeedY = speed * (Random.value < 0.5f ? -1f : 1f);
        Folder(p, folder, name);
        return p;
    }

    /// <summary>Trees standing ON a platform (the Scenery tab's Place One, clicking a platform).</summary>
    private static void TreesOn(PlatformLogic platform, int count, string prefix, float minSize, float maxSize, float ring = 0f)
    {
        SceneryPopulator.SceneryGroup trees = scenery.groups.Find(g => g.name == "Trees") ?? Group("Trees", "Tree", new Vector2(1.5f, 2.5f), true);
        GameObject[] models = Models(prefix);
        if (models.Length == 0) return;
        Physics.SyncTransforms();
        Transform t = platform.TargetChild;
        float r = ring > 0f ? ring : Mathf.Min(platform.widthOffset, platform.depthOffset) * 0.28f;
        for (int i = 0; i < count; i++)
        {
            float a = (i + Random.value * 0.5f) / count * Mathf.PI * 2f;
            Vector3 p = t.position + new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * r * (ring > 0f ? 1f : Random.Range(0.4f, 1f));
            scenery.PlaceOnPlatform(trees, models[Random.Range(0, models.Length)], platform, p, Random.Range(minSize, maxSize));
        }
    }

    /// <summary>Runs one Creatures & Pickups scatter group (whole level, or round a spot). Returns what it made.</summary>
    private static List<ObstacleBase> ScatterGroup(CreatureScatter.Group g, Vector3? around, float areaSize, string folder, System.Action<ObstacleBase> then = null)
    {
        creatureScatter.groups.Add(g);
        g.include = false; // already scattered: a student's 'Scatter All' won't double them
        creatureScatter.place = around.HasValue
            ? new ScatterPlace { area = ScatterArea.AroundSceneView, areaSize = areaSize, spacing = 3f, keepAwayFromStart = 0f, maxSlope = 45f, edgePadding = 20f }
            : new ScatterPlace { area = ScatterArea.WholeLevel, spacing = 4f, keepAwayFromStart = 40f, maxSlope = 40f, edgePadding = 60f };
        HashSet<ObstacleBase> before = new HashSet<ObstacleBase>(Object.FindObjectsByType<ObstacleBase>(FindObjectsInactive.Include, FindObjectsSortMode.None));
        ScatterTools.PlaceAt = around;
        try { ScatterTools.ScatterCreatures(creatureScatter, g); }
        finally { ScatterTools.PlaceAt = null; }
        List<ObstacleBase> made = Object.FindObjectsByType<ObstacleBase>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(o => !before.Contains(o)).ToList();
        foreach (ObstacleBase o in made)
        {
            then?.Invoke(o);
            Folder(o, folder);
        }
        return made;
    }

    /// <summary>Creature scatter aimed at just one platform.</summary>
    private static List<ObstacleBase> ScatterOnto(PlatformLogic platform, string folder, CreatureScatter.Group g)
    {
        Physics.SyncTransforms();
        return ScatterGroup(g, platform.TargetChild.position, 2f, folder);
    }

    /// <summary>One pickup (or creature) on a platform.</summary>
    private static ObstacleBase OnPlatform(PlatformLogic platform, CreatureScatter.Kind kind, string folder)
    {
        bool moving = ScatterTools.GetMovement(platform) != ScatterTools.PlatformMove.Still;
        return ScatterOnto(platform, folder, new CreatureScatter.Group { name = "Prize: " + ScatterTools.KindName(kind), kind = kind, howMany = 1,
            sizeRange = new Vector2(1.5f, 1.5f), glow = true, onGround = false, onStillPlatforms = !moving, onMovingPlatforms = moving }).FirstOrDefault();
    }

    /// <summary>One thing on the ground, like the Game Builder's buttons (borrowing the ground spawner's settings).</summary>
    private static ObstacleBase Spawn(Vector3 point, CreatureScatter.Kind kind, ObstacleSpawnType movement, GameObject model, float scale,
        Vector2? speed = null, float roam = 10f)
    {
        TerrainPopulator spawner = Object.FindAnyObjectByType<TerrainPopulator>();
        HashSet<ObstacleBase> before = new HashSet<ObstacleBase>(Object.FindObjectsByType<ObstacleBase>(FindObjectsInactive.Include, FindObjectsSortMode.None));
        string saved = JsonUtility.ToJson(spawner);
        try
        {
            spawner.scatterObstacleType = movement;
            bool bouncy = kind == CreatureScatter.Kind.BounceBall;
            spawner.baseSettings.payloadType = PayloadOf(kind);
            spawner.baseSettings.payloadAmount = 10;
            spawner.baseSettings.buffDuration = 8f;
            spawner.baseSettings.destroyOnTrigger = !ScatterTools.IsCreature(kind) && !bouncy;
            spawner.baseSettings.isBouncy = bouncy;
            if (bouncy) spawner.baseSettings.launchForce = 30f;
            spawner.baseSettings.objectScale = scale;
            Vector2 s = speed ?? new Vector2(2f, 4f);
            spawner.wanderSettings.wanderRadius = roam; spawner.wanderSettings.minMoveSpeed = s.x; spawner.wanderSettings.maxMoveSpeed = s.y;
            spawner.patrolSettings.moveDistance = roam; spawner.patrolSettings.minMoveSpeed = s.x; spawner.patrolSettings.maxMoveSpeed = s.y;
            spawner.customModel = model;
            spawner.customMesh = null;
            spawner.CreateSingleObstacle();
        }
        finally { JsonUtility.FromJsonOverwrite(saved, spawner); }

        ObstacleBase made = Object.FindObjectsByType<ObstacleBase>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(o => !before.Contains(o));
        if (made == null) return null;
        Transform t = made.transform;
        float lift = t.position.y - GroundY(t.position.x, t.position.z);
        t.position = new Vector3(point.x, point.y + lift, point.z);
        if (!ScatterTools.IsCreature(kind) && kind != CreatureScatter.Kind.BounceBall) ScatterTools.AddGlow(made.gameObject, Sparkles.ColourFor(made.payloadType));
        made.name = ScatterTools.KindName(kind);
        return made;
    }

    private static ObstacleBase.PayloadType PayloadOf(CreatureScatter.Kind kind)
    {
        switch (kind)
        {
            case CreatureScatter.Kind.Health: return ObstacleBase.PayloadType.HealthBooster;
            case CreatureScatter.Kind.Stamina: return ObstacleBase.PayloadType.StaminaBooster;
            case CreatureScatter.Kind.Shield: return ObstacleBase.PayloadType.InvincibilityBuff;
            case CreatureScatter.Kind.SuperJump: return ObstacleBase.PayloadType.JumpBuff;
            case CreatureScatter.Kind.SpeedBoost: return ObstacleBase.PayloadType.SprintBuff;
            case CreatureScatter.Kind.BounceBall: return ObstacleBase.PayloadType.None;
        }
        return ObstacleBase.PayloadType.Damage;
    }

    /// <summary>Ridiculous pickup: a huge amount, a long time, and a shouty message.</summary>
    private static void Mega(ObstacleBase o, int amount, float seconds, string message)
    {
        if (o == null) return;
        o.payloadAmount = amount;
        o.buffDuration = seconds;
        o.showPickupMessage = true;
        o.pickupMessage = message;
        EditorUtility.SetDirty(o);
    }

    private static void Turn(ObstacleBase o, float yRotation)
    {
        if (o == null) return;
        o.initialYRotation = yRotation;
        o.transform.rotation = Quaternion.Euler(0f, yRotation, 0f);
    }

    private static void AddSparkles(GameObject go, AmbientSparkles.Style style, Color colour, float amount = 15f)
    {
        AmbientSparkles s = go.AddComponent<AmbientSparkles>();
        s.style = style;
        s.colour = colour;
        s.fromTop = true; s.fromSides = true; s.fromBottom = false; s.fromCentre = false;
        s.amount = amount;
    }

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
