using UnityEngine;

/// <summary>
/// Данные о предмете (ресурс, инструмент, еда и т.д.)
/// </summary>
[CreateAssetMenu(fileName = "New Item", menuName = "Game/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Основное")]
    public string itemName;
    public ItemType itemType;
    public Sprite icon;                // иконка для инвентаря (можно оставить null — тогда рендерится из текстуры блока)

    [Header("Стак и ценность")]
    public int maxStackSize = 64;
    public int sellValue = 1;

    [Header("Если это блок — что размещать")]
    public BlockData placedBlock;      // ссылка обратно на BlockData

    [Header("Крафт/плавка")]
    public ItemData smeltsInto;
    public float smeltTime = 2f;

    [Header("Бой (для инструментов/оружия)")]
    public int damage = 0;
    public int toolLevel = 0;          // уровень инструмента для добычи
    public float attackSpeed = 1f;

    [Header("Еда")]
    public int hungerRestore = 0;

    public bool CanBePlaced => placedBlock != null;
    public bool CanBeSmelted => smeltsInto != null;
    // Если иконка не задана — UI должен рендерить тайл из placedBlock.tileTop
    public bool HasCustomIcon => icon != null;
}

public enum ItemType
{
    Resource,      // Древесина, камень, земля
    Ore,           // Руда (необработанная)
    Ingot,         // Слиток (обработанный)
    Tool,          // Инструмент (кирка, топор)
    Weapon,        // Оружие (меч, лук)
    Arrow,         // Стрелы
    Food,          // Еда
    Artifact,      // Артефакт
    Material       // Строительный материал
}