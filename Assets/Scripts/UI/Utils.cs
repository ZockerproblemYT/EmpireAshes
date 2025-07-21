using UnityEngine.EventSystems;

public static class Utils
{
    public static bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }
}
