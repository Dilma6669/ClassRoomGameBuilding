using UnityEngine;

#if HAS_KINEMATIC_CC
using KinematicCharacterController;
#endif

[RequireComponent(typeof(FollowPlatform))]
public class PlatformObstacle : ObstacleBase
{
    [Header("Surface Height Snapping")]
    [Range(0.1f, 5f)] private float raycastOriginHeight = 4f;
    [Range(1f, 50f)] private float stepUpSpeed = 15f;

    public FollowPlatform FollowPlatformRef { get; private set; }

    // ---- Falling off platforms -------------------------------------------------------------
    // Moving obstacles that end up with nothing under them fall, then carry on from wherever
    // they land: on another platform (they ride that one) or on the ground (they move around
    // where they landed). Static obstacles stay locked to their platform like before.
    private const float Gravity = 25f;
    private const float StepDownTolerance = 0.75f;  // drops bigger than this count as an edge
    private const float MaxFallTime = 10f;          // fell out of the world: remove it

    private bool isFalling;
    private bool onGroundNotPlatform;               // landed on terrain/ground, no platform anchor
    private float fallSpeed;
    private float fallTime;

    public bool IsFalling => isFalling;

    /// <summary>Wanderers and patrollers move (and can fall); static obstacles never leave their platform.</summary>
    private bool IsMovingObstacle => (currentMovementStrategy != null && !(currentMovementStrategy is PlatformStaticDriver)) || ChasesPlayer;

    private bool ChasesPlayer => chasePlayer && payloadType == PayloadType.Damage;

    /// <summary>A chaser's home spot moves with the platform it rides.</summary>
    protected override Transform HomeSurface => GetPlatformTransform();

    /// <summary>Chase step on a platform: stop at the edge instead of running off it.</summary>
    protected override void StepTowards(Vector3 target, float speed)
    {
        Vector3 up = transform.up;
        Vector3 dir = Vector3.ProjectOnPlane(target - transform.position, up);
        if (dir.sqrMagnitude < 0.0001f) return;
        FaceTowards(target);
        float step = Mathf.Min(speed * Time.deltaTime, dir.magnitude);
        Vector3 ahead = transform.position + dir.normalized * (step + EdgeLookAhead);
        if (!HasGroundAt(ahead)) return; // the player is off the edge: wait here
        transform.position += dir.normalized * step;
    }

    /// <summary>How far ahead movers look for an edge.</summary>
    public float EdgeLookAhead => Mathf.Max(0.3f, GroundLift);

    protected override void Awake()
    {
        base.Awake();
        FollowPlatformRef = GetComponent<FollowPlatform>();
    }

    protected override void Update()
    {
        if (!Application.isPlaying)
        {
            base.Update();
            return;
        }

        if (isFalling)
        {
            UpdateFalling();
            return; // no walking while in the air
        }

        base.Update(); // the movement driver moves us

        if (!IsMovingObstacle)
        {
            SnapToSurfaceHeight(); // static: exactly the old behaviour
            return;
        }

        // Moving: stay on the surface, or start falling if it's gone
        if (FindGround(transform.position, StepDownTolerance + GroundLift, out RaycastHit ground))
        {
            Vector3 p = transform.position;
            p.y = Mathf.Lerp(p.y, ground.point.y + GroundLift, Time.deltaTime * stepUpSpeed);
            transform.position = p;

            // Walked (or got carried) onto a different platform, or onto the ground? Ride that instead.
            UpdateSurfaceOwner(ground);
        }
        else
        {
            StartFalling();
        }
    }

    /// <summary>Is there ground to stand on at this spot (no big drop)? Used by movers to avoid edges.</summary>
    public bool HasGroundAt(Vector3 worldPosition, float maxDrop = StepDownTolerance)
    {
        return FindGround(worldPosition, maxDrop + GroundLift, out _);
    }

    private void StartFalling()
    {
        isFalling = true;
        fallSpeed = 0f;
        fallTime = 0f;
        // Let go of the platform, or it would keep dragging us through the air
        if (FollowPlatformRef != null) FollowPlatformRef.Detach();
    }

