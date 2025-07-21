using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    [SerializeField] private Image fillImage;

    private Unit unit;
    private Health health;
    private CanvasGroup canvasGroup;

    public void Initialize(Unit attachedUnit, Health attachedHealth)
    {
        unit = attachedUnit;
        health = attachedHealth;

        canvasGroup = GetComponentInChildren<CanvasGroup>();
        if (canvasGroup == null)
            Debug.LogWarning("[HealthBarUI] ❌ Kein CanvasGroup gefunden!");
        else
            Debug.Log("[HealthBarUI] ✅ CanvasGroup gefunden.");

        if (health != null)
        {
            health.OnHealthChanged.AddListener(UpdateBar);
            health.OnDeath.AddListener(HandleDeath);

            if (health.Max > 0f)
                UpdateBar(health.Current, health.Max);
            else
                Debug.LogWarning("[HealthBarUI] ⚠️ Health.Max ist 0 – UpdateBar übersprungen");
        }

        UpdateVisibility();
    }

    public void UpdateBar(float current, float max)
    {
        if (fillImage == null || unit == null || health == null || max <= 0f) return;

        float ratio = Mathf.Clamp01(current / max);
        fillImage.fillAmount = ratio;

        Debug.Log($"[HealthBarUI] 🔁 UpdateBar: {current}/{max}, isSelected={unit.IsSelected}");

        UpdateVisibility();
    }

    private void UpdateVisibility()
    {
        if (canvasGroup == null || unit == null || health == null) return;

        bool visible = unit.IsSelected || health.Current < health.Max;
        canvasGroup.alpha = visible ? 1f : 0f;

        Debug.Log($"[HealthBarUI] 👁 Sichtbarkeit: {(visible ? "An" : "Aus")}, isSelected={unit.IsSelected}, currentHP={health.Current}");
    }

    private void HandleDeath()
    {
        Destroy(transform.parent.gameObject); // Wrapper + Canvas entfernen
    }

    private void LateUpdate()
{
    // Leerer Block: Verhindert automatische Rotation
}

}
