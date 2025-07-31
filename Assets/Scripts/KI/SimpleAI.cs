using System.Collections.Generic;
using UnityEngine;

public class SimpleAI : MonoBehaviour
{
    [Header("Setup")]
    public Faction faction;
    public ProductionBuilding hq;
    public ProductionBuilding barracks;
    public UnitData workerUnit;
    public UnitData combatUnit;
    private readonly List<UnitData> combatUnitOptions = new();
    private readonly List<ProductionBuilding> barracksList = new();

    [Header("Verhalten")]
    public int desiredWorkers = 3;
    public int attackGroupSize = 4;
    public float attackInterval = 30f;

    private readonly List<Unit> units = new List<Unit>();
    private readonly List<Unit> workers = new List<Unit>();
    private readonly List<Unit> combatUnits = new List<Unit>();
    private float attackTimer;
    private Building playerHQ;
    private bool initialized = false;

    // Neubau-Logik
    private BuildingData barracksData;
    private BuildingData houseData;
    private BuildingData bunkerData;
    private bool builtExtraBarracks = false;
    private bool builtHouse = false;
    private bool builtBunker = false;

    // Layer mask used for placement checks (mirrors BuildingPlacer settings)
    private LayerMask obstacleLayer;

    private void Awake()
    {
        // Mirror the obstacle mask from BuildingPlacer
        obstacleLayer = LayerMask.GetMask("Default", "Water", "Selectable", "Resource", "Ghost");
    }

    public void Initialize(Faction faction, ProductionBuilding hq, ProductionBuilding barracks, UnitData worker, UnitData combat)
    {
        this.faction = faction;
        this.hq = hq;
        this.barracks = barracks;
        this.workerUnit = worker;
        this.combatUnit = combat;

        combatUnitOptions.Clear();
        if (faction != null && faction.availableUnits != null)
        {
            foreach (var ud in faction.availableUnits)
            {
                if (ud != null && ud.role == UnitRole.Combat)
                    combatUnitOptions.Add(ud);
            }
        }

        // verfügbare BuildingData zuordnen
        if (faction != null && faction.availableBuildings != null)
        {
            foreach (var bData in faction.availableBuildings)
            {
                switch (bData.buildingType)
                {
                    case BuildingType.Barracks:
                        barracksData = bData;
                        break;
                    case BuildingType.House:
                        houseData = bData;
                        break;
                    case BuildingType.Bunker:
                        bunkerData = bData;
                        break;
                }
            }
        }

        foreach (var b in FindObjectsByType<Building>(FindObjectsSortMode.None))
        {
            if (b.GetOwner() == MatchManager.Instance.PlayerFaction)
            {
                playerHQ = b;
                break;
            }
        }

        // Start attack timer so the AI waits before the first assault
        attackTimer = attackInterval;

        initialized = true;
        Debug.Log($"🤖 SimpleAI Initialisiert: {faction?.name}, HQ={(hq != null)}, BarracksList={barracksList.Count}, Worker={worker?.unitName}, CombatUnits={combatUnitOptions.Count}");
    }

    private void Update()
    {
        if (!initialized || faction == null) return;

        RefreshProductionBuildings();
        CollectUnits();
        HandleProduction();
        HandleConstruction();
        EnsureConstructionProgress();
        AssignWorkers();
        HandleAttack();
        SearchForTargets();
    }

    private void CollectUnits()
    {
        units.Clear();
        workers.Clear();
        combatUnits.Clear();

        foreach (var u in FindObjectsByType<Unit>(FindObjectsSortMode.None))
        {
            if (u == null || u.GetOwnerFaction() != faction) continue;

            units.Add(u);
            if (u.role == UnitRole.Worker) workers.Add(u);
            else combatUnits.Add(u);
        }
    }

    private void HandleProduction()
    {
        if (faction == null) return;

        if (hq != null && workerUnit != null && hq.GetOwner() == faction)
        {
            if (workers.Count + hq.CountQueued(workerUnit) < desiredWorkers)
            {
                hq.EnqueueUnit(workerUnit);
            }
        }

        if (combatUnitOptions.Count > 0 && barracksList.Count > 0)
        {
            foreach (var pb in barracksList)
            {
                if (pb == null) continue;

                foreach (var cu in combatUnitOptions)
                {
                    if (cu == null) continue;
                    if (!pb.availableUnits.Contains(cu))
                        continue;
                    if (pb.CountQueued(cu) == 0)
                        pb.EnqueueUnit(cu);
                }
            }
        }
    }

    private void AssignWorkers()
    {
        foreach (var w in workers)
        {
            if (!w.IsIdle() || w.IsMoving() || w.IsBuilding() || w.IsSelected)
                continue;

            if (!TryEnsureHarvest(w))
            {
                if (hq != null)
                    w.MoveTo(hq.transform.position, false);
            }
        }
    }