    private void UpdateFalling()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        fallTime += dt;
        if (fallTime > MaxFallTime)
        {
            Destroy(gameObject); // fell out of the world
            return;
        }

        fallSpeed += Gravity * dt;
        float step = fallSpeed * dt;

        if (FindGround(transform.position, step + GroundLift, out RaycastHit ground))
        {
            Land(ground);
        }
        else
        {
            transform.position += Vector3.down * step;
        }
    }

    private void Land(RaycastHit ground)
    {
        isFalling = false;
        Vector3 p = transform.position;
        p.y = ground.point.y + GroundLift;
        transform.position = p;

        // Landed on a platform? Ride that one. Otherwise we're on the ground now.
        SwitchSurface(PlatformUnder(ground));
    }

    /// <summary>The moving part of the platform this surface belongs to, or null for terrain/ground.</summary>
    private static Transform PlatformUnder(RaycastHit ground)
    {
        PlatformLogic platform = ground.collider.GetComponentInParent<PlatformLogic>();
        Transform moving = platform != null ? platform.TargetChild : null;
        return moving != null && ground.transform.IsChildOf(moving) ? moving : null;
    }

    private void UpdateSurfaceOwner(RaycastHit ground)
    {
        // Standing on another (static) obstacle doesn't change which platform we belong to
        if (ground.collider.GetComponentInParent<ObstacleBase>() != null) return;

        Transform under = PlatformUnder(ground);
        Transform current = onGroundNotPlatform || FollowPlatformRef == null ? null : FollowPlatformRef.targetPlatform;
        if (under != current) SwitchSurface(under);
    }

    /// <summary>Anchor to a platform (ride it), or to the ground when platform is null, and tell the mover.</summary>
    private void SwitchSurface(Transform platform)
    {
        if (platform != null)
        {
            onGroundNotPlatform = false;
            if (FollowPlatformRef != null) FollowPlatformRef.AttachTo(platform);
        }
        else
        {
            onGroundNotPlatform = true;
            if (FollowPlatformRef != null) FollowPlatformRef.Detach();
        }

        // Tell the mover to use its new home
        if (currentMovementStrategy is IObstacleAnchor anchor) anchor.Reanchor();
    }

    /// <summary>
    /// Nearest surface under (or just above) a point, ignoring ourselves, the player and other movers.
    /// Only counts surfaces no more than maxBelow under the point.
    /// </summary>
    private bool FindGround(Vector3 point, float maxBelow, out RaycastHit ground)
    {
        ground = default;
        Vector3 rayOrigin = point + Vector3.up * raycastOriginHeight;
        RaycastHit[] hits = Physics.RaycastAll(rayOrigin, Vector3.down, raycastOriginHeight + maxBelow, Physics.AllLayers, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (!IsStandableSurface(hit)) continue;
            ground = hit;
            return true;
        }
        return false;
    }

    private bool IsStandableSurface(RaycastHit hit)
    {
        if (hit.collider == physicalMeshCollider || hit.transform.IsChildOf(transform)) return false;
#if HAS_KINEMATIC_CC
        if (hit.collider.GetComponentInParent<KinematicCharacterMotor>() != null) return false;
#else
        if (hit.collider.CompareTag("Player")) return false;
#endif
        ObstacleBase hitObstacle = hit.collider.GetComponentInParent<ObstacleBase>();
        if (hitObstacle != null)
        {
            // Static obstacles never stand on obstacles; movers can step on static ones but not on other movers
            if (!IsMovingObstacle) return false;
            IObstacleMovement hitMovement = hitObstacle.GetComponent<IObstacleMovement>();
            if (hitMovement != null && !(hitMovement is PlatformStaticDriver) && !(hitMovement is TerrainStaticDriver)) return false;
        }
        return true;
    }

    protected override void OnValidate()
    {
        base.OnValidate();
        SitOnPlatformInEditor();
    }

    /// <summary>
    /// In the editor, place the obstacle so its bottom sits on the platform's top surface
    /// (matches where it will be during play). Uses the platform's real size, so it works for any scale.
    /// </summary>
    public void SitOnPlatformInEditor()
    {
        if (Application.isPlaying || !sitOnGround) return;
        FollowPlatform follow = GetComponent<FollowPlatform>();
        if (follow == null || follow.targetPlatform == null) return;

        // Highest point of what you can SEE (a platform model, or the plain cylinder)
        float visibleTop = float.NegativeInfinity;
        foreach (Renderer r in follow.targetPlatform.GetComponentsInChildren<Renderer>())
        {
            if (r.enabled) visibleTop = Mathf.Max(visibleTop, r.bounds.max.y);
        }
        if (float.IsInfinity(visibleTop)) return;

        // The platform's surface directly under this obstacle (e.g. on top of a hill on a rock platform)
        float top = float.NegativeInfinity;
        Vector3 rayStart = new Vector3(transform.position.x, visibleTop + 0.5f, transform.position.z);
        foreach (RaycastHit hit in Physics.RaycastAll(rayStart, Vector3.down, 1000f, Physics.AllLayers, QueryTriggerInteraction.Ignore))
        {
            if (hit.transform.IsChildOf(follow.targetPlatform)) top = Mathf.Max(top, hit.point.y);
        }
        if (float.IsInfinity(top)) top = visibleTop;

        float topAboveCentre = top - follow.targetPlatform.position.y;
        follow.offsetY = topAboveCentre + GroundLift;
    }

    /// <summary>The platform we ride, or null when we've fallen onto the ground (movers then use a fixed world spot).</summary>
    public Transform GetPlatformTransform()
    {
        if (onGroundNotPlatform) return null;
        if (FollowPlatformRef != null && FollowPlatformRef.targetPlatform != null)
        {
            return FollowPlatformRef.targetPlatform;
        }
        return transform.parent;
    }

    private void SnapToSurfaceHeight()
    {
        Vector3 rayOrigin = transform.position + (Vector3.up * raycastOriginHeight);
        RaycastHit[] hits = Physics.RaycastAll(rayOrigin, Vector3.down, raycastOriginHeight * 3f, Physics.AllLayers,
            QueryTriggerInteraction.Ignore);
        // RaycastAll returns hits in no particular order - check the nearest surface first
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            // 1. Ignore self and child colliders
            if (hit.collider == physicalMeshCollider || hit.transform.IsChildOf(transform)) continue;

            // 2. Ignore Player colliders
#if HAS_KINEMATIC_CC
            if (hit.collider.GetComponentInParent<KinematicCharacterMotor>() != null) continue;
#else
            if (hit.collider.CompareTag("Player") || hit.collider.GetComponentInParent<Rigidbody>() != null) continue;
#endif

            // 3. Static obstacles ignore all other obstacles (prevents floating loop).
            //    BUT Wanderers CAN walk over static obstacles!
            bool isMovingObstacle =
                currentMovementStrategy != null && !(currentMovementStrategy is PlatformStaticDriver);

            if (!isMovingObstacle)
            {
                // If I am a STATIC obstacle, ignore other obstacles so I don't float into the sky
                if (hit.collider.GetComponentInParent<ObstacleBase>() != null) continue;
            }
            else
            {
                // If I am a WANDERER, ignore ONLY other moving wanderers (don't stack on each other),
                // but ALLOW stepping on static obstacles!
                ObstacleBase hitObstacle = hit.collider.GetComponentInParent<ObstacleBase>();
                if (hitObstacle != null)
                {
                    IObstacleMovement hitMovement = hitObstacle.GetComponent<IObstacleMovement>();
                    bool isHitMoving = hitMovement != null && !(hitMovement is PlatformStaticDriver);
                    if (isHitMoving) continue; // Ignore other wanderers
                }
            }

            // Apply surface height
            // Keep the bottom of the shape on the platform surface, whatever its size
            float targetY = hit.point.y + GroundLift;
            Vector3 currentPos = transform.position;
            currentPos.y = Mathf.Lerp(currentPos.y, targetY, Time.deltaTime * stepUpSpeed);
            transform.position = currentPos;
            break;
        }
    }
}