using UnityEngine;

[ExecuteAlways]
public class ObstacleLogic : MonoBehaviour
{
    [Header("Platform Target")]
    public Transform targetPlatform;

    [Header("Placement Setup")]
    public float heightOffset = 0.5f;

    [Header("Physics Setup")]
    public bool enableRigidbody = false;

    // Persistent target ID saved in the scene so Play mode resets won't trigger a snap
    [HideInInspector] public string snappedTargetID = "";

    private Vector3 lastPlatformPosition;
    private Rigidbody rb;
    private Transform childFolder;

    private void Awake()
    {
        FindChildFolder();
        ToggleRigidbody();
    }

    private void Start()
    {
        FindChildFolder();
        ToggleRigidbody();

        if (targetPlatform != null)
        {
            lastPlatformPosition = targetPlatform.position;
        }
    }

    private void FindChildFolder()
    {
        if (childFolder != null) return;

        // Try to find existing "⚠️ DO NOT TOUCH" child folder
        childFolder = transform.Find("⚠️ DO NOT TOUCH");

        // Fallback: grab first available child transform if present
        if (childFolder == null && transform.childCount > 0)
        {
            childFolder = transform.GetChild(0);
        }
    }

    private void OnValidate()
    {
        FindChildFolder();
        ToggleRigidbody();

        // Never snap during Play mode or play transitions
        if (Application.isPlaying) return;

        if (targetPlatform != null)
        {
            string currentTargetID = targetPlatform.GetInstanceID().ToString();

            // Snap ONLY if a completely new target transform was assigned
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

    private void ToggleRigidbody()
    {
        // Check for Rigidbody on children (or parent as fallback)
        rb = GetComponentInChildren<Rigidbody>();

        GameObject targetTargetGo = childFolder != null ? childFolder.gameObject : gameObject;

        if (enableRigidbody)
        {
            if (rb == null)
            {
                rb = targetTargetGo.AddComponent<Rigidbody>();
            }
            rb.isKinematic = false;
            rb.useGravity = true;
        }
        else
        {
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }
        }
    }

    private void LateUpdate()
    {
        if (targetPlatform == null) return;

        // In Edit mode, track platform movement so the prop follows if the platform moves
        if (!Application.isPlaying)
        {
            Vector3 editorDelta = targetPlatform.position - lastPlatformPosition;
            transform.position += editorDelta;
            lastPlatformPosition = targetPlatform.position;
            return;
        }

        // Keep position relative if platform moves at runtime
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