using UnityEngine;

[ExecuteAlways]
public class EnemyLogic : MonoBehaviour
{
    public enum MovementType { Idle, Patrol, RandomWander, TerrainWander }

    [Header("Movement Mode")]
    public MovementType movementType = MovementType.Patrol;

    [Header("Rotation Setup (Patrol/Idle Only)")]
    [Range(0f, 360f)] public float rotationAngle = 0f;

    [Header("Movement Speed (Units per Second)")]
    [Range(0.1f, 20f)] public float moveSpeed = 3f;

    [Header("Rotation Multiplier")]
    [Tooltip("Scales rotation speed proportionally to movement speed so fast turns match fast moves.")]
    [Range(60f, 360f)] public float turnSpeedMultiplier = 120f;

    [Header("Patrol Range")]
    [Range(0f, 20f)] public float moveDistance = 3f;

    [Header("Wander Setup (Platform Only)")]
    [Range(1f, 30f)] public float wanderRadius = 3f;

    [Header("Terrain Wander Setup")]
    [Tooltip("How often (in seconds) a new random wander angle is chosen.")]
    [Range(1f, 10f)] public float directionChangeInterval = 3f;
    [Tooltip("Maximum angle change (in degrees) when choosing a new heading.")]
    [Range(15f, 180f)] public float maxTurnAngle = 90f;

    [Header("Slope Detection")]
    [Tooltip("Maximum allowed ground angle (in degrees). Steeper slopes trigger a direction flip.")]
    [Range(10f, 60f)] public float maxWalkableSlope = 35f;
    [Tooltip("How far ahead to check terrain slope.")]
    [Range(0.2f, 2f)] public float slopeCheckDistance = 0.8f;

    private const float MIN_WANDER_DISTANCE = 2.0f;

    private Vector3 lastPatrolOffset;
    private Vector3 currentWanderOffsetFromHome;
    private Vector3 currentEnemyOffsetFromHome;
    private MovementType previousMovementType;
    private bool isReturningHome = false;

    // Terrain Wander State
    private Rigidbody parentRb;
    private float targetYAngle;
    private float directionTimer;
    private float slopeFlipCooldownTimer; // Prevents rapid spinning loops

    private int patrolDirection = 1;

    private Collider enemyCollider;
    private Collider playerCollider;
    private Transform childPhysicsObject;

    private void Start()
    {
        lastPatrolOffset = Vector3.zero;
        currentEnemyOffsetFromHome = Vector3.zero;
        previousMovementType = movementType;

        parentRb = GetComponent<Rigidbody>();
        if (parentRb != null)
        {
            parentRb.hideFlags = HideFlags.HideInInspector;
        }

        enemyCollider = GetComponent<Collider>();
        if (enemyCollider != null)
        {
            enemyCollider.hideFlags = HideFlags.HideInInspector;
        }

        Transform childTransform = transform.childCount > 0 ? transform.GetChild(0) : null;
        if (childTransform != null)
        {
            childPhysicsObject = childTransform;
        }

        if (movementType == MovementType.TerrainWander)
        {
            PickNewTerrainAngle();
        }
        else if (movementType != MovementType.RandomWander)
        {
            ApplyRotation();
        }

        if (movementType == MovementType.RandomWander)
        {
            PickNewWanderTarget();
        }
    }

    private void Update()
    {
        if (movementType != previousMovementType)
        {
            ResetReturnHomeState();
            if (movementType == MovementType.TerrainWander)
            {
                PickNewTerrainAngle();
            }
            else if (movementType == MovementType.RandomWander)
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
            if (movementType != MovementType.RandomWander && movementType != MovementType.TerrainWander)
            {
                ApplyRotation();
            }
            lastPatrolOffset = Vector3.zero;
            return;
        }

        if (movementType == MovementType.TerrainWander)
        {
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

    private void FixedUpdate()
    {
        if (!Application.isPlaying || movementType != MovementType.TerrainWander) return;

        if (parentRb == null)
        {
            parentRb = GetComponent<Rigidbody>();
            if (parentRb == null) return;
            parentRb.hideFlags = HideFlags.HideInInspector;
        }

        // Tick down timers
        directionTimer -= Time.fixedDeltaTime;
        if (slopeFlipCooldownTimer > 0f)
        {
            slopeFlipCooldownTimer -= Time.fixedDeltaTime;
        }

        // Only check slope if cooldown has elapsed
        if (slopeFlipCooldownTimer <= 0f && IsSlopeTooSteepAhead())
        {
            // Flip 180 degrees, reset interval timer, and enforce a 1.2s cooldown
            targetYAngle = Mathf.Repeat(targetYAngle + 180f, 360f);
            directionTimer = directionChangeInterval;
            slopeFlipCooldownTimer = 1.2f; 
        }
        else if (directionTimer <= 0f)
        {
            PickNewTerrainAngle();
        }

        // Smooth rotation around Y
        Quaternion targetRotation = Quaternion.Euler(0f, targetYAngle, 0f);
        Quaternion nextRotation = Quaternion.RotateTowards(parentRb.rotation, targetRotation, turnSpeedMultiplier * Time.fixedDeltaTime);
        parentRb.MoveRotation(nextRotation);

        // Movement on X/Z plane
        Vector3 forwardXz = Vector3.ProjectOnPlane(parentRb.transform.forward, Vector3.up).normalized;
        Vector3 currentPos = parentRb.position;
        Vector3 nextPos = currentPos + (forwardXz * moveSpeed * Time.fixedDeltaTime);
        nextPos.y = currentPos.y;

        parentRb.MovePosition(nextPos);
    }

    private bool IsSlopeTooSteepAhead()
    {
        Vector3 forwardXz = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        Vector3 checkOrigin = transform.position + (forwardXz * slopeCheckDistance) + (Vector3.up * 1.0f);

        if (Physics.Raycast(checkOrigin, Vector3.down, out RaycastHit hit, 2.5f))
        {
            float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
            if (slopeAngle > maxWalkableSlope)
            {
                return true;
            }
        }

        return false;
    }

    private void PickNewTerrainAngle()
    {
        float currentAngle = (parentRb != null) ? parentRb.transform.eulerAngles.y : transform.eulerAngles.y;
        float randomOffset = Random.Range(-maxTurnAngle, maxTurnAngle);
        targetYAngle = Mathf.Repeat(currentAngle + randomOffset, 360f);
        directionTimer = directionChangeInterval;
    }

    public void HandleCollision(Collision collision)
    {
        if (!Application.isPlaying || isReturningHome || movementType == MovementType.TerrainWander) return;

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
        if (movementType == MovementType.TerrainWander)
        {
            // Draw Heading Ray
            Gizmos.color = Color.cyan;
            Quaternion targetRot = Quaternion.Euler(0f, targetYAngle, 0f);
            Vector3 forwardDir = targetRot * Vector3.forward;
            Vector3 startPos = (parentRb != null) ? parentRb.transform.position : transform.position;
            Gizmos.DrawRay(startPos + Vector3.up * 0.5f, forwardDir * 2f);

            // Draw Slope Check Raycast
            Vector3 forwardXz = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            Vector3 checkOrigin = transform.position + (forwardXz * slopeCheckDistance) + (Vector3.up * 1.0f);
            Gizmos.color = (slopeFlipCooldownTimer > 0f) ? Color.yellow : Color.red;
            Gizmos.DrawLine(checkOrigin, checkOrigin + (Vector3.down * 2.5f));
            Gizmos.DrawWireSphere(checkOrigin, 0.1f);
            return;
        }

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