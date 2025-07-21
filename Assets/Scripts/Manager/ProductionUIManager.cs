using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ProductionUIManager : MonoBehaviour
{
    [Header("UI Referenzen")]
    public GameObject productionPanel;               // Das gesamte Panel
    public Transform buttonContainer;                // Hier landen alle Buttons
    public GameObject productionButtonPrefab;        // Prefab für Buttons
    public TextMeshProUGUI infoLabel;                // Anzeige: Gebäudename o.ä.

    private ProductionBuilding currentBuilding;

    void Start()
    {
        if (productionPanel != null)
        {
            productionPanel.SetActive(false); // Panel am Anfang ausblenden
        }

        if (buttonContainer == null || productionButtonPrefab == null)
        {
            Debug.LogError("❌ ProductionUIManager: UI-Referenzen fehlen!");
        }
    }

    public void ShowFor(ProductionBuilding building)
    {
        if (building == null)
        {
            Debug.LogWarning("⚠️ ShowFor: Kein gültiges Gebäude übergeben!");
            return;
        }

        currentBuilding = building;

        // Panel aktivieren
        if (productionPanel != null)
        {
            productionPanel.SetActive(true);
        }

        // Gebäudename anzeigen
        if (infoLabel != null)
        {
            infoLabel.text = building.buildingName;
        }

        ClearButtons();

        List<UnitData> units = building.availableUnits;

        for (int i = 0; i < units.Count; i++)
        {
            UnitData unit = units[i];

            GameObject buttonObj = Instantiate(productionButtonPrefab, buttonContainer);
            buttonObj.SetActive(true); // ❗ Sicherstellen, dass Button aktiv ist

            ProductionButtonUI buttonUI = buttonObj.GetComponent<ProductionButtonUI>();
            if (buttonUI != null)
            {
                buttonUI.Setup(unit, building);
                Debug.Log($"🟢 Erzeuge Button für: {unit.unitName} (Index {i})");
            }
            else
            {
                Debug.LogWarning($"❌ Kein ProductionButtonUI gefunden am ButtonPrefab (Index {i})");
            }

            // TooltipTrigger korrekt verknüpfen
            TooltipTrigger tooltip = buttonObj.GetComponent<TooltipTrigger>();
            if (tooltip != null)
            {
                tooltip.unitData = unit;
                Debug.Log($"🔗 TooltipTrigger verbunden für: {unit.unitName}");
            }
            else
            {
                Debug.LogWarning($"⚠️ Kein TooltipTrigger an ButtonPrefab für: {unit.unitName}");
            }
        }

        Debug.Log($"✅ Insgesamt erzeugte Buttons: {units.Count}");
    }

    public void Hide()
    {
        currentBuilding = null;

        if (productionPanel != null)
        {
            productionPanel.SetActive(false);
        }

        ClearButtons();
    }

    private void ClearButtons()
    {
        foreach (Transform child in buttonContainer)
        {
            Destroy(child.gameObject);
        }
    }
}
