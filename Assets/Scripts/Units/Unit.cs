using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Health))]
[RequireComponent(typeof(LineRenderer))]
public class Unit : MonoBehaviour
{
    public enum AttackType { Melee, Ranged, AoE }

    public UnitRole role = UnitRole.Combat;
    public GameObject selectionCircle;
    public GameObject waypointPrefab;
    public GameObject healthBarPrefab;
    [SerializeField] public UnitData unitData;

    public float harvestRange = 1.5f;
    public float harvestTime = 2f;
    public int harvestAmount = 10;
    public int capacity = 10;

    public event Action<Unit> OnArrivedAtDestination;
    public event Action<Vector3> OnWaypointAdded;
    public event Action OnWaypointsCleared;

    private Health health;
    private NavMeshAgent agent;
    private Faction ownerFaction;
    private LineRenderer lineRenderer;
    private bool isSelected = false;

    private Queue<Vector3> waypoints = new();
    private Queue<GameObject> waypointIndicators = new();
    private Vector3? currentWaypoint = null;
    private GameObject currentIndicator = null;
    private HealthBarUI spawnedHealthBar;

    private Coroutine attackRoutine;
    private Unit targetEnemy;

    private enum State { Idle, ToResource, Harvesting, ToDropOff, ToBuild, Building }
    private State currentState = State.Idle;
    private MetalNode currentNode;
    private Refinery currentRefinery;
    private DropOffBuilding currentDropOff;
    private BuildingConstructionSite currentSite;
    private Vector3 resourceTarget, dropOffTarget;
    private float harvestTimer = 0f;
    private int carriedResources = 0;
    private bool isFarmingOil = false;
    private bool hasConfirmedArrival = false;
    private bool isMoving = false;
    private Vector3 currentTarget;

    private Dictionary<State, Action> stateHandlers;

    private bool isPatrolling = false;
    private Vector3 patrolPointA;
    private Vector3 patrolPointB;
    private Vector3 currentPatrolTarget;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        health = GetComponent<Health>();
        lineRenderer = GetComponent<LineRenderer>();
        SetupFromData();

        InitWorkerStates();

        if (healthBarPrefab != null)
            StartCoroutine(SpawnHealthBarDelayed());
    }

    private void SetupFromData()
    {
        if (unitData == null) return;

        agent.speed = unitData.moveSpeed;
        agent.angularSpeed = unitData.angularSpeed;
        agent.acceleration = unitData.acceleration;
        agent.stoppingDistance = Mathf.Max(0.5f, unitData.stoppingDistance);

        health.SetMaxHealth(unitData.maxHealth);
    }

    private IEnumerator SpawnHealthBarDelayed()
    {
        yield return null;
        GameObject hbGO = Instantiate(healthBarPrefab, transform.position + Vector3.up * 2.2f, Quaternion.identity);
        spawnedHealthBar = hbGO.GetComponentInChildren<HealthBarUI>();
        spawnedHealthBar?.Initialize(this, health);
    }

    private void Update()
    {
        if (isSelected && Input.GetMouseButtonDown(1))
            HandleRightClick();

        HandleMovement();
        UpdateWaypointLine();
        UpdateHealthbar();

        if (role == UnitRole.Combat)
            HandleCombat();
        else
            stateHandlers[currentState]?.Invoke();
    }
    public void MoveTo(Vector3 position, bool addWaypoint, GameObject overrideWaypointPrefab = null)
{
    if (addWaypoint)
    {
        Debug.Log($"[MoveTo] ➕ Shift-Klick → Wegpunkt: {position}", this);
        waypoints.Enqueue(position);
        CreateWaypointVisual(position, overrideWaypointPrefab);
        OnWaypointAdded?.Invoke(position);
        return;
    }

    // Prüfe vorab, ob Ziel auf NavMesh liegt
    if (!NavMesh.SamplePosition(position, out NavMeshHit hit, 1f, NavMesh.AllAreas))
    {
        Debug.LogWarning($"[MoveTo] ❌ Kein gültiger NavMesh-Punkt bei: {position}", this);
        return;
    }

    Debug.Log($"[MoveTo] 🖱️ Rechtsklick → Bewegung nach: {hit.position}", this);

    ClearWaypoints();

    currentWaypoint = hit.position;
    currentTarget = hit.position;
    isMoving = true;

    agent.isStopped = false;
    agent.ResetPath();
    agent.SetDestination(hit.position);

    UpdateWaypointLine();

    Debug.Log($"[MoveTo] ✅ Ziel gesetzt: {hit.position} (Distanz: {Vector3.Distance(transform.position, hit.position):F2})", this);
}


