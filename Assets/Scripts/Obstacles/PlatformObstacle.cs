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

    protected override void Awake()
    {
        base.Awake();
        FollowPlatformRef = GetComponent<FollowPlatform>();
    }

    protected override void Update()
    {
        base.Update();
    
        if (Application.isPlaying)
        {
            SnapToSurfaceHeight();
        }
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

    public Transform GetPlatformTransform()
    {
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