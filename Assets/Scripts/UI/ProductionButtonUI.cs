using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Button))]
public class ProductionButtonUI : MonoBehaviour
{
    [Header("UI Referenzen")]
    public Image unitIconImage;
    public Image progressFill;
    public TextMeshProUGUI queueCountText;

    private UnitData unitData;
    private ProductionBuilding building;
    private Button button;

    void Awake()
    {
        // Button-Referenz sicherstellen
        button = GetComponent<Button>();
        if (button == null)
        {
            Debug.LogError("❌ Kein Button-Component vorhanden am ProductionButtonUI-Objekt!", this);
        }

        gameObject.SetActive(false); // Sicherstellen, dass das UI nicht verfrüht angezeigt wird
    }

    void Update()
    {
        if (!gameObject.activeInHierarchy || building == null || unitData == null)
            return;

        if (progressFill == null || queueCountText == null)
        {
            Debug.LogWarning("⚠️ Update: UI-Komponenten fehlen!", this);
            return;
        }

        // Fortschrittsbalken nur bei aktiver Produktion dieser Einheit anzeigen
        if (building.CurrentUnit == unitData)
        {
            progressFill.fillAmount = building.ProductionProgress;
        }
        else
        {
            progressFill.fillAmount = 0f;
        }

        // Produktions-Warteschlange anzeigen
        int count = building.CountQueued(unitData);
        queueCountText.text = count > 1 ? count.ToString() : "";
    }

    public void Setup(UnitData data, ProductionBuilding prodBuilding)
    {
        unitData = data;
        building = prodBuilding;

        if (unitIconImage != null && unitData != null)
            unitIconImage.sprite = unitData.unitIcon;

        if (progressFill != null)
            progressFill.fillAmount = 0f;

        if (queueCountText != null)
            queueCountText.text = "";

        // Event-Handler korrekt zuweisen
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClickProduce);
        }

        gameObject.SetActive(true);
    }

    public void OnClickProduce()
    {
        if (unitData == null)
        {
            Debug.LogWarning("⚠️ Produktion fehlgeschlagen: UnitData fehlt.");
            return;
        }

        var buildings = UnitSelectionHandler.Instance?.SelectedBuildings;
        bool produced = false;

        if (buildings != null && buildings.Count > 1)
        {
            foreach (var b in buildings)
            {
                var pb = b.GetComponent<ProductionBuilding>();
                if (pb != null)
                {
                    pb.EnqueueUnit(unitData);
                    produced = true;
                }
            }
        }

        if (!produced && building != null)
        {
            building.EnqueueUnit(unitData);
            produced = true;
        }

        if (produced)
            Debug.Log($"🔁 Produktion angestoßen für: {unitData.unitName}");
        else
            Debug.LogWarning("⚠️ Produktion fehlgeschlagen: Keine gültigen Gebäude ausgewählt.");
    }
}
