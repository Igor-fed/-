using UnityEngine;

/// <summary>
/// Ломание блоков как в Minecraft:
/// — зажимаем ЛКМ → прогресс растёт
/// — отпускаем / смотрим в другую сторону → прогресс сбрасывается
/// — блок сломан → исчезает, дроп летит на землю
/// </summary>
[RequireComponent(typeof(InventorySystem))]
public class BlockBreaker : MonoBehaviour
{
    [Header("Настройки")]
    [SerializeField] private float reach = 5f;
    [SerializeField] private LayerMask blockLayer;

    [Header("Визуал трещин (необязательно)")]
    [SerializeField] private GameObject crackOverlayPrefab;  // quad с animated crack texture
    [SerializeField] private int crackStages = 8;

    [Header("Дроп")]
    [SerializeField] private GameObject itemDropPrefab;      // prefab с Rigidbody + ItemPickup
    [SerializeField] private float dropForce = 3f;

    // Текущий блок под прицелом
    private Vector3Int  currentBlockPos;
    private BlockData   currentBlockData;
    private float       breakProgress;       // 0 → 1
    private bool        isBreaking;

    private GameObject  crackInstance;
    private World       world;
    private InventorySystem inventory;

    private void Awake()
    {
        world     = FindObjectOfType<World>();
        inventory = GetComponent<InventorySystem>();
    }

    private void Update()
    {
        RaycastBlock(out Vector3Int hitPos, out BlockData hitData, out Vector3 hitNormal);

        bool lmb = Input.GetMouseButton(0);

        // Если смотрим на новый блок — сброс
        if (hitData == null || hitPos != currentBlockPos)
        {
            ResetBreaking();
            currentBlockPos  = hitPos;
            currentBlockData = hitData;
        }

        if (lmb && hitData != null && hitData.isBreakable)
        {
            isBreaking = true;

            // Скорость ломания: инструмент ускоряет
            float speed = GetBreakSpeed(hitData);
            breakProgress += Time.deltaTime * speed;

            UpdateCrackVisual(hitPos);

            if (breakProgress >= 1f)
            {
                BreakBlock(hitPos, hitData);
                ResetBreaking();
            }
        }
        else
        {
            if (isBreaking) ResetBreaking();
        }
    }

    // ── Raycast ───────────────────────────────────────────────────────────────