private void HandleMovement()
{
    if (!isMoving || agent.pathPending) return;

    if (!agent.hasPath || agent.remainingDistance <= agent.stoppingDistance)
    {
        float velocity = agent.velocity.magnitude;
        float distance = Vector3.Distance(transform.position, agent.destination);

        if (velocity == 0f || distance <= agent.stoppingDistance + 0.1f)
        {
            OnArrivedAtDestination?.Invoke(this);

            if (waypoints.Count > 0)
            {
                Vector3 next = waypoints.Dequeue();
                MoveTo(next, false);
            }
            else if (isPatrolling)
            {
                currentPatrolTarget = (currentPatrolTarget == patrolPointA) ? patrolPointB : patrolPointA;
                MoveTo(currentPatrolTarget, false);
            }
            else
            {
                isMoving = false;
                currentWaypoint = null;
                lineRenderer.positionCount = 0;
            }
        }
    }
}
private void SafeSetDestination(Vector3 destination)
{
    if (!agent.isOnNavMesh)
    {
        Debug.LogWarning($"[SafeSetDestination] ⚠️ Agent nicht auf NavMesh!", this);
        return;
    }

    if (NavMesh.SamplePosition(destination, out NavMeshHit hit, 1f, NavMesh.AllAreas))
    {
        agent.SetDestination(hit.position);
        Debug.Log($"[SafeSetDestination] ✅ Setze Ziel: {hit.position}", this);
    }
    else
    {
        Debug.LogWarning($"[SafeSetDestination] ❌ Kein gültiger Punkt nahe {destination}", this);
    }
}


private void CreateWaypointVisual(Vector3 pos, GameObject overridePrefab = null)
{
    GameObject visual = overridePrefab != null ? overridePrefab : Instantiate(waypointPrefab, pos, Quaternion.identity);
    SetWaypointColor(visual);
    waypointIndicators.Enqueue(visual);
    currentWaypoint = pos;
}

