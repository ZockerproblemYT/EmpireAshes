using System.Collections.Generic;
using UnityEngine;

public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance { get; private set; }

    private readonly Dictionary<Faction, List<UnitUpgradeData>> factionUpgrades = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public bool IsResearched(Faction faction, UnitUpgradeData upgrade)
    {
        if (faction == null || upgrade == null) return false;
        return factionUpgrades.TryGetValue(faction, out var list) && list.Contains(upgrade);
    }

    public void ApplyUpgrade(Faction faction, UnitUpgradeData upgrade)
    {
        if (faction == null || upgrade == null) return;

        if (!factionUpgrades.TryGetValue(faction, out var list))
        {
            list = new List<UnitUpgradeData>();
            factionUpgrades[faction] = list;
        }

        if (list.Contains(upgrade)) return;

        list.Add(upgrade);

        foreach (var unit in FindObjectsByType<Unit>(FindObjectsSortMode.None))
        {
            if (unit != null && unit.GetOwnerFaction() == faction && unit.unitData.role == upgrade.affectedUnitRole)
            {
                unit.unitData.attackDamage += upgrade.bonusDamage;
                unit.unitData.maxHealth += upgrade.bonusHealth;
            }
        }
    }

    public List<UnitUpgradeData> GetUpgradesForRole(Faction faction, UnitRole role)
    {
        if (factionUpgrades.TryGetValue(faction, out var list))
            return list.FindAll(u => u.affectedUnitRole == role);

        return new List<UnitUpgradeData>();
    }
}
