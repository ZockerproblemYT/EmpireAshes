using UnityEngine;

public class Building : MonoBehaviour
{
    [Header("Allgemeine Eigenschaften")]
    public string buildingName;
    public int costMetal;
    public int costOil;
    public int costPopulation;

    [Header("Bau")]
    public GameObject buildingUnderConstructionVisual;
    public float buildTime = 10f;
    public bool IsUnderConstruction { get; protected set; } = false;

    [Header("Lebenspunkte")]
    [SerializeField] private int maxHealth = 100;
    private int currentHealth = 1;

    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;

    [Header("UI")]
    [SerializeField] private GameObject uiPrefab;
    private BuildingUIController uiInstance;

    protected Faction owner;
    private bool isDestroyed = false;
    private bool isSelected = false;
    private bool initialized = false;

    private bool isCompleted = false;
    public bool IsCompleted => isCompleted;

    public bool IsSelected => isSelected;

    protected virtual void Awake()
    {
        initialized = true;
    }

    protected virtual void Start()
    {
        if (IsUnderConstruction)
            return;

        if (currentHealth <= 0 || currentHealth == 1)
            currentHealth = maxHealth;

        if (uiPrefab != null)
        {
            GameObject uiGO = Instantiate(uiPrefab);
            uiInstance = uiGO.GetComponent<BuildingUIController>();

            if (uiInstance != null)
                uiInstance.Initialize(transform, this, null);
        }
    }

    public void SetMaxHealth(int value)
    {
        maxHealth = value;
    }

    public void SetHealth(int value)
    {
        if (!initialized)
            return;

        currentHealth = Mathf.Clamp(value, 0, maxHealth);
    }

    public void SetFullHealth()
    {
        SetHealth(maxHealth);
    }

    public void SetOwner(Faction faction)
    {
        owner = faction;

        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            foreach (Material m in r.materials)
            {
                if (m.HasProperty("_Color"))
                    m.color = faction.factionColor;
            }
        }
    }

    public Faction GetOwner() => owner;

    public virtual void SetSelected(bool selected)
    {
        isSelected = selected;
    }

    public void TakeDamage(int amount)
    {
        if (isDestroyed) return;

        currentHealth -= amount;
        currentHealth = Mathf.Max(0, currentHealth);

        if (currentHealth <= 0)
            Die();
    }

    private void Die()
    {
        isDestroyed = true;
        Destroy(gameObject);
    }

    public bool IsDestroyed() => isDestroyed;

    public void SetUnderConstruction(bool active)
    {
        IsUnderConstruction = active;

        if (buildingUnderConstructionVisual != null)
            buildingUnderConstructionVisual.SetActive(active);
    }

    public void AssignUIController(BuildingUIController controller)
    {
        uiInstance = controller;
    }

    public void MarkAsCompleted()
    {
        isCompleted = true;
        IsUnderConstruction = false;

        if (buildingUnderConstructionVisual != null)
            buildingUnderConstructionVisual.SetActive(false);

        // Weitere Logik bei Fertigstellung möglich
        Debug.Log($"🏁 Gebäude '{buildingName}' fertiggestellt.");
    }
}
