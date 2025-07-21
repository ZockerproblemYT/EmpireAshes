using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float moveSpeed = 20f;
    public float edgeSize = 20f;
    public float scrollSpeed = 500f;
    public float minY = 20f;
    public float maxY = 100f;

    void Update()
    {
        Vector3 pos = transform.position;

        if (Input.GetKey("w") || Input.mousePosition.y >= Screen.height - edgeSize)
            pos.z += moveSpeed * Time.deltaTime;
        if (Input.GetKey("s") || Input.mousePosition.y <= edgeSize)
            pos.z -= moveSpeed * Time.deltaTime;
        if (Input.GetKey("d") || Input.mousePosition.x >= Screen.width - edgeSize)
            pos.x += moveSpeed * Time.deltaTime;
        if (Input.GetKey("a") || Input.mousePosition.x <= edgeSize)
            pos.x -= moveSpeed * Time.deltaTime;

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        pos.y -= scroll * Time.deltaTime * scrollSpeed;
        pos.y = Mathf.Clamp(pos.y, minY, maxY);

        transform.position = pos;
    }
}
