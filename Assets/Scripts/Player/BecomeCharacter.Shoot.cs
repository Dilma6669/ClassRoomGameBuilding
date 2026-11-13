using UnityEngine;
using UnityEngine.UI;
#if HAS_KINEMATIC_CC
using KinematicCharacterController.Examples;
#endif

/// <summary>
/// Shooting characters: while you are a 'become a character' with 🏹 Can shoot ticked, hold the LEFT mouse button to shoot
/// the way the character faces (a flying character tipped up or down shoots up or down too).
/// Each shot uses stamina (default), or fills its own yellow bar at the top of the screen, or nothing (no limit).
/// The shots are the same Projectile the creatures use; they hurt creatures (10 damage = one jump on the head).
/// </summary>
public partial class BecomeCharacter
{
    private float nextShotIn;      // seconds until the next shot is allowed
    private float fireIn = -1f;    // a shot waiting for its animation (Shot delay)
    private float barFill;         // own bar: how full it is
    private float barWait;         // own bar: seconds before it starts going down
    private bool barFull;          // own bar: got full, wait until it's down to 'ready at'
    private bool wasLocked;        // the mouse was already locked last frame (the click that locks it doesn't shoot)

    private RectTransform barFillRect;
    private Image barImage;
    private GameObject barRoot;

    private static readonly Color BarYellow = new Color(1f, 0.85f, 0.1f);
    private static readonly Color BarFullRed = new Color(1f, 0.25f, 0.15f);

    private void StartShooting(BecomeSettings s)
    {
        nextShotIn = 0f;
        fireIn = -1f;
        barFill = 0f; barWait = 0f; barFull = false;
        if (s.canShoot && s.shootUses == BecomeSettings.ShootBarKind.OwnBar) ShowBar();
        else HideBar();
    }

    private void StopShooting()
    {
        fireIn = -1f;
        HideBar();
    }

    /// <summary>Called every frame from Update (also for 'for ever' characters).</summary>
    private void UpdateShooting()
    {
        BecomeSettings s = settings;
        if (s == null || !s.canShoot) { wasLocked = Cursor.lockState == CursorLockMode.Locked; return; }
        float dt = Time.deltaTime;

        // Own bar goes back down when you're not shooting
        if (s.shootUses == BecomeSettings.ShootBarKind.OwnBar)
        {
            if (barWait > 0f) barWait -= dt;
            else barFill = Mathf.Max(0f, barFill - s.shootBarRefill * dt);
            if (barFull && barFill <= s.shootBarSize * s.shootBarReadyAt / 100f) barFull = false;
            UpdateBar(s);
        }

        if (nextShotIn > 0f) nextShotIn -= dt;
        if (fireIn >= 0f)
        {
            fireIn -= dt;
            if (fireIn < 0f) FireNow(s);
        }

        bool locked = Cursor.lockState == CursorLockMode.Locked;
        bool canPlay = dt > 0f && locked && wasLocked && (GameManager.Instance == null || GameManager.Instance.State == GameManager.GameState.Playing);
        wasLocked = locked;
        if (!canPlay || !Input.GetMouseButton(0) || nextShotIn > 0f || fireIn >= 0f) return;

        // Is there enough left?
        switch (s.shootUses)
        {
            case BecomeSettings.ShootBarKind.StaminaBar:
                if (logic == null || !logic.UseStamina(s.shootBarPerShot)) return;
                break;
            case BecomeSettings.ShootBarKind.OwnBar:
                if (barFull) return;
                barFill += s.shootBarPerShot;
                barWait = s.shootBarWait;
                if (barFill >= s.shootBarSize) { barFill = s.shootBarSize; barFull = true; }
                break;
        }

        float min = Mathf.Min(s.shootEveryMin, s.shootEveryMax), max = Mathf.Max(s.shootEveryMin, s.shootEveryMax);
        nextShotIn = Random.Range(min, max);
        if (model != null) model.PlayClip(s.shootAnimation != null ? s.shootAnimation : (s.look != null ? s.look.attack : null));
        if (s.shootDelay > 0f) fireIn = s.shootDelay;
        else FireNow(s);
    }

