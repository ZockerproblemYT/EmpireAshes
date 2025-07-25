using UnityEngine;

public class PatrolCommandHandler : MonoBehaviour
{
    private Unit selectedUnit;
    private Vector3? pointA = null;
    private bool awaitingPatrolPoints = false;

    void Update()
    {
        // Taste P gedrückt → Starte Patrouillenmodus
        if (Input.GetKeyDown(KeyCode.P))
        {
            selectedUnit = FindSelectedUnit();
            if (selectedUnit != null)
            {
                awaitingPatrolPoints = true;
                pointA = null;
                Debug.Log("🅿️ Patrouillenmodus gestartet. Klicke zwei Punkte auf dem Boden.");
            }
        }

        // Wenn wir Punkte erwarten, auf Maus klicken
        if (awaitingPatrolPoints && Input.GetMouseButtonDown(1)) // Rechte Maustaste
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, LayerMask.GetMask("Ground")))
            {
                if (pointA == null)
                {
                    pointA = hit.point;
                    Debug.Log("📍 Punkt A gesetzt: " + pointA.Value);
                }
                else
                {
                    Vector3 pointB = hit.point;
                    Debug.Log("📍 Punkt B gesetzt: " + pointB + " → Patrouille startet.");
                    selectedUnit.StartPatrol(pointA.Value, pointB);
                    awaitingPatrolPoints = false;
                    pointA = null;
                }
            }
        }
    }

    private Unit FindSelectedUnit()
    {
        Unit[] all = FindObjectsByType<Unit>(FindObjectsSortMode.None);
        foreach (var u in all)
        {
            if (u.IsSelected && u.role == UnitRole.Combat)
                return u;
        }
        Debug.LogWarning("⚠️ Keine Kampfeinheit ausgewählt.");
        return null;
    }
}
