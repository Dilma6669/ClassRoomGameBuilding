using UnityEngine;

[ExecuteAlways]
public class EnemyPlatformLogic : MonoBehaviour
{
    public enum MovementType { Idle, Patrol, RandomWander }

    [Header("Movement Mode")]
    [Tooltip("How this enemy moves around (like walking back and forth or wandering around).")]
    public MovementType movementType = MovementType.Patrol;

    [Header("General Movement Speed")]
    [Tooltip("How fast the enemy moves (higher numbers mean a faster walk or chase).")]
    [Range(0.1f, 20f)] public float moveSpeed = 3f;

    [Header("Patrol Settings")]
    [Tooltip("How far back and forth the enemy walks when patrolling.")]
    [Range(0f, 20f)] public float moveDistance = 3f;

    [Header("Idle Settings")]
    [Tooltip("Which direction the enemy faces when standing still or starting out (0 to 360 degrees).")]
    [Range(0f, 360f)] public float rotationAngle = 0f;

    [Header("Wandering Settings")]
    [Tooltip("How big of an area the enemy is allowed to explore when wandering.")]
    [Range(1f, 30f)] public float wanderRadius = 3f;

    // Advanced / Internal Movement Variables
    [Range(15f, 180f)] private float maxTurnAngle = 90f;
    [Range(60f, 360f)] private float turnSpeedMultiplier = 120f;

    private const float MIN_WANDER_DISTANCE = 2.0f;

    private Vector3 lastPatrolOffset;
    private Vector3 currentWanderOffsetFromHome;
    private Vector3 currentEnemyOffsetFromHome;
    private MovementType previousMovementType;
    private bool isReturningHome = false;

    private int patrolDirection = 1;

    private Collider enemyCollider;
    private Collider playerCollider;
    private Transform childPhysicsObject;
    private Rigidbody rigidbody;

    private void Start()
    {
        lastPatrolOffset = Vector3.zero;
        currentEnemyOffsetFromHome = Vector3.zero;
        previousMovementType = movementType;

        enemyCollider = GetComponent<Collider>();
        if (enemyCollider != null)
        {
            enemyCollider.hideFlags = HideFlags.HideInInspector;
        }
        
        rigidbody = GetComponent<Rigidbody>();
        if (rigidbody != null)
        {
            rigidbody.hideFlags = HideFlags.HideInInspector;
        }

        Transform childTransform = transform.childCount > 0 ? transform.GetChild(0) : null;
        if (childTransform != null)
        {
            childPhysicsObject = childTransform;
        }

        if (movementType == MovementType.RandomWander)
        {
            PickNewWanderTarget();
        }
        else
        {
            ApplyRotation();
        }
    }

    private void Update()
    {
        if (movementType != previousMovementType)
        {
            ResetReturnHomeState();
            if (movementType == MovementType.RandomWander)
            {
                currentEnemyOffsetFromHome = Vector3.zero;
                PickNewWanderTarget();
            }
            else
            {
                ApplyRotation();
            }
            previousMovementType = movementType;
        }

        if (!Application.isPlaying)
        {
            if (movementType != MovementType.RandomWander)
            {
                ApplyRotation();
            }
            lastPatrolOffset = Vector3.zero;
            return;
        }

        if (isReturningHome)
        {
            UpdateReturnToHomeMovement();
            return;
        }

        switch (movementType)
        {
            case MovementType.Patrol:
                UpdatePatrolMovement();
                break;

            case MovementType.RandomWander:
                UpdateWanderMovement();
                break;

            case MovementType.Idle:
                lastPatrolOffset = Vector3.zero;
                break;
        }
    }

    public void HandleCollision(Collision collision)
    {
        if (!Application.isPlaying || isReturningHome) return;

        if (collision.gameObject.name == "ExampleCharacter")
        {
            playerCollider = collision.collider;
            if (enemyCollider != null && playerCollider != null)
            {
                Physics.IgnoreCollision(enemyCollider, playerCollider, true);
            }

            isReturningHome = true;
        }
    }

    private void AlignChildToParent()
    {
        if (childPhysicsObject != null && childPhysicsObject != transform)
        {
            childPhysicsObject.localPosition = Vector3.zero;
            childPhysicsObject.localRotation = Quaternion.identity;
        }
    }

    private void ApplyRotation()
    {
        Vector3 currentEuler = transform.localEulerAngles;
        transform.localRotation = Quaternion.Euler(currentEuler.x, rotationAngle, currentEuler.z);
    }

