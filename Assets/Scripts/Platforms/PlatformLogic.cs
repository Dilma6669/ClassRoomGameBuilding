using UnityEngine;
using KinematicCharacterController;
using KinematicCharacterController.Examples;
#if UNITY_EDITOR
using UnityEditor;
#endif

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

    [Header("Enemy Spawning Setup")]
    [Tooltip("Find this prefab in: Assets/Prefabs/Enemies/EnemyLogic")]
    public GameObject enemyPrefab;
    [Range(0f, 5f)] public float enemySpawnHeightOffset = 0.5f;

    [Header("Obstacle Spawning Setup")]
    [Tooltip("Find this prefab in: Assets/Prefabs/Obstacles/ObstacleLogic")]
    public GameObject obstaclePrefab;
    [Range(0f, 5f)] public float obstacleSpawnHeightOffset = 0.5f;

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

        // Disable rotations & secondary oscillations
        childMover.RotSpeed = 0f;
        childMover.OscillationPeriod = 0f;
        childMover.OscillationSpeed = 0f;

        Vector3 dir = MoveDirection;
        if (initialDirection == InitialDirection.Reverse)
        {
            dir = -dir;
        }

        // Calculate time needed to complete full round-trip (start -> end -> start)
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

    [ContextMenu("Create Enemy On Platform")]
    public void CreateEnemyOnPlatform()
    {
        if (enemyPrefab == null)
        {
            Debug.LogWarning("⚠️ Cannot create enemy! Please drag an Enemy Prefab into the 'Enemy Prefab' slot on the Platform script first.");
            return;
        }

        FindChildComponents();
        if (childMover == null) return;

        Vector3 spawnPosition = childMover.transform.position + new Vector3(0f, (height / 2f) + enemySpawnHeightOffset, 0f);

#if UNITY_EDITOR
        GameObject newEnemy = (GameObject)PrefabUtility.InstantiatePrefab(enemyPrefab);
        newEnemy.transform.position = spawnPosition;
        newEnemy.transform.SetParent(transform);
        Undo.RegisterCreatedObjectUndo(newEnemy, "Create Enemy On Platform");
#else
        GameObject newEnemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity, transform);
#endif

        FollowPlatform followLogic = newEnemy.GetComponent<FollowPlatform>();
        if (followLogic != null)
        {
            followLogic.targetPlatform = childMover.transform;
        }
    }

    [ContextMenu("Create Obstacle On Platform")]
    public void CreateObstacleOnPlatform()
    {
        if (obstaclePrefab == null)
        {
            Debug.LogWarning("⚠️ Cannot create obstacle! Please drag an Obstacle Prefab into the 'Obstacle Prefab' slot on the Platform script first.");
            return;
        }

        FindChildComponents();
        if (childMover == null) return;

        Vector3 spawnPosition = childMover.transform.position + new Vector3(0f, (height / 2f) + obstacleSpawnHeightOffset, 0f);

#if UNITY_EDITOR
        GameObject newObstacle = (GameObject)PrefabUtility.InstantiatePrefab(obstaclePrefab);
        newObstacle.transform.position = spawnPosition;
        newObstacle.transform.SetParent(transform);
        Undo.RegisterCreatedObjectUndo(newObstacle, "Create Obstacle On Platform");
#else
        GameObject newObstacle = Instantiate(obstaclePrefab, spawnPosition, Quaternion.identity, transform);
#endif

        ObstacleLogic obstacleLogic = newObstacle.GetComponent<ObstacleLogic>();
        if (obstacleLogic != null)
        {
            obstacleLogic.targetPlatform = childMover.transform;
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