using UnityEngine;

public class DamageDealer : MonoBehaviour
{
    [SerializeField, Range(0.1f, 50f)] private int damageAmount = 10;

    private void OnTriggerEnter(Collider other)
    {
        // 1. Check if the collider belongs to the Player (Kinematic Character Motor)
        var motor = other.GetComponent<KinematicCharacterController.KinematicCharacterMotor>();
        
        // 2. Check for IDamagable anywhere on the target (or its parent hierarchy)
        var damagable = other.GetComponent<IDamagable>();

        if (motor != null && damagable != null)
        {
            damagable.TakeDamage(damageAmount);
        }
    }
}