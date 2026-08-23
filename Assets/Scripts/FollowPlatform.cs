using UnityEngine;

[ExecuteAlways]
public class FollowPlatform : MonoBehaviour
{
    public Transform targetPlatform;
    public float heightOffset = 1f;

    private Vector3 lastPlatformPosition;
    private Transform previousPlatform;

    private void Start()
    {
        if (targetPlatform != null)
        {
            lastPlatformPosition = targetPlatform.position;
        }
    }

    private void OnValidate()
    {
        // Don't execute snapping logic when entering or running Play Mode
        if (Application.isPlaying) return;

        // Trigger snap ONLY when a new platform is assigned in the Inspector slot
        if (targetPlatform != null && targetPlatform != previousPlatform)
        {
            previousPlatform = targetPlatform;
            SnapToPlatformCenter();
        }
    }

    private void LateUpdate()
    {
        if (targetPlatform == null) return;

        // In Edit mode, track the platform's editor drag movement without overriding local position changes
        if (!Application.isPlaying)
        {
            Vector3 editorDelta = targetPlatform.position - lastPlatformPosition;
            transform.position += editorDelta;
            lastPlatformPosition = targetPlatform.position;
            return;
        }

        // Calculate runtime movement delta
        Vector3 platformDelta = targetPlatform.position - lastPlatformPosition;
        transform.position += platformDelta;
        lastPlatformPosition = targetPlatform.position;
    }

    [ContextMenu("Snap To Center")]
    public void SnapToPlatformCenter()
    {
        if (targetPlatform == null) return;

        transform.position = targetPlatform.position + new Vector3(0f, heightOffset, 0f);
        lastPlatformPosition = targetPlatform.position;
    }
}