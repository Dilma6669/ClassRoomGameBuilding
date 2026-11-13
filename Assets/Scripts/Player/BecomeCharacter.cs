using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Added to the player (by code) when they touch a "Become a character" pickup: swaps the player's model,
/// animations and chosen settings for a while, shows a countdown, then puts everything back.
/// Touching another one first puts the normal player back, then becomes the new character.
/// Dying restarts the level, which brings the normal player back anyway.
/// </summary>
public class BecomeCharacter : MonoBehaviour
{
    private static BecomeCharacter active;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => active = null; // Play can start without a domain reload

    /// <summary>A 'become' character that is invincible is on right now.</summary>
    public static bool InvincibleNow => active != null && active.settings.invincible;
    /// <summary>A 'become' character with no fall damage is on right now.</summary>
    public static bool NoFallDamageNow => active != null && active.settings.noFallDamage;

    /// <summary>How many jumps on its head a creature needs right now (a character can make it fewer, never below 1).</summary>
    public static int JumpsNeeded(int normal)
    {
        if (active == null || normal <= 0) return normal;
        int amount = Mathf.Max(1, active.settings.creatureHitsAmount);
        switch (active.settings.creatureHits)
        {
            case CreatureHitsChange.TakeAway: return Mathf.Max(1, normal - amount);
            case CreatureHitsChange.DivideBy: return Mathf.Max(1, Mathf.CeilToInt(normal / (float)amount));
        }
        return normal;
    }

    private BecomeSettings settings;
    private float timeLeft;
    private PlayerLogic logic;
    private Health health;
    private CustomModel model;

    // The normal player, to put back afterwards
    private CustomModel.Look normalLook;
    private float moveSpeed, sprintMultiplier, jumpHeight, airSteerSpeed, maxStamina, staminaDrainRate;
    private int maxHealth;

    private TextMeshProUGUI countdown;

    /// <summary>Turns the player into the pickup's character for 'seconds' (or for ever).</summary>
    public static void Begin(PlayerLogic player, BecomeSettings becomeSettings, float seconds)
    {
        if (player == null || becomeSettings == null) return;
        BecomeCharacter b = player.GetComponent<BecomeCharacter>();
        if (b == null) b = player.gameObject.AddComponent<BecomeCharacter>();
        if (b.settings != null) b.PutBack(); // already someone: back to normal first
        b.Become(player, becomeSettings, seconds);
    }

    private void Become(PlayerLogic player, BecomeSettings s, float seconds)
    {
        settings = s;
        timeLeft = seconds;
        logic = player;
        health = player.GetComponentInChildren<Health>(true);
        model = health != null ? health.GetComponent<CustomModel>() : player.GetComponentInChildren<CustomModel>();

        // Remember the normal player
        if (model != null) normalLook = model.GetLook();
        moveSpeed = logic.moveSpeed; sprintMultiplier = logic.sprintMultiplier; jumpHeight = logic.jumpHeight;
        airSteerSpeed = logic.airSteerSpeed; maxStamina = logic.maxStamina; staminaDrainRate = logic.staminaDrainRate;
        if (health != null) maxHealth = health.MaxHealth;

        // Become the character
        if (model != null && s.look != null && s.look.model != null) model.SetLook(s.look);
        if (s.changeSpeed) logic.moveSpeed = s.moveSpeed;
        if (s.changeSprint) logic.sprintMultiplier = s.sprintMultiplier;
        if (s.changeJump) logic.jumpHeight = s.jumpHeight;
        if (s.changeAirSteer) logic.airSteerSpeed = s.airSteerSpeed;
        if (s.changeStaminaUse) logic.staminaDrainRate = s.staminaDrainRate;
        if (s.changeStamina) { logic.maxStamina = s.maxStamina; logic.RestoreStamina(s.maxStamina); }
        if (s.changeHealth && health != null) health.SetMaxHealth(s.maxHealth, true);
        logic.SettingsChanged();

        active = this;
        if (s.showCountdown && !s.forever) ShowCountdown();
        else HideCountdown();
    }

    /// <summary>Back to the normal player.</summary>
    private void PutBack()
    {
        if (settings == null) return;
        BecomeSettings s = settings;
        settings = null;
        if (active == this) active = null;

        if (model != null && normalLook != null && s.look != null && s.look.model != null) model.SetLook(normalLook);
        if (logic != null)
        {
            logic.moveSpeed = moveSpeed; logic.sprintMultiplier = sprintMultiplier; logic.jumpHeight = jumpHeight;
            logic.airSteerSpeed = airSteerSpeed; logic.staminaDrainRate = staminaDrainRate;
            if (s.changeStamina) { logic.maxStamina = maxStamina; logic.RestoreStamina(0f); } // keeps it within the normal most
            logic.SettingsChanged();
        }
        if (s.changeHealth && health != null) health.SetMaxHealth(maxHealth, false);
        HideCountdown();
    }

    private void Update()
    {
        if (settings == null || settings.forever) return;
        timeLeft -= Time.deltaTime;
        if (countdown != null) UpdateCountdown();
        if (timeLeft > 0f) return;

        string who = settings.CharacterName;
        PutBack();
        PickupMessage.Show("You're back to normal!", Color.white);
        SoundManager.Play(SoundEvent.PickUp, transform.position, gameObject);
        if (model != null) Sparkles.Burst(model.transform.position + Vector3.up, Sparkles.ColourFor(ObstacleBase.PayloadType.BecomeCharacter), Sparkles.Style.Poof, 1f, 1f);
    }

    private void OnDestroy()
    {
        if (active == this) active = null;
        if (countdown != null) Destroy(countdown.transform.parent.gameObject);
    }

    // ------------------------------------------------------------------ Countdown on the screen

    private void ShowCountdown()
    {
        if (countdown == null) countdown = CreateCountdown();
        countdown.transform.parent.gameObject.SetActive(true);
        UpdateCountdown();
    }

    private void HideCountdown()
    {
        if (countdown != null) countdown.transform.parent.gameObject.SetActive(false);
    }

    private void UpdateCountdown()
    {
        int secs = Mathf.CeilToInt(Mathf.Max(0f, timeLeft));
        countdown.text = settings.CharacterName + "   " + (secs / 60) + ":" + (secs % 60).ToString("00");
        // The last 5 seconds flash, so you know it's nearly over
        bool nearlyOver = timeLeft < 5f;
        Color c = Sparkles.ColourFor(ObstacleBase.PayloadType.BecomeCharacter);
        if (nearlyOver && Mathf.Repeat(timeLeft * 2f, 1f) < 0.5f) c = Color.white;
        countdown.color = c;
    }

    private static TextMeshProUGUI CreateCountdown()
    {
        GameObject go = new GameObject("Character Countdown (auto)");
        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 440; // above the game and HUD, below the pickup message, menus and videos
        CanvasScaler scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject label = new GameObject("Text");
        label.transform.SetParent(go.transform, false);
        TextMeshProUGUI tmp = label.AddComponent<TextMeshProUGUI>();
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 54f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.raycastTarget = false;
        tmp.outlineWidth = 0.25f;
        tmp.outlineColor = new Color32(0, 0, 0, 200);
        GameSettings gs = GameSettings.Load();
        if (gs != null && gs.bodyFont != null) tmp.font = gs.bodyFont;

        RectTransform rect = tmp.rectTransform;
        rect.anchorMin = new Vector2(0.25f, 0.88f);
        rect.anchorMax = new Vector2(0.75f, 0.97f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return tmp;
    }
}
