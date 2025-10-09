using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class PowerupTimerUI : MonoBehaviour
{
    [Header("Refs")]
    public Image ring;         //circular progress bar (Filled, Radial 360)
    public TMP_Text label;     //numeric countdown text

    [Header("Options")]
    public bool useUnscaledTime = false; //whether to keep counting during pause

    private Coroutine _running;


    //show a countdown ring lasting for durationSeconds
    public void Show(float durationSeconds)
    {
        gameObject.SetActive(true);

        if (_running != null) StopCoroutine(_running);
        _running = StartCoroutine(Run(durationSeconds));
    }

    private IEnumerator Run(float duration)
    {
        //initialize full ring and starting number
        if (ring != null) ring.fillAmount = 1f;
        if (label != null) label.text = Mathf.CeilToInt(duration).ToString();

        float t = 0f;
        while (t < duration)
        {
            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            t += dt;

            float remain = Mathf.Max(0f, duration - t);
            float ratio = Mathf.Clamp01(remain / duration);

            if (ring != null) ring.fillAmount = ratio;          //fill goes from 1 → 0
            if (label != null) label.text = Mathf.CeilToInt(remain).ToString();

            yield return null;
        }

        Hide(); //auto-hide when finished
    }

    public void Hide()
    {
        if (_running != null)
        {
            StopCoroutine(_running);
            _running = null;
        }
        gameObject.SetActive(false);
    }
}
