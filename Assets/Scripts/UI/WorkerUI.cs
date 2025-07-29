using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WorkerUI : MonoBehaviour
{
    [Header("UI Elemente")]
    public GameObject panel;
    public Transform buttonContainer;
    public GameObject buildingButtonPrefab;

    private UnitSelectionHandler selectionHandler;
    private Faction playerFaction;

    void Start()
    {
        selectionHandler = FindAnyObjectByType<UnitSelectionHandler>();
        if (selectionHandler == null)
            Debug.LogError("❌ Kein UnitSelectionHandler gefunden!");

        TrySetPlayerFaction();
    }

    void Update()
    {
        if (selectionHandler == null) return;

        if (playerFaction == null)
            TrySetPlayerFaction();

        if (playerFaction == null) return;

        EvaluateSelection();
    }

    private void EvaluateSelection()
    {
        bool hasWorker = false;

        var selected = selectionHandler.SelectedUnits;
        foreach (Unit unit in selected)
        {
            if (unit.role == UnitRole.Worker)
            {
                hasWorker = true;
                break;
            }
        }

        if (panel.activeSelf != hasWorker)
        {
            panel.SetActive(hasWorker);

            if (hasWorker)
            {
                RefreshButtons();
            }
            else
            {
                ClearButtons();
            }
        }
    }

    public void Refresh()
    {
        EvaluateSelection();
    }

    void TrySetPlayerFaction()
    {
        if (MatchManager.Instance != null && MatchManager.Instance.PlayerFaction != null)
        {
            playerFaction = MatchManager.Instance.PlayerFaction;
            Debug.Log("✅ Spielerfraktion geladen: " + playerFaction.factionName);
        }
        else
        {
            Debug.LogWarning("⏳ Spielerfraktion noch nicht verfügbar.");
        }
    }

    void RefreshButtons()
    {
        ClearButtons();

        if (playerFaction.availableBuildings == null)
        {
            Debug.LogWarning("⚠️ Keine Gebäude in der Fraktion vorhanden.");
            return;
        }

        int index = 0;
        foreach (BuildingData building in playerFaction.availableBuildings)
        {
            if (building == null) continue;

            GameObject btn = Instantiate(buildingButtonPrefab, buttonContainer);
            btn.SetActive(true); // 🔧 wichtig!
            btn.name = $"BuildingButton_{index++}_{building.buildingName}";

            // 🧱 Icon setzen
            Transform iconTransform = btn.transform.Find("Icon");
            if (iconTransform != null)
            {
                Image iconImage = iconTransform.GetComponent<Image>();
                if (iconImage != null && building.icon != null)
                {
                    iconImage.sprite = building.icon;
                }
            }

            // 🧠 TooltipTrigger
            TooltipTrigger trigger = btn.GetComponent<TooltipTrigger>();
            if (trigger != null)
            {
                trigger.header = building.buildingName;
                string cost = TooltipSystem.FormatCostString(building.costMetal, building.costOil, building.costPopulation);
                trigger.content = $"{cost}\n\n{building.description}";
            }

            // 🖱 Klick-Funktion
            Button button = btn.GetComponent<Button>();
            if (button != null)
            {
                BuildingData localBuilding = building;
                button.onClick.AddListener(() =>
                {
                    BuildingPlacer.Instance.StartPlacing(localBuilding, playerFaction);
                });
            }
        }
    }

    void ClearButtons()
    {
        foreach (Transform child in buttonContainer)
        {
            Destroy(child.gameObject);
        }
    }
}
