using UnityEngine;

[ExecuteAlways]
public class FollowPlatform : MonoBehaviour
{
    [HideInInspector]
    public Transform targetPlatform;

    [Header("Starting Position Offset")]
    [Range(-500f, 500f)] public float offsetX = 0f;
    [Range(-500f, 500f)] public float offsetY = 0.5f;
    [Range(-500f, 500f)] public float offsetZ = 0f;

    private Vector3 lastPlatformPosition;
    private Quaternion lastPlatformRotation;
    private bool initialized = false;

    private void OnEnable()
    {
        InitializeTracking();
    }

    private void Start()
    {
        InitializeTracking();
    }

    private void InitializeTracking()
    {
        if (targetPlatform != null)
        {
            lastPlatformPosition = targetPlatform.position;
            lastPlatformRotation = targetPlatform.rotation;
            initialized = true;
        }
    }

    private void LateUpdate()
    {
        if (targetPlatform == null) return;

        if (!initialized)
        {
            InitializeTracking();
        }

        if (Application.isPlaying)
        {
            // 1. Calculate positional delta
            Vector3 platformDelta = targetPlatform.position - lastPlatformPosition;

            // 2. Calculate rotational delta
            Quaternion rotationDelta = targetPlatform.rotation * Quaternion.Inverse(lastPlatformRotation);

            // 3. Rotate follower around platform center and add positional movement
            Vector3 offsetFromPlatform = transform.position - lastPlatformPosition;
            transform.position = targetPlatform.position + (rotationDelta * offsetFromPlatform);

            // 4. Update rotation
            transform.rotation = rotationDelta * transform.rotation;

            lastPlatformPosition = targetPlatform.position;
            lastPlatformRotation = targetPlatform.rotation;
        }
        else
        {
            // Calculate position using local rotation offset from target platform
            Vector3 localOffset = new Vector3(offsetX, offsetY, offsetZ);
            transform.position = targetPlatform.position + (targetPlatform.rotation * localOffset);
            transform.rotation = targetPlatform.rotation;

            lastPlatformPosition = targetPlatform.position;
            lastPlatformRotation = targetPlatform.rotation;
        }
    }

    [ContextMenu("Snap To Center")]
    public void SnapToPlatformCenter()
    {
        if (targetPlatform == null) return;

        offsetX = 0f;
        offsetZ = 0f;
        
        Vector3 localOffset = new Vector3(offsetX, offsetY, offsetZ);
        transform.position = targetPlatform.position + (targetPlatform.rotation * localOffset);
        transform.rotation = targetPlatform.rotation;

        lastPlatformPosition = targetPlatform.position;
        lastPlatformRotation = targetPlatform.rotation;
    }
}