using UnityEngine;
using UnityEngine.UI;

public class BuildingUIController : MonoBehaviour
{
    [Header("Ziel")]
    private Transform targetTransform;
    private Building targetBuilding;
    private BuildingConstructionSite constructionSite;

    [Header("UI-Elemente")]
    [SerializeField] private Image hpBar;
    [SerializeField] private Image buildProgressBar;

    [Header("Offset über Gebäude")]
    [SerializeField] private float defaultYOffset = 1.0f;
    private float dynamicYOffset = 1.0f;

    private CanvasGroup hpCanvasGroup;
    private float lastHpRatio = -1f;
    private string debugBuildingName = "";

    void Awake()
    {
        if (hpBar != null)
        {
            hpCanvasGroup = hpBar.transform.parent.GetComponent<CanvasGroup>();
            if (hpCanvasGroup != null)
                Debug.Log("✅ CanvasGroup für HP-Bar gefunden.");
            else
                Debug.LogWarning("❌ CanvasGroup für HP-Bar NICHT gefunden!");
        }

        if (buildProgressBar == null)
        {
            Debug.LogWarning("❌ BuildProgressBar ist nicht zugewiesen!");
        }
    }

    public void Initialize(Transform target, Building building, BuildingConstructionSite site)
    {
        targetTransform = target;
        targetBuilding = building;
        constructionSite = site;

        debugBuildingName = targetBuilding != null ? targetBuilding.name : "NULL";
        Debug.Log($"🎯 UI Initialize() für: {debugBuildingName}");

        // BuildBar aktivieren, wenn site != null
        if (buildProgressBar != null)
        {
            Transform barRoot = buildProgressBar.transform.parent;
            if (barRoot != null)
            {
                bool show = site != null;
                barRoot.gameObject.SetActive(show);
                Debug.Log($"🔧 BuildBar (Parent) sichtbar: {show}");
            }
        }

        // HP-Bar sichtbar machen
        if (hpCanvasGroup != null)
            hpCanvasGroup.alpha = 1f;

        // Y-Offset berechnen
        float colliderHeight = 0f;
        if (targetTransform != null)
        {
            Collider col = targetTransform.GetComponentInChildren<Collider>();
            if (col != null)
                colliderHeight = col.bounds.size.y;
        }

        dynamicYOffset = colliderHeight > 0 ? colliderHeight + 0.5f : defaultYOffset;
    }

    void LateUpdate()
    {
        if (targetTransform == null)
        {
            Destroy(gameObject);
            return;
        }

        transform.position = targetTransform.position + Vector3.up * dynamicYOffset;

        // === HP-Logik ===
        if (targetBuilding != null && hpBar != null && targetBuilding.MaxHealth > 0)
        {
            float ratio = (float)targetBuilding.CurrentHealth / targetBuilding.MaxHealth;
            ratio = Mathf.Clamp01(ratio);

            if (!Mathf.Approximately(ratio, lastHpRatio))
            {
                lastHpRatio = ratio;
                hpBar.fillAmount = ratio;
            }

            if (hpCanvasGroup != null)
            {
                bool isUnderConstruction = constructionSite != null;
                bool shouldShowHP = isUnderConstruction || targetBuilding.IsSelected;
                hpCanvasGroup.alpha = shouldShowHP ? 1f : 0f;
            }
        }

        // === Baufortschritt ===
        if (constructionSite != null && buildProgressBar != null)
        {
            buildProgressBar.fillAmount = constructionSite.GetProgress01();

            if (constructionSite.IsFinished())
            {
                // Fortschrittsbalken ausblenden
                Transform barRoot = buildProgressBar.transform.parent;
                if (barRoot != null)
                {
                    barRoot.gameObject.SetActive(false);
                    Debug.Log($"✅ Bau abgeschlossen für {debugBuildingName} – BuildBar deaktiviert.");
                }

                constructionSite = null;
            }
        }
    }
}