    private void RaycastBlock(out Vector3Int blockPos, out BlockData blockData, out Vector3 normal)
    {
        blockPos  = Vector3Int.zero;
        blockData = null;
        normal    = Vector3.zero;

        Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f));
        if (!Physics.Raycast(ray, out RaycastHit hit, reach, blockLayer)) return;

        // Смещаемся чуть внутрь блока чтобы получить точные координаты
        Vector3 inside = hit.point - hit.normal * 0.5f;
        blockPos = new Vector3Int(
            Mathf.FloorToInt(inside.x),
            Mathf.FloorToInt(inside.y),
            Mathf.FloorToInt(inside.z)
        );
        normal    = hit.normal;
        BlockType type = world.GetBlockType(blockPos);
        blockData = world.blockRegistry.GetBlockData(type);
    }

    // ── Скорость ломания ──────────────────────────────────────────────────────

    private float GetBreakSpeed(BlockData data)
    {
        float baseSpeed = 1f / Mathf.Max(0.1f, data.breakTime);

        // Проверяем активный инструмент
        ItemStack active = inventory.ActiveItem;
        if (active == null || active.IsEmpty) return baseSpeed;

        ItemData tool = active.item;
        if (tool.itemType == ItemType.Tool && tool.toolLevel >= data.minToolLevel)
            baseSpeed *= 1.5f + tool.toolLevel * 0.5f;

        return baseSpeed;
    }

    // ── Ломание ───────────────────────────────────────────────────────────────

    private void BreakBlock(Vector3Int pos, BlockData data)
    {
        world.SetBlockType(pos, BlockType.Air);

        // Дроп предмета
        if (data.dropItem != null)
        {
            int dropCount = Random.Range(data.minDrop, data.maxDrop + 1);
            SpawnDrop(pos, data.dropItem, dropCount);
        }
    }

    private void SpawnDrop(Vector3Int blockPos, ItemData item, int count)
    {
        if (itemDropPrefab == null)
        {
            // Нет префаба — добавляем сразу в инвентарь
            inventory.AddItem(item, count);
            return;
        }

        Vector3 spawnPos = new Vector3(blockPos.x + 0.5f, blockPos.y + 0.5f, blockPos.z + 0.5f);
        GameObject drop = Instantiate(itemDropPrefab, spawnPos, Random.rotation);

        // Инициализируем компонент дропа если есть
        ItemPickup pickup = drop.GetComponent<ItemPickup>();
        if (pickup != null) pickup.Initialize(item, count);

        // Случайный импульс вверх
        Rigidbody rb = drop.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Vector3 force = new Vector3(
                Random.Range(-0.5f, 0.5f),
                dropForce,
                Random.Range(-0.5f, 0.5f)
            );
            rb.AddForce(force, ForceMode.Impulse);
        }
    }

    // ── Трещины ───────────────────────────────────────────────────────────────

    private void UpdateCrackVisual(Vector3Int pos)
    {
        if (crackOverlayPrefab == null) return;

        if (crackInstance == null)
        {
            crackInstance = Instantiate(crackOverlayPrefab);
        }

        // Позиционируем на блоке
        crackInstance.transform.position = new Vector3(pos.x + 0.5f, pos.y + 0.5f, pos.z + 0.5f);

        // Передаём стадию трещины если есть аниматор/скрипт
        CrackOverlay overlay = crackInstance.GetComponent<CrackOverlay>();
        if (overlay != null)
        {
            int stage = Mathf.FloorToInt(breakProgress * crackStages);
            overlay.SetStage(stage);
        }
    }

    private void ResetBreaking()
    {
        breakProgress    = 0f;
        isBreaking       = false;
        currentBlockData = null;

        if (crackInstance != null)
        {
            Destroy(crackInstance);
            crackInstance = null;
        }
    }

    // ── Gizmo ─────────────────────────────────────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        if (!isBreaking) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(
            new Vector3(currentBlockPos.x + 0.5f, currentBlockPos.y + 0.5f, currentBlockPos.z + 0.5f),
            Vector3.one * 1.02f
        );
    }
}

// ─── Вспомогательный компонент трещины ───────────────────────────────────────
// Повесь на prefab трещины; управляет кадром спрайта/текстуры
public class CrackOverlay : MonoBehaviour
{
    [SerializeField] private Renderer crackRenderer;
    [SerializeField] private Texture2D[] crackTextures; // 8 текстур трещин

    public void SetStage(int stage)
    {
        if (crackTextures == null || crackTextures.Length == 0) return;
        stage = Mathf.Clamp(stage, 0, crackTextures.Length - 1);
        if (crackRenderer != null)
            crackRenderer.material.mainTexture = crackTextures[stage];
    }
}

// ─── Предмет на земле ─────────────────────────────────────────────────────────
// Повесь на prefab дропа
public class ItemPickup : MonoBehaviour
{
    public ItemData item  { get; private set; }
    public int      count { get; private set; }

    [SerializeField] private float pickupRadius   = 1.2f;
    [SerializeField] private float pickupDelay    = 0.5f;  // нельзя подобрать сразу после спавна
    [SerializeField] private float despawnTime    = 300f;  // 5 минут

    private float spawnTime;

    public void Initialize(ItemData item, int count)
    {
        this.item  = item;
        this.count = count;
        spawnTime  = Time.time;
        Destroy(gameObject, despawnTime);
    }

    private void Update()
    {
        if (Time.time - spawnTime < pickupDelay) return;

        // Ищем ближайшего игрока
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) return;

        float dist = Vector3.Distance(transform.position, player.transform.position);
        if (dist > pickupRadius) return;

        InventorySystem inv = player.GetComponent<InventorySystem>();
        if (inv == null) return;

        int leftover = inv.AddItem(item, count);
        if (leftover == 0)
            Destroy(gameObject);
        else
            count = leftover; // часть не влезла, оставшееся остаётся на земле
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
    }
}
