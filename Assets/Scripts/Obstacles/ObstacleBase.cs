using System.Collections.Generic;
using UnityEngine;

#if HAS_KINEMATIC_CC
using KinematicCharacterController;
#endif

[ExecuteAlways]
public abstract partial class ObstacleBase : MonoBehaviour
{
    public enum PayloadType
    {
        None,
        Damage,
        HealthBooster,
        StaminaBooster,
        InvincibilityBuff,
        JumpBuff,
        SprintBuff
    }

    /// <summary>What kind of thing this is, in the same words as the Game Builder's buttons ('Wanderer', 'Health Pickup'...).</summary>
    public string KindName()
    {
        if (isBouncy) return "Bounce Ball";
        switch (payloadType)
        {
            case PayloadType.HealthBooster: return "Health Pickup";
            case PayloadType.StaminaBooster: return "Stamina Pickup";
            case PayloadType.InvincibilityBuff: return "Shield Pickup";
            case PayloadType.JumpBuff: return "Super Jump Pickup";
            case PayloadType.SprintBuff: return "Speed Boost Pickup";
            case PayloadType.None: return "Harmless Thing";
        }

        IObstacleMovement movement = GetComponent<IObstacleMovement>();
        string kind = movement == null ? "" : movement.GetType().Name;
        if (kind.Contains("Wander")) return "Wanderer";
        if (kind.Contains("Patrol")) return "Patroller";
        return "Guard";
    }

    /// <summary>New creatures and pickups are called 'ObstacleLogic' (the prefab's name); this names them by their kind.</summary>
    public static void NameByKind(GameObject obstacle)
    {
        ObstacleBase o = obstacle != null ? obstacle.GetComponent<ObstacleBase>() : null;
        if (o != null && obstacle.name.StartsWith("ObstacleLogic")) obstacle.name = o.KindName();
    }

    [Header("Transform Settings")] [Range(0.1f, 50f)]
    public float objectScale = 1f;
    [Tooltip("Automatically lift the shape so it sits ON the ground instead of half inside it.")]
    public bool sitOnGround = true;
    [Tooltip("Initial rotation (Euler angles) applied to the obstacle.")]
    [Range(0f, 360f)] public float initialYRotation = 0f;
    
    [Header("Payload Settings")] public PayloadType payloadType = PayloadType.Damage;
    [Range(1, 100f)] public int payloadAmount = 10;
    [Range(1f, 60f)] public float buffDuration = 5f;
    public bool destroyOnTrigger = false;

    [Header("Bounce Settings")] public bool isBouncy = false;
    [Range(0.5f, 5f)] public float triggerRadius = 0.5f;
    [Range(5f, 50f)] public float launchForce = 25f;
    [Range(0f, 1f)] public float upwardBias = 0.5f;
    [Range(0f, 1f)] public float momentumTransfer = 0.3f;

    [Header("Jump On Top (creatures only)")]
    [Tooltip("How many times the player must land on this creature's head to defeat it. 0 = it can't be beaten (it hurts even from above).")]
    [Range(0, 10)] public int jumpsToDefeat = 1;
    [Tooltip("How high the player bounces off after landing on its head.")]
    [Range(5f, 30f)] public float stompBounce = 12f;

    [Header("Effects (when picked up or beaten)")]
    [Tooltip("A burst of sparkles when it's picked up or defeated.")]
    public bool sparkles = true;
    public enum SparkleStyle { Automatic, Swirl, Spray, Ring, Fountain, Explosion, Twinkle, Sparks, Shimmer }
    [Tooltip("Automatic: pickups swirl up, creatures spray out.")]
    public SparkleStyle sparkleStyle = SparkleStyle.Automatic;
    [Tooltip("Pick your own sparkle colour (otherwise it matches the glow, or what the thing is).")]
    public bool customSparkleColour = false;
    public Color sparkleColour = Color.white;
    [Tooltip("How many sparkles (1 = normal).")]
    [Range(0.2f, 5f)] public float sparkleAmount = 1f;
    [Tooltip("How big the sparkles are (1 = normal).")]
    [Range(0.3f, 4f)] public float sparkleSize = 1f;

