using UnityEngine;

public class UISoundManager : MonoBehaviour
{
    public static UISoundManager Instance;
    private AudioSource audioSource;

    void Awake() 
    {
        // initialize singleton instance and get attached AudioSource
        Instance = this;
        audioSource = GetComponent<AudioSource>();
    }

    // plays assigned button click sound 
    public void PlayClick()
    {
        if (audioSource != null && audioSource.clip != null)
            audioSource.Play();
    }
}
