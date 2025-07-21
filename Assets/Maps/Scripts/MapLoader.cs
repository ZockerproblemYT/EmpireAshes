using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MapLoader : MonoBehaviour
{
    [Header("Prefabs (alle erforderlich)")]
    public GameObject hqPrefab;
    public GameObject workerPrefab;
    public GameObject metalNodePrefab;
    public GameObject oilNodePrefab;

    [Header("Startpositionen für HQs")]
    public List<Vector3> startPositions = new();

    [Header("Ressourcen")]
    public List<Vector3> metalNodePositions = new();
    public List<Vector3> oilNodePositions = new();

    private void Start()
    {
        Debug.Log("🧭 MapLoader.Start() aufgerufen");
        StartCoroutine(WaitForMatchManagerAndLoad());
    }

    private IEnumerator WaitForMatchManagerAndLoad()
    {
        // Warte, bis MatchManager vollständig initialisiert ist
        while (MatchManager.Instance == null || MatchManager.Instance.PlayerFaction == null)
        {
            Debug.Log("⏳ Warte auf MatchManager...");
            yield return null;
        }

        Debug.Log("✅ MatchManager bereit, lade Map...");
        LoadMap();
    }

    void LoadMap()
    {
        Debug.Log("🗺️ LoadMap gestartet");

        List<Faction> allFactions = MatchManager.Instance.AllFactions;
        Faction playerFaction = MatchManager.Instance.PlayerFaction;

        if (startPositions.Count == 0)
        {
            Debug.LogError("❌ Keine Startpositionen definiert!");
            return;
        }

        for (int i = 0; i < startPositions.Count && i < allFactions.Count; i++)
        {
            Vector3 spawnPos = startPositions[i];
            Faction faction = allFactions[i];

            // HQ
            GameObject hq = Instantiate(hqPrefab, spawnPos, Quaternion.identity);
            var building = hq.GetComponent<Building>();
            if (building != null) building.SetOwner(faction);

            Debug.Log($"🏠 HQ gespawnt für Fraktion {faction.name} bei {spawnPos}");

            // Worker
            Vector3 workerSpawn = spawnPos + new Vector3(3f, 0f, 0f);
            GameObject worker = Instantiate(workerPrefab, workerSpawn, Quaternion.identity);
            var unit = worker.GetComponent<Unit>();
            if (unit != null) unit.SetOwner(faction);

            Debug.Log($"👷 Worker gespawnt für Fraktion {faction.name} bei {workerSpawn}");

            // Kamera zentrieren
            if (faction == playerFaction)
            {
                Camera.main.transform.position = new Vector3(
                    spawnPos.x,
                    Camera.main.transform.position.y,
                    spawnPos.z - 10f
                );
                Debug.Log($"🎥 Kamera auf Spieler ausgerichtet bei {spawnPos}");
            }
        }

        foreach (var pos in metalNodePositions)
        {
            Instantiate(metalNodePrefab, pos, Quaternion.identity);
            Debug.Log($"🪓 MetalNode gespawnt bei {pos}");
        }

        foreach (var pos in oilNodePositions)
        {
            Instantiate(oilNodePrefab, pos, Quaternion.identity);
            Debug.Log($"🛢️ OilNode gespawnt bei {pos}");
        }

        Debug.Log("✅ Map vollständig geladen.");
    }
}
