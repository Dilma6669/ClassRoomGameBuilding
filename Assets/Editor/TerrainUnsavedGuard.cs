using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Makes "Don't Save" really throw away changes to the ground (sculpting, painting, trees, grass).
///
/// Why this is needed: the ground's shape isn't stored in the level file. It lives in its own TerrainData file
/// (e.g. Assets/Tutorial/Tutorial2Terrain.asset). Unity's "Don't Save" only throws away the level's changes,
/// so the changed ground stayed in memory and was written to disk the next time anything saved.
///
/// What it does:
/// - Keeps a hidden copy of each level's ground as it was when the level was opened or last saved.
/// - Changing the ground marks the level as changed, so Unity asks to save it.
/// - Closing (or quitting) without saving puts the ground back from that copy and writes it to disk.
/// Switch it off with Game > Ground: Undo Unsaved Changes (on by default).
/// </summary>
[InitializeOnLoad]
public static class TerrainUnsavedGuard
{
    private const string CopyPrefix = "Saved ground copy #";
    private const string PrefKey = "GameBuilder.TerrainUnsavedGuard";
    private const string MenuPath = "Game/Ground: Undo Unsaved Changes";
    private static double nextCheck;

    public static bool Enabled
    {
        get => EditorPrefs.GetBool(PrefKey, true);
        set => EditorPrefs.SetBool(PrefKey, value);
    }

    [MenuItem(MenuPath)]
    private static void Toggle() => Enabled = !Enabled;

    [MenuItem(MenuPath, true)]
    private static bool ToggleValidate()
    {
        Menu.SetChecked(MenuPath, Enabled);
        return true;
    }

    static TerrainUnsavedGuard()
    {
        EditorSceneManager.sceneOpened += (scene, mode) => Snapshot(scene, replace: true);
        EditorSceneManager.sceneSaved += scene => Snapshot(scene, replace: true);
        EditorSceneManager.sceneClosing += OnSceneClosing;
        EditorApplication.quitting += OnQuitting;
        EditorApplication.update += Watch;
        EditorApplication.playModeStateChanged += state =>
        {
            // Starting or stopping Play closes and reopens levels: that is never "closing without saving"
            if (state == PlayModeStateChange.ExitingEditMode || state == PlayModeStateChange.ExitingPlayMode) inPlayChange = true;
            else if (state == PlayModeStateChange.EnteredEditMode) inPlayChange = false;
        };
    }

    private static bool inPlayChange;
    private static bool Busy => inPlayChange || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isPlaying;

    // ------------------------------------------------------------------ Saved copies

    private static TerrainData FindCopy(TerrainData data)
    {
        string name = CopyPrefix + data.GetInstanceID();
        foreach (TerrainData d in Resources.FindObjectsOfTypeAll<TerrainData>())
            if (d != null && d.name == name && (d.hideFlags & HideFlags.DontSave) != 0) return d;
        return null;
    }

    private static void MakeCopy(TerrainData data)
    {
        TerrainData old = FindCopy(data);
        if (old != null) Object.DestroyImmediate(old);
        TerrainData copy = Object.Instantiate(data);
        copy.name = CopyPrefix + data.GetInstanceID();
        copy.hideFlags = HideFlags.HideAndDontSave;
    }

