using UnityEngine;

public class CampaignMenuController : MonoBehaviour
{
    [Header("Fraktions-Dropdowns")]
    public GameObject wehrmachtDropdown;
    public GameObject britainDropdown;
    public GameObject udssrDropdown;

    public void ToggleWehrmacht()
    {
        bool state = !wehrmachtDropdown.activeSelf;
        CloseAll();
        wehrmachtDropdown.SetActive(state);
    }

    public void ToggleBritain()
    {
        bool state = !britainDropdown.activeSelf;
        CloseAll();
        britainDropdown.SetActive(state);
    }

    public void ToggleUdSSR()
    {
        bool state = !udssrDropdown.activeSelf;
        CloseAll();
        udssrDropdown.SetActive(state);
    }

    private void CloseAll()
    {
        wehrmachtDropdown.SetActive(false);
        britainDropdown.SetActive(false);
        udssrDropdown.SetActive(false);
    }

    // 🎮 Kampagnen-Aktionen
    public void StartNewCampaign(string faction)
    {
        Debug.Log($"🆕 Neue Kampagne gestartet mit Fraktion: {faction}");
        // TODO: Lade neues Spiel, wähle Map, etc.
    }

    public void ContinueCampaign(string faction)
    {
        Debug.Log($"▶️ Kampagne fortgesetzt: {faction}");
        // TODO: Lade letzten Speicherstand
    }

    public void LoadCampaign(string faction)
    {
        Debug.Log($"📂 Kampagne laden für: {faction}");
        // TODO: Öffne Savegame-Auswahl
    }
}