    private int jumpsTaken;
    private bool defeated;
    private float lastStompTime = -10f;

    // Shared Components
    protected MeshCollider physicalMeshCollider;
    protected MeshCollider triggerMeshCollider;
    protected IObstacleMovement currentMovementStrategy;

    protected virtual void OnValidate()
    {
        transform.localScale = Vector3.one;
        for (int i = 0; i < transform.childCount; i++)
        {
            transform.GetChild(i).localScale = Vector3.one * objectScale;
        }
    
        // Lock X and Z to 0, applying rotation strictly around the Y-axis
        transform.localRotation = Quaternion.Euler(0f, initialYRotation, 0f);

        ApplyScale();
        RefitSoon(); // re-fit the hit zone (size, model or height may have changed)
    }

    protected virtual void Awake()
    {
        InitializeCollidersAndPhysics();
        UpdateHitZone(false); // older creatures get their hit zone here
        currentMovementStrategy = GetComponent<IObstacleMovement>();
    }

    private bool fittedInPlay;

    protected virtual void Update()
    {
        if (!Application.isPlaying)
        {
            EditorKeepHitZoneFitted();
            return;
        }
        if (!fittedInPlay)
        {
            // Once, after models have been set up
            fittedInPlay = true;
            UpdateHitZone(fitHitZoneToModel);
        }

#if HAS_KINEMATIC_CC
        // Landing on a creature usually means standing on its solid body (the touch zone isn't entered)
        if (Application.isPlaying && payloadType == PayloadType.Damage && jumpsToDefeat > 0 && !defeated)
        {
            KinematicCharacterMotor player = FindPlayer();
            if (player != null)
            {
                CharacterGroundingReport ground = player.GroundingStatus;
                if (ground.FoundAnyGround && ground.GroundCollider != null && ground.GroundCollider.transform.IsChildOf(transform))
                {
                    GetJumpedOn(player);
                }
            }
        }
#endif

        if (Application.isPlaying && UpdateAttack())
        {
            // attacking: it turns to face the player and doesn't walk
        }
        else if (Application.isPlaying && UpdateChase())
        {
            PlayMoveSound(); // chasing or walking home: the normal movement waits
        }
        else if (Application.isPlaying && currentMovementStrategy != null)
        {
            currentMovementStrategy.ProcessMovement(this);
            PlayMoveSound();
        }
    }

    // General "creature moving" sound (a model's own walk-animation sound replaces it)
    private float moveSoundTimer;
    private Vector3 lastMovePosition;
    private CustomModel ownModel;
    private bool lookedForModel;

    private void PlayMoveSound()
    {
        if (payloadType != PayloadType.Damage || defeated || Time.deltaTime <= 0f) return;
        if (lastMovePosition == Vector3.zero)
        {
            // First frame: don't let every creature make a noise at the same moment
            lastMovePosition = transform.position;
            moveSoundTimer = Random.Range(0f, 1f);
            return;
        }
        Vector3 step = transform.position - lastMovePosition;
        lastMovePosition = transform.position;
        step.y = 0f;
        moveSoundTimer -= Time.deltaTime;
        if (step.magnitude / Time.deltaTime < 0.3f || moveSoundTimer > 0f) return;

        if (!lookedForModel) { ownModel = GetComponentInChildren<CustomModel>(); lookedForModel = true; }
        if (ownModel != null && (ownModel.HasOwnSound(CustomModel.AnimSlot.Walk) || ownModel.HasOwnSound(CustomModel.AnimSlot.Run))) return;

        SoundEntry entry = SoundManager.Find(SoundEvent.CreatureMove, gameObject);
        if (entry == null) { moveSoundTimer = 1f; return; }
        moveSoundTimer = Mathf.Max(entry.repeatEvery, entry.clip.length * 0.9f);
        SoundManager.Play(SoundEvent.CreatureMove, transform.position, gameObject);
    }

