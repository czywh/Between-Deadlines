using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class DanceModeSwitcher : MonoBehaviour
{
    [Header("Refs")]
    public Animator animator;
    public RuntimeAnimatorController locomotionController;
    public RuntimeAnimatorController danceController;
    public KeyAnimationSwitcher animationSwitcher;

    [Header("Disable while dancing")]
    public MonoBehaviour[] movementAndCameraScripts;

    void Awake()
    {
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!locomotionController && animator)
            locomotionController = animator.runtimeAnimatorController;
        if (!animationSwitcher)
            animationSwitcher = GetComponent<KeyAnimationSwitcher>();
        if (animationSwitcher) animationSwitcher.enabled = false;
    }

    void SetEnabled(MonoBehaviour[] list, bool on)
    {
        foreach (var s in list) if (s) s.enabled = on;
    }

    public void EnterDanceMode()
    {
        SetEnabled(movementAndCameraScripts, false);
        if (danceController) animator.runtimeAnimatorController = danceController;
        animator.applyRootMotion = false;
        if (animationSwitcher) animationSwitcher.enabled = true;
    }

    public void ExitDanceMode()
    {
        if (locomotionController) animator.runtimeAnimatorController = locomotionController;
        animator.applyRootMotion = false;
        SetEnabled(movementAndCameraScripts, true);
        if (animationSwitcher) animationSwitcher.enabled = false;
    }

    void Update()
    {
        // Use T/Y only
        bool enter = Input.GetKeyDown(KeyCode.T);
        bool exit  = Input.GetKeyDown(KeyCode.Y);

        #if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        if (kb != null) {
            enter |= kb.tKey.wasPressedThisFrame;
            exit  |= kb.yKey.wasPressedThisFrame;
        }
        #endif

        if (enter) EnterDanceMode();
        if (exit)  ExitDanceMode();
    }
}
