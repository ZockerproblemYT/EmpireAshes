using System.Collections.Generic;
using UnityEngine;

public class SelectionGroupUI : MonoBehaviour
{
    public static SelectionGroupUI Instance { get; private set; }

    public SelectionGroupIcon iconPrefab;
    public Transform iconContainer;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (iconContainer == null)
            iconContainer = transform;

        // the prefab is kept as a hidden template so it must not be
        // destroyed when clearing the container
        if (iconPrefab != null)
            iconPrefab.gameObject.SetActive(false);

        // hide by default until we have something to show
        gameObject.SetActive(false);
    }

    public void UpdateSelectionUI(List<Unit> selectedUnits, List<Building> selectedBuildings)
    {
        if (iconContainer == null)
            iconContainer = transform;

        bool hasSelection = (selectedUnits != null && selectedUnits.Count > 0) ||
                            (selectedBuildings != null && selectedBuildings.Count > 0);

        gameObject.SetActive(hasSelection);

        if (!hasSelection || iconPrefab == null)
            return;

        foreach (Transform child in iconContainer)
        {
            // keep the prefab template so future updates can instantiate it
            if (iconPrefab != null && child == iconPrefab.transform)
                continue;
            Destroy(child.gameObject);
        }

        Dictionary<UnitData, List<Unit>> unitGroups = new Dictionary<UnitData, List<Unit>>();
        if (selectedUnits != null)
        {
            foreach (var unit in selectedUnits)
            {
                if (unit == null || unit.unitData == null)
                    continue;

                if (!unitGroups.TryGetValue(unit.unitData, out var list))
                {
                    list = new List<Unit>();
                    unitGroups[unit.unitData] = list;
                }
                list.Add(unit);
            }
        }

        Dictionary<BuildingData, List<Building>> buildingGroups = new Dictionary<BuildingData, List<Building>>();
        if (selectedBuildings != null)
        {
            foreach (var building in selectedBuildings)
            {
                if (building == null)
                    continue;

                TooltipTrigger trigger = building.GetComponent<TooltipTrigger>();
                BuildingData data = trigger != null ? trigger.buildingData : null;
                if (data == null)
                    continue;

                if (!buildingGroups.TryGetValue(data, out var list))
                {
                    list = new List<Building>();
                    buildingGroups[data] = list;
                }
                list.Add(building);
            }
        }

        foreach (var kvp in unitGroups)
        {
            SelectionGroupIcon icon = Instantiate(iconPrefab, iconContainer);
            icon.gameObject.SetActive(true);
            icon.Setup(kvp.Key.unitIcon, kvp.Value.Count, () =>
            {
                var handler = UnitSelectionHandler.Instance;
                if (handler != null)
                    handler.OverrideSelection(kvp.Value, null);
            });
        }

        foreach (var kvp in buildingGroups)
        {
            SelectionGroupIcon icon = Instantiate(iconPrefab, iconContainer);
            icon.gameObject.SetActive(true);
            icon.Setup(kvp.Key.icon, kvp.Value.Count, () =>
            {
                var handler = UnitSelectionHandler.Instance;
                if (handler != null)
                    handler.OverrideSelection(null, kvp.Value);
            });
        }
    }
}
