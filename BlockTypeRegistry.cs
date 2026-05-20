using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Реестр всех типов блоков. Позволяет получать BlockData по типу.
/// </summary>
[CreateAssetMenu(fileName = "BlockTypeRegistry", menuName = "Game/Block Type Registry")]
public class BlockTypeRegistry : ScriptableObject
{
    [SerializeField]
    private List<BlockData> allBlocks = new List<BlockData>();

    private Dictionary<BlockType, BlockData> blockDataMap;
    private bool isInitialized = false;

    /// <summary>
    /// Инициализировать реестр (вызывать при старте игры)
    /// </summary>
    public void Initialize()
    {
        if (isInitialized) return;

        // Принудительно создаем новый словарь
        blockDataMap = new Dictionary<BlockType, BlockData>();

        Debug.Log($"Initialize: allBlocks.Count = {allBlocks.Count}");

        foreach (BlockData block in allBlocks)
        {
            if (block == null)
            {
                Debug.LogWarning("Null block data in registry");
                continue;
            }

            Debug.Log($"Adding block: {block.blockType} -> {block.name}");

            if (!blockDataMap.ContainsKey(block.blockType))
            {
                blockDataMap.Add(block.blockType, block);
            }
            else
            {
                Debug.LogWarning($"Duplicate block type: {block.blockType} in {block.name}");
            }
        }

        isInitialized = true;
        Debug.Log($"BlockTypeRegistry initialized with {blockDataMap.Count} block types");

        // Дополнительная проверка
        if (blockDataMap.Count == 0)
        {
            Debug.LogError("No blocks were added to registry! Check All Blocks list.");
        }
    }

    /// <summary>
    /// Получить BlockData по типу блока
    /// </summary>
    public BlockData GetBlockData(BlockType type)
    {
        // Если словарь null, создаем его прямо здесь
        if (blockDataMap == null)
        {
            Debug.LogWarning("blockDataMap was null, creating emergency dictionary");
            blockDataMap = new Dictionary<BlockType, BlockData>();

            // Вручную добавляем блоки
            foreach (BlockData block in allBlocks)
            {
                if (block != null && !blockDataMap.ContainsKey(block.blockType))
                {
                    blockDataMap.Add(block.blockType, block);
                    Debug.Log($"Emergency add: {block.blockType}");
                }
            }

            isInitialized = true;
        }

        if (blockDataMap.TryGetValue(type, out BlockData data))
        {
            return data;
        }

        Debug.LogWarning($"BlockData not found for type: {type}");
        return null;
    }

    /// <summary>
    /// Получить BlockData по имени
    /// </summary>
    public BlockData GetBlockData(string blockName)
    {
        return allBlocks.Find(b => b != null && b.blockName == blockName);
    }

    /// <summary>
    /// Получить все блоки
    /// </summary>
    public List<BlockData> GetAllBlocks() => allBlocks;

    /// <summary>
    /// Проверить, существует ли блок
    /// </summary>
    public bool HasBlock(BlockType type)
    {
        if (!isInitialized) Initialize();
        return blockDataMap.ContainsKey(type);
    }
}