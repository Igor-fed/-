using UnityEngine;
using System.Collections.Generic;

public class LevelManager : MonoBehaviour
{
    // ?? Данные уровня ?????????????????????????????????????????????
    public int GameLevel { get; private set; }  // 0 = верхний (первый)
    public int SlotIndex { get; private set; }  // индекс в levelSlots
    public LevelSlot Slot { get; private set; }
    public LevelData Data => Slot?.levelData;
    public bool IsEmpty => Data == null;

    // ?? Чанки ?????????????????????????????????????????????????????
    private List<Chunk> ownedChunks = new List<Chunk>();
    private World world;

    // ?? Состояние ?????????????????????????????????????????????????
    public bool IsActive { get; private set; } = false;

    public void Initialize(int gameLevel, int slotIndex,
                           LevelSlot slot, List<Chunk> chunks, World worldRef)
    {
        GameLevel = gameLevel;
        SlotIndex = slotIndex;
        Slot = slot;
        ownedChunks = chunks;
        world = worldRef;

        Debug.Log($"[LevelManager] Уровень {gameLevel} инициализирован. " +
                  $"Данные: {(IsEmpty ? "пустая комната" : Data.levelName)}, " +
                  $"чанков: {ownedChunks.Count}");
    }

    // ?? API для будущих систем ????????????????????????????????????

    /// <summary>Активировать уровень (игрок вошёл)</summary>
    public void Activate()
    {
        IsActive = true;
        Debug.Log($"[LevelManager_{GameLevel}] Активирован");
        // TODO: спавн врагов, включение ловушек и т.д.
    }

    /// <summary>Деактивировать уровень (игрок ушёл)</summary>
    public void Deactivate()
    {
        IsActive = false;
        Debug.Log($"[LevelManager_{GameLevel}] Деактивирован");
        // TODO: пауза AI, отключение ловушек
    }

    /// <summary>Получить все чанки этого уровня</summary>
    public IReadOnlyList<Chunk> GetChunks() => ownedChunks;

    /// <summary>Получить Y-позицию пола уровня в мировых координатах</summary>
    public float GetFloorWorldY()
    {
        if (ownedChunks.Count == 0) return 0;
        int minChunkY = int.MaxValue;
        foreach (var c in ownedChunks)
            if (c.chunkPosition.y < minChunkY)
                minChunkY = c.chunkPosition.y;
        return minChunkY * Chunk.Size;
    }

    /// <summary>Получить Y-позицию потолка уровня в мировых координатах</summary>
    public float GetCeilingWorldY() => GetFloorWorldY() + world.LevelHeightBlocks;
}