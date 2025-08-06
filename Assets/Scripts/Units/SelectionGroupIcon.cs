using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SelectionGroupIcon : MonoBehaviour
{
    public Image iconImage;
    public TextMeshProUGUI countText;
    public Button button;

    public void Setup(Sprite icon, int count, Action onClick)
    {
        if (iconImage != null && icon != null)
            iconImage.sprite = icon;

        if (countText != null)
            countText.text = $"x{count}";

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            if (onClick != null)
                button.onClick.AddListener(() => onClick());
        }
    }
}
