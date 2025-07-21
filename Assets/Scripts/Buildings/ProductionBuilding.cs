using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Collider))]
public class ProductionBuilding : Building
{
    [Header("Produktion")]
    public List<UnitData> availableUnits;
    public Transform spawnPoint;

    private Queue<UnitData> productionQueue = new();
    private float productionTimer;

    [Header("Rally Point")]
    [SerializeField] private GameObject rallyPointPrefab;
    private Vector3 rallyPoint;
    private bool hasRallyPoint = false;
    private GameObject rallyPointVisual;

    [Header("Line Renderer")]
    [SerializeField] private LineRenderer lineRenderer;

    new void Start()
    {
        base.Start();
        productionTimer = 0f;
        rallyPoint = Vector3.zero;

        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 2;
            lineRenderer.enabled = false;
        }
    }

    void Update()
    {
        if (productionQueue.Count > 0)
        {
            productionTimer += Time.deltaTime;
            UnitData current = productionQueue.Peek();

            if (productionTimer >= current.productionTime)
            {
                SpawnUnit(current);
                productionQueue.Dequeue();
                productionTimer = 0f;
            }
        }

        UpdateRallyLine();
    }

    private void UpdateRallyLine()
    {
        if (lineRenderer == null) return;

        if (hasRallyPoint)
        {
            lineRenderer.enabled = true;
            lineRenderer.SetPosition(0, transform.position + Vector3.up * 1.5f);
            lineRenderer.SetPosition(1, rallyPoint + Vector3.up * 0.2f);
        }
        else
        {
            lineRenderer.enabled = false;
        }
    }

    public void EnqueueUnit(UnitData unit)
    {
        if (unit == null)
        {
            Debug.LogWarning("⚠️ EnqueueUnit: UnitData ist null");
            return;
        }

        if (!ResourceManager.Instance.HasEnough(unit.costMetal, unit.costOil, unit.costPopulation))
        {
            Debug.Log("❌ Nicht genug Ressourcen!");
            return;
        }

        ResourceManager.Instance.Spend(unit.costMetal, unit.costOil, unit.costPopulation);
        productionQueue.Enqueue(unit);
        Debug.Log($"✅ {unit.unitName} zur Produktionswarteschlange hinzugefügt.");
    }

    private void SpawnUnit(UnitData data)
    {
        if (data == null || data.unitPrefab == null)
        {
            Debug.LogWarning("⚠️ SpawnUnit: Ungültige Daten");
            return;
        }

        Vector3 spawnPos;
        if (hasRallyPoint)
        {
            spawnPos = GetClosestPointOnBuildingTowards(rallyPoint);
        }
        else if (spawnPoint != null)
        {
            spawnPos = spawnPoint.position;
        }
        else
        {
            spawnPos = transform.position;
        }

        if (NavMesh.SamplePosition(spawnPos, out NavMeshHit hit, 3f, NavMesh.AllAreas))
        {
            spawnPos = hit.position;
        }
        else
        {
            spawnPos.y = 0f;
        }

        GameObject unitObj = Instantiate(data.unitPrefab, spawnPos, Quaternion.identity);
        Unit unit = unitObj.GetComponent<Unit>();

        if (unit != null)
        {
            unit.SetOwner(MatchManager.Instance.PlayerFaction);

            if (hasRallyPoint)
            {
                GameObject wp = null;
                if (unit.waypointPrefab != null)
                {
                    wp = Instantiate(unit.waypointPrefab, rallyPoint, Quaternion.identity);
                    wp.layer = LayerMask.NameToLayer("Ignore Raycast");
                }

                if (unit.role == UnitRole.Worker)
                {
                    // Automatisch Erntezyklus prüfen
                    Collider[] hits = Physics.OverlapSphere(rallyPoint, 1.5f);
                    foreach (var hitCollider in hits)
                    {
                        MetalNode node = hitCollider.GetComponent<MetalNode>();
                        if (node != null)
                        {
                            DropOffBuilding dropOff = FindClosestDropOff(unit.transform.position);
                            if (dropOff != null)
                            {
                                unit.StartHarvestCycle(node, dropOff);
                                Debug.Log("⛏ Worker startet Farmzyklus über RallyPoint.");
                            }
                            else
                            {
                                Debug.LogWarning("⚠️ Kein DropOffBuilding gefunden – Rally ignoriert.");
                            }
                            return;
                        }
                    }

                    // Wenn kein Resource Node, einfach laufen
                    unit.MoveTo(rallyPoint, false, wp);
                }
                else
                {
                    unit.MoveTo(rallyPoint, false, wp);
                }
            }

            Debug.Log($"🚀 Einheit gespawnt: {data.unitName} bei {spawnPos}");
        }
        else
        {
            Debug.LogWarning("⚠️ Gespawntes Objekt enthält keine Unit-Komponente");
        }
    }

    public void SetRallyPoint(Vector3 position)
    {
        rallyPoint = position;
        hasRallyPoint = true;

        if (rallyPointVisual == null && rallyPointPrefab != null)
        {
            rallyPointVisual = Instantiate(rallyPointPrefab, position, Quaternion.identity);
        }
        else if (rallyPointVisual != null)
        {
            rallyPointVisual.transform.position = position;
        }
    }

    public Vector3 GetRallyPoint() => rallyPoint;

    private Vector3 GetClosestPointOnBuildingTowards(Vector3 target)
    {
        Collider col = GetComponent<Collider>();
        if (col == null)
            return spawnPoint != null ? spawnPoint.position : transform.position;

        Vector3 dir = (target - transform.position).normalized;
        Vector3 outsidePoint = transform.position + dir * 10f;
        return col.ClosestPoint(outsidePoint);
    }

    public UnitData CurrentUnit => productionQueue.Count > 0 ? productionQueue.Peek() : null;

    public float ProductionProgress =>
        productionQueue.Count > 0
            ? Mathf.Clamp01(productionTimer / productionQueue.Peek().productionTime)
            : 0f;

    public int CountQueued(UnitData unit)
    {
        int count = 0;
        foreach (var u in productionQueue)
        {
            if (u == unit) count++;
        }
        return count;
    }

    public int GetQueueLength() => productionQueue.Count;

    private DropOffBuilding FindClosestDropOff(Vector3 position)
    {
        DropOffBuilding[] all = FindObjectsByType<DropOffBuilding>(FindObjectsSortMode.None);
        DropOffBuilding closest = null;
        float minDist = Mathf.Infinity;

        foreach (var drop in all)
        {
            float dist = Vector3.Distance(position, drop.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                closest = drop;
            }
        }

        return closest;
    }
}
