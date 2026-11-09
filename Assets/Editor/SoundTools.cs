using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor helpers for the sounds: makes Assets/Resources/GameSounds.asset, plays previews,
/// and can make a set of simple starter sounds and two starter songs (made from code, so free to use).
/// </summary>
public static class SoundTools
{
    public const string AssetPath = "Assets/Resources/GameSounds.asset";
    public const string MySoundsFolder = "Assets/MySounds";
    public const string StarterFolder = "Assets/MySounds/Starter Sounds";
    public const string StarterMusicFolder = "Assets/MySounds/Starter Music";

    [MenuItem("Game/🔊 Sounds and Music", priority = 2)]
    public static void OpenSoundsTab()
    {
        GameBuilderWindow.OpenTab(GameBuilderWindow.SoundsTabIndex);
    }

    /// <summary>Loads (or makes) the sounds asset, with one slot for every sound event.</summary>
    public static GameSounds EnsureAsset()
    {
        GameSounds sounds = AssetDatabase.LoadAssetAtPath<GameSounds>(AssetPath);
        if (sounds == null)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
            sounds = ScriptableObject.CreateInstance<GameSounds>();
            AssetDatabase.CreateAsset(sounds, AssetPath);
        }
        if (!AssetDatabase.IsValidFolder(MySoundsFolder))
        {
            AssetDatabase.CreateFolder("Assets", "MySounds");
            File.WriteAllText(MySoundsFolder + "/HOW TO ADD SOUNDS.txt",
                "Put your sound files here (.wav, .mp3 or .ogg).\n\n" +
                "Then open Game > Game Builder > Sounds and drag a file onto a slot.\n" +
                "Songs go in the Music list at the bottom of the Sounds tab.\n\n" +
                "Free sounds: make your own, or use sites with free (CC0) sounds.\n");
            AssetDatabase.ImportAsset(MySoundsFolder + "/HOW TO ADD SOUNDS.txt");
        }

