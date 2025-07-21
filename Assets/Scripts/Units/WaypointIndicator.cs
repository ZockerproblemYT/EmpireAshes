using UnityEngine;

public class WaypointIndicator : MonoBehaviour
{
    public Vector3 targetPosition;
    public GameObject linkedUnit;

    public void SetColor(Color color)
    {
        Renderer renderer = GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = color;
        }
    }
}
