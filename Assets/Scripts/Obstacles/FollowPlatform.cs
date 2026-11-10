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

    /// <summary>Stop riding the platform (e.g. the obstacle fell off it).</summary>
    public void Detach()
    {
        targetPlatform = null;
        initialized = false;
    }

    /// <summary>Start riding a (new) platform from where we are now.</summary>
    public void AttachTo(Transform platform)
    {
        targetPlatform = platform;
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

            // 4. Apply platform rotation delta onto object's existing rotation
            transform.rotation = rotationDelta * transform.rotation;

            lastPlatformPosition = targetPlatform.position;
            lastPlatformRotation = targetPlatform.rotation;
        }
        else
        {
            ObstacleBase obstacle = GetComponent<ObstacleBase>();
            KeepHandMadeMoves(obstacle);

            // Calculate position using local position offset
            Vector3 localOffset = new Vector3(offsetX, offsetY, offsetZ);
            transform.position = targetPlatform.position + (targetPlatform.rotation * localOffset);

            // Fetch initial Y-rotation slider value if present
            float yRot = obstacle != null ? obstacle.initialYRotation : 0f;

            // Apply target platform rotation PLUS the local Y-rotation offset
            transform.rotation = targetPlatform.rotation * Quaternion.Euler(0f, yRot, 0f);

            lastPlatformPosition = targetPlatform.position;
            lastPlatformRotation = targetPlatform.rotation;
            lastSetPosition = transform.position;
            lastSetRotation = transform.rotation;
            lastSetTurn = yRot;
        }
    }

    // Where this script last put the obstacle before Play (so a hand-made move can be spotted)
    [System.NonSerialized] private Vector3? lastSetPosition;
    [System.NonSerialized] private Quaternion lastSetRotation;
    [System.NonSerialized] private float lastSetTurn;

    /// <summary>
    /// Before Play, the obstacle is put at its offsets on the platform every update. If someone moved or
    /// turned it in the Scene view (or with the Game Builder's Position buttons), keep that: copy it into the offsets.
    /// </summary>
    private void KeepHandMadeMoves(ObstacleBase obstacle)
    {
        if (!lastSetPosition.HasValue) return;
        bool moved = (transform.position - lastSetPosition.Value).sqrMagnitude > 0.000001f;
        // (A changed 'Initial Y Rotation' slider also turns it: that's not a hand-made turn)
        bool sliderChanged = obstacle != null && !Mathf.Approximately(obstacle.initialYRotation, lastSetTurn);
        bool turned = !sliderChanged && Quaternion.Angle(transform.rotation, lastSetRotation) > 0.01f;
        if (!moved && !turned) return;

        if (moved)
        {
            Vector3 local = Quaternion.Inverse(targetPlatform.rotation) * (transform.position - targetPlatform.position);
            offsetX = local.x;
            offsetY = local.y;
            offsetZ = local.z;
        }
        if (turned && obstacle != null)
        {
            obstacle.initialYRotation = Mathf.Repeat((Quaternion.Inverse(targetPlatform.rotation) * transform.rotation).eulerAngles.y, 360f);
        }
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        if (obstacle != null) UnityEditor.EditorUtility.SetDirty(obstacle);
#endif
    }

    [ContextMenu("Snap To Center")]
    public void SnapToPlatformCenter()
    {
        if (targetPlatform == null) return;

        offsetX = 0f;
        offsetZ = 0f;
        
        Vector3 localOffset = new Vector3(offsetX, offsetY, offsetZ);
        transform.position = targetPlatform.position + (targetPlatform.rotation * localOffset);

        ObstacleBase obstacle = GetComponent<ObstacleBase>();
        float yRot = obstacle != null ? obstacle.initialYRotation : 0f;
        transform.rotation = targetPlatform.rotation * Quaternion.Euler(0f, yRot, 0f);

        lastPlatformPosition = targetPlatform.position;
        lastPlatformRotation = targetPlatform.rotation;
    }
}