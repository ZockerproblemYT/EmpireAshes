using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MapLoader : MonoBehaviour
{
    [Header("Prefabs (alle erforderlich)")]
    public GameObject hqPrefab;
    public GameObject barrackPrefab;
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
        List<Faction> allFactions = MatchManager.Instance.AllFactions;
        Faction playerFaction = MatchManager.Instance.PlayerFaction;
        Faction aiFaction = MatchManager.Instance.AIFaction;

        for (int i = 0; i < startPositions.Count && i < allFactions.Count; i++)
        {
            Vector3 spawnPos = startPositions[i];
            Faction faction = allFactions[i];

            // HQ spawnen
            GameObject hq = Instantiate(hqPrefab, spawnPos, Quaternion.identity);
            foreach (var bld in hq.GetComponentsInChildren<Building>())
            {
                bld.SetOwner(faction);
            }

            // Worker spawnen
            Vector3 workerSpawn;
            Collider hqCollider = hq.GetComponent<Collider>();
            if (hqCollider != null)
            {
                Vector3 target = hq.transform.position + hq.transform.forward * 10f;
                workerSpawn = hqCollider.ClosestPoint(target);
            }
            else
            {
                workerSpawn = spawnPos + hq.transform.forward * 3f;
            }
            GameObject workerGO = Instantiate(workerPrefab, workerSpawn, Quaternion.identity);
            var unit = workerGO.GetComponent<Unit>();
            if (unit != null) unit.SetOwner(faction);

            // Kamera zentrieren auf Spieler
            if (faction == playerFaction)
            {
                Camera.main.transform.position = new Vector3(
                    spawnPos.x,
                    Camera.main.transform.position.y,
                    spawnPos.z - 10f
                );
            }

            // KI initialisieren
            if (faction == aiFaction)
            {
                GameObject barrack = null;

                SimpleAI ai = FindFirstObjectByType<SimpleAI>();
                if (ai != null)
                {
                    ProductionBuilding hqProd = hq.GetComponent<ProductionBuilding>();
                    ProductionBuilding barrackProd = barrack?.GetComponent<ProductionBuilding>();

                    UnitData workerUnitData = null;
                    UnitData combatUnitData = null;

                    if (faction.availableUnits != null)
                    {
                        foreach (var ud in faction.availableUnits)
                        {
                            if (ud.role == UnitRole.Worker)
                                workerUnitData = ud;
                            else if (ud.role == UnitRole.Combat && combatUnitData == null)
                                combatUnitData = ud;
                        }
                    }

                    ai.Initialize(faction, hqProd, barrackProd, workerUnitData, combatUnitData);
                }
            }
        }

        // Ressourcen platzieren
        foreach (var pos in metalNodePositions)
        {
            Instantiate(metalNodePrefab, pos, Quaternion.identity);
        }

        foreach (var pos in oilNodePositions)
        {
            Instantiate(oilNodePrefab, pos, Quaternion.identity);
        }

        Debug.Log("✅ Map vollständig geladen.");
    }
}
