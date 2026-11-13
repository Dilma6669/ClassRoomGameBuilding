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
    /// <summary>An animation box like Idle/Walk/Jump: drag any animation clip in (e.g. a Jump), or click ⊙ to pick one. set is called (with Undo).</summary>
    public static void AnimationSlot(CustomModel cm, GUIContent label, AnimationClip current, System.Action<AnimationClip> set)
    {
        if (cm == null) return;
        EditorGUI.BeginChangeCheck();
        AnimationClip picked = (AnimationClip)EditorGUILayout.ObjectField(label, current, typeof(AnimationClip), false);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(cm, label.text);
            set(picked);
            EditorUtility.SetDirty(cm);
        }
    }

    /// <summary>The ✂ trim slider for one of this model's animations (see AnimationTrimEditor).</summary>
    public static void Trim(CustomModel cm, AnimationClip clip)
    {
        if (cm == null) return;
        if (cm.trims == null) cm.trims = new System.Collections.Generic.List<CustomModel.ClipTrim>();
        AnimationTrimEditor.TrimRow(cm, cm.trims, clip, cm.model, cm.turn, cm.animationSpeed);
    }

    private bool showAnimations = true;

    /// <summary>
    /// Set by the Game Builder's Player tab: it already has a 'Player Model' box and draws the
    /// Library box itself (below 'Show me my player'), so this panel leaves those out.
    /// </summary>
    public static bool InPlayerTab;

    /// <summary>Set while drawn inside a '🎨 Model' box that already has the title (Game Builder creature panel).</summary>
    public static bool InModelBox;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        CustomModel cm = (CustomModel)target;

        GUIStyle bold = new GUIStyle(EditorStyles.boldLabel) { fontSize = 13 };
        if (!InPlayerTab)
        {
            EditorGUILayout.HelpBox(
                "1. Put a model file (.fbx, .glb or .obj) in the Assets/MyModels folder.\n" +
                "2. Drag it into the Model box below.\n" +
                "Animations called idle, walk, run, jump or fall are found and played automatically.",
                MessageType.Info);

            EditorGUILayout.Space(4);
            if (!InModelBox) EditorGUILayout.LabelField("🎨 Model", bold);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("model"));
        }
        EditorGUILayout.PropertyField(serializedObject.FindProperty("size"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("turn"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("raiseLower"), new GUIContent("Raise / Lower"));

        EditorGUILayout.Space(8);
        showAnimations = FoldoutBar.Begin(showAnimations, "🏃 Animations");
        if (showAnimations)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("animationsFrom"), new GUIContent("Animations From (optional)"));
            EditorGUILayout.Space(2);
            bool one = targets.Length == 1;
            if (one) AnimationTrimEditor.ShowTrimsToggle();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("idle"), new GUIContent("Idle (standing)"));
            if (one) Trim(cm, cm.idle);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("walk"), new GUIContent("Walk"));
            if (one) Trim(cm, cm.walk);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("run"), new GUIContent("Run (sprinting)"));
            if (one) Trim(cm, cm.run);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("jump"), new GUIContent("Jump (going up)"));
            if (one) Trim(cm, cm.jump);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("fall"), new GUIContent("Fall (coming down)"));
            if (one) Trim(cm, cm.fall);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("fly"), new GUIContent("Fly (flying characters)", "Only plays for a player that can fly (a 'become a character' pickup with Can fly)."));
            if (one) Trim(cm, cm.fly);
            if (one && cm.GetComponentInParent<ObstacleBase>() != null)
            {
                AnimationSlot(cm, new GUIContent("Attack (when it hurts you)", "Drag in any animation, e.g. a Jump if it has no attack one."), cm.attack, c => cm.attack = c);
                Trim(cm, cm.attack);
                AnimationSlot(cm, new GUIContent("Death (when it's beaten)", "Plays when it's beaten; it stays in the last pose, then shrinks away."), cm.death, c => cm.death = c);
                Trim(cm, cm.death);
            }
            else if (one && cm.GetComponentInParent<Health>() != null)
            {
                AnimationSlot(cm, new GUIContent("Hurt (when you get hurt)", "Plays when something hurts you. Drag in any animation."), cm.hurt, c => cm.hurt = c);
                Trim(cm, cm.hurt);
            }
            if (one) AnimationTrimEditor.CopyButton(cm);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("animationSpeed"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("animationMovesModel"), new GUIContent("Animation can move it", "Off (normal): the model stays in its spot. On: an animation that walks the model forwards moves it away, then it snaps back when the animation starts again."));
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
        FoldoutBar.End();

        bool changed = serializedObject.ApplyModifiedProperties();
        if (changed)
        {
            foreach (Object t in targets) ((CustomModel)t).Refresh();
        }

        EditorGUILayout.Space(8);
        if (cm.model != null && GUILayout.Button("❌ Remove Model (go back to the original shape)"))
        {
            foreach (Object t in targets)
            {
                CustomModel m = (CustomModel)t;
                Undo.RecordObject(m, "Remove Model");
                m.model = null;
                m.Refresh();
            }
            GUIUtility.ExitGUI();
        }

        // The Library box (in the Player tab, the Game Builder draws it lower down itself)
        if (!InPlayerTab && targets.Length == 1 && cm.model != null)
        {
            EditorGUILayout.Space(8);
            DrawLibrarySection(cm);
        }
    }

    // ------------------------------------------------------------------ Library box

    private static GUIStyle libraryBigButton;

    /// <summary>
    /// The model's Library entry: what the Library is, its picture (if taken), and buttons to
    /// add it / take a new picture / delete the picture.
    /// </summary>
    public static void DrawLibrarySection(CustomModel cm)
    {
        if (cm == null || cm.model == null) return;
        DrawLibrarySection(ObjectNames.NicifyVariableName(cm.model.name), cm.ModelInstance, () => GuessCategory(cm), null);
    }

    /// <summary>The Library box for any model: entryName = its Library name, model = what the picture shows.</summary>
    public static void DrawLibrarySection(string entryName, Transform model, System.Func<LibraryCategory> category, string backstory)
    {
        if (libraryBigButton == null) libraryBigButton = new GUIStyle(GUI.skin.button) { fontSize = 14, fontStyle = FontStyle.Bold, fixedHeight = 40 };

        GameSettings settings = GameSettings.Load();
        LibraryEntry entry = settings != null ? settings.libraryEntries.Find(e => e != null && e.name == entryName) : null;

        using (new EditorGUILayout.VerticalScope(new GUIStyle(EditorStyles.helpBox) { margin = new RectOffset(8, 8, 4, 4), padding = new RectOffset(8, 8, 6, 8) }))
        {
            EditorGUILayout.LabelField("📖 Library", new GUIStyle(EditorStyles.boldLabel) { fontSize = 13 });
            EditorGUILayout.LabelField("The Library is a book players can read in the game (Esc > Library). Each entry has a picture and a backstory you write in Signs & Story > Game Settings > Library Entries.", EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.Space(4);

            if (entry == null)
            {
                EditorGUILayout.LabelField("'" + entryName + "' isn't in the Library yet.", EditorStyles.miniBoldLabel);
                using (new EditorGUI.DisabledScope(model == null))
                {
                    GUI.backgroundColor = new Color(0.2f, 0.8f, 0.3f);
                    if (GUILayout.Button("📸 Add to Library (takes a picture)", libraryBigButton)) { AddToLibrary(entryName, model, category, backstory); GUIUtility.ExitGUI(); }
                    GUI.backgroundColor = Color.white;
                }
                return;
            }

            // The picture (on a dark square, so a see-through background still shows the shape)
            using (new EditorGUILayout.HorizontalScope())
            {
                Rect pictureRect = GUILayoutUtility.GetRect(128, 128, GUILayout.Width(128), GUILayout.Height(128));
                EditorGUI.DrawRect(pictureRect, new Color(0.12f, 0.13f, 0.16f));
                if (entry.picture != null) GUI.DrawTexture(pictureRect, entry.picture.texture, ScaleMode.ScaleToFit);
                else GUI.Label(pictureRect, "No picture", new GUIStyle(EditorStyles.centeredGreyMiniLabel) { alignment = TextAnchor.MiddleCenter });

                using (new EditorGUILayout.VerticalScope())
                {
                    EditorGUILayout.LabelField("'" + entryName + "' is in the Library (" + entry.category + ").", EditorStyles.boldLabel);
                    if (entry.picture != null)
                        EditorGUILayout.LabelField("Picture saved in Assets/MyModels/Library Pictures.", EditorStyles.wordWrappedMiniLabel);
                    else
                        EditorGUILayout.LabelField("It has no picture. Take one with the button below.", EditorStyles.wordWrappedMiniLabel);
                }
            }

            EditorGUILayout.Space(4);
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(model == null))
                {
                    GUI.backgroundColor = new Color(0.4f, 0.75f, 1f);
                    if (GUILayout.Button(entry.picture != null ? "📸 Take a new picture" : "📸 Take a picture", libraryBigButton)) { AddToLibrary(entryName, model, category, backstory); GUIUtility.ExitGUI(); }
                }
                using (new EditorGUI.DisabledScope(entry.picture == null))
                {
                    GUI.backgroundColor = new Color(0.95f, 0.5f, 0.5f);
                    if (GUILayout.Button("🗑 Delete picture", libraryBigButton)) { DeletePicture(settings, entry); GUIUtility.ExitGUI(); }
                }
                GUI.backgroundColor = Color.white;
            }
        }
    }

    /// <summary>Deletes the entry's picture file (the entry and its backstory stay in the Library).</summary>
    private static void DeletePicture(GameSettings settings, LibraryEntry entry)
    {
        if (entry.picture == null) return;
        if (!EditorUtility.DisplayDialog("Delete the Library picture?",
                "This deletes the picture of '" + entry.name + "'.\nIts Library entry and backstory are kept, and you can take a new picture any time.",
                "Delete picture", "Cancel")) return;

        string path = AssetDatabase.GetAssetPath(entry.picture);
        Undo.RecordObject(settings, "Delete Library picture");
        entry.picture = null;
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
        // Only delete files this tool made, never a picture someone dragged in from elsewhere
        if (!string.IsNullOrEmpty(path) && path.StartsWith("Assets/MyModels/Library Pictures/")) AssetDatabase.DeleteAsset(path);
    }

    // ------------------------------------------------------------------ Library picture

    private static void AddToLibrary(string entryName, Transform model, System.Func<LibraryCategory> category, string backstory)
    {
        Sprite picture = TakePicture(model, entryName);
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
                category = category(),
                backstory = backstory ?? "Who is " + entryName + "? Where did they come from, and why are they here?\n\nWrite the backstory here!"
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
    public static Sprite TakePicture(CustomModel cm, string pictureName) => TakePicture(cm != null ? cm.ModelInstance : null, pictureName);

    /// <summary>Renders just this object (and its children) with a temporary camera and saves it as a Sprite.</summary>
    public static Sprite TakePicture(Transform model, string pictureName)
    {
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
