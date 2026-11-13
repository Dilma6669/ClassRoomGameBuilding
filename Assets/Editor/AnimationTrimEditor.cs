using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using ClipTrim = CustomModel.ClipTrim;

/// <summary>
/// ✂ Trim animations: a two-handle slider under each animation box, so only part of the animation plays
/// (e.g. cut the slow start off an attack, or the stumble at the end of a walk). The animation file itself is never
/// changed: the trim is saved on that one creature / player / pickup. ▶ opens a little picture right under the slider
/// that plays the trimmed part on a copy of the model (nothing in the level is touched).
/// </summary>
public static class AnimationTrimEditor
{
    private const string ShowKey = "GameBuilder.ShowAnimationTrims";

    /// <summary>The "✂ Trim animations" tick box: shows a trim slider under every animation.</summary>
    public static bool ShowTrims
    {
        get => EditorPrefs.GetBool(ShowKey, false);
        set => EditorPrefs.SetBool(ShowKey, value);
    }

    public static void ShowTrimsToggle()
    {
        bool show = EditorGUILayout.ToggleLeft(new GUIContent("✂ Trim animations (play only part of them)",
            "Shows a slider under each animation: drag its two ends to cut off the start or the end. ▶ shows it playing. Only this one creature / player changes, never the animation file."), ShowTrims);
        if (show != ShowTrims) ShowTrims = show;
    }

    /// <summary>
    /// The trim row for one animation. owner = the object that holds the trims list (for Undo),
    /// model = the model it plays on (for the ▶ picture; null = no picture), turn = its turn slider, speed = its animation speed.
    /// </summary>
    public static void TrimRow(Object owner, List<ClipTrim> trims, AnimationClip clip, GameObject model, float turn, float speed)
    {
        if (owner == null || trims == null || clip == null || clip.length <= 0.06f) return;
        float len = clip.length;
        bool trimmed = CustomModel.GetTrim(trims, clip, out float s, out float e);

        if (!ShowTrims)
        {
            if (trimmed) EditorGUILayout.LabelField(" ", "✂ Plays " + s.ToString("0.00") + " to " + e.ToString("0.00") + " s (of " + len.ToString("0.00") + " s)", EditorStyles.miniLabel);
            return;
        }

        bool open = AnimationTrimPreview.IsOpen(owner, clip);
        bool canPreview = model != null && !EditorApplication.isPlayingOrWillChangePlaymode;

        EditorGUI.indentLevel++;
        float newS = s, newE = e;
        EditorGUILayout.MinMaxSlider(new GUIContent("✂ Plays from / to", "Drag the left end to cut off the start, the right end to cut off the end. '" + clip.name + "' is " + len.ToString("0.00") + " seconds long."), ref newS, ref newE, 0f, len);
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.PrefixLabel(" ");
            int oldIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            float oldLabel = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 36;
            newS = EditorGUILayout.FloatField(new GUIContent("From", "Seconds into the animation where it starts."), newS, GUILayout.MinWidth(70));
            newE = EditorGUILayout.FloatField(new GUIContent("To", "Seconds into the animation where it stops."), newE, GUILayout.MinWidth(70));
            EditorGUIUtility.labelWidth = oldLabel;
            GUILayout.Label("of " + len.ToString("0.00") + " s", EditorStyles.miniLabel, GUILayout.ExpandWidth(false));

            using (new EditorGUI.DisabledScope(!canPreview))
            {
                bool want = GUILayout.Toggle(open, new GUIContent(" Show preview", !canPreview && model == null ? "It has no model to show it on." :
                        "Shows the trimmed part playing in a picture just below (one animation at a time)."), GUILayout.ExpandWidth(false));
                if (want != open)
                {
                    if (want) AnimationTrimPreview.Open(owner, clip, model, turn);
                    else AnimationTrimPreview.Close();
                    open = AnimationTrimPreview.IsOpen(owner, clip);
                }
            }
            using (new EditorGUI.DisabledScope(!trimmed))
            {
                if (GUILayout.Button(new GUIContent("↺", "Play the whole animation again (no trim)."), GUILayout.Width(26))) { newS = 0f; newE = len; }
            }
            EditorGUI.indentLevel = oldIndent;
        }

