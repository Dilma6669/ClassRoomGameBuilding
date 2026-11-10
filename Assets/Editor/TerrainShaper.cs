using UnityEngine;

/// <summary>
/// Makes a new shape for a terrain: a valley in the middle (around 'valley') with gentle hills,
/// rising into a ring of ridged mountains. Each seed gives different mountains.
/// The valley itself is flattened afterwards by the caller (TutorialLevelBuilder.FlattenArea).
/// </summary>
public static class TerrainShaper
{
    public static void NewMountains(Terrain terrain, int seed, Rect valley, float valleyHeight)
    {
        TerrainData data = terrain.terrainData;
        Vector3 origin = terrain.GetPosition();
        int res = data.heightmapResolution;
        float[,] heights = new float[res, res];

        System.Random rng = new System.Random(seed);
        float ox = (float)rng.NextDouble() * 1000f, oz = (float)rng.NextDouble() * 1000f;
        float hx = (float)rng.NextDouble() * 1000f, hz = (float)rng.NextDouble() * 1000f;
        float maxHeight = data.size.y * 0.4f;

        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                float wx = origin.x + x / (float)(res - 1) * data.size.x;
                float wz = origin.z + y / (float)(res - 1) * data.size.z;

                // How far outside the valley this point is
                float dx = Mathf.Max(valley.xMin - wx, 0f, wx - valley.xMax);
                float dz = Mathf.Max(valley.yMin - wz, 0f, wz - valley.yMax);
                float distance = Mathf.Sqrt(dx * dx + dz * dz);

                // Ridged noise: sharp mountain ridges and peaks
                float ridges = 0f, amp = 1f, freq = 1f / 220f, total = 0f;
                for (int octave = 0; octave < 5; octave++)
                {
                    float v = 1f - Mathf.Abs(Mathf.PerlinNoise(wx * freq + ox, wz * freq + oz) * 2f - 1f);
                    ridges += v * v * amp;
                    total += amp;
                    amp *= 0.5f;
                    freq *= 2.1f;
                }
                ridges /= total;

                float hills = Mathf.PerlinNoise(wx / 70f + hx, wz / 70f + hz) * 8f * Mathf.Clamp01(distance / 40f);
                float mountains = Mathf.SmoothStep(0f, 1f, (distance - 40f) / 300f); // 0 near the valley, 1 far away
                float metres = valleyHeight + hills + mountains * (30f + ridges * maxHeight);
                heights[y, x] = Mathf.Clamp01(metres / data.size.y);
            }
        }
        data.SetHeights(0, 0, heights);
    }
}
