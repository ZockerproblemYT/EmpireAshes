using UnityEngine;
using UnityEngine.UI;

public class BuildingUIController : MonoBehaviour
{
    [Header("Ziel")]
    private Transform targetTransform;
    private Building targetBuilding;
    private BuildingConstructionSite constructionSite;
    private ProductionBuilding productionBuilding;

    [Header("UI-Elemente")]
    [SerializeField] private Image hpBar;
    [SerializeField] private Image buildProgressBar;

    [Header("Offset über Gebäude")]
    [SerializeField] private float defaultYOffset = 1.0f;
    private float dynamicYOffset = 1.0f;

    private CanvasGroup hpCanvasGroup;
    private float lastHpRatio = -1f;
    private string debugBuildingName = "";

    private bool hpVisible = false;

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

    public void SetHPVisible(bool visible)
    {
        hpVisible = visible;
        if (hpCanvasGroup != null)
            hpCanvasGroup.alpha = visible ? 1f : 0f;
    }

    public void Initialize(Transform target, Building building, BuildingConstructionSite site)
    {
        targetTransform = target;
        targetBuilding = building;
        constructionSite = site;

        debugBuildingName = targetBuilding != null ? targetBuilding.name : "NULL";
        Debug.Log($"🎯 UI Initialize() für: {debugBuildingName}");

        productionBuilding = building as ProductionBuilding;

        if (buildProgressBar != null)
        {
            Transform barRoot = buildProgressBar.transform.parent;
            if (barRoot != null)
            {
                bool show = site != null ||
                            (productionBuilding != null && productionBuilding.GetQueueLength() > 0);
                barRoot.gameObject.SetActive(show);
                Debug.Log($"🔧 BuildBar (Parent) sichtbar: {show}");
            }
        }

        // HP-Bar anfangs ausblenden
        if (hpCanvasGroup != null)
            hpCanvasGroup.alpha = hpVisible ? 1f : 0f;

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
                bool isDamaged = targetBuilding != null && targetBuilding.CurrentHealth < targetBuilding.MaxHealth;
                bool shouldShowHP = isUnderConstruction || hpVisible || isDamaged;
                hpCanvasGroup.alpha = shouldShowHP ? 1f : 0f;
            }
        }

        // === Baufortschritt ===
        if (buildProgressBar != null)
        {
            Transform barRoot = buildProgressBar.transform.parent;

            if (constructionSite != null)
            {
                buildProgressBar.fillAmount = constructionSite.GetProgress01();

                if (constructionSite.IsFinished())
                {
                    if (barRoot != null)
                        barRoot.gameObject.SetActive(false);
                    Debug.Log($"✅ Bau abgeschlossen für {debugBuildingName} – BuildBar deaktiviert.");
                    constructionSite = null;
                }
                else if (barRoot != null && !barRoot.gameObject.activeSelf)
                {
                    barRoot.gameObject.SetActive(true);
                }
            }
            else if (productionBuilding != null)
            {
                if (productionBuilding.GetQueueLength() > 0)
                {
                    buildProgressBar.fillAmount = productionBuilding.ProductionProgress;
                    if (barRoot != null && !barRoot.gameObject.activeSelf)
                        barRoot.gameObject.SetActive(true);
                }
                else if (barRoot != null && barRoot.gameObject.activeSelf)
                {
                    barRoot.gameObject.SetActive(false);
                }
            }
        }
    }
}
