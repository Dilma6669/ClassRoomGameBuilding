using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Scatter scenery (trees, rocks, grass, bushes...) across the terrain, or place it by hand.
/// Scenery is just for looks (plus optional bumping into it) - no gameplay, no movement, so it's cheap.
///
/// Scenery is organised in GROUPS (e.g. Trees, Rocks, Plants), each with its own models, amount and size.
/// Quick way to fill a group: type words like "Tree" and click "Add matching models".
/// </summary>
public class SceneryPopulator : MonoBehaviour
{
    public const string ContainerName = "Scenery (auto)";

    [System.Serializable]
    public class SceneryGroup
    {
        public string name = "Trees";
        [Tooltip("Untick to leave this group out when you click Scatter All.")]
        public bool include = true;
        [Tooltip("Models to pick from at random.")]
        public List<GameObject> models = new List<GameObject>();
        [Tooltip("How many to scatter each time.")]
        [Range(1, 5000)] public int howMany = 30;
        [Tooltip("Random size range (1 = the model's normal size).")]
        public Vector2 sizeRange = new Vector2(0.8f, 1.3f);
        [Tooltip("Tilt with the ground (good for rocks; off for trees so they grow straight up).")]
        public bool tiltWithGround = false;
        [Tooltip("The player bumps into these (on), or walks straight through them (off, good for grass).")]
        public bool solid = true;
        [Tooltip("Landing on these never hurts, however far you fell (giant mushrooms, bouncy trees...).")]
        public bool safeLanding = false;

        [Tooltip("Make these glow (great for crystals, mushrooms or lanterns). Lots of glowing things can slow the game down.")]
        public bool glow = false;
        public Color glowColour = new Color(0.5f, 0.9f, 1f);
        [Tooltip("Sparkles floating around each one (fireflies, magic dust...). Seen when you press Play.")]
        public bool sparkles = false;
        public AmbientSparkles.Style sparkleStyle = AmbientSparkles.Style.Fireflies;
        public Color sparkleColour = new Color(1f, 0.95f, 0.5f);

        [Tooltip("Words to look for in model names, separated by commas. Put - in front to leave things out, e.g. Tree -Snow -Dead")]
        public string quickFillWords = "Tree";
    }

    [Tooltip("Each group has its own models, amount and size.")]
    public List<SceneryGroup> groups = new List<SceneryGroup>();

    [Header("Settings for all groups")]
    [Tooltip("Give each one a random turn so they don't all look the same.")]
    public bool randomTurn = true;
    [ShowAs("Steepest hill to grow on"), Tooltip("Don't put scenery on slopes steeper than this (degrees).")]
    [Range(0f, 90f)] public float maxSlope = 35f;
    [ShowAs("Keep away from the edge"), Tooltip("Keep this far from the edge of the ground (metres).")]
    [Range(0f, 100f)] public float edgePadding = 10f;
    [ShowAs("Push into the ground"), Tooltip("Sink a little into the ground so nothing looks like it's floating on bumpy ground.")]
    [Range(0f, 2f)] public float sinkIntoGround = 0.1f;
    [ShowAs("Click a platform: put it ON the platform"), Tooltip("Place One: clicking a platform stands the tree (or rock) on that platform, and it moves and spins with it.\nOff: it goes on the ground under the platform instead.")]
    public bool placeOnPlatforms = true;

    private void Reset()
    {
        groups = new List<SceneryGroup>
        {
            new SceneryGroup { name = "Trees", howMany = 60, sizeRange = new Vector2(1.5f, 2.5f), solid = true, quickFillWords = "Tree, Willow -Stump -Snow -Dead -Autumn" },
            new SceneryGroup { name = "Rocks", howMany = 40, sizeRange = new Vector2(0.8f, 2.5f), tiltWithGround = true, solid = true, quickFillWords = "Rock -Snow" },
            new SceneryGroup { name = "Plants & Grass", howMany = 200, sizeRange = new Vector2(1f, 1.6f), solid = false, quickFillWords = "Bush, Grass, Flower, Plant -Snow" },
        };
    }

