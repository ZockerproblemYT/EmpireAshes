using UnityEngine;

public class Bunker : Building
{
    [SerializeField] private float infantryDamageMultiplier = 0.5f;

    public override void TakeDamage(int amount, Unit attacker = null)
    {
        if (attacker != null && attacker.unitData != null && attacker.unitData.unitName == "Infantry")
        {
            amount = Mathf.RoundToInt(amount * infantryDamageMultiplier);
        }
        base.TakeDamage(amount, attacker);
    }
}
