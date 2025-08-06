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
    }

    public void UpdateSelectionUI(List<Unit> selectedUnits)
    {
        if (iconContainer == null || iconPrefab == null)
            return;

        foreach (Transform child in iconContainer)
            Destroy(child.gameObject);

        Dictionary<UnitData, List<Unit>> grouped = new();

        foreach (var unit in selectedUnits)
        {
            if (unit == null || unit.unitData == null)
                continue;
            if (!grouped.TryGetValue(unit.unitData, out var list))
            {
                list = new List<Unit>();
                grouped[unit.unitData] = list;
            }
            list.Add(unit);
        }

        foreach (var kvp in grouped)
        {
            SelectionGroupIcon icon = Instantiate(iconPrefab, iconContainer);
            icon.Setup(kvp.Key, kvp.Value.Count, () =>
            {
                UnitSelectionHandler.Instance?.OverrideSelection(kvp.Value);
            });
        }
    }
}
