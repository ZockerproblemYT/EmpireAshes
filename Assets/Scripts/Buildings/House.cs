using UnityEngine;

public class House : Building
{
    [SerializeField] private int populationBonus = 5;

    protected override void Start()
    {
        base.Start();
        if (!IsUnderConstruction)
            ApplyBonus();
    }

    private void ApplyBonus()
    {
        var faction = GetOwner();
        if (faction != null)
            ResourceManager.Instance?.IncreaseMaxPopulation(faction, populationBonus);
    }
}
