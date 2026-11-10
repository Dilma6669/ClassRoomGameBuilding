using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>The 🔊 Sounds tab of the Game Builder: sound effects and the music list.</summary>
public partial class GameBuilderWindow
{
    private double soundsSaveAt;
    [System.NonSerialized] private GUIStyle soundHeaderStyle;

    // The order the moments are listed in the tab
    private static readonly SoundEvent[] TabOrder =
    {
        SoundEvent.Run, SoundEvent.Sprint, SoundEvent.Jump, SoundEvent.Damage, SoundEvent.FallDamage, SoundEvent.PlayerDeath,
        SoundEvent.PickUp, SoundEvent.Attack, SoundEvent.Bounce,
        SoundEvent.CreatureMove, SoundEvent.EnemyAttack, SoundEvent.EnemyDamage, SoundEvent.EnemyDeath
    };

    /// <summary>Opens the Game Builder on a tab (e.g. from the Game menu).</summary>
    public static void OpenTab(int tabIndex)
    {
        EditorPrefs.SetInt(TabKey, tabIndex);
        Open();
        GameBuilderWindow window = GetWindow<GameBuilderWindow>();
        window.tab = (Tab)tabIndex;
        window.Repaint();
    }

    private void Update()
    {
        // Save the sounds a moment after the last change (not on every slider move)
        if (soundsSaveAt > 0 && EditorApplication.timeSinceStartup > soundsSaveAt)
        {
            soundsSaveAt = 0;
            GameSounds sounds = AssetDatabase.LoadAssetAtPath<GameSounds>(SoundTools.AssetPath);
            if (sounds != null) AssetDatabase.SaveAssetIfDirty(sounds);
        }
    }

    private void SoundsChanged(GameSounds sounds)
    {
        EditorUtility.SetDirty(sounds);
        soundsSaveAt = EditorApplication.timeSinceStartup + 1.0;
    }

    private void DrawSoundsTab()
    {
        Title("🔊 Sounds & Music", "Drag a sound file (.wav, .mp3 or .ogg) from the Project window onto a slot. Keep your sounds in the Assets/MySounds folder.\nThese sounds are used in EVERY level. In the game, players press Esc > Sound & Music to change the volume and pick songs.");

        GameSounds sounds = SoundTools.EnsureAsset();
        if (soundHeaderStyle == null)
        {
            soundHeaderStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 13, fontStyle = FontStyle.Bold };
        }

        // Two buttons exactly the same size, lined up like every other button grid
        int soundButton = ButtonGrid(2, new[] { "📁 Show the MySounds folder", "🎵 Fill empty slots with starter sounds" },
            new Color?[] { null, new Color(0.6f, 0.85f, 1f) });
        {
            if (soundButton == 0)
            {
                UnityEngine.Object folder = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(SoundTools.MySoundsFolder);
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = folder;
                EditorGUIUtility.PingObject(folder);
            }
            if (soundButton == 1)
            {
                int filled = SoundTools.FillEmptySlotsWithStarterSounds(sounds, true);
                ShowNotification(new GUIContent(filled > 0 ? "Added " + filled + " starter sounds" : "No empty slots to fill"));
                GUIUtility.ExitGUI();
            }
        }

        // ---------------- General sound effects (the defaults)
        Section("🔊 General sounds (used by everything, unless a model has its own)");
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Each moment can have more than one sound. Click '➕ Another sound' to:\n• pick a different sound for some creatures or pickups (untick 'For every...'), or\n• add variety: when several sounds fit, one is picked at random.\nTo give ONE model its own sounds (a dragon's roar, a horse's hooves), use 🎭 Sounds for one model further down.", EditorStyles.wordWrappedMiniLabel);

