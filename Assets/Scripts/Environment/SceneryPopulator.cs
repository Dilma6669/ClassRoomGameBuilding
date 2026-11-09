using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Scatter scenery (trees, rocks, bushes, ruins...) across the terrain, or place one by hand.
/// Scenery is just for looks (plus optional bumping into it) - no gameplay, no movement, so it's cheap.
///
/// HOW TO USE: drag one or more models from Assets/MyModels into "Scenery Models",
/// set how many you want, then click Scatter.
/// </summary>
public class SceneryPopulator : MonoBehaviour
{
    public const string ContainerName = "Scenery (auto)";

    [Tooltip("Drag trees, rocks, bushes... from Assets/MyModels here. Scatter picks randomly from this list.")]
    public List<GameObject> sceneryModels = new List<GameObject>();

    [Tooltip("How many to scatter each time you click Scatter.")]
    [Range(1, 500)] public int howMany = 30;

    [Tooltip("Random size range (1 = the model's normal size).")]
    public Vector2 sizeRange = new Vector2(0.8f, 1.3f);

    [Tooltip("Give each one a random turn so they don't all look the same.")]
    public bool randomTurn = true;

    [Tooltip("Don't put scenery on slopes steeper than this (degrees).")]
    [Range(0f, 90f)] public float maxSlope = 35f;

    [Tooltip("Keep this far from the edge of the terrain.")]
    [Range(0f, 100f)] public float edgePadding = 10f;

    [Tooltip("Tilt with the ground (good for rocks; turn off for trees so they grow straight up).")]
    public bool tiltWithGround = false;

    [Tooltip("Sink a little into the ground so nothing looks like it's floating on bumpy ground.")]
    [Range(0f, 2f)] public float sinkIntoGround = 0.1f;

    [Tooltip("The player bumps into scenery (on), or walks straight through it (off).")]
    public bool solid = true;

    private Terrain FindTerrain()
    {
        Terrain t = GetComponentInChildren<Terrain>();
        if (t == null) t = GetComponentInParent<Terrain>();
        if (t == null) t = Terrain.activeTerrain;
        return t;
    }

#if UNITY_EDITOR
    private Transform GetContainer()
    {
        Transform container = transform.Find(ContainerName);
        if (container == null)
        {
            container = new GameObject(ContainerName).transform;
            container.SetParent(transform, false);
            Undo.RegisterCreatedObjectUndo(container.gameObject, "Create Scenery");
        }
        return container;
    }

    public int Scatter()
    {
        Terrain terrain = FindTerrain();
        List<GameObject> models = sceneryModels.FindAll(m => m != null);
        if (terrain == null || models.Count == 0) return 0;

        Vector3 origin = terrain.GetPosition();
        Vector3 size = terrain.terrainData.size;
        int placed = 0;

        for (int attempt = 0; attempt < howMany * 10 && placed < howMany; attempt++)
        {
            float x = Random.Range(origin.x + edgePadding, origin.x + size.x - edgePadding);
            float z = Random.Range(origin.z + edgePadding, origin.z + size.z - edgePadding);

            // Skip steep slopes
            Vector2 normalised = new Vector2((x - origin.x) / size.x, (z - origin.z) / size.z);
            if (terrain.terrainData.GetSteepness(normalised.x, normalised.y) > maxSlope) continue;

            Vector3 normal = terrain.terrainData.GetInterpolatedNormal(normalised.x, normalised.y);
            float y = terrain.SampleHeight(new Vector3(x, 0f, z)) + origin.y;

            if (PlaceAt(models[Random.Range(0, models.Count)], new Vector3(x, y, z), normal, "Scatter Scenery") != null) placed++;
        }
        return placed;
    }

    /// <summary>Places one random scenery model on the ground at (or under) a point.</summary>
    public GameObject PlaceOne(Vector3 point)
    {
        List<GameObject> models = sceneryModels.FindAll(m => m != null);
        if (models.Count == 0) return null;

        Vector3 normal = Vector3.up;
        if (Physics.Raycast(point + Vector3.up * 500f, Vector3.down, out RaycastHit hit, 2000f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            point = hit.point;
            normal = hit.normal;
        }
        GameObject go = PlaceAt(models[Random.Range(0, models.Count)], point, normal, "Place Scenery");
        if (go != null) Selection.activeGameObject = go;
        return go;
    }

    private GameObject PlaceAt(GameObject model, Vector3 groundPoint, Vector3 groundNormal, string undoName)
    {
        GameObject go = PrefabUtility.IsPartOfPrefabAsset(model)
            ? (GameObject)PrefabUtility.InstantiatePrefab(model, GetContainer())
            : Instantiate(model, GetContainer());
        go.name = model.name;

        Quaternion turn = randomTurn ? Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) : Quaternion.identity;
        Quaternion tilt = tiltWithGround ? Quaternion.FromToRotation(Vector3.up, groundNormal) : Quaternion.identity;
        go.transform.rotation = tilt * turn;
        go.transform.localScale = Vector3.one * Random.Range(Mathf.Min(sizeRange.x, sizeRange.y), Mathf.Max(sizeRange.x, sizeRange.y));

        // Stand the bottom of the model on the ground
        go.transform.position = groundPoint;
        float bottom = LowestPoint(go);
        go.transform.position += Vector3.up * (groundPoint.y - bottom - sinkIntoGround);

        // Never animate or simulate scenery
        foreach (Animator a in go.GetComponentsInChildren<Animator>(true)) a.enabled = false;
        foreach (Rigidbody rb in go.GetComponentsInChildren<Rigidbody>(true)) Object.DestroyImmediate(rb);
        foreach (Collider c in go.GetComponentsInChildren<Collider>(true)) c.enabled = solid;

        if (solid)
        {
            // Bumpable: add colliders from the model's own shape (where it doesn't have any)
            foreach (MeshFilter mf in go.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh != null && mf.GetComponent<Collider>() == null) mf.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
            }
            foreach (SkinnedMeshRenderer smr in go.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (smr.sharedMesh != null && smr.GetComponent<Collider>() == null) smr.gameObject.AddComponent<MeshCollider>().sharedMesh = smr.sharedMesh;
            }
        }

        GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
        Undo.RegisterCreatedObjectUndo(go, undoName);
        return go;
    }

    private static float LowestPoint(GameObject go)
    {
        float lowest = float.PositiveInfinity;
        foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
        {
            Mesh mesh = r is SkinnedMeshRenderer smr ? smr.sharedMesh : r.GetComponent<MeshFilter>()?.sharedMesh;
            if (mesh == null) continue;
            Bounds b = mesh.bounds;
            Matrix4x4 m = r.transform.localToWorldMatrix;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                lowest = Mathf.Min(lowest, m.MultiplyPoint3x4(corner).y);
            }
        }
        return float.IsInfinity(lowest) ? go.transform.position.y : lowest;
    }

    public int Clear()
    {
        Transform container = transform.Find(ContainerName);
        if (container == null) return 0;
        int count = container.childCount;
        Undo.DestroyObjectImmediate(container.gameObject);
        return count;
    }
#endif
}
