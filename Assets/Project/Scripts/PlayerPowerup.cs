using UnityEngine;
using System.Collections;

public class PlayerPowerup : MonoBehaviour
{
    [Header("Settings")]
    public KeyCode activationKey = KeyCode.Q; //press Q to activate
    public float duration = 5f;               //countdown lasts 5 seconds

    [Header("Refs")]
    public PowerupTimerUI timerUI;            //reference to the UI script from the previous step

    [Header("State (debug)")]
    public bool hasPowerup = false;           //whether the player currently has a powerup

    private bool active = false;
    private Coroutine _effectCo;

    void Update()
    {
        //works directly with the old Input System or "Both" mode
        if (Input.GetKeyDown(activationKey))
        {
            TryActivate();
        }
    }

    // Call this when the player picks up a powerup (will implement in the future)
    public void OnPickupPowerup()
    {
        hasPowerup = true;
    }

    private void TryActivate()
    {
        if (!hasPowerup || active) return;

        //ensure powerup is one-time use
        hasPowerup = false;

        if (_effectCo != null) StopCoroutine(_effectCo);
        _effectCo = StartCoroutine(EffectRoutine());
    }

    private IEnumerator EffectRoutine()
    {
        active = true;
        // Effect starts here
        // Show circular countdown for 5 seconds
        if (timerUI != null) timerUI.Show(duration);

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            yield return null;
        }

        //effect ends here

        active = false;
        _effectCo = null;
    }
}
