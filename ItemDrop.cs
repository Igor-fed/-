using UnityEngine;

/// <summary>
/// Выпавший предмет на земле
/// </summary>
public class ItemDrop : MonoBehaviour
{
    public ItemData item;
    public int amount;

    private float pickupRadius = 1.5f;
    private float lifetime = 60f;           // Живет 60 секунд
    private float timer;

    public void Initialize(ItemData itemData, int itemAmount)
    {
        item = itemData;
        amount = itemAmount;
        timer = lifetime;

        // Визуал (простой кубик с текстурой)
        SpriteRenderer sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = itemData.icon;
        sr.sortingOrder = 5;

        // Добавляем физику (падает)
        Rigidbody rb = gameObject.AddComponent<Rigidbody>();
        rb.AddForce(Vector3.up * 3f + Random.insideUnitSphere * 2f, ForceMode.Impulse);

        // Добавляем коллайдер
        SphereCollider collider = gameObject.AddComponent<SphereCollider>();
        collider.isTrigger = true;
        collider.radius = 0.5f;
    }

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0)
        {
            Destroy(gameObject);
        }

        //// Притягиваемся к игроку, если он рядом
        //PlayerController player = FindObjectOfType<PlayerController>();
        //if (player != null)
        //{
        //    float distance = Vector3.Distance(transform.position, player.transform.position);
        //    if (distance < pickupRadius)
        //    {
        //        // Двигаемся к игроку
        //        transform.position = Vector3.MoveTowards(
        //            transform.position,
        //            player.transform.position,
        //            Time.deltaTime * 5f
        //        );

        //        if (distance < 0.5f)
        //        {
        //            Pickup(player);
        //        }
        //    }
        //}
    }

    //void Pickup(PlayerController player)
    //{
    //    // Добавляем предмет в инвентарь
    //    Inventory inventory = player.GetComponent<Inventory>();
    //    if (inventory != null && inventory.AddItem(item, amount))
    //    {
    //        Destroy(gameObject);
    //    }
    //}
}