public class UpgradeManager : MonoBehaviour
{
    private List<UnitUpgradeData> researchedUpgrades = new();

    public void ApplyUpgrade(UnitUpgradeData upgrade)
    {
        if (!researchedUpgrades.Contains(upgrade))
            researchedUpgrades.Add(upgrade);
    }

    public List<UnitUpgradeData> GetUpgradesForRole(UnitRole role)
    {
        return researchedUpgrades.FindAll(u => u.affectedUnitRole == role);
    }
}
