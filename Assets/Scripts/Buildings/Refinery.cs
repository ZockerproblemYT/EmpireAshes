using UnityEngine;
using UnityEngine.AI;

public class Refinery : Building
{
    private OilNode assignedNode;

    protected override void Start()
    {
        base.Start();

        // Verknüpfe mit OilNode, nur zur Validierung beim Bau
        Collider[] hits = Physics.OverlapSphere(transform.position, 1f);
        foreach (var hit in hits)
        {
            OilNode node = hit.GetComponent<OilNode>();
            if (node != null)
            {
                assignedNode = node;
                node.AssignRefinery(this);
                Debug.Log("🛢️ Refinery mit OilNode verknüpft.");
                return;
            }
        }

        Debug.LogWarning("❗ Raffinerie steht NICHT auf einem OilNode!");
    }

    /// <summary>
    /// Gibt den nächstgelegenen Punkt auf dem Collider zurück, ähnlich wie bei MetalNode.
    /// </summary>
    public Vector3 GetClosestPoint(Vector3 fromPosition)
    {
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogError("❌ [Refinery] Kein Collider gefunden!");
            return transform.position;
        }

        Vector3 raw = col.ClosestPoint(fromPosition);

        if (NavMesh.SamplePosition(raw, out NavMeshHit hit, 2f, NavMesh.AllAreas))
        {
            Debug.DrawLine(fromPosition + Vector3.up * 0.2f, hit.position + Vector3.up * 0.2f, Color.cyan, 2f);
            return hit.position;
        }

        Debug.LogError("❌ [Refinery] Kein gültiger Punkt auf dem NavMesh gefunden!");
        return transform.position;
    }

    public OilNode GetOilNode()
    {
        return assignedNode;
    }
}