private void UpdateWaypointLine()
{
    if (!isSelected || (currentWaypoint == null && waypoints.Count == 0))
    {
        lineRenderer.enabled = false;
        return;
    }

    List<Vector3> positions = new() { transform.position };
    if (currentWaypoint.HasValue) positions.Add(currentWaypoint.Value);
    positions.AddRange(waypoints);

    lineRenderer.positionCount = positions.Count;
    lineRenderer.SetPositions(positions.ToArray());
    lineRenderer.enabled = true;
}

    private void HandleRightClick()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, 100f, LayerMask.GetMask("Ground", "Resource", "Selectable"))) return;

        bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        Vector3 clickPos = hit.point;
        GameObject obj = hit.collider.gameObject;

        GameObject flag = null;
        if (waypointPrefab)
        {
            flag = Instantiate(waypointPrefab, clickPos, Quaternion.identity);
            flag.layer = LayerMask.NameToLayer("Ignore Raycast");
            SetWaypointColor(flag);
        }

        if (role == UnitRole.Worker)
        {
            if (obj.GetComponentInParent<MetalNode>() is MetalNode node)
            {
                var drop = FindClosestDropOff(transform.position);
                if (!shift) CancelWorkerJob();
                if (drop != null)
                {
                    StartHarvestCycle(node, drop);
                    return;
                }
            }

            if (obj.GetComponentInParent<Refinery>() is Refinery refinery && refinery.IsCompleted)
            {
                var drop = FindClosestDropOff(transform.position);
                if (!shift) CancelWorkerJob();
                if (drop != null)
                {
                    StartOilCycle(refinery, drop);
                    return;
                }
            }

            if (!shift)
                CancelWorkerJob();
        }

        MoveTo(clickPos, shift, flag);
    }

    private void HandleCombat()
    {
        if (targetEnemy != null)
        {
            float dist = Vector3.Distance(transform.position, targetEnemy.transform.position);

            if (targetEnemy.health.Current <= 0 || dist > unitData.visionRange * 2f)
            {
                targetEnemy = null;
                if (attackRoutine != null) StopCoroutine(attackRoutine);
                attackRoutine = null;
            }
        }

        if (targetEnemy == null)
        {
            targetEnemy = FindClosestEnemyInRange(unitData.visionRange);
        }

        if (targetEnemy != null)
        {
            float dist = Vector3.Distance(transform.position, targetEnemy.transform.position);

            if (dist <= unitData.attackRange)
            {
                FaceTarget(targetEnemy.transform);

                if (attackRoutine == null)
                    attackRoutine = StartCoroutine(AttackRoutine());
            }
            else
            {
                if (!IsMoving() || currentTarget != targetEnemy.transform.position)
                    MoveTo(targetEnemy.transform.position, false);
            }
        }
    }

    private IEnumerator AttackRoutine()
    {
        agent.isStopped = true;

        while (targetEnemy != null)
        {
            float dist = Vector3.Distance(transform.position, targetEnemy.transform.position);

            if (dist <= unitData.attackRange)
            {
                FaceTarget(targetEnemy.transform);

                if (unitData.attackType == AttackType.AoE)
                {
                    Collider[] hits = Physics.OverlapSphere(transform.position, unitData.attackRange);
                    foreach (var hit in hits)
                    {
                        Unit u = hit.GetComponent<Unit>();
                        if (u != null && u != this && u.ownerFaction != this.ownerFaction)
                        {
                            u.health.TakeDamage(unitData.attackDamage, unitData.damageType, u.unitData.armor);
                        }
                    }
                }
                else
                {
                    targetEnemy.health.TakeDamage(unitData.attackDamage, unitData.damageType, targetEnemy.unitData.armor);
                }

                yield return new WaitForSeconds(1f / unitData.attackRate);
            }
            else
            {
                break;
            }
        }

        agent.isStopped = false;
        attackRoutine = null;
    }
    private Unit FindClosestEnemyInRange(float range)
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, range);
        Unit closest = null;
        float minDist = float.MaxValue;

        foreach (var hit in hits)
        {
            Unit u = hit.GetComponent<Unit>();
            if (u == null || u == this || u.ownerFaction == this.ownerFaction) continue;
            float dist = Vector3.Distance(transform.position, u.transform.position);
            if (dist < minDist)
            {
                closest = u;
                minDist = dist;
            }
        }

        return closest;
    }

    public void StartPatrol(Vector3 pointA, Vector3 pointB)
    {
        isPatrolling = true;
        patrolPointA = pointA;
        patrolPointB = pointB;
        currentPatrolTarget = patrolPointB;
        MoveTo(currentPatrolTarget, false);
    }

    private void InitWorkerStates()
    {
        stateHandlers = new()
        {
            { State.Idle, () => { } },
            { State.ToResource, () => {
                if (IsNear(resourceTarget, harvestRange)) StartHarvesting();
            }},
            { State.Harvesting, () => {
                harvestTimer -= Time.deltaTime;
                if (harvestTimer <= 0f) FinishHarvest();
            }},
            { State.ToDropOff, () => {
                if (IsNear(dropOffTarget, 1.5f)) DropOff();
            }},
            { State.ToBuild, () => {
                if (!hasConfirmedArrival && currentSite != null && IsNear(currentSite.GetClosestPoint(transform.position), 2f))
                    ConfirmArrivalAtConstruction();
            }},
            { State.Building, () => {
                currentSite?.Build(Time.deltaTime);
            }}
        };
    }

    public void StartHarvestCycle(MetalNode node, DropOffBuilding drop)
    {
        CancelWorkerJob();
        currentNode = node;
        currentDropOff = drop;
        isFarmingOil = false;
        resourceTarget = node.GetClosestPoint(transform.position);
        currentState = State.ToResource;
        MoveTo(resourceTarget, false);
    }

    public void StartOilCycle(Refinery refinery, DropOffBuilding drop)
    {
        CancelWorkerJob();
        currentRefinery = refinery;
        currentDropOff = drop;
        isFarmingOil = true;
        resourceTarget = refinery.GetClosestPoint(transform.position);
        currentState = State.ToResource;
        MoveTo(resourceTarget, false);
    }

    private void StartHarvesting()
    {
        if (carriedResources >= capacity)
        {
            GoToDropOff();
            return;
        }

        currentState = State.Harvesting;
        harvestTimer = harvestTime;
        agent.ResetPath();
    }
    private void FinishHarvest()
    {
        carriedResources = Mathf.Min(carriedResources + harvestAmount, capacity);
        if (carriedResources >= capacity) GoToDropOff();
        else StartHarvesting();
    }

    private void GoToDropOff()
    {
        dropOffTarget = currentDropOff.GetClosestPoint(transform.position);
        currentState = State.ToDropOff;
        MoveTo(dropOffTarget, false);
    }

    private void DropOff()
    {
        var type = isFarmingOil ? ResourceType.Oil : ResourceType.Metal;
        ResourceManager.Instance.AddResources(type, carriedResources);
        carriedResources = 0;
        if (isFarmingOil) StartOilCycle(currentRefinery, currentDropOff);
        else StartHarvestCycle(currentNode, currentDropOff);
    }

    public void AssignToConstruction(BuildingConstructionSite site)
    {
        CancelWorkerJob();
        currentSite = site;
        hasConfirmedArrival = false;
        site.AddBuilder(this);
        currentState = State.ToBuild;
        MoveTo(site.GetClosestPoint(transform.position), false);
    }

    private void ConfirmArrivalAtConstruction()
    {
        hasConfirmedArrival = true;
        currentSite.NotifyWorkerArrived(this);
        currentState = State.Building;
        agent.ResetPath();
    }

    public void CancelWorkerJob()
    {
        agent.ResetPath();
        currentNode = null;
        currentDropOff = null;
        currentRefinery = null;

        if (currentSite != null)
        {
            currentSite.NotifyWorkerLeft(this);
            currentSite.RemoveBuilder(this);
            currentSite = null;
        }

        carriedResources = 0;
        hasConfirmedArrival = false;
        currentState = State.Idle;
        ClearWaypoints();
    }

    private void ClearWaypoints()
    {
        waypoints.Clear();
        foreach (var obj in waypointIndicators)
            if (obj != null) Destroy(obj);
        waypointIndicators.Clear();
        currentWaypoint = null;
        currentIndicator = null;
        agent.ResetPath();
        OnWaypointsCleared?.Invoke();
    }

    private bool IsNear(Vector3 pos, float range)
    {
        if (NavMesh.SamplePosition(pos, out NavMeshHit hit, range, NavMesh.AllAreas))
            return Vector3.Distance(transform.position, hit.position) <= range;
        return false;
    }

    private void FaceTarget(Transform target)
    {
        Vector3 dir = (target.position - transform.position).normalized;
        dir.y = 0;
        if (dir != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(dir);
    }

    private DropOffBuilding FindClosestDropOff(Vector3 pos)
    {
        DropOffBuilding[] all = FindObjectsByType<DropOffBuilding>(FindObjectsSortMode.None);
        float bestDist = float.MaxValue;
        DropOffBuilding best = null;

        foreach (var b in all)
        {
            float dist = Vector3.Distance(pos, b.transform.position);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = b;
            }
        }

        return best;
    }
    private void UpdateHealthbar()
{
    if (spawnedHealthBar != null)
        spawnedHealthBar.transform.position = transform.position + Vector3.up * 2.2f;
}


    private void SetWaypointColor(GameObject flag)
    {
        if (ownerFaction != null && flag.TryGetComponent<MeshRenderer>(out var mr))
            mr.material.color = ownerFaction.factionColor;
    }

    public void SetOwner(Faction faction)
    {
        ownerFaction = faction;
        if (selectionCircle && selectionCircle.TryGetComponent<SpriteRenderer>(out var rend))
            rend.color = faction.factionColor;
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        selectionCircle?.SetActive(selected);
        spawnedHealthBar?.UpdateBar(health.Current, health.Max);
    }

    public bool IsSelected => isSelected;
    public bool IsBuilding() => currentState == State.Building;
    public bool IsMoving() => agent.hasPath && agent.remainingDistance > agent.stoppingDistance + 0.05f;
    public Faction GetOwnerFaction() => ownerFaction;
}