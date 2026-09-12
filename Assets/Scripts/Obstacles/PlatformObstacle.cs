using UnityEngine;

[RequireComponent(typeof(FollowPlatform))]
public class PlatformObstacle : ObstacleBase
{
    [Header("Surface Height Snapping")]
    [Range(0.1f, 5f)] [SerializeField] private float raycastOriginHeight = 4f;
    [Range(0f, 2f)] [SerializeField] private float surfaceOffset = 0.5f;
    [Range(1f, 50f)] [SerializeField] private float stepUpSpeed = 15f;

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
        RaycastHit[] hits = Physics.RaycastAll(rayOrigin, Vector3.down, raycastOriginHeight * 3f, Physics.AllLayers, QueryTriggerInteraction.Ignore);

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == physicalMeshCollider || hit.transform.IsChildOf(transform)) continue;

            float targetY = hit.point.y + surfaceOffset;
            Vector3 currentPos = transform.position;
            currentPos.y = Mathf.Lerp(currentPos.y, targetY, Time.deltaTime * stepUpSpeed);
            transform.position = currentPos;
            break;
        }
    }
}