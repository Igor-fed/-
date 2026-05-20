using UnityEngine;

public class SurfaceGenerator
{
    private int seed;

    public void Initialize(int worldSeed)
    {
        seed = worldSeed;
    }

    public void GenerateSurface(Chunk chunk, BlockType mainBlock, BlockType subBlock)
    {
        float s = (seed % 1000) * 0.01f;
        Vector3Int chunkPos = chunk.chunkPosition;

        for (int x = 0; x < Chunk.Size; x++)
        {
            int worldX = chunkPos.x * Chunk.Size + x;
            for (int z = 0; z < Chunk.Size; z++)
            {
                int worldZ = chunkPos.z * Chunk.Size + z;

                // ¬ысота рельефа 0Ц3 блока
                float noise = Mathf.PerlinNoise(worldX * 0.05f + s, worldZ * 0.05f + s);
                int height = Mathf.RoundToInt(noise * 3f); // 0Ц3

                for (int y = 0; y < Chunk.Size; y++)
                {
                    if (y == height)
                        chunk.SetBlock(x, y, z, mainBlock);      // верхний слой (трава)
                    else if (y < height)
                        chunk.SetBlock(x, y, z, subBlock);       // под травой (земл€)
                    else
                        chunk.SetBlock(x, y, z, BlockType.Air);  // выше Ч воздух
                }
            }
        }

        chunk.isGenerated = true;
    }
}