        bool changed = false;
        foreach (SoundEvent e in Enum.GetValues(typeof(SoundEvent)))
        {
            if (!sounds.sounds.Exists(s => s != null && s.when == e && s.onlyModel == null))
            {
                sounds.sounds.Add(new SoundEntry(e));
                changed = true;
            }
        }
        if (changed)
        {
            EditorUtility.SetDirty(sounds);
            AssetDatabase.SaveAssetIfDirty(sounds);
        }
        return sounds;
    }

    // ------------------------------------------------------------------ Previews (Unity has no public API for this)

    private static MethodInfo playPreview, stopPreviews;

    public static void Preview(AudioClip clip)
    {
        if (clip == null) return;
        StopPreview();
        if (playPreview == null)
        {
            Type audioUtil = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");
            if (audioUtil == null) return;
            foreach (MethodInfo m in audioUtil.GetMethods(BindingFlags.Static | BindingFlags.Public))
            {
                if (m.Name == "PlayPreviewClip") playPreview = m;
                if (m.Name == "StopAllPreviewClips") stopPreviews = m;
            }
        }
        if (playPreview == null) return;

        ParameterInfo[] ps = playPreview.GetParameters();
        object[] args = new object[ps.Length];
        args[0] = clip;
        for (int i = 1; i < ps.Length; i++)
        {
            args[i] = ps[i].HasDefaultValue ? ps[i].DefaultValue : (ps[i].ParameterType.IsValueType ? Activator.CreateInstance(ps[i].ParameterType) : null);
        }
        try { playPreview.Invoke(null, args); } catch (Exception) { /* preview only */ }
    }

    public static void StopPreview()
    {
        if (stopPreviews == null) return;
        try { stopPreviews.Invoke(null, null); } catch (Exception) { }
    }

    // ------------------------------------------------------------------ Starter sounds

    /// <summary>Makes the starter sounds (if missing) and puts them in every EMPTY slot. Returns how many slots were filled.</summary>
    public static int FillEmptySlotsWithStarterSounds(GameSounds sounds, bool music)
    {
        MakeStarterSounds();
        Undo.RecordObject(sounds, "Add starter sounds");
        int filled = 0;
        foreach (SoundEntry entry in sounds.sounds)
        {
            if (entry == null || entry.clip != null) continue;
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(StarterFolder + "/" + StarterName(entry.when) + ".wav");
            if (clip != null) { entry.clip = clip; filled++; }
        }
        if (music && sounds.soundtrack.TrueForAll(c => c == null))
        {
            sounds.soundtrack.Clear();
            foreach (string song in new[] { "Sunny Hills", "Cave Echoes" })
            {
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(StarterMusicFolder + "/" + song + ".wav");
                if (clip != null) { sounds.soundtrack.Add(clip); filled++; }
            }
        }
        EditorUtility.SetDirty(sounds);
        AssetDatabase.SaveAssetIfDirty(sounds);
        return filled;
    }

    private static string StarterName(SoundEvent e)
    {
        switch (e)
        {
            case SoundEvent.Attack: return "Attack";
            case SoundEvent.EnemyAttack: return "Enemy Attack";
            case SoundEvent.Run: return "Footstep";
            case SoundEvent.Jump: return "Jump";
            case SoundEvent.Sprint: return "Sprint Step";
            case SoundEvent.Damage: return "Damage";
            case SoundEvent.FallDamage: return "Fall Damage";
            case SoundEvent.EnemyDamage: return "Enemy Damage";
            case SoundEvent.PlayerDeath: return "Player Death";
            case SoundEvent.EnemyDeath: return "Enemy Death";
            case SoundEvent.PickUp: return "Pick Up";
            case SoundEvent.Bounce: return "Bounce";
            case SoundEvent.CreatureMove: return "Creature Step";
            default: return e.ToString();
        }
    }

    private const int Rate = 22050;
    private static System.Random noise = new System.Random(7);

    public static void MakeStarterSounds()
    {
        EnsureFolder(StarterFolder);
        EnsureFolder(StarterMusicFolder);
        var made = new List<string>();

        void Make(string folder, string name, float seconds, Func<float, float> wave)
        {
            string path = folder + "/" + name + ".wav";
            if (File.Exists(path)) return;
            WriteWav(path, seconds, wave);
            made.Add(path);
        }

        Make(StarterFolder, "Jump", 0.2f, t => Square(Sweep(t, 0.2f, 300, 750)) * Fade(t, 0.2f) * 0.35f);
        Make(StarterFolder, "Footstep", 0.07f, t => Noise() * Fade(t, 0.07f) * 0.35f);
        Make(StarterFolder, "Sprint Step", 0.05f, t => Noise() * Fade(t, 0.05f) * 0.4f);
        Make(StarterFolder, "Creature Step", 0.12f, t => (Sine(Sweep(t, 0.12f, 140, 70)) * 0.6f + Noise() * 0.25f) * Fade(t, 0.12f) * 0.5f);
        Make(StarterFolder, "Attack", 0.16f, t => (Sine(Sweep(t, 0.16f, 520, 140)) * 0.6f + Noise() * Fade(t, 0.03f) * 0.4f) * Fade(t, 0.16f) * 0.6f);
        Make(StarterFolder, "Enemy Attack", 0.35f, t => (Saw(110 + 15 * Mathf.Sin(t * 60f), t) * 0.5f + Noise() * 0.3f) * Fade(t, 0.35f) * 0.5f);
        Make(StarterFolder, "Damage", 0.22f, t => Square(Sweep(t, 0.22f, 420, 180)) * (0.6f + 0.4f * Mathf.Sin(t * 180f)) * Fade(t, 0.22f) * 0.3f);
        Make(StarterFolder, "Fall Damage", 0.32f, t => (Sine(Sweep(t, 0.32f, 130, 45)) * 0.8f + Noise() * Fade(t, 0.06f) * 0.5f) * Fade(t, 0.32f) * 0.7f);
        Make(StarterFolder, "Enemy Damage", 0.18f, t => Square(t < 0.08f ? 620 * t : 380 * t) * Fade(t, 0.18f) * 0.3f);
        Make(StarterFolder, "Player Death", 0.9f, t =>
        {
            float[] notes = { 523, 440, 349, 262 };
            int n = Mathf.Min(3, (int)(t / 0.22f));
            float local = t - n * 0.22f;
            return Square(notes[n] * t) * Fade(local, n == 3 ? 0.24f : 0.22f) * 0.3f;
        });
        Make(StarterFolder, "Enemy Death", 0.4f, t => (Sine(Sweep(t, 0.4f, 800, 90)) * 0.5f + Noise() * Fade(t, 0.15f) * 0.4f) * Fade(t, 0.4f) * 0.6f);
        Make(StarterFolder, "Pick Up", 0.28f, t => Square((t < 0.08f ? 988f : 1319f) * t) * Fade(t, 0.28f) * 0.25f);
        Make(StarterFolder, "Bounce", 0.3f, t => Sine(Sweep(t, 0.3f, 180, 820) + 0f) * (0.7f + 0.3f * Mathf.Sin(t * 90f)) * Fade(t, 0.3f) * 0.6f);

        // Two little songs (they loop nicely). Notes are semitones above A3 (220 Hz); -99 = rest.
        Make(StarterMusicFolder, "Sunny Hills", 16f, t => Song(t, 0.25f,
            new[] { 7, 11, 14, 11, 12, 11, 9, 7, 9, 12, 16, 12, 14, 12, 11, 9, 7, 11, 14, 19, 18, 16, 14, 12, 11, 12, 14, 11, 9, 7, 7, -99 },
            new[] { -14, -14, -9, -9, -11, -11, -16, -14 }));
        Make(StarterMusicFolder, "Cave Echoes", 19.2f, t => Song(t, 0.3f,
            new[] { 0, 3, 7, 3, 8, 7, 3, 2, 0, 3, 7, 10, 8, 7, 5, 3, 5, 8, 12, 8, 7, 3, 2, -2, 0, -99, 0, 3, 2, -2, 0, -99 },
            new[] { -12, -12, -16, -16, -19, -19, -17, -17 }));

        foreach (string path in made) AssetDatabase.ImportAsset(path);
        foreach (string path in made)
        {
            // Long songs stream from disk instead of filling memory
            if (path.StartsWith(StarterMusicFolder) && AssetImporter.GetAtPath(path) is AudioImporter importer)
            {
                AudioImporterSampleSettings s = importer.defaultSampleSettings;
                s.loadType = AudioClipLoadType.Streaming;
                importer.defaultSampleSettings = s;
                importer.SaveAndReimport();
            }
        }
    }

    private static float Song(float t, float beat, int[] melody, int[] bass)
    {
        int step = (int)(t / beat);
        float inNote = (t - step * beat) / beat;
        int note = melody[step % melody.Length];
        float lead = note == -99 ? 0f : Square(Freq(note) * t) * (1f - inNote * 0.8f) * 0.12f;

        int bar = (int)(t / (beat * 4));
        float bassFreq = Freq(bass[bar % bass.Length]);
        float bassWave = Triangle(bassFreq * t) * 0.22f;

        float hat = step % 2 == 1 && inNote < 0.15f ? Noise() * 0.05f * (1f - inNote / 0.15f) : 0f;
        float kick = step % 4 == 0 && inNote < 0.25f ? Sine(Mathf.Lerp(120f, 50f, inNote * 4f) * t) * 0.3f * (1f - inNote * 4f) : 0f;
        return lead + bassWave + hat + kick;
    }

    private static float Freq(int semitonesFromA3) => 220f * Mathf.Pow(2f, semitonesFromA3 / 12f);
    private static float Sweep(float t, float length, float from, float to) => (from * t) + (to - from) * t * t / (2f * length); // phase of a rising/falling pitch
    private static float Fade(float t, float length) => Mathf.Clamp01(1f - t / length) * Mathf.Clamp01(t * 400f);
    private static float Sine(float cycles) => Mathf.Sin(cycles * 2f * Mathf.PI);
    private static float Square(float cycles) => (cycles - Mathf.Floor(cycles)) < 0.5f ? 1f : -1f;
    private static float Triangle(float cycles) { float p = cycles - Mathf.Floor(cycles); return 4f * Mathf.Abs(p - 0.5f) - 1f; }
    private static float Saw(float freq, float t) { float p = freq * t; return 2f * (p - Mathf.Floor(p)) - 1f; }
    private static float Noise() => (float)(noise.NextDouble() * 2.0 - 1.0);

    private static void WriteWav(string path, float seconds, Func<float, float> wave)
    {
        int count = Mathf.CeilToInt(seconds * Rate);
        using (var stream = new FileStream(path, FileMode.Create))
        using (var w = new BinaryWriter(stream))
        {
            w.Write(new[] { 'R', 'I', 'F', 'F' });
            w.Write(36 + count * 2);
            w.Write(new[] { 'W', 'A', 'V', 'E', 'f', 'm', 't', ' ' });
            w.Write(16);
            w.Write((short)1);       // PCM
            w.Write((short)1);       // mono
            w.Write(Rate);
            w.Write(Rate * 2);       // bytes per second
            w.Write((short)2);       // block align
            w.Write((short)16);      // bits
            w.Write(new[] { 'd', 'a', 't', 'a' });
            w.Write(count * 2);
            for (int i = 0; i < count; i++)
            {
                float v = Mathf.Clamp(wave(i / (float)Rate), -1f, 1f);
                w.Write((short)(v * 32000f));
            }
        }
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
