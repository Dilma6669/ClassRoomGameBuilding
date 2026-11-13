using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

/// <summary>
/// ✨ Graphics & effects for one level (Game Builder > 🏔 Level): film colours, glow shine (bloom), dark edges
/// (vignette), soft corner shadows (ambient occlusion) and how far shadows reach.
/// The effects are made in memory each time (nothing extra is saved into the level) and show in the Game view
/// straight away. The player's Graphics choice (Esc menu: Low / Medium / High) can turn them down.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public class LevelGraphics : MonoBehaviour
{
    public enum FilmLook { Gentle, Strong }

    [Header("🎨 Colours")]
    [Tooltip("Makes the colours look like a film: bright things stay nice instead of going flat white.")]
    [ShowAs("🎨 Film colours")]
    public bool filmColours = true;

    [Tooltip("Gentle = close to normal. Strong = darker shadows and richer colours (a moody film look).")]
    [ShowAs("Film look")]
    public FilmLook filmLook = FilmLook.Gentle;

    [Tooltip("Lighter or darker overall.")]
    [ShowAs("Brightness")]
    [Range(-2f, 2f)] public float brightness = 0f;

    [Tooltip("More or less colourful. All the way left = black and white!")]
    [ShowAs("Colourful")]
    [Range(-100f, 100f)] public float colourful = 10f;

    [Tooltip("More = darker darks and brighter brights.")]
    [ShowAs("Contrast")]
    [Range(-100f, 100f)] public float contrast = 10f;

    [Tooltip("Left = cold and blue, right = warm and orange.")]
    [ShowAs("Cold ↔ warm")]
    [Range(-100f, 100f)] public float warmth = 0f;

    [Header("✨ Glow shine (bloom)")]
    [Tooltip("Bright and glowing things (Glow pickups, teleporters, the sun on the sky) shine a soft light around them.")]
    [ShowAs("✨ Glow shine")]
    public bool glowShine = true;

    [Tooltip("How strong the shine is.")]
    [ShowAs("Shine strength")]
    [Range(0f, 20f)] public float shineStrength = 2f;

    [Tooltip("Only things brighter than this shine. Lower = more things shine.")]
    [ShowAs("Shine starts at brightness")]
    [Range(0f, 5f)] public float shineFrom = 1f;

    [Header("🌑 Dark edges (vignette)")]
    [Tooltip("Darkens the edges of the screen. Great for spooky or menacing levels.")]
    [ShowAs("🌑 Dark edges")]
    public bool darkEdges = true;

    [Tooltip("How dark the edges get.")]
    [ShowAs("Edge darkness")]
    [Range(0f, 1f)] public float edgeDarkness = 0.3f;

    [Tooltip("The colour of the edges (black, or try dark red!).")]
    [ShowAs("Edge colour")]
    public Color edgeColour = Color.black;

    [Header("🌫 Corner shadows (ambient occlusion)")]
    [Tooltip("Soft shadows where things meet (under rocks, in corners). Looks great but makes the game SLOWER: leave off for weak computers.")]
    [ShowAs("🌫 Corner shadows (slower)")]
    public bool cornerShadows = false;

    [Tooltip("How dark the corner shadows are.")]
    [ShowAs("Corner shadow strength")]
    [Range(0f, 4f)] public float cornerShadowStrength = 1f;

    [Header("☀️ Shadows")]
    [Tooltip("How far away things still have shadows (metres). Less = faster and uses less power. About 120 looks the same as 300. Shows when you press Play.")]
    [ShowAs("Shadows reach (m)")]
    [Range(20f, 300f)] public float shadowsReach = 120f;

    [SerializeField, HideInInspector] private PostProcessResources resources;

    private PostProcessProfile profile;
    private PostProcessVolume volume;
    private PostProcessLayer addedLayer;
    private float nextCameraCheck;

    public static LevelGraphics Current { get; private set; }

    private void OnEnable()
    {
        Current = this;
        FindResources();
        GraphicsQuality.Changed += Refresh;
        if (Application.isPlaying)
        {
            GraphicsQuality.LevelShadowDistance = shadowsReach;
            GraphicsQuality.Apply(); // also calls Refresh
        }
        Refresh();
    }

    private void OnDisable()
    {
        GraphicsQuality.Changed -= Refresh;
        if (Current == this) Current = null;
        RemoveEffects();
    }

    private void OnValidate()
    {
        FindResources();
        if (!isActiveAndEnabled) return;
        if (Application.isPlaying && GraphicsQuality.LevelShadowDistance != shadowsReach)
        {
            GraphicsQuality.LevelShadowDistance = shadowsReach;
            GraphicsQuality.Apply();
        }
        Refresh();
    }

    private void Update()
    {
        // The camera may appear later (Create Character, a level loading): check now and then
        if (Time.realtimeSinceStartup < nextCameraCheck) return;
        nextCameraCheck = Time.realtimeSinceStartup + 1f;
        if (addedLayer == null && WantsEffects()) Refresh();
    }

    private void FindResources()
    {
#if UNITY_EDITOR
        if (resources == null)
            resources = UnityEditor.AssetDatabase.LoadAssetAtPath<PostProcessResources>("Packages/com.unity.postprocessing/PostProcessing/PostProcessResources.asset");
#endif
    }

    private bool WantsEffects()
    {
        if (GraphicsQuality.Current == GraphicsQuality.Level.Low && Application.isPlaying) return false;
        return filmColours || glowShine || darkEdges || cornerShadows;
    }

    /// <summary>Builds (or rebuilds) the effects from the settings above and the player's Graphics choice.</summary>
    public void Refresh()
    {
        if (!isActiveAndEnabled) return;
        if (!WantsEffects() || resources == null)
        {
            RemoveEffects();
            return;
        }

        bool high = !Application.isPlaying || GraphicsQuality.Current == GraphicsQuality.Level.High;

        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<PostProcessProfile>();
            profile.name = "Level graphics (made while playing)";
            profile.hideFlags = HideFlags.HideAndDontSave;
        }
        profile.settings.ForEach(s => { if (s != null) DestroyImmediate(s); });
        profile.settings.Clear();

        if (filmColours)
        {
            ColorGrading grading = profile.AddSettings<ColorGrading>();
            grading.enabled.Override(true);
            grading.gradingMode.Override(SystemInfo.supportsComputeShaders ? GradingMode.HighDefinitionRange : GradingMode.LowDefinitionRange);
            grading.tonemapper.Override(filmLook == FilmLook.Strong ? Tonemapper.ACES : Tonemapper.Neutral);
            // The strong (ACES) look darkens a lot: lift it a bit so 'Brightness 0' still looks right
            grading.postExposure.Override(brightness + (filmLook == FilmLook.Strong ? 0.6f : 0f));
            grading.brightness.Override(brightness * 25f); // used by the simple (LDR) version
            grading.saturation.Override(colourful);
            grading.contrast.Override(contrast);
            grading.temperature.Override(warmth);
        }
        if (glowShine)
        {
            Bloom bloom = profile.AddSettings<Bloom>();
            bloom.enabled.Override(true);
            bloom.intensity.Override(shineStrength);
            bloom.threshold.Override(shineFrom);
            bloom.fastMode.Override(!high);
        }
        if (darkEdges)
        {
            Vignette vignette = profile.AddSettings<Vignette>();
            vignette.enabled.Override(true);
            vignette.intensity.Override(edgeDarkness);
            vignette.color.Override(edgeColour);
            vignette.smoothness.Override(0.4f);
        }
        if (cornerShadows && high)
        {
            AmbientOcclusion ao = profile.AddSettings<AmbientOcclusion>();
            ao.enabled.Override(true);
            ao.intensity.Override(cornerShadowStrength);
            ao.mode.Override(SystemInfo.supportsComputeShaders ? AmbientOcclusionMode.MultiScaleVolumetricObscurance : AmbientOcclusionMode.ScalableAmbientObscurance);
        }

        if (volume == null)
        {
            volume = gameObject.AddComponent<PostProcessVolume>();
            volume.hideFlags = HideFlags.HideAndDontSave;
            volume.isGlobal = true;
            volume.priority = 10f;
        }
        volume.sharedProfile = profile;

        Camera cam = FindCamera();
        if (cam == null) return;
        if (glowShine || filmColours) cam.allowHDR = true;
        PostProcessLayer layer = cam.GetComponent<PostProcessLayer>();
        if (layer == null)
        {
            layer = cam.gameObject.AddComponent<PostProcessLayer>();
            layer.hideFlags = HideFlags.DontSave;
            addedLayer = layer;
        }
        layer.Init(resources);
        layer.volumeTrigger = cam.transform;
        layer.volumeLayer |= 1 << gameObject.layer;
        layer.antialiasingMode = PostProcessLayer.Antialiasing.None;
        layer.stopNaNPropagation = true;
    }

    private static Camera FindCamera()
    {
        if (Camera.main != null) return Camera.main;
        foreach (Camera c in FindObjectsByType<Camera>(FindObjectsSortMode.None))
        {
            if (c.enabled && c.targetTexture == null) return c;
        }
        return null;
    }

    private void RemoveEffects()
    {
        if (addedLayer != null) DestroyImmediate(addedLayer);
        addedLayer = null;
        if (volume != null) DestroyImmediate(volume);
        volume = null;
        if (profile != null)
        {
            profile.settings.ForEach(s => { if (s != null) DestroyImmediate(s); });
            DestroyImmediate(profile);
        }
        profile = null;
    }
}
