using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class RankedLobbyController : MonoBehaviour
{
    [Header("UI References")]
    public GameObject rankedLobbyPanel;
    public Transform playerSlotsContainer;
    public GameObject playerSlotPrefab;

    public TMP_Text countdownText;
    public TMP_Text mapNameText;
    public TMP_Text mapDescriptionText;

    [Header("Ladebildschirm")]
    public GameObject loadingScreenPanel;
    public TMP_Text loadingText;

    [Header("Map Info")]
    public string mapName = "Urban Conflict";
    public string mapDescription = "Eine zerstörte Stadt mit engen Gassen und strategischen Punkten.";

    [Header("Spieler-Simulation")]
    public int totalPlayers = 4;

    private List<GameObject> spawnedSlots = new List<GameObject>();
    private int countdown = 20;

    void Start()
    {
        rankedLobbyPanel.SetActive(false);
        if (loadingScreenPanel != null)
            loadingScreenPanel.SetActive(false);
    }

    public void ShowLobby()
    {
        rankedLobbyPanel.SetActive(true);
        ClearSlots();
        SpawnPlayers();
        ShowMapInfo();
        StartCoroutine(CountdownRoutine());
    }

    void ClearSlots()
    {
        foreach (var slot in spawnedSlots)
        {
            Destroy(slot);
        }
        spawnedSlots.Clear();
    }

    void SpawnPlayers()
    {
        for (int i = 0; i < totalPlayers; i++)
        {
            GameObject newSlot = Instantiate(playerSlotPrefab, playerSlotsContainer);
            spawnedSlots.Add(newSlot);

            string teamName = (i % 2 == 0) ? "Team 1" : "Team 2";

            TMP_Text teamLabel = newSlot.transform.Find("TeamLabel")?.GetComponent<TMP_Text>();
            if (teamLabel != null) teamLabel.text = teamName;

            TMP_Text nameText = newSlot.transform.Find("ProfileContainer/PlayerName")?.GetComponent<TMP_Text>();
            if (nameText != null) nameText.text = "Spieler " + (i + 1);

            TMP_Dropdown factionDropdown = newSlot.transform.Find("FactionDropdown")?.GetComponent<TMP_Dropdown>();
            TMP_Dropdown colorDropdown = newSlot.transform.Find("ColorDropdown")?.GetComponent<TMP_Dropdown>();

            if (factionDropdown != null) SetupFactionDropdown(factionDropdown);
            if (colorDropdown != null) SetupColorDropdown(colorDropdown);
        }

        Debug.Log($"✅ {totalPlayers} Spieler-Slots erzeugt.");
    }

    void SetupFactionDropdown(TMP_Dropdown dropdown)
    {
        dropdown.ClearOptions();
        dropdown.AddOptions(new List<string> { "Wehrmacht", "Großbritannien", "UdSSR" });
        dropdown.value = 0;
    }

    void SetupColorDropdown(TMP_Dropdown dropdown)
    {
        dropdown.ClearOptions();

        var colors = new List<Color> { Color.red, Color.blue, Color.green, Color.yellow };
        var options = new List<TMP_Dropdown.OptionData>();

        for (int i = 0; i < colors.Count; i++)
            options.Add(new TMP_Dropdown.OptionData("●"));

        dropdown.options = options;
        dropdown.value = 0;

        if (dropdown.captionText != null)
        {
            dropdown.captionText.text = "●";
            dropdown.captionText.color = colors[dropdown.value];
        }

        StartCoroutine(ApplyDropdownColors(dropdown, colors.ToArray()));
    }

    IEnumerator ApplyDropdownColors(TMP_Dropdown dropdown, Color[] colors)
    {
        yield return null;

        if (dropdown.captionText != null && dropdown.value < colors.Length)
        {
            dropdown.captionText.text = "●";
            dropdown.captionText.color = colors[dropdown.value];
        }
    }

    void ShowMapInfo()
    {
        if (mapNameText != null)
            mapNameText.text = mapName;
        if (mapDescriptionText != null)
            mapDescriptionText.text = mapDescription;
    }

    IEnumerator CountdownRoutine()
    {
        countdown = 20;

        while (countdown > 0)
        {
            if (countdownText != null)
                countdownText.text = $"Match startet in {countdown}s";
            yield return new WaitForSeconds(1f);
            countdown--;
        }

        if (countdownText != null)
            countdownText.text = "Spiel startet...";
        Debug.Log("🚀 Spielstart jetzt!");

        yield return new WaitForSeconds(1f);

        StartCoroutine(ShowLoadingScreen());
    }

    IEnumerator ShowLoadingScreen()
    {
        rankedLobbyPanel.SetActive(false);
        if (loadingScreenPanel != null)
        {
            loadingScreenPanel.SetActive(true);
            if (loadingText != null) loadingText.text = "Lade Karte…";
            yield return new WaitForSeconds(1f);

            if (loadingText != null) loadingText.text = "Initialisiere Spieler…";
            yield return new WaitForSeconds(1f);

            if (loadingText != null) loadingText.text = "Starte Spiel…";
            yield return new WaitForSeconds(1f);
        }

        SceneManager.LoadScene("GameScene"); // Passe den Szenennamen an
    }
}
