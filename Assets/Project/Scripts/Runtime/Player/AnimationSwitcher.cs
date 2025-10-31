using UnityEngine;

public class KeyAnimationSwitcher : MonoBehaviour
{
    public Animator animator;

    public string[] stateNames = {
        "excited",     // 1
        "hiphop",      // 2
        "sadidle",      // 3
        "sittingsad",  // 4
        "rejected",    // 5
        "housedance"   // 6
    };

    void Reset() => animator = GetComponent<Animator>();

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) PlayIndex(0);
        else if (Input.GetKeyDown(KeyCode.Alpha2)) PlayIndex(1);
        else if (Input.GetKeyDown(KeyCode.Alpha3)) PlayIndex(2);
        else if (Input.GetKeyDown(KeyCode.Alpha4)) PlayIndex(3);
        else if (Input.GetKeyDown(KeyCode.Alpha5)) PlayIndex(4);
        else if (Input.GetKeyDown(KeyCode.Alpha6)) PlayIndex(5);
    }

void PlayIndex(int i)
{
    if (!animator || i < 0 || i >= stateNames.Length) return;
    animator.Play(stateNames[i], 0, 0f);
}
}
