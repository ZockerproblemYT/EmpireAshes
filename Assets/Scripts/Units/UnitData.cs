using UnityEngine;

[CreateAssetMenu(fileName = "UnitData", menuName = "RTS/UnitData")]
public class UnitData : ScriptableObject
{
    [Header("Allgemein")]
    public string unitName;

    [TextArea]
    public string description;

    public Sprite unitIcon;
    public GameObject unitPrefab;

    [Header("Einheitentyp")]
    public UnitRole role;

    [Header("Produktion")]
    [Tooltip("Dauer in Sekunden, um diese Einheit zu produzieren")]
    public float productionTime = 5f;

    public int costMetal;
    public int costOil;
    public int costPopulation;

    [Header("Kampfwerte")]
    [Tooltip("Maximale Lebenspunkte der Einheit")]
    public float maxHealth = 100f;

    [Tooltip("Reichweite der Einheit (in Metern)")]
    public float attackRange = 5f;

    [Tooltip("Schaden pro Schuss/Angriff")]
    public float attackDamage = 10f;

    [Tooltip("Angriffe pro Sekunde")]
    public float attackRate = 1f;

    [Tooltip("Sichtweite (für Auto-Angriff)")]
    public float visionRange = 7f;

    [Tooltip("Kann automatisch Gegner angreifen")]
    public bool isCombatUnit = false;

    [Tooltip("Art des Angriffs: Nahkampf, Fernkampf oder Flächenschaden")]
    public Unit.AttackType attackType = Unit.AttackType.Melee;

    [Tooltip("Art des verursachten Schadens (z. B. gegen Rüstungstypen)")]
    public DamageType damageType = DamageType.Normal;

    [Tooltip("Rüstungsart der Einheit")]
    public ArmorType armor = ArmorType.Medium;

    [Header("Bewegung")]
    public float moveSpeed = 5f;
    public float angularSpeed = 120f;
    public float acceleration = 8f;
    public float stoppingDistance = 0.1f;
}
