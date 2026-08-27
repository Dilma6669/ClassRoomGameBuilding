using UnityEngine;
using KinematicCharacterController;
using KinematicCharacterController.Examples;

[ExecuteAlways]
public class PlatformLogic : MonoBehaviour
{
    public enum InitialDirection { Forward, Reverse }

    [Header("Platform Size")]
    [Range(0.5f, 20f)] public float width = 3f;
    [Range(0.2f, 5f)] public float height = 0.5f;
    [Range(0.5f, 20f)] public float depth = 3f;

    [Header("Starting Position Offset")]
    [Range(-20f, 20f)] public float offsetX = 0f;
    [Range(-20f, 20f)] public float offsetY = 0f;
    [Range(-20f, 20f)] public float offsetZ = 0f;

    // Movement Settings
    public InitialDirection initialDirection = InitialDirection.Forward;
    [Range(0f, 1000f)] public float moveDistance = 0f;
    [Range(0.1f, 10f)] public float moveSpeed = 2f;

    // Movement Toggles (X = Left/Right, Y = Up/Down, Z = Forward/Backward)
    [HideInInspector] public bool moveX = true;
    [HideInInspector] public bool moveY = false;
    [HideInInspector] public bool moveZ = false;

    private ExampleMovingPlatform childMover;

    // Gizmo path caching for Play mode
    private Vector3 cachedGizmoStart;
    private Vector3 cachedGizmoEnd;
    private bool hasCachedGizmos = false;

    public bool HasActiveAxis => moveX || moveY || moveZ;

    public Vector3 MoveDirection
    {
        get
        {
            Vector3 dir = new Vector3(moveX ? 1f : 0f, moveY ? 1f : 0f, moveZ ? 1f : 0f);
            return dir.sqrMagnitude > 0 ? dir.normalized : Vector3.zero;
        }
    }

    public Transform TargetChild => childMover != null ? childMover.transform : null;

    private void Awake()
    {
        EnsureParentScaleReset();
        FindChildComponents();
    }

    private void EnsureParentScaleReset()
    {
        if (transform.localScale != Vector3.one)
        {
            transform.localScale = Vector3.one;
        }
    }

    private void FindChildComponents()
    {
        if (childMover == null)
            childMover = GetComponentInChildren<ExampleMovingPlatform>(true);

        if (childMover == null && transform.childCount > 0)
            childMover = transform.GetChild(0).GetComponent<ExampleMovingPlatform>();
    }

    private void Start()
    {
        EnsureParentScaleReset();
        FindChildComponents();

        if (Application.isPlaying && childMover != null)
        {
            Vector3 initialPos = childMover.transform.position;
            Vector3 dir = MoveDirection;
            float halfDist = moveDistance / 2f;

            cachedGizmoStart = initialPos - (dir * halfDist);
            cachedGizmoEnd = initialPos + (dir * halfDist);
            hasCachedGizmos = true;

            ConfigureChildMover();
        }
    }

    private void ConfigureChildMover()
    {
        if (childMover == null) return;

        if (!HasActiveAxis || moveDistance <= 0f)
        {
            childMover.TranslationPeriod = 0f;
            childMover.TranslationSpeed = 0f;
            childMover.RotSpeed = 0f;
            childMover.OscillationPeriod = 0f;
            childMover.OscillationSpeed = 0f;
            return;
        }

        childMover.RotSpeed = 0f;
        childMover.OscillationPeriod = 0f;
        childMover.OscillationSpeed = 0f;

        Vector3 dir = MoveDirection;
        if (initialDirection == InitialDirection.Reverse)
        {
            dir = -dir;
        }

        float totalRoundTripDistance = moveDistance * 2f;
        float calculatedPeriod = totalRoundTripDistance / Mathf.Max(0.01f, moveSpeed);

        childMover.TranslationAxis = dir;
        childMover.TranslationPeriod = moveDistance / 2f;
        childMover.TranslationSpeed = (2f * Mathf.PI) / calculatedPeriod;
    }

    private void Update()
    {
        EnsureParentScaleReset();

        FindChildComponents();
        if (childMover == null) return;

        if (!Application.isPlaying)
        {
            childMover.transform.localPosition = new Vector3(offsetX, offsetY, offsetZ);
        }

        Vector3 targetScale = new Vector3(width, height, depth);
        if (childMover.transform.localScale != targetScale)
        {
            childMover.transform.localScale = targetScale;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (HasActiveAxis && childMover != null)
        {
            Gizmos.color = Color.cyan;
            Vector3 startPos, endPos;

            if (Application.isPlaying && hasCachedGizmos)
            {
                startPos = cachedGizmoStart;
                endPos = cachedGizmoEnd;
            }
            else
            {
                Vector3 centerPos = childMover.transform.position;
                Vector3 dir = MoveDirection;
                float halfDist = moveDistance / 2f;

                startPos = centerPos - (dir * halfDist);
                endPos = centerPos + (dir * halfDist);
            }

            Gizmos.DrawLine(startPos, endPos);
            Gizmos.DrawWireCube(startPos, childMover.transform.localScale);
            Gizmos.DrawWireCube(endPos, childMover.transform.localScale);
        }
    }
}