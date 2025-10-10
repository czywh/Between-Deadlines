using UnityEngine;

public class UISoundManager : MonoBehaviour
{
    public static UISoundManager Instance;
    private AudioSource audioSource;

    void Awake() 
    {
        Instance = this;
        audioSource = GetComponent<AudioSource>();
    }

    public void PlayClick()
    {
        if (audioSource != null && audioSource.clip != null)
        audioSource.Play();
    }
}
