using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Adds a "Game" menu to the Unity menu bar:
///   Game > Settings and Library      - edit menu text, death screen, world border and Library backstories
///   Game > Add Open Scene to Build   - make the current level part of the built game
///   Game > Build Game (Windows)      - make a .exe you can run outside Unity
/// Also makes sure Assets/Resources/GameSettings.asset exists.
/// </summary>
[InitializeOnLoad]
public static class GameMenuItems
{
    private const string ResourcesFolder = "Assets/Resources";
    private const string SettingsPath = ResourcesFolder + "/GameSettings.asset";

    static GameMenuItems()
    {
        EditorApplication.delayCall += () => EnsureSettingsAsset();
    }

    public static GameSettings EnsureSettingsAsset()
    {
        GameSettings settings = AssetDatabase.LoadAssetAtPath<GameSettings>(SettingsPath);
        if (settings != null) return settings;

        // File exists but isn't imported yet - never overwrite the students' work
        if (File.Exists(SettingsPath)) return null;

        if (!AssetDatabase.IsValidFolder(ResourcesFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }

        settings = ScriptableObject.CreateInstance<GameSettings>();
        settings.libraryEntries = GameSettings.DefaultLibrary();
        settings.titleFont = FindFont("SueEllenFrancisco");
        ApplyDefaultModels(settings);

        AssetDatabase.CreateAsset(settings, SettingsPath);
        AssetDatabase.SaveAssets();
        Debug.Log("✅ Created " + SettingsPath + ". Open it from Game > Settings and Library.");
        return settings;
    }

    /// <summary>Fills any empty Default Model slots with the class models, if they are in Assets/MyModels.</summary>
    public static bool ApplyDefaultModels(GameSettings settings)
    {
        bool changed = false;
        GameObject Find(string fileName)
        {
            string guid = AssetDatabase.FindAssets(fileName, new[] { MyModelsImporter.ModelsFolder }).FirstOrDefault();
            return guid == null ? null : AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
        }
        if (settings.defaultPlayerModel == null) { settings.defaultPlayerModel = Find("Oldman-model"); changed |= settings.defaultPlayerModel != null; }
        if (settings.defaultWanderEnemy == null) { settings.defaultWanderEnemy = Find("Spider-model"); changed |= settings.defaultWanderEnemy != null; }
        if (settings.defaultPatrolEnemy == null) { settings.defaultPatrolEnemy = Find("Zombie-model"); changed |= settings.defaultPatrolEnemy != null; }
        if (settings.defaultStaticEnemy == null) { settings.defaultStaticEnemy = Find("Snake-model"); changed |= settings.defaultStaticEnemy != null; }
        return changed;
    }

    private static TMP_FontAsset FindFont(string search)
    {
        string guid = AssetDatabase.FindAssets(search + " t:TMP_FontAsset").FirstOrDefault();
        return guid == null ? null : AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guid));
    }

    [MenuItem("Game/Settings and Library", priority = 0)]
    public static void OpenSettings()
    {
        GameSettings settings = EnsureSettingsAsset();
        if (settings == null) return;
        Selection.activeObject = settings;
        EditorGUIUtility.PingObject(settings);
    }

    [MenuItem("Game/Create Checkpoint", priority = 10)]
    public static void CreateCheckpoint()
    {
        // Place it where the Scene view is looking, dropped onto the ground
        Vector3 position = Vector3.zero;
        SceneView view = SceneView.lastActiveSceneView;
        if (view != null) position = view.pivot;
        if (Physics.Raycast(position + Vector3.up * 500f, Vector3.down, out RaycastHit hit, 2000f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            position = hit.point;
        }

        GameObject container = GameObject.Find("Checkpoints");
        if (container == null)
        {
            container = new GameObject("Checkpoints");
            Undo.RegisterCreatedObjectUndo(container, "Create Checkpoint");
        }

        GameObject root = new GameObject("Checkpoint " + (container.transform.childCount + 1));
        root.transform.SetParent(container.transform, false);
        root.transform.position = position;

        BoxCollider zone = root.AddComponent<BoxCollider>();
        zone.isTrigger = true;
        zone.center = new Vector3(0f, 2f, 0f);
        zone.size = new Vector3(3f, 4f, 3f);

        AddFlagPart(root.transform, "Base", PrimitiveType.Cylinder, new Vector3(0f, 0.05f, 0f), new Vector3(1.2f, 0.05f, 1.2f));
        AddFlagPart(root.transform, "Pole", PrimitiveType.Cylinder, new Vector3(0f, 1.5f, 0f), new Vector3(0.12f, 1.5f, 0.12f));
        AddFlagPart(root.transform, "Flag", PrimitiveType.Cube, new Vector3(0f, 2.65f, 0.45f), new Vector3(0.05f, 0.6f, 0.9f));

        root.AddComponent<Checkpoint>();

        Undo.RegisterCreatedObjectUndo(root, "Create Checkpoint");
        Selection.activeGameObject = root;
        if (view != null) view.FrameSelected();
        Debug.Log("🚩 Checkpoint created. Move it to where the player should restart after dying.");
    }

    private static void AddFlagPart(Transform parent, string partName, PrimitiveType type, Vector3 localPosition, Vector3 localScale)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        part.name = partName;
        Object.DestroyImmediate(part.GetComponent<Collider>()); // the player walks through the flag
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localScale = localScale;
    }

    [MenuItem("Game/Add Open Scene to Build", priority = 20)]
    public static void AddOpenSceneToBuild()
    {
        string path = EditorSceneManager.GetActiveScene().path;
        if (string.IsNullOrEmpty(path))
        {
            EditorUtility.DisplayDialog("Save the scene first", "Save this scene before adding it to the build.", "OK");
            return;
        }

        var scenes = EditorBuildSettings.scenes.ToList();
        if (scenes.Any(s => s.path == path))
        {
            EditorUtility.DisplayDialog("Already added", path + " is already in the build.", "OK");
            return;
        }

        scenes.Add(new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = scenes.ToArray();
        EditorUtility.DisplayDialog("Scene added", path + " was added to the build.\n\nThe first scene in the list is where the game starts.", "OK");
    }

    [MenuItem("Game/Build Game (Windows)", priority = 21)]
    public static void BuildWindowsMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        BuildReport report = BuildWindows();
        if (report == null) return;

        if (report.summary.result == BuildResult.Succeeded)
        {
            string exe = report.summary.outputPath;
            if (EditorUtility.DisplayDialog("Build finished!",
                    "Your game was built to:\n" + exe + "\n\nCopy the whole folder to share the game.",
                    "Show in Explorer", "Close"))
            {
                EditorUtility.RevealInFinder(exe);
            }
        }
        else
        {
            EditorUtility.DisplayDialog("Build failed",
                "The build did not finish (" + report.summary.totalErrors + " errors). Check the Console window for red error messages.", "OK");
        }
    }

    /// <summary>Builds every enabled scene in Build Settings to Build/Windows. Also usable from the command line.</summary>
    public static BuildReport BuildWindows()
    {
        EnsureSettingsAsset();

        string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        if (scenes.Length == 0)
        {
            Debug.LogError("No scenes in Build Settings. Open a level and use Game > Add Open Scene to Build.");
            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog("No scenes", "There are no scenes in the build yet.\nOpen a level and use Game > Add Open Scene to Build.", "OK");
            }
            return null;
        }

        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string exeName = string.Join("_", PlayerSettings.productName.Split(Path.GetInvalidFileNameChars())) + ".exe";
        string outputPath = Path.Combine(projectRoot, "Build", "Windows", exeName);

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = BuildTarget.StandaloneWindows64,
            targetGroup = BuildTargetGroup.Standalone,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        Debug.Log("Build " + report.summary.result + ": " + outputPath + " (" + report.summary.totalErrors + " errors)");

        if (Application.isBatchMode)
        {
            EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
        }
        return report;
    }

    // ------------------------------------------------------------------ Mac

    public static bool MacSupported => BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneOSX);

    [MenuItem("Game/Build Game (Mac)", priority = 22)]
    public static void BuildMacMenu()
    {
        if (!MacSupported)
        {
            EditorUtility.DisplayDialog("Mac builds aren't set up",
                "This Unity can't make Mac games yet. In Unity Hub: Installs > the ⚙ next to this Unity version > Add modules > tick 'Mac Build Support (Mono)'.", "OK");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        string zip = BuildMac(out BuildReport report);
        if (report == null) return;
        if (report.summary.result == BuildResult.Succeeded && zip != null)
        {
            if (EditorUtility.DisplayDialog("Mac build finished!",
                    "Your Mac game is zipped here:\n" + zip + "\n\nSend the zip to Mac users. It has a 'HOW TO OPEN ON A MAC' note inside.",
                    "Show in Explorer", "Close"))
            {
                EditorUtility.RevealInFinder(zip);
            }
        }
        else
        {
            EditorUtility.DisplayDialog("Build failed",
                "The Mac build did not finish (" + report.summary.totalErrors + " errors). Check the Console window for red error messages.", "OK");
        }
    }

    /// <summary>Builds every enabled scene to Build/Mac/&lt;Game&gt;.app and zips it (Build/&lt;Game&gt; (Mac).zip). Returns the zip's path.</summary>
    public static string BuildMac(out BuildReport report)
    {
        report = null;
        EnsureSettingsAsset();
        string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        if (scenes.Length == 0)
        {
            Debug.LogError("No scenes in Build Settings. Open a level and use Game > Add Open Scene to Build.");
            return null;
        }

        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string gameName = string.Join("_", PlayerSettings.productName.Split(Path.GetInvalidFileNameChars()));
        string folder = Path.Combine(projectRoot, "Build", "Mac");
        report = BuildMacApp(scenes, folder, gameName);
        if (report.summary.result != BuildResult.Succeeded) return null;
        return MacZip.ZipApp(Path.Combine(folder, gameName + ".app"), Path.Combine(projectRoot, "Build", gameName + " (Mac).zip"));
    }

    /// <summary>Builds the scenes as a Mac app (Intel and Apple-chip Macs) into an empty folder.</summary>
    public static BuildReport BuildMacApp(string[] scenes, string folder, string appName)
    {
        if (Directory.Exists(folder)) Directory.Delete(folder, true);
        SetMacUniversal();
        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = Path.Combine(folder, appName + ".app"),
            target = BuildTarget.StandaloneOSX,
            targetGroup = BuildTargetGroup.Standalone,
            options = BuildOptions.None
        };
        BuildReport report = BuildPipeline.BuildPlayer(options);
        Debug.Log("Mac build " + report.summary.result + ": " + options.locationPathName + " (" + report.summary.totalErrors + " errors)");
        return report;
    }

    /// <summary>Intel + Apple silicon in one app. The setting lives in the Mac module, so it's set by name (no error if it's missing).</summary>
    private static void SetMacUniversal()
    {
        System.Type settings = System.AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.GetType("UnityEditor.OSXStandalone.UserBuildSettings"))
            .FirstOrDefault(t => t != null);
        System.Reflection.PropertyInfo arch = settings?.GetProperty("architecture", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        if (arch == null) return;
        try
        {
            object universal = System.Enum.Parse(arch.PropertyType, "x64ARM64");
            arch.SetValue(null, universal);
        }
        catch (System.Exception e) { Debug.LogWarning("Couldn't set the Mac build to Intel + Apple silicon: " + e.Message); }
    }
}