        newS = Mathf.Clamp(newS, 0f, len - 0.05f);
        newE = Mathf.Clamp(newE, newS + 0.05f, len);
        if (Mathf.Abs(newS - s) > 0.0001f || Mathf.Abs(newE - e) > 0.0001f)
        {
            Undo.RecordObject(owner, "Trim animation");
            SetTrim(trims, clip, newS, newE);
            EditorUtility.SetDirty(owner);
            // Open the picture (if it isn't) and show the frame at the end that was dragged
            if (canPreview)
            {
                if (!AnimationTrimPreview.IsOpen(owner, clip)) AnimationTrimPreview.Open(owner, clip, model, turn);
                AnimationTrimPreview.Hold(Mathf.Abs(newE - e) > 0.0001f ? Mathf.Min(newE, len - 0.001f) : newS);
                open = true;
            }
        }

        if (open && canPreview) AnimationTrimPreview.Draw(trims, speed);
        EditorGUI.indentLevel--;
    }

    /// <summary>Saves a trim (the whole animation = no trim, so it's removed).</summary>
    public static void SetTrim(List<ClipTrim> trims, AnimationClip clip, float start, float end)
    {
        trims.RemoveAll(t => t == null || t.clip == null);
        ClipTrim trim = trims.Find(t => t.clip == clip);
        bool whole = start <= 0.001f && end >= clip.length - 0.001f;
        if (whole) { if (trim != null) trims.Remove(trim); return; }
        if (trim == null) { trim = new ClipTrim { clip = clip }; trims.Add(trim); }
        trim.start = start;
        trim.end = end;
    }

    /// <summary>"📋 Use these trims on every X in the level": copies this model's trims to the others with the same model.</summary>
    public static void CopyToSameModels(CustomModel cm)
    {
        if (cm == null || cm.model == null || !cm.gameObject.scene.IsValid()) return;
        List<CustomModel> others = new List<CustomModel>();
        foreach (GameObject rootObject in cm.gameObject.scene.GetRootGameObjects())
            foreach (CustomModel other in rootObject.GetComponentsInChildren<CustomModel>(true))
                if (other != cm && other.model == cm.model) others.Add(other);

        string name = ObjectNames.NicifyVariableName(cm.model.name);
        if (others.Count == 0)
        {
            EditorUtility.DisplayDialog("No others", "There are no other '" + name + "' models in this level.", "OK");
            return;
        }
        if (!EditorUtility.DisplayDialog("Use these trims on every " + name + "?",
                "The " + others.Count + " other '" + name + "' model(s) in this level get the same trimmed animations as this one (their own trims are replaced).\n\nYou can undo this with Ctrl+Z.",
                "Copy trims", "Cancel")) return;

        Undo.RecordObjects(others.ToArray(), "Copy animation trims");
        foreach (CustomModel other in others)
        {
            other.trims = CustomModel.CopyTrims(cm.trims);
            EditorUtility.SetDirty(other);
        }
        Debug.Log("✂ Copied the animation trims of '" + cm.name + "' to " + others.Count + " other '" + name + "' model(s).");
    }

    /// <summary>The copy button (shown when trims are showing and this model has some trims).</summary>
    public static void CopyButton(CustomModel cm)
    {
        if (!ShowTrims || cm == null || cm.model == null || cm.trims == null || cm.trims.Count == 0) return;
        string name = ObjectNames.NicifyVariableName(cm.model.name);
        if (GUILayout.Button(new GUIContent("📋 Use these trims on every " + name + " in the level",
                "Trims are saved on each creature on its own. This copies this one's trims to all the others with the same model.")))
        {
            CopyToSameModels(cm);
            GUIUtility.ExitGUI();
        }
    }
}

/// <summary>
/// The ▶ picture under a trim slider: a copy of the model on its own little stage (not in the level),
/// playing just the trimmed part over and over, with a bar showing where it is in the whole animation.
/// </summary>
[InitializeOnLoad]
public static class AnimationTrimPreview
{
    private const float Height = 180f;

    private static Object owner;
    private static AnimationClip clip;
    private static GameObject model, copy, animRoot;
    private static PreviewRenderUtility stage;
    private static Vector3 lookAt;
    private static float distance;
    private static double startedAt, holdUntil;
    private static float holdAt;
    private static readonly List<EditorWindow> windows = new List<EditorWindow>();

