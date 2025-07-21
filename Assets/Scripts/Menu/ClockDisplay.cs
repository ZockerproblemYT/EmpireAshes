using UnityEngine;
using TMPro;
using System;

public class ClockDisplay : MonoBehaviour
{
    public TMP_Text clockText;

    void Update()
    {
        if (clockText != null)
            clockText.text = DateTime.Now.ToString("HH:mm");
    }
}
