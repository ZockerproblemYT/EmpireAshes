using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class Health : MonoBehaviour
{
    [Header("Events")]
    public UnityEvent<float, float> OnHealthChanged = new UnityEvent<float, float>(); // (current, max)
    public UnityEvent<Unit> OnDamaged = new UnityEvent<Unit>();
    public UnityEvent OnDeath = new UnityEvent();

    [Header("Status")]
    [SerializeField] private float maxHealth = 100f;

    private float currentHealth;

    public float Max => maxHealth;
    public float Current => currentHealth;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    private void Start()
    {
        Debug.Log($"[Health] ⛑ Initial: {currentHealth}/{maxHealth}", this);
        OnHealthChanged.Invoke(currentHealth, maxHealth);
    }

    public void SetMaxHealth(float max)
    {
        maxHealth = max;
        currentHealth = max;
        Debug.Log($"[Health] 🔄 MaxHealth gesetzt: {maxHealth}", this);
        OnHealthChanged.Invoke(currentHealth, maxHealth);
    }

    public void TakeDamage(float amount, Unit attacker = null)
    {
        if (currentHealth <= 0f) return;

        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        Debug.Log($"[Health] 💥 Schaden: {amount} -> {currentHealth}/{maxHealth}", this);
        OnHealthChanged.Invoke(currentHealth, maxHealth);
        OnDamaged.Invoke(attacker);

        if (currentHealth <= 0)
            Die();
    }

    public void TakeDamage(float amount, DamageType damageType, ArmorType targetArmor, Unit attacker = null)
    {
        float multiplier = CalculateDamageMultiplier(damageType, targetArmor);
        float finalDamage = amount * multiplier;
        Debug.Log($"[Health] ⚔️ Typ-Schaden: {amount} * {multiplier} = {finalDamage} ({damageType} → {targetArmor})", this);
        TakeDamage(finalDamage, attacker);
    }

    public void Heal(float amount)
    {
        if (currentHealth <= 0f) return;

        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        Debug.Log($"[Health] 💚 Heilung: {amount} -> {currentHealth}/{maxHealth}", this);
        OnHealthChanged.Invoke(currentHealth, maxHealth);
    }

    private void Die()
    {
        Debug.Log($"[Health] ☠️ Tod von: {gameObject.name}", this);
        OnDeath.Invoke();
        Destroy(gameObject);
    }

    private float CalculateDamageMultiplier(DamageType dmg, ArmorType armor)
    {
        return (dmg, armor) switch
        {
            (DamageType.Normal, ArmorType.Light) => 1.0f,
            (DamageType.Normal, ArmorType.Medium) => 1.0f,
            (DamageType.Normal, ArmorType.Heavy) => 0.75f,

            (DamageType.Explosive, ArmorType.Light) => 0.8f,
            (DamageType.Explosive, ArmorType.Medium) => 1.0f,
            (DamageType.Explosive, ArmorType.Heavy) => 1.5f,

            (DamageType.ArmorPiercing, ArmorType.Light) => 1.2f,
            (DamageType.ArmorPiercing, ArmorType.Medium) => 1.2f,
            (DamageType.ArmorPiercing, ArmorType.Heavy) => 1.5f,

            _ => 1.0f
        };
    }
}