using UnityEngine;

[ExecuteAlways]
public class EnemyLogic : MonoBehaviour
{
    [Header("Rotation Setup")]
    [Range(0f, 360f)] public float rotationAngle = 0f;

    [Header("Patrol Setup")]
    public bool enablePatrol = true;
    [Range(0.1f, 20f)] public float moveSpeed = 3f;

    [Header("Patrol Range")]
    [Range(0f, 20f)] public float moveDistance = 3f;

    private Vector3 lastPatrolOffset;

    private void Start()
    {
        lastPatrolOffset = Vector3.zero;
        ApplyRotation();
    }

    private void Update()
    {
        ApplyRotation();

        if (!Application.isPlaying || !enablePatrol)
        {
            lastPatrolOffset = Vector3.zero;
            return;
        }

        UpdatePatrolMovement();
    }

    private void ApplyRotation()
    {
        Vector3 currentEuler = transform.localEulerAngles;
        transform.localRotation = Quaternion.Euler(currentEuler.x, rotationAngle, currentEuler.z);
    }

    private void UpdatePatrolMovement()
    {
        float pingPong = Mathf.PingPong(Time.time * moveSpeed, 1f);
        float smoothedProgress = Mathf.SmoothStep(0f, 1f, pingPong);

        // Calculate forward/backward patrol offset along local Z axis
        float targetOffsetZ = Mathf.Lerp(-moveDistance, moveDistance, smoothedProgress);
        Vector3 currentPatrolOffset = new Vector3(0f, 0f, targetOffsetZ);

        Vector3 patrolDelta = currentPatrolOffset - lastPatrolOffset;

        // Translate along local space (respects current Y rotation)
        transform.Translate(patrolDelta, Space.Self);

        lastPatrolOffset = currentPatrolOffset;
    }

    private void OnDrawGizmosSelected()
    {
        // Draw gizmo line forward and backward along local facing direction
        Vector3 startPos = transform.position + transform.TransformDirection(new Vector3(0f, 0f, -moveDistance) - lastPatrolOffset);
        Vector3 endPos = transform.position + transform.TransformDirection(new Vector3(0f, 0f, moveDistance) - lastPatrolOffset);

        Gizmos.color = Color.red;
        Gizmos.DrawLine(startPos, endPos);
        Gizmos.DrawWireSphere(startPos, 0.3f);
        Gizmos.DrawWireSphere(endPos, 0.3f);
    }
}