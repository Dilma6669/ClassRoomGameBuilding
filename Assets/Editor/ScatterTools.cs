using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

#if HAS_KINEMATIC_CC
using KinematicCharacterController;
#endif

/// <summary>
/// Quick platforms and scattering for the Game Builder: platforms (with random size and movement)
/// and creatures & pickups (on the ground and/or on platforms).
/// </summary>
public static class ScatterTools
{
    public enum PlatformMove { Still, SideToSide, UpAndDown, Spinning, Growing }

    public const string PlatformGroup = "Platforms";

    // ------------------------------------------------------------------ Quick platforms

    /// <summary>One platform in front of the Scene view camera, a few metres above the ground.</summary>
    /// <summary>How high above the ground new quick platforms float (the Platforms tab's slider; 3 m to start with).</summary>
    public static float NewPlatformHeight
    {
        get => EditorPrefs.GetFloat("GameBuilder.PlatformHeight", 3f);
        set => EditorPrefs.SetFloat("GameBuilder.PlatformHeight", value);
    }

    /// <summary>How big new creatures are (the Creatures tab's slider; 2 to start with, about the player's height for a zombie). Pickups aren't changed.</summary>
    public static float NewCreatureSize
    {
        get => EditorPrefs.GetFloat("GameBuilder.CreatureSize", 2f);
        set => EditorPrefs.SetFloat("GameBuilder.CreatureSize", value);
    }

    public static PlatformLogic QuickPlatform(PlatformController controller, PlatformMove move, PlatformLogic.PlatformType type = PlatformLogic.PlatformType.Normal)
    {
        Vector3 ground = LookPoint();
        float width = move == PlatformMove.Spinning ? 6f : 4f;
        float depth = move == PlatformMove.Spinning ? 1.5f : 4f; // a spinning bar is more fun than a spinning disc
        float lift = NewPlatformHeight + (move == PlatformMove.UpAndDown ? 1.5f : 0f); // an elevator starts a bit higher (its lowest point is just above the ground)

        // Clicked twice without moving the view? Step sideways until there's room
        Vector3 right = SceneView.lastActiveSceneView != null ? SceneView.lastActiveSceneView.camera.transform.right : Vector3.right;
        right.y = 0f;
        right = right.sqrMagnitude < 0.01f ? Vector3.right : right.normalized;
        float myReach = Mathf.Max(width, depth) * 0.5f + (move == PlatformMove.SideToSide ? 3f : 0f);
        PlatformLogic[] existing = Object.FindObjectsByType<PlatformLogic>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Vector3 spot = ground;
        for (int i = 1; i < 20 && !PlaceAt.HasValue; i++) // a clicked spot is used as it is
        {
            Vector3 here = spot;
            if (!existing.Any(e => e.TargetChild != null && Flat(e.TargetChild.position - here) < Reach(e) + myReach + 1.5f)) break;
            spot = ground + right * (7f * ((i + 1) / 2) * (i % 2 == 1 ? 1f : -1f));
            spot.y = GroundHeight(spot);
        }
        ground = spot;

        Undo.SetCurrentGroupName("Quick Platform");
        PlatformLogic p = CreatePlatformAt(controller, ground + Vector3.up * lift, width, depth, 0.8f, 0f, null);
        if (p == null) return null;
        SetMovement(p, move, move == PlatformMove.UpAndDown ? 8f : 6f, 2f);
        SetPlatformType(p, type);
        p.name = PlatformName(p);
        Selection.activeGameObject = p.gameObject;
        return p;
    }

    public static void SetPlatformType(PlatformLogic p, PlatformLogic.PlatformType type)
    {
        Undo.RecordObject(p, "Platform Type");
        bool changed = p.platformType != type;
        p.platformType = type;

        // Good starting effects for each type (students can change them)
        if (changed && type == PlatformLogic.PlatformType.Bouncy)
        {
            if (p.landEffect == Sparkles.Effect.None) p.landEffect = Sparkles.Effect.Ring;
            p.effectColour = PlatformLogic.BouncyColour;
        }
        else if (changed && type == PlatformLogic.PlatformType.Crumbling)
        {
            p.effectColour = PlatformLogic.CrumblingColour;
            p.crumbleAfter = Mathf.Max(p.crumbleAfter, 2f); // flash... flash... break!
        }
        EditorUtility.SetDirty(p);
    }

