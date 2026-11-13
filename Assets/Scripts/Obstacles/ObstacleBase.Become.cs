using UnityEngine;

/// <summary>
/// "Become a character" pickups: touching one turns the player into another character (e.g. Roses Fox)
/// for a while, with its own animations and, if you like, different speed, jumping, health...
/// The countdown length is 'Power-up lasts' (buffDuration). See Scripts/Player/BecomeCharacter.cs.
/// </summary>
public abstract partial class ObstacleBase
{
    [Tooltip("Become a character pickups: who you turn into, and what changes while you are them.")]
    public BecomeSettings become = new BecomeSettings();
}

public enum CreatureHitsChange { NoChange, TakeAway, DivideBy }

[System.Serializable]
public class BecomeSettings
{
    [Tooltip("Who you turn into: the model and its animations.")]
    public CustomModel.Look look = new CustomModel.Look();

    [Tooltip("Ticked: you stay this character until the level restarts (no countdown).")]
    public bool forever = false;
    [Tooltip("Show the countdown on the screen.")]
    public bool showCountdown = true;

    // Each change: ticked = use this value while you are the character, then it goes back to normal
    public bool changeSpeed; public float moveSpeed = 10f;
    public bool changeSprint; public float sprintMultiplier = 1.8f;
    public bool changeJump; public float jumpHeight = 10f;
    public bool changeAirSteer; public float airSteerSpeed = 15f;
    public bool changeStamina; public float maxStamina = 100f;
    public bool changeStaminaUse; public float staminaDrainRate = 12.5f;
    public bool changeHealth; public int maxHealth = 100;
    [Tooltip("Nothing can hurt you while you are the character.")]
    public bool invincible;
    [Tooltip("Falling never hurts while you are the character.")]
    public bool noFallDamage;

    [Tooltip("Flying characters (dragons...): jump to take off, then hold Space to go up and Ctrl to come down. Let go to hover.")]
    public bool canFly;
    public float flyUpSpeed = 10f;
    public float flyDownSpeed = 10f;
    [Tooltip("Flying: the character tips its nose up when going up (Space) and down when coming down (Ctrl), by this many degrees. 0 = stays level.")]
    public float flyTilt = 20f;
    [Tooltip("Flying characters: the camera sits this many metres higher, so the character is lower on the screen and you can see where you're going. 0 = normal.")]
    public float flyCameraLift = 3f;

    // 🏹 Shooting (left click) while you are the character. Shots fly the way the character faces
    // (a flying character tipping its nose up/down shoots up/down too). 10 damage = one jump on a creature's head.
    [Tooltip("Hold the left mouse button to shoot.")]
    public bool canShoot = false;
    [Range(1, 1000)] public int shootDamage = 10;
    [Tooltip("Seconds between shots while you hold the button: a random time between the shortest and longest (the same = steady).")]
    [Range(0.03f, 10f)] public float shootEveryMin = 0.3f;
    [Range(0.03f, 10f)] public float shootEveryMax = 0.3f;
    [Range(1f, 300f)] public float shootSpeed = 30f;
    [Range(1, 12)] public int shotsAtOnce = 1;
    [Range(0f, 60f)] public float shootSpread = 10f;
    [Range(0.1f, 20f)] public float shootSize = 0.6f;
    public Color shootColour = new Color(1f, 0.85f, 0.1f);
    [Tooltip("Shoot a model instead of a glowing ball. Empty = a glowing ball.")]
    public GameObject shootModel;
    public bool shootTrail = true;
    public Sparkles.Effect shootHitEffect = Sparkles.Effect.Explosion;
    [Range(0.5f, 30f)] public float shootLasts = 3f;
    [Tooltip("The shots curve towards the nearest creature in front of you. 0 = straight.")]
    [Range(0f, 1f)] public float shootHoming = 0f;
    [Tooltip("The shots fall like a thrown ball.")]
    public bool shootDrops = false;
    [Tooltip("Aim up (+) or down (-) a little, in degrees.")]
    [Range(-45f, 45f)] public float shootAimUpDown = 0f;
    [Tooltip("The animation when you shoot. Empty = its attack animation (if it has one).")]
    public AnimationClip shootAnimation;
    [Range(0f, 3f)] public float shootDelay = 0f;
    [Tooltip("Where shots come out: 0 = its feet, 1 = the top of its head.")]
    [Range(0f, 1f)] public float shootFromHeight = 0.6f;

    public enum ShootBarKind { StaminaBar, OwnBar, NoLimit }
    [Tooltip("Stamina: each shot uses stamina, so you juggle sprinting and shooting. Own bar: a yellow bar at the top fills with each shot; when it's full you must wait for it to go down. No limit: shoot for ever.")]
    public ShootBarKind shootUses = ShootBarKind.StaminaBar;
    [Tooltip("How full the shooting bar can get (bigger = more shots before it's full).")]
    [Range(1f, 1000f)] public float shootBarSize = 100f;
    [Tooltip("How much each shot fills the bar (or uses stamina).")]
    [Range(0f, 500f)] public float shootBarPerShot = 10f;
    [Tooltip("How fast the bar goes back down each second when you're not shooting.")]
    [Range(1f, 500f)] public float shootBarRefill = 30f;
    [Tooltip("Seconds after shooting before the bar starts going down.")]
    [Range(0f, 5f)] public float shootBarWait = 0.5f;
    [Tooltip("When the bar is full you can shoot again once it's down to this % full.")]
    [Range(0f, 95f)] public float shootBarReadyAt = 30f;

    [Tooltip("The sparkles when you turn into the character. None = no sparkles.")]
    public Sparkles.Effect transformEffect = Sparkles.Effect.Explosion;
    [Tooltip("Pick your own colour for the turning-into sparkles (otherwise pink).")]
    public bool transformCustomColour = false;
    public Color transformColour = new Color(1f, 0.5f, 0.9f);
    [Range(0.2f, 20f)] public float transformAmount = 3f;
    [Range(0.3f, 10f)] public float transformSize = 2f;
    [Tooltip("The new character pops in: it grows from tiny to full size.")]
    public bool transformGrow = true;
    [Tooltip("The new character spins round once as it appears.")]
    public bool transformSpin = true;
    [Tooltip("How long the pop-in and spin take (seconds).")]
    [Range(0.1f, 3f)] public float transformTime = 0.6f;
    [Tooltip("The same effect when you turn back to normal.")]
    public bool transformEffectWhenBack = true;

    /// <summary>The turning-into sparkle colour.</summary>
    public Color TransformColour => transformCustomColour ? transformColour : Sparkles.ColourFor(ObstacleBase.PayloadType.BecomeCharacter);

    [Tooltip("The pickup is made the same size as the character you turn into (so you can see how big you'll be).")]
    public bool pickupSameSize = false;

    [Tooltip("Creatures need fewer jumps on the head to be beaten while you are the character.")]
    public CreatureHitsChange creatureHits = CreatureHitsChange.NoChange;
    [Range(1, 100)] public int creatureHitsAmount = 1;

    /// <summary>The character's name, from the model file ("Roses Fox - Rigged & Animated (C)" -> "Roses Fox").</summary>
    public string CharacterName
    {
        get
        {
            if (look == null || look.model == null) return "someone else";
            string n = look.model.name;
            int dash = n.IndexOf(" - ");
            if (dash > 0) n = n.Substring(0, dash);
            if (n.EndsWith("-model")) n = n.Substring(0, n.Length - 6);
            return n.Trim();
        }
    }
}
