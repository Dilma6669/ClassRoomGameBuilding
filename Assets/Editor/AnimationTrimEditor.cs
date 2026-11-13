using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using ClipTrim = CustomModel.ClipTrim;

/// <summary>
/// ✂ Trim animations: a two-handle slider under each animation box, so only part of the animation plays
/// (e.g. cut the slow start off an attack, or the stumble at the end of a walk). The animation file itself is never
/// changed: the trim is saved on that one creature / player / pickup. ▶ plays the trimmed part on the model in the Scene view.
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
            "Shows a slider under each animation: drag its two ends to cut off the start or the end. Only this one creature / player changes, never the animation file."), ShowTrims);
        if (show != ShowTrims) ShowTrims = show;
    }

    /// <summary>
    /// The trim row for one animation. owner = the object that holds the trims list (for Undo),
    /// previewOn = the model to play it on in the Scene view (null = no ▶ button), speed = its animation speed.
    /// </summary>
    public static void TrimRow(Object owner, List<ClipTrim> trims, AnimationClip clip, CustomModel previewOn, float speed)
    {
        if (owner == null || trims == null || clip == null || clip.length <= 0.06f) return;
        float len = clip.length;
        bool trimmed = CustomModel.GetTrim(trims, clip, out float s, out float e);

        if (!ShowTrims)
        {
            if (trimmed) EditorGUILayout.LabelField(" ", "✂ Plays " + s.ToString("0.00") + " to " + e.ToString("0.00") + " s (of " + len.ToString("0.00") + " s)", EditorStyles.miniLabel);
            return;
        }

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

            GameObject root = previewOn != null && previewOn.model != null ? previewOn.AnimationRoot() : null;
            using (new EditorGUI.DisabledScope(root == null || EditorApplication.isPlayingOrWillChangePlaymode))
            {
                bool playing = AnimationTrimPreview.IsPlaying(root, clip);
                if (GUILayout.Button(new GUIContent(playing ? "■" : "▶", playing ? "Stop the preview." :
                        root == null ? "No model in the scene to show it on." : "Plays the trimmed part on the model in the Scene view, over and over."), GUILayout.Width(26)))
                {
                    if (playing) AnimationTrimPreview.Stop();
                    else AnimationTrimPreview.Play(root, clip, trims, speed);
                }
            }
            using (new EditorGUI.DisabledScope(!trimmed))
            {
                if (GUILayout.Button(new GUIContent("↺", "Play the whole animation again (no trim)."), GUILayout.Width(26))) { newS = 0f; newE = len; }
            }
            EditorGUI.indentLevel = oldIndent;
        }
        EditorGUI.indentLevel--;

        newS = Mathf.Clamp(newS, 0f, len - 0.05f);
        newE = Mathf.Clamp(newE, newS + 0.05f, len);
        if (Mathf.Abs(newS - s) > 0.0001f || Mathf.Abs(newE - e) > 0.0001f)
        {
            Undo.RecordObject(owner, "Trim animation");
            SetTrim(trims, clip, newS, newE);
            EditorUtility.SetDirty(owner);
            // Show the frame at the end that was dragged
            GameObject root = previewOn != null && previewOn.model != null ? previewOn.AnimationRoot() : null;
            if (root != null && !EditorApplication.isPlayingOrWillChangePlaymode)
                AnimationTrimPreview.Peek(root, clip, trims, speed, Mathf.Abs(newE - e) > 0.0001f ? Mathf.Min(newE, len - 0.001f) : newS);
        }
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

/// <summary>Plays a trimmed animation on a model in the Scene view (nothing is saved: the pose goes back when it stops).</summary>
[InitializeOnLoad]
public static class AnimationTrimPreview
{
    private static GameObject root;
    private static AnimationClip clip;
    private static List<ClipTrim> trims;
    private static float speed = 1f;
    private static bool looping;
    private static double time, lastTick, peekUntil;
    private static float peekAt;
    private static bool hooked;

    static AnimationTrimPreview()
    {
        EditorApplication.playModeStateChanged += _ => Stop();
        AssemblyReloadEvents.beforeAssemblyReload += Stop;
        Selection.selectionChanged += Stop;
        UnityEditor.SceneManagement.EditorSceneManager.sceneClosing += (s, r) => Stop();
    }

    public static bool IsPlaying(GameObject r, AnimationClip c) => looping && r != null && root == r && clip == c;

    /// <summary>Plays the trimmed part over and over.</summary>
    public static void Play(GameObject r, AnimationClip c, List<ClipTrim> t, float s)
    {
        Begin(r, c, t, s);
        looping = true;
        time = 0;
        peekUntil = 0;
    }

    /// <summary>Shows one moment for a little while (while dragging a trim end), then carries on / stops.</summary>
    public static void Peek(GameObject r, AnimationClip c, List<ClipTrim> t, float s, float at)
    {
        if (!(root == r && clip == c)) { Begin(r, c, t, s); looping = false; }
        peekAt = at;
        peekUntil = EditorApplication.timeSinceStartup + 0.8;
        time = 0;
        Tick();
    }

    public static void Stop()
    {
        if (hooked) EditorApplication.update -= Tick;
        hooked = false;
        looping = false;
        root = null;
        clip = null;
        trims = null;
        if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
        SceneView.RepaintAll();
    }

    private static void Begin(GameObject r, AnimationClip c, List<ClipTrim> t, float s)
    {
        if (root != null && root != r && AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode(); // put the other model back
        root = r; clip = c; trims = t; speed = Mathf.Max(0.1f, s);
        if (!AnimationMode.InAnimationMode()) AnimationMode.StartAnimationMode();
        lastTick = EditorApplication.timeSinceStartup;
        if (!hooked) { EditorApplication.update += Tick; hooked = true; }
    }

    private static void Tick()
    {
        if (root == null || clip == null || EditorApplication.isPlayingOrWillChangePlaymode || !AnimationMode.InAnimationMode()) { Stop(); return; }
        double now = EditorApplication.timeSinceStartup;
        double dt = now - lastTick;
        lastTick = now;

        float at;
        if (now < peekUntil) at = peekAt;
        else if (!looping) { Stop(); return; }
        else
        {
            CustomModel.GetTrim(trims, clip, out float s, out float e);
            time += dt * speed;
            double len = Mathf.Max(0.05f, e - s);
            at = s + (float)(time % len);
        }

        AnimationMode.BeginSampling();
        AnimationMode.SampleAnimationClip(root, clip, at);
        AnimationMode.EndSampling();
        SceneView.RepaintAll();
    }
}
