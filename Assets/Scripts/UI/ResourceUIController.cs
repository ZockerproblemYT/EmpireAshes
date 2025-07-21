using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ResourceUIController : MonoBehaviour
{
    [System.Serializable]
    public class ResourceSlot
    {
        public ResourceType type;
        public Image icon;
        public TextMeshProUGUI text;
    }

    [Header("UI-Slots")]
    public ResourceSlot[] slots;

    void Update()
    {
        foreach (var slot in slots)
        {
            int amount = 0;
            string display = "";

            switch (slot.type)
            {
                case ResourceType.Metal:
                    amount = ResourceManager.Instance.GetMetal();
                    display = amount.ToString();
                    break;
                case ResourceType.Oil:
                    amount = ResourceManager.Instance.GetOil();
                    display = amount.ToString();
                    break;
                case ResourceType.Population:
                    display = $"{ResourceManager.Instance.GetPopulation()} / {ResourceManager.Instance.GetMaxPopulation()}";
                    break;
                case ResourceType.Polymer:
                    // Später wenn Plastik verfügbar ist
                    amount = 0; // später ersetzen
                    display = amount.ToString();
                    break;
            }

            slot.text.text = display;
        }
    }
}
