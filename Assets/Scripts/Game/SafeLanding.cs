using UnityEngine;

/// <summary>
/// Put this on anything (a tree, a rock, a giant mushroom...): landing on it never gives fall damage.
/// Platforms, creatures and pickups have their own '🪂 Landing on it never hurts' tick box instead.
/// </summary>
[DisallowMultipleComponent]
public class SafeLanding : MonoBehaviour
{
    /// <summary>Is landing on this collider safe? (a safe platform, creature/pickup, or anything with SafeLanding)</summary>
    public static bool IsSafe(Collider ground)
    {
        if (ground == null) return false;
        if (ground.GetComponentInParent<SafeLanding>() != null) return true;
        PlatformLogic platform = ground.GetComponentInParent<PlatformLogic>();
        if (platform != null && platform.safeLanding) return true;
        ObstacleBase thing = ground.GetComponentInParent<ObstacleBase>();
        return thing != null && thing.safeLanding;
    }
}
