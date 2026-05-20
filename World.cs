using UnityEngine;
using System.Collections.Generic;

public class World : MonoBehaviour
{
    [Header("Размеры одного уровня (в чанках)")]
    public int widthInChunks = 3;   // ширина уровня
    public int depthInChunks = 3;   // глубина уровня
    public int chunksPerLevel = 2;   // высота одного уровня в чанках (2 = 32 блока)

    [Header("Уровни — заполни снизу вверх по игровой логике")]
    public List<LevelSlot> levelSlots = new List<LevelSlot>();
    // Пример: слот 0 = самый глубокий уровень, слот N = верхний

    [Header("Поверхность")]
    public int surfaceWidthInChunks = 6;
    public int surfaceDepthInChunks = 6;
    public bool generateSurface = true;
    public BlockType surfaceMainBlock = BlockType.Grass;
    public BlockType surfaceSubBlock = BlockType.Dirt;

    [Header("Ссылки")]
    public GameObject chunkPrefab;
    public GameObject levelManagerPrefab;   // префаб с компонентом LevelManager
    public BlockTypeRegistry blockRegistry;
    public TextureAtlas textureAtlas;

    [Header("Сид")]
    public int worldSeed = 0;
    public bool useRandomSeed = true;

    // Внутренние данные
    private Dictionary<Vector3Int, Chunk> chunks = new Dictionary<Vector3Int, Chunk>();
    private List<LevelManager> levelManagers = new List<LevelManager>();
    private WorldGenerator generator;
    private SurfaceGenerator surfaceGenerator;

    // Публичные свойства
    public int LevelCount => levelSlots.Count;
    public int LevelHeightBlocks => chunksPerLevel * Chunk.Size;

    void Awake()
    {
        if (useRandomSeed)
            worldSeed = Random.Range(1, 99999999);
        Random.InitState(worldSeed);
        Debug.Log($"World seed: {worldSeed}");

        if (chunkPrefab == null) { Debug.LogError("chunkPrefab not assigned!"); return; }
        if (blockRegistry == null) { Debug.LogError("blockRegistry not assigned!"); return; }

        blockRegistry.Initialize();
        if (textureAtlas != null) textureAtlas.Initialize();

        generator = new WorldGenerator();
        generator.Initialize(blockRegistry, textureAtlas, worldSeed);

        surfaceGenerator = new SurfaceGenerator();
        surfaceGenerator.Initialize(worldSeed);
    }

    void Start()
    {
        GenerateWorld();
    }

    public void GenerateWorld()
    {
        Debug.Log($"Generating world: {levelSlots.Count} levels...");

        // ?? 1. Создаём чанки и генерируем блоки ??????????????????????
        for (int slotIndex = 0; slotIndex < levelSlots.Count; slotIndex++)
        {
            // В Unity Y растёт вверх, поэтому слот 0 = самый нижний чанк
            int chunkYBase = slotIndex * chunksPerLevel;

            for (int cy = 0; cy < chunksPerLevel; cy++)
                for (int cx = 0; cx < widthInChunks; cx++)
                    for (int cz = 0; cz < depthInChunks; cz++)
                    {
                        var chunkPos = new Vector3Int(cx, chunkYBase + cy, cz);
                        CreateChunk(chunkPos);
                    }

            // Генерируем содержимое уровня
            LevelData data = levelSlots[slotIndex].levelData;
            int worldYBase = chunkYBase * Chunk.Size;

            for (int cy = 0; cy < chunksPerLevel; cy++)
                for (int cx = 0; cx < widthInChunks; cx++)
                    for (int cz = 0; cz < depthInChunks; cz++)
                    {
                        var chunkPos = new Vector3Int(cx, chunkYBase + cy, cz);
                        if (chunks.TryGetValue(chunkPos, out Chunk chunk))
                        {
                            if (data != null)
                                generator.GenerateChunk(chunk, data);
                            else
                                GenerateEmptyRoom(chunk);   // пустая комната если LevelData не задан
                        }
                    }
        }

        // ?? 2. Поверхность ????????????????????????????????????????????
        if (generateSurface)
        {
            int surfaceChunkY = levelSlots.Count * chunksPerLevel;
            for (int cx = 0; cx < surfaceWidthInChunks; cx++)
                for (int cz = 0; cz < surfaceDepthInChunks; cz++)
                {
                    var chunkPos = new Vector3Int(cx, surfaceChunkY, cz);
                    CreateChunk(chunkPos);
                    if (chunks.TryGetValue(chunkPos, out Chunk chunk))
                        surfaceGenerator.GenerateSurface(chunk, surfaceMainBlock, surfaceSubBlock);
                }
        }

        // ?? 3. Строим меши ????????????????????????????????????????????
        foreach (var chunk in chunks.Values)
            chunk.RebuildMesh();

        // ?? 4. Создаём LevelManager'ы ?????????????????????????????????
        // Игровая логика: уровень 0 = верхний (первый куда попадает игрок)
        // Поэтому назначаем менеджеры сверху вниз
        CreateLevelManagers();

        Debug.Log("World generation complete!");
    }

    // ?? LevelManager'ы ????????????????????????????????????????????????