    private Terrain FindTerrain()
    {
        Terrain t = GetComponentInChildren<Terrain>();
        if (t == null) t = GetComponentInParent<Terrain>();
        if (t == null) t = Terrain.activeTerrain;
        return t;
    }

#if UNITY_EDITOR
    private Transform GetContainer(SceneryGroup group)
    {
        Transform root = transform.Find(ContainerName);
        if (root == null)
        {
            root = new GameObject(ContainerName).transform;
            root.SetParent(transform, false);
            Undo.RegisterCreatedObjectUndo(root.gameObject, "Create Scenery");
        }
        Transform container = root.Find(group.name);
        if (container == null)
        {
            container = new GameObject(group.name).transform;
            container.SetParent(root, false);
            Undo.RegisterCreatedObjectUndo(container.gameObject, "Create Scenery");
        }
        return container;
    }

    /// <summary>Adds every model in Assets/MyModels whose name matches the group's quick-fill words.</summary>
    public int QuickFill(SceneryGroup group)
    {
        List<string> include = new List<string>();
        List<string> exclude = new List<string>();
        foreach (string raw in (group.quickFillWords ?? "").Split(',', ' '))
        {
            string word = raw.Trim().ToLowerInvariant();
            if (word.Length == 0) continue;
            if (word.StartsWith("-")) { if (word.Length > 1) exclude.Add(word.Substring(1)); }
            else include.Add(word);
        }
        if (include.Count == 0) return 0;

        int added = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { MyModelsFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string fileName = System.IO.Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
            if (!include.Exists(w => fileName.Contains(w))) continue;
            if (exclude.Exists(w => fileName.Contains(w))) continue;

            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (model != null && !group.models.Contains(model)) { group.models.Add(model); added++; }
        }
        return added;
    }

    private const string MyModelsFolder = "Assets/MyModels";

    public int ScatterAll()
    {
        int total = 0;
        foreach (SceneryGroup g in groups) if (g.include) total += Scatter(g);
        return total;
    }

    public int Scatter(SceneryGroup group)
    {
        Terrain terrain = FindTerrain();
        List<GameObject> models = group.models.FindAll(m => m != null);
        if (terrain == null || models.Count == 0) return 0;

        Vector3 origin = terrain.GetPosition();
        Vector3 size = terrain.terrainData.size;
        int placed = 0;

        for (int attempt = 0; attempt < group.howMany * 10 && placed < group.howMany; attempt++)
        {
            float x = Random.Range(origin.x + edgePadding, origin.x + size.x - edgePadding);
            float z = Random.Range(origin.z + edgePadding, origin.z + size.z - edgePadding);

            // Skip steep slopes
            Vector2 n = new Vector2((x - origin.x) / size.x, (z - origin.z) / size.z);
            if (terrain.terrainData.GetSteepness(n.x, n.y) > maxSlope) continue;

            Vector3 normal = terrain.terrainData.GetInterpolatedNormal(n.x, n.y);
            float y = terrain.SampleHeight(new Vector3(x, 0f, z)) + origin.y;

            if (PlaceAt(group, models[Random.Range(0, models.Count)], new Vector3(x, y, z), normal, "Scatter Scenery") != null) placed++;
        }
        return placed;
    }

