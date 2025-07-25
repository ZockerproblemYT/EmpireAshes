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
        var faction = MatchManager.Instance?.PlayerFaction;
        if (faction == null) return;

        foreach (var slot in slots)
        {
            int amount = 0;
            string display = "";

            switch (slot.type)
            {
                case ResourceType.Metal:
                    amount = ResourceManager.Instance.GetMetal(faction);
                    display = amount.ToString();
                    break;
                case ResourceType.Oil:
                    amount = ResourceManager.Instance.GetOil(faction);
                    display = amount.ToString();
                    break;
                case ResourceType.Population:
                    display = $"{ResourceManager.Instance.GetPopulation(faction)} / {ResourceManager.Instance.GetMaxPopulation(faction)}";
                    break;
                case ResourceType.Polymer:
                    // Platzhalter für spätere Ressource
                    amount = 0;
                    display = amount.ToString();
                    break;
            }

            slot.text.text = display;
        }
    }
}
