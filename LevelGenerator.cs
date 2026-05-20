using UnityEngine;

/// <summary>
/// Генератор одного уровня
/// </summary>
public class LevelGenerator
{
    private int seed;
    private System.Random random;

    public void Initialize(int worldSeed)
    {
        seed = worldSeed;
        random = new System.Random(seed);
    }

    /// <summary>
    /// Генерация уровня
    /// </summary>
    /// <param name="chunk">Чанк для генерации</param>
    /// <param name="level">Настройки уровня</param>
    /// <param name="yOffset">Смещение по Y (глубина)</param>
    public void GenerateLevel(Chunk chunk, LevelData level, int yOffset)
    {
        Vector3Int chunkPos = chunk.chunkPosition;

        for (int x = 0; x < Chunk.Size; x++)
        {
            for (int z = 0; z < Chunk.Size; z++)
            {
                int worldX = chunkPos.x * Chunk.Size + x;
                int worldZ = chunkPos.z * Chunk.Size + z;

                for (int y = 0; y < Chunk.Size; y++)
                {
                    int worldY = yOffset + y;

                    BlockType type = DetermineBlockType(worldX, worldY, worldZ, level);
                    chunk.SetBlock(x, y, z, type);
                }
            }
        }
    }

    private BlockType DetermineBlockType(int x, int y, int z, LevelData level)
    {
        int levelHeight = 32;  // Высота уровня в блоках
        int localY = y % levelHeight;  // Y внутри уровня (0-31)

        // === ГРАНИЦЫ УРОВНЯ ===
        // Стены по краям (x=0, x=47, z=0, z=47)
        if (x == 0 || x == 47 || z == 0 || z == 47)
        {
            return level.borderBlock;
        }

        // Потолок (верх уровня)
        if (localY == levelHeight - 1)
        {
            return level.ceilingBlock;
        }

        // Пол (низ уровня)
        if (localY == 0)
        {
            return level.floorBlock;
        }

        // === ГЕНЕРАЦИЯ ПЕЩЕР ===
        float caveNoise = GetCaveNoise(x, y, z, level);
        if (caveNoise > level.caveDensity && localY > 2 && localY < levelHeight - 3)
        {
            return BlockType.Air;
        }

        // === ГЕНЕРАЦИЯ РУД ===
        foreach (var ore in level.ores)
        {
            if (localY >= ore.minHeight && localY <= ore.maxHeight)
            {
                float oreNoise = GetOreNoise(x, y, z, ore.oreType);
                if (oreNoise < ore.spawnChance)
                {
                    // Проверяем, не руда ли уже
                    if (GetBlockTypeFromWorld(x, y - 1, z) != BlockType.Air)
                    {
                        return ore.oreType;
                    }
                }
            }
        }

        // === ОСНОВНОЙ БЛОК ===
        return level.mainBlock;
    }

    private float GetCaveNoise(int x, int y, int z, LevelData level)
    {
        // 3D шум для пещер
        float noise1 = Mathf.PerlinNoise(x * 0.05f + seed, z * 0.05f + seed);
        float noise2 = Mathf.PerlinNoise(y * 0.05f, x * 0.05f);
        float noise3 = Mathf.PerlinNoise(z * 0.05f, y * 0.05f);

        return (noise1 + noise2 + noise3) / 3f;
    }

    private float GetOreNoise(int x, int y, int z, BlockType oreType)
    {
        // Разные оффсеты для разных руд
        int offset = (int)oreType * 100;
        return Mathf.PerlinNoise(x * 0.1f + offset + seed, z * 0.1f + offset + seed);
    }

    private BlockType GetBlockTypeFromWorld(int x, int y, int z)
    {
        // Вспомогательный метод (упрощенно)
        return BlockType.Stone;
    }
}