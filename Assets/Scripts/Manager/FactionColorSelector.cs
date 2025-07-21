using UnityEngine;
using TMPro;

public class FactionColorSelector : MonoBehaviour
{
    [Header("UI")]
    public TMP_Dropdown colorDropdown;

    [Header("Farboptionen (Reihenfolge = Dropdown)")]
    public Color[] availableColors;

    private const string PlayerColorKey = "PlayerFactionColor";

    void Start()
    {
        colorDropdown.onValueChanged.AddListener(OnColorChanged);

        // Dropdown zurücksetzen oder gespeicherte Auswahl laden
        int savedIndex = PlayerPrefs.GetInt(PlayerColorKey, 0);
        colorDropdown.value = savedIndex;
        ApplyColor(savedIndex);
    }

    private void OnColorChanged(int index)
    {
        ApplyColor(index);
        PlayerPrefs.SetInt(PlayerColorKey, index);
        PlayerPrefs.Save();
    }

    private void ApplyColor(int index)
    {
        if (index >= 0 && index < availableColors.Length)
        {
            Color selectedColor = availableColors[index];
            PlayerPrefs.SetFloat("FactionColorR", selectedColor.r);
            PlayerPrefs.SetFloat("FactionColorG", selectedColor.g);
            PlayerPrefs.SetFloat("FactionColorB", selectedColor.b);
        }
    }
}
