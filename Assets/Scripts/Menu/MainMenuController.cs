using UnityEngine;

public class MainMenuController : MonoBehaviour
{
    [Header("Panels")]
    public GameObject campaignPanel;
    public GameObject rankedPanel;
    public GameObject coopPanel;
    public GameObject customPanel;
    public GameObject profilePanel;
    public GameObject friendPanel;

    [Header("UI")]
    public GameObject modeButtons;

    [Header("Match-Lobby")]
    public RankedLobbyController rankedLobby; // ✅ Controller statt nur Panel

    private GameObject currentPanel;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && currentPanel != null)
        {
            currentPanel.SetActive(false);
            currentPanel = null;
            modeButtons.SetActive(true);
        }
    }

    public void OnClickCampaign() => ShowOnly(campaignPanel);
    public void OnClickRanked()   => ShowOnly(rankedPanel);
    public void OnClickCoOp()     => ShowOnly(coopPanel);
    public void OnClickCustom()   => ShowOnly(customPanel);

    public void OnClickProfile()
    {
        profilePanel.SetActive(!profilePanel.activeSelf);
    }

    public void OnClickFriends()
    {
        friendPanel.SetActive(!friendPanel.activeSelf);
    }

    private void ShowOnly(GameObject panelToShow)
    {
        campaignPanel.SetActive(false);
        rankedPanel.SetActive(false);
        coopPanel.SetActive(false);
        customPanel.SetActive(false);

        if (rankedLobby != null)
            rankedLobby.rankedLobbyPanel.SetActive(false); // Panel über Controller deaktivieren

        modeButtons.SetActive(false);

        if (panelToShow != null)
        {
            panelToShow.SetActive(true);
            currentPanel = panelToShow;
        }
    }

    public void ShowRankedLobby() // ✅ nach Match-Fund
    {
        if (currentPanel != null)
            currentPanel.SetActive(false);

        if (rankedLobby != null)
        {
            rankedLobby.ShowLobby(); // direkt die Methode aufrufen
            currentPanel = rankedLobby.rankedLobbyPanel;
        }
        else
        {
            Debug.LogError("❌ RankedLobbyController fehlt im MainMenuController!");
        }

        modeButtons.SetActive(false);
    }
}
