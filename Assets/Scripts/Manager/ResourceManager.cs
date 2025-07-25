using System.Collections.Generic;
using UnityEngine;

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }

    private class ResourceState
    {
        public int metal;
        public int oil;
        public int population;
        public int maxPopulation;
    }

    private Dictionary<Faction, ResourceState> factionResources = new Dictionary<Faction, ResourceState>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    // Alternative Initialisierungsmethode für MatchManager-Kompatibilität
    public void InitializeResources(Faction faction, int metal, int oil, int maxPop)
    {
        if (!factionResources.ContainsKey(faction))
        {
            factionResources[faction] = new ResourceState();
        }

        factionResources[faction].metal = metal;
        factionResources[faction].oil = oil;
        factionResources[faction].population = 0;
        factionResources[faction].maxPopulation = maxPop;
    }

    // Ressourcen abfragen
    public int GetMetal(Faction faction) => factionResources.TryGetValue(faction, out var res) ? res.metal : 0;
    public int GetOil(Faction faction) => factionResources.TryGetValue(faction, out var res) ? res.oil : 0;
    public int GetPopulation(Faction faction) => factionResources.TryGetValue(faction, out var res) ? res.population : 0;
    public int GetMaxPopulation(Faction faction) => factionResources.TryGetValue(faction, out var res) ? res.maxPopulation : 0;
    public bool IsPopulationFull(Faction faction) => GetPopulation(faction) >= GetMaxPopulation(faction);

    // Ressourcen hinzufügen
    public void AddResources(Faction faction, ResourceType type, int amount)
    {
        if (!factionResources.TryGetValue(faction, out var res)) return;

        switch (type)
        {
            case ResourceType.Metal:
                res.metal += amount;
                break;
            case ResourceType.Oil:
                res.oil += amount;
                break;
            case ResourceType.Population:
                res.population = Mathf.Max(0, res.population - amount);
                break;
        }
    }

    public void AddResources(Faction faction, int addMetal, int addOil, int reducePopulation = 0)
    {
        if (!factionResources.TryGetValue(faction, out var res)) return;

        res.metal += addMetal;
        res.oil += addOil;
        res.population = Mathf.Max(0, res.population - reducePopulation);
    }

    // Ressourcen prüfen
    public bool HasEnough(Faction faction, int costMetal, int costOil, int costPop)
    {
        if (!factionResources.TryGetValue(faction, out var res)) return false;

        return res.metal >= costMetal && res.oil >= costOil && (res.population + costPop) <= res.maxPopulation;
    }

    public bool HasEnough(Faction faction, int costMetal, int costOil)
    {
        return HasEnough(faction, costMetal, costOil, 0);
    }

    // Ressourcen ausgeben
    public void Spend(Faction faction, int costMetal, int costOil, int costPop)
    {
        if (!factionResources.TryGetValue(faction, out var res)) return;

        res.metal -= costMetal;
        res.oil -= costOil;
        res.population += costPop;
    }

    public void Spend(Faction faction, int costMetal, int costOil)
    {
        Spend(faction, costMetal, costOil, 0);
    }

    // Rückerstattung (z.B. Bau abgebrochen)
    public void Refund(Faction faction, int refundMetal, int refundOil, int refundPop = 0)
    {
        if (!factionResources.TryGetValue(faction, out var res)) return;

        res.metal += refundMetal;
        res.oil += refundOil;
        res.population = Mathf.Max(0, res.population - refundPop);
    }

    public bool HasPopulationCapacity(Faction faction, int costPop)
    {
        return factionResources.TryGetValue(faction, out var res) && (res.population + costPop) <= res.maxPopulation;
    }
}