    /// <summary>"Moving Platform", "Bouncy Platform", "Crumbling Elevator Platform"...</summary>
    public static string PlatformName(PlatformLogic p)
    {
        PlatformMove move = GetMovement(p);
        string type = p.platformType == PlatformLogic.PlatformType.Normal ? "" : p.platformType + " ";
        string moving = move == PlatformMove.Still && type != "" ? "" : MoveName(move) + " ";
        return type + moving + "Platform";
    }

    /// <summary>A row of small platforms going away from the camera, each a bit higher.</summary>
    public static void SteppingStones(PlatformController controller, int count = 6)
    {
        Vector3 start = LookPoint();
        Vector3 forward = SceneView.lastActiveSceneView != null ? SceneView.lastActiveSceneView.camera.transform.forward : Vector3.forward;
        forward.y = 0f;
        forward = forward.sqrMagnitude < 0.01f ? Vector3.forward : forward.normalized;

        Undo.SetCurrentGroupName("Stepping Stones");
        int undoGroup = Undo.GetCurrentGroup();
        GameObject last = null;
        for (int i = 0; i < count; i++)
        {
            Vector3 xz = start + forward * (i * 3.5f);
            float y = Mathf.Max(GroundHeight(xz) + 1f, start.y + 1f + i * 0.7f);
            PlatformLogic p = CreatePlatformAt(controller, new Vector3(xz.x, y, xz.z), 2f, 2f, 0.6f, Random.Range(0f, 360f), null);
            if (p == null) break;
            p.name = "Stepping Stone " + (i + 1);
            SetMovement(p, PlatformMove.Still, 0f, 2f);
            last = p.gameObject;
        }
        Undo.CollapseUndoOperations(undoGroup);
        if (last != null) Selection.activeGameObject = last;
    }

    /// <summary>Makes a platform with the PlatformController, then sizes and places it. model null = the normal look.</summary>
    public static PlatformLogic CreatePlatformAt(PlatformController controller, Vector3 center, float width, float depth, float thickness, float turnY, GameObject model)
    {
        if (controller.platformLogicPrefab == null || controller.movingPlatformPrefab == null) controller.SetupPlatforms();

        HashSet<PlatformLogic> before = new HashSet<PlatformLogic>(Object.FindObjectsByType<PlatformLogic>(FindObjectsInactive.Include, FindObjectsSortMode.None));
        GameObject savedModel = controller.customModel;
        if (model != null) controller.customModel = model;
        controller.CreatePlatform();
        controller.customModel = savedModel;

        PlatformLogic p = Object.FindObjectsByType<PlatformLogic>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(x => !before.Contains(x));
        if (p == null) return null;

        Vector3 local = p.transform.InverseTransformPoint(center);
        p.widthOffset = width;
        p.depthOffset = depth;
        p.heightOffset = thickness;
        p.rotationY = Mathf.Repeat(turnY, 360f);
        p.offsetX = local.x;
        p.offsetY = local.y;
        p.offsetZ = local.z;

        // Keep "Uniform Scale" working from this size
        SerializedObject so = new SerializedObject(p);
        so.FindProperty("baseScale").vector3Value = new Vector3(width, thickness, depth);
        so.ApplyModifiedPropertiesWithoutUndo();

        // Put it there now (PlatformLogic would only do it on its next update)
        Transform moving = p.TargetChild;
        if (moving != null)
        {
            moving.localPosition = local;
            moving.localRotation = Quaternion.Euler(0f, p.rotationY, 0f);
            moving.localScale = new Vector3(width, thickness, depth);
        }
        PlatformModel pm = p.GetComponent<PlatformModel>();
        if (pm != null) pm.Refresh();

        EditorUtility.SetDirty(p);
        return p;
    }

