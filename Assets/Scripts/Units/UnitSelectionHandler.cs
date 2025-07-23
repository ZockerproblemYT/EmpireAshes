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

    private readonly List<Unit> selectedUnits = new();
    private readonly List<Building> selectedBuildings = new();

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
            if (EventSystem.current.IsPointerOverGameObject()) return;

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
            if (Vector2.Distance(startPos, Input.mousePosition) < 10f)
            {
                SingleClickSelect();
            }
            else
            {
                SelectByBox();
            }

            selectionBox.gameObject.SetActive(false);
        }
    }

    void HandleRightClick()
    {
        if (!Input.GetMouseButtonDown(1))
            return;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit))
            return;

        if (selectedUnits.Count > 0)
        {
            foreach (Unit unit in selectedUnits)
                unit.HandleRightClick();
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
            if (EventSystem.current.IsPointerOverGameObject()) return;

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
                AddToSelection(building);
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
                    AddToSelection(building);
            }
        }
    }

    void AddToSelection(Unit unit)
    {
        if (!selectedUnits.Contains(unit))
        {
            selectedUnits.Add(unit);
            unit.SetSelected(true);
            Debug.Log($"➕ [Selection] Unit hinzugefügt: {unit.name} | Typ: {unit.GetType()}");

            workerUI?.Refresh();
        }
    }

    void AddToSelection(Building building)
    {
        if (!selectedBuildings.Contains(building))
        {
            selectedBuildings.Add(building);
            building.SetSelected(true);

            ProductionBuilding pb = building.GetComponent<ProductionBuilding>();
            if (uiManager != null)
            {
                if (selectedBuildings.Count == 1 && pb != null)
                {
                    uiManager.ShowFor(pb);
                }
                else if (selectedBuildings.Count > 1)
                {
                    uiManager.Hide();
                }
            }

            workerUI?.Refresh();
        }
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
        workerUI?.Refresh();
    }
}
