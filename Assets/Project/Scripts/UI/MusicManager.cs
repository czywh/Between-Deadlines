using UnityEngine;
using System.Collections;
public class MusicManager : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] private float sadMaxVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float happyMaxVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float victoryMaxVolume = 1f;


    [Header("Audio Sources")]
    public AudioSource sadMusic;
    public AudioSource happyMusic;
    public AudioSource victoryMusic;

    [Header("Fade Settings")]
    public float fadeDuration = 3f;

    // track if happy music has started before
    private bool hasPlayedOnce = false;

    void Start()
    {
        sadMusic.volume = sadMaxVolume;
        happyMusic.volume = 0f;
        victoryMusic.volume = 0f;

        sadMusic.Play();
        happyMusic.Play();
        happyMusic.Pause();
        victoryMusic.Play();
        victoryMusic.Pause();
    }

    //  called when sphere opened
    public void OnSphereOpened()
    {
        StopAllCoroutines();
        StartCoroutine(FadeToHappy());
    }

    // fade out sad music and faede in happy music 
    private IEnumerator FadeToHappy()
    {
        float time = 0f;

        // start musc from 20 second onwards 
        if (!hasPlayedOnce)
        {
            happyMusic.time = 20f;
            hasPlayedOnce = true;
        }
        happyMusic.UnPause();

        float sadStart = sadMusic.volume;
        float happyStart = happyMusic.volume;

        // gradually adjust volumes over fadeDuration
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

    // called when sphere is closed - triggers transition back to sad music
    public void OnSphereClosed()
    {
        StopAllCoroutines();
        sadMusic.UnPause();
        StartCoroutine(FadeBackToSad());
    }

    // fade out happy music and fade in sad music
    private IEnumerator FadeBackToSad()
    {
        float time = 0f;

        float sadStart = sadMusic.volume;
        float happyStart = happyMusic.volume;

        // gradually adjust volumes over fadeDuration seconds
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

    // called when player reaches teddy bear - triggers victory music
    public void OnVictory()
    {
        StopAllCoroutines();
        StartCoroutine(FadeToVictory());
    }

    // fade in victory music while stopping all other tracks
    private IEnumerator FadeToVictory()
    {
        float time = 0f;

        // stop other music
        sadMusic.Pause();
        happyMusic.Pause();

        victoryMusic.UnPause();
        float victoryStart = 0f;

        // gradually fade in victory music over fadeDuration seconds
        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            float t = time / fadeDuration;
            victoryMusic.volume = Mathf.Lerp(victoryStart, victoryMaxVolume, t);
            yield return null;
        }

        victoryMusic.volume = victoryMaxVolume;
    }
}