    /// <summary>Sets how a platform moves. distance is how far it travels (for side-to-side and up-and-down).</summary>
    public static void SetMovement(PlatformLogic p, PlatformMove move, float distance, float speed)
    {
        Undo.RecordObject(p, "Platform Movement");
        p.moveX = move == PlatformMove.SideToSide;
        p.moveY = move == PlatformMove.UpAndDown;
        p.moveZ = false;
        p.enableRotation = move == PlatformMove.Spinning;
        p.enableScaling = move == PlatformMove.Growing;

        p.moveDistance = (move == PlatformMove.SideToSide || move == PlatformMove.UpAndDown) ? distance : 0f;
        p.moveSpeed = Mathf.Max(0.1f, speed);
        p.rotationSpeedY = 25f * speed * (Random.value < 0.5f ? -1f : 1f);
        p.scaleSpeed = Mathf.Max(0.3f, speed * 0.6f);
        p.minScale = 0.5f;
        p.maxScale = 1.3f;
        EditorUtility.SetDirty(p);
    }

    public static PlatformMove GetMovement(PlatformLogic p)
    {
        if (p.enableRotation) return PlatformMove.Spinning;
        if (p.enableScaling) return PlatformMove.Growing;
        if (p.moveDistance > 0f && p.moveY) return PlatformMove.UpAndDown;
        if (p.moveDistance > 0f && (p.moveX || p.moveZ)) return PlatformMove.SideToSide;
        return PlatformMove.Still;
    }

    public static string MoveName(PlatformMove move)
    {
        switch (move)
        {
            case PlatformMove.SideToSide: return "Moving";
            case PlatformMove.UpAndDown: return "Elevator";
            case PlatformMove.Spinning: return "Spinning";
            case PlatformMove.Growing: return "Growing";
        }
        return "Floating";
    }

    // ------------------------------------------------------------------ Scatter platforms

