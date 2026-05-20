using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// UI инвентаря как в Minecraft.
/// 
/// Иерархия Canvas (создай в Unity):
/// Canvas
///   ├── Hotbar          (HorizontalLayoutGroup)
///   │     └── SlotPrefab x9
///   └── InventoryPanel  (GridLayoutGroup, изначально выключен)
///         ├── InventoryGrid   (GridLayoutGroup, 9x3)
///         │     └── SlotPrefab x27
///         └── HotbarInPanel   (HorizontalLayoutGroup)
///               └── SlotPrefab x9  (те же слоты 0-8)
/// </summary>
public class InventoryUI : MonoBehaviour
{
    [Header("Ссылки на InventorySystem")]
    [SerializeField] private InventorySystem inventory;

    [Header("Хотбар (нижний)")]
    [SerializeField] private Transform hotbarRoot;          // родитель 9 слотов хотбара
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private Sprite selectionSprite;        // рамка выбранного слота

    [Header("Полный инвентарь")]
    [SerializeField] private GameObject inventoryPanel;     // весь панель (вкл/выкл по E)
    [SerializeField] private Transform  inventoryGrid;      // 27 слотов инвентаря
    [SerializeField] private Transform  hotbarInPanel;      // хотбар внутри панели инвентаря

    [Header("Иконка блока из атласа (необязательно)")]
    [SerializeField] private TextureAtlas textureAtlas;

    // Внутренние слоты UI
    private List<SlotUI> hotbarSlots    = new List<SlotUI>();
    private List<SlotUI> inventorySlots = new List<SlotUI>();

    private bool isOpen = false;

    // Drag & drop
    private SlotUI  dragSource;
    private Image   dragIcon;

    private void Start()
    {
        if (inventory == null)
            inventory = FindObjectOfType<InventorySystem>();

        BuildHotbar();
        BuildInventoryPanel();

        inventory.OnInventoryChanged         += RefreshAll;
        inventory.OnHotbarSelectionChanged   += UpdateHotbarSelection;

        inventoryPanel.SetActive(false);
        RefreshAll();
        UpdateHotbarSelection(inventory.SelectedHotbarSlot);
    }

    private void Update()
    {
        // Открыть / закрыть инвентарь
        if (Input.GetKeyDown(KeyCode.E))
        {
            isOpen = !isOpen;
            inventoryPanel.SetActive(isOpen);
            Cursor.lockState = isOpen ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible   = isOpen;
        }

        // Drag icon следует за мышью
        if (dragIcon != null)
        {
            dragIcon.transform.position = Input.mousePosition;
        }
    }

    // ── Создание UI ──────────────────────────────────────────────────────────

    private void BuildHotbar()
    {
        hotbarSlots.Clear();
        foreach (Transform child in hotbarRoot) Destroy(child.gameObject);

        for (int i = 0; i < InventorySystem.HotbarSize; i++)
        {
            int slotIndex = i;
            var go  = Instantiate(slotPrefab, hotbarRoot);
            var sui = go.GetComponent<SlotUI>() ?? go.AddComponent<SlotUI>();
            sui.Setup(slotIndex, this);
            hotbarSlots.Add(sui);
        }
    }

    private void BuildInventoryPanel()
    {
        inventorySlots.Clear();

        // 27 слотов инвентаря (индексы 9-35)
        foreach (Transform child in inventoryGrid) Destroy(child.gameObject);
        for (int i = 0; i < InventorySystem.InventorySize; i++)
        {
            int slotIndex = InventorySystem.HotbarSize + i;
            var go  = Instantiate(slotPrefab, inventoryGrid);
            var sui = go.GetComponent<SlotUI>() ?? go.AddComponent<SlotUI>();
            sui.Setup(slotIndex, this);
            inventorySlots.Add(sui);
        }

        // Хотбар внутри панели (те же слоты 0-8, дублируем визуально)
        if (hotbarInPanel != null)
        {
            foreach (Transform child in hotbarInPanel) Destroy(child.gameObject);
            for (int i = 0; i < InventorySystem.HotbarSize; i++)
            {
                int slotIndex = i;
                var go  = Instantiate(slotPrefab, hotbarInPanel);
                var sui = go.GetComponent<SlotUI>() ?? go.AddComponent<SlotUI>();
                sui.Setup(slotIndex, this);
                hotbarSlots.Add(sui); // добавляем в тот же список чтобы обновлялись
            }
        }
    }

    // ── Обновление ───────────────────────────────────────────────────────────

