using UnityEngine;

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }

    private int metal;
    private int oil;
    private int population;
    private int maxPopulation;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void InitializeResources(int startMetal, int startOil, int startPopulation)
    {
        metal = startMetal;
        oil = startOil;
        population = 0; // Startet bei 0 belegter Bevölkerung
        maxPopulation = startPopulation;
    }

    // Ressourcen hinzufügen
    public void AddResources(ResourceType type, int amount)
    {
        switch (type)
        {
            case ResourceType.Metal: metal += amount; break;
            case ResourceType.Oil: oil += amount; break;
            case ResourceType.Population: population = Mathf.Max(0, population - amount); break;
        }
    }

    public void AddResources(int addMetal, int addOil, int reducePopulation = 0)
    {
        metal += addMetal;
        oil += addOil;
        population = Mathf.Max(0, population - reducePopulation);
    }

    // Ressourcenstand abfragen
    public int GetResource(ResourceType type)
    {
        return type switch
        {
            ResourceType.Metal => metal,
            ResourceType.Oil => oil,
            ResourceType.Population => population,
            _ => 0
        };
    }

    public int GetMetal() => metal;
    public int GetOil() => oil;
    public int GetPopulation() => population;
    public int GetMaxPopulation() => maxPopulation;
    public bool IsPopulationFull() => population >= maxPopulation;

    // Prüfung ob genug Ressourcen vorhanden sind
    public bool HasEnough(int costMetal, int costOil, int costPop)
    {
        return metal >= costMetal && oil >= costOil && (population + costPop) <= maxPopulation;
    }

    public bool HasEnough(int costMetal, int costOil)
    {
        return HasEnough(costMetal, costOil, 0);
    }

    // Ressourcen ausgeben
    public void Spend(int costMetal, int costOil, int costPop)
    {
        metal -= costMetal;
        oil -= costOil;
        population += costPop;
    }

    public void Spend(int costMetal, int costOil)
    {
        Spend(costMetal, costOil, 0);
    }

    // Rückerstattung für Bauabbruch
    public void Refund(int refundMetal, int refundOil, int refundPop = 0)
    {
        metal += refundMetal;
        oil += refundOil;
        population = Mathf.Max(0, population - refundPop);
    }

    // Prüfung ob genug Bevölkerungskapazität frei ist
    public bool HasPopulationCapacity(int costPop)
    {
        return (population + costPop) <= maxPopulation;
    }
}
