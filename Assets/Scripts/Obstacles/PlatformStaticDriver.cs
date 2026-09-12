using UnityEngine;

[RequireComponent(typeof(PlatformObstacle))]
public class PlatformStaticDriver : MonoBehaviour, IObstacleMovement
{
    private PlatformObstacle platformHost;
    private Transform platformTransform;

    private void Awake()
    {
        platformHost = GetComponent<PlatformObstacle>();
    }

    private void Start()
    {
        CachePlatformAnchor();
    }

    private void CachePlatformAnchor()
    {
        if (platformHost != null && platformHost.GetPlatformTransform() != null)
        {
            platformTransform = platformHost.GetPlatformTransform();
        }
        else
        {
            platformTransform = transform.parent;
        }
    }

    public void ProcessMovement(ObstacleBase host)
    {
        // Static obstacle — intentionally empty to stay locked to the platform anchor
    }
}