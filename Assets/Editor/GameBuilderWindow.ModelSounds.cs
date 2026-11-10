using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Game Builder > 🔊 Sounds > 🎭 Sounds for one model: pick a model, then drag a sound onto each of its
/// animations, and optionally give it its own attack / hurt / death sounds. Empty slots use the general sounds.
/// </summary>
public partial class GameBuilderWindow
{
    private GameObject soundModel;
    [System.NonSerialized] private List<GameObject> myModels;
    [System.NonSerialized] private GameObject clipsCachedFor;
    private string animSearch;
    private List<AnimationClip> cachedClips = new List<AnimationClip>();
    private Dictionary<AnimationClip, string> cachedUsedFor = new Dictionary<AnimationClip, string>();

    private static readonly SoundEvent[] ModelCreatureEvents = { SoundEvent.EnemyAttack, SoundEvent.EnemyDamage, SoundEvent.EnemyDeath, SoundEvent.Attack };
    private static readonly SoundEvent[] ModelPlayerEvents = { SoundEvent.Jump, SoundEvent.Damage, SoundEvent.FallDamage, SoundEvent.PlayerDeath };
    private static readonly SoundEvent[] ModelPickupEvents = { SoundEvent.PickUp, SoundEvent.Bounce };

    private void DrawModelSounds(GameSounds sounds)
    {
        EditorGUILayout.LabelField("Pick a model (from Assets/MyModels or the Library) to see its animations. Drag a sound onto an animation and it plays every time that animation plays, on the player or on a creature.\nA model's own sound beats the general sound. Anything left empty uses the general sound.", EditorStyles.wordWrappedMiniLabel);

        if (myModels == null) myModels = FindMyModels();

        // Models that already have sounds, as quick buttons
        List<GameObject> withSounds = sounds.modelSounds.Where(m => m != null && m.model != null && m.animations.Any(a => a != null && (a.sound != null || (a.playWhen != AnimationMoment.Normal && a.playWhen != AnimationMoment.Never)))).Select(m => m.model)
            .Concat(sounds.sounds.Where(s => s != null && s.onlyModel != null && s.clip != null).Select(s => s.onlyModel)).Distinct().ToList();
        if (withSounds.Count > 0)
        {
            EditorGUILayout.LabelField("Models with their own sounds:", EditorStyles.miniBoldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                int shown = 0;
                foreach (GameObject m in withSounds)
                {
                    if (shown > 0 && shown % 4 == 0) { EditorGUILayout.EndHorizontal(); EditorGUILayout.BeginHorizontal(); }
                    Color old = GUI.backgroundColor;
                    if (m == soundModel) GUI.backgroundColor = new Color(0.6f, 0.85f, 1f);
                    if (GUILayout.Button("🎭 " + m.name)) soundModel = m;
                    GUI.backgroundColor = old;
                    shown++;
                }
                GUILayout.FlexibleSpace();
            }
        }

        // Pick a model: from the list, or by dragging one in
        GameSettings settings = GameSettings.Load();
        HashSet<string> inLibrary = new HashSet<string>(settings.libraryEntries.Where(l => l != null).Select(l => l.name.ToLowerInvariant()));
        // Models in sub-folders (e.g. the nature pack) go in a sub-menu
        string[] names = new[] { "Choose a model..." }.Concat(myModels.Select(m =>
        {
            string folder = System.IO.Path.GetDirectoryName(AssetDatabase.GetAssetPath(m)).Replace('\\', '/');
            string sub = folder.Length > "Assets/MyModels".Length ? folder.Substring("Assets/MyModels/".Length).Split('/')[0] + "/" : "";
            return sub + m.name + (inLibrary.Contains(m.name.ToLowerInvariant()) ? "   📖" : "");
        })).ToArray();
        using (new EditorGUILayout.HorizontalScope())
        {
            int index = myModels.IndexOf(soundModel) + 1;
            int chosen = EditorGUILayout.Popup(index, names, GUILayout.Height(22));
            if (chosen != index) soundModel = chosen == 0 ? null : myModels[chosen - 1];
            if (GUILayout.Button(new GUIContent("🔄", "Look for new models"), GUILayout.Width(30))) myModels = FindMyModels();
        }
        soundModel = (GameObject)EditorGUILayout.ObjectField("...or drag a model here", soundModel, typeof(GameObject), false);
        EditorGUILayout.LabelField("📖 = it has a Library entry", EditorStyles.miniLabel);

        if (soundModel == null) return;

        // Slightly narrower than the section box, so it sits inside it
        GUIStyle modelBox = new GUIStyle(EditorStyles.helpBox) { margin = new RectOffset(10, 10, 6, 6) };
        using (new EditorGUILayout.VerticalScope(modelBox))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                Texture2D picture = AssetPreview.GetAssetPreview(soundModel);
                // Always keep the space (the picture can finish loading between two GUI passes)
                Rect pictureRect = GUILayoutUtility.GetRect(64, 64, GUILayout.Width(64), GUILayout.Height(64));
                if (picture != null) GUI.DrawTexture(pictureRect, picture, ScaleMode.ScaleToFit);
                else if (AssetPreview.IsLoadingAssetPreview(soundModel.GetInstanceID())) Repaint();
                GUILayout.Label("🎭 " + soundModel.name, titleStyle);
            }

