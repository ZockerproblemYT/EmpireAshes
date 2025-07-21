using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class RankedMenuController : MonoBehaviour
{
    [Header("Modi")]
    public Toggle toggle1v1;
    public Toggle toggle2v2;
    public Toggle toggle3v3;
    public Toggle toggle4v4;

    [Header("Suche")]
    public Button searchButton;
    public TMP_Text searchButtonText;

    private int selectedMode = 0;
    private Coroutine searchRoutine;
    private float searchTime = 0f;

    void Start()
    {
        searchButtonText.text = "Spielsuche";
        searchButton.interactable = false;

        toggle1v1.onValueChanged.AddListener((_) => OnModeChanged());
        toggle2v2.onValueChanged.AddListener((_) => OnModeChanged());
        toggle3v3.onValueChanged.AddListener((_) => OnModeChanged());
        toggle4v4.onValueChanged.AddListener((_) => OnModeChanged());
    }

    public void OnModeChanged()
    {
        if (toggle1v1.isOn) SelectMode(1);
        else if (toggle2v2.isOn) SelectMode(2);
        else if (toggle3v3.isOn) SelectMode(3);
        else if (toggle4v4.isOn) SelectMode(4);
        else
        {
            selectedMode = 0;
            if (searchRoutine == null)
                searchButton.interactable = false;
        }
    }

    public void SelectMode(int mode)
    {
        selectedMode = mode;
        Debug.Log($"🎮 Modus ausgewählt: {mode}v{mode}");
        searchButton.interactable = true;
        searchButtonText.text = "Spielsuche";
    }

    public void OnSearchClicked()
    {
        if (searchRoutine == null)
        {
            Debug.Log("🔍 Spielsuche gestartet...");
            searchRoutine = StartCoroutine(SearchRoutine());
        }
        else
        {
            Debug.Log("⛔ Suche abgebrochen.");
            StopCoroutine(searchRoutine);
            ResetSearch();
        }
    }

    IEnumerator SearchRoutine()
    {
        searchTime = 0f;

        while (true)
        {
            yield return new WaitForSeconds(1f);
            searchTime += 1f;
            searchButtonText.text = $"Abbrechen ({searchTime:00})";

            if (searchTime >= 10f)
            {
                Debug.Log("✅ Match gefunden! → Weiter zur Lobby...");
                searchButtonText.text = "Match gefunden";
                break;
            }
        }

        searchRoutine = null;
        searchButton.interactable = false;

        yield return new WaitForSeconds(1f); // kleine Verzögerung

        MainMenuController menu = FindFirstObjectByType<MainMenuController>();
        if (menu != null)
            menu.ShowRankedLobby();
        else
            Debug.LogWarning("❌ MainMenuController nicht gefunden!");
    }

    private void ResetSearch()
    {
        if (searchRoutine != null)
            StopCoroutine(searchRoutine);

        searchRoutine = null;
        searchButtonText.text = "Spielsuche";
        searchButton.interactable = (selectedMode != 0);
    }
}
