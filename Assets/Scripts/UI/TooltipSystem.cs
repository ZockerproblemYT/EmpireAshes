using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TooltipSystem : MonoBehaviour
{
    public static TooltipSystem Instance;

    [Header("Referenzen")]
    public RectTransform backgroundRectTransform;  // Das Hauptobjekt (Root) des Tooltips
    public TextMeshProUGUI headerText;
    public TextMeshProUGUI contentText;
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
}
