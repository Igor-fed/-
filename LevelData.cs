using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Настройки для генерации одного уровня
/// </summary>
[CreateAssetMenu(fileName = "New Level", menuName = "Game/Level Data")]
public class LevelData : ScriptableObject
{
    [Header("Основная информация")]
    public int levelIndex;              // Номер уровня (0 = поверхность)
    public string levelName = "Каменный уровень";

    [Header("Базовые блоки")]
    public BlockType mainBlock = BlockType.Stone;      // Основной блок уровня
    public BlockType ceilingBlock = BlockType.Stone;   // Блок потолка
    public BlockType floorBlock = BlockType.Stone;     // Блок пола

    [Header("Границы")]
    public BlockType borderBlock = BlockType.Bedrock;  // Блок границ (стены)

    [Header("Руды (шансы появления)")]
    public List<OreSpawn> ores = new List<OreSpawn>();

    [Header("Пещеры")]
    public float caveDensity = 0.3f;     // Плотность пещер (0-1)
    public float caveSize = 0.08f;       // Размер пещер

    [Header("Биом")]
    public LevelBiome biome = LevelBiome.Rocky;

    [Header("Враги")]
    public List<EnemySpawn> enemies = new List<EnemySpawn>();
}

/// <summary>
/// Настройка спавна руды
/// </summary>
[System.Serializable]
public class OreSpawn
{
    public BlockType oreType;            // Тип руды
    public float spawnChance;            // Шанс появления (0-1)
    public int veinSize;                 // Размер жилы
    public int minHeight;                // Минимальная высота внутри уровня
    public int maxHeight;                // Максимальная высота внутри уровня
}

/// <summary>
/// Настройка спавна врагов
/// </summary>
[System.Serializable]
public class EnemySpawn
{
    public string enemyName;
    public float spawnChance;
    public int minDepth;
    public int maxDepth;
}

public enum LevelBiome
{
    Rocky,      // Каменный — обычный
    Mushroom,   // Грибной — грибы, свет
    Crystal,    // Кристальный — кристаллы, магия
    Molten,     // Лавовый — лава, огонь
    Ancient,    // Древний — руины, артефакты
    Void        // Пустота — тьма, босс
}