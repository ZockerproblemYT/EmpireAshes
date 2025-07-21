using UnityEngine;
using UnityEngine.UI;

public class ColorDropdownSelector : MonoBehaviour
{
    public Dropdown colorDropdown;

    void Start()
    {
        colorDropdown.onValueChanged.AddListener(OnColorChanged);
        LoadPreviousColor(); // Setzt Dropdown beim Start auf gespeicherte Auswahl
    }

    void OnColorChanged(int index)
    {
        Color selectedColor = GetColorFromIndex(index);

        PlayerPrefs.SetFloat("FactionColor_R", selectedColor.r);
        PlayerPrefs.SetFloat("FactionColor_G", selectedColor.g);
        PlayerPrefs.SetFloat("FactionColor_B", selectedColor.b);
        PlayerPrefs.SetInt("SelectedColorIndex", index);
        PlayerPrefs.Save();

        Debug.Log($"✅ Farbe gespeichert: {selectedColor}");
    }

    void LoadPreviousColor()
    {
        if (PlayerPrefs.HasKey("SelectedColorIndex"))
        {
            int savedIndex = PlayerPrefs.GetInt("SelectedColorIndex");
            colorDropdown.value = savedIndex;
        }
    }

    Color GetColorFromIndex(int index)
    {
        return index switch
        {
            0 => Color.red,
            1 => Color.blue,
            2 => Color.green,
            3 => Color.yellow,
            _ => Color.white
        };
    }
}