    /// <summary>Places one random model from the group on the ground at (or under) a point.</summary>
    public GameObject PlaceOne(SceneryGroup group, Vector3 point)
    {
        List<GameObject> models = group.models.FindAll(m => m != null);
        if (models.Count == 0) return null;

        Vector3 normal = Vector3.up;
        if (Physics.Raycast(point + Vector3.up * 500f, Vector3.down, out RaycastHit hit, 2000f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            point = hit.point;
            normal = hit.normal;
        }
        GameObject go = PlaceAt(group, models[Random.Range(0, models.Count)], point, normal, "Place Scenery");
        if (go != null) Selection.activeGameObject = go;
        return go;
    }

    /// <summary>
    /// Places one random model from the group exactly at a clicked spot on the ground.
    /// Clicked on something else (a tree, a rock)? It goes on the ground underneath instead.
    /// </summary>
    public GameObject PlaceOneAt(SceneryGroup group, RaycastHit clicked)
    {
        List<GameObject> models = group.models.FindAll(m => m != null);
        if (models.Count == 0) return null;

        Vector3 point = clicked.point;
        Vector3 normal = clicked.normal;

        // Clicked a platform? Stand it on the platform, so it rides along
        PlatformLogic platform = clicked.collider != null ? clicked.collider.GetComponentInParent<PlatformLogic>() : null;
        if (placeOnPlatforms && platform != null && platform.TargetChild != null)
            return PlaceOnPlatform(group, models[Random.Range(0, models.Count)], platform, point);

        if (!(clicked.collider is TerrainCollider))
        {
            Terrain terrain = FindTerrain();
            if (terrain != null)
            {
                point.y = terrain.SampleHeight(point) + terrain.GetPosition().y;
                Vector3 local = point - terrain.GetPosition();
                normal = terrain.terrainData.GetInterpolatedNormal(local.x / terrain.terrainData.size.x, local.z / terrain.terrainData.size.z);
            }
        }
        return PlaceAt(group, models[Random.Range(0, models.Count)], point, normal, "Place Scenery");
    }

    /// <summary>
    /// Stands one model (null = a random one from the group) on a platform at 'point' (or on the platform's surface under it).
    /// It's put in the platform's scenery holder, so it moves, spins and floats with the platform.
    /// </summary>
    public GameObject PlaceOnPlatform(SceneryGroup group, GameObject model, PlatformLogic platform, Vector3 point, float size = 0f)
    {
        if (model == null)
        {
            List<GameObject> models = group.models.FindAll(m => m != null);
            if (models.Count == 0) return null;
            model = models[Random.Range(0, models.Count)];
        }
        Transform holder = PlatformSceneryHolder.For(platform);
        if (holder == null) return null;

        // The platform's surface at this spot (the top of a rock model, or the cylinder's top)
        Physics.SyncTransforms();
        Vector3 surface = point;
        float best = float.NegativeInfinity;
        foreach (RaycastHit hit in Physics.RaycastAll(point + Vector3.up * 300f, Vector3.down, 600f, Physics.AllLayers, QueryTriggerInteraction.Ignore))
        {
            if (!hit.transform.IsChildOf(platform.TargetChild) || hit.transform.IsChildOf(holder)) continue;
            if (hit.point.y > best) { best = hit.point.y; surface = hit.point; }
        }

        GameObject go = PlaceAt(group, model, surface, Vector3.up, "Place Scenery on Platform", holder, size);
        return go;
    }

    private GameObject PlaceAt(SceneryGroup group, GameObject model, Vector3 groundPoint, Vector3 groundNormal, string undoName, Transform onPlatform = null, float size = 0f)
    {
        Transform container = onPlatform != null ? onPlatform : GetContainer(group);

        // The model sits inside a holder that we size and turn, so the model keeps its own built-in
        // rotation and scale (FBX files from Blender have a 90 degree turn and x100 scale on them)
        GameObject go = new GameObject(model.name);
        go.transform.SetParent(container, false);
        GameObject instance = PrefabUtility.IsPartOfPrefabAsset(model)
            ? (GameObject)PrefabUtility.InstantiatePrefab(model, go.transform)
            : Instantiate(model, go.transform);
        instance.name = model.name;
        SceneryPiece tag = go.AddComponent<SceneryPiece>(); // shows it in the Game Builder's Scenery List
        tag.group = group.name;
        tag.hideFlags = HideFlags.HideInInspector;

        Quaternion turn = randomTurn ? Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) : Quaternion.identity;
        Quaternion tilt = group.tiltWithGround ? Quaternion.FromToRotation(Vector3.up, groundNormal) : Quaternion.identity;
        go.transform.rotation = tilt * turn;
        go.transform.localScale = Vector3.one * (size > 0f ? size : Random.Range(Mathf.Min(group.sizeRange.x, group.sizeRange.y), Mathf.Max(group.sizeRange.x, group.sizeRange.y)));

        // Stand the bottom of the model on the ground
        go.transform.position = groundPoint;
        float bottom = LowestPoint(go);
        go.transform.position += Vector3.up * (groundPoint.y - bottom - sinkIntoGround);

        // Never animate or simulate scenery
        foreach (Animator a in go.GetComponentsInChildren<Animator>(true)) a.enabled = false;
        foreach (Rigidbody rb in go.GetComponentsInChildren<Rigidbody>(true)) Object.DestroyImmediate(rb);
        foreach (Collider c in go.GetComponentsInChildren<Collider>(true)) c.enabled = group.solid;

        if (group.solid)
        {
            // Bumpable: add colliders from the model's own shape (where it doesn't have any)
            foreach (MeshFilter mf in go.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh != null && mf.GetComponent<Collider>() == null) mf.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
            }
            foreach (SkinnedMeshRenderer smr in go.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (smr.sharedMesh != null && smr.GetComponent<Collider>() == null) smr.gameObject.AddComponent<MeshCollider>().sharedMesh = smr.sharedMesh;
            }
        }

