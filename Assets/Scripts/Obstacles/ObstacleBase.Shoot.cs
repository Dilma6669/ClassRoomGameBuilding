using UnityEngine;

#if HAS_KINEMATIC_CC
using KinematicCharacterController;
#endif

/// <summary>
/// Shooting (optional, creatures only): when the player is close enough, the creature shoots at them every few seconds:
/// glowing balls or any model, with its own damage, speed, homing, dropping like a thrown ball, several at once...
/// It can play a shooting animation and turn to face you. See Projectile.cs.
/// </summary>
public abstract partial class ObstacleBase
{
    [Tooltip("It shoots things at you when you're close enough.")]
    public bool shoots = false;
    [Tooltip("Seconds between shots.")]
    [Range(0.1f, 30f)] public float shootEvery = 2f;
    [Tooltip("How much each shot hurts.")]
    [Range(1, 1000)] public int shootDamage = 10;
    [Tooltip("How fast the shots fly (metres per second).")]
    [Range(1f, 300f)] public float shootSpeed = 15f;
    [Tooltip("It starts shooting when you're this close (metres).")]
    [Range(2f, 500f)] public float shootRange = 25f;
    [Tooltip("It doesn't shoot when you're closer than this (metres), e.g. so it bites up close and shoots from far away. 0 = shoots however close you are.")]
    [Range(0f, 500f)] public float shootMinRange = 0f;
    [Tooltip("It only shoots while it's chasing you (needs 'Chase the player').")]
    public bool shootOnlyWhenChasing = false;
    [Tooltip("Aims at you, up and down too (so it hits you when you jump or fly). Untick: shoots straight ahead, level.")]
    public bool shootAimAtYou = true;
    [Tooltip("Aims where you're running to, not where you are now (harder to dodge).")]
    public bool shootAimAhead = false;
    [Tooltip("The shots curve towards you. 0 = straight, 1 = turns very hard.")]
    [Range(0f, 1f)] public float shootHoming = 0f;
    [Tooltip("The shots fall like a thrown ball (it aims higher to reach you).")]
    public bool shootDrops = false;
    [Tooltip("How many shots at once (spread out like a fan).")]
    [Range(1, 12)] public int shotsAtOnce = 1;
    [Tooltip("How wide the fan is (degrees between shots).")]
    [Range(0f, 60f)] public float shootSpread = 12f;
    [Tooltip("How big each shot is (metres across).")]
    [Range(0.1f, 20f)] public float shootSize = 0.5f;
    [Tooltip("The shot's colour (glowing balls, the trail and the burst when it hits).")]
    public Color shootColour = new Color(1f, 0.45f, 0.1f);
    [Tooltip("Shoot a model instead of a glowing ball: drag one in from Assets/MyModels (e.g. a rock). Empty = a glowing ball.")]
    public GameObject shootModel;
    [Tooltip("A glowing trail behind each shot.")]
    public bool shootTrail = true;
    [Tooltip("The sparkles when a shot hits something.")]
    public Sparkles.Effect shootHitEffect = Sparkles.Effect.Explosion;
    [Tooltip("A shot that misses disappears after this many seconds.")]
    [Range(0.5f, 30f)] public float shootLasts = 5f;
    [Tooltip("How hard a hit pushes you back. 0 = no push.")]
    [Range(0f, 100f)] public float shootKnockback = 0f;
    [Tooltip("It quickly turns to face you when it shoots.")]
    public bool shootFaceYou = true;
    [Tooltip("The animation it plays when it shoots. Empty = its attack animation.")]
    public AnimationClip shootAnimation;
    [Tooltip("The shot leaves this many seconds after the animation starts (so it matches the animation).")]
    [Range(0f, 3f)] public float shootDelay = 0.25f;
    [Tooltip("Where the shot comes out: 0 = its feet, 1 = the top of its head.")]
    [Range(0f, 1f)] public float shootFromHeight = 0.6f;

    private float shootTimer = -1f;
    private float fireIn = -1f;
    private float shotDamageTaken; // from the player's shots: 10 damage = one jump on its head

    /// <summary>How much shot damage counts as one jump on a creature's head.</summary>
    public const float ShotDamagePerJump = 10f;

    /// <summary>
    /// The player (as a shooting character) hit this creature with a shot. Every 10 damage counts as one jump on its head,
    /// so a creature that needs 3 jumps is beaten by 30 damage. Creatures that can't be beaten (0 jumps) just flash.
    /// Returns true if it was a creature (the shot stops there).
    /// </summary>
    public bool TakeShot(float damage)
    {
        if (payloadType != PayloadType.Damage || isBouncy) return false;
        if (defeated) return true;
        int needed = BecomeCharacter.JumpsNeeded(jumpsToDefeat);
        if (needed <= 0)
        {
            if (sparkles) BurstSparkles(Sparkles.Style.Sparks); // can't be beaten
            return true;
        }
        shotDamageTaken += Mathf.Max(0f, damage);
        float hits = jumpsTaken + shotDamageTaken / ShotDamagePerJump;
        if (hits >= needed - 0.0001f) Defeat();
        else
        {
            if (!PlayOwnMoment(AnimationMoment.GetsHit, out _)) SoundManager.Play(SoundEvent.EnemyDamage, transform.position, gameObject);
            StartCoroutine(Squash(false));
        }
        return true;
    }

