using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

#if HAS_KINEMATIC_CC
using KinematicCharacterController;
#endif

/// <summary>
/// Plays the sound effects and the music from Game Builder > 🔊 Sounds.
/// It creates itself when the game starts and stays alive between levels, so music doesn't restart
/// when the player dies. Players change the volumes and songs in the Esc menu > Sound.
/// </summary>
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }
    public GameSounds Sounds { get; private set; }

    private const string MusicVolumeKey = "Sound.MusicVolume";
    private const string EffectsVolumeKey = "Sound.EffectsVolume";
    private const string OrderKey = "Sound.MusicOrder";

    private AudioSource music;
    private readonly List<AudioSource> pool = new List<AudioSource>();
    private int nextPoolIndex;

    private int currentTrack = -1;
    private bool userPausedMusic;
    private bool musicStarted;
    private readonly List<int> shuffleOrder = new List<int>();

    // ------------------------------------------------------------------ Startup

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => Instance = null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null) return;
        GameObject go = new GameObject("SoundManager (auto)");
        DontDestroyOnLoad(go);
        go.AddComponent<SoundManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Sounds = GameSounds.Load();

        music = gameObject.AddComponent<AudioSource>();
        music.playOnAwake = false;
        music.ignoreListenerPause = true; // menus pause the game's sounds, but not the music
        music.spatialBlend = 0f;
        music.priority = 0;

        for (int i = 0; i < 16; i++)
        {
            GameObject child = new GameObject("Sound " + i);
            child.transform.SetParent(transform, false);
            AudioSource source = child.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 4f;
            source.maxDistance = 45f;
            pool.Add(source);
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
        SetUpLevel();
    }

    private void OnDestroy()
    {
        if (Instance != this) return;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        Instance = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => SetUpLevel();

    private void SetUpLevel()
    {
        // Something has to "hear" the sounds: normally the player's camera has an Audio Listener
        if (FindAnyObjectByType<AudioListener>() == null)
        {
            Camera cam = Camera.main != null ? Camera.main : FindAnyObjectByType<Camera>();
            if (cam != null) cam.gameObject.AddComponent<AudioListener>();
        }

#if HAS_KINEMATIC_CC
        foreach (KinematicCharacterMotor motor in FindObjectsByType<KinematicCharacterMotor>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (motor.GetComponent<PlayerSounds>() == null) motor.gameObject.AddComponent<PlayerSounds>();
        }
#endif
    }

    // ------------------------------------------------------------------ Volumes

    public static float MusicVolume
    {
        get => PlayerPrefs.GetFloat(MusicVolumeKey, GameSounds.Load().startMusicVolume);
        set { PlayerPrefs.SetFloat(MusicVolumeKey, Mathf.Clamp01(value)); PlayerPrefs.Save(); }
    }

    public static float EffectsVolume
    {
        get => PlayerPrefs.GetFloat(EffectsVolumeKey, GameSounds.Load().startEffectsVolume);
        set { PlayerPrefs.SetFloat(EffectsVolumeKey, Mathf.Clamp01(value)); PlayerPrefs.Save(); }
    }

    // ------------------------------------------------------------------ Sound effects

    /// <summary>
    /// Plays the sound chosen for this moment. 'source' is the creature, pickup or platform involved
    /// (null for the player's own sounds), so sounds limited to certain things are picked correctly.
    /// </summary>
    public static void Play(SoundEvent e, Vector3 position, GameObject source = null)
    {
        if (Instance == null) return;
        SoundEntry entry = Instance.Pick(e, source);
        if (entry == null) return;

        bool fromPlayer = SoundEntry.TargetsFor(e) == SoundTarget.Nothing || e == SoundEvent.Attack;
        if (!fromPlayer && Instance.TooFarToHear(position)) return; // don't use up a sound slot for nothing
        // The death sound must keep playing when the death screen pauses everything
        Instance.PlayClip(entry, position, fromPlayer ? 0f : 0.75f, e == SoundEvent.PlayerDeath);
    }

    /// <summary>Is there any sound set for this moment? (Used by the player's footsteps.)</summary>
    public static SoundEntry Find(SoundEvent e, GameObject source = null)
    {
        return Instance != null ? Instance.Pick(e, source) : null;
    }

    private SoundEntry Pick(SoundEvent e, GameObject source)
    {
        SoundTarget target = TargetOf(source, out GameObject model);
        List<SoundEntry> best = new List<SoundEntry>();
        int bestScore = -1;

        foreach (SoundEntry entry in Sounds.sounds)
        {
            if (entry == null || entry.when != e || entry.clip == null) continue;

            int score = 0;
            if (!entry.forEverything)
            {
                if (target == SoundTarget.Nothing || (entry.onlyFor & target) == 0) continue;
                score = 1;
            }
            if (entry.onlyModel != null)
            {
                if (model != entry.onlyModel) continue;
                score += 2; // the most exact match wins
            }

            if (score > bestScore) { best.Clear(); bestScore = score; }
            if (score == bestScore) best.Add(entry);
        }

        return best.Count == 0 ? null : best[Random.Range(0, best.Count)];
    }

    /// <summary>What kind of thing made the sound, and its 3D model (if it has one).</summary>
    public static SoundTarget TargetOf(GameObject source, out GameObject model)
    {
        model = null;
        if (source == null) return SoundTarget.Nothing;

        CustomModel custom = source.GetComponentInChildren<CustomModel>();
        if (custom != null) model = custom.model;

        ObstacleBase obstacle = source.GetComponentInParent<ObstacleBase>();
        if (obstacle != null)
        {
            if (obstacle.isBouncy) return SoundTarget.BounceBalls;
            switch (obstacle.payloadType)
            {
                case ObstacleBase.PayloadType.HealthBooster: return SoundTarget.HealthPickups;
                case ObstacleBase.PayloadType.StaminaBooster: return SoundTarget.StaminaPickups;
                case ObstacleBase.PayloadType.InvincibilityBuff: return SoundTarget.ShieldPickups;
                case ObstacleBase.PayloadType.JumpBuff: return SoundTarget.SuperJumpPickups;
                case ObstacleBase.PayloadType.SprintBuff: return SoundTarget.SpeedBoostPickups;
            }
            IObstacleMovement movement = obstacle.GetComponent<IObstacleMovement>();
            string kind = movement != null ? movement.GetType().Name : "";
            if (kind.Contains("Wander")) return SoundTarget.Wanderers;
            if (kind.Contains("Patrol")) return SoundTarget.Patrollers;
            return SoundTarget.Guards;
        }

        if (source.GetComponentInParent<PlatformLogic>() != null) return SoundTarget.BouncyPlatforms;
        return SoundTarget.Nothing;
    }

    private void PlayClip(SoundEntry entry, Vector3 position, float spatial, bool ignorePause)
    {
        AudioSource source = pool[nextPoolIndex];
        nextPoolIndex = (nextPoolIndex + 1) % pool.Count;

        source.Stop();
        source.transform.position = position;
        source.spatialBlend = spatial;
        source.ignoreListenerPause = ignorePause;
        source.pitch = entry.varyPitch ? Random.Range(0.9f, 1.1f) : 1f;
        source.clip = entry.clip;
        source.volume = entry.volume * EffectsVolume;
        source.Play();
    }

    // ------------------------------------------------------------------ One model's animation sounds

    /// <summary>The sound set for this model's animation, or null (then the general sounds are used).</summary>
    public static AnimationSound FindAnimationSound(GameObject model, AnimationClip clip)
    {
        if (model == null || clip == null) return null;
        foreach (ModelSounds m in GameSounds.Load().modelSounds)
        {
            if (m == null || m.model != model) continue;
            foreach (AnimationSound a in m.animations)
                if (a != null && a.animation == clip && a.sound != null) return a;
        }
        return null;
    }

    private AudioListener listener;

    private bool TooFarToHear(Vector3 position)
    {
        if (listener == null || !listener.isActiveAndEnabled) listener = FindAnyObjectByType<AudioListener>();
        return listener != null && Vector3.Distance(listener.transform.position, position) > pool[0].maxDistance;
    }

    public static void PlayAnimationSound(AnimationSound sound, Vector3 position, bool isPlayer)
    {
        if (Instance == null || sound == null || sound.sound == null) return;
        if (!isPlayer && Instance.TooFarToHear(position)) return;
        SoundEntry asEntry = new SoundEntry { clip = sound.sound, volume = sound.volume, varyPitch = sound.varyPitch };
        Instance.PlayClip(asEntry, position, isPlayer ? 0f : 0.75f, false);
    }

    /// <summary>A short sound so players hear how loud effects are while moving the slider in the menu.</summary>
    public void PreviewEffects()
    {
        SoundEntry entry = Pick(SoundEvent.PickUp, null) ?? Pick(SoundEvent.Jump, null);
        if (entry == null)
        {
            foreach (SoundEntry any in Sounds.sounds) if (any != null && any.clip != null) { entry = any; break; }
        }
        if (entry != null) PlayClip(entry, Vector3.zero, 0f, true);
    }

    // ------------------------------------------------------------------ Music

    public int TrackCount => Sounds.soundtrack.Count;
    public int CurrentTrack => currentTrack;
    public bool IsMusicPaused => userPausedMusic || !music.isPlaying;

    public GameSounds.MusicOrder Order
    {
        get => (GameSounds.MusicOrder)PlayerPrefs.GetInt(OrderKey, (int)Sounds.musicOrder);
        set
        {
            PlayerPrefs.SetInt(OrderKey, (int)value);
            PlayerPrefs.Save();
            shuffleOrder.Clear();
            music.loop = value == GameSounds.MusicOrder.RepeatOneSong;
        }
    }

    public string TrackName(int index)
    {
        if (index < 0 || index >= TrackCount) return "";
        AudioClip clip = Sounds.soundtrack[index];
        return clip != null ? clip.name.Replace('_', ' ').Replace('-', ' ') : "(empty)";
    }

    public void PlayTrack(int index)
    {
        if (TrackCount == 0) return;
        index = ((index % TrackCount) + TrackCount) % TrackCount;
        AudioClip clip = Sounds.soundtrack[index];
        currentTrack = index;
        userPausedMusic = false;
        musicStarted = true;
        if (clip == null) return; // an empty slot: Update moves on to the next song
        music.clip = clip;
        music.loop = Order == GameSounds.MusicOrder.RepeatOneSong;
        music.time = 0f;
        music.Play();
    }

    public void NextTrack() => PlayTrack(NextIndex(+1));
    public void PreviousTrack()
    {
        // Like a music player: "back" restarts the song unless it has only just started
        if (music.isPlaying && music.time > 3f) PlayTrack(currentTrack);
        else PlayTrack(NextIndex(-1));
    }

    public void ToggleMusicPause()
    {
        if (TrackCount == 0) return;
        if (currentTrack < 0) { PlayTrack(FirstIndex()); return; }
        if (userPausedMusic) { userPausedMusic = false; music.UnPause(); }
        else { userPausedMusic = true; music.Pause(); }
    }

    private int FirstIndex()
    {
        if (Order != GameSounds.MusicOrder.Shuffle) return 0;
        return Random.Range(0, TrackCount);
    }

    private int NextIndex(int step)
    {
        if (TrackCount == 0) return 0;
        if (Order != GameSounds.MusicOrder.Shuffle) return currentTrack + step;

        // Shuffle: play every song once before any repeats
        if (shuffleOrder.Count != TrackCount)
        {
            shuffleOrder.Clear();
            for (int i = 0; i < TrackCount; i++) shuffleOrder.Add(i);
            for (int i = TrackCount - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (shuffleOrder[i], shuffleOrder[j]) = (shuffleOrder[j], shuffleOrder[i]);
            }
        }
        int at = shuffleOrder.IndexOf(currentTrack);
        return shuffleOrder[((at + step) % TrackCount + TrackCount) % TrackCount];
    }

    private void Update()
    {
        GameManager game = GameManager.Instance;
        bool inMenu = game != null && game.State != GameManager.GameState.Playing && game.State != GameManager.GameState.WatchingVideo;
        bool watchingVideo = game != null && game.State == GameManager.GameState.WatchingVideo;

        float volume = MusicVolume;
        if (watchingVideo) volume *= 0.15f; // let the video be heard
        if (inMenu && !Sounds.musicInMenus) volume = 0f;
        music.volume = volume;

        if (!Sounds.playMusic || TrackCount == 0)
        {
            if (music.isPlaying) music.Stop();
            return;
        }

        if (!musicStarted) { PlayTrack(FirstIndex()); return; }

        // The song ended (or the slot was empty): play the next one
        if (!userPausedMusic && !music.isPlaying) NextTrack();
    }
}
