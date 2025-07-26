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

    public void Initialize(Faction faction, ProductionBuilding hq, ProductionBuilding barracks, UnitData worker, UnitData combat)
    {
        this.faction = faction;
        this.hq = hq;
        this.barracks = barracks;
        this.workerUnit = worker;
        this.combatUnit = combat;

        foreach (var b in FindObjectsByType<Building>(FindObjectsSortMode.None))
        {
            if (b.GetOwner() == MatchManager.Instance.PlayerFaction)
            {
                playerHQ = b;
                break;
            }
        }

        initialized = true;
        Debug.Log($"🤖 SimpleAI Initialisiert: {faction?.name}, HQ={(hq != null)}, Barracks={(barracks != null)}, Worker={worker?.unitName}, Combat={combat?.unitName}");
    }

    private void Update()
    {
        if (!initialized || faction == null) return;

        CollectUnits();
        HandleProduction();
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

        if (barracks != null && combatUnit != null && barracks.GetOwner() == faction)
        {
            barracks.EnqueueUnit(combatUnit);
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
                unit.SetTarget(enemy);
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
}
