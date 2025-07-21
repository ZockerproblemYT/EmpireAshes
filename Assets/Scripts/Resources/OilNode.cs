using UnityEngine;

public class OilNode : MonoBehaviour
{
    private Refinery assignedRefinery;

    public void AssignRefinery(Refinery refinery)
    {
        if (assignedRefinery == null)
        {
            assignedRefinery = refinery;
            Debug.Log("🔗 OilNode mit Refinery verknüpft.");
        }
        else
        {
            Debug.LogWarning("⚠️ OilNode ist bereits mit einer Refinery verknüpft!");
        }
    }

    public bool HasRefinery()
    {
        return assignedRefinery != null;
    }

    public Refinery GetRefinery()
    {
        return assignedRefinery;
    }
}
