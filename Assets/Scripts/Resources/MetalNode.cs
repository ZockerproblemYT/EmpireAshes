using UnityEngine;

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
    Collider col = GetComponent<Collider>();
    if (col != null)
        return col.ClosestPoint(fromPosition);

    return transform.position;
}

}
