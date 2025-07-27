using UnityEngine;

[CreateAssetMenu(menuName = "RTS/Unit Upgrade")]
public class UnitUpgradeData : ScriptableObject
{
    public string upgradeName;
    [TextArea] public string description;
    public Sprite icon;

    public int bonusDamage = 0;
    public int bonusArmor = 0;
    public int bonusHealth = 0;

    public DamageType bonusDamageType;
    public int bonusDamageAgainstType = 0;

    public float researchTime = 5f;
    public int costMetal = 100;
    public int costOil = 50;

    public UnitRole affectedUnitRole;
}