    public static int ScatterPlatforms(PlatformController controller, PlatformScatter s)
    {
        Terrain terrain = FindTerrain();
        if (terrain == null) return 0;

        List<PlatformMove> moves = new List<PlatformMove>();
        if (s.still) moves.Add(PlatformMove.Still);
        if (s.sideToSide) moves.Add(PlatformMove.SideToSide);
        if (s.upAndDown) moves.Add(PlatformMove.UpAndDown);
        if (s.spinning) moves.Add(PlatformMove.Spinning);
        if (s.growing) moves.Add(PlatformMove.Growing);
        if (moves.Count == 0) moves.Add(PlatformMove.Still);
        List<GameObject> models = s.models.Where(m => m != null).ToList();

        // Room already taken by platforms in the level
        List<(Vector3 pos, float radius)> taken = new List<(Vector3, float)>();
        foreach (PlatformLogic existing in Object.FindObjectsByType<PlatformLogic>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (existing.TargetChild != null) taken.Add((existing.TargetChild.position, Reach(existing)));
        }

        Undo.SetCurrentGroupName("Scatter Platforms");
        int undoGroup = Undo.GetCurrentGroup();
        Physics.SyncTransforms();
        Vector3 center = LookPoint();
        int placed = 0;

        for (int attempt = 0; attempt < s.howMany * 40 && placed < s.howMany; attempt++)
        {
            if (!TryPickGround(terrain, s.place, center, false, out Vector3 ground)) continue;

            float width = RandomIn(s.sizeRange);
            float depth = RandomIn(s.sizeRange);
            float thickness = RandomIn(s.thicknessRange);
            PlatformMove move = moves[Random.Range(0, moves.Count)];
            float distance = RandomIn(s.moveDistanceRange);
            Vector3 pos = ground + Vector3.up * (RandomIn(s.heightRange) + thickness * 0.5f);
            if (move == PlatformMove.UpAndDown) pos.y += distance * 0.5f; // its lowest point is still at the chosen height

            float reach = Mathf.Max(width, depth) * 0.5f * (move == PlatformMove.Growing ? 1.3f : 1f)
                          + (move == PlatformMove.SideToSide ? distance * 0.5f : 0f);
            if (taken.Any(t => Flat(t.pos - pos) < t.radius + reach + s.place.spacing && Mathf.Abs(t.pos.y - pos.y) < 6f + distance)) continue;

            // Don't put it inside trees, rocks or the ground
            Vector3 half = new Vector3(width, thickness, depth) * 0.5f + Vector3.one * 0.3f;
            if (Physics.CheckBox(pos, half, Quaternion.identity, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) continue;

            GameObject model = models.Count > 0 ? models[Random.Range(0, models.Count)] : null;
            PlatformLogic p = CreatePlatformAt(controller, pos, width, depth, thickness, s.randomTurn ? Random.Range(0f, 360f) : 0f, model);
            if (p == null) break;
            SetMovement(p, move, distance, RandomIn(s.speedRange));
            float roll = Random.Range(0f, 100f);
            if (roll < s.crumblingPercent) SetPlatformType(p, PlatformLogic.PlatformType.Crumbling);
            else if (roll < s.crumblingPercent + s.bouncyPercent) SetPlatformType(p, PlatformLogic.PlatformType.Bouncy);
            p.name = PlatformName(p);
            Tag(p.gameObject, PlatformGroup);
            taken.Add((pos, reach));
            placed++;
        }

        Undo.CollapseUndoOperations(undoGroup);
        Selection.activeGameObject = null;
        return placed;
    }

    /// <summary>How far a platform reaches out from its middle (including how far it moves).</summary>
    private static float Reach(PlatformLogic p)
    {
        float r = Mathf.Max(p.widthOffset, p.depthOffset) * 0.5f;
        if (p.moveDistance > 0f && (p.moveX || p.moveZ)) r += p.moveDistance * 0.5f;
        return r;
    }

    // ------------------------------------------------------------------ Scatter creatures & pickups

    public static int ScatterAllCreatures(CreatureScatter s)
    {
        int total = 0;
        foreach (CreatureScatter.Group g in s.groups) if (g.include) total += ScatterCreatures(s, g);
        return total;
    }

    public static int ScatterCreatures(CreatureScatter s, CreatureScatter.Group g)
    {
        Terrain terrain = FindTerrain();
        TerrainPopulator groundSpawner = Object.FindAnyObjectByType<TerrainPopulator>(FindObjectsInactive.Include);
        Vector3 start = PlayerStart();

        List<PlatformLogic> platforms = Object.FindObjectsByType<PlatformLogic>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(p => p.GetComponent<PlatformPopulator>() != null && p.TargetChild != null)
            .Where(p => GetMovement(p) == PlatformMove.Still ? g.onStillPlatforms : g.onMovingPlatforms)
            .Where(p => s.place.area == ScatterArea.WholeLevel || Flat(p.TargetChild.position - LookPoint()) < s.place.areaSize * 0.5f)
            .Where(p => Flat(p.TargetChild.position - start) >= s.place.keepAwayFromStart)
            .ToList();
        bool useGround = g.onGround && terrain != null && groundSpawner != null;
        if (!useGround && platforms.Count == 0) return 0;

        List<Vector3> taken = Object.FindObjectsByType<ObstacleBase>(FindObjectsInactive.Include, FindObjectsSortMode.None).Select(o => o.transform.position).ToList();
        List<GameObject> models = g.models.Where(m => m != null).ToList();

        Undo.SetCurrentGroupName("Scatter " + g.name);
        int undoGroup = Undo.GetCurrentGroup();
        Physics.SyncTransforms();
        Vector3 center = LookPoint();
        int placed = 0;

        for (int attempt = 0; attempt < g.howMany * 30 && placed < g.howMany; attempt++)
        {
            bool onPlatform = platforms.Count > 0 && (!useGround || Random.value < 0.5f);
            GameObject model = models.Count > 0 ? models[Random.Range(0, models.Count)] : null;
            ObstacleBase made;

            if (onPlatform)
            {
                PlatformLogic platform = platforms[Random.Range(0, platforms.Count)];
                made = SpawnOnPlatform(platform.GetComponent<PlatformPopulator>(), g, model);
            }
            else
            {
                if (!TryPickGround(terrain, s.place, center, true, out Vector3 ground)) continue;
                if (taken.Any(t => Flat(t - ground) < s.place.spacing)) continue;
                made = SpawnOnGround(groundSpawner, terrain, ground, g, model);
            }
            if (made == null) continue;

            // Too close to another one (platform spots are picked by the platform itself)
            if (onPlatform && taken.Any(t => (t - made.transform.position).magnitude < Mathf.Min(s.place.spacing, 1.5f)))
            {
                Undo.DestroyObjectImmediate(made.gameObject);
                continue;
            }

            made.name = KindName(g.kind);
            if (IsCreature(g.kind)) made.jumpsToDefeat = g.jumpsToDefeat;
            if (g.kind == CreatureScatter.Kind.BounceBall) made.upwardBias = 0.25f; // fly off at different angles
            if (g.glow) AddGlow(made.gameObject, Sparkles.ColourFor(made.payloadType));
            Tag(made.gameObject, CreatureGroupId(g));
            taken.Add(made.transform.position);
            placed++;
        }

        Undo.CollapseUndoOperations(undoGroup);
        Selection.activeGameObject = null;
        return placed;
    }

    private static ObstacleBase SpawnOnGround(TerrainPopulator spawner, Terrain terrain, Vector3 point, CreatureScatter.Group g, GameObject model)
    {
        HashSet<ObstacleBase> before = new HashSet<ObstacleBase>(Object.FindObjectsByType<ObstacleBase>(FindObjectsInactive.Include, FindObjectsSortMode.None));

        // Borrow the spawner's settings for this one, then put them back
        string saved = JsonUtility.ToJson(spawner);
        spawner.scatterObstacleType = Movement(g.kind);
        ApplyKind(spawner.baseSettings, g);
        spawner.wanderSettings.wanderRadius = RandomIn(g.roamRange);
        spawner.wanderSettings.minMoveSpeed = Mathf.Min(g.speedRange.x, g.speedRange.y);
        spawner.wanderSettings.maxMoveSpeed = Mathf.Max(g.speedRange.x, g.speedRange.y);
        spawner.patrolSettings.moveDistance = RandomIn(g.roamRange);
        spawner.patrolSettings.minMoveSpeed = spawner.wanderSettings.minMoveSpeed;
        spawner.patrolSettings.maxMoveSpeed = spawner.wanderSettings.maxMoveSpeed;
        spawner.customModel = model;
        spawner.CreateSingleObstacle();
        JsonUtility.FromJsonOverwrite(saved, spawner);

        ObstacleBase made = Object.FindObjectsByType<ObstacleBase>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(o => !before.Contains(o));
        if (made == null) return null;

        // It was made in the middle of the map: move it, keeping its height above the ground
        Transform t = made.transform;
        Vector3 tPos = terrain.transform.position;
        float lift = t.position.y - (terrain.SampleHeight(t.position) + tPos.y);
        t.position = new Vector3(point.x, terrain.SampleHeight(point) + tPos.y + lift, point.z);
        t.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        made.initialYRotation = t.eulerAngles.y;
        return made;
    }

    private static ObstacleBase SpawnOnPlatform(PlatformPopulator spawner, CreatureScatter.Group g, GameObject model)
    {
        HashSet<ObstacleBase> before = new HashSet<ObstacleBase>(Object.FindObjectsByType<ObstacleBase>(FindObjectsInactive.Include, FindObjectsSortMode.None));

        string saved = JsonUtility.ToJson(spawner);
        spawner.scatterObstacleType = Movement(g.kind);
        ApplyKind(spawner.baseSettings, g);
        spawner.wanderSettings.wanderRadius = RandomIn(g.roamRange);
        spawner.wanderSettings.minMoveSpeed = Mathf.Min(g.speedRange.x, g.speedRange.y);
        spawner.wanderSettings.maxMoveSpeed = Mathf.Max(g.speedRange.x, g.speedRange.y);
        spawner.patrolSettings.moveDistance = RandomIn(g.roamRange);
        spawner.patrolSettings.minMoveSpeed = spawner.wanderSettings.minMoveSpeed;
        spawner.patrolSettings.maxMoveSpeed = spawner.wanderSettings.maxMoveSpeed;
        spawner.customModel = model;
        spawner.CreateSingleObstacle(); // made in the middle of the platform
        JsonUtility.FromJsonOverwrite(saved, spawner);

        ObstacleBase made = Object.FindObjectsByType<ObstacleBase>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(o => !before.Contains(o));
        FollowPlatform follow = made != null ? made.GetComponent<FollowPlatform>() : null;
        PlatformLogic logic = spawner.GetComponent<PlatformLogic>();
        if (follow == null || follow.targetPlatform == null || logic == null) return made;

        // Move it to a random spot that's really on the platform. Platforms are round (or a model),
        // and growing ones shrink to half size, so stay well inside the edge.
        Transform platform = follow.targetPlatform;
        float inside = GetMovement(logic) == PlatformMove.Growing ? 0.2f : 0.38f;
        for (int attempt = 0; attempt < 12; attempt++)
        {
            Vector2 r = attempt == 11 ? Vector2.zero : Random.insideUnitCircle;
            Vector3 local = new Vector3(r.x * logic.widthOffset * inside, follow.offsetY, r.y * logic.depthOffset * inside);
            Vector3 world = platform.position + platform.rotation * local;
            bool onIt = Physics.RaycastAll(world + Vector3.up * 30f, Vector3.down, 60f, Physics.AllLayers, QueryTriggerInteraction.Ignore)
                .Any(h => h.transform.IsChildOf(platform));
            if (!onIt) continue;

            follow.offsetX = local.x;
            follow.offsetZ = local.z;
            made.transform.position = world;
            made.initialYRotation = Random.Range(0f, 360f);
            made.transform.rotation = platform.rotation * Quaternion.Euler(0f, made.initialYRotation, 0f);
            if (made is PlatformObstacle po) po.SitOnPlatformInEditor();
            return made;
        }

        Undo.DestroyObjectImmediate(made.gameObject); // couldn't find a safe spot on this platform
        return null;
    }

    /// <summary>What each kind does (the same as the one-click buttons), with a random size.</summary>
    private static void ApplyKind(TerrainPopulator.BaseObstacleSettings b, CreatureScatter.Group g)
    {
        Kind(g.kind, out ObstacleBase.PayloadType payload, out int amount, out float duration, out bool usedUp, out bool bouncy);
        b.payloadType = payload; b.payloadAmount = amount; b.buffDuration = duration; b.destroyOnTrigger = usedUp; b.isBouncy = bouncy;
        b.objectScale = RandomIn(g.sizeRange) * (bouncy ? 1.4f : 1f) * (IsCreature(g.kind) ? NewCreatureSize : 1f); // 1 = the normal creature size
        if (bouncy) b.launchForce = 30f;
    }

    private static void ApplyKind(PlatformPopulator.BaseObstacleSettings b, CreatureScatter.Group g)
    {
        Kind(g.kind, out ObstacleBase.PayloadType payload, out int amount, out float duration, out bool usedUp, out bool bouncy);
        b.payloadType = payload; b.payloadAmount = amount; b.buffDuration = duration; b.destroyOnTrigger = usedUp; b.isBouncy = bouncy;
        b.objectScale = RandomIn(g.sizeRange) * (bouncy ? 1.4f : 1f) * (IsCreature(g.kind) ? NewCreatureSize : 1f); // 1 = the normal creature size
        if (bouncy) b.launchForce = 30f;
    }

    private static void Kind(CreatureScatter.Kind kind, out ObstacleBase.PayloadType payload, out int amount, out float duration, out bool usedUp, out bool bouncy)
    {
        amount = 10; duration = 5f; usedUp = true; bouncy = false;
        switch (kind)
        {
            case CreatureScatter.Kind.Health: payload = ObstacleBase.PayloadType.HealthBooster; amount = 25; break;
            case CreatureScatter.Kind.Stamina: payload = ObstacleBase.PayloadType.StaminaBooster; amount = 50; break;
            case CreatureScatter.Kind.Shield: payload = ObstacleBase.PayloadType.InvincibilityBuff; amount = 1; duration = 8f; break;
            case CreatureScatter.Kind.SuperJump: payload = ObstacleBase.PayloadType.JumpBuff; amount = 2; duration = 8f; break;
            case CreatureScatter.Kind.SpeedBoost: payload = ObstacleBase.PayloadType.SprintBuff; amount = 2; duration = 8f; break;
            case CreatureScatter.Kind.BounceBall: payload = ObstacleBase.PayloadType.None; usedUp = false; bouncy = true; break;
            default: payload = ObstacleBase.PayloadType.Damage; usedUp = false; break; // creatures
        }
    }

    private static ObstacleSpawnType Movement(CreatureScatter.Kind kind)
    {
        if (kind == CreatureScatter.Kind.Wanderer) return ObstacleSpawnType.Wander;
        if (kind == CreatureScatter.Kind.Patroller) return ObstacleSpawnType.Patrol;
        return ObstacleSpawnType.Static;
    }

    public static bool IsCreature(CreatureScatter.Kind kind) => kind <= CreatureScatter.Kind.Guard;
    public static bool Moves(CreatureScatter.Kind kind) => kind <= CreatureScatter.Kind.Patroller;

    public static string KindName(CreatureScatter.Kind kind)
    {
        switch (kind)
        {
            case CreatureScatter.Kind.Wanderer: return "Wanderer";
            case CreatureScatter.Kind.Patroller: return "Patroller";
            case CreatureScatter.Kind.Guard: return "Guard";
            case CreatureScatter.Kind.Health: return "Health Pickup";
            case CreatureScatter.Kind.Stamina: return "Stamina Pickup";
            case CreatureScatter.Kind.Shield: return "Shield Pickup";
            case CreatureScatter.Kind.SuperJump: return "Super Jump Pickup";
            case CreatureScatter.Kind.SpeedBoost: return "Speed Boost Pickup";
        }
        return "Bounce Ball";
    }

    public static string KindLabel(CreatureScatter.Kind kind)
    {
        switch (kind)
        {
            case CreatureScatter.Kind.Wanderer: return "🕷 Wanderer (walks around)";
            case CreatureScatter.Kind.Patroller: return "🧟 Patroller (back & forth)";
            case CreatureScatter.Kind.Guard: return "🐍 Guard (stays still)";
            case CreatureScatter.Kind.Health: return "❤️ Health";
            case CreatureScatter.Kind.Stamina: return "⚡ Stamina";
            case CreatureScatter.Kind.Shield: return "🛡 Shield";
            case CreatureScatter.Kind.SuperJump: return "🦘 Super Jump";
            case CreatureScatter.Kind.SpeedBoost: return "💨 Speed Boost";
        }
        return "🏀 Bounce Ball";
    }

    public static string CreatureGroupId(CreatureScatter.Group g) => "Creatures/" + g.name;

    /// <summary>Makes something glow (or returns its existing glow).</summary>
    public static Glow AddGlow(GameObject go, Color colour)
    {
        Glow glow = go.GetComponent<Glow>();
        if (glow != null) return glow;
        glow = Undo.AddComponent<Glow>(go);
        glow.colour = colour;
        return glow;
    }

    // ------------------------------------------------------------------ Clearing

    /// <summary>Removes scattered things. group null = every scattered creature & pickup; "Platforms" = scattered platforms.</summary>
    public static int Clear(string group)
    {
        List<GameObject> doomed = Object.FindObjectsByType<ScatterTag>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(t => group == null ? t.group.StartsWith("Creatures/") : t.group == group)
            .Select(t => t.gameObject).ToList();
        Undo.SetCurrentGroupName("Clear Scattered");
        int undoGroup = Undo.GetCurrentGroup();
        foreach (GameObject go in doomed) if (go != null) Undo.DestroyObjectImmediate(go);
        Undo.CollapseUndoOperations(undoGroup);
        return doomed.Count;
    }

    public static int Count(string group) => Object.FindObjectsByType<ScatterTag>(FindObjectsInactive.Include, FindObjectsSortMode.None).Count(t => t.group == group);

    private static void Tag(GameObject go, string group)
    {
        ScatterTag tag = go.AddComponent<ScatterTag>();
        tag.group = group;
        tag.hideFlags = HideFlags.HideInInspector;
    }

    // ------------------------------------------------------------------ Finding spots

    /// <summary>A random ground spot that passes the "where" rules. openGround = nothing (trees, platforms...) above it.</summary>
    public static bool TryPickGround(Terrain terrain, ScatterPlace place, Vector3 center, bool openGround, out Vector3 point)
    {
        point = Vector3.zero;
        TerrainData data = terrain.terrainData;
        Vector3 origin = terrain.GetPosition();
        Vector3 size = data.size;
        float pad = Mathf.Min(place.edgePadding, size.x * 0.45f);

        float x, z;
        if (place.area == ScatterArea.AroundSceneView)
        {
            Vector2 r = Random.insideUnitCircle * place.areaSize * 0.5f;
            x = center.x + r.x;
            z = center.z + r.y;
        }
        else
        {
            x = Random.Range(origin.x, origin.x + size.x);
            z = Random.Range(origin.z, origin.z + size.z);
        }
        if (x < origin.x + pad || x > origin.x + size.x - pad || z < origin.z + pad || z > origin.z + size.z - pad) return false;

        float nx = (x - origin.x) / size.x, nz = (z - origin.z) / size.z;
        if (data.GetSteepness(nx, nz) > place.maxSlope) return false;
        if (!OnAllowedGround(data, nx, nz, place.onlyOnGround)) return false;

        point = new Vector3(x, terrain.SampleHeight(new Vector3(x, 0f, z)) + origin.y, z);
        if (Flat(point - PlayerStart()) < place.keepAwayFromStart) return false;

        if (openGround)
        {
            // The first thing straight above-down must be the ground itself
            if (!Physics.Raycast(point + Vector3.up * 300f, Vector3.down, out RaycastHit hit, 400f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) return false;
            if (!(hit.collider is TerrainCollider)) return false;
        }
        return true;
    }

    private static bool OnAllowedGround(TerrainData data, float nx, float nz, List<TerrainLayer> allowed)
    {
        if (allowed == null || !allowed.Any(l => l != null)) return true;
        TerrainLayer[] layers = data.terrainLayers;
        if (layers.Length == 0) return true;

        int ax = Mathf.Clamp(Mathf.RoundToInt(nx * (data.alphamapWidth - 1)), 0, data.alphamapWidth - 1);
        int az = Mathf.Clamp(Mathf.RoundToInt(nz * (data.alphamapHeight - 1)), 0, data.alphamapHeight - 1);
        float[,,] mix = data.GetAlphamaps(ax, az, 1, 1);
        int best = 0;
        for (int i = 1; i < mix.GetLength(2); i++) if (mix[0, 0, i] > mix[0, 0, best]) best = i;
        return best < layers.Length && allowed.Contains(layers[best]);
    }

    /// <summary>Where the Scene view camera is looking, on the ground.</summary>
    /// <summary>Set while placing by clicking in the Scene (ScenePlacer): LookPoint() returns this spot instead.</summary>
    public static Vector3? PlaceAt;

    public static Vector3 LookPoint()
    {
        SceneView view = SceneView.lastActiveSceneView;
        if (PlaceAt.HasValue) { Vector3 spot = PlaceAt.Value; spot.y = GroundHeight(spot); return spot; }
        if (view == null) return Vector3.zero;
        Vector3 target = view.pivot;
        Transform cam = view.camera.transform;
        if (Physics.Raycast(cam.position, cam.forward, out RaycastHit hit, 5000f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) target = hit.point;
        target.y = GroundHeight(target);
        return target;
    }

    private static float GroundHeight(Vector3 p)
    {
        Terrain terrain = FindTerrain();
        return terrain != null ? terrain.SampleHeight(p) + terrain.GetPosition().y : p.y;
    }

    public static Terrain FindTerrain()
    {
        Terrain t = Object.FindAnyObjectByType<Terrain>(FindObjectsInactive.Exclude);
        return t != null ? t : Terrain.activeTerrain;
    }

    private static Vector3 PlayerStart()
    {
#if HAS_KINEMATIC_CC
        KinematicCharacterMotor motor = Object.FindAnyObjectByType<KinematicCharacterMotor>(FindObjectsInactive.Exclude);
        if (motor != null) return motor.transform.position;
#endif
        PlayerController pc = Object.FindAnyObjectByType<PlayerController>(FindObjectsInactive.Exclude);
        return pc != null ? pc.transform.position : new Vector3(99999f, 0f, 99999f);
    }

    private static float RandomIn(Vector2 range) => Random.Range(Mathf.Min(range.x, range.y), Mathf.Max(range.x, range.y));
    private static float Flat(Vector3 v) => new Vector2(v.x, v.z).magnitude;
}
