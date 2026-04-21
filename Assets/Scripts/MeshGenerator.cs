using UnityEngine;

public static class MeshGenerator
{
    public static MeshData GenerateTerrainMesh(float[,] heightMap, float heightMultiplier)
    {
        return GenerateTerrainMesh(heightMap, heightMultiplier, 1, 0f);
    }

    public static MeshData GenerateTerrainMesh(float[,] heightMap, float heightMultiplier, int resolutionMultiplier, float smoothingStrength)
    {
        int smoothingPasses = Mathf.Max(1, resolutionMultiplier);
        smoothingStrength = Mathf.Clamp01(smoothingStrength);

        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);

        float[,] sampledHeightMap = smoothingStrength > 0f
            ? SmoothHeightMap(heightMap, smoothingStrength, smoothingPasses)
            : heightMap;

        float topLeftX = (width - 1) / -2f;
        float topLeftZ = (height - 1) / 2f;

        MeshData meshData = new MeshData(width, height);
        int vertexIndex = 0;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                meshData.vertices[vertexIndex] = new Vector3(topLeftX + x, sampledHeightMap[x, y] * heightMultiplier, topLeftZ - y);
                meshData.uvs[vertexIndex] = new Vector2(x / (float)width, y / (float)height);

                if (x < width - 1 && y < height - 1)
                {
                    meshData.AddTriangle(vertexIndex, vertexIndex + width + 1, vertexIndex + width);
                    meshData.AddTriangle(vertexIndex + width + 1, vertexIndex, vertexIndex + 1);
                }

                vertexIndex++;
            }
        }

        return meshData;
    }

    private static float[,] SmoothHeightMap(float[,] source, float smoothingStrength, int smoothingPasses)
    {
        int width = source.GetLength(0);
        int height = source.GetLength(1);
        float[,] current = source;

        for (int pass = 0; pass < smoothingPasses; pass++)
        {
            float[,] smoothed = new float[width, height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float sum = 0f;
                    int count = 0;

                    for (int oy = -1; oy <= 1; oy++)
                    {
                        for (int ox = -1; ox <= 1; ox++)
                        {
                            int nx = Mathf.Clamp(x + ox, 0, width - 1);
                            int ny = Mathf.Clamp(y + oy, 0, height - 1);
                            sum += current[nx, ny];
                            count++;
                        }
                    }

                    float average = sum / count;
                    smoothed[x, y] = Mathf.Lerp(current[x, y], average, smoothingStrength);
                }
            }

            current = smoothed;
        }

        return current;
    }
}   

public class MeshData
{
    public Vector3[] vertices;
    public int[] triangles;
    public Vector2[] uvs;

    int triangleIndex;

    public MeshData(int meshWidth, int meshHeight)
    {
        vertices = new Vector3[meshWidth * meshHeight];
        triangles = new int[(meshWidth - 1) * (meshHeight - 1) * 6];
        uvs = new Vector2[meshWidth * meshHeight];
    }

    public void AddTriangle(int a, int b, int c)
    {
        triangles[triangleIndex] = a;
        triangles[triangleIndex + 1] = b;
        triangles[triangleIndex + 2] = c;
        triangleIndex += 3;
    }

    public Mesh CreateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();

        return mesh;
    }
}