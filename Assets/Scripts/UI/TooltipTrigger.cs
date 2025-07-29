using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI;

[RequireComponent(typeof(EventTrigger))]
public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Tooltip-Inhalt")]
    [TextArea] public string header;
    [TextArea] public string content;

    [Header("Optional: Verknüpfte Einheit")]
    public UnitData unitData;

    [Header("Optional: Verknüpftes Gebäude")]
    public BuildingData buildingData;

    private void Awake()
    {
        if (TooltipSystem.Instance == null)
        {
            Debug.LogWarning($"⚠️ Kein TooltipSystem vorhanden für {gameObject.name}");
        }

        if (GetComponent<CanvasRenderer>() == null && GetComponentInChildren<Image>() == null)
        {
            Debug.LogWarning($"⚠️ {gameObject.name} ist möglicherweise kein UI-Element mit RaycastTarget.");
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (TooltipSystem.Instance == null)
        {
            Debug.LogWarning("⚠️ TooltipSystem.Instance ist null!");
            return;
        }

        if (unitData != null)
        {
            string content = GenerateTooltipContent(unitData);
            Debug.Log($"🟢 TooltipTrigger: Zeige Tooltip für Unit: {unitData.unitName}");
            TooltipSystem.Instance.Show(unitData.unitName, content);
        }
        else if (buildingData != null)
        {
            string content = GenerateTooltipContent(buildingData);
            Debug.Log($"🏢 TooltipTrigger: Zeige Tooltip für Building: {buildingData.buildingName}");
            TooltipSystem.Instance.Show(buildingData.buildingName, content);
        }
        else
        {
            Debug.LogWarning("⚠️ TooltipTrigger: Weder UnitData noch BuildingData gesetzt – Fallback auf Texteingabe.");

            if (string.IsNullOrWhiteSpace(header) && string.IsNullOrWhiteSpace(content))
            {
                Debug.LogWarning("⚠️ TooltipTrigger: Header UND Content leer");
            }

            TooltipSystem.Instance.Show(header, content);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Debug.Log($"🔵 [TooltipTrigger] Pointer EXIT auf: {gameObject.name}");
        TooltipSystem.Instance?.Hide();
    }

    public void SetTooltip(string headerText, string contentText)
    {
        header = headerText;
        content = contentText;
    }

    private string GenerateTooltipContent(UnitData data)
    {
        string cost = TooltipSystem.FormatCostString(data.costMetal, data.costOil, data.costPopulation);
        return $"{cost}\n\n{data.description}";
    }

    private string GenerateTooltipContent(BuildingData data)
    {
        string cost = TooltipSystem.FormatCostString(data.costMetal, data.costOil, data.costPopulation);
        return $"{cost}\n\n{data.description}";
    }
}
