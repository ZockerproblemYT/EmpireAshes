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

    private Queue<Vector3> waypoints = new Queue<Vector3>();
    private Queue<GameObject> waypointIndicators = new Queue<GameObject>();
    private readonly Queue<System.Action> jobQueue = new Queue<System.Action>();
    private Vector3? currentWaypoint = null;
    private GameObject currentIndicator = null;
    private HealthBarUI spawnedHealthBar;

    private Coroutine attackRoutine;
    private Unit targetEnemy;
    private Building targetBuilding;

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

        if (health != null)
            health.OnDeath.AddListener(HandleDeath);

        // ensure selection circle exists even if prefab reference lost
        if (selectionCircle == null || !selectionCircle.scene.IsValid())
        {
            Transform found = transform.Find("selectionCircle");
            if (found != null)
                selectionCircle = found.gameObject;
        }
        selectionCircle?.SetActive(false);
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
        HandleMovement();
        UpdateWaypointLine();
        UpdateHealthbar();

        if (role == UnitRole.Combat)
            HandleCombat();
        else
            stateHandlers[currentState]?.Invoke();
    }
    public void MoveTo(Vector3 position, bool addWaypoint, GameObject overrideWaypointPrefab = null, bool fromQueue = false)
    {
        if (addWaypoint)
        {
            Debug.Log($"[MoveTo] ➕ Shift-Klick → Wegpunkt: {position}", this);
            waypoints.Enqueue(position);
            GameObject visual = CreateWaypointVisual(position, overrideWaypointPrefab);
            waypointIndicators.Enqueue(visual);
            OnWaypointAdded?.Invoke(position);
            UpdateWaypointLine();
            return;
        }

    // Prüfe vorab, ob Ziel auf NavMesh liegt
        if (!NavMesh.SamplePosition(position, out NavMeshHit hit, 4f, NavMesh.AllAreas))
    {
        Debug.LogWarning($"[MoveTo] ❌ Kein gültiger NavMesh-Punkt bei: {position}", this);
        return;
    }

        Debug.Log($"[MoveTo] 🖱️ Rechtsklick → Bewegung nach: {hit.position}", this);

        if (!fromQueue)
        {
            ClearWaypoints();
            GameObject visual = CreateWaypointVisual(hit.position, overrideWaypointPrefab);
            currentIndicator = visual;
        }
        else
        {
            if (currentIndicator != null)
                Destroy(currentIndicator);
            if (waypointIndicators.Count > 0)
                currentIndicator = waypointIndicators.Dequeue();
        }

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
                MoveTo(next, false, null, true);
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
                if (jobQueue.Count > 0)
                    ProcessNextJob();
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

private GameObject CreateWaypointVisual(Vector3 pos, GameObject overridePrefab = null)
{
    GameObject visual = overridePrefab != null ? overridePrefab : Instantiate(waypointPrefab, pos, Quaternion.identity);
    SetWaypointColor(visual);
    return visual;
}

private void UpdateWaypointLine()
{
    if (!isSelected || (currentWaypoint == null && waypoints.Count == 0))
    {
        lineRenderer.enabled = false;
        return;
    }

        List<Vector3> positions = new List<Vector3> { transform.position };
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

        if (role == UnitRole.Worker)
        {
            if (obj.GetComponentInParent<BuildingConstructionSite>() is BuildingConstructionSite site && !site.IsFinished())
            {
                AssignToConstruction(site, shift);
                return;
            }

            if (obj.GetComponentInParent<MetalNode>() is MetalNode node)
            {
                var drop = FindClosestDropOff(node.transform.position);
                if (drop != null)
                {
                    StartHarvestCycle(node, drop, shift);
                    return;
                }
            }

            if (obj.GetComponentInParent<Refinery>() is Refinery refinery && refinery.IsCompleted)
            {
                var drop = FindClosestDropOff(refinery.transform.position);
                if (drop != null)
                {
                    StartOilCycle(refinery, drop, shift);
                    return;
                }
            }

        }

        // Check if clicked on enemy unit
        Unit enemyUnit = obj.GetComponentInParent<Unit>();
        if (enemyUnit != null && enemyUnit != this && IsEnemy(enemyUnit))
        {
            targetEnemy = enemyUnit;
            targetBuilding = null;
            MoveTo(enemyUnit.transform.position, false);
            return;
        }

        // Check if clicked on enemy building
        Building enemyBld = obj.GetComponentInParent<Building>();
        if (enemyBld != null && IsEnemy(enemyBld))
        {
            targetBuilding = enemyBld;
            targetEnemy = null;
            MoveTo(enemyBld.transform.position, false);
            return;
        }

        if (waypointPrefab)
        {
            flag = Instantiate(waypointPrefab, clickPos, Quaternion.identity);
            flag.layer = LayerMask.NameToLayer("Ignore Raycast");
            SetWaypointColor(flag);
        }

        targetEnemy = null;
        targetBuilding = null;
        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }

        bool hasJob = currentState != State.Idle || jobQueue.Count > 0;
        if (role == UnitRole.Worker && shift && hasJob)
        {
            QueueMoveTo(clickPos);
        }
        else
        {
            if (!shift)
                CancelWorkerJob();
            MoveTo(clickPos, shift && !hasJob, flag);
        }
    }

    private void HandleCombat()
    {
        if (unitData == null)
            return;
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

        if (targetBuilding != null)
        {
            float dist = Vector3.Distance(transform.position, targetBuilding.transform.position);

            if (targetBuilding.IsDestroyed() || dist > unitData.visionRange * 2f)
            {
                targetBuilding = null;
                if (attackRoutine != null) StopCoroutine(attackRoutine);
                attackRoutine = null;
            }
        }

        if (targetEnemy == null && targetBuilding == null)
        {
            targetEnemy = FindClosestEnemyInRange(unitData.visionRange);
            if (targetEnemy == null)
                targetBuilding = FindClosestEnemyBuildingInRange(unitData.visionRange);
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
        else if (targetBuilding != null)
        {
            float dist = Vector3.Distance(transform.position, targetBuilding.transform.position);

            if (dist <= unitData.attackRange)
            {
                FaceTarget(targetBuilding.transform);

                if (attackRoutine == null)
                    attackRoutine = StartCoroutine(AttackRoutine());
            }
            else
            {
                if (!IsMoving() || currentTarget != targetBuilding.transform.position)
                    MoveTo(targetBuilding.transform.position, false);
            }
        }
    }

    private IEnumerator AttackRoutine()
    {
        if (unitData == null)
            yield break;
        agent.isStopped = true;

        while (targetEnemy != null || targetBuilding != null)
        {
            if (targetEnemy != null)
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
                            if (u != null && u != this && IsEnemy(u))
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
                    continue;
                }
                else
                {
                    break;
                }
            }

            if (targetBuilding != null)
            {
                float dist = Vector3.Distance(transform.position, targetBuilding.transform.position);

                if (dist <= unitData.attackRange)
                {
                    FaceTarget(targetBuilding.transform);
                    targetBuilding.TakeDamage(Mathf.RoundToInt(unitData.attackDamage));
                    yield return new WaitForSeconds(1f / unitData.attackRate);
                    continue;
                }
                else
                {
                    break;
                }
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
            if (u == null || u == this || !IsEnemy(u)) continue;
            float dist = Vector3.Distance(transform.position, u.transform.position);
            if (dist < minDist)
            {
                closest = u;
                minDist = dist;
            }
        }

        return closest;
    }

    private Building FindClosestEnemyBuildingInRange(float range)
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, range);
        Building closest = null;
        float minDist = float.MaxValue;

        foreach (var hit in hits)
        {
            Building b = hit.GetComponentInParent<Building>();
            if (b == null || !IsEnemy(b)) continue;
            float dist = Vector3.Distance(transform.position, b.transform.position);
            if (dist < minDist)
            {
                closest = b;
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
        stateHandlers = new Dictionary<State, Action>()
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
                if (currentSite == null || currentSite.IsComplete())
                {
                    CancelWorkerJob(false);
                }
                else
                {
                    currentSite.Build(Time.deltaTime);
                }
            }}
        };
    }

    public void StartHarvestCycle(MetalNode node, DropOffBuilding drop, bool keepQueue = false)
    {
        CancelWorkerJob(!keepQueue, false);
        currentNode = node;
        currentDropOff = drop;
        isFarmingOil = false;
        resourceTarget = node.GetClosestPoint(transform.position);
        if (NavMesh.SamplePosition(resourceTarget, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            resourceTarget = hit.position;
        currentState = State.ToResource;
        MoveTo(resourceTarget, false);
    }

    public void StartOilCycle(Refinery refinery, DropOffBuilding drop, bool keepQueue = false)
    {
        CancelWorkerJob(!keepQueue, false);
        currentRefinery = refinery;
        currentDropOff = drop;
        isFarmingOil = true;
        resourceTarget = refinery.GetClosestPoint(transform.position);
        if (NavMesh.SamplePosition(resourceTarget, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            resourceTarget = hit.position;
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

    if (ownerFaction == null)
    {
        Debug.LogWarning($"[DropOff] ⚠️ Keine Fraktion gesetzt für Unit: {name}", this);
        return;
    }

    ResourceManager.Instance.AddResources(ownerFaction, type, carriedResources);
    Debug.Log($"[DropOff] ➕ {carriedResources} {type} an Fraktion {ownerFaction.name} übergeben", this);

    carriedResources = 0;

    if (isFarmingOil)
        StartOilCycle(currentRefinery, currentDropOff, true);
    else
        StartHarvestCycle(currentNode, currentDropOff, true);
}


    public void AssignToConstruction(BuildingConstructionSite site, bool keepQueue = false)
    {
        CancelWorkerJob(!keepQueue, false);
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

    public void CancelWorkerJob(bool clearQueue = true, bool processQueue = true)
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
        if (clearQueue)
            jobQueue.Clear();
        else if (processQueue)
            ProcessNextJob();
    }

    private void ClearWaypoints()
    {
        waypoints.Clear();
        foreach (var obj in waypointIndicators)
            if (obj != null) Destroy(obj);
        waypointIndicators.Clear();
        if (currentIndicator != null)
            Destroy(currentIndicator);
        currentWaypoint = null;
        currentIndicator = null;
        agent.ResetPath();
        OnWaypointsCleared?.Invoke();
    }

    private void ProcessNextJob()
    {
        if (currentState != State.Idle || jobQueue.Count == 0)
            return;

        var job = jobQueue.Dequeue();
        job?.Invoke();
    }

    public void QueueConstruction(BuildingConstructionSite site)
    {
        if (site == null) return;
        jobQueue.Enqueue(() => AssignToConstruction(site, true));
        if (currentState == State.Idle)
            ProcessNextJob();
    }

    public void QueueHarvestCycle(MetalNode node, DropOffBuilding drop)
    {
        if (node == null || drop == null) return;
        jobQueue.Enqueue(() => StartHarvestCycle(node, drop, true));
    }

    public void QueueOilCycle(Refinery refinery, DropOffBuilding drop)
    {
        if (refinery == null || drop == null) return;
        jobQueue.Enqueue(() => StartOilCycle(refinery, drop, true));
    }

    public void QueueMoveTo(Vector3 position)
    {
        jobQueue.Enqueue(() => MoveTo(position, false, null, true));
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
        float bestDist = float.MaxValue;
        DropOffBuilding best = null;

        foreach (var b in DropOffBuilding.Instances)
        {
            if (b == null) continue;
            var building = b.GetComponent<Building>();
            if (building != null && IsEnemy(building))
                continue;

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
        spawnedHealthBar.transform.position = transform.position + Vector3.up *2.2f;
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

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        selectionCircle?.SetActive(selected);
        spawnedHealthBar?.UpdateBar(health.Current, health.Max);
    }

    public bool IsSelected => isSelected;
    public bool IsBuilding() => currentState == State.Building;
    public bool IsIdle() => currentState == State.Idle;
    public bool IsMoving() => agent.hasPath && agent.remainingDistance > agent.stoppingDistance + 0.05f;
    public Faction GetOwnerFaction() => ownerFaction;

    public bool IsEnemy(Unit other)
    {
        if (other == null) return false;
        if (ownerFaction == null || other.ownerFaction == null) return false;
        return other.ownerFaction != ownerFaction;
    }

    public bool IsEnemy(Building building)
    {
        if (building == null) return false;
        var bOwner = building.GetOwner();
        if (ownerFaction == null || bOwner == null) return false;
        return bOwner != ownerFaction;
    }

    public void SetTarget(Unit enemy)
    {
        // avoid resetting the current attack state if we already target this unit
        if (enemy == null || !IsEnemy(enemy) || enemy == targetEnemy)
            return;

        AttackUnit(enemy);
    }

    /// <summary>
    /// Sets a building as attack target if it belongs to an enemy faction.
    /// </summary>
    /// <param name="building">Building to attack.</param>
    public void SetTarget(Building building)
    {
        if (building == null || !IsEnemy(building) || building == targetBuilding || building.IsDestroyed())
            return;

        AttackBuilding(building);
    }

    public void AttackUnit(Unit enemy)
    {
        if (enemy == null || !IsEnemy(enemy) || enemy == targetEnemy) return;

        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }

        targetEnemy = enemy;
        targetBuilding = null;
        MoveTo(enemy.transform.position, false);
    }

    public void AttackBuilding(Building building)
    {
        if (building == null || !IsEnemy(building) || building == targetBuilding) return;

        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }

        targetBuilding = building;
        targetEnemy = null;
        MoveTo(building.transform.position, false);
    }

    private void HandleDeath()
    {
        if (ownerFaction != null && unitData != null)
            ResourceManager.Instance.AddResources(ownerFaction, 0, 0, unitData.costPopulation);

        ClearWaypoints();
    }
}