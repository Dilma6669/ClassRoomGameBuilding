using UnityEngine;
#if HAS_KINEMATIC_CC
using KinematicCharacterController;
#endif

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
            case PayloadType.BecomeCharacter:
                string who = become != null ? become.CharacterName : "someone else";
                string becomeMessage = become != null && become.forever ? "You are " + who + "!" : "You are " + who + " for " + seconds + "!";
                if (become != null && become.canFly) becomeMessage += "\nJump, then hold Space to fly up, Ctrl to come down!";
                return becomeMessage;
        }
        return "";
    }
}

/// <summary>
/// Pickups that disappear can come back after a while, and 'Become a character' pickups can be made
/// the same size as the character you turn into.
/// </summary>
public abstract partial class ObstacleBase
{
    [Tooltip("After it disappears, it comes back after this many seconds. 0 = gone for good.")]
    [Range(0f, 600f)] public float comeBackAfter = 0f;

    /// <summary>Used up: vanish for good, or hide for a while and come back.</summary>
    private void UseUp()
    {
        if (comeBackAfter > 0f) StartCoroutine(HideThenComeBack());
        else Destroy(gameObject);
    }

    private bool hidden;

    private System.Collections.IEnumerator HideThenComeBack()
    {
        if (hidden) yield break;
        hidden = true;
        var renderers = new System.Collections.Generic.List<Renderer>();
        var colliders = new System.Collections.Generic.List<Collider>();
        var lights = new System.Collections.Generic.List<Light>();
        foreach (Renderer r in GetComponentsInChildren<Renderer>()) if (r.enabled) { r.enabled = false; renderers.Add(r); }
        foreach (Collider c in GetComponentsInChildren<Collider>()) if (c.enabled) { c.enabled = false; colliders.Add(c); }
        foreach (Light l in GetComponentsInChildren<Light>()) if (l.enabled) { l.enabled = false; lights.Add(l); }
        var effects = new System.Collections.Generic.List<AmbientSparkles>();
        foreach (AmbientSparkles a in GetComponentsInChildren<AmbientSparkles>()) if (a.enabled) { a.enabled = false; effects.Add(a); } // its sparkles live outside it
        yield return new WaitForSeconds(comeBackAfter);
        foreach (AmbientSparkles a in effects) if (a != null) a.enabled = true;
        foreach (Renderer r in renderers) if (r != null) r.enabled = true;
        foreach (Collider c in colliders) if (c != null) c.enabled = true;
        foreach (Light l in lights) if (l != null) l.enabled = true;
        if (sparkles) BurstSparkles(Sparkles.Style.Pickup);
        hidden = false;
    }

#if UNITY_EDITOR
    /// <summary>
    /// Become a character pickups with '📏 Pickup is as big as the character': makes the pickup's model as tall as the
    /// character will be (the player is 2 m tall, times the character's Size). Runs in the Editor now and then.
    /// </summary>
    public void FitToCharacterSize() => KeepSameSizeAsCharacter();

    private void KeepSameSizeAsCharacter()
    {
        if (payloadType != PayloadType.BecomeCharacter || become == null || !become.pickupSameSize || become.look == null || become.look.model == null) return;
        CustomModel cm = GetComponentInChildren<CustomModel>();
        if (cm == null || cm.ModelInstance == null) return;
        bool found = false;
        Bounds b = new Bounds();
        foreach (Renderer r in cm.ModelInstance.GetComponentsInChildren<Renderer>())
        {
            if (!r.enabled || r is ParticleSystemRenderer) continue;
            if (!found) { b = r.bounds; found = true; } else b.Encapsulate(r.bounds);
        }
        if (!found || b.size.y < 0.01f) return;
        float playerHeight = 2f;
#if HAS_KINEMATIC_CC
        KinematicCharacterMotor player = FindAnyObjectByType<KinematicCharacterMotor>();
        if (player != null && player.Capsule != null) playerHeight = player.Capsule.height;
#endif
        float want = playerHeight * Mathf.Max(0.1f, become.look.size);
        float ratio = want / b.size.y;
        if (Mathf.Abs(ratio - 1f) < 0.02f) return;
        UnityEditor.Undo.RecordObject(this, "Pickup as big as the character");
        objectScale = Mathf.Clamp(objectScale * ratio, 0.1f, 200f);
        ApplyScale();
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}
