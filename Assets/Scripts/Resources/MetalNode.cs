using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Collider))]
public class MetalNode : MonoBehaviour
{
    private Collider col;

    void Awake()
    {
        col = GetComponent<Collider>();
    }

    public Vector3 GetClosestPoint(Vector3 fromPosition)
    {
        if (col == null)
            col = GetComponent<Collider>();

        Vector3 raw = col != null ? col.ClosestPoint(fromPosition) : transform.position;

        if (NavMesh.SamplePosition(raw, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            return hit.position;

        return raw;
    }

}
