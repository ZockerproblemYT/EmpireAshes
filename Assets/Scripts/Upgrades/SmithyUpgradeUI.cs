using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simple UI controller for smithy upgrades.
/// Each upgrade path contains three levels.
/// After researching a level the next button becomes visible.
/// Layout example:
/// Weapon column and Armor column side by side,
/// each column showing Level1 -> Level2 -> Level3 vertically.
/// </summary>
public class SmithyUpgradeUI : MonoBehaviour
{
    [System.Serializable]
    public class UpgradeButton
    {
        public Button button;
        public Image progressFill;
        public UnitUpgradeData data;
        public GameObject nextButton;
    }

    [System.Serializable]
    public class UpgradeColumn
    {
        public UpgradeButton level1;
        public UpgradeButton level2;
        public UpgradeButton level3;
    }

    [Header("Weapon Upgrades")]
    public UpgradeColumn weaponColumn;

    [Header("Armor Upgrades")]
    public UpgradeColumn armorColumn;

    [Tooltip("Fraktion für diese Upgrades")]
    public Faction faction;

    void Start()
    {
        if (faction == null)
            faction = MatchManager.Instance?.PlayerFaction;

        InitColumn(weaponColumn);
        InitColumn(armorColumn);
    }

    private void InitColumn(UpgradeColumn col)
    {
        InitButton(col.level1);
        InitButton(col.level2);
        InitButton(col.level3);

        if (col.level1?.button != null)
            col.level1.button.gameObject.SetActive(true);
        if (col.level2?.button != null)
            col.level2.button.gameObject.SetActive(false);
        if (col.level3?.button != null)
            col.level3.button.gameObject.SetActive(false);
    }

    private void InitButton(UpgradeButton u)
    {
        if (u?.button == null) return;
        u.button.onClick.AddListener(() => OnResearch(u));
        if (u.progressFill != null)
            u.progressFill.fillAmount = 0f;
    }

    private void OnResearch(UpgradeButton upgrade)
    {
        if (upgrade == null || upgrade.data == null || faction == null)
            return;

        if (UpgradeManager.Instance != null && UpgradeManager.Instance.IsResearched(faction, upgrade.data))
            return;

        if (!ResourceManager.Instance.HasEnough(faction, upgrade.data.costMetal, upgrade.data.costOil))
        {
            Debug.Log("⛔ Not enough resources for upgrade");
            return;
        }

        StartCoroutine(ResearchRoutine(upgrade));
    }

    private IEnumerator ResearchRoutine(UpgradeButton upgrade)
    {
        ResourceManager.Instance.Spend(faction, upgrade.data.costMetal, upgrade.data.costOil);
        if (upgrade.button != null)
            upgrade.button.interactable = false;

        float timer = 0f;
        float duration = Mathf.Max(0.1f, upgrade.data.researchTime);

        while (timer < duration)
        {
            timer += Time.deltaTime;
            if (upgrade.progressFill != null)
                upgrade.progressFill.fillAmount = timer / duration;
            yield return null;
        }

        if (upgrade.progressFill != null)
            upgrade.progressFill.fillAmount = 1f;

        UpgradeManager.Instance.ApplyUpgrade(faction, upgrade.data);

        if (upgrade.nextButton != null)
            upgrade.nextButton.SetActive(true);
    }
}