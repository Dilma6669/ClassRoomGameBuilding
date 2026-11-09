using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// All the game's sound effects and music, in one place for every level.
/// Edit it in Game Builder > 🔊 Sounds (or Game > Sounds and Music).
/// The asset lives at Assets/Resources/GameSounds.asset so it is included in builds.
/// </summary>
[CreateAssetMenu(menuName = "Game/Game Sounds", fileName = "GameSounds")]
public class GameSounds : ScriptableObject
{
    public const string ResourcePath = "GameSounds";

    [Tooltip("Every sound effect. One event can have several sounds: e.g. a different one for each creature, or a few that are picked at random.")]
    public List<SoundEntry> sounds = new List<SoundEntry>();

    [Tooltip("Sounds for one model's animations. A model's own sound beats the general sound; empty slots use the general sound.")]
    public List<ModelSounds> modelSounds = new List<ModelSounds>();

    [Header("Music")]
    [Tooltip("Songs that play in the background. Add or remove as many as you like.")]
    public List<AudioClip> soundtrack = new List<AudioClip>();
    public bool playMusic = true;
    public MusicOrder musicOrder = MusicOrder.InOrder;
    [Tooltip("Keep the music playing while a menu is open.")]
    public bool musicInMenus = true;

    [Header("Starting volumes (players can change them in the Esc menu > Sound)")]
    [Range(0f, 1f)] public float startMusicVolume = 0.5f;
    [Range(0f, 1f)] public float startEffectsVolume = 0.8f;

    public enum MusicOrder { InOrder, Shuffle, RepeatOneSong }

    private static GameSounds cached;

    /// <summary>Loads the sounds asset, or an empty one if it's missing (the game stays silent).</summary>
    public static GameSounds Load()
    {
        if (cached != null) return cached;
        cached = Resources.Load<GameSounds>(ResourcePath);
        if (cached == null) cached = CreateInstance<GameSounds>();
        return cached;
    }
}

/// <summary>The moments in the game that can make a sound. Add new ones at the END only.</summary>
public enum SoundEvent
{
    Attack,
    EnemyAttack,
    Run,
    Jump,
    Sprint,
    Damage,
    FallDamage,
    EnemyDamage,
    PlayerDeath,
    EnemyDeath,
    PickUp,
    Bounce,
    CreatureMove
}

/// <summary>Kinds of things a sound can be limited to.</summary>
[System.Flags]
public enum SoundTarget
{
    Nothing = 0,
    Wanderers = 1 << 0,
    Patrollers = 1 << 1,
    Guards = 1 << 2,
    HealthPickups = 1 << 3,
    StaminaPickups = 1 << 4,
    ShieldPickups = 1 << 5,
    SuperJumpPickups = 1 << 6,
    SpeedBoostPickups = 1 << 7,
    BounceBalls = 1 << 8,
    BouncyPlatforms = 1 << 9
}

[System.Serializable]
public class SoundEntry
{
    public SoundEvent when = SoundEvent.Jump;
    public AudioClip clip;
    [Range(0f, 1f)] public float volume = 0.8f;
    [Tooltip("Change the pitch a little each time so it doesn't sound robotic.")]
    public bool varyPitch = true;

    [Tooltip("Plays for every kind of thing this event can happen to.")]
    public bool forEverything = true;
    [Tooltip("When 'For everything' is off: the kinds of things it plays for.")]
    public SoundTarget onlyFor = SoundTarget.Nothing;
    [Tooltip("Optional: only creatures using this 3D model (e.g. only spiders).")]
    public GameObject onlyModel;

    [Tooltip("Run and Sprint: seconds between steps. A long sound (a running loop) just repeats when it finishes.")]
    [Range(0.1f, 2f)] public float repeatEvery = 0.35f;

    public SoundEntry() { }
    public SoundEntry(SoundEvent when)
    {
        this.when = when;
        repeatEvery = when == SoundEvent.Sprint ? 0.25f : when == SoundEvent.CreatureMove ? 0.5f : 0.35f;
        if (when == SoundEvent.CreatureMove) volume = 0.5f;
    }

    /// <summary>Which group a moment belongs to in the Sounds tab.</summary>
    public enum Group { Player, Touching, Creatures }

    public static Group GroupOf(SoundEvent e)
    {
        switch (e)
        {
            case SoundEvent.EnemyAttack:
            case SoundEvent.EnemyDamage:
            case SoundEvent.EnemyDeath:
            case SoundEvent.CreatureMove:
                return Group.Creatures;
            case SoundEvent.Attack:
            case SoundEvent.PickUp:
            case SoundEvent.Bounce:
                return Group.Touching;
            default:
                return Group.Player;
        }
    }

    /// <summary>The kinds of things an event can be limited to (Nothing = only ever the player).</summary>
    public static SoundTarget TargetsFor(SoundEvent e)
    {
        switch (e)
        {
            case SoundEvent.Attack:
            case SoundEvent.EnemyAttack:
            case SoundEvent.EnemyDamage:
            case SoundEvent.EnemyDeath:
            case SoundEvent.CreatureMove:
                return SoundTarget.Wanderers | SoundTarget.Patrollers | SoundTarget.Guards;
            case SoundEvent.PickUp:
                return SoundTarget.HealthPickups | SoundTarget.StaminaPickups | SoundTarget.ShieldPickups | SoundTarget.SuperJumpPickups | SoundTarget.SpeedBoostPickups;
            case SoundEvent.Bounce:
                return SoundTarget.BounceBalls | SoundTarget.BouncyPlatforms;
            default:
                return SoundTarget.Nothing;
        }
    }

