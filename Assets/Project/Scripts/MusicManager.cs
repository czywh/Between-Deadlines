using UnityEngine;
using System.Collections;
public class MusicManager : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] private float sadMaxVolume = 0.2f;
    [SerializeField, Range(0f, 1f)] private float happyMaxVolume = 0.1f;

    public AudioSource sadMusic;
    public AudioSource happyMusic;
    public float fadeDuration = 3f;

    // track if happy music has started before
    private bool hasPlayedOnce = false;

    void Start()
    {
        sadMusic.volume = sadMaxVolume;
        happyMusic.volume = 0f;

        sadMusic.Play();
        happyMusic.Play();
        happyMusic.Pause();
    }

    //  called when enter world b
    public void OnSphereOpened()
    {
        StopAllCoroutines();
        StartCoroutine(FadeToHappy());
    }

    private IEnumerator FadeToHappy()
    {
        float time = 0f;

        if (!hasPlayedOnce)
        {
            happyMusic.time = 20f;
            hasPlayedOnce = true;
        }
        happyMusic.UnPause();

        float sadStart = sadMusic.volume;
        float happyStart = happyMusic.volume;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            float t = time / fadeDuration;

            sadMusic.volume = Mathf.Lerp(sadStart, 0f, t);
            happyMusic.volume = Mathf.Lerp(happyStart, happyMaxVolume, t);

            yield return null;
        }
        sadMusic.Pause();
        sadMusic.volume = 0f;
        happyMusic.volume = happyMaxVolume;
    }

    // called when return from world b
    public void OnSphereClosed()
    {
        StopAllCoroutines();
        sadMusic.UnPause();
        StartCoroutine(FadeBackToSad());
    }

    private IEnumerator FadeBackToSad()
    {
        float time = 0f;

        float sadStart = sadMusic.volume;
        float happyStart = happyMusic.volume;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            float t = time / fadeDuration;

            sadMusic.volume = Mathf.Lerp(sadStart, sadMaxVolume, t);
            happyMusic.volume = Mathf.Lerp(happyStart, 0f, t);

            yield return null;
        }

        happyMusic.Pause();
        sadMusic.volume = sadMaxVolume;
        happyMusic.volume = 0f;
    }
}
