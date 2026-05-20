using UnityEngine;
using System.Collections.Generic;
using System;

// ─── Один стак предметов ──────────────────────────────────────────────────────
[Serializable]
public class ItemStack
{
    public ItemData item;
    public int count;

    public ItemStack(ItemData item, int count)
    {
        this.item = item;
        this.count = count;
    }

    public bool IsEmpty => item == null || count <= 0;

    public int MaxStack => item != null ? item.maxStackSize : 64;

    // Сколько можно добавить в этот стак
    public int CanAdd(ItemData other)
    {
        if (item == null) return other.maxStackSize;
        if (item != other) return 0;
        return Mathf.Max(0, MaxStack - count);
    }

    public void Clear() { item = null; count = 0; }
}

// ─── Инвентарь ────────────────────────────────────────────────────────────────
public class InventorySystem : MonoBehaviour
{
    public const int HotbarSize   = 9;
    public const int InventoryRows = 3;
    public const int InventoryCols = 9;
    public const int InventorySize = InventoryRows * InventoryCols; // 27
    public const int TotalSize     = HotbarSize + InventorySize;    // 36

    // Слоты: 0-8 = хотбар, 9-35 = инвентарь
    private ItemStack[] slots = new ItemStack[TotalSize];

    public int SelectedHotbarSlot { get; private set; } = 0;
    public ItemStack ActiveItem => slots[SelectedHotbarSlot];

    public event Action OnInventoryChanged;
    public event Action<int> OnHotbarSelectionChanged;

    private void Awake()
    {
        for (int i = 0; i < TotalSize; i++)
            slots[i] = new ItemStack(null, 0);
    }

    private void Update()
    {
        // Выбор слота хотбара колесом мыши
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll > 0f)  SelectHotbarSlot((SelectedHotbarSlot - 1 + HotbarSize) % HotbarSize);
        if (scroll < 0f)  SelectHotbarSlot((SelectedHotbarSlot + 1) % HotbarSize);

        // Цифровые клавиши 1–9
        for (int i = 0; i < HotbarSize; i++)
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                SelectHotbarSlot(i);
    }

    // ── Публичное API ─────────────────────────────────────────────────────────

    public void SelectHotbarSlot(int index)
    {
        if (index < 0 || index >= HotbarSize) return;
        SelectedHotbarSlot = index;
        OnHotbarSelectionChanged?.Invoke(index);
    }

    /// <summary>Добавить предмет. Возвращает остаток который не влез.</summary>
    public int AddItem(ItemData item, int count = 1)
    {
        // Сначала догружаем существующие стаки
        for (int i = 0; i < TotalSize && count > 0; i++)
        {
            int canAdd = slots[i].CanAdd(item);
            if (canAdd > 0)
            {
                int add = Mathf.Min(canAdd, count);
                slots[i].item   = item;
                slots[i].count += add;
                count -= add;
            }
        }

        // Потом ищем пустые слоты
        for (int i = 0; i < TotalSize && count > 0; i++)
        {
            if (slots[i].IsEmpty)
            {
                int add = Mathf.Min(item.maxStackSize, count);
                slots[i].item  = item;
                slots[i].count = add;
                count -= add;
            }
        }

        OnInventoryChanged?.Invoke();
        return count; // остаток
    }

    /// <summary>Убрать N предметов из активного слота хотбара.</summary>
    public void ConsumeActive(int count = 1)
    {
        var stack = slots[SelectedHotbarSlot];
        stack.count -= count;
        if (stack.count <= 0) stack.Clear();
        OnInventoryChanged?.Invoke();
    }

    public ItemStack GetSlot(int index) => slots[index];

    /// <summary>Обменять содержимое двух слотов (drag & drop).</summary>
    public void SwapSlots(int a, int b)
    {
        var tmp = new ItemStack(slots[a].item, slots[a].count);
        slots[a].item  = slots[b].item;
        slots[a].count = slots[b].count;
        slots[b].item  = tmp.item;
        slots[b].count = tmp.count;
        OnInventoryChanged?.Invoke();
    }

    public bool IsFull()
    {
        for (int i = 0; i < TotalSize; i++)
            if (slots[i].IsEmpty) return false;
        return true;
    }
}
