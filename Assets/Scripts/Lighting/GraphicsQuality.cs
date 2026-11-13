using UnityEngine;

/// <summary>
/// The player's Graphics choice (Esc menu): Low / Medium / High, saved on this computer.
/// Low and Medium turn shadows, lights and screen effects down so weak laptops run smoothly (and use less power).
/// High = everything the level asks for. Only changes things while playing (never the project's own settings).
/// </summary>
public static class GraphicsQuality
{
    public enum Level { Low, Medium, High }

    private const string PrefKey = "GraphicsQuality";

    public static event System.Action Changed;

    public static Level Current
    {
        get => (Level)Mathf.Clamp(PlayerPrefs.GetInt(PrefKey, (int)Level.High), 0, 2);
        set
        {
            PlayerPrefs.SetInt(PrefKey, (int)value);
            PlayerPrefs.Save();
            Apply();
        }
    }

    /// <summary>High -> Low -> Medium -> High (one button in the Esc menu).</summary>
    public static void Next() => Current = Current == Level.High ? Level.Low : Current + 1;

    public static string Label => "Graphics: " + Current;

    // The project's own values, taken the first time we change anything (put back when Play stops in the Editor)
    private static bool saved;
    private static float projectShadowDistance;
    private static int projectCascades, projectPixelLights, projectAntiAliasing;
    private static ShadowQuality projectShadows;
    private static ShadowResolution projectShadowResolution;
    private static float projectLodBias;

    /// <summary>The shadow distance the level asks for (0 = the project's own).</summary>
    public static float LevelShadowDistance { get; set; }

    /// <summary>Sets Unity's quality values for the chosen level. Play mode / builds only.</summary>
    public static void Apply()
    {
        if (!Application.isPlaying) return;
        Remember();

        float wanted = LevelShadowDistance > 0f ? LevelShadowDistance : projectShadowDistance;
        switch (Current)
        {
            case Level.Low:
                QualitySettings.shadows = ShadowQuality.HardOnly;
                QualitySettings.shadowResolution = ShadowResolution.Low;
                QualitySettings.shadowDistance = Mathf.Min(wanted, 40f);
                QualitySettings.shadowCascades = 1;
                QualitySettings.pixelLightCount = 1;
                QualitySettings.antiAliasing = 0;
                QualitySettings.lodBias = 0.7f;
                break;
            case Level.Medium:
                QualitySettings.shadows = ShadowQuality.All;
                QualitySettings.shadowResolution = ShadowResolution.Medium;
                QualitySettings.shadowDistance = Mathf.Min(wanted, 80f);
                QualitySettings.shadowCascades = 2;
                QualitySettings.pixelLightCount = 2;
                QualitySettings.antiAliasing = 0;
                QualitySettings.lodBias = 1f;
                break;
            default:
                QualitySettings.shadows = projectShadows;
                QualitySettings.shadowResolution = projectShadowResolution;
                QualitySettings.shadowDistance = wanted;
                QualitySettings.shadowCascades = projectCascades;
                QualitySettings.pixelLightCount = projectPixelLights;
                QualitySettings.antiAliasing = projectAntiAliasing;
                QualitySettings.lodBias = projectLodBias;
                break;
        }
        ApplyGrass();
        Changed?.Invoke();
    }

    // Each ground's own grass distance/thickness (scene values, so they go back by themselves when Play stops)
    private static readonly System.Collections.Generic.Dictionary<Terrain, Vector2> grassSettings = new System.Collections.Generic.Dictionary<Terrain, Vector2>();

    /// <summary>Less grass, less far away on Low and Medium (grass is one of the slowest things to draw).</summary>
    private static void ApplyGrass()
    {
        foreach (Terrain terrain in Terrain.activeTerrains)
        {
            if (terrain == null) continue;
            if (!grassSettings.TryGetValue(terrain, out Vector2 own))
            {
                own = new Vector2(terrain.detailObjectDistance, terrain.detailObjectDensity);
                grassSettings[terrain] = own;
            }
            switch (Current)
            {
                case Level.Low:
                    terrain.detailObjectDistance = Mathf.Min(own.x, 25f);
                    terrain.detailObjectDensity = own.y * 0.4f;
                    break;
                case Level.Medium:
                    terrain.detailObjectDistance = Mathf.Min(own.x, 45f);
                    terrain.detailObjectDensity = own.y * 0.7f;
                    break;
                default:
                    terrain.detailObjectDistance = own.x;
                    terrain.detailObjectDensity = own.y;
                    break;
            }
        }
    }

    private static void Remember()
    {
        if (saved) return;
        saved = true;
        projectShadowDistance = QualitySettings.shadowDistance;
        projectCascades = QualitySettings.shadowCascades;
        projectPixelLights = QualitySettings.pixelLightCount;
        projectAntiAliasing = QualitySettings.antiAliasing;
        projectShadows = QualitySettings.shadows;
        projectShadowResolution = QualitySettings.shadowResolution;
        projectLodBias = QualitySettings.lodBias;
#if UNITY_EDITOR
        // Quality changes made in Play would otherwise stick in the project after Play stops
        UnityEditor.EditorApplication.playModeStateChanged += PutBack;
#endif
    }

#if UNITY_EDITOR
    private static void PutBack(UnityEditor.PlayModeStateChange state)
    {
        if (state != UnityEditor.PlayModeStateChange.ExitingPlayMode || !saved) return;
        UnityEditor.EditorApplication.playModeStateChanged -= PutBack;
        QualitySettings.shadowDistance = projectShadowDistance;
        QualitySettings.shadowCascades = projectCascades;
        QualitySettings.pixelLightCount = projectPixelLights;
        QualitySettings.antiAliasing = projectAntiAliasing;
        QualitySettings.shadows = projectShadows;
        QualitySettings.shadowResolution = projectShadowResolution;
        QualitySettings.lodBias = projectLodBias;
        saved = false;
        LevelShadowDistance = 0f;
    }
#endif
}
