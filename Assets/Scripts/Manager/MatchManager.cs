using UnityEngine;
using System.Collections.Generic;

public class MatchManager : MonoBehaviour
{
    public static MatchManager Instance { get; private set; }

    [Header("Fraktionseinstellungen")]
    [Tooltip("Fraktion des Spielers (wird instanziiert)")]
    public Faction startingFaction;

    [Tooltip("Fraktion der KI (wird separat instanziiert)")]
    public Faction aiFaction;

    public Faction PlayerFaction { get; private set; }
    public Faction AIFaction { get; private set; }
    public List<Faction> AllFactions { get; private set; } = new List<Faction>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        if (startingFaction == null)
        {
            Debug.LogError("❌ MatchManager: Keine Spielerfraktion gesetzt!");
            return;
        }

        // Spielerfraktion instanziieren
        PlayerFaction = Instantiate(startingFaction);
        AllFactions.Add(PlayerFaction);

        // Farbwahl des Spielers laden (falls vorhanden)
        if (PlayerPrefs.HasKey("PlayerColorR"))
        {
            float r = PlayerPrefs.GetFloat("PlayerColorR");
            float g = PlayerPrefs.GetFloat("PlayerColorG");
            float b = PlayerPrefs.GetFloat("PlayerColorB");

            PlayerFaction.factionColor = new Color(r, g, b);
            Debug.Log($"🎨 Spielerfarbe aus PlayerPrefs geladen: {PlayerFaction.factionColor}");
        }

        // Ressourcen für Spieler setzen
        ResourceManager.Instance.InitializeResources(
            PlayerFaction,
            PlayerFaction.startMetal,
            PlayerFaction.startOil,
            PlayerFaction.startPopulation
        );

        Debug.Log($"✅ Spielerfraktion instanziiert: {PlayerFaction.name}");

        // KI-Fraktion instanziieren (wenn angegeben)
        if (aiFaction != null)
        {
            AIFaction = Instantiate(aiFaction);
            AllFactions.Add(AIFaction);

            // Farbänderung, falls gleich
            if (AIFaction.factionColor == PlayerFaction.factionColor)
            {
                AIFaction.factionColor = Color.red;
                Debug.Log("🔴 KI-Farbe wurde zur besseren Unterscheidung auf Rot gesetzt.");
            }

            // Ressourcen für KI setzen
            ResourceManager.Instance.InitializeResources(
                AIFaction,
                AIFaction.startMetal,
                AIFaction.startOil,
                AIFaction.startPopulation
            );

            Debug.Log($"🤖 KI-Fraktion instanziiert: {AIFaction.name}");
        }
        else
        {
            Debug.LogWarning("⚠️ Keine KI-Fraktion zugewiesen.");
        }
    }
}
