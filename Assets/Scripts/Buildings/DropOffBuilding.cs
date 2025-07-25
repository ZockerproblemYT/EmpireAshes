using UnityEngine;

using System.Collections.Generic;

public class DropOffBuilding : MonoBehaviour
{
    public static readonly List<DropOffBuilding> Instances = new();

    private void OnEnable()
    {
        if (!Instances.Contains(this))
            Instances.Add(this);
    }

    private void OnDisable()
    {
        Instances.Remove(this);
    }

    public Vector3 GetClosestPoint(Vector3 fromPosition)
    {
        Collider col = GetComponent<Collider>();
        return col != null ? col.ClosestPoint(fromPosition) : transform.position;
    }
}