    private void UpdatePatrolMovement()
    {
        float currentZ = lastPatrolOffset.z + (patrolDirection * moveSpeed * Time.deltaTime);

        if (currentZ >= moveDistance)
        {
            currentZ = moveDistance;
            patrolDirection = -1;
            AlignChildToParent();
        }
        else if (currentZ <= -moveDistance)
        {
            currentZ = -moveDistance;
            patrolDirection = 1;
            AlignChildToParent();
        }

        Vector3 targetPatrolOffset = new Vector3(0f, 0f, currentZ);
        Vector3 patrolDelta = targetPatrolOffset - lastPatrolOffset;

        transform.Translate(patrolDelta, Space.Self);
        lastPatrolOffset = targetPatrolOffset;
    }

    private void UpdateWanderMovement()
    {
        Vector3 vectorToTarget = currentWanderOffsetFromHome - currentEnemyOffsetFromHome;
        vectorToTarget.y = 0f;

        if (vectorToTarget.magnitude < 0.2f)
        {
            AlignChildToParent();
            currentEnemyOffsetFromHome = currentWanderOffsetFromHome;
            PickNewWanderTarget();
            return;
        }

        Vector3 moveDir = vectorToTarget.normalized;
        if (moveDir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
            float dynamicTurnSpeed = moveSpeed * turnSpeedMultiplier;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, dynamicTurnSpeed * Time.deltaTime);
        }

        Vector3 moveStep = Vector3.forward * moveSpeed * Time.deltaTime;
        Vector3 worldMoveDelta = transform.TransformDirection(moveStep);
        
        transform.Translate(moveStep, Space.Self);

        currentEnemyOffsetFromHome += worldMoveDelta;
    }

    private void UpdateReturnToHomeMovement()
    {
        Vector3 vectorToHome = -currentEnemyOffsetFromHome;
        vectorToHome.y = 0f;

        if (vectorToHome.magnitude < 0.25f)
        {
            ResetReturnHomeState();
            AlignChildToParent();

            currentEnemyOffsetFromHome = Vector3.zero;

            if (movementType == MovementType.RandomWander)
            {
                PickNewWanderTarget();
            }
            else if (movementType == MovementType.Patrol)
            {
                lastPatrolOffset = Vector3.zero;
                ApplyRotation();
            }
            return;
        }

        Vector3 moveDir = vectorToHome.normalized;
        if (moveDir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
            float dynamicTurnSpeed = moveSpeed * turnSpeedMultiplier;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, dynamicTurnSpeed * Time.deltaTime);
        }

        Vector3 moveStep = Vector3.forward * moveSpeed * Time.deltaTime;
        Vector3 worldMoveDelta = transform.TransformDirection(moveStep);

        transform.Translate(moveStep, Space.Self);
        currentEnemyOffsetFromHome += worldMoveDelta;
    }

    private void ResetReturnHomeState()
    {
        if (enemyCollider != null && playerCollider != null)
        {
            Physics.IgnoreCollision(enemyCollider, playerCollider, false);
            playerCollider = null;
        }
        isReturningHome = false;
    }

    [ContextMenu("Debug: Pick New Wander Target")]
    public void PickNewWanderTarget()
    {
        Vector3 potentialTarget = Vector3.zero;
        
        for (int i = 0; i < 15; i++)
        {
            Vector2 circle = Random.insideUnitCircle * wanderRadius;
            potentialTarget = new Vector3(circle.x, 0f, circle.y);
            
            if (Vector3.Distance(currentEnemyOffsetFromHome, potentialTarget) >= MIN_WANDER_DISTANCE)
            {
                break;
            }
        }
        
        currentWanderOffsetFromHome = potentialTarget;
    }

    private void OnDrawGizmosSelected()
    {
        if (movementType == MovementType.RandomWander || isReturningHome)
        {
            Vector3 homeWorldPos = transform.position - currentEnemyOffsetFromHome;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(homeWorldPos, wanderRadius);

            Vector3 activeTargetWorld = isReturningHome ? homeWorldPos : (homeWorldPos + currentWanderOffsetFromHome);
            Gizmos.color = isReturningHome ? Color.magenta : Color.cyan;
            Gizmos.DrawSphere(activeTargetWorld, 0.4f);
            Gizmos.DrawLine(transform.position, activeTargetWorld);
        }

        if (movementType == MovementType.Patrol && !isReturningHome)
        {
            Vector3 startPos = transform.position + transform.TransformDirection(new Vector3(0f, 0f, -moveDistance) - lastPatrolOffset);
            Vector3 endPos = transform.position + transform.TransformDirection(new Vector3(0f, 0f, moveDistance) - lastPatrolOffset);

            Gizmos.color = Color.red;
            Gizmos.DrawLine(startPos, endPos);
            Gizmos.DrawWireSphere(startPos, 0.3f);
            Gizmos.DrawWireSphere(endPos, 0.3f);
        }
    }
}