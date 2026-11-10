using UnityEngine;

#if HAS_KINEMATIC_CC
using KinematicCharacterController;
using KinematicCharacterController.Examples;
#endif

[ExecuteAlways]
public class PlatformLogic : MonoBehaviour
{
    public enum InitialDirection { Forward, Reverse }

    [Header("Platform Size & Orientation")]
    [Range(0.1f, 500f)] public float uniformScale = 1f;
    [Range(0.5f, 500f)] public float widthOffset = 3f;
    [Range(0.2f, 500f)] public float heightOffset = 3f;
    [Range(0.5f, 500f)] public float depthOffset = 3f;
    [Range(0f, 360f)] public float rotationY = 0f;

    [Header("Starting Position Offset")]
    [Range(-500f, 500f)] public float offsetX = 0f;
    [Range(-500f, 500f)] public float offsetY = 0f;
    [Range(-500f, 500f)] public float offsetZ = 0f;

    // Toggles
    [HideInInspector] public bool moveX = true;
    [HideInInspector] public bool moveY = false;
    [HideInInspector] public bool moveZ = false;
    [HideInInspector] public bool enableRotation = false;
    [HideInInspector] public bool enableScaling = false;

    // Movement Settings
    public InitialDirection initialDirection = InitialDirection.Forward;
    [Range(0f, 1000f)] public float moveDistance = 0f;
    [Range(0.1f, 1000f)] public float moveSpeed = 2f;

    // Rotation Settings
    [Range(-3600f, 3600f)]
    [Tooltip("Degrees per second to rotate around the Y axis.")]
    public float rotationSpeedY = 90f;

    // Scale Settings (X & Z only)
    [Range(0.1f, 100f)] public float scaleSpeed = 2f;
    [Range(0.1f, 50f)] public float minScale = 0.5f;
    [Range(0.1f, 50f)] public float maxScale = 1.5f;

    public enum PlatformType { Normal, Crumbling, Bouncy }

    [Header("Special Platform")]
    [Tooltip("Normal, Crumbling (falls soon after you stand on it), or Bouncy (launches you up).")]
    public PlatformType platformType = PlatformType.Normal;
    [Tooltip("Seconds of shaking before it falls.")]
    [Range(0.2f, 60f)] public float crumbleAfter = 2f;
    [Tooltip("Seconds until it comes back. 0 = gone for good.")]
    [Range(0f, 300f)] public float comeBackAfter = 3f;
    [Tooltip("How high it launches you.")]
    [Range(5f, 500f)] public float bounceStrength = 20f;
    [Tooltip("Landing on it never hurts, however far you fell. Bouncy platforms have it on, so you can bounce from one to the next.")]
    [ShowAs("🪂 Landing on it never hurts")]
    public bool safeLanding = false;
    [Tooltip("Colour it so players can tell it's special.")]
    public bool colourIt = true;

    [Header("Effects")]
    [Tooltip("Sparkles when the player lands on it.")]
    public Sparkles.Effect landEffect = Sparkles.Effect.None;
    [Tooltip("Crumbling: sparkles when it breaks.")]
    public Sparkles.Effect breakEffect = Sparkles.Effect.Spray;
    [Tooltip("The sparkles' colour.")]
    public Color effectColour = new Color(1f, 0.85f, 0.5f);
    [Tooltip("How many sparkles (1 = normal).")]
    [Range(0.2f, 20f)] public float effectAmount = 1f;
    [Tooltip("How big the sparkles are (1 = normal).")]
    [Range(0.3f, 10f)] public float effectSize = 1f;
    [Tooltip("Crumbling: flash while it shakes, so players can count down to it breaking.")]
    public bool warningFlash = true;
    [Tooltip("Seconds between flashes (1 = one flash every second).")]
    [Range(0.2f, 2f)] public float flashEvery = 1f;
    public Color flashColour = Color.white;

    public static readonly Color CrumblingColour = new Color(0.78f, 0.55f, 0.3f);
    public static readonly Color BouncyColour = new Color(0.3f, 0.95f, 0.45f);

    private PlatformType tintedAs = PlatformType.Normal;
    private bool tintedColourIt;
    private int tintCheckFrame;
    private static MaterialPropertyBlock tintBlock;

#if HAS_KINEMATIC_CC
    private ExampleMovingPlatform childMover;
#else
    private Transform fallbackChildTransform;
#endif

