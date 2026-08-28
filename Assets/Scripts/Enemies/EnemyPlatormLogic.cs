using KinematicCharacterController.Examples;
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

    [Header("Surface Height Snapping")]
    [Tooltip("Distance above local feet to cast down from.")]
    [Range(0.1f, 5f)] public float raycastOriginHeight = 1.5f;

    [Tooltip("Height offset above the mesh surface.")]
    [Range(0f, 2f)] public float surfaceOffset = 0.5f;

    [Tooltip("How fast it steps up/down over bumps.")]
    [Range(1f, 50f)] public float stepUpSpeed = 15f;

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
    private Rigidbody rigidbodyComponent;
    private Transform platformTransform;

    // Local offset relative to the platform calculated at game start
    private Vector3 initialPlatformLocalHome;

    private void Start()
    {
        lastPatrolOffset = Vector3.zero;
        currentEnemyOffsetFromHome = Vector3.zero;
        previousMovementType = movementType;

        if (transform.parent != null)
        {
            ExampleMovingPlatform movingPlatform = transform.parent.GetComponentInChildren<ExampleMovingPlatform>();
            if (movingPlatform != null)
            {
                platformTransform = movingPlatform.transform;
            }
        }

        if (Application.isPlaying)
        {
            Transform refTransform = platformTransform != null ? platformTransform : transform.parent;
            if (refTransform != null)
            {
                initialPlatformLocalHome = refTransform.InverseTransformPoint(transform.position);
            }
        }

        enemyCollider = GetComponent<Collider>();
        if (enemyCollider != null)
        {
            enemyCollider.hideFlags = HideFlags.HideInInspector;
        }
        
        rigidbodyComponent = GetComponent<Rigidbody>();
        if (rigidbodyComponent != null)
        {
            rigidbodyComponent.hideFlags = HideFlags.HideInInspector;
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
            SnapToSurfaceHeight();
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

        SnapToSurfaceHeight();
    }

    private void SnapToSurfaceHeight()
    {
        // Cast from higher up to avoid starting inside collider geometry when walking up steep bumps
        Vector3 rayOrigin = transform.position + (Vector3.up * raycastOriginHeight);
        float rayLength = raycastOriginHeight * 4f;

        RaycastHit[] hits = Physics.RaycastAll(rayOrigin, Vector3.down, rayLength, Physics.AllLayers, QueryTriggerInteraction.Ignore);
        
        // Sort hits by distance to ensure we get the highest surface beneath the origin
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == enemyCollider || hit.transform.IsChildOf(transform)) continue;

            float targetY = hit.point.y + surfaceOffset;
            Vector3 currentPos = transform.position;

            currentPos.y = Mathf.Lerp(currentPos.y, targetY, Time.deltaTime * stepUpSpeed);

            transform.position = currentPos;
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

        float distanceToTarget = vectorToTarget.magnitude;

        if (distanceToTarget < 0.2f)
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

        float stepDistance = Mathf.Min(moveSpeed * Time.deltaTime, distanceToTarget);
        Vector3 worldMoveDelta = moveDir * stepDistance;

        currentEnemyOffsetFromHome += worldMoveDelta;

        transform.position += worldMoveDelta;
    }

    private void UpdateReturnToHomeMovement()
    {
        Vector3 vectorToHome = -currentEnemyOffsetFromHome;
        vectorToHome.y = 0f;

        float distanceToHome = vectorToHome.magnitude;

        if (distanceToHome < 0.25f)
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

        float stepDistance = Mathf.Min(moveSpeed * Time.deltaTime, distanceToHome);
        Vector3 worldMoveDelta = moveDir * stepDistance;

        currentEnemyOffsetFromHome += worldMoveDelta;

        transform.position += worldMoveDelta;
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
            Vector3 homeWorldPos;

            if (Application.isPlaying)
            {
                Transform refTransform = platformTransform;
                if (refTransform == null && transform.parent != null)
                {
                    ExampleMovingPlatform movingPlatform = transform.parent.GetComponentInChildren<ExampleMovingPlatform>();
                    if (movingPlatform != null)
                    {
                        refTransform = movingPlatform.transform;
                    }
                }

                homeWorldPos = (refTransform != null) ? refTransform.TransformPoint(initialPlatformLocalHome) : (transform.position - currentEnemyOffsetFromHome);
            }
            else
            {
                homeWorldPos = transform.position;
            }

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