    public void RefreshAll()
    {
        foreach (var s in hotbarSlots)    s.Refresh(inventory.GetSlot(s.SlotIndex));
        foreach (var s in inventorySlots) s.Refresh(inventory.GetSlot(s.SlotIndex));
    }

    private void UpdateHotbarSelection(int selected)
    {
        for (int i = 0; i < InventorySystem.HotbarSize; i++)
        {
            bool active = i == selected;
            // hotbarSlots может содержать дубли из панели
            if (i < hotbarSlots.Count)
                hotbarSlots[i].SetSelected(active, selectionSprite);
        }
    }

    // ── Drag & drop ──────────────────────────────────────────────────────────

    public void BeginDrag(SlotUI source)
    {
        if (inventory.GetSlot(source.SlotIndex).IsEmpty) return;
        dragSource = source;

        // Создаём плавающую иконку
        var go = new GameObject("DragIcon");
        go.transform.SetParent(transform, false);
        dragIcon = go.AddComponent<Image>();
        dragIcon.sprite       = source.GetIcon();
        dragIcon.raycastTarget = false;
        dragIcon.SetNativeSize();
    }

    public void EndDrag(SlotUI target)
    {
        if (dragSource != null && target != null && dragSource != target)
        {
            inventory.SwapSlots(dragSource.SlotIndex, target.SlotIndex);
        }

        if (dragIcon != null)
        {
            Destroy(dragIcon.gameObject);
            dragIcon = null;
        }
        dragSource = null;
    }
}

// ─── Один слот UI ─────────────────────────────────────────────────────────────
public class SlotUI : MonoBehaviour,
    UnityEngine.EventSystems.IPointerClickHandler,
    UnityEngine.EventSystems.IBeginDragHandler,
    UnityEngine.EventSystems.IEndDragHandler,
    UnityEngine.EventSystems.IDropHandler
{
    public int SlotIndex { get; private set; }

    private Image       background;
    private Image       icon;
    private TMP_Text    countText;
    private InventoryUI ui;

    private Sprite normalSprite;

    public void Setup(int index, InventoryUI inventoryUI)
    {
        SlotIndex = index;
        ui        = inventoryUI;

        background = GetComponent<Image>();
        if (background == null) background = gameObject.AddComponent<Image>();
        normalSprite = background.sprite;

        // Иконка
        var iconGO = transform.Find("Icon");
        if (iconGO == null)
        {
            iconGO = new GameObject("Icon").transform;
            iconGO.SetParent(transform, false);
            var rt = iconGO.gameObject.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(4, 4);
            rt.offsetMax = new Vector2(-4, -4);
        }
        icon = iconGO.GetComponent<Image>();
        if (icon == null) icon = iconGO.gameObject.AddComponent<Image>();
        icon.preserveAspect = true;
        icon.raycastTarget  = false;

        // Текст количества
        var textGO = transform.Find("Count");
        if (textGO == null)
        {
            textGO = new GameObject("Count").transform;
            textGO.SetParent(transform, false);
            var rt = textGO.gameObject.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
        countText = textGO.GetComponent<TMP_Text>();
        if (countText == null) countText = textGO.gameObject.AddComponent<TextMeshProUGUI>();
        countText.alignment   = TextAlignmentOptions.BottomRight;
        countText.fontSize    = 14;
        countText.raycastTarget = false;
    }

    public void Refresh(ItemStack stack)
    {
        if (stack == null || stack.IsEmpty)
        {
            icon.enabled      = false;
            countText.text    = "";
        }
        else
        {
            icon.enabled = true;
            // Иконку берём из ItemData.icon если есть, иначе пустой слот
            icon.sprite = stack.item.HasCustomIcon ? stack.item.icon : null;
            icon.enabled = icon.sprite != null;
            countText.text = stack.count > 1 ? stack.count.ToString() : "";
        }
    }

    public void SetSelected(bool selected, Sprite selectionSprite)
    {
        if (selected && selectionSprite != null)
            background.sprite = selectionSprite;
        else
            background.sprite = normalSprite;
    }

    public Sprite GetIcon() => icon.sprite;

    // ── Events ────────────────────────────────────────────────────────────────

    public void OnPointerClick(UnityEngine.EventSystems.PointerEventData e) { }

    public void OnBeginDrag(UnityEngine.EventSystems.PointerEventData e)
        => ui.BeginDrag(this);

    public void OnEndDrag(UnityEngine.EventSystems.PointerEventData e)
        => ui.EndDrag(null);

    public void OnDrop(UnityEngine.EventSystems.PointerEventData e)
        => ui.EndDrag(this);
}
