using UnityEngine;

// AudioManager.cs
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        Instance = this;
    }

    public void PlaySFX(AudioClip clip) { /* später */ }
}
