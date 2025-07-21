using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Faction", menuName = "RTS/Faction")]
public class Faction : ScriptableObject
{
    [Header("Verfügbare Gebäude")]
public List<BuildingData> availableBuildings;

    public string factionName;
    public Color factionColor;

    [Header("Startressourcen")]
    public int startMetal = 500;
    public int startOil = 300;
    public int startPopulation = 10;

    [Header("Verfügbare Einheiten")]
    public List<UnitData> availableUnits;
}
