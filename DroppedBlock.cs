using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(MeshRenderer))]
public class DroppedBlock : MonoBehaviour
{
    public ItemData itemData;
    public int amount = 1;

    private Transform player;
    private Rigidbody rb;
    private float pickupDelay = 1f;
    private const float PickupRadius = 1.5f;
    private const float AttractRadius = 4f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        player = GameObject.FindWithTag("Player")?.transform;

        // Разлетается при спавне
        rb.AddForce(new Vector3(
            Random.Range(-2f, 2f),
            Random.Range(4f, 6f),
            Random.Range(-2f, 2f)
        ), ForceMode.Impulse);

        rb.angularVelocity = Random.insideUnitSphere * 5f;
    }

    void Update()
    {
        pickupDelay -= Time.deltaTime;
        if (pickupDelay > 0 || player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);

        if (dist < AttractRadius)
        {
            // Притягивается — чем ближе тем быстрее
            Vector3 dir = (player.position - transform.position).normalized;
            rb.linearVelocity = dir * Mathf.Lerp(15f, 3f, dist / AttractRadius);
        }

        if (dist < PickupRadius)
            PickUp();
    }

    private void PickUp()
    {
        // Замени на свой инвентарь:
        // Inventory.Instance.AddItem(itemData, amount);
        Debug.Log($"Picked up: {itemData?.itemName} x{amount}");
        Destroy(gameObject);
    }

    // ?? Статический спавн ????????????????????????????????????????
    public static void Spawn(Vector3 worldPos, ItemData item, int amount,
                             Material atlasMaterial, Texture2D atlas, int tilesPerRow)
    {
        // Маленький куб 0.25 юнита
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.transform.position = worldPos + Vector3.up * 0.5f;
        go.transform.localScale = Vector3.one * 0.25f;
        go.name = $"Drop_{item.itemName}";

        // Физика
        var rb = go.AddComponent<Rigidbody>();
        rb.mass = 0.5f;

        // Скрипт
        var db = go.AddComponent<DroppedBlock>();
        db.itemData = item;
        db.amount = amount;

        // Текстура из атласа
        if (item.placedBlock != null && atlas != null)
        {
            var mat = new Material(atlasMaterial);
            mat.mainTexture = atlas;

            // UV вручную через MaterialPropertyBlock не работает на кубе —
            // поэтому вырезаем кусок атласа в отдельную текстуру
            Texture2D tileTexture = ExtractTile(atlas, item.placedBlock.tileTop, tilesPerRow);
            mat.mainTexture = tileTexture;
            go.GetComponent<MeshRenderer>().material = mat;
        }
    }

    // Вырезает один тайл из атласа в отдельную Texture2D
    private static Texture2D ExtractTile(Texture2D atlas, Vector2Int tile, int tilesPerRow)
    {
        int tileSize = atlas.width / tilesPerRow;
        int px = tile.x * tileSize;
        int py = tile.y * tileSize;

        Color[] pixels = atlas.GetPixels(px, py, tileSize, tileSize);
        Texture2D result = new Texture2D(tileSize, tileSize, TextureFormat.RGBA32, false);
        result.filterMode = FilterMode.Point;  // пиксельный стиль
        result.SetPixels(pixels);
        result.Apply();
        return result;
    }
}