using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UnitInfoUI : MonoBehaviour
{
    public static UnitInfoUI Instance { get; private set; }

    [Header("UI References")]
    public GameObject panel;
    public Image portraitImage;
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI damageText;
    public TextMeshProUGUI armorText;
    public TextMeshProUGUI typeText;
    public TextMeshProUGUI strongAgainstText;

    private UnitSelectionHandler selectionHandler;
    private Unit currentUnit;

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
        selectionHandler = FindAnyObjectByType<UnitSelectionHandler>();
        if (panel != null)
            panel.SetActive(false);
    }

    void Update()
    {
        UpdateHealth();
    }

    private void UpdateHealth()
    {
        if (currentUnit == null)
            return;
        if (currentUnit.TryGetComponent<Health>(out var health))
        {
            if (hpText != null)
                hpText.text = $"{Mathf.CeilToInt(health.Current)}/{Mathf.CeilToInt(health.Max)}";
        }
    }

    public void Refresh()
    {
        if (selectionHandler == null)
            selectionHandler = FindAnyObjectByType<UnitSelectionHandler>();

        List<Unit> units = selectionHandler != null ? selectionHandler.SelectedUnits : null;
        if (units != null && units.Count == 1)
        {
            Show(units[0]);
        }
        else
        {
            Hide();
        }
    }

    private void Show(Unit unit)
    {
        currentUnit = unit;
        if (panel != null && !panel.activeSelf)
            panel.SetActive(true);

        if (unit == null)
            return;

        UnitData data = unit.unitData;
        if (data != null)
        {
            if (portraitImage != null)
                portraitImage.sprite = data.unitIcon;

            if (nameText != null)
                nameText.text = data.unitName;

            if (damageText != null)
                damageText.text = $"Damage: {data.attackDamage}";

            if (armorText != null)
                armorText.text = $"Armor: {data.armor}";

            if (typeText != null)
                typeText.text = $"Type: {data.damageType}";

            if (strongAgainstText != null)
                strongAgainstText.text = GetStrongAgainstText(data.damageType);
        }
        UpdateHealth();
    }

    private void Hide()
    {
        currentUnit = null;
        if (panel != null && panel.activeSelf)
            panel.SetActive(false);
    }

    private string GetStrongAgainstText(DamageType dmg)
    {
        ArmorType bestArmor = ArmorType.Light;
        float best = -1f;
        foreach (ArmorType armor in System.Enum.GetValues(typeof(ArmorType)))
        {
            float mult = GetMultiplier(dmg, armor);
            if (mult > best)
            {
                best = mult;
                bestArmor = armor;
            }
        }
        return $"Strong vs {bestArmor}";
    }

    private float GetMultiplier(DamageType dmg, ArmorType armor)
    {
        switch (dmg)
        {
            case DamageType.Normal:
                return armor == ArmorType.Heavy ? 0.75f : 1f;
            case DamageType.Explosive:
                switch (armor)
                {
                    case ArmorType.Light: return 0.8f;
                    case ArmorType.Medium: return 1f;
                    case ArmorType.Heavy: return 1.5f;
                }
                break;
            case DamageType.ArmorPiercing:
                switch (armor)
                {
                    case ArmorType.Light: return 1.2f;
                    case ArmorType.Medium: return 1.2f;
                    case ArmorType.Heavy: return 1.5f;
                }
                break;
        }
        return 1f;
    }
}
