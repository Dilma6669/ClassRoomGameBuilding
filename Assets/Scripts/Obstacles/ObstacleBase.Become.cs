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
