using UnityEngine;

/// <summary>
/// Holds scenery (trees, rocks...) that sits ON a platform, so it moves, spins and floats with it.
/// The platform's moving part is stretched to its Width / Thickness / Depth, so this holder cancels
/// that stretch: the trees keep their own shape and size. Made automatically when scenery is placed on a platform.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public class PlatformSceneryHolder : MonoBehaviour
{
    public const string HolderName = "Scenery on this platform (auto)";

    /// <summary>The holder on a platform's moving part (made if it isn't there yet).</summary>
    public static Transform For(PlatformLogic platform)
    {
        Transform target = platform != null ? platform.TargetChild : null;
        if (target == null) return null;
        Transform holder = target.Find(HolderName);
        if (holder == null)
        {
            holder = new GameObject(HolderName).transform;
            holder.SetParent(target, false);
            holder.gameObject.AddComponent<PlatformSceneryHolder>();
#if UNITY_EDITOR
            UnityEditor.Undo.RegisterCreatedObjectUndo(holder.gameObject, "Scenery on platform");
#endif
        }
        holder.GetComponent<PlatformSceneryHolder>().KeepUnstretched();
        return holder;
    }

    private void LateUpdate() => KeepUnstretched();

    public void KeepUnstretched()
    {
        Transform parent = transform.parent;
        if (parent == null) return;
        Vector3 s = parent.localScale;
        Vector3 wanted = new Vector3(1f / Mathf.Max(s.x, 0.0001f), 1f / Mathf.Max(s.y, 0.0001f), 1f / Mathf.Max(s.z, 0.0001f));
        if (transform.localScale != wanted) transform.localScale = wanted;
        if (transform.localPosition != Vector3.zero) transform.localPosition = Vector3.zero;
        if (transform.localRotation != Quaternion.identity) transform.localRotation = Quaternion.identity;
    }
}
