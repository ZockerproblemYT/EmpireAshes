using UnityEngine;

public class DropOffBuilding : MonoBehaviour
{
    public Vector3 GetClosestPoint(Vector3 fromPosition)
    {
        Collider col = GetComponent<Collider>();
        return col != null ? col.ClosestPoint(fromPosition) : transform.position;
    }
}