    public static string Label(SoundEvent e)
    {
        switch (e)
        {
            case SoundEvent.Attack: return "⚔ Jump on a creature (attack)";
            case SoundEvent.EnemyAttack: return "👹 Enemy attack (a creature hurts you)";
            case SoundEvent.Run: return "👟 Run (footsteps)";
            case SoundEvent.Jump: return "⤴ Jump";
            case SoundEvent.Sprint: return "💨 Sprint (footsteps while sprinting)";
            case SoundEvent.Damage: return "🤕 Damage (you get hurt)";
            case SoundEvent.FallDamage: return "🪂 Fall damage (you land too hard)";
            case SoundEvent.EnemyDamage: return "💢 Enemy damage (a creature gets hit)";
            case SoundEvent.PlayerDeath: return "💀 Player death";
            case SoundEvent.EnemyDeath: return "☠ Enemy death (a creature is beaten)";
            case SoundEvent.PickUp: return "⭐ Walk into a pickup (collect it)";
            case SoundEvent.Bounce: return "🏀 Bounce off a bounce ball or bouncy platform";
            case SoundEvent.CreatureMove: return "👣 Creature moving (footsteps, slithering...)";
            default: return e.ToString();
        }
    }

    public static string TargetLabel(SoundTarget t)
    {
        switch (t)
        {
            case SoundTarget.HealthPickups: return "Health";
            case SoundTarget.StaminaPickups: return "Stamina";
            case SoundTarget.ShieldPickups: return "Shield";
            case SoundTarget.SuperJumpPickups: return "Super Jump";
            case SoundTarget.SpeedBoostPickups: return "Speed Boost";
            case SoundTarget.BounceBalls: return "Bounce balls";
            case SoundTarget.BouncyPlatforms: return "Bouncy platforms";
            default: return t.ToString();
        }
    }
}

/// <summary>Sounds for ONE model: a sound for each of its animations, e.g. wings flapping or a dragon's roar.</summary>
[System.Serializable]
public class ModelSounds
{
    public GameObject model;
    public List<AnimationSound> animations = new List<AnimationSound>();
}

[System.Serializable]
public class AnimationSound
{
    public AnimationClip animation;
    public AudioClip sound;
    [Range(0f, 1f)] public float volume = 0.8f;
    public bool varyPitch = true;
    [Tooltip("Play again every time the animation loops (good for footsteps or flapping). Off = only when it starts.")]
    public bool repeatEachLoop = true;
    [Tooltip("For the model's EXTRA animations (bark, bite, death...): the moment the game plays it.")]
    public AnimationMoment playWhen = AnimationMoment.Normal;
}

/// <summary>When the game plays one of a model's extra animations. Add new ones at the END only.</summary>
public enum AnimationMoment
{
    Normal,        // idle / walk / run / jump / fall: the game picks these itself
    Never,
    NowAndThen,    // every few seconds while standing still
    AttacksYou,    // a creature touches the player
    GetsHit,       // jumped on, but not beaten yet
    IsBeaten,      // jumped on for the last time
    GetsHurt,      // the player takes damage
    LandsTooHard   // the player takes fall damage
}

public static class AnimationMoments
{
    public static readonly AnimationMoment[] Choices =
    {
        AnimationMoment.Never, AnimationMoment.NowAndThen, AnimationMoment.AttacksYou, AnimationMoment.GetsHit,
        AnimationMoment.IsBeaten, AnimationMoment.GetsHurt, AnimationMoment.LandsTooHard
    };

    public static string Label(AnimationMoment m)
    {
        switch (m)
        {
            case AnimationMoment.Never: return "Never (not used)";
            case AnimationMoment.NowAndThen: return "Now and then, while standing still";
            case AnimationMoment.AttacksYou: return "Creature: it attacks you";
            case AnimationMoment.GetsHit: return "Creature: you jump on it (gets hit)";
            case AnimationMoment.IsBeaten: return "Creature: it is beaten";
            case AnimationMoment.GetsHurt: return "Player: gets hurt";
            case AnimationMoment.LandsTooHard: return "Player: lands too hard";
            default: return m.ToString();
        }
    }

    /// <summary>The general sound this moment replaces when the model's animation has its own sound.</summary>
    public static SoundEvent? GeneralSound(AnimationMoment m)
    {
        switch (m)
        {
            case AnimationMoment.AttacksYou: return SoundEvent.EnemyAttack;
            case AnimationMoment.GetsHit: return SoundEvent.EnemyDamage;
            case AnimationMoment.IsBeaten: return SoundEvent.EnemyDeath;
            case AnimationMoment.GetsHurt: return SoundEvent.Damage;
            case AnimationMoment.LandsTooHard: return SoundEvent.FallDamage;
            default: return null;
        }
    }
}
