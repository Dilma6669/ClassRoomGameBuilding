using UnityEngine;
using UnityEngine.AI;
using KinematicCharacterController;

[ExecuteAlways]
public class ObstacleLogic : MonoBehaviour
{
    [Header("Transform Settings")]
    [Range(0.1f, 50f)] public float objectScale = 1f;
    
    [Header("NavMesh Settings")]
    [Range(0.01f, 5f)] public float navMeshRadius = 0.5f;
    
    public enum MovementSpace { AutoDetect, TerrainNavMesh, MovingPlatform }
    public enum MovementType { Static, Patrol, RandomWander }
    public enum PayloadType 
    { 
        None, 
        Damage, 
        HealthBooster, 
        StaminaBooster, 
        InvincibilityBuff, 
        JumpBuff, 
        SprintBuff 
    }

    [Header("Environment & Movement Space")]
    [Tooltip("AutoDetect checks for a FollowPlatform component. Terrain uses NavMesh. MovingPlatform tracks targetPlatform.")]
    public MovementSpace movementSpace = MovementSpace.AutoDetect;
    public MovementType movementType = MovementType.Patrol;

    [Header("Payload Settings")]
    public PayloadType payloadType = PayloadType.Damage;
    [Range(1, 100f)] public int payloadAmount = 10;
    [Tooltip("Duration in seconds for temporary buffs.")]
    [Range(1f, 60f)] public float buffDuration = 5f;
    [Tooltip("If checked, destroys this object when triggered (useful for health/stamina/buff pickups).")]
    public bool destroyOnTrigger = false;

    [Header("General Movement Speed Range")]
    [Range(0.1f, 1000f)] public float minMoveSpeed = 2f;
    [Range(0.1f, 1000f)] public float maxMoveSpeed = 5f;

    [Header("Patrol Settings (Platform Space)")]
    [Range(0.1f, 500f)] public float moveDistance = 3f;

    [Header("Wandering Settings")]
    [Range(0.1f, 1000f)] public float minWanderRadius = 2f;
    [Range(0.1f, 1000f)] public float maxWanderRadius = 6f;

    [Header("Idle Rotation")]
    [Range(0f, 360f)] public float rotationAngle = 0f;

    public bool isBouncy = false;
    [Range(0.5f, 5f)] public float triggerRadius = 0.5f;
    [Range(5f, 50f)] public float launchForce = 25f;
    [Range(0f, 1f)] public float upwardBias = 0.5f;
    [Range(0f, 1f)] public float momentumTransfer = 0.3f;

    [Header("Surface Height Snapping (Platform Mode)")]
    [Range(0.1f, 5f)] private float raycastOriginHeight = 4f;
    [Range(0f, 2f)] private float surfaceOffset = 0.5f;
    [Range(1f, 50f)] private float stepUpSpeed = 15f;

    // References
    private NavMeshAgent navMeshAgent;
    private FollowPlatform followPlatform;
    private MeshCollider physicalMeshCollider;
    private MeshCollider triggerMeshCollider;
    private Collider obstacleCollider;
    private Rigidbody rb;

    private Vector3 initialPlatformLocalPosition;
    private Vector3 spawnCenterPosition; // Fixed home anchor for Terrain wandering
    private Vector3 lastPatrolOffset;
    private Vector3 currentWanderOffset;
    private int patrolDirection = 1;
    private MovementType previousMovementType;

    private float currentMoveSpeed;
    private float turnSpeedMultiplier = 120f;
    private bool isFalling = false;

    private void OnValidate()
    {
        // Keep root transform at 1 so physical collider and NavMesh stay exact
        transform.localScale = Vector3.one;

        // Scale child objects (visual mesh & trigger collider)
        int childCount = transform.childCount;
        for (int i = 0; i < childCount; i++)
        {
            transform.GetChild(i).localScale = Vector3.one * objectScale;
        }

        if (navMeshAgent != null)
        {
            navMeshAgent.radius = navMeshRadius;
        }

        if (minWanderRadius > maxWanderRadius)
        {
            minWanderRadius = maxWanderRadius;
        }

        if (minMoveSpeed > maxMoveSpeed)
        {
            minMoveSpeed = maxMoveSpeed;
        }
    }

