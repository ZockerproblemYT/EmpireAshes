// Assets/Scripts/UI/SelectedUnitsUI.cs
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SelectedUnitsUI : MonoBehaviour
{
    public static SelectedUnitsUI Instance { get; private set; }

    [Header("UI References")]
    public GameObject panel;
    public Transform container;
    public GameObject iconPrefab;

    private UnitSelectionHandler selectionHandler;

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
        selectionHandler = UnitSelectionHandler.Instance ?? FindAnyObjectByType<UnitSelectionHandler>();
        if (panel != null)
            panel.SetActive(false);
    }

    public void Refresh()
    {
        selectionHandler ??= UnitSelectionHandler.Instance ?? FindAnyObjectByType<UnitSelectionHandler>();
        if (selectionHandler == null || container == null || iconPrefab == null)
            return;

        foreach (Transform child in container)
            Destroy(child.gameObject);

        List<Unit> units = selectionHandler.SelectedUnits;
        if (units.Count == 0)
        {
            if (panel != null && panel.activeSelf)
                panel.SetActive(false);
            return;
        }

        if (panel != null && !panel.activeSelf)
            panel.SetActive(true);

        Dictionary<UnitData, List<Unit>> groups = new Dictionary<UnitData, List<Unit>>();
        foreach (Unit unit in units)
        {
            if (unit == null || unit.unitData == null)
                continue;
            if (!groups.TryGetValue(unit.unitData, out var list))
            {
                list = new List<Unit>();
                groups[unit.unitData] = list;
            }
            list.Add(unit);
        }

        foreach (var kvp in groups)
        {
            GameObject entry = Instantiate(iconPrefab, container);
            entry.SetActive(true);

            Transform iconTransform = entry.transform.Find("Icon");
            if (iconTransform != null)
            {
                Image img = iconTransform.GetComponent<Image>();
                if (img != null && kvp.Key.unitIcon != null)
                    img.sprite = kvp.Key.unitIcon;
            }

            Transform countTransform = entry.transform.Find("Count");
            if (countTransform != null)
            {
                TextMeshProUGUI txt = countTransform.GetComponent<TextMeshProUGUI>();
                if (txt != null)
                    txt.text = kvp.Value.Count.ToString();
            }

            UnitData data = kvp.Key;
            Button btn = entry.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.AddListener(() =>
                {
                    UnitSelectionHandler.Instance?.SelectUnitsByData(data);
                });
            }
        }
    }
}