    private bool TryEnsureHarvest(Unit worker)
    {
        MetalNode node = FindClosest<MetalNode>(worker.transform.position);
        DropOffBuilding drop = FindClosest<DropOffBuilding>(worker.transform.position);
        if (node != null && drop != null)
        {
            worker.StartHarvestCycle(node, drop);
            return true;
        }
        return false;
    }

    private T FindClosest<T>(Vector3 pos) where T : MonoBehaviour
    {
        T[] all = FindObjectsByType<T>(FindObjectsSortMode.None);
        float dist = float.MaxValue;
        T best = null;
        foreach (var t in all)
        {
            float d = Vector3.Distance(pos, t.transform.position);
            if (d < dist)
            {
                dist = d;
                best = t;
            }
        }
        return best;
    }

    private void RefreshProductionBuildings()
    {
        barracksList.Clear();
        ProductionBuilding[] all = FindObjectsByType<ProductionBuilding>(FindObjectsSortMode.None);

        foreach (var pb in all)
        {
            if (pb == null) continue;
            var building = pb.GetComponent<Building>();
            if (building == null || !building.IsCompleted || building.GetOwner() != faction)
                continue;

            if (hq == null && workerUnit != null && pb.availableUnits.Contains(workerUnit))
                hq = pb;

            bool isBarracks = false;
            foreach (var cu in combatUnitOptions)
            {
                if (cu != null && pb.availableUnits.Contains(cu))
                {
                    isBarracks = true;
                    break;
                }
            }

            if (isBarracks)
            {
                barracksList.Add(pb);
                if (barracks == null)
                    barracks = pb;
            }
        }
    }

    private void HandleAttack()
    {
        if (playerHQ == null) return;
        attackTimer -= Time.deltaTime;
        if (attackTimer > 0f) return;
        if (combatUnits.Count < attackGroupSize) return;

        foreach (var c in combatUnits)
        {
            c.MoveTo(playerHQ.transform.position, false);
        }
        attackTimer = attackInterval;
    }

    private void SearchForTargets()
    {
        foreach (var unit in combatUnits)
        {
            if (unit == null || unit.unitData == null) continue;
            Unit enemy = FindEnemyInRange(unit.transform.position, unit.unitData.visionRange);
            if (enemy != null)
            {
                unit.SetTarget(enemy);
                continue;
            }

            Building building = FindEnemyBuildingInRange(unit.transform.position, unit.unitData.visionRange);
            if (building != null)
                unit.SetTarget(building);
        }
    }

    private Unit FindEnemyInRange(Vector3 pos, float range)
    {
        Collider[] hits = Physics.OverlapSphere(pos, range);
        Unit best = null;
        float dist = float.MaxValue;
        foreach (var hit in hits)
        {
            Unit u = hit.GetComponent<Unit>();
            if (u == null || u.GetOwnerFaction() == faction) continue;
            float d = Vector3.Distance(pos, u.transform.position);
            if (d < dist)
            {
                dist = d;
                best = u;
            }
        }
        return best;
    }

    private Building FindEnemyBuildingInRange(Vector3 pos, float range)
    {
        Collider[] hits = Physics.OverlapSphere(pos, range);
        Building best = null;
        float dist = float.MaxValue;
        foreach (var hit in hits)
        {
            Building b = hit.GetComponentInParent<Building>();
            if (b == null) continue;
            if (!b.IsCompleted || b.IsDestroyed()) continue;
            if (b.GetOwner() == faction) continue;
            float d = Vector3.Distance(pos, b.transform.position);
            if (d < dist)
            {
                dist = d;
                best = b;
            }
        }
        return best;
    }

    // -------------------------------------------------------
    // Bau- und Produktionslogik

    private void HandleConstruction()
    {
        if (hq == null) return;

        // Status der Gebäude aktualisieren
        if (houseData != null)
            builtHouse = HasCompletedBuilding(houseData.buildingName);
        if (barracksData != null)
            builtExtraBarracks = HasCompletedBuilding(barracksData.buildingName);
        if (bunkerData != null)
            builtBunker = HasCompletedBuilding(bunkerData.buildingName);

        // Nur bauen, wenn kein fertiges Gebäude und keine Baustelle existiert
        if (!builtHouse && houseData != null && !HasConstructionSite(houseData.buildingName))
        {
            TryBuild(houseData, hq.transform.position + new Vector3(4f, 0f, -4f));
        }

        if (!builtExtraBarracks && barracksData != null && !HasConstructionSite(barracksData.buildingName))
        {
            TryBuild(barracksData, hq.transform.position + new Vector3(-6f, 0f, 3f));
        }

        if (!builtBunker && bunkerData != null && !HasConstructionSite(bunkerData.buildingName))
        {
            TryBuild(bunkerData, hq.transform.position + new Vector3(0f, 0f, 8f));
        }
    }

