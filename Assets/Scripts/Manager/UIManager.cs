using UnityEngine;

// UIManager.cs
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        Instance = this;
    }

    public void UpdateResourcesUI() { /* später */ }
}
