using UnityEngine;
using System.Collections.Generic;

public class WorldGenerator
{
    private BlockTypeRegistry blockRegistry;
    private TextureAtlas textureAtlas;
    private int seed;
    private System.Random random;


    public void Initialize(BlockTypeRegistry registry, TextureAtlas atlas, int worldSeed)
    {
        blockRegistry = registry;
        textureAtlas = atlas;
        seed = worldSeed;
        random = new System.Random(seed);
    }

    public void GenerateChunk(Chunk chunk, LevelData currentLevel)
    {
        Vector3Int chunkPos = chunk.chunkPosition;
        int worldYBase = chunkPos.y * Chunk.Size;

        for (int x = 0; x < Chunk.Size; x++)
        {
            int worldX = chunkPos.x * Chunk.Size + x;
            for (int z = 0; z < Chunk.Size; z++)
            {
                int worldZ = chunkPos.z * Chunk.Size + z;
                for (int y = 0; y < Chunk.Size; y++)
                {
                    int worldY = worldYBase + y;
                    BlockType type = GetBlockForLevel(worldX, worldY, worldZ, currentLevel);
                    chunk.SetBlock(x, y, z, type);
                }
            }
        }
        chunk.isGenerated = true;
    }

    private BlockType GetBlockForLevel(int worldX, int worldY, int worldZ, LevelData level)
    {
        int levelHeight = 32;
        int localY = worldY % levelHeight;
        float s = (seed % 1000) * 0.01f;


        // Границы
        if (worldX == 0 || worldX == 47 || worldZ == 0 || worldZ == 47)
            return level.borderBlock;

        // Потолок
        if (localY == levelHeight - 1)
            return level.ceilingBlock;

        // Пол
        if (localY == 0)
            return level.floorBlock;

        // Пещеры
        float noiseXZ = Mathf.PerlinNoise(worldX * level.caveSize + s,
                                           worldZ * level.caveSize + s);
        float noiseXY = Mathf.PerlinNoise(worldX * level.caveSize + s + 31.7f,
                                           worldY * level.caveSize);
        float noiseYZ = Mathf.PerlinNoise(worldY * level.caveSize,
                                           worldZ * level.caveSize + s + 57.3f);

        float caveValue = noiseXZ * noiseXY * noiseYZ;

        if (caveValue < level.caveDensity * 0.5f && localY > 2 && localY < levelHeight - 3)
            return BlockType.Air;

        // Руды
        foreach (var ore in level.ores)
        {
            if (localY >= ore.minHeight && localY <= ore.maxHeight)
            {
                float oreNoise = Mathf.PerlinNoise(
                    worldX * 0.1f + (int)ore.oreType * 13.7f + s,
                    worldZ * 0.1f + (int)ore.oreType * 7.3f
                );
                if (oreNoise < ore.spawnChance)
                    return ore.oreType;
            }
        }

        return level.mainBlock;
    }
}