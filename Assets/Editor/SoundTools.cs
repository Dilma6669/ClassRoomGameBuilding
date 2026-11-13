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
            foreach (string song in new[] { "Cave Echoes", "Shadow March", "Haunted Hollow", "Creeping Dread" })
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
        // Sunny Hills: C major, chords C G Am F | C F G C (one chord per bar, every tune note fits its chord)
        Make(StarterMusicFolder, "Sunny Hills", 16f, t => Song(t, 0.25f,
            new[] { 7, 10, 15, 10,   17, 14, 10, 14,   15, 12, 7, 12,   15, 12, 8, 12,
                    10, 15, 19, 15,  12, 15, 20, 15,   17, 14, 10, 14,  15, 10, 15, -99 },
            new[] { -9, -14, -12, -16, -9, -16, -14, -9 }));
        Make(StarterMusicFolder, "Cave Echoes", 19.2f, t => Song(t, 0.3f,
            new[] { 0, 3, 7, 3, 8, 7, 3, 2, 0, 3, 7, 10, 8, 7, 5, 3, 5, 8, 12, 8, 7, 3, 2, -2, 0, -99, 0, 3, 2, -2, 0, -99 },
            new[] { -12, -12, -16, -16, -19, -19, -17, -17 }));

        // Three darker songs (teacher, 2026-11-14: "the current ones are too happy"); minor keys, slow, deep drums, echoing bells
        Make(StarterMusicFolder, "Shadow March", 32f, ShadowMarch);
        Make(StarterMusicFolder, "Haunted Hollow", 38.4f, HauntedHollow);
        Make(StarterMusicFolder, "Creeping Dread", 25.6f, CreepingDread);

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

    // ------------------------------------------------------------------ Dark songs

    /// <summary>A soft pad: each note is two slightly out-of-tune sines (a slow, wobbly shimmer).</summary>
    private static float Pad(float t, int[] notes, float volume)
    {
        float v = 0f;
        foreach (int n in notes)
        {
            float f = Freq(n);
            v += Sine(f * t) * 0.6f + Sine(f * 1.004f * t) * 0.4f + Triangle(f * 0.5f * t) * 0.15f;
        }
        return v * volume / notes.Length;
    }

    /// <summary>A music-box / bell note that rings and fades (tn = seconds since it was struck).</summary>
    private static float Bell(float f, float tn)
    {
        if (tn < 0f) return 0f;
        return (Sine(f * tn) * Mathf.Exp(-2.5f * tn) + Sine(f * 2.76f * tn) * 0.3f * Mathf.Exp(-6f * tn)) * Mathf.Clamp01(tn * 300f);
    }

    /// <summary>The tune: a bell on each note (-99 = rest), plus a quieter echo of it 3 beats later.</summary>
    private static float BellTune(float t, float beat, int[] melody, int octave, float volume)
    {
        float v = 0f;
        for (int echo = 0; echo < 2; echo++)
        {
            float te = t - echo * beat * 0.75f;
            if (te < 0f) te += melody.Length * beat; // the echo of the end wraps round, so the loop is seamless
            int step = (int)(te / beat);
            float tn = te - step * beat;
            // let the previous note ring on too
            for (int back = 0; back < 3; back++)
            {
                int s = step - back;
                if (s < 0) s += melody.Length;
                int note = melody[s % melody.Length];
                if (note != -99) v += Bell(Freq(note + octave), tn + back * beat) * (echo == 0 ? 1f : 0.35f);
            }
        }
        return v * volume;
    }

    /// <summary>A deep drum: a falling sine thump (tn = seconds since it was hit).</summary>
    private static float Boom(float tn, float volume)
    {
        if (tn < 0f || tn > 0.6f) return 0f;
        return Sine(Mathf.Lerp(90f, 38f, Mathf.Clamp01(tn * 3f)) * tn) * Mathf.Exp(-6f * tn) * volume;
    }

    private static int[] Chord(params int[] n) => n;

    // D minor march: Dm Bb Gm A (harmonic minor), kick on 1 and 3, a soft snare on 4, pulsing low bass.
    private static float ShadowMarch(float t)
    {
        const float beat = 0.5f;
        int[][] chords = { Chord(-7, -4, 0), Chord(-11, -7, -4), Chord(-14, -11, -7), Chord(-12, -8, -5) };
        int bar = (int)(t / (beat * 4));
        int[] chord = chords[(bar / 2) % chords.Length];
        float inBar = t - bar * beat * 4;
        int beatInBar = (int)(inBar / beat);
        float inBeat = inBar - beatInBar * beat;

        float pad = Pad(t, chord, 0.09f);
        // bass: the chord's root two octaves down, pulsing in eighths
        float eighth = inBeat % (beat * 0.5f);
        float bass = (Triangle(Freq(chord[0] - 12) * t) * 0.8f + Square(Freq(chord[0] - 12) * t) * 0.08f) * Mathf.Exp(-4f * eighth) * 0.3f;
        float kick = beatInBar % 2 == 0 ? Boom(inBeat, 0.55f) : 0f;
        float snare = beatInBar == 3 && inBeat < 0.18f ? Noise() * 0.07f * (1f - inBeat / 0.18f) : 0f;
        int[] tune = { 12, -99, 10, 8, 7, -99, 5, -99, 3, 5, 7, -99, 8, 7, 5, 4,
                       5, -99, 3, 2, 0, -99, 2, 3, 5, -99, 4, -99, 0, -99, -99, -99,
                       12, -99, 13, 12, 10, -99, 8, 7, 5, -99, 7, 8, 7, -99, 4, -99,
                       5, 3, 2, -99, 0, 2, 3, -99, 2, -99, 1, -99, 2, -99, -99, -99 };
        float bells = BellTune(t, beat, tune, 0, 0.12f);
        return pad + bass + kick + snare + bells;
    }

    // A minor, slow and spooky: Am F Dm E pads, a music box with echoes, a heartbeat every bar, a low rumble.
    private static float HauntedHollow(float t)
    {
        const float beat = 0.6f;
        int[][] chords = { Chord(0, 3, 7), Chord(-4, 0, 3), Chord(-7, -4, 0), Chord(-5, -1, 2) };
        int bar = (int)(t / (beat * 4));
        int[] chord = chords[(bar / 2) % chords.Length];
        float inBar = t - bar * beat * 4;
        float swell = 0.75f + 0.25f * Mathf.Sin(t * 0.8f);
        float pad = Pad(t, chord, 0.1f) * swell;
        // heartbeat: two thumps at the start of each bar
        float heart = Boom(inBar, 0.4f) + Boom(inBar - 0.28f, 0.28f);
        float rumble = Sine(Freq(chord[0] - 24) * t) * 0.07f;
        int[] tune = { 12, -99, 15, -99, 14, 12, -99, 11, 12, -99, -99, -99, 7, -99, 8, -99,
                       8, -99, 12, -99, 15, 14, -99, 12, 10, -99, -99, -99, 8, 7, -99, 5,
                       5, -99, 8, -99, 12, -99, 10, 8, 7, -99, -99, -99, 3, -99, 5, -99,
                       7, -99, 11, -99, 14, -99, 12, 11, 7, -99, -99, -99, -99, -99, -99, -99 };
        float bells = BellTune(t, beat, tune, 12, 0.09f);
        return (pad + heart + rumble + bells) * 1.45f;
    }

    // E Phrygian creeping: a low E-F-E-F crawl, a tritone drone, a ticking clock, and eerie gliding whistles.
    private static float CreepingDread(float t)
    {
        const float beat = 0.4f;
        int step = (int)(t / beat);
        float inBeat = t - step * beat;
        int[] crawl = { -29, -28, -29, -28, -29, -28, -26, -28 };
        int bassNote = crawl[step % crawl.Length];
        float bass = (Triangle(Freq(bassNote) * t) + Sine(Freq(bassNote + 12) * t) * 0.3f) * Mathf.Exp(-2.5f * inBeat) * 0.33f;
        float drone = Pad(t, new[] { -5, 1 }, 0.06f) * (0.6f + 0.4f * Mathf.Sin(t * 0.5f)); // E and Bb: the 'devil's interval'
        float tick = inBeat < 0.012f ? Noise() * 0.12f : 0f;
        float tock = step % 4 == 0 ? Boom(inBeat, 0.35f) : 0f;
        // a whistle that slides down, once every 8 beats
        float whistleT = t % (beat * 8f);
        float whistle = whistleT > beat * 4f ? Sine((1320f - 180f * (whistleT - beat * 4f)) * (whistleT - beat * 4f)) * 0.035f
                                              * Mathf.Sin(Mathf.PI * (whistleT - beat * 4f) / (beat * 4f)) : 0f;
        int[] tune = { 7, -99, -99, 8, -99, -99, 7, -99, 5, -99, -99, 4, -99, -99, 2, -99,
                       7, -99, -99, 8, -99, -99, 11, -99, 10, -99, -99, 8, -99, 7, -99, -99,
                       4, -99, -99, 5, -99, -99, 4, -99, 1, -99, -99, 2, -99, -99, -1, -99,
                       -5, -99, -99, -4, -99, -99, -1, -99, -5, -99, -99, -99, -99, -99, -99, -99 };
        float bells = BellTune(t, beat, tune, 12, 0.07f);
        return bass + drone + tick + tock + whistle + bells;
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
