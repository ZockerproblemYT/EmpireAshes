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
    [SerializeField] private GameObject selectionBoxPrefab;
    private BuildingUIController uiInstance;
    private GameObject selectionBoxInstance;

    protected Faction owner;
    private bool isDestroyed = false;
    private bool isSelected = false;
    private bool showHPBar = true;
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
        if (owner == null)
        {
            Debug.LogWarning($"⚠️ Building '{name}' hat keinen Owner. Bitte rufe SetOwner() nach dem Spawn auf!");
        }

        if (IsUnderConstruction)
            return;

        if (currentHealth <= 0 || currentHealth == 1)
            currentHealth = maxHealth;

        SpawnUI();
    }

    public void SpawnUI()
    {
        if (uiPrefab == null || uiInstance != null)
            return;

        GameObject uiGO = Instantiate(uiPrefab);
        uiInstance = uiGO.GetComponent<BuildingUIController>();

        if (uiInstance != null)
            uiInstance.Initialize(transform, this, null);
    }

    public void SetMaxHealth(int value)
    {
        maxHealth = value;
    }

    public void SetHealth(int value)
    {
        if (!initialized) return;
        currentHealth = Mathf.Clamp(value, 0, maxHealth);
    }

    public void SetFullHealth() => SetHealth(maxHealth);

    public void SetOwner(Faction faction)
    {
        if (faction == null)
        {
            Debug.LogError($"❌ SetOwner: Fraktion ist NULL für Gebäude {name}");
            return;
        }

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

    public virtual void SetSelected(bool selected, bool showHP = true)
    {
        isSelected = selected;
        showHPBar = showHP && selected;

        if (uiInstance != null)
            uiInstance.SetHPVisible(showHPBar);

        if (selected)
            ShowSelectionBox();
        else
            HideSelectionBox();
    }

    public bool ShouldShowHP() => showHPBar;

    public virtual void TakeDamage(int amount, Unit attacker = null)
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

    void ShowSelectionBox()
    {
        if (selectionBoxPrefab == null)
            return;

        if (selectionBoxInstance == null)
            selectionBoxInstance = Instantiate(
                selectionBoxPrefab,
                transform.position,
                Quaternion.identity,
                transform);

        Collider col = GetComponentInChildren<Collider>();
        if (col != null)
        {
            Bounds b = col.bounds;
            // convert world bounds to local space so scaling isn't affected by parent scale
            selectionBoxInstance.transform.localPosition = transform.InverseTransformPoint(b.center);
            Vector3 worldSize = new Vector3(b.size.x + 1f, b.size.y, b.size.z + 1f);
            Vector3 lossy = transform.lossyScale;
            if (lossy.x != 0 && lossy.y != 0 && lossy.z != 0)
            {
                selectionBoxInstance.transform.localScale = new Vector3(
                    worldSize.x / lossy.x,
                    worldSize.y / lossy.y,
                    worldSize.z / lossy.z
                );
            }
        }

        selectionBoxInstance.transform.rotation = Quaternion.identity;
        selectionBoxInstance.SetActive(true);
    }

    void HideSelectionBox()
    {
        if (selectionBoxInstance != null)
            selectionBoxInstance.SetActive(false);
    }

    public void MarkAsCompleted()
    {
        isCompleted = true;
        IsUnderConstruction = false;

        if (buildingUnderConstructionVisual != null)
            buildingUnderConstructionVisual.SetActive(false);

        Debug.Log($"🏁 Gebäude '{buildingName}' fertiggestellt.");
    }
}