        // Static = merged and never moves (scenery on a platform has to move with it)
        if (onPlatform == null) GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
        if (group.safeLanding) go.AddComponent<SafeLanding>();
        if (group.glow) AddGlow(go, group);
        if (group.sparkles) AddSparkles(go, group);
        Undo.RegisterCreatedObjectUndo(go, undoName);
        return go;
    }

    private static void AddGlow(GameObject go, SceneryGroup group)
    {
        Glow glow = go.GetComponent<Glow>();
        if (glow == null) glow = go.AddComponent<Glow>();
        glow.colour = group.glowColour;
        glow.lightRange = 3f;
        // Glowing scenery can't be merged by static batching (its materials glow separately)
        GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
    }

    private static void AddSparkles(GameObject go, SceneryGroup group)
    {
        AmbientSparkles s = go.GetComponent<AmbientSparkles>();
        if (s == null) s = go.AddComponent<AmbientSparkles>();
        s.style = group.sparkleStyle;
        s.colour = group.sparkleColour;
        s.amount = 4f;
    }

    /// <summary>Turns glow and sparkles on or off for everything already scattered in a group. Returns how many pieces.</summary>
    public int ApplyGlowToPlaced(SceneryGroup group)
    {
        Transform root = transform.Find(ContainerName);
        Transform container = root != null ? root.Find(group.name) : null;
        if (container == null) return 0;

        int count = 0;
        foreach (Transform piece in container)
        {
            AmbientSparkles sparkle = piece.GetComponent<AmbientSparkles>();
            if (group.sparkles) { Undo.RegisterFullObjectHierarchyUndo(piece.gameObject, "Scenery Sparkles"); AddSparkles(piece.gameObject, group); }
            else if (sparkle != null) Undo.DestroyObjectImmediate(sparkle);

            Glow existing = piece.GetComponent<Glow>();
            if (group.glow)
            {
                Undo.RegisterFullObjectHierarchyUndo(piece.gameObject, "Scenery Glow");
                if (existing == null) { AddGlow(piece.gameObject, group); count++; }
                else { existing.colour = group.glowColour; count++; }
            }
            else if (existing != null)
            {
                Undo.DestroyObjectImmediate(existing);
                GameObjectUtility.SetStaticEditorFlags(piece.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
                count++;
            }
        }
        return count;
    }

    private static float LowestPoint(GameObject go)
    {
        float lowest = float.PositiveInfinity;
        foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
        {
            Mesh mesh = r is SkinnedMeshRenderer smr ? smr.sharedMesh : r.GetComponent<MeshFilter>()?.sharedMesh;
            if (mesh == null) continue;
            Bounds b = mesh.bounds;
            Matrix4x4 m = r.transform.localToWorldMatrix;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                lowest = Mathf.Min(lowest, m.MultiplyPoint3x4(corner).y);
            }
        }
        return float.IsInfinity(lowest) ? go.transform.position.y : lowest;
    }

    /// <summary>Removes one group's scattered scenery (or all of it when group is null).</summary>
    public int Clear(SceneryGroup group = null)
    {
        Transform root = transform.Find(ContainerName);
        if (root == null) return 0;
        Transform target = group == null ? root : root.Find(group.name);
        if (target == null) return 0;

        int count = 0;
        if (group == null) { foreach (Transform groupContainer in root) count += groupContainer.childCount; }
        else count = target.childCount;
        Undo.DestroyObjectImmediate(target.gameObject);
        return count;
    }
#endif
}
