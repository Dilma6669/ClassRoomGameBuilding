using UnityEngine;

[ExecuteAlways]
public class FollowPlatform : MonoBehaviour
{
    public Transform targetPlatform;
    public float heightOffset = 1f;

    // Persistent target ID saved in the scene so Play mode resets won't trigger a snap
    [HideInInspector] public string snappedTargetID = "";

    private Vector3 lastPlatformPosition;

    private void Start()
    {
        if (targetPlatform != null)
        {
            lastPlatformPosition = targetPlatform.position;
        }
    }

    private void OnValidate()
    {
        // Never snap during Play mode or during scene play transitions
        if (Application.isPlaying) return;

        if (targetPlatform != null)
        {
            // Get unique Unity Instance ID for the assigned target transform
            string currentTargetID = targetPlatform.GetInstanceID().ToString();

            // Snap ONLY if a completely new target transform was assigned in the Inspector
            if (snappedTargetID != currentTargetID)
            {
                snappedTargetID = currentTargetID;
                SnapToPlatformCenter();
            }
        }
        else
        {
            snappedTargetID = "";
        }
    }

    private void LateUpdate()
    {
        if (targetPlatform == null) return;

        // In Edit mode, track platform movement without overriding local position changes
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