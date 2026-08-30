using UnityEngine;
using UnityEngine.AI;
using KinematicCharacterController;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class ObstacleLogic : MonoBehaviour
{
    public enum MovementSpace { AutoDetect, TerrainNavMesh, MovingPlatform }
    public enum MovementType { Static, Patrol, RandomWander }
    public enum PayloadType 
    { 
        None, 
        Damage, 
        HealthBooster, 
        StaminaBooster, 
        InvincibilityBuff, 
        DoubleJumpBuff, 
        DoubleSprintBuff 
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

    [Header("General Movement Speed")]
    [Range(0.1f, 100f)] public float moveSpeed = 3f;

    [Header("Patrol Settings (Platform Space)")]
    [Range(0.1f, 300f)] public float moveDistance = 3f;

    [Header("Wandering Settings")]
    [Range(0.1f, 100f)] public float minWanderRadius = 2f;
    [Range(0.1f, 200f)] public float maxWanderRadius = 6f;
    private float maxDistanceFromHome = 30f;

    [Header("Idle Rotation")]
    [Range(0f, 360f)] public float rotationAngle = 0f;

    [Header("Bounce / Trampoline Settings")]
    public bool isBouncy = false;
    [Range(0.5f, 5f)] public float triggerRadius = 1.2f;
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
    private SphereCollider bounceTriggerCollider;
    private Collider obstacleCollider;

    private Vector3 lastPatrolOffset;
    private Vector3 currentWanderOffsetFromHome;
    private Vector3 homePosition;
    private int patrolDirection = 1;
    private MovementType previousMovementType;

    private float turnSpeedMultiplier = 120f;

    private void Awake()
    {
        SetupComponents();
    }

    private void OnValidate()
    {
        SetupComponents();
    }

    private void SetupComponents()
    {
        navMeshAgent = GetComponentInChildren<NavMeshAgent>();
        followPlatform = GetComponent<FollowPlatform>();
        
        SphereCollider[] sphereColliders = GetComponents<SphereCollider>();
        foreach (SphereCollider col in sphereColliders)
        {
            if (col.isTrigger)
            {
                bounceTriggerCollider = col;
                break;
            }
        }

        if (bounceTriggerCollider == null)
        {
            bounceTriggerCollider = gameObject.AddComponent<SphereCollider>();
            bounceTriggerCollider.isTrigger = true;
        }

        bounceTriggerCollider.radius = triggerRadius;
        bounceTriggerCollider.enabled = isBouncy || payloadType != PayloadType.None;
    }

    private void Start()
    {
        SetupComponents();
        homePosition = transform.position;
        lastPatrolOffset = Vector3.zero;
        previousMovementType = movementType;

        if (movementSpace == MovementSpace.AutoDetect)
        {
            if (followPlatform != null && followPlatform.targetPlatform != null)
            {
                movementSpace = MovementSpace.MovingPlatform;
            }
            else
            {
                movementSpace = MovementSpace.TerrainNavMesh;
            }
        }

        obstacleCollider = GetComponentInChildren<Collider>();

        if (movementSpace == MovementSpace.TerrainNavMesh)
        {
            if (navMeshAgent != null)
            {
                navMeshAgent.enabled = true;
                SetNewNavMeshDestination();
            }
        }
        else
        {
            if (navMeshAgent != null) navMeshAgent.enabled = false;
            if (movementType == MovementType.RandomWander) PickNewWanderTarget();
            else ApplyRotation();
        }
    }

    private void Update()
    {
        if (movementType != previousMovementType)
        {
            if (movementSpace == MovementSpace.MovingPlatform)
            {
                if (movementType == MovementType.RandomWander) PickNewWanderTarget();
                else ApplyRotation();
            }
            else if (movementSpace == MovementSpace.TerrainNavMesh)
            {
                SetNewNavMeshDestination();
            }
            previousMovementType = movementType;
        }

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

    private void UpdateTerrainNavMeshLogic()
    {
        if (navMeshAgent == null || !navMeshAgent.isOnNavMesh || movementType == MovementType.Static) return;

        navMeshAgent.speed = moveSpeed;
        navMeshAgent.acceleration = moveSpeed * 8f;
        navMeshAgent.angularSpeed = Mathf.Max(120f, moveSpeed * 60f);

        if (!navMeshAgent.pathPending && navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance)
        {
            SetNewNavMeshDestination();
        }
    }

    private void SetNewNavMeshDestination()
    {
        if (navMeshAgent == null || !navMeshAgent.isOnNavMesh || movementType == MovementType.Static) return;

        Vector3 currentHome = Application.isPlaying ? homePosition : transform.position;

        for (int i = 0; i < 10; i++)
        {
            float chosenRadius = Random.Range(minWanderRadius, maxWanderRadius);
            Vector2 randomCircle = Random.insideUnitCircle.normalized * chosenRadius;
            Vector3 candidatePoint = transform.position + new Vector3(randomCircle.x, 0f, randomCircle.y);

            if (Vector3.Distance(candidatePoint, currentHome) > maxDistanceFromHome)
            {
                candidatePoint = currentHome + (candidatePoint - currentHome).normalized * Random.Range(minWanderRadius, Mathf.Min(maxWanderRadius, maxDistanceFromHome));
            }

            if (NavMesh.SamplePosition(candidatePoint, out NavMeshHit hit, maxWanderRadius, NavMesh.AllAreas))
            {
                navMeshAgent.SetDestination(hit.position);
                return;
            }
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
        float currentZ = lastPatrolOffset.z + (patrolDirection * moveSpeed * Time.deltaTime);

        if (currentZ >= moveDistance)
        {
            currentZ = moveDistance;
            patrolDirection = -1;
        }
        else if (currentZ <= -moveDistance)
        {
            currentZ = -moveDistance;
            patrolDirection = 1;
        }

        Vector3 targetPatrolOffset = new Vector3(0f, 0f, currentZ);
        Vector3 patrolDelta = targetPatrolOffset - lastPatrolOffset;

        transform.Translate(patrolDelta, Space.Self);
        lastPatrolOffset = targetPatrolOffset;
    }

    private void UpdateWanderMovement()
    {
        Vector3 homeWorldPos = GetHomeWorldPosition();
        Vector3 targetWorldPos = homeWorldPos + currentWanderOffsetFromHome;
        Vector3 vectorToTarget = targetWorldPos - transform.position;
        vectorToTarget.y = 0f;

        if (vectorToTarget.magnitude < 0.2f)
        {
            PickNewWanderTarget();
            return;
        }

        Vector3 moveDir = vectorToTarget.normalized;
        if (moveDir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, moveSpeed * turnSpeedMultiplier * Time.deltaTime);
        }

        transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime, Space.Self);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!Application.isPlaying) return;

        KinematicCharacterMotor motor = other.GetComponentInParent<KinematicCharacterMotor>();
        if (motor == null) motor = other.GetComponent<KinematicCharacterMotor>();

        if (motor != null)
        {
            // 1. Handle Bounce Logic
            if (isBouncy)
            {
                Vector3 surfaceNormal = (motor.TransientPosition - transform.position).normalized;
                if (surfaceNormal == Vector3.zero) surfaceNormal = Vector3.up;

                Vector3 launchDirection = Vector3.Lerp(surfaceNormal, Vector3.up, upwardBias).normalized;
                motor.ForceUnground();
                float totalLaunchSpeed = launchForce + (motor.BaseVelocity.magnitude * momentumTransfer);
                motor.BaseVelocity = launchDirection * totalLaunchSpeed;
            }

            // 2. Handle Payload Logic
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

                case PayloadType.DoubleJumpBuff:
                    if (playerLogic != null) playerLogic.ApplyDoubleJumpBuff(buffDuration);
                    break;

                case PayloadType.DoubleSprintBuff:
                    if (playerLogic != null) playerLogic.ApplySprintBuff(payloadAmount, buffDuration);
                    break;
            }

            if (destroyOnTrigger && payloadType != PayloadType.None)
            {
                Destroy(gameObject);
            }
        }
    }

    public Vector3 GetHomeWorldPosition()
    {
        if (followPlatform != null && followPlatform.targetPlatform != null)
        {
            return followPlatform.targetPlatform.position;
        }
        return homePosition;
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
        Vector2 circle = Random.insideUnitCircle * Random.Range(minWanderRadius, maxWanderRadius);
        currentWanderOffsetFromHome = new Vector3(circle.x, 0f, circle.y);
    }

    private void OnDrawGizmosSelected()
    {
        if (movementSpace == MovementSpace.TerrainNavMesh)
        {
            Vector3 center = Application.isPlaying ? homePosition : transform.position;
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.75f);
            Gizmos.DrawWireSphere(center, maxDistanceFromHome);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, minWanderRadius);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, maxWanderRadius);
        }
        else
        {
            if (movementType == MovementType.RandomWander)
            {
                Vector3 homeWorldPos = Application.isPlaying ? GetHomeWorldPosition() : transform.position;
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(homeWorldPos, maxWanderRadius);

                Vector3 activeTargetWorld = homeWorldPos + currentWanderOffsetFromHome;
                Gizmos.color = Color.cyan;
                Gizmos.DrawSphere(activeTargetWorld, 0.4f);
                Gizmos.DrawLine(transform.position, activeTargetWorld);
            }

            if (movementType == MovementType.Patrol)
            {
                Vector3 startPos = transform.position + transform.TransformDirection(new Vector3(0f, 0f, -moveDistance) - lastPatrolOffset);
                Vector3 endPos = transform.position + transform.TransformDirection(new Vector3(0f, 0f, moveDistance) - lastPatrolOffset);

                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(startPos, endPos);
                Gizmos.DrawWireSphere(startPos, 0.3f);
                Gizmos.DrawWireSphere(endPos, 0.3f);
            }
        }

        if (isBouncy)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, triggerRadius);
        }
    }
}