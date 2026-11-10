using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Makes something glow: a soft coloured light around it (you can see this while editing),
/// and in the game its own surface lights up too. It can gently pulse.
/// Add it with the "✨ Make it glow" button in the Game Builder.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public class Glow : MonoBehaviour
{
    [Tooltip("The glow colour.")]
    public Color colour = new Color(1f, 0.85f, 0.3f);
    [Tooltip("How bright the glow is.")]
    [Range(0.2f, 20f)] public float strength = 1.5f;
    [Tooltip("How far the light spreads around it (metres).")]
    [Range(1f, 200f)] public float lightRange = 4f;
    [Tooltip("Gently get brighter and dimmer.")]
    public bool pulse = true;
    [Tooltip("How many pulses each second (1 = one pulse every second, 0.5 = one every two seconds).")]
    [Range(0.1f, 5f)] public float pulsesPerSecond = 0.5f;

    /// <summary>Set by other scripts (e.g. a crumbling platform counting down) to flash brighter: 0 = normal, 1 = full flash.</summary>
    [System.NonSerialized] public float flash;

    private Light glowLight;
    private readonly List<Material> glowingMaterials = new List<Material>();

    private void OnEnable()
    {
        EnsureLight();
        if (Application.isPlaying) MakeSurfacesGlow();
    }

    private void OnDisable()
    {
        if (glowLight != null) glowLight.enabled = false;
    }

    private void OnDestroy()
    {
        foreach (Material m in glowingMaterials) if (m != null) Destroy(m);
        if (glowLight == null) return;
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            // Can't remove a component while this one is being removed: do it straight after
            Light l = glowLight;
            UnityEditor.EditorApplication.delayCall += () => { if (l != null) DestroyImmediate(l); };
            return;
        }
#endif
        Destroy(glowLight);
    }

    private void EnsureLight()
    {
        if (glowLight == null) glowLight = GetComponent<Light>();
        if (glowLight == null) glowLight = gameObject.AddComponent<Light>();
        glowLight.hideFlags = HideFlags.HideInInspector; // it's controlled from here
        glowLight.type = LightType.Point;
        glowLight.shadows = LightShadows.None;
        glowLight.renderMode = LightRenderMode.Auto;
        glowLight.enabled = true;
    }

    private void MakeSurfacesGlow()
    {
        glowingMaterials.Clear();
        foreach (Renderer r in GetComponentsInChildren<Renderer>())
        {
            if (r is ParticleSystemRenderer) continue;
            foreach (Material m in r.materials) // copies, so the shared material isn't changed
            {
                if (!m.HasProperty("_EmissionColor")) continue;
                m.EnableKeyword("_EMISSION");
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                glowingMaterials.Add(m);
            }
        }
    }

    private void Update()
    {
        if (glowLight == null) EnsureLight();

        float time = Application.isPlaying ? Time.time : (float)(System.DateTime.Now.TimeOfDay.TotalSeconds);
        float wave = pulse ? 0.7f + 0.3f * Mathf.Sin(time * pulsesPerSecond * 2f * Mathf.PI) : 1f;
        wave *= 1f + 2.5f * flash;

        glowLight.color = colour;
        glowLight.intensity = strength * wave;
        glowLight.range = lightRange;

        Color emission = colour * (0.6f * strength * wave);
        foreach (Material m in glowingMaterials) if (m != null) m.SetColor("_EmissionColor", emission);
    }
}
