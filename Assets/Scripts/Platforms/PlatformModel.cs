using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

/// <summary>
/// Swap a platform's plain cylinder for your own 3D model (a floating rock, an island with hills...).
///
/// HOW TO USE: drag a model from Assets/MyModels into "Platform Model".
/// - The model moves, spins and scales with the platform exactly like the cylinder did.
/// - The player and enemies stand on the model's real shape (hills, bumps and all).
/// - Width / Depth on the platform still change its size. Raise / Lower and Turn fine-tune it.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(PlatformLogic))]
public class PlatformModel : MonoBehaviour
{
    public const string HolderName = "Platform Model (auto)";

    [Tooltip("Drag a 3D model here (.fbx, .glb or .obj from Assets/MyModels). Leave empty for the plain cylinder.")]
    public GameObject model;

    [Tooltip("On: the model keeps its shape and fits inside the platform's Width and Depth.\nOff: the model is stretched to fill the platform's Width, Height and Depth exactly.")]
    public bool keepProportions = true;

    [Tooltip("Make the model bigger or smaller.")]
    [Range(0.1f, 50f)] public float size = 1f;

    [Tooltip("Turn the model around.")]
    [Range(-180f, 180f)] public float turn = 0f;

    [Tooltip("Move the model up or down. By default the top of the model sits where the top of the cylinder was.")]
    [Range(-200f, 200f)] public float raiseLower = 0f;

    [SerializeField, HideInInspector] private GameObject builtFrom;
    [SerializeField, HideInInspector] private Transform holder;
    [SerializeField, HideInInspector] private Transform modelInstance;
    [SerializeField, HideInInspector] private Bounds modelBounds;     // model's own size, unscaled
    [SerializeField, HideInInspector] private Bounds originalBounds;  // cylinder's mesh bounds, in its local space

    private PlatformLogic logic;

    private Transform Target
    {
        get
        {
            if (logic == null) logic = GetComponent<PlatformLogic>();
            if (logic != null && logic.TargetChild != null) return logic.TargetChild;
            // Just-created platforms may not have found their moving part yet
            Transform folder = transform.Find("⚠️ DO NOT TOUCH");
            return folder != null && folder.childCount > 0 ? folder.GetChild(0) : null;
        }
    }

    // ------------------------------------------------------------------ Lifecycle

    private void LateUpdate()
    {
        // Runs every frame (also in the editor) so the model follows the Width/Depth sliders
        // and the platform's grow/shrink animation.
        ApplyPlacement();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying || EditorUtility.IsPersistent(this)) return;
        EditorApplication.delayCall -= DelayedRefresh;
        EditorApplication.delayCall += DelayedRefresh;
    }

    private void DelayedRefresh()
    {
        if (this == null || Application.isPlaying) return;
        Refresh();
    }
