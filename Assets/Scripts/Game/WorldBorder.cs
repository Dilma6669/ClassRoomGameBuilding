using UnityEngine;
using UnityEngine.SceneManagement;

#if HAS_KINEMATIC_CC
using KinematicCharacterController;
#endif

/// <summary>
/// Invisible walls around the level plus a "sky limit" ceiling, so the player can't leave the map.
/// The size is worked out automatically from the terrain and platforms in the level.
/// If the player still ends up far below the level, they die and the level restarts.
/// Created automatically by GameManager. Turn it off or resize it in Game > Settings and Library.
/// </summary>
public class WorldBorder : MonoBehaviour
{
    private const float WallThickness = 10f;

    private GameSettings settings;
    private Bounds playArea;
    private float killHeight;
    private Health playerHealth;
    private bool hasBounds;

    public static WorldBorder CreateFor(Scene scene, GameSettings settings)
    {
        GameObject go = new GameObject("WorldBorder (auto)");
        SceneManager.MoveGameObjectToScene(go, scene);
        WorldBorder border = go.AddComponent<WorldBorder>();
        border.settings = settings;
        return border;
    }

    private void Start()
    {
        playerHealth = FindAnyObjectByType<Health>();

        if (!TryGetLevelBounds(out playArea))
        {
            Debug.Log("WorldBorder: no terrain or platforms found, so no border was made.");
            enabled = false;
            return;
        }

        // Make sure the player's starting spot is inside the border
        if (playerHealth != null) playArea.Encapsulate(playerHealth.transform.position + Vector3.up * 3f);

        float pad = settings.borderPadding;
        playArea.Expand(new Vector3(pad * 2f, 0f, pad * 2f));

        float top = playArea.max.y + settings.skyLimitHeight;
        killHeight = playArea.min.y - settings.fallKillDepth;
        float bottom = killHeight - 20f;
        float height = top - bottom;
        float midY = (top + bottom) * 0.5f;

        Vector3 c = playArea.center;
        Vector3 s = playArea.size;
        float t = WallThickness;

        CreateWall("Wall West", new Vector3(playArea.min.x - t / 2f, midY, c.z), new Vector3(t, height, s.z + t * 2f));
        CreateWall("Wall East", new Vector3(playArea.max.x + t / 2f, midY, c.z), new Vector3(t, height, s.z + t * 2f));
        CreateWall("Wall South", new Vector3(c.x, midY, playArea.min.z - t / 2f), new Vector3(s.x + t * 2f, height, t));
        CreateWall("Wall North", new Vector3(c.x, midY, playArea.max.z + t / 2f), new Vector3(s.x + t * 2f, height, t));
        CreateWall("Sky Limit", new Vector3(c.x, top + t / 2f, c.z), new Vector3(s.x + t * 2f, t, s.z + t * 2f));

        hasBounds = true;
    }

    private void Update()
    {
        if (!hasBounds) return;

        if (playerHealth == null) playerHealth = FindAnyObjectByType<Health>();
        if (playerHealth == null || playerHealth.IsDead) return;

        // Safety net: fell out of the world somehow
        if (playerHealth.transform.position.y < killHeight)
        {
            playerHealth.Kill();
        }
    }

    private void CreateWall(string wallName, Vector3 center, Vector3 size)
    {
        GameObject wall = new GameObject(wallName);
        wall.transform.SetParent(transform, false);
        wall.transform.position = center;
        BoxCollider box = wall.AddComponent<BoxCollider>();
        box.size = size;
    }

    private bool TryGetLevelBounds(out Bounds bounds)
    {
        bounds = new Bounds();
        bool found = false;
        Scene myScene = gameObject.scene;

        // Terrains (works even if the terrain has no collider)
        foreach (Terrain terrain in FindObjectsByType<Terrain>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (terrain.gameObject.scene != myScene || terrain.terrainData == null) continue;
            Bounds tb = terrain.terrainData.bounds;
            tb.center += terrain.GetPosition();
            Add(ref bounds, ref found, tb);
        }

        // Solid colliders: ground, platforms, buildings...
        foreach (Collider col in FindObjectsByType<Collider>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (!col.enabled || col.isTrigger || col.gameObject.scene != myScene) continue;
            if (col.transform.IsChildOf(transform)) continue;
            if (IsPlayerOrMovingObstacle(col)) continue;
            Add(ref bounds, ref found, col.bounds);
        }

        return found;
    }

    private static bool IsPlayerOrMovingObstacle(Collider col)
    {
        if (col.GetComponentInParent<Health>() != null) return true;
#if HAS_KINEMATIC_CC
        if (col.GetComponentInParent<KinematicCharacterMotor>() != null) return true;
#endif
        if (col.GetComponentInParent<ObstacleBase>() != null) return true;
        return false;
    }

    private static void Add(ref Bounds bounds, ref bool found, Bounds extra)
    {
        if (!found)
        {
            bounds = extra;
            found = true;
        }
        else
        {
            bounds.Encapsulate(extra);
        }
    }

    private void OnDrawGizmos()
    {
        if (!hasBounds) return;
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.8f);
        Vector3 size = playArea.size;
        size.y += settings.skyLimitHeight;
        Gizmos.DrawWireCube(playArea.center + Vector3.up * settings.skyLimitHeight * 0.5f, size);
    }
}
