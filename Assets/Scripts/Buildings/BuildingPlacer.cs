using UnityEngine;
using UnityEngine.EventSystems;

public class BuildingPlacer : MonoBehaviour
{
    public static BuildingPlacer Instance { get; private set; }

    [Header("Platzierung")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private float gridSize = 1f;

    [Header("Materialien")]
    [SerializeField] private Material validMaterial;
    [SerializeField] private Material invalidMaterial;

    private GameObject currentGhost;
    private BuildingData currentData;
    private Faction playerFaction;
    private bool isPlacing = false;
    private bool justPlaced = false;

    public bool IsPlacing => isPlacing;

    /// <summary>
    /// Returns true if a building has been placed since the last check.
    /// </summary>
    public bool ConsumeJustPlaced()
    {
        bool placed = justPlaced;
        justPlaced = false;
        return placed;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        if (!isPlacing || currentGhost == null) return;

        UpdateGhostPosition();

        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
        {
            Debug.Log("🖱️ Linksklick erkannt – prüfe Platzierung…");

            if (!IsPlacementValid(currentGhost.transform.position))
            {
                Debug.LogWarning("❌ Ungültige Platzierung – Abbruch.");
                return;
            }

            if (!ResourceManager.Instance.HasEnough(
                playerFaction,
                currentData.costMetal,
                currentData.costOil,
                currentData.costPopulation))
            {
                Debug.LogWarning("❌ Nicht genug Ressourcen für dieses Gebäude.");
                return;
            }

            PlaceBuilding();
        }

        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
        {
            Debug.Log("❌ Platzierung abgebrochen.");
            CancelPlacement();
        }
    }

    private void UpdateGhostPosition()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Debug.DrawRay(ray.origin, ray.direction * 100f, Color.green);

        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, groundLayer))
        {
            Vector3 snapped = GetSnappedPosition(hit.point);
            currentGhost.transform.position = snapped;

            UpdateGhostMaterial(snapped);
        }
        else
        {
            Debug.LogWarning("⚠️ Raycast hat keinen Ground getroffen!");
        }
    }

    public void StartPlacing(BuildingData data, Faction faction)
    {
        Debug.Log($"🏗️ Starte Bauplatzierung für: {data.buildingName}");

        CancelPlacement();

        currentData = data;
        playerFaction = faction;
        isPlacing = true;

        currentGhost = Instantiate(data.ghostPrefab);
        currentGhost.name = $"Ghost_{data.buildingName}";

        foreach (var col in currentGhost.GetComponentsInChildren<Collider>())
        {
            col.enabled = false;
        }
    }

    private void PlaceBuilding()
    {
        Vector3 finalPos = currentGhost.transform.position;
        Quaternion rot = currentGhost.transform.rotation;

        Debug.Log($"✅ Platziere Gebäude bei: {finalPos}");

        GameObject siteObj = Instantiate(currentData.constructionPrefab, finalPos, rot);
        BuildingConstructionSite site = siteObj.GetComponent<BuildingConstructionSite>();

        if (site == null)
        {
            Debug.LogError("❌ constructionPrefab hat kein BuildingConstructionSite-Script!");
            return;
        }

        site.Initialize(currentData, playerFaction);
        Debug.Log("🏗️ Baustelle initialisiert.");

        ResourceManager.Instance.Spend(
            playerFaction,
            currentData.costMetal,
            currentData.costOil,
            currentData.costPopulation);

        bool shift = Input.GetKey(KeyCode.LeftShift);

        foreach (var unit in UnitSelectionHandler.Instance.SelectedUnits)
        {
            if (unit.role == UnitRole.Worker)
            {
                if (shift)
                    unit.QueueConstruction(site);
                else
                    unit.AssignToConstruction(site);

                Debug.Log($"👷 Worker zugewiesen: {unit.name} | Queue: {shift}");
            }
        }

        justPlaced = true;

        if (shift)
        {
            StartPlacing(currentData, playerFaction);
        }
        else
        {
            CancelPlacement();
        }
    }

    private void CancelPlacement()
    {
        if (currentGhost != null)
            Destroy(currentGhost);

        currentGhost = null;
        currentData = null;
        isPlacing = false;
    }

    private Vector3 GetSnappedPosition(Vector3 raw)
    {
        return new Vector3(
            Mathf.Round(raw.x / gridSize) * gridSize,
            raw.y,
            Mathf.Round(raw.z / gridSize) * gridSize
        );
    }

    private void UpdateGhostMaterial(Vector3 position)
    {
        bool isValid = IsPlacementValid(position);
        Material mat = isValid ? validMaterial : invalidMaterial;

        foreach (var renderer in currentGhost.GetComponentsInChildren<MeshRenderer>())
        {
            renderer.material = mat;
        }
    }

    private bool IsPlacementValid(Vector3 position)
    {
        if (!ResourceManager.Instance.HasEnough(
            playerFaction,
            currentData.costMetal,
            currentData.costOil,
            currentData.costPopulation))
        {
            return false;
        }

        Vector3 halfExtents = currentGhost.GetComponentInChildren<Renderer>().bounds.extents;
        Collider[] colliders = Physics.OverlapBox(position, halfExtents, Quaternion.identity, obstacleLayer);

        foreach (var col in colliders)
        {
            if (col.GetComponent<OilNode>() != null || col.GetComponent<MetalNode>() != null)
                continue;

            if (!col.isTrigger && col.gameObject != currentGhost)
                return false;
        }

        bool isOverOil = false;
        bool isOverMetal = false;

        Collider[] hits = Physics.OverlapSphere(position, 1.5f);
        foreach (var hit in hits)
        {
            if (hit.GetComponent<OilNode>() != null)
                isOverOil = true;
            if (hit.GetComponent<MetalNode>() != null)
                isOverMetal = true;
        }

        switch (currentData.buildingType)
        {
            case BuildingType.Refinery:
                if (!isOverOil)
                    return false;
                break;

            default:
                if (isOverOil || isOverMetal)
                    return false;
                break;
        }

        return true;
    }
}