    private void Awake()
    {
        InitializeObstacle();
    }

    private void InitializeObstacle()
    {
        transform.localScale = Vector3.one;

        int childCount = transform.childCount;
        for (int i = 0; i < childCount; i++)
        {
            transform.GetChild(i).localScale = Vector3.one * objectScale;
        }

        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        rb.isKinematic = true;
        rb.useGravity = true;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        MeshFilter childMeshFilter = GetComponentInChildren<MeshFilter>();
        Mesh targetMesh = childMeshFilter != null ? childMeshFilter.sharedMesh : null;

        // Parent physical collider setup
        physicalMeshCollider = GetComponent<MeshCollider>();
        if (physicalMeshCollider != null)
        {
            if (targetMesh != null && physicalMeshCollider.sharedMesh == null)
            {
                physicalMeshCollider.sharedMesh = targetMesh;
            }
            physicalMeshCollider.convex = true;
            physicalMeshCollider.isTrigger = false;
        }

        // Child trigger collider setup
        if (childMeshFilter != null)
        {
            triggerMeshCollider = childMeshFilter.GetComponent<MeshCollider>();
            if (triggerMeshCollider == null)
            {
                triggerMeshCollider = childMeshFilter.gameObject.AddComponent<MeshCollider>();
            }

            if (targetMesh != null && triggerMeshCollider.sharedMesh == null)
            {
                triggerMeshCollider.sharedMesh = targetMesh;
            }
            
            triggerMeshCollider.convex = true;
            triggerMeshCollider.isTrigger = true;
            triggerMeshCollider.enabled = isBouncy || payloadType != PayloadType.None;
        }

        navMeshAgent = GetComponent<NavMeshAgent>();
        if (navMeshAgent == null) navMeshAgent = GetComponentInChildren<NavMeshAgent>();

        if (navMeshAgent != null)
        {
            navMeshAgent.radius = navMeshRadius;
            navMeshAgent.stoppingDistance = 0.2f;
            navMeshAgent.obstacleAvoidanceType = UnityEngine.AI.ObstacleAvoidanceType.NoObstacleAvoidance;
        }

        followPlatform = GetComponent<FollowPlatform>();
    }

