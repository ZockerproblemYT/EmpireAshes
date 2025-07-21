using UnityEngine;
using System.Collections.Generic;

public class MatchManager : MonoBehaviour
{
    public static MatchManager Instance { get; private set; }

    [Header("Fraktionseinstellungen")]
    public Faction startingFaction; // Vom Menü gesetzt
    public Faction PlayerFaction { get; private set; }
    public List<Faction> AllFactions { get; private set; } = new(); // Nur 1 Fraktion vorerst

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
        if (startingFaction != null)
        {
            PlayerFaction = Instantiate(startingFaction);

            // Farbwahl laden
            if (PlayerPrefs.HasKey("PlayerColorR"))
            {
                float r = PlayerPrefs.GetFloat("PlayerColorR");
                float g = PlayerPrefs.GetFloat("PlayerColorG");
                float b = PlayerPrefs.GetFloat("PlayerColorB");

                Color loadedColor = new Color(r, g, b);
                PlayerFaction.factionColor = loadedColor;

                Debug.Log($"🎨 Fraktionsfarbe aus PlayerPrefs geladen: {loadedColor}");
            }
            else
            {
                Debug.Log("ℹ️ Keine gespeicherte Farbe gefunden, Standardfarbe wird verwendet.");
            }

            // Ressourcen setzen
            ResourceManager.Instance.InitializeResources(
                PlayerFaction.startMetal,
                PlayerFaction.startOil,
                PlayerFaction.startPopulation
            );

            // Fraktion in AllFactions einfügen
            AllFactions.Add(PlayerFaction);

            Debug.Log($"✅ MatchManager: Spielerfraktion gesetzt: {PlayerFaction.name}");
        }
        else
        {
            Debug.LogError("❌ MatchManager: Keine Fraktion gesetzt!");
        }
    }
}