    static AnimationTrimPreview()
    {
        EditorApplication.playModeStateChanged += _ => Close();
        AssemblyReloadEvents.beforeAssemblyReload += Close;
        Selection.selectionChanged += Close;
    }

    public static bool IsOpen(Object o, AnimationClip c) => stage != null && copy != null && owner == o && clip == c;

    public static void Open(Object o, AnimationClip c, GameObject m, float turn)
    {
        Close();
        if (m == null || c == null) return;
        owner = o; clip = c; model = m;

        stage = new PreviewRenderUtility();
        stage.camera.fieldOfView = 30f;
        stage.camera.clearFlags = CameraClearFlags.SolidColor;
        stage.camera.backgroundColor = new Color(0.16f, 0.18f, 0.22f);
        stage.lights[0].intensity = 1.3f;
        stage.lights[0].transform.rotation = Quaternion.Euler(40f, 40f, 0f);
        stage.lights[1].intensity = 0.6f;
        stage.ambientColor = new Color(0.35f, 0.35f, 0.4f);

        copy = Object.Instantiate(m);
        copy.hideFlags = HideFlags.HideAndDontSave;
        foreach (MonoBehaviour b in copy.GetComponentsInChildren<MonoBehaviour>(true)) Object.DestroyImmediate(b); // just the look, no scripts
        copy.transform.position = Vector3.zero;
        copy.transform.rotation = Quaternion.Euler(0f, turn, 0f);
        stage.AddSingleGO(copy);
        Animator animator = copy.GetComponentInChildren<Animator>();
        animRoot = animator != null ? animator.gameObject : copy;

        // Frame it from its starting pose (so you can see it move)
        clip.SampleAnimation(animRoot, 0f);
        Bounds b2 = new Bounds(copy.transform.position, Vector3.zero);
        bool any = false;
        foreach (Renderer r in copy.GetComponentsInChildren<Renderer>(true))
        {
            if (!any) { b2 = r.bounds; any = true; } else b2.Encapsulate(r.bounds);
        }
        float size = Mathf.Max(0.1f, b2.extents.magnitude);
        lookAt = b2.center;
        FindTravellingBone(size);
        distance = size * 3.6f;
        stage.camera.nearClipPlane = distance * 0.01f;
        stage.camera.farClipPlane = distance * 10f;

        startedAt = EditorApplication.timeSinceStartup;
        holdUntil = 0;

        windows.Clear();
        foreach (EditorWindow w in Resources.FindObjectsOfTypeAll<EditorWindow>())
        {
            string n = w.GetType().Name;
            if (n == "InspectorWindow" || n == "GameBuilderWindow") windows.Add(w);
        }
        EditorApplication.update += RepaintWindows;
    }

    private const string KeepKey = "GameBuilder.TrimPreviewKeepInMiddle";
    /// <summary>"📌 Keep it in the middle": an animation that runs / rolls away plays on the spot in the picture.</summary>
    public static bool KeepInMiddle
    {
        get => EditorPrefs.GetBool(KeepKey, true);
        set => EditorPrefs.SetBool(KeepKey, value);
    }

    private static Transform travelBone;  // the part of the skeleton that moves across the ground (usually the hips)
    private static Vector3 travelRest;    // where it is at the start of the animation

    /// <summary>Finds the bone closest to the top of the skeleton that moves sideways during the animation.</summary>
    private static void FindTravellingBone(float size)
    {
        travelBone = null;
        List<Transform> order = new List<Transform>();
        Queue<Transform> queue = new Queue<Transform>();
        queue.Enqueue(animRoot.transform);
        while (queue.Count > 0)
        {
            Transform t = queue.Dequeue();
            order.Add(t);
            foreach (Transform child in t) queue.Enqueue(child);
        }
        order.Remove(animRoot.transform);

        clip.SampleAnimation(animRoot, 0f);
        Vector3[] start = new Vector3[order.Count];
        for (int i = 0; i < order.Count; i++) start[i] = order[i].position;
        float[] moved = new float[order.Count];
        for (int k = 1; k <= 8; k++)
        {
            clip.SampleAnimation(animRoot, clip.length * k / 8f);
            for (int i = 0; i < order.Count; i++)
            {
                Vector3 d = order[i].position - start[i];
                moved[i] = Mathf.Max(moved[i], new Vector2(d.x, d.z).magnitude);
            }
        }
        // Only a bone that carries most of the body (root / hips), never a foot or hand swinging about
        int total = order.Count;
        for (int i = 0; i < order.Count; i++)
        {
            if (order[i].GetComponentsInChildren<Transform>(true).Length < total * 0.4f) continue;
            if (moved[i] > size * 0.1f) { travelBone = order[i]; travelRest = start[i]; break; }
        }
        clip.SampleAnimation(animRoot, 0f);
    }

