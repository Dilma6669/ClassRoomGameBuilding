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
    private const string TemplateScene = "Assets/Scenes/Level01.unity";
    private const string AssetFolder = "Assets/Tutorial";
    private const string NatureFolder = "Assets/MyModels/NatureModelsPackTest/FBX/";

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

    /// <summary>Builds and saves the tutorial scene. Returns a short report.</summary>
    public static string Build()
    {
        if (!AssetDatabase.IsValidFolder(AssetFolder)) AssetDatabase.CreateFolder("Assets", "Tutorial");

        // 1. Start from the empty template level (it has all the controllers, HUD and pop-up dialog)
        if (File.Exists(ScenePath)) AssetDatabase.DeleteAsset(ScenePath);
        AssetDatabase.CopyAsset(TemplateScene, ScenePath);
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        // 2. Terrain: its own copy (so Level02's terrain isn't touched), with a meadow for the course
        Terrain terrain = CreateTutorialTerrain();
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
            "Welcome to Tutorial Island! Walk into each yellow sign to see how this level was made.\nWASD = move,  Space = jump,  Shift = sprint,  Esc = menu");

        Sign(signs, signMaterial, At(-4f, 8f), At(0f, 8f, 1.5f), "2. Pop-up signs",
            "Signs like this are Triggers: TriggerController > Create Trigger, then type your Message.\nTick 'Change Scene' to make one send players to another level.");

        Sign(signs, signMaterial, At(-4f, 17f), At(0f, 17f, 1.5f), "3. This is you",
            "You are the Old Man. To change your look: select PlayerController and drag a model\ninto 'Player Model'. Green bar = health (falls and creatures hurt), yellow = stamina.");

        // Scenery grove around station 3
        SceneryPopulator scenery = BuildScenery(terrain, At);
        Sign(signs, signMaterial, At(-4f, 30f), At(0f, 30f, 1.5f), "4. Scenery",
            "These trees and rocks came from the Scenery Tool. Select TerrainLogicDefault,\ntype words like Tree into 'Find models named', click Add, then Scatter.");

        // Platforms: plain, moving, rock
        PlatformController platforms = Object.FindAnyObjectByType<PlatformController>();
        Sign(signs, signMaterial, At(-6f, 50f), At(0f, 50f, 1.5f), "5. Platforms",
            "Platforms: PlatformController > Create New Platform. Change Width and Height,\nand tick Left/Right to make one move. Jump across them!");
        GameObject p1 = MakePlatform(platforms, At(0f, 60f), 1.2f, 6f);
        GameObject p2 = MakePlatform(platforms, At(0f, 69f), 2.4f, 5f);
        PlatformLogic moving = p2.GetComponent<PlatformLogic>();
        moving.moveX = true; moving.moveZ = false; moving.moveDistance = 6f; moving.moveSpeed = 1.5f;
        GameObject p3 = MakePlatform(platforms, At(0f, 78f), 3.4f, 6f);
        GameObject rock = AssetDatabase.LoadAssetAtPath<GameObject>(NatureFolder + "Rock_Moss_6.fbx");
        if (rock != null) PlatformController.ApplyPlatformModel(p3, rock);
        float rockTop = TopOf(p3);
        Sign(signs, signMaterial, At(-6f, 78f), new Vector3(Start.x, rockTop + 1.5f, Start.z + 78f), "6. Rock platform",
            "This platform is a ROCK! Select a platform and drag a model into 'Platform Model'.\nYou stand on its real shape, bumps and all.");

        // Creature arena
        GameObject arena = MakePlatform(platforms, At(0f, 91f), 3.4f, 12f);
        float arenaTop = TopOf(arena);
        AddCreatures(arena);
        Sign(signs, signMaterial, At(-9f, 91f), new Vector3(Start.x, arenaTop + 1.5f, Start.z + 88f), "7. Creatures",
            "Careful, creatures hurt! Made with 'Create Single Obstacle' on a platform.\nMovement can be Wander, Patrol or Static. The glowing ball heals you.");

        // Bounce pad on the ground
        AddBouncePad(terrain, At(0f, 110f));
        Sign(signs, signMaterial, At(-5f, 104f), At(0f, 104f, 1.5f), "8. Bounce!",
            "Jump onto the ball to get launched sky-high! Any obstacle can be bouncy:\ntick 'Is Bouncy' and change Launch Force to go higher.");

        // Checkpoint
        GameMenuItems.CreateCheckpoint();
        GameObject flag = Selection.activeGameObject;
        flag.transform.position = At(3f, 120f);
        Sign(signs, signMaterial, At(-4f, 120f), At(0f, 120f, 1.5f), "9. Checkpoint",
            "Checkpoint flags save your place: GameController > Create Checkpoint.\nIf you die after touching a flag, you start again from there.");

        Sign(signs, signMaterial, At(-4f, 132f), At(0f, 132f, 1.5f), "10. Library",
            "Press Esc > Library for the Story and backstories. Game > Settings and Library changes\nthe game title, death screen, default models and Library entries.");

        GameObject finish = Sign(signs, signMaterial, At(-4f, 146f), At(0f, 146f, 1.5f), "11. Finish!",
            "You finished! New level: Game > Add Open Scene to Build. Share your game: Game > Build Game.\nThis sign sends you to the next level. Now go and build your own!");
        TriggerLogic finishTrigger = finish.GetComponent<TriggerLogic>();
        finishTrigger.changeScene = true;
        finishTrigger.sceneToLoad = "Level01";

        // 4. Save and add to the build (after the student's levels)
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        AddToBuild();

        int sceneryCount = scenery.transform.Find(SceneryPopulator.ContainerName)?.GetComponentsInChildren<Renderer>().Length ?? 0;
        return $"Built Tutorial Island: 11 signs, 4 platforms, creatures, a bounce pad, a checkpoint and {sceneryCount} scenery pieces.";
    }

    // ------------------------------------------------------------------ Terrain

    private static Terrain CreateTutorialTerrain()
    {
        EnvironmentController env = Object.FindAnyObjectByType<EnvironmentController>();
        env.CreateTerrain();
        Terrain terrain = env.GetComponentInChildren<Terrain>();

        // Own copy of the terrain data, so sculpting here never changes other levels
        string dataPath = AssetFolder + "/TutorialTerrain.asset";
        AssetDatabase.DeleteAsset(dataPath);
        TerrainData copy = Object.Instantiate(terrain.terrainData);
        AssetDatabase.CreateAsset(copy, dataPath);
        terrain.terrainData = copy;
        TerrainCollider tc = terrain.GetComponent<TerrainCollider>();
        if (tc != null) tc.terrainData = copy;

        // Flatten a meadow for the course, blending smoothly into the mountains
        FlattenArea(terrain,
            new Rect(Start.x - 35f, Start.z - 25f, 70f, 200f),
            MeadowHeight, 30f);
        return terrain;
    }

    private static void FlattenArea(Terrain terrain, Rect area, float height, float blend)
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

        // A little grove around station 3 (both sides of the path, never on it)
        SceneryPopulator.SceneryGroup trees = scenery.groups.Find(g => g.name == "Trees");
        SceneryPopulator.SceneryGroup rocks = scenery.groups.Find(g => g.name == "Rocks");
        SceneryPopulator.SceneryGroup plants = scenery.groups.Find(g => g.name == "Plants & Grass");
        Random.InitState(42); // same layout every time it's rebuilt
        float[] treeSpots = { 22f, 27f, 33f, 38f, 44f };
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
        ClearCourse(scenery.transform.Find(SceneryPopulator.ContainerName), new Rect(Start.x - 7f, Start.z - 10f, 14f, 170f), new Rect(Start.x - 16f, Start.z + 18f, 32f, 32f));
        Random.InitState(System.Environment.TickCount);
        return scenery;
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
    private static GameObject MakePlatform(PlatformController controller, Vector3 groundPoint, float topHeight, float width)
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

    private static float TopOf(GameObject platform)
    {
        PlatformLogic logic = platform.GetComponent<PlatformLogic>();
        Transform target = logic.TargetChild != null ? logic.TargetChild : platform.transform.Find("⚠️ DO NOT TOUCH").GetChild(0);
        return RendererBounds(target).max.y;
    }

    private static Bounds RendererBounds(Transform root)
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
    }

    private static void Nudge(GameObject obstacle, float x, float z)
    {
        if (obstacle == null) return;
        FollowPlatform follow = obstacle.GetComponent<FollowPlatform>();
        if (follow != null) { follow.offsetX += x; follow.offsetZ += z; }
        PlatformObstacle po = obstacle.GetComponent<PlatformObstacle>();
        if (po != null) po.SitOnPlatformInEditor();
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
    private static GameObject Sign(Transform parent, Material material, Vector3 postGround, Vector3 triggerCentre, string title, string message)
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

        // Pop-up trigger (same as TriggerController > Create Trigger)
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

    private static void AddPart(Transform parent, PrimitiveType type, Vector3 localPosition, Vector3 localScale, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        Object.DestroyImmediate(part.GetComponent<Collider>());
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localScale = localScale;
        part.GetComponent<Renderer>().sharedMaterial = material;
    }

    private static Material GetSignMaterial()
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

    private static void AddToBuild()
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        if (scenes.Exists(s => s.path == ScenePath)) return;
        scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