    private static IEnumerable<TerrainData> GroundIn(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded) yield break;
        var seen = new HashSet<TerrainData>();
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Terrain t in root.GetComponentsInChildren<Terrain>(true))
                if (t.terrainData != null && EditorUtility.IsPersistent(t.terrainData) && seen.Add(t.terrainData))
                    yield return t.terrainData;
    }

    private static void Snapshot(Scene scene, bool replace)
    {
        if (Busy) return;
        foreach (TerrainData data in GroundIn(scene))
        {
            if (!replace && FindCopy(data) != null) continue;
            // Saving the level also saves its ground, so the file on disk matches
            if (replace && EditorUtility.IsDirty(data)) AssetDatabase.SaveAssetIfDirty(data);
            MakeCopy(data);
        }
    }

    /// <summary>A ground file was replaced on disk (e.g. restored from git): forget its old copy; Watch makes a new one.</summary>
    private static readonly HashSet<string> savedByUnity = new HashSet<string>();

    // Unity saving a file from memory also shows up as an import below: remember those, they aren't replacements
    private class SaveWatcher : AssetModificationProcessor
    {
        private static string[] OnWillSaveAssets(string[] paths)
        {
            foreach (string p in paths) savedByUnity.Add(p);
            return paths;
        }
    }

    private class ReimportWatcher : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            foreach (string path in imported)
            {
                if (!path.EndsWith(".asset")) continue;
                if (savedByUnity.Remove(path)) continue;
                TerrainData data = AssetDatabase.LoadAssetAtPath<TerrainData>(path);
                if (data == null) continue;
                TerrainData copy = FindCopy(data);
                if (copy != null) Object.DestroyImmediate(copy);
            }
        }
    }

    // ------------------------------------------------------------------ Watching for changes

    private static void Watch()
    {
        if (EditorApplication.timeSinceStartup < nextCheck) return;
        nextCheck = EditorApplication.timeSinceStartup + 0.5;
        if (!Enabled || Busy) return;

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (string.IsNullOrEmpty(scene.path)) continue; // a brand-new level that was never saved
            foreach (TerrainData data in GroundIn(scene))
            {
                // Ground we haven't seen yet (a new terrain, or a level's own copy of the ground): remember it as it is now
                TerrainData copy = FindCopy(data);
                if (copy == null) { MakeCopy(data); continue; }
                // The ground was changed: make sure Unity asks to save the level
                if (EditorUtility.IsDirty(data) && !scene.isDirty && !Same(copy, data)) EditorSceneManager.MarkSceneDirty(scene);
            }
        }
    }

    // ------------------------------------------------------------------ Putting it back

    private static void OnSceneClosing(Scene scene, bool removingScene)
    {
        if (!Enabled || Busy) return;
        // Not dirty = it was saved (or nothing changed): keep the ground as it is
        if (scene.isDirty) PutBack(scene);
        foreach (TerrainData data in GroundIn(scene))
        {
            TerrainData copy = FindCopy(data);
            if (copy != null) Object.DestroyImmediate(copy);
        }
    }

    private static void OnQuitting()
    {
        if (!Enabled) return;
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene.isDirty) PutBack(scene);
        }
    }

    /// <summary>Puts the ground of an unsaved level back the way it was last saved.</summary>
    public static int PutBack(Scene scene)
    {
        int count = 0;
        foreach (TerrainData data in GroundIn(scene))
        {
            TerrainData copy = FindCopy(data);
            if (copy == null || Same(copy, data)) continue;
            try { CopyGround(copy, data); }
            catch (System.Exception e)
            {
                Debug.LogWarning("🏔 Couldn't put the ground back (" + AssetDatabase.GetAssetPath(data) + "): " + e.Message);
                continue;
            }
            AssetDatabase.SaveAssetIfDirty(data);
            count++;
            Debug.Log($"🏔 The ground changes in '{scene.name}' weren't saved, so the ground was put back the way it was ({AssetDatabase.GetAssetPath(data)}).");
        }
        return count;
    }

    private static void CopyGround(TerrainData from, TerrainData to)
    {
        if (to.heightmapResolution != from.heightmapResolution) to.heightmapResolution = from.heightmapResolution;
        to.size = from.size;
        to.SetHeights(0, 0, from.GetHeights(0, 0, from.heightmapResolution, from.heightmapResolution));

        to.terrainLayers = from.terrainLayers;
        if (to.alphamapResolution != from.alphamapResolution) to.alphamapResolution = from.alphamapResolution;
        if (HasPaint(from) && HasPaint(to))
            to.SetAlphamaps(0, 0, from.GetAlphamaps(0, 0, from.alphamapWidth, from.alphamapHeight));

        if (to.holesResolution == from.holesResolution)
            to.SetHoles(0, 0, from.GetHoles(0, 0, from.holesResolution, from.holesResolution));

        to.detailPrototypes = from.detailPrototypes;
        if (to.detailResolution != from.detailResolution || to.detailResolutionPerPatch != from.detailResolutionPerPatch)
            to.SetDetailResolution(from.detailResolution, from.detailResolutionPerPatch);
        for (int layer = 0; layer < from.detailPrototypes.Length; layer++)
            to.SetDetailLayer(0, 0, layer, from.GetDetailLayer(0, 0, from.detailWidth, from.detailHeight, layer));

        to.treePrototypes = from.treePrototypes;
        to.SetTreeInstances(from.treeInstances, false);
        to.RefreshPrototypes();
        EditorUtility.SetDirty(to);

        // Update the collider and the terrains that show it
        foreach (Terrain t in Object.FindObjectsByType<Terrain>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (t.terrainData == to)
            {
                t.Flush();
                TerrainCollider col = t.GetComponent<TerrainCollider>();
                if (col != null) { col.terrainData = null; col.terrainData = to; }
            }
    }

    private static bool HasPaint(TerrainData d)
    {
        if (d.alphamapLayers == 0) return false;
        foreach (Texture2D t in d.alphamapTextures) if (t == null) return false;
        return true;
    }

    private static bool Same(TerrainData a, TerrainData b)
    {
        if (a.heightmapResolution != b.heightmapResolution || a.size != b.size) return false;
        if (a.alphamapResolution != b.alphamapResolution || a.alphamapLayers != b.alphamapLayers) return false;
        if (a.treeInstanceCount != b.treeInstanceCount || a.detailPrototypes.Length != b.detailPrototypes.Length) return false;

        int r = a.heightmapResolution;
        float[,] ha = a.GetHeights(0, 0, r, r), hb = b.GetHeights(0, 0, r, r);
        for (int y = 0; y < r; y++)
            for (int x = 0; x < r; x++)
                if (ha[y, x] != hb[y, x]) return false;

        if (HasPaint(a) && HasPaint(b))
        {
            float[,,] aa = a.GetAlphamaps(0, 0, a.alphamapWidth, a.alphamapHeight), ab = b.GetAlphamaps(0, 0, b.alphamapWidth, b.alphamapHeight);
            for (int y = 0; y < aa.GetLength(0); y++)
                for (int x = 0; x < aa.GetLength(1); x++)
                    for (int l = 0; l < aa.GetLength(2); l++)
                        if (aa[y, x, l] != ab[y, x, l]) return false;
        }

        if (a.holesResolution == b.holesResolution)
        {
            bool[,] oa = a.GetHoles(0, 0, a.holesResolution, a.holesResolution), ob = b.GetHoles(0, 0, b.holesResolution, b.holesResolution);
            for (int y = 0; y < oa.GetLength(0); y++)
                for (int x = 0; x < oa.GetLength(1); x++)
                    if (oa[y, x] != ob[y, x]) return false;
        }

        TreeInstance[] ta = a.treeInstances, tb = b.treeInstances;
        for (int i = 0; i < ta.Length; i++)
            if (ta[i].position != tb[i].position || ta[i].prototypeIndex != tb[i].prototypeIndex || ta[i].widthScale != tb[i].widthScale) return false;

        if (a.detailResolution != b.detailResolution) return false;
        for (int layer = 0; layer < a.detailPrototypes.Length; layer++)
        {
            int[,] da = a.GetDetailLayer(0, 0, a.detailWidth, a.detailHeight, layer), db = b.GetDetailLayer(0, 0, b.detailWidth, b.detailHeight, layer);
            for (int y = 0; y < da.GetLength(0); y++)
                for (int x = 0; x < da.GetLength(1); x++)
                    if (da[y, x] != db[y, x]) return false;
        }
        return true;
    }
}
