using UnityEngine;

public class Smithy : Building
{
    [SerializeField] private int infantryDamageBonus = 1;
    private bool upgradeApplied = false;

    public void UpgradeInfantry()
    {
        if (upgradeApplied) return;

        var faction = GetOwner();
        if (faction == null) return;

        UnitData infantry = faction.availableUnits.Find(u => u != null && u.unitName == "Infantry");
        if (infantry != null)
        {
            infantry.attackDamage += infantryDamageBonus;
            upgradeApplied = true;
            Debug.Log($"Smithy upgraded Infantry for {faction.factionName}");
        }
    }
}
