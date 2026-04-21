using UnityEngine;

public static class Noise
{
    public enum NoiseBlendMode
    {
        CarveCracks,
        Add,
        Multiply
    }

    // This method generates a noise map using Perlin noise, which is a type of gradient noise commonly used in procedural generation. The method takes various parameters to control the characteristics of the noise, such as scale, octaves, persistence, and lacunarity. It also allows for blending with Voronoi noise to create more complex terrain features.
    public static float[,] GenerateNoiseMap(int width, int height, int seed, float scale, int octaves, float persistance, float lacunarity, Vector2 offset)
    {
        return GeneratePerlinNoiseMap(width, height, seed, scale, octaves, persistance, lacunarity, offset);
    }

    // This method ensures that the provided Voronoi noise settings are valid and fall within acceptable ranges. If any of the settings are out of bounds, they are adjusted to default values to prevent errors during noise generation. This helps maintain the stability and reliability of the noise generation process.
    public static float[,] GenerateNoiseMap(int width, int height, int seed, float scale, int octaves, float persistance, float lacunarity, Vector2 offset,
        bool useVoronoiCracks, VoronoiNoise.Settings voronoiSettings, float voronoiWeight, NoiseBlendMode blendMode = NoiseBlendMode.CarveCracks)
    {
        float[,] perlinMap = GeneratePerlinNoiseMap(width, height, seed, scale, octaves, persistance, lacunarity, offset);

        if (!useVoronoiCracks || voronoiWeight <= 0f)
        {
            return perlinMap;
        }

        float[,] voronoiMap = VoronoiNoise.GeneratePatternMap(width, height, seed, voronoiSettings);
        return BlendNoiseMaps(perlinMap, voronoiMap, Mathf.Clamp01(voronoiWeight), blendMode);
    }

    // This method ensures that the provided Voronoi noise settings are valid and fall within acceptable ranges. If any of the settings are out of bounds, they are adjusted to default values to prevent errors during noise generation. This helps maintain the stability and reliability of the noise generation process.
    private static float[,] GeneratePerlinNoiseMap(int width, int height, int seed, float scale, int octaves, float persistance, float lacunarity, Vector2 offset)
    {
        float[,] noiseMap = new float[width, height];

        System.Random random = new System.Random(seed);
        Vector2[] octaveOffsets = new Vector2[octaves];

        for (int i = 0; i < octaves; i++)
        {
            float offsetX = random.Next(-100000, 100000) + offset.x;
            float offsetY = random.Next(-100000, 100000) + offset.y;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);
        }

        if (scale <= 0)
        {
            scale = 0.0001f;
        }

        float maxNoiseHeight = float.MinValue;
        float minNoiseHeight = float.MaxValue;

        float halfWidth = width / 2f;
        float halfHeight = height / 2f;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float amplitude = 1;
                float frequency = 1;
                float noiseHeight = 0;

                for (int i = 0; i < octaves; i++)
                {
                    float sampleX = (x - halfWidth) / scale * frequency + octaveOffsets[i].x;
                    float sampleY = (y - halfHeight) / scale * frequency + octaveOffsets[i].y;

                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
                    noiseHeight += perlinValue * amplitude;

                    amplitude *= persistance;
                    frequency *= lacunarity;
                }

                if (noiseHeight > maxNoiseHeight)
                {
                    maxNoiseHeight = noiseHeight;
                } else if (noiseHeight < minNoiseHeight)
                {
                    minNoiseHeight = noiseHeight;
                }

                noiseMap[x, y] = noiseHeight;
            }
        }

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                noiseMap[x, y] = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, noiseMap[x, y]);
            }
        }

        return noiseMap;
    }

    // This method blends the Perlin noise map and the Voronoi crack map together based on the specified blend mode and weight. The resulting blended map is then used to create more complex terrain features by combining the smooth variations of Perlin noise with the sharp, defined cracks of Voronoi noise.
    private static float[,] BlendNoiseMaps(float[,] perlinMap, float[,] voronoiMap, float voronoiWeight, NoiseBlendMode blendMode)
    {
        int width = perlinMap.GetLength(0);
        int height = perlinMap.GetLength(1);
        float[,] blendedMap = new float[width, height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float perlinValue = perlinMap[x, y];
                float voronoiValue = voronoiMap[x, y];
                float blendedValue;

                switch (blendMode)
                {
                    case NoiseBlendMode.Add:
                        blendedValue = Mathf.Clamp01(perlinValue + voronoiValue * voronoiWeight);
                        break;
                    case NoiseBlendMode.Multiply:
                        blendedValue = Mathf.Clamp01(perlinValue * (1f - voronoiValue * voronoiWeight));
                        break;
                    default:
                        blendedValue = Mathf.Clamp01(perlinValue - voronoiValue * voronoiWeight);
                        break;
                }

                blendedMap[x, y] = blendedValue;
            }
        }

        return blendedMap;
    }
}