    private void Start()
    {
        spawnCenterPosition = transform.position;

        // Capture anchor relative to target platform or parent
        Transform platformTransform = GetPlatformTransform();
        if (platformTransform != null)
        {
            initialPlatformLocalPosition = platformTransform.InverseTransformPoint(transform.position);
        }
        else
        {
            initialPlatformLocalPosition = transform.localPosition;
        }

        lastPatrolOffset = Vector3.zero;
        previousMovementType = movementType;
        RandomizeSpeed();

        if (movementSpace == MovementSpace.AutoDetect)
        {
            movementSpace = (followPlatform != null && followPlatform.targetPlatform != null)
                ? MovementSpace.MovingPlatform
                : MovementSpace.TerrainNavMesh;
        }

        obstacleCollider = physicalMeshCollider;

        if (movementSpace == MovementSpace.TerrainNavMesh)
        {
            if (navMeshAgent != null)
            {
                if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 15f, NavMesh.AllAreas))
                {
                    transform.position = hit.position + Vector3.up * 0.1f;
                    navMeshAgent.Warp(hit.position);
                    spawnCenterPosition = hit.position; // Lock spawn center to exact NavMesh position
                }

                SetKinematicState(true);

                if (movementType == MovementType.RandomWander)
                {
                    Invoke(nameof(SetNewNavMeshDestination), Random.Range(0.05f, 0.5f));
                }
                else if (movementType == MovementType.Patrol)
                {
                    SetNavMeshPatrolTarget();
                }
                else
                {
                    navMeshAgent.ResetPath();
                }
            }
        }
        else
        {
            if (navMeshAgent != null) 
            {
                navMeshAgent.enabled = false;
            }
        
            if (movementType == MovementType.RandomWander) PickNewWanderTarget();
            else ApplyRotation();
        }
    }

    private Transform GetPlatformTransform()
    {
        if (followPlatform != null && followPlatform.targetPlatform != null)
        {
            return followPlatform.targetPlatform;
        }
        return transform.parent;
    }

    private void Update()
    {
        if (movementType != previousMovementType)
        {
            OnMovementTypeChanged();
            previousMovementType = movementType;
        }

        if (movementType == MovementType.Static) return;

        if (!Application.isPlaying)
        {
            if (movementType != MovementType.RandomWander) ApplyRotation();
            lastPatrolOffset = Vector3.zero;
            return;
        }

        if (movementSpace == MovementSpace.TerrainNavMesh)
        {
            UpdateTerrainNavMeshLogic();
        }
        else
        {
            UpdatePlatformLogic();
        }
    }

    private void RandomizeSpeed()
    {
        currentMoveSpeed = Random.Range(minMoveSpeed, maxMoveSpeed);
    }

    private void OnMovementTypeChanged()
    {
        RandomizeSpeed();

        if (movementSpace == MovementSpace.TerrainNavMesh)
        {
            if (navMeshAgent != null && navMeshAgent.enabled && navMeshAgent.isOnNavMesh)
            {
                if (movementType == MovementType.RandomWander)
                {
                    SetNewNavMeshDestination();
                }
                else if (movementType == MovementType.Patrol)
                {
                    SetNavMeshPatrolTarget();
                }
                else
                {
                    navMeshAgent.ResetPath();
                }
            }
        }
        else
        {
            if (movementType == MovementType.RandomWander)
            {
                PickNewWanderTarget();
            }
            else
            {
                ApplyRotation();
            }
        }
    }

    private void UpdateTerrainNavMeshLogic()
    {
        if (navMeshAgent == null || movementType == MovementType.Static) return;

        if (isFalling)
        {
            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit groundHit, 1.2f, Physics.AllLayers, QueryTriggerInteraction.Ignore))
            {
                if (NavMesh.SamplePosition(groundHit.point, out NavMeshHit navHit, 1f, NavMesh.AllAreas))
                {
                    SetKinematicState(true);
                    navMeshAgent.Warp(navHit.position);
                    isFalling = false;
                
                    if (movementType == MovementType.RandomWander) SetNewNavMeshDestination();
                    else if (movementType == MovementType.Patrol) SetNavMeshPatrolTarget();
                }
            }
            return;
        }

        if (!navMeshAgent.isOnNavMesh)
        {
            SetKinematicState(false);
            isFalling = true;
            return;
        }

        navMeshAgent.radius = navMeshRadius;
        navMeshAgent.speed = currentMoveSpeed;
        navMeshAgent.acceleration = currentMoveSpeed * 8f;
        navMeshAgent.angularSpeed = Mathf.Max(120f, currentMoveSpeed * 60f);

        if (!navMeshAgent.pathPending)
        {
            if (!navMeshAgent.hasPath || navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance)
            {
                RandomizeSpeed();

                if (movementType == MovementType.RandomWander)
                {
                    SetNewNavMeshDestination();
                }
                else if (movementType == MovementType.Patrol)
                {
                    patrolDirection *= -1;
                    SetNavMeshPatrolTarget();
                }
            }
        }
    }

    private void SetKinematicState(bool enableKinematic)
    {
        if (rb != null)
        {
            rb.isKinematic = enableKinematic;
        }
        if (navMeshAgent != null)
        {
            navMeshAgent.enabled = enableKinematic;
        }
    }

    private void SetNavMeshPatrolTarget()
    {
        if (navMeshAgent == null || !navMeshAgent.enabled || !navMeshAgent.isOnNavMesh || movementType != MovementType.Patrol) return;

        Vector3 targetOffset = transform.forward * (moveDistance * patrolDirection);
        Vector3 targetPos = transform.position + targetOffset;

        if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, moveDistance, NavMesh.AllAreas))
        {
            navMeshAgent.SetDestination(hit.position);
        }
    }

    private void SetNewNavMeshDestination()
    {
        if (navMeshAgent == null || !navMeshAgent.enabled || !navMeshAgent.isOnNavMesh || movementType != MovementType.RandomWander) return;

        Vector2 randomDirection = Random.insideUnitCircle.normalized;
        float randomDistance = Random.Range(minWanderRadius, maxWanderRadius);
        
        // Calculate point anchored to original spawn location
        Vector3 candidatePoint = spawnCenterPosition + new Vector3(randomDirection.x * randomDistance, 0f, randomDirection.y * randomDistance);

        if (NavMesh.SamplePosition(candidatePoint, out NavMeshHit hit, maxWanderRadius, NavMesh.AllAreas))
        {
            navMeshAgent.SetDestination(hit.position);
        }
    }

    private void UpdatePlatformLogic()
    {
        switch (movementType)
        {
            case MovementType.Patrol:
                UpdatePatrolMovement();
                break;
            case MovementType.RandomWander:
                UpdateWanderMovement();
                break;
            case MovementType.Static:
                lastPatrolOffset = Vector3.zero;
                break;
        }

        SnapToSurfaceHeight();
    }

    private void UpdatePatrolMovement()
    {
        float currentZ = lastPatrolOffset.z + (patrolDirection * currentMoveSpeed * Time.deltaTime);

        if (currentZ >= moveDistance)
        {
            currentZ = moveDistance;
            patrolDirection = -1;
            RandomizeSpeed();
        }
        else if (currentZ <= -moveDistance)
        {
            currentZ = -moveDistance;
            patrolDirection = 1;
            RandomizeSpeed();
        }

        lastPatrolOffset = new Vector3(0f, 0f, currentZ);

        Transform platformTransform = GetPlatformTransform();
        if (platformTransform != null)
        {
            transform.position = platformTransform.TransformPoint(initialPlatformLocalPosition + lastPatrolOffset);
        }
        else
        {
            transform.localPosition = initialPlatformLocalPosition + lastPatrolOffset;
        }
    }

    private void UpdateWanderMovement()
    {
        Transform platformTransform = GetPlatformTransform();
        Vector3 centerWorldPos = platformTransform != null 
            ? platformTransform.TransformPoint(initialPlatformLocalPosition) 
            : transform.parent != null ? transform.parent.TransformPoint(initialPlatformLocalPosition) : initialPlatformLocalPosition;

        Vector3 targetWorldPos = centerWorldPos + transform.TransformDirection(currentWanderOffset);
        Vector3 worldDelta = targetWorldPos - transform.position;
        worldDelta.y = 0f;

        if (worldDelta.magnitude < 0.2f)
        {
            PickNewWanderTarget();
            return;
        }

        Vector3 moveDir = worldDelta.normalized;
        if (moveDir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, currentMoveSpeed * turnSpeedMultiplier * Time.deltaTime);
        }

        transform.Translate(Vector3.forward * currentMoveSpeed * Time.deltaTime, Space.Self);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!Application.isPlaying) return;

        KinematicCharacterMotor motor = other.GetComponentInParent<KinematicCharacterMotor>();
        if (motor == null) motor = other.GetComponent<KinematicCharacterMotor>();

        if (motor != null)
        {
            if (isBouncy)
            {
                Vector3 surfaceNormal = (motor.TransientPosition - transform.position).normalized;
                if (surfaceNormal == Vector3.zero) surfaceNormal = Vector3.up;

                Vector3 launchDirection = Vector3.Lerp(surfaceNormal, Vector3.up, upwardBias).normalized;
                motor.ForceUnground();
                float totalLaunchSpeed = launchForce + (motor.BaseVelocity.magnitude * momentumTransfer);
                motor.BaseVelocity = launchDirection * totalLaunchSpeed;
            }

            var playerLogic = other.GetComponentInParent<PlayerLogic>();

            switch (payloadType)
            {
                case PayloadType.Damage:
                    var damagable = other.GetComponentInParent<IDamagable>();
                    if (damagable != null) damagable.TakeDamage(payloadAmount);
                    break;

                case PayloadType.HealthBooster:
                    var playerHealth = other.GetComponentInParent<Health>();
                    if (playerHealth != null) playerHealth.Heal(payloadAmount);
                    break;

                case PayloadType.StaminaBooster:
                    if (playerLogic != null) playerLogic.RestoreStamina(payloadAmount);
                    break;

                case PayloadType.InvincibilityBuff:
                    var healthComp = other.GetComponentInParent<Health>();
                    if (healthComp != null) healthComp.ApplyInvincibility(buffDuration);
                    break;

                case PayloadType.JumpBuff:
                    if (playerLogic != null) playerLogic.ApplyJumpBuff(payloadAmount, buffDuration);
                    break;

                case PayloadType.SprintBuff:
                    if (playerLogic != null) playerLogic.ApplySprintBuff(payloadAmount, buffDuration);
                    break;
            }

            if (destroyOnTrigger && payloadType != PayloadType.None)
            {
                Destroy(gameObject);
            }
        }
    }

    private void SnapToSurfaceHeight()
    {
        Vector3 rayOrigin = transform.position + (Vector3.up * raycastOriginHeight);
        RaycastHit[] hits = Physics.RaycastAll(rayOrigin, Vector3.down, raycastOriginHeight * 3f, Physics.AllLayers, QueryTriggerInteraction.Ignore);

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == obstacleCollider || hit.transform.IsChildOf(transform)) continue;

            float targetY = hit.point.y + surfaceOffset;
            Vector3 currentPos = transform.position;
            currentPos.y = Mathf.Lerp(currentPos.y, targetY, Time.deltaTime * stepUpSpeed);
            transform.position = currentPos;
            break;
        }
    }

    private void ApplyRotation()
    {
        Vector3 currentEuler = transform.localEulerAngles;
        transform.localRotation = Quaternion.Euler(currentEuler.x, rotationAngle, currentEuler.z);
    }

    public void PickNewWanderTarget()
    {
        RandomizeSpeed();
    
        // Pick a random direction and distance strictly between minWanderRadius and maxWanderRadius
        Vector2 randomDirection = Random.insideUnitCircle.normalized;
        float randomDistance = Random.Range(minWanderRadius, maxWanderRadius);
    
        // Offset is anchored relative to the spawn center, preventing target drift
        currentWanderOffset = new Vector3(randomDirection.x * randomDistance, 0f, randomDirection.y * randomDistance);
    }

    private void OnDrawGizmosSelected()
    {
        if (followPlatform == null) followPlatform = GetComponent<FollowPlatform>();

        Transform platformTransform = GetPlatformTransform();
        Vector3 centerPoint;

        if (Application.isPlaying)
        {
            centerPoint = (movementSpace == MovementSpace.TerrainNavMesh)
                ? spawnCenterPosition
                : (platformTransform != null ? platformTransform.TransformPoint(initialPlatformLocalPosition) : transform.position);
        }
        else
        {
            centerPoint = transform.position;
        }

        if (movementType == MovementType.Patrol)
        {
            Vector3 currentOffset = Application.isPlaying ? lastPatrolOffset : Vector3.zero;
            Vector3 startPos = centerPoint + transform.TransformDirection(new Vector3(0f, 0f, -moveDistance) - currentOffset);
            Vector3 endPos = centerPoint + transform.TransformDirection(new Vector3(0f, 0f, moveDistance) - currentOffset);

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(startPos, endPos);
            Gizmos.DrawWireSphere(startPos, 1f);
            Gizmos.DrawWireSphere(endPos, 1f);
        }
        else if (movementType == MovementType.RandomWander)
        {
            // Max radius outer boundary
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(centerPoint, maxWanderRadius);

            // Min radius inner deadzone
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(centerPoint, minWanderRadius);

            if (movementSpace == MovementSpace.TerrainNavMesh)
            {
                if (Application.isPlaying && navMeshAgent != null && navMeshAgent.enabled && navMeshAgent.hasPath)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawSphere(navMeshAgent.destination, 0.4f);

                    Gizmos.color = Color.magenta;
                    Vector3[] corners = navMeshAgent.path.corners;
                    for (int i = 0; i < corners.Length - 1; i++)
                    {
                        Gizmos.DrawLine(corners[i], corners[i + 1]);
                    }
                }
            }
            else
            {
                Vector3 activeTargetWorld = centerPoint + transform.TransformDirection(currentWanderOffset);
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(activeTargetWorld, 0.4f);
                Gizmos.DrawLine(centerPoint, activeTargetWorld);
            }
        }

        if (isBouncy)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, triggerRadius * objectScale);
        }
    }
}