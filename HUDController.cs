using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;


// ─── HUD Controller ───────────────────────────────────────────────────────────
// Повесь на Canvas или отдельный GameObject
public class HUDController : MonoBehaviour
{
    [Header("Ссылки на игрока")]
    [SerializeField] private PlayerHealth playerHealth;

    // ── Сердца ──
    [Header("HP — сердца (как в Minecraft)")]
    [SerializeField] private Transform  heartsRoot;     // родитель иконок сердец (HorizontalLayoutGroup)
    [SerializeField] private GameObject heartPrefab;    // префаб одного сердца (Image)
    [SerializeField] private Sprite     heartFull;      // полное сердце
    [SerializeField] private Sprite     heartHalf;      // половина сердца
    [SerializeField] private Sprite     heartEmpty;     // пустое сердце

    // ── Альтернатива: полоска HP вместо сердец ──
    [Header("HP — полоска (альтернатива сердцам)")]
    [SerializeField] private Slider     healthSlider;   // если используешь Slider
    [SerializeField] private Image      healthFill;     // Image fill у Slider
    [SerializeField] private TMP_Text   healthText;     // "15 / 20"
    [SerializeField] private Gradient   healthGradient; // зелёный → жёлтый → красный

    // ── Мини-карта ──
    [Header("Мини-карта")]
    [SerializeField] private Camera     minimapCamera;  // ортографическая камера сверху
    [SerializeField] private RawImage   minimapImage;   // RawImage куда рендерится текстура
    [SerializeField] private float      minimapHeight = 50f;   // высота камеры над игроком
    [SerializeField] private float      minimapZoom   = 30f;   // orthographicSize камеры
    [SerializeField] private RectTransform playerDot;   // маленькая точка игрока на карте (необязательно)

    // ── Прицел ──
    [Header("Прицел")]
    [SerializeField] private GameObject crosshair;      // Image с крестиком

    // Внутренние
    private Image[] heartImages;
    private Transform playerTransform;
    private bool useHearts; // true = сердца, false = слайдер

    // ─────────────────────────────────────────────────────────────────────────

    private void Start()
    {
        // Находим игрока если не назначен
        if (playerHealth == null)
            playerHealth = FindObjectOfType<PlayerHealth>();

        if (playerHealth != null)
        {
            playerTransform = playerHealth.transform;
            playerHealth.OnHealthChanged += OnHealthChanged;

            // Определяем режим отображения HP
            useHearts = (heartsRoot != null && heartPrefab != null && heartFull != null);

            if (useHearts)
                BuildHearts(playerHealth.MaxHealth);
            else
                SetupSlider(playerHealth.MaxHealth);

            // Начальное состояние
            OnHealthChanged(playerHealth.CurrentHealth, playerHealth.MaxHealth);
        }

        // Мини-карта
        if (minimapCamera != null)
        {
            minimapCamera.orthographic     = true;
            minimapCamera.orthographicSize = minimapZoom;
        }

        // Прицел всегда виден
        if (crosshair != null)
            crosshair.SetActive(true);
    }

    private void LateUpdate()
    {
        UpdateMinimap();
    }

    // ── Сердца ───────────────────────────────────────────────────────────────

    private void BuildHearts(int maxHealth)
    {
        // Очищаем старые
        foreach (Transform child in heartsRoot)
            Destroy(child.gameObject);

        int heartCount = Mathf.CeilToInt(maxHealth / 2f); // 20 HP = 10 сердец
        heartImages = new Image[heartCount];

        for (int i = 0; i < heartCount; i++)
        {
            var go  = Instantiate(heartPrefab, heartsRoot);
            heartImages[i] = go.GetComponent<Image>();
        }
    }

    private void RefreshHearts(int current, int max)
    {
        if (heartImages == null) return;

        for (int i = 0; i < heartImages.Length; i++)
        {
            int heartHP = i * 2; // каждое сердце = 2 HP

            if (current >= heartHP + 2)
                heartImages[i].sprite = heartFull;
            else if (current == heartHP + 1)
                heartImages[i].sprite = heartHalf;
            else
                heartImages[i].sprite = heartEmpty != null ? heartEmpty : heartFull;

            // Анимация урона — лёгкое мигание
            if (current < heartHP + 2 && heartImages[i].sprite != heartFull)
                StartCoroutine(FlashHeart(heartImages[i]));
        }
    }

    private IEnumerator FlashHeart(Image heart)
    {
        heart.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        heart.color = Color.white;
    }

    // ── Слайдер ───────────────────────────────────────────────────────────────

    private void SetupSlider(int maxHealth)
    {
        if (healthSlider == null) return;
        healthSlider.minValue = 0;
        healthSlider.maxValue = maxHealth;
        healthSlider.value    = maxHealth;
    }

    private void RefreshSlider(int current, int max)
    {
        if (healthSlider != null)
        {
            healthSlider.value = current;

            // Цвет полоски: зелёный при полном HP, красный при низком
            if (healthFill != null && healthGradient != null)
                healthFill.color = healthGradient.Evaluate((float)current / max);
        }

        if (healthText != null)
            healthText.text = $"{current} / {max}";
    }

    // ── Общий обработчик изменения HP ────────────────────────────────────────

    private void OnHealthChanged(int current, int max)
    {
        if (useHearts)
            RefreshHearts(current, max);
        else
            RefreshSlider(current, max);
    }

    // ── Мини-карта ────────────────────────────────────────────────────────────

    private void UpdateMinimap()
    {
        if (minimapCamera == null || playerTransform == null) return;

        // Камера следует за игроком по X и Z, Y фиксирован
        Vector3 camPos = playerTransform.position;
        camPos.y = playerTransform.position.y + minimapHeight;
        minimapCamera.transform.position = camPos;

        // Камера всегда смотрит строго вниз
        minimapCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        // Точка игрока всегда в центре мини-карты
        if (playerDot != null)
        {
            playerDot.anchoredPosition = Vector2.zero;

            // Поворачиваем точку вслед за игроком
            float yaw = playerTransform.eulerAngles.y;
            playerDot.localRotation = Quaternion.Euler(0f, 0f, -yaw);
        }
    }

    // ── Публичные методы для других скриптов ─────────────────────────────────

    /// <summary>Показать/скрыть прицел (например при открытом инвентаре)</summary>
    public void SetCrosshairVisible(bool visible)
    {
        if (crosshair != null) crosshair.SetActive(visible);
    }

    /// <summary>Изменить зум мини-карты колесом мыши</summary>
    public void SetMinimapZoom(float zoom)
    {
        minimapZoom = Mathf.Clamp(zoom, 10f, 100f);
        if (minimapCamera != null)
            minimapCamera.orthographicSize = minimapZoom;
    }

    private void OnDestroy()
    {
        if (playerHealth != null)
            playerHealth.OnHealthChanged -= OnHealthChanged;
    }
}
