using UnityEngine;

public class HealthBooster : MonoBehaviour
{
    [SerializeField, Range(0.1f, 50f)] private int healAmount = 25;
    [SerializeField] private bool destroyOnPickup = true;

    private void OnTriggerEnter(Collider other)
    {
        // 1. Check if the collider belongs to the Player (Kinematic Character Motor)
        var motor = other.GetComponent<KinematicCharacterController.KinematicCharacterMotor>();

        // 2. Look for Health script on target or its parent/children
        var playerHealth = other.GetComponentInParent<Health>();
        if (playerHealth == null)
        {
            playerHealth = other.GetComponentInChildren<Health>();
        }

        // 3. Heal player and destroy object if configured
        if (motor != null && playerHealth != null)
        {
            playerHealth.Heal(healAmount);

            if (destroyOnPickup)
            {
                Destroy(gameObject);
            }
        }
    }
}