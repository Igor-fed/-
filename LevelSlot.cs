// LevelSlot.cs
using UnityEngine;

[System.Serializable]
public class LevelSlot
{
    public string slotName = "Уровень";   // имя для инспектора
    public LevelData levelData;            // null = пустая комната
}