            string[] groupTitles = { "🧙 Player", "🎯 Touching things (pickups, jumping on creatures, bouncing)", "👹 Creatures (enemies)" };
            foreach (SoundEntry.Group group in Enum.GetValues(typeof(SoundEntry.Group)))
            {
                if (BeginFoldout("sounds." + group, groupTitles[(int)group], true))
                {
                    foreach (SoundEvent e in TabOrder)
                    {
                        if (SoundEntry.GroupOf(e) == group) DrawSoundEvent(sounds, e);
                    }
                    // Same gap under the last sound as above the first one (6 + the box's 4)
                    EditorGUILayout.Space(6);
                }
                EndFoldout();
            }
            EditorGUILayout.Space(4);
        }

        // ---------------- One model
        Section("🎭 Sounds for one model");
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            DrawModelSounds(sounds);
        }

        // ---------------- Music
        Section("📀 Music (soundtrack)");
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            DrawMusic(sounds);
        }

        Section("🔉 Starting volumes");
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("How loud things are the first time someone plays. Players can change it in the game (Esc > Sound & Music).", EditorStyles.wordWrappedMiniLabel);
            EditorGUI.BeginChangeCheck();
            float musicVol = EditorGUILayout.Slider("Music", sounds.startMusicVolume, 0f, 1f);
            float fxVol = EditorGUILayout.Slider("Sound effects", sounds.startEffectsVolume, 0f, 1f);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(sounds, "Change volume");
                sounds.startMusicVolume = musicVol;
                sounds.startEffectsVolume = fxVol;
                SoundsChanged(sounds);
            }
            if (GUILayout.Button("Forget the volumes I picked while testing (use the starting volumes)"))
            {
                PlayerPrefs.DeleteKey("Sound.MusicVolume");
                PlayerPrefs.DeleteKey("Sound.EffectsVolume");
                PlayerPrefs.DeleteKey("Sound.MusicOrder");
                PlayerPrefs.DeleteKey("Sound.RepeatSong");
                ShowNotification(new GUIContent("Done"));
            }
        }
    }

    private void DrawSoundEvent(GameSounds sounds, SoundEvent e)
    {
        List<SoundEntry> entries = sounds.sounds.Where(s => s != null && s.when == e && s.onlyModel == null).ToList();
        SoundTarget allowed = SoundEntry.TargetsFor(e);
        bool canLimit = allowed != SoundTarget.Nothing;
        bool creatures = (allowed & SoundTarget.Wanderers) != 0;

        EditorGUILayout.Space(6);
        // Slightly narrower than the General sounds box, so it sits inside it
        using (new EditorGUILayout.VerticalScope(new GUIStyle(EditorStyles.helpBox) { margin = new RectOffset(10, 10, 4, 4) }))
        {
            GUILayout.Label(SoundEntry.Label(e), soundHeaderStyle);

            SoundEntry remove = null;
            for (int i = 0; i < entries.Count; i++)
            {
                SoundEntry entry = entries[i];
                if (i > 0) EditorGUILayout.Space(4);

                EditorGUI.BeginChangeCheck();
                AudioClip clip;
                float volume;
                bool vary;
                float repeat = entry.repeatEvery;
                bool forEverything = entry.forEverything;
                SoundTarget onlyFor = entry.onlyFor;
                GameObject onlyModel = entry.onlyModel;

                using (new EditorGUILayout.HorizontalScope())
                {
                    clip = (AudioClip)EditorGUILayout.ObjectField(entry.clip, typeof(AudioClip), false, GUILayout.Height(22));
                    GUI.enabled = entry.clip != null;
                    if (GUILayout.Button(new GUIContent("▶", "Play it"), GUILayout.Width(30), GUILayout.Height(22))) SoundTools.Preview(entry.clip);
                    if (GUILayout.Button(new GUIContent("■", "Stop it"), GUILayout.Width(30), GUILayout.Height(22))) SoundTools.StopPreview();
                    GUI.enabled = true;
                    if (GUILayout.Button(new GUIContent("✕", entries.Count > 1 ? "Remove this sound" : "Empty this slot"), GUILayout.Width(30), GUILayout.Height(22))) remove = entry;
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    volume = EditorGUILayout.Slider("Volume", entry.volume, 0f, 1f);
                    vary = GUILayout.Toggle(entry.varyPitch, new GUIContent(" Vary pitch", "Change the pitch a little each time so it doesn't sound robotic."), GUILayout.Width(90));
                }

                if (e == SoundEvent.Run || e == SoundEvent.Sprint)
                {
                    repeat = EditorGUILayout.Slider(new GUIContent("Step every (seconds)", "How often the step sound plays. A long sound (a running loop) just repeats when it ends."), entry.repeatEvery, 0.1f, 2f);
                    if (e == SoundEvent.Sprint && entry.clip == null) EditorGUILayout.LabelField("   (empty: the Run sound plays faster instead)", EditorStyles.miniLabel);
                }

                if (canLimit)
                {
                    forEverything = EditorGUILayout.ToggleLeft(creatures ? "For every creature" : "For everything", entry.forEverything);
                    if (!forEverything)
                    {
                        EditorGUILayout.LabelField("Only for:", EditorStyles.miniBoldLabel);
                        onlyFor = DrawTargetToggles(entry.onlyFor, allowed);
                        if (onlyFor == SoundTarget.Nothing) EditorGUILayout.HelpBox("Tick at least one, or this sound never plays.", MessageType.Warning);
                    }
                }

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(sounds, "Change sound");
                    entry.clip = clip;
                    entry.volume = volume;
                    entry.varyPitch = vary;
                    entry.repeatEvery = repeat;
                    entry.forEverything = forEverything;
                    entry.onlyFor = onlyFor;
                    entry.onlyModel = onlyModel;
                    SoundsChanged(sounds);
                }
            }

            if (remove != null)
            {
                Undo.RecordObject(sounds, "Remove sound");
                if (entries.Count > 1) sounds.sounds.Remove(remove);
                else remove.clip = null;
                SoundsChanged(sounds);
                GUIUtility.ExitGUI();
            }

            string addLabel = canLimit ? "➕ Another sound (for other creatures, or for variety)" : "➕ Another sound (one is picked at random)";
            if (e == SoundEvent.PickUp) addLabel = "➕ Another sound (for other pickups, or for variety)";
            if (e == SoundEvent.Bounce) addLabel = "➕ Another sound (balls vs platforms, or for variety)";
            if (GUILayout.Button(addLabel))
            {
                Undo.RecordObject(sounds, "Add sound");
                SoundEntry added = new SoundEntry(e);
                if (canLimit) { added.forEverything = false; added.onlyFor = SoundTarget.Nothing; }
                int lastIndex = sounds.sounds.LastIndexOf(entries[entries.Count - 1]);
                sounds.sounds.Insert(lastIndex + 1, added);
                SoundsChanged(sounds);
                GUIUtility.ExitGUI();
            }
        }
    }

    private static SoundTarget DrawTargetToggles(SoundTarget value, SoundTarget allowed)
    {
        SoundTarget result = value & allowed;
        List<SoundTarget> options = Enum.GetValues(typeof(SoundTarget)).Cast<SoundTarget>()
            .Where(t => t != SoundTarget.Nothing && (allowed & t) != 0).ToList();

        for (int i = 0; i < options.Count; i += 3)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                for (int j = i; j < Mathf.Min(i + 3, options.Count); j++)
                {
                    SoundTarget t = options[j];
                    bool on = GUILayout.Toggle((result & t) != 0, " " + SoundEntry.TargetLabel(t), GUILayout.MinWidth(110));
                    result = on ? result | t : result & ~t;
                }
                GUILayout.FlexibleSpace();
            }
        }
        return result;
    }

    private void DrawMusic(GameSounds sounds)
    {
        EditorGUI.BeginChangeCheck();
        bool play = EditorGUILayout.ToggleLeft("Play music", sounds.playMusic);
        GameSounds.MusicOrder order = (GameSounds.MusicOrder)EditorGUILayout.Popup("Order", (int)sounds.musicOrder, new[] { "In order", "Shuffle", "Repeat one song" });
        bool inMenus = EditorGUILayout.ToggleLeft("Keep playing while a menu is open", sounds.musicInMenus);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(sounds, "Change music");
            sounds.playMusic = play;
            sounds.musicOrder = order;
            sounds.musicInMenus = inMenus;
            SoundsChanged(sounds);
        }

        EditorGUILayout.Space(4);
        int removeAt = -1, moveFrom = -1, moveTo = -1;
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            if (sounds.soundtrack.Count == 0) EditorGUILayout.LabelField("No songs yet. Drag songs into the box below, or click '➕ Add a song'.", EditorStyles.wordWrappedMiniLabel);
            for (int i = 0; i < sounds.soundtrack.Count; i++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label((i + 1) + ".", GUILayout.Width(22));
                    EditorGUI.BeginChangeCheck();
                    AudioClip clip = (AudioClip)EditorGUILayout.ObjectField(sounds.soundtrack[i], typeof(AudioClip), false, GUILayout.Height(20));
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(sounds, "Change song");
                        sounds.soundtrack[i] = clip;
                        SoundsChanged(sounds);
                    }
                    GUI.enabled = sounds.soundtrack[i] != null;
                    if (GUILayout.Button(new GUIContent("▶", "Play it"), GUILayout.Width(28))) SoundTools.Preview(sounds.soundtrack[i]);
                    if (GUILayout.Button(new GUIContent("■", "Stop it"), GUILayout.Width(28))) SoundTools.StopPreview();
                    GUI.enabled = i > 0;
                    if (GUILayout.Button("▲", GUILayout.Width(26))) { moveFrom = i; moveTo = i - 1; }
                    GUI.enabled = i < sounds.soundtrack.Count - 1;
                    if (GUILayout.Button("▼", GUILayout.Width(26))) { moveFrom = i; moveTo = i + 1; }
                    GUI.enabled = true;
                    if (GUILayout.Button("✕", GUILayout.Width(26))) removeAt = i;
                }
            }
        }

        if (removeAt >= 0 || moveFrom >= 0)
        {
            Undo.RecordObject(sounds, "Change music list");
            if (removeAt >= 0) sounds.soundtrack.RemoveAt(removeAt);
            else
            {
                AudioClip c = sounds.soundtrack[moveFrom];
                sounds.soundtrack.RemoveAt(moveFrom);
                sounds.soundtrack.Insert(moveTo, c);
            }
            SoundsChanged(sounds);
            GUIUtility.ExitGUI();
        }

        // Drop zone: drag one or many songs at once
        Rect drop = GUILayoutUtility.GetRect(0, 46, GUILayout.ExpandWidth(true));
        GUI.Box(drop, "⬇  Drag songs here to add them to the list  ⬇", new GUIStyle(EditorStyles.helpBox) { alignment = TextAnchor.MiddleCenter, fontSize = 12 });
        Event ev = Event.current;
        if ((ev.type == EventType.DragUpdated || ev.type == EventType.DragPerform) && drop.Contains(ev.mousePosition))
        {
            List<AudioClip> clips = DragAndDrop.objectReferences.OfType<AudioClip>().ToList();
            DragAndDrop.visualMode = clips.Count > 0 ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;
            if (ev.type == EventType.DragPerform && clips.Count > 0)
            {
                DragAndDrop.AcceptDrag();
                Undo.RecordObject(sounds, "Add songs");
                sounds.soundtrack.AddRange(clips);
                SoundsChanged(sounds);
            }
            ev.Use();
        }

        if (GUILayout.Button("➕ Add a song (empty slot)"))
        {
            Undo.RecordObject(sounds, "Add song");
            sounds.soundtrack.Add(null);
            SoundsChanged(sounds);
        }
    }
}