    private void CreateLevelManagers()
    {
        levelManagers.Clear();
        int totalLevels = levelSlots.Count;

        for (int gameLevel = 0; gameLevel < totalLevels; gameLevel++)
        {
            // gameLevel 0 = верхний уровень (самый высокий чанк)
            // gameLevel N = нижний уровень
            int slotIndex = totalLevels - 1 - gameLevel;   // переворот
            int chunkYBase = slotIndex * chunksPerLevel;

            // Собираем чанки этого уровня
            var levelChunks = new List<Chunk>();
            for (int cy = 0; cy < chunksPerLevel; cy++)
                for (int cx = 0; cx < widthInChunks; cx++)
                    for (int cz = 0; cz < depthInChunks; cz++)
                    {
                        var pos = new Vector3Int(cx, chunkYBase + cy, cz);
                        if (chunks.TryGetValue(pos, out Chunk chunk))
                            levelChunks.Add(chunk);
                    }

            // Позиция менеджера — центр уровня
            float worldY = chunkYBase * Chunk.Size + LevelHeightBlocks * 0.5f;
            float centerX = widthInChunks * Chunk.Size * 0.5f;
            float centerZ = depthInChunks * Chunk.Size * 0.5f;
            var managerPos = new Vector3(centerX, worldY, centerZ);

            LevelManager mgr;
            if (levelManagerPrefab != null)
            {
                var go = Instantiate(levelManagerPrefab, managerPos, Quaternion.identity, transform);
                go.name = $"LevelManager_{gameLevel}";
                mgr = go.GetComponent<LevelManager>();
            }
            else
            {
                var go = new GameObject($"LevelManager_{gameLevel}");
                go.transform.SetParent(transform);
                go.transform.position = managerPos;
                mgr = go.AddComponent<LevelManager>();
            }

            mgr.Initialize(gameLevel, slotIndex, levelSlots[slotIndex], levelChunks, this);
            levelManagers.Add(mgr);

            Debug.Log($"LevelManager_{gameLevel} ? слот {slotIndex} " +
                      $"({levelSlots[slotIndex].levelData?.levelName ?? "пустая комната"})");
        }
    }

    // ?? Вспомогательные ??????????????????????????????????????????????

    private void GenerateEmptyRoom(Chunk chunk)
    {
        // Только пол, потолок и стены — внутри воздух
        for (int x = 0; x < Chunk.Size; x++)
            for (int y = 0; y < Chunk.Size; y++)
                for (int z = 0; z < Chunk.Size; z++)
                {
                    bool isWall = x == 0 || x == Chunk.Size - 1 ||
                                  z == 0 || z == Chunk.Size - 1;
                    bool isFloor = y == 0;
                    bool isCeiling = y == Chunk.Size - 1;

                    chunk.SetBlock(x, y, z,
                        (isWall || isFloor || isCeiling) ? BlockType.Stone : BlockType.Air);
                }
        chunk.isGenerated = true;
    }

    private void CreateChunk(Vector3Int chunkPos)
    {
        if (chunks.ContainsKey(chunkPos)) return;

        var go = Instantiate(chunkPrefab, transform);
        var chunk = go.GetComponent<Chunk>();
        if (chunk == null) { Debug.LogError("No Chunk component on prefab!"); return; }

        chunk.Initialize(chunkPos, this);
        chunks[chunkPos] = chunk;
    }

    // ?? Публичное API ????????????????????????????????????????????????

    public LevelManager GetLevelManager(int gameLevel)
    {
        if (gameLevel < 0 || gameLevel >= levelManagers.Count) return null;
        return levelManagers[gameLevel];
    }

    public Chunk GetChunk(Vector3Int chunkPos)
    {
        chunks.TryGetValue(chunkPos, out Chunk c);
        return c;
    }

    public BlockType GetBlockType(Vector3Int worldPos)
    {
        var chunkPos = GetChunkPosition(worldPos);
        var localPos = GetLocalPosition(worldPos);
        if (chunks.TryGetValue(chunkPos, out Chunk chunk))
            return chunk.GetBlock(localPos.x, localPos.y, localPos.z);
        return BlockType.Air;
    }

    public void SetBlockType(Vector3Int worldPos, BlockType type)
    {
        var chunkPos = GetChunkPosition(worldPos);
        var localPos = GetLocalPosition(worldPos);
        if (chunks.TryGetValue(chunkPos, out Chunk chunk))
        {
            chunk.SetBlock(localPos.x, localPos.y, localPos.z, type);
            chunk.RebuildMesh();
            RebuildNeighbors(chunkPos, localPos);
        }
    }

    public Vector3Int GetChunkPosition(Vector3Int worldPos) => new Vector3Int(
        Mathf.FloorToInt(worldPos.x / (float)Chunk.Size),
        Mathf.FloorToInt(worldPos.y / (float)Chunk.Size),
        Mathf.FloorToInt(worldPos.z / (float)Chunk.Size));

    public Vector3Int GetLocalPosition(Vector3Int worldPos) => new Vector3Int(
        worldPos.x % Chunk.Size,
        worldPos.y % Chunk.Size,
        worldPos.z % Chunk.Size);

    private void RebuildNeighbors(Vector3Int chunkPos, Vector3Int localPos)
    {
        if (localPos.x == 0) TryRebuild(chunkPos + Vector3Int.left);
        if (localPos.x == Chunk.Size - 1) TryRebuild(chunkPos + Vector3Int.right);
        if (localPos.y == 0) TryRebuild(chunkPos + Vector3Int.down);
        if (localPos.y == Chunk.Size - 1) TryRebuild(chunkPos + Vector3Int.up);
        if (localPos.z == 0) TryRebuild(chunkPos + Vector3Int.back);
        if (localPos.z == Chunk.Size - 1) TryRebuild(chunkPos + Vector3Int.forward);
    }

    private void TryRebuild(Vector3Int pos)
    {
        if (chunks.TryGetValue(pos, out Chunk c)) c.RebuildMesh();
    }
}