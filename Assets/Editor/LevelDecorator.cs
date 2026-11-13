using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Makes a level feel more alive WITHOUT rebuilding it: grass painted over the ground (cheap picture grass),
/// plus extra trees, flowers, shrubs and rocks (some big). Solid things are kept away from signs, creatures,
/// pickups, platforms, teleporters, checkpoints and the player's start; big ones also stay out of the play area.
/// Used by the tutorial builders, and by Game > Tutorial > Add Grass & Scenery to the open level.
/// Each group is its own scenery group (Scenery tab), so students can see and change them.
/// </summary>
public static class LevelDecorator
{
    public const string MoreTrees = "More Trees", Flowers = "Flowers", Shrubs = "Shrubs & Bushes", MoreRocks = "More Rocks", BigRocks = "Big Rocks";

    [MenuItem("Game/Tutorial/Add Grass and More Scenery to the Open Level", priority = 48)]
    private static void Menu()
    {
        if (!EditorUtility.DisplayDialog("Add grass and scenery",
            "Paint grass over the ground and add more trees, flowers, shrubs and rocks to the open level?\n\n" +
            "Things are kept away from signs, creatures and platforms. Ctrl+Z undoes it (or don't save).", "Add them", "Cancel")) return;
        Debug.Log(Decorate(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), null, 1f));
    }

    /// <summary>
    /// Grass + more scenery in a scene. playArea (x/z): big trees and rocks stay outside it (null = work it out from the signs).
    /// amount: 1 = normal. Runs again safely: it first removes what it added last time.
    /// </summary>
    public static string Decorate(UnityEngine.SceneManagement.Scene scene, Rect? playArea, float amount, int seed = 4242)
    {
        Terrain terrain = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Terrain>(true)).FirstOrDefault();
        if (terrain == null) return "No ground in " + scene.name;
        SceneryPopulator scenery = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<SceneryPopulator>(true)).FirstOrDefault();
        if (scenery == null)
        {
            TerrainPopulator tp = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<TerrainPopulator>(true)).FirstOrDefault();
            if (tp == null) return "No scenery tool in " + scene.name;
            scenery = Undo.AddComponent<SceneryPopulator>(tp.gameObject);
        }

        Random.State old = Random.state;
        Random.InitState(seed);
        try
        {
            int grass = PaintGrass(terrain, seed);
            List<Clear> keepClear = KeepClearSpots(scene);
            Rect area = playArea ?? PlayAreaFromSigns(scene);
            int added = 0;
            added += AddGroup(scenery, terrain, MoreTrees, "Tree, Willow -Stump -Snow -Dead", new Vector2(1.4f, 2.6f), true, false, Mathf.RoundToInt(90 * amount), keepClear, area, 7f, outsideOnly: true);
            added += AddGroup(scenery, terrain, Flowers, "Flowers, Plant, CactusFlower -Snow", new Vector2(1.2f, 2.2f), false, false, Mathf.RoundToInt(260 * amount), keepClear, area, 1.5f, outsideOnly: false);
            added += AddGroup(scenery, terrain, Shrubs, "Bush -Snow", new Vector2(1.2f, 2.4f), false, false, Mathf.RoundToInt(140 * amount), keepClear, area, 2.5f, outsideOnly: false);
            added += AddGroup(scenery, terrain, MoreRocks, "Rock -Snow", new Vector2(0.6f, 2f), true, true, Mathf.RoundToInt(70 * amount), keepClear, area, 3f, outsideOnly: false);
            added += AddGroup(scenery, terrain, BigRocks, "Rock -Snow", new Vector2(4f, 12f), true, true, Mathf.RoundToInt(28 * amount), keepClear, area, 14f, outsideOnly: true);
            EditorUtility.SetDirty(scenery);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            return scene.name + ": grass on " + grass + "% of the ground, " + added + " scenery pieces added.";
        }
        finally { Random.state = old; }
    }

    // ------------------------------------------------------------------ Grass

    /// <summary>Patchy picture grass on all the gentle ground (not on steep mountain sides). Returns the % covered.</summary>
    public static int PaintGrass(Terrain terrain, int seed)
    {
        TerrainData data = terrain.terrainData;
        int index = GameBuilderWindow.AddSimpleGrass(terrain);
        if (index < 0) return 0;
        // A meadow look: grass shows up to 70 m, fairly thick (still the cheap kind)
        terrain.detailObjectDistance = Mathf.Max(terrain.detailObjectDistance, 70f);
        terrain.detailObjectDensity = Mathf.Max(terrain.detailObjectDensity, 0.7f);
        terrain.drawTreesAndFoliage = true;

        Undo.RegisterCompleteObjectUndo(data, "Paint grass");
        int res = data.detailResolution;
        if (res <= 0) return 0;
        int[,] layer = new int[res, res];
        float offX = seed * 0.37f, offZ = seed * 0.71f;
        int covered = 0;
        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                float nx = (x + 0.5f) / res, nz = (y + 0.5f) / res;
                float steep = data.GetSteepness(nx, nz);
                if (steep > 32f) continue;
                // Patches: big soft clumps, with a few bare spots
                float n = Mathf.PerlinNoise(offX + nx * 18f, offZ + nz * 18f) * 0.7f + Mathf.PerlinNoise(offX + nx * 70f, offZ + nz * 70f) * 0.3f;
                if (n < 0.28f) continue;
                float slopeFade = Mathf.InverseLerp(32f, 18f, steep);
                // Coverage mode (newer grounds): 0-255 = how much of the cell is covered. Instance mode: tufts per cell.
                float most = data.detailScatterMode == DetailScatterMode.CoverageMode ? 255f : 9f;
                int v = Mathf.RoundToInt(Mathf.Lerp(most * 0.35f, most, Mathf.InverseLerp(0.28f, 0.75f, n)) * slopeFade);
                if (v <= 0) continue;
                layer[y, x] = v;
                covered++;
            }
        }
        data.SetDetailLayer(0, 0, index, layer);
        EditorUtility.SetDirty(data);
        EditorUtility.SetDirty(terrain);
        return Mathf.RoundToInt(100f * covered / (res * (float)res));
    }

    // ------------------------------------------------------------------ Scenery

    private struct Clear { public Vector2 pos; public float radius; }

    /// <summary>Everything the player plays with: solid scenery must not be put on top of these.</summary>
    private static List<Clear> KeepClearSpots(UnityEngine.SceneManagement.Scene scene)
    {
        var list = new List<Clear>();
        void Add(Vector3 p, float r) => list.Add(new Clear { pos = new Vector2(p.x, p.z), radius = r });
        GameObject[] roots = scene.GetRootGameObjects();
        foreach (ObstacleBase o in roots.SelectMany(g => g.GetComponentsInChildren<ObstacleBase>(true)))
            Add(o.transform.position, 3f + 1.5f * Mathf.Clamp(o.objectScale, 1f, 8f));
        foreach (PlatformLogic p in roots.SelectMany(g => g.GetComponentsInChildren<PlatformLogic>(true)))
        {
            Transform t = p.TargetChild != null ? p.TargetChild : p.transform;
            float r = 0.5f * Mathf.Max(p.widthOffset, p.depthOffset) + 4f + (p.HasActiveAxis ? p.moveDistance * 0.5f : 0f);
            Add(t.position, r);
        }
        foreach (TriggerLogic t in roots.SelectMany(g => g.GetComponentsInChildren<TriggerLogic>(true))) Add(t.transform.position, 6f);
        foreach (Teleporter t in roots.SelectMany(g => g.GetComponentsInChildren<Teleporter>(true))) Add(t.transform.position, 5f);
        foreach (Checkpoint c in roots.SelectMany(g => g.GetComponentsInChildren<Checkpoint>(true))) Add(c.transform.position, 5f);
        foreach (PlayerLogic p in roots.SelectMany(g => g.GetComponentsInChildren<PlayerLogic>(true))) Add(p.transform.position, 10f);
        return list;
    }

    /// <summary>The play area: everything within 12 m of the signs' box.</summary>
    private static Rect PlayAreaFromSigns(UnityEngine.SceneManagement.Scene scene)
    {
        var signs = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<TriggerLogic>(true)).Select(t => t.transform.position).ToList();
        if (signs.Count == 0) return new Rect(0f, 0f, 0f, 0f);
        float minX = signs.Min(p => p.x) - 12f, maxX = signs.Max(p => p.x) + 12f;
        float minZ = signs.Min(p => p.z) - 12f, maxZ = signs.Max(p => p.z) + 12f;
        return Rect.MinMaxRect(minX, minZ, maxX, maxZ);
    }

    private static int AddGroup(SceneryPopulator scenery, Terrain terrain, string name, string words, Vector2 size, bool solid, bool tilt, int howMany,
        List<Clear> keepClear, Rect area, float clearance, bool outsideOnly)
    {
        if (howMany <= 0) return 0;
        SceneryPopulator.SceneryGroup g = scenery.groups.Find(x => x.name == name);
        if (g == null) { g = new SceneryPopulator.SceneryGroup { name = name }; scenery.groups.Add(g); }
        g.quickFillWords = words;
        g.sizeRange = size;
        g.solid = solid;
        g.tiltWithGround = tilt;
        g.howMany = howMany;
        g.models.Clear();
        scenery.QuickFill(g);
        if (g.models.Count == 0) return 0;

        // Start fresh: remove what this group added before (so running it again doesn't pile up)
        Transform root = scenery.transform.Find(SceneryPopulator.ContainerName);
        Transform folder = root != null ? root.Find(name) : null;
        if (folder != null) Undo.DestroyObjectImmediate(folder.gameObject);

        scenery.Scatter(g);
        root = scenery.transform.Find(SceneryPopulator.ContainerName);
        folder = root != null ? root.Find(name) : null;
        if (folder == null) return 0;

        var remove = new List<GameObject>();
        foreach (Transform piece in folder)
        {
            Vector2 p = new Vector2(piece.position.x, piece.position.z);
            float extra = clearance + (solid ? piece.localScale.x * 0.8f : 0f); // bigger pieces keep further away
            bool inArea = area.width > 0f && area.Contains(p);
            // Walk-through flowers and shrubs can go anywhere; solid things keep clear of everything you play with
            if ((outsideOnly && inArea) || (solid && keepClear.Any(k => Vector2.Distance(k.pos, p) < k.radius + extra))) remove.Add(piece.gameObject);
        }
        foreach (GameObject go in remove) Object.DestroyImmediate(go);
        return folder.childCount;
    }
}
