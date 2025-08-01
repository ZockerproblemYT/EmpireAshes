using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum TooltipResourceType { Metal, Oil, Population }

public class TooltipSystem : MonoBehaviour
{
    public static TooltipSystem Instance;

    [Header("Referenzen")]
    public RectTransform backgroundRectTransform;  // Das Hauptobjekt (Root) des Tooltips
    public TextMeshProUGUI headerText;
    public TextMeshProUGUI contentText;

    [Header("Sprite Assets")]
    [Tooltip("Sprite Asset für Metall-Icon")] public TMP_SpriteAsset metalSpriteAsset;
    [Tooltip("Sprite Asset für Öl-Icon")] public TMP_SpriteAsset oilSpriteAsset;
    [Tooltip("Sprite Asset für Bevölkerungs-Icon")] public TMP_SpriteAsset populationSpriteAsset;
    public Vector2 mouseOffset = new Vector2(30f, -30f);  // Rechts oberhalb

    private Canvas parentCanvas;
    private CanvasGroup canvasGroup;
    private bool isVisible = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (backgroundRectTransform == null)
            Debug.LogError("❌ TooltipSystem: backgroundRectTransform nicht zugewiesen!");

        if (headerText == null || contentText == null)
            Debug.LogError("❌ TooltipSystem: Textfelder nicht zugewiesen!");

        canvasGroup = backgroundRectTransform.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            Debug.LogWarning("⚠️ TooltipSystem: Keine CanvasGroup – SetActive wird verwendet.");
        }

        parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas == null)
        {
            parentCanvas = FindAnyObjectByType<Canvas>();
            if (parentCanvas != null)
                Debug.LogWarning("⚠️ TooltipSystem: Canvas automatisch zugewiesen.");
            else
                Debug.LogError("❌ TooltipSystem: Kein Canvas gefunden!");
        }

        HideImmediate();
    }

    void Update()
    {
        if (!isVisible || backgroundRectTransform == null || parentCanvas == null)
            return;

        Vector2 mousePosition = Input.mousePosition + (Vector3)mouseOffset;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentCanvas.transform as RectTransform,
            mousePosition,
            parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : parentCanvas.worldCamera,
            out Vector2 localPoint
        );

        backgroundRectTransform.anchoredPosition = localPoint;
    }

    public void Show(string header, string content)
    {
        if (string.IsNullOrEmpty(header) && string.IsNullOrEmpty(content))
        {
            Debug.LogWarning("⚠️ Tooltip Show() mit leerem Text aufgerufen!");
            return;
        }

        if (headerText == null || contentText == null)
        {
            Debug.LogWarning("⚠️ TooltipSystem: Show() Abbruch – fehlende Textreferenzen.");
            return;
        }

        // Sprite Assets zuweisen, sodass alle Ressourcen-Icons genutzt werden können
        TMP_SpriteAsset primaryAsset = metalSpriteAsset ?? oilSpriteAsset ?? populationSpriteAsset;
        if (primaryAsset != null)
        {
            headerText.spriteAsset = primaryAsset;
            contentText.spriteAsset = primaryAsset;

            var fallbacks = new System.Collections.Generic.List<TMP_SpriteAsset>();
            if (primaryAsset != metalSpriteAsset && metalSpriteAsset != null) fallbacks.Add(metalSpriteAsset);
            if (primaryAsset != oilSpriteAsset && oilSpriteAsset != null) fallbacks.Add(oilSpriteAsset);
            if (primaryAsset != populationSpriteAsset && populationSpriteAsset != null) fallbacks.Add(populationSpriteAsset);

            TMP_Settings.defaultSpriteAsset = primaryAsset;

            // Lookup-Tabellen der SpriteAssets aktualisieren, damit Tags korrekt
            // aufgelöst werden können.
            primaryAsset.UpdateLookupTables();
            foreach (var fb in fallbacks)
                fb.UpdateLookupTables();

            // Einige Unity-Versionen besitzen keine public "fallbackSpriteAssets"-
            // Eigenschaft, oder sie ist als Field deklariert. Daher via
            // Reflection nach Property oder Field suchen und, falls vorhanden,
            // die Fallback-Liste setzen.
            var prop = typeof(TMP_Settings).GetProperty("fallbackSpriteAssets");
            if (prop != null)
            {
                prop.SetValue(null, fallbacks);
            }
            else
            {
                var field = typeof(TMP_Settings).GetField("fallbackSpriteAssets",
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Static);
                if (field != null)
                    field.SetValue(null, fallbacks);
            }
        }

        headerText.text = header;
        contentText.text = content;

        LayoutRebuilder.ForceRebuildLayoutImmediate(backgroundRectTransform);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = false;
        }
        else
        {
            backgroundRectTransform.gameObject.SetActive(true);
        }

        isVisible = true;
    }

    public void Hide()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }
        else if (backgroundRectTransform != null)
        {
            backgroundRectTransform.gameObject.SetActive(false);
        }

        isVisible = false;
    }

    public void HideImmediate()
    {
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
        else if (backgroundRectTransform != null)
            backgroundRectTransform.gameObject.SetActive(false);

        isVisible = false;
    }

    /// <summary>
    /// Gibt das TMP-Sprite Tag für die gewünschte Ressource zurück.
    /// </summary>
    public static string GetResourceSpriteTag(TooltipResourceType resource)
    {
        if (Instance == null)
            return string.Empty;

        // SpriteAsset prüfen, um ungültige Tags zu vermeiden
        TMP_SpriteAsset asset = null;
        string resourceName = resource.ToString();
        switch (resource)
        {
            case TooltipResourceType.Metal:
                asset = Instance.metalSpriteAsset;
                break;
            case TooltipResourceType.Oil:
                asset = Instance.oilSpriteAsset;
                break;
            case TooltipResourceType.Population:
                asset = Instance.populationSpriteAsset;
                break;
        }

        if (asset == null)
            return string.Empty;

        // Da der SpriteAsset bereits am Text zugewiesen wird, reicht der Name.
        return $"<sprite name=\"{resourceName}\">";
    }

    /// <summary>
    /// Gibt einen formatierten Kosten-String mit vorangestellten Sprite-Icons zurück.
    /// </summary>
    public static string FormatCostString(int metal, int oil, int population)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        void AppendCost(int amount, TooltipResourceType type)
        {
            if (amount <= 0)
                return;

            if (sb.Length > 0)
                sb.Append("   ");

            sb.Append($"{GetResourceSpriteTag(type)} {amount}");
        }

        AppendCost(metal, TooltipResourceType.Metal);
        AppendCost(oil, TooltipResourceType.Oil);
        AppendCost(population, TooltipResourceType.Population);

        return sb.ToString();
    }
}
