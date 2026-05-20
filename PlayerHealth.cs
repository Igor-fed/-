using UnityEngine;

/// <summary>
/// Здоровье игрока. Повесь на объект Player.
/// 20 HP = 10 сердец как в Minecraft.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [Header("Здоровье")]
    [SerializeField] private int maxHealth = 20;

    private int currentHealth;

    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;

    // Подписывайся на эти события из HUDController и других скриптов
    public event System.Action<int, int> OnHealthChanged;  // (current, max)
    public event System.Action OnDeath;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    private void onEnable()
    {
        // Сообщаем HUD начальное состояние
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    /// <summary>Нанести урон. 2 = одно сердце.</summary>
    public void TakeDamage(int amount)
    {
        if (currentHealth <= 0) return;

        currentHealth = Mathf.Max(0, currentHealth - amount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth == 0)
            OnDeath?.Invoke();
    }

    /// <summary>Восстановить здоровье.</summary>
    public void Heal(int amount)
    {
        if (currentHealth <= 0) return;  // мёртвый не лечится

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    /// <summary>Изменить максимальное здоровье (например бонус от брони).</summary>
    public void SetMaxHealth(int newMax, bool refillHealth = false)
    {
        maxHealth = Mathf.Max(1, newMax);
        if (refillHealth) currentHealth = maxHealth;
        currentHealth = Mathf.Min(currentHealth, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public bool IsAlive => currentHealth > 0;

    // Тест в редакторе — нажми кнопку в инспекторе
    [ContextMenu("Take 2 Damage (Test)")]
    private void TestDamage() => TakeDamage(2);

    [ContextMenu("Heal 2 HP (Test)")]
    private void TestHeal() => Heal(2);

    [ContextMenu("Kill (Test)")]
    private void TestKill() => TakeDamage(999);
}