    /// <summary>
    /// How far the shape's bottom is below this object's position (at the current scale).
    /// Lifting by this much makes the shape sit on the ground. 0 if Sit On Ground is off.
    /// </summary>
    public float GroundLift
    {
        get
        {
            if (!sitOnGround) return 0f;
            Transform visual = transform.Find("⚠️ DO NOT TOUCH");
            if (visual == null) return 0f;
            MeshFilter filter = visual.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null) return 0f;
            return Mathf.Max(0f, -filter.sharedMesh.bounds.min.y * visual.localScale.y);
        }
    }

    private void InitializeCollidersAndPhysics()
    {
        transform.localScale = Vector3.one;

        // 1. Solid Collider on "⚠️ DO NOT TOUCH"
        Transform visualChild = transform.Find("⚠️ DO NOT TOUCH");
        if (visualChild != null)
        {
            physicalMeshCollider = visualChild.GetComponent<MeshCollider>();
        }

        // 2. Trigger Collider on "TriggerCollider"
        Transform triggerChild = transform.Find("⚠️ DO NOT TOUCH/TriggerCollider");
        if (triggerChild == null && visualChild != null) triggerChild = visualChild.Find("TriggerCollider");
    
        if (triggerChild != null)
        {
            triggerMeshCollider = triggerChild.GetComponent<MeshCollider>();
            if (triggerMeshCollider != null)
            {
                triggerMeshCollider.isTrigger = true;
                triggerMeshCollider.enabled = isBouncy || payloadType != PayloadType.None;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!Application.isPlaying) return;

        bool isPlayerTrigger = false;

#if HAS_KINEMATIC_CC
        KinematicCharacterMotor motor = other.GetComponentInParent<KinematicCharacterMotor>();
        if (motor == null) motor = other.GetComponent<KinematicCharacterMotor>();

        if (motor != null)
        {
            isPlayerTrigger = true;

            // Landed on a creature's head: it gets hit instead of the player
            if (payloadType == PayloadType.Damage && jumpsToDefeat > 0 && IsLandingOnTop(motor))
            {
                GetJumpedOn(motor);
                return;
            }
            if (defeated) return;

            if (isBouncy)
            {
                Vector3 surfaceNormal = (motor.TransientPosition - transform.position).normalized;
                if (surfaceNormal == Vector3.zero) surfaceNormal = Vector3.up;

                Vector3 launchDirection = Vector3.Lerp(surfaceNormal, Vector3.up, upwardBias).normalized;
                motor.ForceUnground();
                float totalLaunchSpeed = launchForce + (motor.BaseVelocity.magnitude * momentumTransfer);
                motor.BaseVelocity = launchDirection * totalLaunchSpeed;
                SoundManager.Play(SoundEvent.Bounce, transform.position, gameObject);
            }
        }
#else
        // Fallback bounce handling using standard Unity Rigidbody physics
        Rigidbody rb = other.GetComponentInParent<Rigidbody>();
        if (rb == null) rb = other.GetComponent<Rigidbody>();

        if (rb != null || other.CompareTag("Player"))
        {
            isPlayerTrigger = true;

            if (isBouncy && rb != null)
            {
                Vector3 surfaceNormal = (other.transform.position - transform.position).normalized;
                if (surfaceNormal == Vector3.zero) surfaceNormal = Vector3.up;

                Vector3 launchDirection = Vector3.Lerp(surfaceNormal, Vector3.up, upwardBias).normalized;
                float totalLaunchSpeed = launchForce + (rb.linearVelocity.magnitude * momentumTransfer);
                rb.linearVelocity = launchDirection * totalLaunchSpeed;
            }
        }
#endif

        if (isPlayerTrigger)
        {
            var playerLogic = other.GetComponentInParent<PlayerLogic>();

            switch (payloadType)
            {
                case PayloadType.Damage:
                    var damagable = other.GetComponentInParent<IDamagable>();
                    if (damagable != null)
                    {
                        if (!StartAttack(other.transform)) SoundManager.Play(SoundEvent.EnemyAttack, transform.position, gameObject);
                        damagable.TakeDamage(payloadAmount);
                    }
                    break;

                case PayloadType.HealthBooster:
                    var playerHealth = other.GetComponentInParent<Health>();
                    if (playerHealth != null) playerHealth.Heal(payloadAmount);
                    break;

                case PayloadType.StaminaBooster:
                    if (playerLogic != null) playerLogic.RestoreStamina(payloadAmount);
                    break;

                case PayloadType.InvincibilityBuff:
                    var healthComp = other.GetComponentInParent<Health>();
                    if (healthComp != null) healthComp.ApplyInvincibility(buffDuration);
                    break;

                case PayloadType.JumpBuff:
                    if (playerLogic != null) playerLogic.ApplyJumpBuff(payloadAmount, buffDuration);
                    break;

                case PayloadType.SprintBuff:
                    if (playerLogic != null) playerLogic.ApplySprintBuff(payloadAmount, buffDuration);
                    break;
            }

            bool isPickup = payloadType != PayloadType.None && payloadType != PayloadType.Damage;
            if (sparkles && isPickup) BurstSparkles(Sparkles.Style.Pickup);
            if (isPickup) SoundManager.Play(SoundEvent.PickUp, transform.position, gameObject);

            if (destroyOnTrigger && payloadType != PayloadType.None)
            {
                Destroy(gameObject);
            }
        }
    }

#if HAS_KINEMATIC_CC
    private static KinematicCharacterMotor cachedPlayer;
    private static int lastPlayerSearchFrame = -100;

    private static KinematicCharacterMotor FindPlayer()
    {
        // Look for the player now and then, not every frame for every creature
        if (cachedPlayer == null && Time.frameCount - lastPlayerSearchFrame > 30)
        {
            lastPlayerSearchFrame = Time.frameCount;
            cachedPlayer = FindAnyObjectByType<KinematicCharacterMotor>();
        }
        return cachedPlayer;
    }

    /// <summary>Is the player coming down onto the top half of this creature (not walking into its side)?</summary>
    private bool IsLandingOnTop(KinematicCharacterMotor motor)
    {
        float feet = motor.TransientPosition.y;

        // Standing on it, or feet up at its top: the character controller may already count
        // the player as "on the ground" (with no speed) in the same moment they land
        CharacterGroundingReport ground = motor.GroundingStatus;
        if (ground.GroundCollider != null && ground.GroundCollider.transform.IsChildOf(transform)) return true;
        if (physicalMeshCollider != null && feet >= physicalMeshCollider.bounds.max.y - 0.25f) return true;

        Collider shape = triggerMeshCollider != null ? triggerMeshCollider : physicalMeshCollider;
        if (shape == null) return false;
        bool inTheAir = !ground.IsStableOnGround || motor.BaseVelocity.y < -0.5f;
        return inTheAir && motor.BaseVelocity.y < 1f && feet > shape.bounds.center.y;
    }

    private void GetJumpedOn(KinematicCharacterMotor motor)
    {
        // Bounce the player off its head
        motor.ForceUnground();
        Vector3 v = motor.BaseVelocity;
        v.y = stompBounce;
        motor.BaseVelocity = v;

        if (defeated || Time.time - lastStompTime < 0.25f) return; // one landing = one hit
        lastStompTime = Time.time;

        jumpsTaken++;
        SoundManager.Play(SoundEvent.Attack, transform.position, gameObject);
        if (jumpsTaken >= jumpsToDefeat) Defeat();
        else
        {
            if (!PlayOwnMoment(AnimationMoment.GetsHit, out _)) SoundManager.Play(SoundEvent.EnemyDamage, transform.position, gameObject);
            StartCoroutine(Squash(false));
        }
    }
#endif

    /// <summary>Beaten by being jumped on: stop, flatten, disappear.</summary>
    private void Defeat()
    {
        defeated = true;
        bool ownSound = StartDeath(out float beatenLength);
        if (!ownSound) SoundManager.Play(SoundEvent.EnemyDeath, transform.position, gameObject);
        currentMovementStrategy = null;
        UnityEngine.AI.NavMeshAgent agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null) agent.enabled = false;
        foreach (Collider c in GetComponentsInChildren<Collider>()) c.enabled = false;
        if (beatenLength > 0f)
        {
            // It has its own death animation (e.g. falling over): play it, then shrink away (or vanish)
            StartCoroutine(DieThenShrink(beatenLength));
            return;
        }
        if (sparkles) BurstSparkles(Sparkles.Style.Poof);
        StartCoroutine(Squash(true));
    }

    /// <summary>
    /// Plays this creature's own extra animation for the moment (Game Builder > Sounds > one model).
    /// Returns true if that animation had its own sound (then the general sound is skipped).
    /// </summary>
    private bool PlayOwnMoment(AnimationMoment moment, out float length)
    {
        length = 0f;
        if (!lookedForModel) { ownModel = GetComponentInChildren<CustomModel>(); lookedForModel = true; }
        if (ownModel == null) return false;
        length = ownModel.PlayMoment(moment, out bool playedSound);
        return playedSound;
    }

    private void BurstSparkles(Sparkles.Style automaticStyle)
    {
        Sparkles.Style style = automaticStyle;
        if (sparkleStyle == SparkleStyle.Swirl) style = Sparkles.Style.Pickup;
        else if (sparkleStyle == SparkleStyle.Spray) style = Sparkles.Style.Poof;
        else if (sparkleStyle == SparkleStyle.Ring) style = Sparkles.Style.Ring;
        else if (sparkleStyle == SparkleStyle.Fountain) style = Sparkles.Style.Fountain;
        else if (sparkleStyle == SparkleStyle.Explosion) style = Sparkles.Style.Explosion;
        else if (sparkleStyle == SparkleStyle.Twinkle) style = Sparkles.Style.Twinkle;
        else if (sparkleStyle == SparkleStyle.Sparks) style = Sparkles.Style.Sparks;
        else if (sparkleStyle == SparkleStyle.Shimmer) style = Sparkles.Style.Shimmer;
        Sparkles.Burst(transform.position + Vector3.up * 0.5f, SparkleColour(), style, sparkleAmount, sparkleSize);
    }

    /// <summary>The chosen colour, else its glow colour if it glows, else a colour that suits what it is.</summary>
    private Color SparkleColour()
    {
        if (customSparkleColour) return sparkleColour;
        Glow glow = GetComponent<Glow>();
        return glow != null ? glow.colour : Sparkles.ColourFor(payloadType);
    }

    /// <summary>Squish flat for a moment (and vanish if defeated).</summary>
    private System.Collections.IEnumerator Squash(bool thenVanish)
    {
        List<Transform> parts = new List<Transform>();
        List<Vector3> scales = new List<Vector3>();
        for (int i = 0; i < transform.childCount; i++)
        {
            parts.Add(transform.GetChild(i));
            scales.Add(transform.GetChild(i).localScale);
        }

        for (float t = 0f; t < 0.12f; t += Time.deltaTime)
        {
            for (int i = 0; i < parts.Count; i++)
                if (parts[i] != null) parts[i].localScale = Vector3.Scale(scales[i], new Vector3(1.3f, Mathf.Lerp(1f, 0.3f, t / 0.12f), 1.3f));
            yield return null;
        }

        if (thenVanish)
        {
            yield return new WaitForSeconds(0.3f);
            Destroy(gameObject);
            yield break;
        }

        yield return new WaitForSeconds(0.1f);
        for (int i = 0; i < parts.Count; i++)
            if (parts[i] != null) parts[i].localScale = scales[i];
    }

    public void ApplyScale()
    {
        transform.localScale = Vector3.one;
        for (int i = 0; i < transform.childCount; i++)
        {
            transform.GetChild(i).localScale = Vector3.one * objectScale;
        }
    }

    protected virtual void OnDrawGizmosSelected()
    {
        // The hit zone and chase circles are drawn by Editor/ObstacleSceneDrawer.cs (they show even with Gizmos off)
    }
}