using UnityEngine;

public static class VoronoiNoise
{
    public enum PatternMode
    {
        Cracks,
        Cellular
    }

    [System.Serializable]
    public struct Settings
    {
        // Cell density controls how many Voronoi cells are generated across the noise map. Higher values create more, smaller cells, while lower values create fewer, larger cells.
        [Min(1)] public int cellDensity;
        // Jitter adds randomness to the position of the Voronoi feature points within each cell. A value of 0 means feature points are at the center of each cell, while a value of 1 allows them to be anywhere within the cell.
        [Range(0f, 1f)] public float jitter;
        // Crack width determines how wide the cracks will be in the final noise map. Smaller values create thinner cracks, while larger values create wider cracks.
        [Min(0.0001f)] public float crackWidth;
        // Edge sharpness controls how sharply defined the cracks are. Higher values create more distinct, sharper cracks, while lower values create softer, more blended cracks.
        [Min(0.1f)] public float edgeSharpness;
        // Offset allows you to shift the entire Voronoi pattern across the noise map. This can be used to create variation between different noise maps or to animate the noise over time.
        public Vector2 offset;
        public PatternMode patternMode;

        // Provides a default set of settings for the Voronoi noise, which can be used if no custom settings are provided. This ensures that the noise generation will still function with reasonable parameters even if the user does not specify them.
        public static Settings Default => new Settings
        {
            cellDensity = 12,
            jitter = 0.9f,
            crackWidth = 0.08f,
            edgeSharpness = 1.25f,
            offset = Vector2.zero,
            patternMode = PatternMode.Cracks
        };
    }

    public static float[,] GeneratePatternMap(int width, int height, int seed, Settings settings)
    {
        Settings safeSettings = Sanitize(settings);
        float[,] patternMap = new float[width, height];

        float minValue = float.MaxValue;
        float maxValue = float.MinValue;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float sampleX = (x / (float)width) * safeSettings.cellDensity + safeSettings.offset.x;
                float sampleY = (y / (float)height) * safeSettings.cellDensity + safeSettings.offset.y;

                float patternValue = safeSettings.patternMode == PatternMode.Cellular
                    ? SampleVoronoiCellular(sampleX, sampleY, seed, safeSettings.jitter, safeSettings.edgeSharpness)
                    : SampleVoronoiCrack(sampleX, sampleY, seed, safeSettings.jitter, safeSettings.crackWidth, safeSettings.edgeSharpness);

                patternMap[x, y] = patternValue;

                if (patternValue < minValue)
                {
                    minValue = patternValue;
                }
                else if (patternValue > maxValue)
                {
                    maxValue = patternValue;
                }
            }
        }

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                patternMap[x, y] = Mathf.InverseLerp(minValue, maxValue, patternMap[x, y]);
            }
        }

        return patternMap;
    }

    public static float[,] GenerateCrackMap(int width, int height, int seed, Settings settings)
    {
        settings.patternMode = PatternMode.Cracks;
        return GeneratePatternMap(width, height, seed, settings);
    }

    private static float SampleVoronoiCrack(float x, float y, int seed, float jitter, float crackWidth, float edgeSharpness)
    {
        GetNearestDistances(x, y, seed, jitter, out float nearestDistance, out float secondNearestDistance);

        float edgeDistance = secondNearestDistance - nearestDistance;
        float crackMask = 1f - Mathf.SmoothStep(0f, crackWidth, edgeDistance);
        return Mathf.Pow(Mathf.Clamp01(crackMask), edgeSharpness);
    }

    private static float SampleVoronoiCellular(float x, float y, int seed, float jitter, float edgeSharpness)
    {
        GetNearestDistances(x, y, seed, jitter, out float nearestDistance, out _);

        float cellValue = 1f - Mathf.SmoothStep(0f, 1.0f, nearestDistance);
        return Mathf.Pow(Mathf.Clamp01(cellValue), edgeSharpness);
    }

    private static void GetNearestDistances(float x, float y, int seed, float jitter, out float nearestDistance, out float secondNearestDistance)
    {
        int baseCellX = Mathf.FloorToInt(x);
        int baseCellY = Mathf.FloorToInt(y);

        nearestDistance = float.MaxValue;
        secondNearestDistance = float.MaxValue;

        for (int yOffset = -1; yOffset <= 1; yOffset++)
        {
            for (int xOffset = -1; xOffset <= 1; xOffset++)
            {
                int cellX = baseCellX + xOffset;
                int cellY = baseCellY + yOffset;

                Vector2 featurePoint = GetFeaturePoint(cellX, cellY, seed, jitter);
                float distance = Vector2.Distance(new Vector2(x, y), featurePoint);

                if (distance < nearestDistance)
                {
                    secondNearestDistance = nearestDistance;
                    nearestDistance = distance;
                }
                else if (distance < secondNearestDistance)
                {
                    secondNearestDistance = distance;
                }
            }
        }
    }

    private static Vector2 GetFeaturePoint(int cellX, int cellY, int seed, float jitter)
    {
        float randomX = HashTo01(cellX, cellY, seed);
        float randomY = HashTo01(cellX, cellY, seed + 1337);

        float jitterOffsetX = (randomX * 2f - 1f) * jitter;
        float jitterOffsetY = (randomY * 2f - 1f) * jitter;

        return new Vector2(cellX + jitterOffsetX, cellY + jitterOffsetY);
    }

    private static float HashTo01(int x, int y, int seed)
    {
        int value = x * 374761393 + y * 668265263 + seed * 700001;
        value = (value ^ (value >> 13)) * 1274126177;
        value ^= value >> 16;

        return (value & 0x7fffffff) / (float)int.MaxValue;
    }

    private static Settings Sanitize(Settings settings)
    {
        settings.cellDensity = Mathf.Max(1, settings.cellDensity);
        settings.jitter = Mathf.Clamp01(settings.jitter);
        settings.crackWidth = Mathf.Max(0.0001f, settings.crackWidth);
        settings.edgeSharpness = Mathf.Max(0.1f, settings.edgeSharpness);
        return settings;
    }
}