            // ---- Animations
            if (clipsCachedFor != soundModel)
            {
                clipsCachedFor = soundModel;
                foldouts["modelPlayerAnyway"] = false;
                cachedClips = CustomModel.ClipsForModel(soundModel, out cachedUsedFor);
            }

            ModelSounds ms = sounds.modelSounds.FirstOrDefault(m => m != null && m.model == soundModel);

            GUILayout.Label("🎬 Its animations (the game plays these by itself)", EditorStyles.boldLabel);
            if (cachedClips.Count == 0)
            {
                EditorGUILayout.HelpBox("This model has no animations, so only the sounds below can be set.", MessageType.None);
            }
            foreach (AnimationClip clip in cachedClips.Where(c => cachedUsedFor.ContainsKey(c)))
            {
                DrawAnimationRow(sounds, ref ms, clip, cachedUsedFor[clip]);
            }

            List<AnimationClip> extras = cachedClips.Where(c => !cachedUsedFor.ContainsKey(c)).ToList();
            if (extras.Count > 0 && BeginFoldout("extraAnims", "🎭 Its extra animations (" + extras.Count + "): choose when each one plays", true))
            {
                EditorGUILayout.HelpBox("Pick when an extra animation plays (e.g. Bite = 'it attacks you', Death = 'it is beaten', Howl = 'now and then'), and drag in a sound for it. Its sound replaces the general sound for that moment.", MessageType.None);
                // Some models have hundreds of animations: the ones already set up, then a search
                ModelSounds current = ms;
                List<AnimationClip> setUp = extras.Where(c => current != null && current.animations.Any(a => a != null && a.animation == c)).ToList();
                foreach (AnimationClip clip in setUp) DrawAnimationRow(sounds, ref ms, clip, null);

                EditorGUILayout.Space(4);
                animSearch = EditorGUILayout.TextField(new GUIContent("🔍 Find an animation", "Type part of a name, e.g. death, bite, yell"), animSearch ?? "");
                List<AnimationClip> matches = extras.Where(c => !setUp.Contains(c) && (string.IsNullOrEmpty(animSearch) || c.name.ToLowerInvariant().Contains(animSearch.ToLowerInvariant()))).ToList();
                const int MaxShown = 12;
                foreach (AnimationClip clip in matches.Take(MaxShown)) DrawAnimationRow(sounds, ref ms, clip, null);
                if (matches.Count > MaxShown) EditorGUILayout.LabelField("...and " + (matches.Count - MaxShown) + " more. Type in the search box to find one.", EditorStyles.miniBoldLabel);
            }
            if (extras.Count > 0) EndFoldout();

            // ---- Its own versions of the general sounds
            const string JustThisModel = "  (replace the default sound for just this model)";
            if (BeginFoldout("modelCreature", "👹 When it's a creature" + JustThisModel, true))
            {
                foreach (SoundEvent e in ModelCreatureEvents) DrawModelEvent(sounds, e);
            }
            EndFoldout();

