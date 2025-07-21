using UnityEngine;
using UnityEngine.UI;

public class ProductionQueueUI : MonoBehaviour
{
    [Header("Referenzen")]
    public ProductionBuilding productionBuilding; // Muss im Inspector zugewiesen sein
    public Text queueLengthText;

    void Update()
    {
        if (productionBuilding == null || queueLengthText == null)
            return;

        int queueLength = productionBuilding.GetQueueLength();
        queueLengthText.text = queueLength > 0 ? queueLength.ToString() : "";
    }
}
