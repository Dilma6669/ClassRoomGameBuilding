using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Makes anything see-through (ghostly creatures, glass platforms, teleporter pads...).
/// 'How see-through' 0% = solid, 100% = invisible (but still there: you can still bump into it).
///
/// How it works: each of the object's materials gets a see-through copy (same picture and colour) while the game runs.
/// In the editor the copies are only a preview: they're never saved, so the level keeps its normal materials.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public class SeeThrough : MonoBehaviour
{
    [Tooltip("0% = solid, 50% = half see-through, 100% = invisible (you can still bump into it).")]
    [Range(0f, 100f)] public float howSeeThrough = 40f;

    // The normal materials, so they can be put back (and so saving never keeps the see-through copies)
    [System.Serializable]
    private class Original { public Renderer renderer; public Material[] materials; }
    [SerializeField, HideInInspector] private List<Original> originals = new List<Original>();

    [System.NonSerialized] private readonly Dictionary<Material, Material> copies = new Dictionary<Material, Material>();
    [System.NonSerialized] private float appliedAmount = -1f;
    [System.NonSerialized] private bool applied;
    private int checkFrame;

    /// <summary>Everything currently made see-through (the editor puts them back before saving).</summary>
    public static readonly HashSet<SeeThrough> Active = new HashSet<SeeThrough>();

    // Saved "Fade" materials in Assets/Resources (see CopyOf)
    public const string TemplateName = "SeeThroughFade";
    public const string TemplateGlowName = "SeeThroughFadeGlow";

    private void OnEnable()
    {
        Active.Add(this);
        Apply();
    }

    private void OnDisable()
    {
        Active.Remove(this);
        Restore();
    }

    private void OnDestroy()
    {
        Restore();
        foreach (Material m in copies.Values) if (m != null) DestroyImmediate(m);
        copies.Clear();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Can't change materials inside OnValidate: do it straight after
        UnityEditor.EditorApplication.delayCall -= DelayedApply;
        UnityEditor.EditorApplication.delayCall += DelayedApply;
    }

    private void DelayedApply()
    {
        if (this != null && isActiveAndEnabled) { appliedAmount = -1f; Apply(); }
    }
#endif

    private void Update()
    {
        // The slider moved, or a model was swapped (new renderers)? Do it again.
        if (!Mathf.Approximately(appliedAmount, howSeeThrough)) Apply();
        else if (++checkFrame % 60 == 0 && NeedsRefresh()) { Restore(); Apply(); }
    }

    private bool NeedsRefresh()
    {
        foreach (Renderer r in Renderers())
            if (!originals.Exists(o => o.renderer == r)) return true;
        return false;
    }

    private IEnumerable<Renderer> Renderers()
    {
        foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
        {
            if (r is ParticleSystemRenderer || r is TrailRenderer || r is LineRenderer) continue;
            // A see-through platform leaves the trees and teleporters standing on it alone
            PlatformSceneryHolder holder = r.GetComponentInParent<PlatformSceneryHolder>(true);
            if (holder != null && holder.transform != transform && holder.transform.IsChildOf(transform)) continue;
            yield return r;
        }
    }

    /// <summary>How solid a renderer should be drawn (1 = solid): for scripts that colour things with their own alpha.</summary>
    public static float AlphaFor(Component c)
    {
        SeeThrough s = c != null ? c.GetComponentInParent<SeeThrough>() : null;
        return s != null && s.isActiveAndEnabled ? 1f - Mathf.Clamp01(s.howSeeThrough / 100f) : 1f;
    }

    /// <summary>Swaps in the see-through copies at the current amount.</summary>
    public void Apply()
    {
        if (!applied)
        {
            originals.RemoveAll(o => o.renderer == null);
            foreach (Renderer r in Renderers())
                if (!originals.Exists(o => o.renderer == r)) originals.Add(new Original { renderer = r, materials = r.sharedMaterials });
        }

        float alpha = 1f - Mathf.Clamp01(howSeeThrough / 100f);
        foreach (Original o in originals)
        {
            if (o.renderer == null) continue;
            Material[] mats = new Material[o.materials.Length];
            for (int i = 0; i < mats.Length; i++) mats[i] = CopyOf(o.materials[i], alpha);
            o.renderer.sharedMaterials = mats;
        }
        applied = true;
        appliedAmount = howSeeThrough;
        Teleporter pad = GetComponent<Teleporter>();
        if (pad != null) pad.ApplyColour(); // its colour carries the see-through amount too
    }

    /// <summary>Puts the normal materials back.</summary>
    public void Restore()
    {
        // (not only when applied: after a script reload the copies are still on the renderers)
        foreach (Original o in originals)
            if (o.renderer != null && o.materials != null) o.renderer.sharedMaterials = o.materials;
        applied = false;
        appliedAmount = -1f;
    }

    private Material CopyOf(Material source, float alpha)
    {
        if (source == null) return null;
        if (!copies.TryGetValue(source, out Material copy) || copy == null)
        {
            bool glows = source.IsKeywordEnabled("_EMISSION") && source.HasProperty("_EmissionColor");
            // Start from a saved see-through material in Resources: a built game only has the shader's see-through
            // version if a saved material uses it (a material made from scratch here would draw solid in the build).
            Material template = Resources.Load<Material>(glows ? TemplateGlowName : TemplateName);
            copy = template != null ? new Material(template) : new Material(Shader.Find("Standard"));
            copy.name = source.name + " (see-through)";
            copy.hideFlags = HideFlags.DontSave;
            // Keep its picture and colour (Standard, glTF and most other shaders)
            Texture tex = Pick<Texture>(source, "_MainTex", "baseColorTexture", "_BaseMap", "_BaseColorMap");
            if (tex != null) copy.mainTexture = tex;
            copy.SetFloat("_Glossiness", source.HasProperty("_Glossiness") ? source.GetFloat("_Glossiness") : 0.2f);
            if (glows)
            {
                copy.EnableKeyword("_EMISSION");
                copy.SetColor("_EmissionColor", source.GetColor("_EmissionColor"));
            }
            // Standard shader "Fade" mode
            copy.SetFloat("_Mode", 2f);
            copy.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            copy.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            copy.SetInt("_ZWrite", 0);
            copy.DisableKeyword("_ALPHATEST_ON");
            copy.EnableKeyword("_ALPHABLEND_ON");
            copy.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            copy.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            copies[source] = copy;
        }
        Color c = Color.white;
        if (source.HasProperty("_Color")) c = source.GetColor("_Color");
        else if (source.HasProperty("baseColorFactor")) c = source.GetColor("baseColorFactor");
        else if (source.HasProperty("_BaseColor")) c = source.GetColor("_BaseColor");
        c.a *= alpha;
        copy.color = c;
        return copy;
    }

    private static T Pick<T>(Material m, params string[] names) where T : Object
    {
        foreach (string n in names)
            if (m.HasProperty(n) && m.GetTexture(n) is T t) return t;
        return null;
    }
}
