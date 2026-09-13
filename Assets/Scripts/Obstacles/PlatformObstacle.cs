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
            float heightOffset = (FollowPlatformRef != null) ? FollowPlatformRef.offsetY : 0;
            float targetY = hit.point.y + heightOffset;
            Vector3 currentPos = transform.position;
            currentPos.y = Mathf.Lerp(currentPos.y, targetY, Time.deltaTime * stepUpSpeed);
            transform.position = currentPos;
            break;
        }
    }
}