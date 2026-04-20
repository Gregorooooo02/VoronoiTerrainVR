using Unity.VisualScripting;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    public enum DrawMode { NoiseMap, ColorMap, Mesh };
    [SerializeField] private DrawMode drawMode;
    
    [SerializeField] private int width = 256;
    [SerializeField] private int height = 256;
    [SerializeField] private float noiseScale = 20f;

    [SerializeField] private int octaves = 1;
    [Range(0, 1)]
    [SerializeField] private float persistance = 1;
    [SerializeField] private float lacunarity = 1;

    [SerializeField] private int seed = 0;
    [SerializeField] private Vector2 offset;
    [SerializeField] private float meshHeightMultiplier = 1f;

    [SerializeField] public bool autoUpdate = false;

    [SerializeField] private TerrainType[] regions;

    public void GenerateMap()
    {
        float[,] noiseMap = Noise.GenerateNoiseMap(width, height, seed, noiseScale, octaves, persistance, lacunarity, offset);
        Color[] colorMap = new Color[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float currentHeight = noiseMap[x, y];

                foreach (TerrainType region in regions)
                {
                    if (currentHeight <= region.height)
                    {
                        colorMap[y * width + x] = region.color;
                        break;
                    }
                }
            }
        }

        MapDisplay display = FindAnyObjectByType<MapDisplay>();
        if (drawMode == DrawMode.NoiseMap)
        {
            display.DrawTexture(TextureGenerator.TextureFromHeightMap(noiseMap));
        }
        else if (drawMode == DrawMode.ColorMap)
        {
            display.DrawTexture(TextureGenerator.TextureFromColorMap(colorMap, width, height));
        }
        else if (drawMode == DrawMode.Mesh)
        {
            display.DrawMesh(MeshGenerator.GenerateTerrainMesh(noiseMap, meshHeightMultiplier), TextureGenerator.TextureFromColorMap(colorMap, width, height));
        }
    }

    public void OnValidate()
    {
        if (width < 1)
        {
            width = 1;
        }
        if (height < 1)
        {
            height = 1;
        }
        if (lacunarity < 1)
        {
            lacunarity = 1;
        }
        if (octaves < 0)
        {
            octaves = 0;
        }
    }
    
    [System.Serializable]
    public struct TerrainType
    {
        public string name;
        public float height;
        public Color color;
    }
}