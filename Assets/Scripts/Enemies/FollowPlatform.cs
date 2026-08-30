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
            // During Play mode, follow platform motion delta while preserving initial offset
            Vector3 platformDelta = targetPlatform.position - lastPlatformPosition;
            transform.position += platformDelta;
            lastPlatformPosition = targetPlatform.position;
        }
        else
        {
            // In Edit mode, set world position based directly on slider offsets relative to target platform
            transform.position = targetPlatform.position + new Vector3(offsetX, offsetY, offsetZ);
            lastPlatformPosition = targetPlatform.position;
        }
    }

    [ContextMenu("Snap To Center")]
    public void SnapToPlatformCenter()
    {
        if (targetPlatform == null) return;

        offsetX = 0f;
        offsetZ = 0f;
        transform.position = targetPlatform.position + new Vector3(offsetX, offsetY, offsetZ);
        lastPlatformPosition = targetPlatform.position;
    }
}