using UnityEngine;
using UnityEngine.LightTransport;

/// <summary>
/// Компонент блока в мире. Прикрепляется к GameObject блока.
/// </summary>
public class Block : MonoBehaviour
{
    [Header("Ссылки")]
    public BlockData data;                 // Ссылка на ScriptableObject с данными
    public Vector3Int position;            // Позиция в сетке мира

    [Header("Состояние")]
    public float currentHealth;            // Текущая прочность блока
    public bool isBeingMined = false;      // Копается ли сейчас

    private MeshRenderer meshRenderer;
    private MaterialPropertyBlock propertyBlock;

    void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        propertyBlock = new MaterialPropertyBlock();
    }

    void Start()
    {
        // Инициализируем здоровье блока (время на добычу)
        currentHealth = data.breakTime;

        // Применяем визуальные эффекты
        UpdateVisuals();
    }

    /// <summary>
    /// Обновляет визуал блока (материал, свечение)
    /// </summary>
    public void UpdateVisuals()
    {
        if (meshRenderer == null) return;

        // Применяем материал
        //if (data.blockMaterial != null)
        //{
        //    meshRenderer.material = data.blockMaterial;
        //}

        // Применяем свечение (для руд, магических блоков)
        if (data.emissionIntensity > 0)
        {
            meshRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor("_EmissionColor", data.emissionColor * data.emissionIntensity);
            meshRenderer.SetPropertyBlock(propertyBlock);
        }
    }

    /// <summary>
    /// Нанести урон блоку (копание)
    /// </summary>
    /// <param name="damage">Урон от инструмента</param>
    /// <returns>True если блок сломан</returns>
    public bool TakeDamage(float damage)
    {
        if (!data.isBreakable) return false;

        currentHealth -= damage;

        // Визуальный эффект (мигание)
        StartCoroutine(FlashRed());

        if (currentHealth <= 0)
        {
            //Break();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Сломать блок
    /// </summary>
    //private void Break()
    //{
    //    // Спавним дроп
    //    DropItem();

    //    // Уведомляем мир, что блок сломан
    //    World world = FindObjectOfType<World>();
    //    if (world != null)
    //    {
    //        world.OnBlockBroken(position);
    //    }

    //    // Уничтожаем объект
    //    Destroy(gameObject);
    //}

    /// <summary>
    /// Выпадение предметов
    /// </summary>
    private void DropItem()
    {
        if (data.dropItem == null) return;

        int amount = 1;

        // Создаем дроп в мире (можно сделать через Object Pool)
        GameObject dropObject = new GameObject("ItemDrop");
        dropObject.transform.position = transform.position + Vector3.up * 0.5f;

        ItemDrop drop = dropObject.AddComponent<ItemDrop>();
        drop.Initialize(data.dropItem, amount);
    }

    private System.Collections.IEnumerator FlashRed()
    {
        if (meshRenderer == null) yield break;

        Color originalColor = Color.white;
        meshRenderer.material.color = Color.red;
        yield return new WaitForSeconds(0.05f);
        meshRenderer.material.color = originalColor;
    }

    /// <summary>
    /// Инициализация блока при создании
    /// </summary>
    public void Initialize(BlockData blockData, Vector3Int pos)
    {
        data = blockData;
        position = pos;
        transform.position = pos;

        currentHealth = data.breakTime;
        UpdateVisuals();
    }
}