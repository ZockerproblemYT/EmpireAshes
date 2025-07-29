using System.Collections.Generic;
using UnityEngine;

public class BuildingConstructionSite : MonoBehaviour
{
    [Header("Baudaten")]
    public BuildingData buildingData;

    [Header("Fortschritt")]
    public float currentProgress = 0f;
    public float buildTime = 5f;

    private List<Unit> assignedBuilders = new List<Unit>();
    private HashSet<Unit> arrivedBuilders = new HashSet<Unit>();

    private bool isCompleted = false;
    private Faction owner;
    private Transform visual;
    private Building hpBuilding;

    [Header("UI")]
    [SerializeField] private GameObject uiPrefab;
    private BuildingUIController uiInstance;

    void Awake()
    {
        visual = transform.GetChild(0);
    }

    void Start()
    {
        SpawnUI();
    }

    void Update()
    {
        if (isCompleted || hpBuilding == null) return;

        int activeBuilders = 0;

        foreach (Unit unit in arrivedBuilders)
        {
            if (unit == null) continue;

            if (unit.role == UnitRole.Worker && unit.IsBuilding())
                activeBuilders++;
        }

        if (activeBuilders > 0)
        {
            float buildDelta = Time.deltaTime * Mathf.Min(activeBuilders, 4); // Max 4 Builder gleichzeitig
            Build(buildDelta);
        }
    }

    public void Initialize(BuildingData data, Faction faction)
    {
        buildingData = data;
        buildTime = data.buildTime;
        owner = faction;

        hpBuilding = GetComponent<Building>() ?? GetComponentInChildren<Building>();
        if (hpBuilding == null)
        {
            Debug.LogError("❌ Kein Building-Script für HP gefunden!");
            return;
        }

        hpBuilding.SetUnderConstruction(true);
        hpBuilding.SetMaxHealth(buildingData.maxHealth);
        hpBuilding.SetHealth(1);
    }

    public void AddBuilder(Unit unit)
    {
        if (unit.role != UnitRole.Worker) return;

        if (!assignedBuilders.Contains(unit))
        {
            assignedBuilders.Add(unit);
            Debug.Log($"👷 Worker zugewiesen: {unit.name}");
        }
    }

    public void RemoveBuilder(Unit unit)
    {
        if (assignedBuilders.Remove(unit))
            Debug.Log($"❌ Worker entfernt: {unit.name}");

        if (arrivedBuilders.Remove(unit))
            Debug.Log($"🚶‍♂️ Worker hat Baustelle verlassen: {unit.name}");
    }

    public void NotifyWorkerArrived(Unit unit)
    {
        if (unit.role != UnitRole.Worker) return;

        AddBuilder(unit);

        if (!arrivedBuilders.Contains(unit))
        {
            arrivedBuilders.Add(unit);
            Debug.Log($"✅ Worker angekommen: {unit.name}");
        }
    }

    public void NotifyWorkerLeft(Unit unit)
    {
        if (unit.role != UnitRole.Worker) return;

        if (arrivedBuilders.Remove(unit))
            Debug.Log($"🚪 Worker hat Baustelle verlassen: {unit.name}");
    }

    public void Build(float amount)
    {
        if (isCompleted || hpBuilding == null) return;

        float buildFraction = amount / buildTime;
        currentProgress += buildFraction;
        currentProgress = Mathf.Clamp01(currentProgress);

        int maxHP = hpBuilding.MaxHealth;
        int targetHP = Mathf.Clamp(Mathf.RoundToInt(maxHP * currentProgress), 1, maxHP);
        hpBuilding.SetHealth(targetHP);

        if (currentProgress >= 1f)
            CompleteConstruction();
    }

    public void CancelConstruction()
    {
        if (isCompleted) return;

        // Refund 70% of spent resources
        int refundMetal = Mathf.RoundToInt(buildingData.costMetal * 0.7f);
        int refundOil = Mathf.RoundToInt(buildingData.costOil * 0.7f);
        int refundPop = Mathf.RoundToInt(buildingData.costPopulation * 0.7f);
        ResourceManager.Instance.Refund(owner, refundMetal, refundOil, refundPop);

        // Inform assigned builders
        var buildersCopy = new List<Unit>(assignedBuilders);
        foreach (var worker in buildersCopy)
        {
            if (worker != null)
                worker.CancelWorkerJob();
        }

        if (uiInstance != null)
            Destroy(uiInstance.gameObject);

        if (hpBuilding != null)
            Destroy(hpBuilding.gameObject);

        Destroy(gameObject);
    }

    private void CompleteConstruction()
    {
        isCompleted = true;

        GameObject built = Instantiate(buildingData.prefab, transform.position, transform.rotation);
        var buildings = built.GetComponents<Building>();
        Refinery refinery = built.GetComponent<Refinery>();

        Building finalBuilding = null;
        if (buildings.Length > 0)
        {
            finalBuilding = buildings[0];
            foreach (var b in buildings)
            {
                b.SetOwner(owner);
                b.SetFullHealth();
                b.MarkAsCompleted();
            }
        }
        else
        {
            Debug.LogWarning("⚠️ Finales Gebäude hat kein Building.cs");
        }

        if (finalBuilding != null && uiPrefab != null)
        {
            GameObject uiGO = Instantiate(uiPrefab);
            BuildingUIController ui = uiGO.GetComponent<BuildingUIController>();
            if (ui != null)
                ui.Initialize(built.transform, finalBuilding, null);
        }

        if (refinery != null)
        {
            var buildersCopy = new List<Unit>(arrivedBuilders);
            foreach (var worker in buildersCopy)
            {
                if (worker == null) continue;
                DropOffBuilding drop = FindClosestDropOff(worker.transform.position);
                if (drop != null)
                    worker.StartOilCycle(refinery, drop);
            }
        }

        if (uiInstance != null)
            Destroy(uiInstance.gameObject);

        if (hpBuilding != null)
            Destroy(hpBuilding);

        Destroy(gameObject);
    }

    private void SpawnUI()
    {
        if (uiPrefab == null) return;

        GameObject uiGO = Instantiate(uiPrefab);
        uiInstance = uiGO.GetComponent<BuildingUIController>();
        if (uiInstance != null)
            uiInstance.Initialize(transform, hpBuilding, this);
    }

    public Vector3 GetClosestPoint(Vector3 from)
    {
        Collider col = GetComponentInChildren<Collider>();
        return col != null ? col.ClosestPoint(from) : transform.position;
    }

    private DropOffBuilding FindClosestDropOff(Vector3 position)
    {
        DropOffBuilding[] drops = FindObjectsByType<DropOffBuilding>(FindObjectsSortMode.None);
        DropOffBuilding best = null;
        float bestDist = float.MaxValue;

        foreach (var d in drops)
        {
            float dist = Vector3.Distance(position, d.transform.position);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = d;
            }
        }

        return best;
    }

    // 🔎 Public Helper
    public bool IsFinished() => currentProgress >= 1f;
    public bool IsComplete() => isCompleted;
    public float GetProgress() => currentProgress;
    public float GetProgress01() => Mathf.Clamp01(currentProgress);
    public int GetBuilderCount() => assignedBuilders.Count;
}
