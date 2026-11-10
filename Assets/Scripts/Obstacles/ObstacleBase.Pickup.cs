using UnityEngine;

/// <summary>
/// Pickup message (pickups only): a big message saying what you picked up ("+25 Health!").
/// Leave the text empty for the automatic one, or type your own.
/// </summary>
public abstract partial class ObstacleBase
{
    [Tooltip("Show a message on the screen saying what you picked up.")]
    public bool showPickupMessage = true;
    [Tooltip("Your own message. Leave it empty for the automatic one (e.g. '+25 Health!').")]
    public string pickupMessage = "";

    [Tooltip("Speed Boost: sprinting doesn't use up stamina while it lasts.")]
    public bool speedBoostFreeSprint = true;
    [Tooltip("Super Jump: falling doesn't hurt while it lasts (a fall that starts during it is safe too).")]
    public bool superJumpNoFallDamage = true;

    /// <summary>The message shown when it's picked up: your own text, or the automatic one.</summary>
    public string PickupMessageText => string.IsNullOrWhiteSpace(pickupMessage) ? AutomaticPickupMessage() : pickupMessage;

    /// <summary>What the pickup does, in a few words.</summary>
    public string AutomaticPickupMessage()
    {
        string seconds = Mathf.RoundToInt(buffDuration) + " seconds";
        switch (payloadType)
        {
            case PayloadType.HealthBooster: return "+" + payloadAmount + " Health!";
            case PayloadType.StaminaBooster: return "Stamina refilled!";
            case PayloadType.InvincibilityBuff: return "Shield! Nothing can hurt you for " + seconds;
            case PayloadType.JumpBuff: return "Super Jump for " + seconds + "!";
            case PayloadType.SprintBuff: return "Speed Boost for " + seconds + "!";
        }
        return "";
    }
}
