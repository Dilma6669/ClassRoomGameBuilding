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
    private List<AnimationClip> cachedClips = new List<AnimationClip>();
    private Dictionary<AnimationClip, string> cachedUsedFor = new Dictionary<AnimationClip, string>();

    private static readonly SoundEvent[] ModelCreatureEvents = { SoundEvent.EnemyAttack, SoundEvent.EnemyDamage, SoundEvent.EnemyDeath, SoundEvent.Attack };
    private static readonly SoundEvent[] ModelPlayerEvents = { SoundEvent.Jump, SoundEvent.Damage, SoundEvent.FallDamage, SoundEvent.PlayerDeath };

    private bool Foldout(string key, string label, bool startOpen)
    {
        if (!foldouts.ContainsKey(key)) foldouts[key] = startOpen;
        return Foldout(key, label);
    }

    private void DrawModelSounds(GameSounds sounds)
    {
        EditorGUILayout.HelpBox("Pick a model (from Assets/MyModels or the Library) to see its animations. Drag a sound onto an animation and it plays every time that animation plays, on the player or on a creature.\nA model's own sound beats the general sound. Anything left empty uses the general sound.", MessageType.None);

        if (myModels == null) myModels = FindMyModels();

        // Models that already have sounds, as quick buttons
        List<GameObject> withSounds = sounds.modelSounds.Where(m => m != null && m.model != null && m.animations.Any(a => a.sound != null)).Select(m => m.model)
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

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
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
                cachedClips = CustomModel.ClipsForModel(soundModel, out cachedUsedFor);
            }

            GUILayout.Label("🎬 Its animations", EditorStyles.boldLabel);
            if (cachedClips.Count == 0)
            {
                EditorGUILayout.HelpBox("This model has no animations, so only the sounds below can be set.", MessageType.None);
            }

            ModelSounds ms = sounds.modelSounds.FirstOrDefault(m => m != null && m.model == soundModel);
            bool showUnused = false;
            bool unusedHeaderDrawn = false;
            foreach (AnimationClip clip in cachedClips)
            {
                AnimationSound a = ms?.animations.FirstOrDefault(x => x != null && x.animation == clip);
                string used = cachedUsedFor.TryGetValue(clip, out string slot) ? "  (plays when: " + slot + ")" : "";

                // Animations the game never plays go in a fold-out (clips are sorted: used ones first)
                if (!cachedUsedFor.ContainsKey(clip))
                {
                    if (!unusedHeaderDrawn)
                    {
                        unusedHeaderDrawn = true;
                        showUnused = Foldout("unusedAnims", "Its other animations (the game doesn't play these yet)", false);
                    }
                    if (!showUnused && (a == null || a.sound == null)) continue;
                }

                EditorGUILayout.Space(2);
                GUILayout.Label(clip.name + used, cachedUsedFor.ContainsKey(clip) ? EditorStyles.label : EditorStyles.miniLabel);
                EditorGUI.BeginChangeCheck();
                AudioClip sound;
                float volume = a != null ? a.volume : 0.8f;
                bool repeat = a == null || a.repeatEachLoop;
                bool vary = a == null || a.varyPitch;
                using (new EditorGUILayout.HorizontalScope())
                {
                    sound = (AudioClip)EditorGUILayout.ObjectField(a?.sound, typeof(AudioClip), false, GUILayout.Height(20));
                    GUI.enabled = a != null && a.sound != null;
                    if (GUILayout.Button("▶", GUILayout.Width(28))) SoundTools.Preview(a.sound);
                    GUI.enabled = true;
                }
                if (a != null && a.sound != null)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        volume = EditorGUILayout.Slider("Volume", a.volume, 0f, 1f);
                        repeat = GUILayout.Toggle(a.repeatEachLoop, new GUIContent(" Every loop", "Play again each time the animation repeats (footsteps, flapping). Off = only when it starts."), GUILayout.Width(90));
                        vary = GUILayout.Toggle(a.varyPitch, new GUIContent(" Vary pitch"), GUILayout.Width(85));
                    }
                }
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(sounds, "Change animation sound");
                    if (ms == null) { ms = new ModelSounds { model = soundModel }; sounds.modelSounds.Add(ms); }
                    if (sound == null)
                    {
                        if (a != null) ms.animations.Remove(a);
                        if (ms.animations.Count == 0) sounds.modelSounds.Remove(ms);
                    }
                    else
                    {
                        if (a == null)
                        {
                            a = new AnimationSound { animation = clip, repeatEachLoop = (slot ?? "").Contains("Walk") || (slot ?? "").Contains("Run") };
                            ms.animations.Add(a);
                        }
                        a.sound = sound;
                        a.volume = volume;
                        a.repeatEachLoop = repeat;
                        a.varyPitch = vary;
                    }
                    SoundsChanged(sounds);
                    GUIUtility.ExitGUI();
                }
            }

            // ---- Its own versions of the general sounds
            EditorGUILayout.Space(8);
            GUILayout.Label("👹 When it's a creature", EditorStyles.boldLabel);
            foreach (SoundEvent e in ModelCreatureEvents) DrawModelEvent(sounds, e);
            EditorGUILayout.Space(4);
            GUILayout.Label("🧙 When it's the player", EditorStyles.boldLabel);
            foreach (SoundEvent e in ModelPlayerEvents) DrawModelEvent(sounds, e);
        }
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
            if (GUILayout.Button("▶", GUILayout.Width(28))) SoundTools.Preview(entry.clip);
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