            // There's only one player, so its sounds are usually just the general sounds at the top.
            // Only show this group when this model really is (or might be) the player.
            bool showPlayer = IsPlayerModel(sounds, soundModel) || (foldouts.TryGetValue("modelPlayerAnyway", out bool anyway) && anyway);
            if (showPlayer)
            {
                if (BeginFoldout("modelPlayer", "🧙 When it's the player" + JustThisModel, true))
                {
                    foreach (SoundEvent e in ModelPlayerEvents) DrawModelEvent(sounds, e);
                }
                EndFoldout();
            }
            else
            {
                EditorGUILayout.Space(8);
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("🧙 This model isn't the player in this level, so the player sounds are hidden.", EditorStyles.miniLabel);
                    if (GUILayout.Button(new GUIContent("Show them", "Use this if the model is the player in a DIFFERENT level"), EditorStyles.miniButton, GUILayout.Width(80)))
                    {
                        foldouts["modelPlayerAnyway"] = true;
                    }
                }
            }

            if (BeginFoldout("modelPickup", "⭐ When it's a pickup or bounce ball" + JustThisModel, true))
            {
                foreach (SoundEvent e in ModelPickupEvents) DrawModelEvent(sounds, e);
            }
            EndFoldout();
        }
    }

    /// <summary>
    /// True when this model is the player in the open level, is the starting player for new levels,
    /// or already has player sounds (so they never disappear from view).
    /// </summary>
    private static bool IsPlayerModel(GameSounds sounds, GameObject model)
    {
        if (model == null) return false;
        if (GameSettings.Load().defaultPlayerModel == model) return true;
        if (sounds.sounds.Any(s => s != null && s.onlyModel == model && s.clip != null && ModelPlayerEvents.Contains(s.when))) return true;

        foreach (PlayerController player in UnityEngine.Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
        {
            foreach (CustomModel custom in player.GetComponentsInChildren<CustomModel>(true))
            {
                if (custom.model == model) return true;
            }
        }
        return false;
    }

    /// <summary>
    /// One animation: a sound slot, plus (for extra animations) when the game plays it.
    /// slot = "Walk", "Idle, Walk"... for the animations the game plays itself; null for extra ones.
    /// </summary>
    private void DrawAnimationRow(GameSounds sounds, ref ModelSounds ms, AnimationClip clip, string slot)
    {
        bool extra = slot == null;
        AnimationSound a = ms?.animations.FirstOrDefault(x => x != null && x.animation == clip);
        AnimationMoment when = extra ? (a != null && a.playWhen != AnimationMoment.Normal ? a.playWhen : AnimationMoment.Never) : AnimationMoment.Normal;

        EditorGUILayout.Space(2);
        EditorGUI.BeginChangeCheck();
        AudioClip sound;
        float volume = a != null ? a.volume : 0.8f;
        bool repeat = a != null ? a.repeatEachLoop : (slot ?? "").Contains("Walk") || (slot ?? "").Contains("Run");
        bool vary = a == null || a.varyPitch;

        if (extra)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(clip.name, GUILayout.Width(130));
                int index = System.Array.IndexOf(AnimationMoments.Choices, when);
                index = EditorGUILayout.Popup(Mathf.Max(0, index), AnimationMoments.Choices.Select(AnimationMoments.Label).ToArray());
                when = AnimationMoments.Choices[index];
            }
        }
        else
        {
            GUILayout.Label(clip.name + "  (plays when: " + slot + ")");
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            sound = (AudioClip)EditorGUILayout.ObjectField(a?.sound, typeof(AudioClip), false, GUILayout.Height(20));
            GUI.enabled = a != null && a.sound != null;
            if (GUILayout.Button(new GUIContent("▶", "Play it"), GUILayout.Width(28))) SoundTools.Preview(a.sound);
            if (GUILayout.Button(new GUIContent("■", "Stop it"), GUILayout.Width(28))) SoundTools.StopPreview();
            GUI.enabled = true;
        }
        if (a != null && a.sound != null)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                volume = EditorGUILayout.Slider("Volume", a.volume, 0f, 1f);
                if (!extra) repeat = GUILayout.Toggle(a.repeatEachLoop, new GUIContent(" Every loop", "Play again each time the animation repeats (footsteps, flapping). Off = only when it starts."), GUILayout.Width(90));
                vary = GUILayout.Toggle(a.varyPitch, new GUIContent(" Vary pitch"), GUILayout.Width(85));
            }
            if (extra && when == AnimationMoment.Never)
                EditorGUILayout.LabelField("   ↑ Choose when it plays, or this sound is never heard.", EditorStyles.miniBoldLabel);
        }

        if (!EditorGUI.EndChangeCheck()) return;

        Undo.RecordObject(sounds, "Change animation sound");
        bool keep = sound != null || (extra && when != AnimationMoment.Never);
        if (!keep)
        {
            if (a != null && ms != null) ms.animations.Remove(a);
            if (ms != null && ms.animations.Count == 0) sounds.modelSounds.Remove(ms);
        }
        else
        {
            if (ms == null) { ms = new ModelSounds { model = soundModel }; sounds.modelSounds.Add(ms); }
            if (a == null)
            {
                a = new AnimationSound { animation = clip };
                ms.animations.Add(a);
            }
            a.sound = sound;
            a.volume = volume;
            a.repeatEachLoop = repeat;
            a.varyPitch = vary;
            a.playWhen = when;
        }
        SoundsChanged(sounds);
        GUIUtility.ExitGUI();
    }

    private void DrawModelEvent(GameSounds sounds, SoundEvent e)
    {
        SoundEntry entry = sounds.sounds.FirstOrDefault(s => s != null && s.when == e && s.onlyModel == soundModel);
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUI.BeginChangeCheck();
            AudioClip clip = (AudioClip)EditorGUILayout.ObjectField(new GUIContent(ShortModelLabel(e), SoundEntry.Label(e) + ". Empty = use the general sound."), entry?.clip, typeof(AudioClip), false);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(sounds, "Change model sound");
                if (clip == null) { if (entry != null) sounds.sounds.Remove(entry); }
                else
                {
                    if (entry == null)
                    {
                        entry = new SoundEntry(e) { onlyModel = soundModel, forEverything = true };
                        sounds.sounds.Add(entry);
                    }
                    entry.clip = clip;
                }
                SoundsChanged(sounds);
                GUIUtility.ExitGUI();
            }
            GUI.enabled = entry != null && entry.clip != null;
            if (GUILayout.Button(new GUIContent("▶", "Play it"), GUILayout.Width(28))) SoundTools.Preview(entry.clip);
            if (GUILayout.Button(new GUIContent("■", "Stop it"), GUILayout.Width(28))) SoundTools.StopPreview();
            GUI.enabled = true;
        }
    }

    private static string ShortModelLabel(SoundEvent e)
    {
        switch (e)
        {
            case SoundEvent.EnemyAttack: return "👹 It attacks you";
            case SoundEvent.EnemyDamage: return "💢 It gets hit";
            case SoundEvent.EnemyDeath: return "☠ It is beaten";
            case SoundEvent.Attack: return "⚔ You jump on it";
            case SoundEvent.Jump: return "⤴ Jump";
            case SoundEvent.Damage: return "🤕 Gets hurt";
            case SoundEvent.FallDamage: return "🪂 Lands too hard";
            case SoundEvent.PlayerDeath: return "💀 Dies";
            case SoundEvent.PickUp: return "⭐ You walk into it";
            case SoundEvent.Bounce: return "🏀 You bounce off it";
            default: return e.ToString();
        }
    }

    /// <summary>Every model file in Assets/MyModels (not animation-only files).</summary>
    private static List<GameObject> FindMyModels()
    {
        List<GameObject> found = new List<GameObject>();
        if (!AssetDatabase.IsValidFolder("Assets/MyModels")) return found;
        foreach (string guid in AssetDatabase.FindAssets("t:GameObject", new[] { "Assets/MyModels" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string ext = System.IO.Path.GetExtension(path).ToLowerInvariant();
            if (ext != ".fbx" && ext != ".glb" && ext != ".gltf" && ext != ".obj" && ext != ".prefab") continue;
            GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go != null && go.GetComponentInChildren<Renderer>(true) != null && !found.Contains(go)) found.Add(go);
        }
        return found.OrderBy(g => g.name).ToList();
    }
}