    private void FireNow(BecomeSettings s)
    {
        fireIn = -1f;
#if HAS_KINEMATIC_CC
        if (controller == null) return;
        Transform root = controller.MeshRoot != null ? controller.MeshRoot : controller.transform;
        float size = s.look != null && s.look.model != null ? Mathf.Max(0.1f, s.look.size) : 1f;
        float height = controller.Motor.Capsule != null ? controller.Motor.Capsule.height : 2f;
        float radius = controller.Motor.Capsule != null ? controller.Motor.Capsule.radius : 0.5f;

        // The way the character faces (tipped up/down while flying), plus the aim slider
        Vector3 forward = root.forward;
        Vector3 side = Vector3.Cross(Vector3.up, forward);
        if (side.sqrMagnitude < 0.0001f) side = root.right;
        Vector3 dir = Quaternion.AngleAxis(-s.shootAimUpDown, side.normalized) * forward;

        // Out of the front of the body, at 'shoots from' height (scaled by the character's size)
        float up = Mathf.Lerp(0f, height * size, s.shootFromHeight);
        Vector3 from = controller.Motor.TransientPosition + root.up * up + forward * (radius * size + s.shootSize * 0.6f);

        ObstacleBase target = s.shootHoming > 0f ? NearestCreatureAhead(from, dir, s.shootSpeed * s.shootLasts) : null;
        SoundManager.Play(SoundEvent.Attack, from, gameObject);
        int n = Mathf.Max(1, s.shotsAtOnce);
        for (int i = 0; i < n; i++)
        {
            float angle = (i - (n - 1) * 0.5f) * s.shootSpread;
            Vector3 d = Quaternion.AngleAxis(angle, Vector3.up) * dir;
            Projectile.FireFromPlayer(transform, from, d * s.shootSpeed, target, s.shootDamage, s.shootHoming, s.shootDrops, s.shootLasts,
                s.shootSize, s.shootColour, s.shootModel, s.shootTrail, s.shootHitEffect);
        }
#endif
    }

    /// <summary>For homing shots: the closest creature within 45° of where you're aiming and within reach.</summary>
    private static ObstacleBase NearestCreatureAhead(Vector3 from, Vector3 dir, float reach)
    {
        ObstacleBase best = null;
        float bestDist = reach;
        foreach (ObstacleBase o in FindObjectsByType<ObstacleBase>(FindObjectsSortMode.None))
        {
            if (o.payloadType != ObstacleBase.PayloadType.Damage || o.isBouncy || o.HitZoneBody == null || !o.HitZoneBody.enabled) continue;
            Vector3 to = o.HitZoneBody.bounds.center - from;
            float d = to.magnitude;
            if (d >= bestDist || d < 0.01f) continue;
            if (Vector3.Angle(dir, to) > 45f) continue;
            best = o; bestDist = d;
        }
        return best;
    }

    // ------------------------------------------------------------------ The yellow shooting bar (top middle)

    private void ShowBar()
    {
        if (barRoot == null) CreateBar();
        barRoot.SetActive(true);
    }

    private void HideBar()
    {
        if (barRoot != null) barRoot.SetActive(false);
    }

    private void UpdateBar(BecomeSettings s)
    {
        if (barRoot == null || !barRoot.activeSelf) return;
        float t = s.shootBarSize > 0f ? Mathf.Clamp01(barFill / s.shootBarSize) : 0f;
        barFillRect.anchorMax = new Vector2(t, 1f);
        Color c = barFull ? BarFullRed : BarYellow;
        if (barFull && Mathf.Repeat(Time.time * 3f, 1f) < 0.5f) c = Color.Lerp(BarFullRed, Color.white, 0.4f); // flashes while it cools
        barImage.color = c;
    }

    private void CreateBar()
    {
        barRoot = new GameObject("Shooting Bar (auto)");
        Canvas canvas = barRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 440;
        CanvasScaler scaler = barRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        // Dark back, top middle of the screen
        GameObject back = new GameObject("Back");
        back.transform.SetParent(barRoot.transform, false);
        Image backImage = back.AddComponent<Image>();
        backImage.color = new Color(0f, 0f, 0f, 0.55f);
        backImage.raycastTarget = false;
        RectTransform br = backImage.rectTransform;
        br.anchorMin = br.anchorMax = new Vector2(0.5f, 1f);
        br.pivot = new Vector2(0.5f, 1f);
        br.sizeDelta = new Vector2(500f, 22f);
        br.anchoredPosition = new Vector2(0f, -18f);

        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(back.transform, false);
        barImage = fill.AddComponent<Image>();
        barImage.color = BarYellow;
        barImage.raycastTarget = false;
        barFillRect = barImage.rectTransform;
        barFillRect.anchorMin = Vector2.zero;
        barFillRect.anchorMax = new Vector2(0f, 1f);
        barFillRect.offsetMin = new Vector2(3f, 3f);
        barFillRect.offsetMax = new Vector2(-3f, -3f);
    }
}
