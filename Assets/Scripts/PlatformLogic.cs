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

    [Header("Movement Settings")]
    public bool enableMovement = false;
    public InitialDirection initialDirection = InitialDirection.Forward;
    [Range(0f, 50f)] public float moveDistance = 5f;
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

    public Vector3 MoveDirection
    {
        get
        {
            Vector3 dir = new Vector3(moveX ? 1f : 0f, moveY ? 1f : 0f, moveZ ? 1f : 0f);
            return dir.sqrMagnitude > 0 ? dir.normalized : Vector3.right;
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
            childMover = GetComponentInChildren<ExampleMovingPlatform>();
    }

    private void Start()
    {
        EnsureParentScaleReset();
        FindChildComponents();

        if (Application.isPlaying && childMover != null)
        {
            // Cache fixed Gizmo track relative to initial placement
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
        if (!enableMovement)
        {
            childMover.TranslationPeriod = 0f;
            childMover.TranslationSpeed = 0f;
            childMover.RotSpeed = 0f;
            childMover.OscillationPeriod = 0f;
            childMover.OscillationSpeed = 0f;
            return;
        }

        // Disable rotations & secondary oscillations
        childMover.RotSpeed = 0f;
        childMover.OscillationPeriod = 0f;
        childMover.OscillationSpeed = 0f;

        Vector3 dir = MoveDirection;
        if (initialDirection == InitialDirection.Reverse)
        {
            dir = -dir;
        }

        // Configure sine movement directly on ExampleMovingPlatform
        childMover.TranslationAxis = dir;
        childMover.TranslationPeriod = moveDistance / 2f;
        childMover.TranslationSpeed = moveSpeed;
    }

    private void Update()
    {
        EnsureParentScaleReset();

        FindChildComponents();
        if (childMover == null) return;

        // Apply starting position offset in Edit mode only
        if (!Application.isPlaying)
        {
            childMover.transform.localPosition = new Vector3(offsetX, offsetY, offsetZ);
        }

        // Scale only the child object
        Vector3 targetScale = new Vector3(width, height, depth);
        if (childMover.transform.localScale != targetScale)
        {
            childMover.transform.localScale = targetScale;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (enableMovement && childMover != null)
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