    /// <summary>Show this moment for a little while (while a trim end is dragged), then carry on playing.</summary>
    public static void Hold(float at)
    {
        holdAt = at;
        holdUntil = EditorApplication.timeSinceStartup + 0.8;
        startedAt = holdUntil;
    }

    public static void Close()
    {
        EditorApplication.update -= RepaintWindows;
        if (copy != null) Object.DestroyImmediate(copy);
        if (stage != null) stage.Cleanup();
        stage = null; copy = null; animRoot = null; owner = null; clip = null; model = null;
        windows.Clear();
    }

    private static double lastRepaint;
    private static void RepaintWindows()
    {
        double now = EditorApplication.timeSinceStartup;
        if (now - lastRepaint < 1.0 / 30.0) return;
        lastRepaint = now;
        foreach (EditorWindow w in windows) if (w != null) w.Repaint();
    }

    /// <summary>Draws the picture (and the where-is-it bar) at this spot in the panel.</summary>
    public static void Draw(List<ClipTrim> trims, float speed)
    {
        if (stage == null || copy == null || clip == null) return;
        CustomModel.GetTrim(trims, clip, out float s, out float e);
        double now = EditorApplication.timeSinceStartup;
        float at = now < holdUntil ? holdAt : s + (float)(((now - startedAt) * Mathf.Max(0.1f, speed)) % Mathf.Max(0.05f, e - s));

        Rect area = EditorGUI.IndentedRect(GUILayoutUtility.GetRect(10f, Height, GUILayout.ExpandWidth(true)));
        Rect bar = EditorGUI.IndentedRect(GUILayoutUtility.GetRect(10f, 14f, GUILayout.ExpandWidth(true)));
        bool keep = EditorGUILayout.ToggleLeft(new GUIContent("📌 Keep it in the middle (so it can't run or roll out of the picture)",
            "Ticked: an animation that runs, rolls or walks away plays on the spot in this picture. Only the picture: in the game, 'Animation can move it' decides that."), KeepInMiddle);
        if (keep != KeepInMiddle) KeepInMiddle = keep;
        EditorGUILayout.Space(4);
        if (Event.current.type != EventType.Repaint) return;

        copy.transform.position = Vector3.zero;
        clip.SampleAnimation(animRoot, at);
        if (KeepInMiddle && travelBone != null)
        {
            // Slide the whole copy back so the travelling bone stays over its starting spot (up / down still moves)
            Vector3 p = travelBone.position;
            copy.transform.position = new Vector3(travelRest.x - p.x, 0f, travelRest.z - p.z);
        }

        // Slowly turn round it so you can see it from the side and the front
        float angle = 30f + Mathf.Sin((float)now * 0.4f) * 25f;
        Quaternion rot = Quaternion.Euler(12f, angle + 180f, 0f);
        stage.camera.transform.position = lookAt - rot * Vector3.forward * distance;
        stage.camera.transform.rotation = rot;

        stage.BeginPreview(area, GUIStyle.none);
        stage.camera.Render();
        Texture tex = stage.EndPreview();
        GUI.DrawTexture(area, tex, ScaleMode.StretchToFill, false);
        GUI.Label(new Rect(area.x + 6, area.y + 4, area.width - 12, 18), clip.name + "   " + at.ToString("0.00") + " s", EditorStyles.whiteMiniLabel);

        // The bar: the whole animation, the part that plays (green), and where it is now (white line)
        float len = clip.length;
        EditorGUI.DrawRect(bar, new Color(0.25f, 0.25f, 0.25f));
        EditorGUI.DrawRect(new Rect(bar.x + bar.width * (s / len), bar.y, bar.width * ((e - s) / len), bar.height), new Color(0.3f, 0.75f, 0.35f));
        EditorGUI.DrawRect(new Rect(bar.x + bar.width * (at / len) - 1f, bar.y - 2f, 2f, bar.height + 4f), Color.white);
    }
}
