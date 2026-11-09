using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Makes new levels for the Game Builder (Levels & Build tab), and gives every level its own ground,
/// so sculpting one level never changes another.
/// </summary>
[InitializeOnLoad]
public static class LevelMaker
{
    public const string ScenesFolder = "Assets/Scenes";
    public const string TemplatePath = "Assets/Templates/EmptyLevel - DO NOT EDIT.unity";
    public const string GroundFolder = ScenesFolder + "/LevelData";

    // The ground every "Create Terrain" starts from (inside the TerrainLogicDefault prefab)
    private const string SharedGroundPath = "Assets/Prefabs/Terrain/TerrainDefault/TerrainDefault.asset";

    static LevelMaker()
    {
        // You have to select the ground before you can sculpt it, so that's the moment to give the level its own copy
        Selection.selectionChanged += () =>
        {
            GameObject go = Selection.activeGameObject;
            if (go == null || EditorApplication.isPlayingOrWillChangePlaymode) return;
            Terrain terrain = go.GetComponentInParent<Terrain>();
            if (terrain != null) GiveOwnGround(terrain);
        };
    }

    // ------------------------------------------------------------------ Names

    /// <summary>The next unused "LevelNN" name.</summary>
    public static string NextFreeName()
    {
        for (int i = 1; i < 100; i++)
        {
            string name = "Level" + i.ToString("00");
            if (!File.Exists(ScenePath(name))) return name;
        }
        return "MyLevel";
    }

    public static string ScenePath(string levelName) => ScenesFolder + "/" + levelName + ".unity";

    /// <summary>Null if the name is fine, otherwise what's wrong with it.</summary>
    public static string CheckName(string levelName)
    {
        if (string.IsNullOrWhiteSpace(levelName)) return "Type a name for the level.";
        if (levelName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || levelName.Contains("/") || levelName.Contains("."))
            return "Names can't have these in them:  / \\ : * ? \" < > | .";
        if (File.Exists(ScenePath(levelName.Trim()))) return "There's already a level called " + levelName.Trim() + ".";
        return null;
    }

    // ------------------------------------------------------------------ Making levels

    /// <summary>Makes an empty level (no ground or player yet), adds it to the game and opens it.</summary>
    public static bool CreateNewLevel(string levelName)
    {
        if (!File.Exists(TemplatePath))
        {
            EditorUtility.DisplayDialog("Template missing", "The empty level template is missing:\n" + TemplatePath + "\n\nAsk your teacher to restore it.", "OK");
            return false;
        }
        return MakeLevel(TemplatePath, levelName.Trim(), false);
    }

    /// <summary>Copies the open level (ground, platforms, everything) into a new level and opens it.</summary>
    public static bool CopyCurrentLevel(string levelName)
    {
        string source = SceneManager.GetActiveScene().path;
        if (string.IsNullOrEmpty(source))
        {
            EditorUtility.DisplayDialog("Save first", "Save this level before copying it.", "OK");
            return false;
        }
        return MakeLevel(source, levelName.Trim(), true);
    }

    private static bool MakeLevel(string sourcePath, string levelName, bool copyGround)
    {
        string problem = CheckName(levelName);
        if (problem != null)
        {
            EditorUtility.DisplayDialog("Level name", problem, "OK");
            return false;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return false;

        string path = ScenePath(levelName);
        if (!AssetDatabase.CopyAsset(sourcePath, path))
        {
            EditorUtility.DisplayDialog("Couldn't make the level", "Unity couldn't copy " + sourcePath + " to " + path + ".", "OK");
            return false;
        }

        // Add to the end of the level list, ticked (by GUID, so it never points at an old file)
        GUID guid = new GUID(AssetDatabase.AssetPathToGUID(path));
        EditorBuildSettings.scenes = EditorBuildSettings.scenes
            .Where(s => s.path != path)
            .Append(new EditorBuildSettingsScene(guid, true))
            .ToArray();

        Scene scene = EditorSceneManager.OpenScene(path);

        if (copyGround)
        {
            // The copy still points at the old level's ground: give it its own
            foreach (Terrain terrain in Object.FindObjectsByType<Terrain>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (terrain.gameObject.scene == scene) GiveOwnGround(terrain, force: true);
            }
            EditorSceneManager.SaveScene(scene);
        }

        Debug.Log("✅ Made a new level: " + levelName + " (added to the end of your game's level list).");
        return true;
    }

    // ------------------------------------------------------------------ Each level owns its ground

    /// <summary>
    /// If this terrain still uses the shared starting ground (or force is on), copy the ground
    /// into Assets/Scenes/LevelData/[level]_Ground.asset and use that instead.
    /// </summary>
    public static void GiveOwnGround(Terrain terrain, bool force = false)
    {
        if (terrain == null || terrain.terrainData == null) return;
        if (EditorUtility.IsPersistent(terrain)) return; // a prefab asset, not a level

        string dataPath = AssetDatabase.GetAssetPath(terrain.terrainData);
        if (!force && dataPath != SharedGroundPath) return;

        Scene scene = terrain.gameObject.scene;
        if (string.IsNullOrEmpty(scene.path))
        {
            Debug.LogWarning("💾 Save this level first, so its ground can be kept separate from other levels.");
            return;
        }

        if (!AssetDatabase.IsValidFolder(GroundFolder)) AssetDatabase.CreateFolder(ScenesFolder, "LevelData");
        string newPath = AssetDatabase.GenerateUniqueAssetPath(GroundFolder + "/" + scene.name + "_Ground.asset");

        TerrainData copy = Object.Instantiate(terrain.terrainData);
        copy.name = Path.GetFileNameWithoutExtension(newPath);
        AssetDatabase.CreateAsset(copy, newPath);

        // Record both first: setting the terrain's data can quietly update the collider too,
        // and then the collider's change wouldn't be saved in the level
        TerrainCollider collider = terrain.GetComponent<TerrainCollider>();
        Undo.RecordObject(terrain, "Own Ground");
        if (collider != null) Undo.RecordObject(collider, "Own Ground");
        terrain.terrainData = copy;
        if (collider != null) collider.terrainData = copy;
        PrefabUtility.RecordPrefabInstancePropertyModifications(terrain);
        if (collider != null) PrefabUtility.RecordPrefabInstancePropertyModifications(collider);
        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("🏔 " + scene.name + " now has its own ground (" + newPath + "), so sculpting it won't change other levels.");
    }
}
