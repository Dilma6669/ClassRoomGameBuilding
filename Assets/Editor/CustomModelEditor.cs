using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Friendly Inspector for CustomModel: model box, simple sliders, animation slots,
/// and a button that takes a picture of the model for the Library.
/// </summary>
[CustomEditor(typeof(CustomModel))]
[CanEditMultipleObjects]
public class CustomModelEditor : Editor
{
    private bool showAnimations = true;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        CustomModel cm = (CustomModel)target;

        EditorGUILayout.HelpBox(
            "1. Put a model file (.fbx, .glb or .obj) in the Assets/MyModels folder.\n" +
            "2. Drag it into the Model box below.\n" +
            "Animations called idle, walk, run, jump or fall are found and played automatically.",
            MessageType.Info);

        EditorGUILayout.Space(4);
        GUIStyle bold = new GUIStyle(EditorStyles.boldLabel) { fontSize = 13 };
        EditorGUILayout.LabelField("🎨 Model", bold);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("model"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("size"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("turn"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("raiseLower"), new GUIContent("Raise / Lower"));

        EditorGUILayout.Space(8);
        showAnimations = FoldoutBar.Draw(showAnimations, "🏃 Animations");
        if (showAnimations)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("animationsFrom"), new GUIContent("Animations From (optional)"));
            EditorGUILayout.Space(2);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("idle"), new GUIContent("Idle (standing)"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("walk"), new GUIContent("Walk"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("run"), new GUIContent("Run (sprinting)"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("jump"), new GUIContent("Jump (going up)"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("fall"), new GUIContent("Fall (coming down)"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("animationSpeed"));
            EditorGUI.indentLevel--;

            if (targets.Length == 1 && cm.model != null && cm.idle == null && cm.walk == null && cm.run == null && cm.jump == null && cm.fall == null)
            {
                EditorGUILayout.HelpBox("No animations found, so this model will stand still. That's fine for rocks and objects! " +
                                        "For moving creatures, use a model that includes animations, or drag animation clips into the boxes above.", MessageType.None);
            }

            if (GUILayout.Button("🔍 Find Animations Again"))
            {
                foreach (Object t in targets)
                {
                    CustomModel m = (CustomModel)t;
                    Undo.RecordObject(m, "Find Animations");
                    m.ClearClips();
                    m.AutoFindClips();
                    EditorUtility.SetDirty(m);
                }
            }
        }

        bool changed = serializedObject.ApplyModifiedProperties();
        if (changed)
        {
            foreach (Object t in targets) ((CustomModel)t).Refresh();
        }

        EditorGUILayout.Space(12);
        GUIStyle big = new GUIStyle(GUI.skin.button) { fontSize = 14, fontStyle = FontStyle.Bold, fixedHeight = 40 };

        using (new EditorGUI.DisabledScope(cm.ModelInstance == null || targets.Length > 1))
        {
            GUI.backgroundColor = new Color(0.2f, 0.8f, 0.3f);
            if (GUILayout.Button("📸 Add to Library (with picture)", big))
            {
                AddToLibrary(cm);
            }
            GUI.backgroundColor = Color.white;
        }

        if (cm.model != null && GUILayout.Button("❌ Remove Model (go back to the original shape)"))
        {
            foreach (Object t in targets)
            {
                CustomModel m = (CustomModel)t;
                Undo.RecordObject(m, "Remove Model");
                m.model = null;
                m.Refresh();
            }
        }
    }

    // ------------------------------------------------------------------ Library picture

    private static void AddToLibrary(CustomModel cm)
    {
        string entryName = ObjectNames.NicifyVariableName(cm.model.name);
        Sprite picture = TakePicture(cm, entryName);
        if (picture == null)
        {
            EditorUtility.DisplayDialog("Couldn't take a picture", "Make sure the model is visible in the scene and try again.", "OK");
            return;
        }

        GameSettings settings = GameMenuItems.EnsureSettingsAsset();
        if (settings == null) return;

        Undo.RecordObject(settings, "Add to Library");
        LibraryEntry entry = settings.libraryEntries.Find(e => e != null && e.name == entryName);
        bool isNew = entry == null;
        if (isNew)
        {
            entry = new LibraryEntry
            {
                name = entryName,
                category = GuessCategory(cm),
                backstory = "Who is " + entryName + "? Where did they come from, and why are they here?\n\nWrite the backstory here!"
            };
            settings.libraryEntries.Add(entry);
        }
        entry.picture = picture;

        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();

        Selection.activeObject = settings;
        EditorGUIUtility.PingObject(settings);
        EditorUtility.DisplayDialog(isNew ? "Added to the Library!" : "Picture updated!",
            (isNew ? "'" + entryName + "' was added to the Library with a picture.\n\nNow write its backstory: scroll down to Library Entries (it's the last one)."
                   : "The picture for '" + entryName + "' was updated."), "OK");
    }

    private static LibraryCategory GuessCategory(CustomModel cm)
    {
        if (cm.GetComponentInParent<PlayerController>() != null || cm.GetComponent<Health>() != null) return LibraryCategory.Character;
        ObstacleBase obstacle = cm.GetComponent<ObstacleBase>();
        if (obstacle != null)
        {
            bool moves = cm.GetComponent<PlatformStaticDriver>() == null && cm.GetComponent<TerrainStaticDriver>() == null;
            if (obstacle.payloadType == ObstacleBase.PayloadType.Damage || moves) return LibraryCategory.Creature;
        }
        return LibraryCategory.Object;
    }

    private const int PictureLayer = 31;

    /// <summary>Renders just the model with a temporary camera and saves it as a Sprite.</summary>
    public static Sprite TakePicture(CustomModel cm, string pictureName)
    {
        Transform model = cm.ModelInstance;
        if (model == null) return null;

        Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return null;
        Bounds bounds = renderers[0].bounds;
        foreach (Renderer r in renderers) bounds.Encapsulate(r.bounds);

        // Put the model on its own layer so the camera sees nothing else
        Transform[] all = model.GetComponentsInChildren<Transform>(true);
        int[] oldLayers = new int[all.Length];
        for (int i = 0; i < all.Length; i++) { oldLayers[i] = all[i].gameObject.layer; all[i].gameObject.layer = PictureLayer; }

        GameObject camGO = new GameObject("Library Picture Camera") { hideFlags = HideFlags.HideAndDontSave };
        Camera cam = camGO.AddComponent<Camera>();
        GameObject lightGO = new GameObject("Library Picture Light") { hideFlags = HideFlags.HideAndDontSave };
        RenderTexture rt = new RenderTexture(512, 512, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
        Texture2D tex = new Texture2D(512, 512, TextureFormat.RGBA32, false);

        try
        {
            Light light = lightGO.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.cullingMask = 1 << PictureLayer;
            lightGO.transform.rotation = Quaternion.LookRotation(-model.forward + Vector3.down * 0.8f - model.right * 0.4f);

            cam.cullingMask = 1 << PictureLayer;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0, 0, 0, 0);
            cam.fieldOfView = 30f;
            cam.targetTexture = rt;

            // Look from the front, a little to the side and above
            Vector3 dir = (model.forward + model.right * 0.45f + Vector3.up * 0.35f).normalized;
            float distance = bounds.extents.magnitude / Mathf.Sin(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * 1.05f;
            cam.transform.position = bounds.center + dir * distance;
            cam.transform.LookAt(bounds.center);
            cam.nearClipPlane = Mathf.Max(0.01f, distance * 0.01f);
            cam.farClipPlane = distance * 3f;

            cam.Render();
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, 512, 512), 0, 0);
            tex.Apply();
        }
        finally
        {
            RenderTexture.active = null;
            for (int i = 0; i < all.Length; i++) all[i].gameObject.layer = oldLayers[i];
            Object.DestroyImmediate(camGO);
            Object.DestroyImmediate(lightGO);
            rt.Release();
            Object.DestroyImmediate(rt);
        }

        string folder = "Assets/MyModels/Library Pictures";
        Directory.CreateDirectory(folder);
        string safeName = string.Join("_", pictureName.Split(Path.GetInvalidFileNameChars()));
        string path = folder + "/" + safeName + ".png";
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