    // Base dimensions saved prior to uniform scaling
    [SerializeField, HideInInspector] private Vector3 baseScale = new Vector3(3f, 3f, 3f);
    private float lastUniformScale = 1f;

    // Gizmo path caching for Play mode
    private Vector3 cachedGizmoStart;
    private Vector3 cachedGizmoEnd;
    private bool hasCachedGizmos = false;

    public bool HasActiveAxis => moveX || moveY || moveZ;

    public Vector3 MoveDirection
    {
        get
        {
            Vector3 rawLocalDir = new Vector3(moveX ? 1f : 0f, moveY ? 1f : 0f, moveZ ? 1f : 0f);
            if (rawLocalDir.sqrMagnitude <= 0f) return Vector3.zero;

            Vector3 normalizedLocalDir = rawLocalDir.normalized;

            Transform target = TargetChild;
            if (target != null)
            {
                return target.TransformDirection(normalizedLocalDir);
            }

            return Quaternion.Euler(0f, rotationY, 0f) * normalizedLocalDir;
        }
    }

    public Transform TargetChild
    {
        get
        {
            // A just-created platform may not have looked for its moving part yet
            FindChildComponents();
#if HAS_KINEMATIC_CC
            return childMover != null ? childMover.transform : null;
#else
            return fallbackChildTransform;
#endif
        }
    }

    private void Awake()
    {
        EnsureParentScaleReset();
        FindChildComponents();
        lastUniformScale = uniformScale;
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
#if HAS_KINEMATIC_CC
        if (childMover == null)
            childMover = GetComponentInChildren<ExampleMovingPlatform>(true);

        if (childMover == null && transform.childCount > 0)
            childMover = transform.GetChild(0).GetComponent<ExampleMovingPlatform>();
#else
        if (fallbackChildTransform == null && transform.childCount > 0)
        {
            fallbackChildTransform = transform.GetChild(0);
        }
#endif
    }

    private void Start()
    {
        EnsureParentScaleReset();
        FindChildComponents();

        Transform target = TargetChild;
        if (Application.isPlaying && target != null)
        {
            Vector3 initialPos = target.position;
            Vector3 dir = MoveDirection;
            float halfDist = moveDistance / 2f;

            cachedGizmoStart = initialPos - (dir * halfDist);
            cachedGizmoEnd = initialPos + (dir * halfDist);
            hasCachedGizmos = true;

            ConfigureChildMover();

            bool needsSpecial = platformType != PlatformType.Normal || landEffect != Sparkles.Effect.None;
            if (needsSpecial && GetComponent<SpecialPlatform>() == null)
            {
                gameObject.AddComponent<SpecialPlatform>();
            }
        }
    }

    /// <summary>Tints crumbling/bouncy platforms (not saved: re-applied whenever the level loads).</summary>
    private void UpdateTint()
    {
        bool wantTint = platformType != PlatformType.Normal && colourIt;
        bool changed = platformType != tintedAs || colourIt != tintedColourIt;
        // Re-check now and then too, in case the platform got a new model
        if (!changed && !(wantTint && ++tintCheckFrame % 30 == 0)) return;

        Transform target = TargetChild;
        if (target == null) return;
        if (tintBlock == null) tintBlock = new MaterialPropertyBlock();

        Color colour = platformType == PlatformType.Bouncy ? BouncyColour : CrumblingColour;
        colour.a = SeeThrough.AlphaFor(target);
        foreach (Renderer r in target.GetComponentsInChildren<Renderer>(true))
        {
            if (r.GetComponentInParent<PlatformSceneryHolder>(true) != null) continue; // trees on it keep their own colours
            if (!wantTint) { r.SetPropertyBlock(null); continue; }
            r.GetPropertyBlock(tintBlock);
            tintBlock.SetColor("_Color", colour);
            tintBlock.SetColor("_BaseColor", colour);
            r.SetPropertyBlock(tintBlock);
        }
        tintedAs = platformType;
        tintedColourIt = colourIt;
    }

