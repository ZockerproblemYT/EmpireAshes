using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UnitSelectionHandler : MonoBehaviour
{
    public static UnitSelectionHandler Instance { get; private set; }

    [Header("Layer Settings")]
    public LayerMask selectableLayer;

    [Header("Selection Box UI")]
    public RectTransform selectionBox;

    private Vector2 startPos;
    private Camera cam;

    private ProductionUIManager uiManager;
    private WorkerUI workerUI;
    private bool startedOnUI = false;

    private readonly List<Unit> selectedUnits = new List<Unit>();
    private readonly List<Building> selectedBuildings = new List<Building>();

    public List<Unit> SelectedUnits => selectedUnits;
    public List<Building> SelectedBuildings => selectedBuildings;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void Start()
    {
        cam = Camera.main;
        selectionBox.gameObject.SetActive(false);
        uiManager = FindAnyObjectByType<ProductionUIManager>();
        workerUI = FindAnyObjectByType<WorkerUI>();

        if (uiManager == null)
            Debug.LogWarning("⚠️ Kein ProductionUIManager gefunden!");

        if (workerUI == null)
            Debug.LogWarning("⚠️ Kein WorkerUI gefunden!");
    }

    void Update()
    {
        HandleLeftClick();
        HandleRightClick();
        HandleSelectionBox();
    }

    void HandleLeftClick()
    {
        if (Input.GetMouseButtonDown(0))
        {
            startedOnUI = EventSystem.current.IsPointerOverGameObject();
            if (startedOnUI) return;

            startPos = Input.mousePosition;

            bool isPlacing = BuildingPlacer.Instance != null && BuildingPlacer.Instance.IsPlacing;

            if (!Input.GetKey(KeyCode.LeftShift) && !isPlacing)
            {
                DeselectAll();
                uiManager?.Hide();
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (startedOnUI || (EventSystem.current.IsPointerOverGameObject() && !selectionBox.gameObject.activeSelf))
            {
                selectionBox.gameObject.SetActive(false);
                startedOnUI = false;
                return;
            }

            if (Vector2.Distance(startPos, Input.mousePosition) < 10f)
            {
                SingleClickSelect();
            }
            else
            {
                SelectByBox();
            }

            selectionBox.gameObject.SetActive(false);
            startedOnUI = false;
        }
    }

    void HandleRightClick()
    {
        if (!Input.GetMouseButtonDown(1))
            return;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray);
        if (hits.Length == 0)
            return;

        RaycastHit hit = default;
        float bestDist = float.MaxValue;
        bool found = false;

        foreach (var h in hits)
        {
            Unit u = h.collider.GetComponentInParent<Unit>();
            if (u != null && selectedUnits.Contains(u))
                continue;

            if (h.distance < bestDist)
            {
                bestDist = h.distance;
                hit = h;
                found = true;
            }
        }

        if (!found)
            return;

        bool shift = Input.GetKey(KeyCode.LeftShift);

        Unit clickedUnit = hit.collider.GetComponentInParent<Unit>();
        Building clickedBuilding = hit.collider.GetComponentInParent<Building>();

        if (selectedUnits.Count > 0)
        {
            foreach (Unit unit in selectedUnits)
            {
                if (unit.role == UnitRole.Worker)
                {
                    if (hit.collider.GetComponentInParent<BuildingConstructionSite>() is BuildingConstructionSite site)
                    {
                        if (!shift)
                            unit.CancelWorkerJob();
                        unit.AssignToConstruction(site, shift);
                        continue;
                    }

                    // Ressourceninteraktion prüfen
                    if (hit.collider.GetComponentInParent<MetalNode>() is MetalNode node)
                    {
                        var drop = FindClosestDropOff(node.transform.position, unit.GetOwnerFaction());
                        if (!shift) unit.CancelWorkerJob();
                        if (drop != null)
                        {
                            unit.StartHarvestCycle(node, drop);
                            continue;
                        }
                    }

                    if (hit.collider.GetComponentInParent<Refinery>() is Refinery refinery && refinery.IsCompleted)
                    {
                        var drop = FindClosestDropOff(refinery.transform.position, unit.GetOwnerFaction());
                        if (!shift) unit.CancelWorkerJob();
                        if (drop != null)
                        {
                            unit.StartOilCycle(refinery, drop);
                            continue;
                        }
                    }

                    if (!shift)
                        unit.CancelWorkerJob();
                }

                if (clickedUnit != null && unit.IsEnemy(clickedUnit))
                {
                    unit.AttackUnit(clickedUnit);
                    continue;
                }

                if (clickedBuilding != null && unit.IsEnemy(clickedBuilding))
                {
                    unit.AttackBuilding(clickedBuilding);
                    continue;
                }

                GameObject flag = null;
                if (unit.waypointPrefab != null)
                    flag = Instantiate(unit.waypointPrefab, hit.point, Quaternion.identity);

                unit.MoveTo(hit.point, shift, flag);
            }
        }
        else if (selectedBuildings.Count == 1)
        {
            ProductionBuilding pb = selectedBuildings[0].GetComponent<ProductionBuilding>();
            if (pb != null)
            {
                pb.SetRallyPoint(hit.point);
                Debug.Log($"🎯 RallyPoint gesetzt bei: {hit.point}");
            }
        }
    }

    void HandleSelectionBox()
    {
        if (Input.GetMouseButton(0))
        {
            if (startedOnUI || EventSystem.current.IsPointerOverGameObject()) return;

            if (!selectionBox.gameObject.activeSelf)
                selectionBox.gameObject.SetActive(true);

            Vector2 current = Input.mousePosition;
            Vector2 size = current - startPos;

            selectionBox.anchoredPosition = startPos;
            selectionBox.sizeDelta = new Vector2(Mathf.Abs(size.x), Mathf.Abs(size.y));
            selectionBox.pivot = new Vector2(size.x < 0 ? 1 : 0, size.y < 0 ? 1 : 0);
        }
    }

    void SingleClickSelect()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, selectableLayer))
        {
            Unit unit = hit.collider.GetComponentInParent<Unit>();
            Building building = hit.collider.GetComponentInParent<Building>();

            if (unit != null)
                AddToSelection(unit);
            else if (building != null)
                AddToSelection(building, false);
        }
    }

    void SelectByBox()
    {
        Vector2 min = Vector2.Min(startPos, Input.mousePosition);
        Vector2 max = Vector2.Max(startPos, Input.mousePosition);
        Rect selectionRect = new Rect(min, max - min);

        bool foundUnit = false;

        foreach (Unit unit in FindObjectsByType<Unit>(FindObjectsSortMode.None))
        {
            if (unit == null) continue;

            Vector3 screenPos = cam.WorldToScreenPoint(unit.transform.position);
            if (screenPos.z < 0) continue;

            if (selectionRect.Contains(screenPos))
            {
                AddToSelection(unit);
                foundUnit = true;
            }
        }

        if (!foundUnit)
        {
            foreach (Building building in FindObjectsByType<Building>(FindObjectsSortMode.None))
            {
                if (building == null) continue;

                Vector3 screenPos = cam.WorldToScreenPoint(building.transform.position);
                if (screenPos.z < 0) continue;

                if (selectionRect.Contains(screenPos))
                    AddToSelection(building, true);
            }
        }
    }

    void AddToSelection(Unit unit)
    {
        if (unit == null)
            return;

        Faction playerFaction = MatchManager.Instance?.PlayerFaction;
        if (playerFaction != null && unit.GetOwnerFaction() != playerFaction)
            return;

        if (!selectedUnits.Contains(unit))
        {
            selectedUnits.Add(unit);
            unit.SetSelected(true);
            Debug.Log($"➕ [Selection] Unit hinzugefügt: {unit.name} | Typ: {unit.GetType()}");

            workerUI?.Refresh();
        }
    }

    void AddToSelection(Building building, bool showHP = true)
    {
        if (building == null)
            return;

        Faction playerFaction = MatchManager.Instance?.PlayerFaction;
        if (playerFaction != null && building.GetOwner() != playerFaction)
            return;

        if (!selectedBuildings.Contains(building))
        {
            selectedBuildings.Add(building);
            building.SetSelected(true, showHP);

            ProductionBuilding pb = building.GetComponent<ProductionBuilding>();
            if (pb != null && uiManager != null)
            {
                uiManager.ShowFor(pb);
            }

            Smithy smithy = building.GetComponent<Smithy>();
            if (smithy != null && SmithyUpgradeUI.Instance != null)
                SmithyUpgradeUI.Instance.ShowFor(smithy);

            workerUI?.Refresh();
        }
    }

    DropOffBuilding FindClosestDropOff(Vector3 position, Faction faction)
    {
        float bestDist = float.MaxValue;
        DropOffBuilding best = null;

        foreach (var drop in DropOffBuilding.Instances)
        {
            if (drop == null) continue;
            var building = drop.GetComponent<Building>();
            if (building != null && building.GetOwner() != faction)
                continue;

            float dist = Vector3.Distance(position, drop.transform.position);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = drop;
            }
        }

        return best;
    }

    void DeselectAll()
    {
        foreach (Unit unit in selectedUnits)
            unit.SetSelected(false);
        selectedUnits.Clear();

        foreach (Building building in selectedBuildings)
            building.SetSelected(false);
        selectedBuildings.Clear();

        Debug.Log("🧹 Auswahl geleert.");
        uiManager?.Hide();
        SmithyUpgradeUI.Instance?.Hide();
        workerUI?.Refresh();
    }
}
