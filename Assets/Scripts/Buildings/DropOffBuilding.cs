using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class DropOffBuilding : MonoBehaviour
{
    public static readonly List<DropOffBuilding> Instances = new List<DropOffBuilding>();

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
        Collider[] colliders = GetComponentsInChildren<Collider>();
        if (colliders == null || colliders.Length == 0)
            return transform.position;

        Vector3 closestPoint = transform.position;
        float closestSqrDistance = float.MaxValue;

        foreach (Collider col in colliders)
        {
            Vector3 candidate = col.ClosestPoint(fromPosition);
            float sqrDist = (candidate - fromPosition).sqrMagnitude;

            if (sqrDist < closestSqrDistance)
            {
                closestSqrDistance = sqrDist;
                closestPoint = candidate;
            }
        }

        if (NavMesh.SamplePosition(closestPoint, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            return hit.position;

        return closestPoint;
    }
}