    private void ConfigureChildMover()
    {
#if HAS_KINEMATIC_CC
        if (childMover == null) return;

        // Apply continuous Y rotation to ExampleMovingPlatform if enabled
        childMover.RotationAxis = Vector3.up;
        childMover.RotSpeed = enableRotation ? rotationSpeedY : 0f;

        // Clear ExampleMovingPlatform's oscillation built-ins so it doesn't displace position
        childMover.OscillationPeriod = 0f;
        childMover.OscillationSpeed = 0f;

        if (!HasActiveAxis || moveDistance <= 0f)
        {
            childMover.TranslationPeriod = 0f;
            childMover.TranslationSpeed = 0f;
            return;
        }

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
#endif
    }

    private void OnValidate()
    {
        if (!Mathf.Approximately(uniformScale, lastUniformScale))
        {
            widthOffset = Mathf.Clamp(baseScale.x * uniformScale, 0.5f, 500f);
            heightOffset = Mathf.Clamp(baseScale.y * uniformScale, 0.2f, 500f);
            depthOffset = Mathf.Clamp(baseScale.z * uniformScale, 0.5f, 500f);
            lastUniformScale = uniformScale;
        }
        else
        {
            float safeScale = Mathf.Max(0.01f, uniformScale);
            baseScale = new Vector3(widthOffset / safeScale, heightOffset / safeScale, depthOffset / safeScale);
        }
    }

    // What this script last put on the moving part (so a move made with the Move/Rotate tool can be spotted)
    [System.NonSerialized] private Vector3? lastAppliedPosition;
    [System.NonSerialized] private float lastAppliedTurn;

    /// <summary>
    /// Before Play, the platform is put at its Offset X/Y/Z every update. If someone dragged it (or turned it)
    /// in the Scene view since last time, keep that: copy the new spot into the offsets instead of snapping back.
    /// </summary>
    private void KeepHandMadeMoves(Transform target)
    {
        if (!lastAppliedPosition.HasValue) return;
        bool moved = (target.localPosition - lastAppliedPosition.Value).sqrMagnitude > 0.000001f;
        float turn = target.localEulerAngles.y;
        bool turned = Mathf.Abs(Mathf.DeltaAngle(turn, lastAppliedTurn)) > 0.01f;
        if (!moved && !turned) return;

        if (moved)
        {
            offsetX = target.localPosition.x;
            offsetY = target.localPosition.y;
            offsetZ = target.localPosition.z;
        }
        if (turned) rotationY = Mathf.Repeat(turn, 360f);
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    private void Update()
    {
        EnsureParentScaleReset();

        FindChildComponents();
        Transform target = TargetChild;
        if (target == null) return;

        UpdateTint();

        if (Application.isPlaying)
        {
            if (enableScaling)
            {
                // Smoothly oscillate between minScale and maxScale over time
                float sineWave = (Mathf.Sin(Time.time * scaleSpeed) + 1f) * 0.5f;
                float scaleFactor = Mathf.Lerp(minScale, maxScale, sineWave);

                target.localScale = new Vector3(widthOffset * scaleFactor, heightOffset, depthOffset * scaleFactor);
            }
            else
            {
                target.localScale = new Vector3(widthOffset, heightOffset, depthOffset);
            }
        }
        else
        {
            KeepHandMadeMoves(target);
            target.localPosition = new Vector3(offsetX, offsetY, offsetZ);
            target.localRotation = Quaternion.Euler(0f, rotationY, 0f);
            lastAppliedPosition = target.localPosition;
            lastAppliedTurn = rotationY;

            Vector3 targetScale = new Vector3(widthOffset, heightOffset, depthOffset);
            if (target.localScale != targetScale)
            {
                target.localScale = targetScale;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Transform target = TargetChild;
        if (HasActiveAxis && target != null)
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
                Vector3 centerPos = target.position;
                Vector3 dir = MoveDirection;
                float halfDist = moveDistance / 2f;

                startPos = centerPos - (dir * halfDist);
                endPos = centerPos + (dir * halfDist);
            }

            Gizmos.DrawLine(startPos, endPos);

            Matrix4x4 oldMatrix = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(startPos, target.rotation, target.localScale);
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);

            Gizmos.matrix = Matrix4x4.TRS(endPos, target.rotation, target.localScale);
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);

            Gizmos.matrix = oldMatrix;
        }
    }
}