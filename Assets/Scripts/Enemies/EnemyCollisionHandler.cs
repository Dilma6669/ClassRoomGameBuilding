using UnityEngine;

public class EnemyCollisionHandler : MonoBehaviour
{
    private EnemyLogic parentLogic;

    private void Awake()
    {
        parentLogic = GetComponentInParent<EnemyLogic>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (parentLogic != null)
        {
            parentLogic.HandleCollision(collision);
        }
    }
}