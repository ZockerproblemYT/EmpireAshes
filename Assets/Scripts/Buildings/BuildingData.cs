using UnityEngine;

[CreateAssetMenu(menuName = "Buildings/Building Data")]
public class BuildingData : ScriptableObject
{
    [Header("Allgemein")]
    public string buildingName;

    [Tooltip("Kategorie des Gebäudes (z. B. HQ, Barracks, Refinery)")]
    public BuildingType buildingType; // ✅ NEU

    [Tooltip("Das finale, vollgebaute Gebäude-GameObject")]
    public GameObject prefab;

    [Tooltip("Ghost-Vorschau für die Platzierung (transparentes Mesh)")]
    public GameObject ghostPrefab;

    [Tooltip("Baustellen-Variante mit ConstructionSite-Komponente")]
    public GameObject constructionPrefab;

    [Tooltip("Icon zur Darstellung im Bau-Menü")]
    public Sprite icon;

    [Tooltip("Maximale Lebenspunkte des Gebäudes")]
    public int maxHealth = 100;

    [Header("Baukosten")]
    [Tooltip("Metallkosten für den Bau")]
    public int costMetal;

    [Tooltip("Ölkosten für den Bau")]
    public int costOil;

    [Tooltip("Bevölkerungskosten")]
    public int costPopulation;

    [Header("Bauverhalten")]
    [Tooltip("Gesamtdauer (in Sekunden) zum vollständigen Bau")]
    public float buildTime = 10f;

    [Header("Beschreibung")]
    [TextArea(2, 5)]
    [Tooltip("Ingame-Beschreibung")]
    public string description;
}