    private bool TryBuild(BuildingData data, Vector3 position)
    {
        if (!ResourceManager.Instance.HasEnough(faction, data.costMetal, data.costOil, data.costPopulation))
            return false;

        if (!FindValidBuildPosition(data, ref position))
            return false;

        GameObject siteObj = Instantiate(data.constructionPrefab, position, Quaternion.identity);
        BuildingConstructionSite site = siteObj.GetComponent<BuildingConstructionSite>();
        if (site != null)
        {
            site.Initialize(data, faction);
            ResourceManager.Instance.Spend(faction, data.costMetal, data.costOil, data.costPopulation);

            int assigned = 0;
            foreach (var w in workers)
            {
                if (assigned >= 2) break;
                if (w == null) continue;

                if (w.IsIdle() && !w.IsMoving() && !w.IsBuilding())
                    w.AssignToConstruction(site);
                else
                    w.QueueConstruction(site);

                assigned++;
            }
            return true;
        }

        Destroy(siteObj);
        return false;
    }

    private bool FindValidBuildPosition(BuildingData data, ref Vector3 position)
    {
        if (IsPlacementValid(data, position))
            return true;

        float[] radii = { 4f, 6f, 8f, 10f };
        for (int r = 0; r < radii.Length; r++)
        {
            for (int i = 0; i < 8; i++)
            {
                Vector3 offset = Quaternion.Euler(0f, i * 45f, 0f) * Vector3.forward * radii[r];
                Vector3 candidate = hq.transform.position + offset;
                if (IsPlacementValid(data, candidate))
                {
                    position = candidate;
                    return true;
                }
            }
        }

        return false;
    }

    private bool IsPlacementValid(BuildingData data, Vector3 position)
    {
        var renderer = data.constructionPrefab.GetComponentInChildren<Renderer>();
        Vector3 halfExtents = renderer != null ? renderer.bounds.extents : Vector3.one;

        Collider[] colliders = Physics.OverlapBox(position, halfExtents, Quaternion.identity, obstacleLayer);

        foreach (var col in colliders)
        {
            if (col.GetComponent<OilNode>() != null || col.GetComponent<MetalNode>() != null)
                continue;

            if (!col.isTrigger)
                return false;
        }

        bool isOverOil = false;
        bool isOverMetal = false;

        Collider[] hits = Physics.OverlapSphere(position, 1.5f);
        foreach (var hit in hits)
        {
            if (hit.GetComponent<OilNode>() != null)
                isOverOil = true;
            if (hit.GetComponent<MetalNode>() != null)
                isOverMetal = true;
        }

        switch (data.buildingType)
        {
            case BuildingType.Refinery:
                if (!isOverOil)
                    return false;
                break;

            default:
                if (isOverOil || isOverMetal)
                    return false;
                break;
        }

        return true;
    }

    private bool HasCompletedBuilding(string name)
    {
        var buildings = FindObjectsByType<Building>(FindObjectsSortMode.None);
        foreach (var b in buildings)
        {
            if (b == null || !b.IsCompleted) continue;
            if (b.GetOwner() != faction) continue;
            if (b.buildingName == name)
                return true;
        }
        return false;
    }

    private bool HasConstructionSite(string name)
    {
        var sites = FindObjectsByType<BuildingConstructionSite>(FindObjectsSortMode.None);
        foreach (var s in sites)
        {
            if (s == null || s.IsComplete()) continue;
            var building = s.GetComponent<Building>() ?? s.GetComponentInChildren<Building>();
            if (building == null || building.GetOwner() != faction) continue;
            if (s.buildingData != null && s.buildingData.buildingName == name)
                return true;
        }
        return false;
    }

    private void EnsureConstructionProgress()
    {
        var sites = FindObjectsByType<BuildingConstructionSite>(FindObjectsSortMode.None);
        foreach (var site in sites)
        {
            if (site == null || site.IsComplete())
                continue;

            var build = site.GetComponent<Building>() ?? site.GetComponentInChildren<Building>();
            if (build == null || build.GetOwner() != faction)
                continue;

            int needed = Mathf.Max(0, 2 - site.GetBuilderCount());
            foreach (var w in workers)
            {
                if (needed <= 0) break;
                if (w == null) continue;

                if (w.IsIdle() && !w.IsMoving() && !w.IsBuilding())
                    w.AssignToConstruction(site);
                else
                    w.QueueConstruction(site);

                needed--;
            }
        }
    }
}