#endif

    // ------------------------------------------------------------------ Building

    public void Refresh()
    {
        if (model != builtFrom || (model != null && modelInstance == null) || (model == null && holder != null))
        {
            Rebuild();
        }
        else
        {
            ApplyPlacement();
        }
    }

    public void Rebuild()
    {
        Transform target = Target;
        if (target == null) return;

        RemoveModel(target);

        if (model == null)
        {
            SetOriginalShape(target, true);
            builtFrom = null;
            MarkDirty();
            return;
        }

        // Remember the cylinder's size so the model can take its place
        MeshFilter originalFilter = FindOriginalMeshFilter(target);
        originalBounds = originalFilter != null && originalFilter.sharedMesh != null
            ? TransformBounds(originalFilter.sharedMesh.bounds, target.InverseTransformPoint(originalFilter.transform.position), originalFilter.transform.localScale)
            : new Bounds(Vector3.zero, new Vector3(1f, 2f, 1f));

        // A holder that cancels the platform's stretching, so the model is never squashed or sheared
        holder = new GameObject(HolderName).transform;
        holder.SetParent(target, false);

        // The model sits inside a wrapper that we size and turn, so the model keeps its own built-in
        // rotation and scale (FBX files from Blender have a 90 degree turn and x100 scale on them)
        GameObject wrapper = new GameObject(model.name);
        GameObject instance = CreateInstance(model);
        instance.name = model.name;
        instance.transform.SetParent(wrapper.transform, false);
        modelBounds = MeasureMeshes(wrapper.transform);

        wrapper.transform.SetParent(holder, false);
        modelInstance = wrapper.transform;
        builtFrom = model;

        // Collision follows the model's real shape
        foreach (Collider c in instance.GetComponentsInChildren<Collider>(true)) c.enabled = false;
        foreach (Renderer r in instance.GetComponentsInChildren<Renderer>(true))
        {
            Mesh mesh = r is SkinnedMeshRenderer smr ? smr.sharedMesh : r.GetComponent<MeshFilter>()?.sharedMesh;
            if (mesh == null) continue;
            MeshCollider mc = r.gameObject.AddComponent<MeshCollider>();
            mc.sharedMesh = mesh;
            mc.convex = false;
        }

        SetOriginalShape(target, false);
        ApplyPlacement();
        MarkDirty();
    }

    /// <summary>Fits the model to the platform's current size. Cheap enough to run every frame.</summary>
    public void ApplyPlacement()
    {
        if (holder == null || modelInstance == null) return;
        Transform target = holder.parent;
        if (target == null) return;

        // Undo the platform's (possibly uneven) stretch so the holder is "normal" sized
        Vector3 s = target.localScale;
        holder.localScale = new Vector3(1f / Mathf.Max(s.x, 0.0001f), 1f / Mathf.Max(s.y, 0.0001f), 1f / Mathf.Max(s.z, 0.0001f));
        holder.localPosition = Vector3.zero;
        holder.localRotation = Quaternion.identity;

        // The space the cylinder took up, in real-world sizes
        Vector3 space = Vector3.Scale(originalBounds.size, s);
        Vector3 spaceCentre = Vector3.Scale(originalBounds.center, s);
        float spaceTop = originalBounds.max.y * s.y;

        Vector3 m = modelBounds.size;
        Quaternion rotation = Quaternion.Euler(0f, turn, 0f);
        Vector3 scale;
        if (keepProportions)
        {
            float fit = Mathf.Min(space.x / Mathf.Max(m.x, 0.001f), space.z / Mathf.Max(m.z, 0.001f));
            scale = Vector3.one * fit * size;
        }
        else
        {
            scale = new Vector3(space.x / Mathf.Max(m.x, 0.001f), space.y / Mathf.Max(m.y, 0.001f), space.z / Mathf.Max(m.z, 0.001f)) * size;
        }

        // Put the top-centre of the model where the top-centre of the cylinder was
        Vector3 modelTopCentre = new Vector3(modelBounds.center.x, modelBounds.max.y, modelBounds.center.z);
        Vector3 targetTopCentre = new Vector3(spaceCentre.x, spaceTop + raiseLower, spaceCentre.z);

        modelInstance.localRotation = rotation;
        modelInstance.localScale = scale;
        modelInstance.localPosition = targetTopCentre - rotation * Vector3.Scale(modelTopCentre, scale);
    }

    private void RemoveModel(Transform target)
    {
        List<GameObject> old = new List<GameObject>();
        foreach (Transform child in target)
        {
            if (child.name == HolderName) old.Add(child.gameObject);
        }
        foreach (GameObject go in old)
        {
            if (Application.isPlaying) Destroy(go);
            else DestroyImmediate(go);
        }
        holder = null;
        modelInstance = null;
    }

    /// <summary>Show/hide the cylinder and switch its colliders on/off (the model's colliders replace them).</summary>
    private void SetOriginalShape(Transform target, bool on)
    {
        foreach (Renderer r in target.GetComponentsInChildren<Renderer>(true))
        {
            if (IsPartOfModel(r.transform, target)) continue;
            r.enabled = on;
        }
        foreach (Collider c in target.GetComponentsInChildren<Collider>(true))
        {
            if (IsPartOfModel(c.transform, target)) continue;
            c.enabled = on;
        }
    }

    private static bool IsPartOfModel(Transform t, Transform target)
    {
        for (Transform p = t; p != null && p != target; p = p.parent)
        {
            if (p.name == HolderName || p.name == PlatformSceneryHolder.HolderName) return true; // the model, or scenery standing on the platform
        }
        return false;
    }

    private static MeshFilter FindOriginalMeshFilter(Transform target)
    {
        foreach (MeshFilter mf in target.GetComponentsInChildren<MeshFilter>(true))
        {
            if (!IsPartOfModel(mf.transform, target)) return mf;
        }
        return null;
    }

    private static Bounds TransformBounds(Bounds b, Vector3 offset, Vector3 scale)
    {
        return new Bounds(offset + Vector3.Scale(b.center, scale), Vector3.Scale(b.size, scale));
    }

    private static Bounds MeasureMeshes(Transform root)
    {
        Bounds bounds = new Bounds();
        bool found = false;
        foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true))
        {
            Mesh mesh = r is SkinnedMeshRenderer smr ? smr.sharedMesh : r.GetComponent<MeshFilter>()?.sharedMesh;
            if (mesh == null) continue;
            Matrix4x4 m = r.transform.localToWorldMatrix;
            Bounds local = mesh.bounds;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = local.center + Vector3.Scale(local.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                Vector3 world = m.MultiplyPoint3x4(corner);
                if (!found) { bounds = new Bounds(world, Vector3.zero); found = true; }
                else bounds.Encapsulate(world);
            }
        }
        return found ? bounds : new Bounds(Vector3.zero, Vector3.one);
    }

    private GameObject CreateInstance(GameObject source)
    {
#if UNITY_EDITOR
        if (!Application.isPlaying && PrefabUtility.IsPartOfPrefabAsset(source))
        {
            return (GameObject)PrefabUtility.InstantiatePrefab(source);
        }
#endif
        return Instantiate(source);
    }

    private void MarkDirty()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying && gameObject.scene.IsValid())
        {
            EditorUtility.SetDirty(this);
            EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }
#endif
    }
}