    /// <summary>Counts down to the next shot; plays the animation, then lets the shot go.</summary>
    private void UpdateShooting()
    {
#if HAS_KINEMATIC_CC
        if (!shoots || payloadType != PayloadType.Damage || defeated) return;
        KinematicCharacterMotor player = FindPlayer();
        if (player == null) return;
        float dt = Time.deltaTime;

        if (fireIn >= 0f)
        {
            fireIn -= dt;
            if (fireIn < 0f) FireNow(player);
            return;
        }

        if (shootTimer < 0f) shootTimer = Random.Range(0.3f, Mathf.Max(0.4f, shootEvery)); // creatures don't all shoot at once
        shootTimer -= dt;
        if (shootTimer > 0f) return;

        Vector3 middle = PlayerMiddle(player);
        float away = (middle - ShotOrigin(middle)).magnitude;
        bool close = away <= shootRange && away >= shootMinRange;
        bool allowed = !shootOnlyWhenChasing || chaseState == ChaseState.Chasing;
        if (!close || !allowed) { shootTimer = 0.25f; return; } // look again soon

        shootTimer = shootEvery;
        fireIn = shootDelay;

        // The shooting animation (or its attack one), turning to face you
        if (!lookedForModel) { ownModel = GetComponentInChildren<CustomModel>(); lookedForModel = true; }
        float length = 0f;
        if (ownModel != null) length = ownModel.PlayClip(shootAnimation != null ? shootAnimation : ownModel.attack);
        if (shootFaceYou)
        {
            if (attackTimeLeft <= 0f && turnBackLeft <= 0f)
            {
                FollowPlatform follow = GetComponent<FollowPlatform>();
                turnParent = follow != null ? follow.targetPlatform : null;
                rotationBeforeAttack = turnParent != null ? Quaternion.Inverse(turnParent.rotation) * transform.rotation : transform.rotation;
            }
            attackTarget = player.transform;
            attackTimeLeft = Mathf.Max(shootDelay + 0.15f, Mathf.Min(length, 1f));
            turnBackLeft = 0f;
            if (chaseState == ChaseState.Chasing) attackTimeLeft = shootDelay + 0.1f; // chasers barely stop
        }
        if (fireIn <= 0f) { fireIn = -1f; FireNow(player); }
#endif
    }

#if HAS_KINEMATIC_CC
    private static Vector3 PlayerMiddle(KinematicCharacterMotor p) =>
        p.TransientPosition + p.CharacterUp * (p.Capsule != null ? p.Capsule.height * 0.5f : 1f);

    /// <summary>Where shots come out: the front of its body, at 'Shoots from' height.</summary>
    private Vector3 ShotOrigin(Vector3 towards)
    {
        Collider body = HitZoneBody != null ? HitZoneBody : physicalMeshCollider;
        if (body == null) return transform.position + Vector3.up;
        Bounds b = body.bounds;
        Vector3 o = new Vector3(b.center.x, Mathf.Lerp(b.min.y, b.max.y, shootFromHeight), b.center.z);
        Vector3 flat = towards - o; flat.y = 0f;
        if (flat.sqrMagnitude < 0.0001f) flat = transform.forward;
        float reach = Mathf.Min(b.extents.x, b.extents.z) * 0.9f;
        return o + flat.normalized * reach;
    }

    private void FireNow(KinematicCharacterMotor player)
    {
        fireIn = -1f;
        if (defeated) return;
        Vector3 aimAt = PlayerMiddle(player);
        Vector3 from = ShotOrigin(aimAt);
        if (shootAimAhead)
        {
            float time = (aimAt - from).magnitude / Mathf.Max(1f, shootSpeed);
            Vector3 v = player.Velocity; v.y = shootAimAtYou ? v.y : 0f;
            aimAt += v * time;
        }

        Vector3 dir;
        if (!shootAimAtYou)
        {
            dir = aimAt - from; dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) dir = transform.forward;
            dir.Normalize();
        }
        else dir = (aimAt - from).normalized;
        if (shootDrops) dir = LobDirection(from, aimAt, shootSpeed, dir);

        SoundManager.Play(SoundEvent.EnemyAttack, transform.position, gameObject);
        int n = Mathf.Max(1, shotsAtOnce);
        for (int i = 0; i < n; i++)
        {
            float angle = (i - (n - 1) * 0.5f) * shootSpread;
            Vector3 d = Quaternion.AngleAxis(angle, Vector3.up) * dir;
            Projectile.Fire(this, from, d * shootSpeed, player.transform, shootDamage, shootHoming, shootDrops, shootLasts,
                shootKnockback, shootSize, shootColour, shootModel, shootTrail, shootHitEffect);
        }
    }

    /// <summary>The direction to throw at 'speed' so it lands on the target (the low arc); 45° up if it can't reach.</summary>
    private static Vector3 LobDirection(Vector3 from, Vector3 to, float speed, Vector3 fallback)
    {
        Vector3 flat = to - from; float y = flat.y; flat.y = 0f;
        float x = flat.magnitude;
        if (x < 0.01f) return fallback;
        float g = Physics.gravity.magnitude;
        float v2 = speed * speed;
        float root = v2 * v2 - g * (g * x * x + 2f * y * v2);
        float angle = root >= 0f ? Mathf.Atan((v2 - Mathf.Sqrt(root)) / (g * x)) : Mathf.PI / 4f;
        return (flat / x * Mathf.Cos(angle) + Vector3.up * Mathf.Sin(angle)).normalized;
    }
#endif
}
