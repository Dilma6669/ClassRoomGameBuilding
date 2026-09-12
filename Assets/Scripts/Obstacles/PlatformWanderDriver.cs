using UnityEngine;

[RequireComponent(typeof(PlatformObstacle))]
public class PlatformWanderDriver : MonoBehaviour, IObstacleMovement
{
    [Header("Wander Settings")]
    [Range(0.1f, 1000f)] public float minWanderRadius = 2f;
    [Range(0.1f, 1000f)] public float maxWanderRadius = 6f;
    [Range(0.1f, 1000f)] public float minMoveSpeed = 2f;
    [Range(0.1f, 1000f)] public float maxMoveSpeed = 5f;

    private Vector3 currentWanderOffset;
    private float currentMoveSpeed;
    private float turnSpeedMultiplier = 120f;
    private Vector3 initialLocalPosition;

    private void Start()
    {
        initialLocalPosition = transform.localPosition;
        PickNewWanderTarget();
    }

    public void ProcessMovement(ObstacleBase host)
    {
        Vector3 targetLocalPos = initialLocalPosition + currentWanderOffset;
        Vector3 localDelta = targetLocalPos - transform.localPosition;
        localDelta.y = 0f;

        if (localDelta.magnitude < 0.2f)
        {
            PickNewWanderTarget();
            return;
        }

        Vector3 moveDir = localDelta.normalized;
        if (moveDir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
            transform.localRotation = Quaternion.RotateTowards(transform.localRotation, targetRot, currentMoveSpeed * turnSpeedMultiplier * Time.deltaTime);
        }

        transform.Translate(Vector3.forward * (currentMoveSpeed * Time.deltaTime), Space.Self);
    }

    public void PickNewWanderTarget()
    {
        currentMoveSpeed = Random.Range(minMoveSpeed, maxMoveSpeed);
        Vector2 randomDir = Random.insideUnitCircle.normalized;
        float dist = Random.Range(minWanderRadius, maxWanderRadius);
        currentWanderOffset = new Vector3(randomDir.x * dist, 0f, randomDir.y * dist);
    }
}