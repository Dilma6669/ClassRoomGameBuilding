using UnityEngine;

[ExecuteAlways]
public class FollowPlatform : MonoBehaviour
{
    public Transform targetPlatform;
    public float heightOffset = 1f;

    private Vector3 lastPlatformPosition;

    private void Start()
    {
        if (targetPlatform != null)
        {
            lastPlatformPosition = targetPlatform.position;
        }
    }

    private void LateUpdate()
    {
        if (targetPlatform == null) return;

        // In Edit mode, keep the enemy anchored cleanly above the platform
        if (!Application.isPlaying)
        {
            SnapToPlatformCenter();
            lastPlatformPosition = targetPlatform.position;
            return;
        }

        // Calculate how much the platform moved this frame
        Vector3 platformDelta = targetPlatform.position - lastPlatformPosition;

        // Apply that exact movement delta to the enemy
        transform.position += platformDelta;

        // Store platform position for next frame
        lastPlatformPosition = targetPlatform.position;
    }

    [ContextMenu("Snap To Platform")]
    public void SnapToPlatformCenter()
    {
        if (targetPlatform == null) return;

        // Snap enemy directly to platform center + height offset
        transform.position = targetPlatform.position + new Vector3(0f, heightOffset, 0f);
    }
}