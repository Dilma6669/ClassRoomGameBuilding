using UnityEngine;

[ExecuteAlways]
public class EnemyLogic : MonoBehaviour
{
    [Header("Patrol Setup")]
    public bool enablePatrol = true;
    [Range(0.1f, 20f)] public float moveSpeed = 3f;

    [Header("Patrol Range")]
    [Range(0f, 20f)] public float moveDistanceX = 3f;
    [Range(0f, 20f)] public float moveDistanceZ = 0f;

    private Vector3 initialLocalOffset;
    private Vector3 lastPatrolOffset;

    private void Start()
    {
        lastPatrolOffset = Vector3.zero;
    }

    private void Update()
    {
        if (!Application.isPlaying || !enablePatrol) 
        {
            lastPatrolOffset = Vector3.zero;
            return;
        }

        UpdatePatrolMovement();
    }

    private void UpdatePatrolMovement()
    {
        // Smooth ping-pong factor (0 to 1)
        float pingPong = Mathf.PingPong(Time.time * moveSpeed, 1f);
        float smoothedProgress = Mathf.SmoothStep(0f, 1f, pingPong);

        // Calculate absolute target offsets from center
        float targetOffsetX = Mathf.Lerp(-moveDistanceX, moveDistanceX, smoothedProgress);
        float targetOffsetZ = Mathf.Lerp(-moveDistanceZ, moveDistanceZ, smoothedProgress);

        Vector3 currentPatrolOffset = new Vector3(targetOffsetX, 0f, targetOffsetZ);

        // Calculate how much the patrol offset changed since last frame
        Vector3 patrolDelta = currentPatrolOffset - lastPatrolOffset;

        // Apply only the change in patrol position in local space
        transform.Translate(patrolDelta, Space.Self);

        // Store current offset for next frame
        lastPatrolOffset = currentPatrolOffset;
    }

    private void OnDrawGizmosSelected()
    {
        // Draw visual patrol gizmo in Scene view relative to initial placement
        Vector3 startPos = transform.position + transform.TransformDirection(new Vector3(-moveDistanceX, 0f, -moveDistanceZ) - lastPatrolOffset);
        Vector3 endPos = transform.position + transform.TransformDirection(new Vector3(moveDistanceX, 0f, moveDistanceZ) - lastPatrolOffset);

        Gizmos.color = Color.red;
        Gizmos.DrawLine(startPos, endPos);
        Gizmos.DrawWireSphere(startPos, 0.3f);
        Gizmos.DrawWireSphere(endPos, 0.3f);
    }
}