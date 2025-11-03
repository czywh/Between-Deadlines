using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class IntroStory : MonoBehaviour
{
    [Header("UI References")]
    public GameObject storyPanel;
    public TextMeshProUGUI storyText;
    public Button continueButton;

    [Header("Audio")]
    public AudioSource typingAudioSource;

    [Header("Story Settings")]
    [TextArea(5, 10)]
    public string story = 
        "Another late night. The assignment's due soon - but something feels off.\n" +
        "Your focus slips, your thoughts twist, and suddenly... you begin to shrink.\n\n" +
        "You find yourself trapped on your own study desk - a vast miniature world of papers, pens, and a glowing computer screen.\n\n" +
        "Two realities overlap: a dull, lifeless world bound by pressure, and a vibrant one filled with clarity and calm.\n\n" +
        "Switch between them, solve the puzzles of your mind, and find your way back to balance.";

    public float typingSpeed = 0.03f;

    private bool isTyping;
    private Coroutine typingCoroutine;

    void Start()
    {
        // pause gameplay while intro runs
        Time.timeScale = 0f;
        storyPanel.SetActive(true);
        continueButton.gameObject.SetActive(true);

        // start typing animation
        typingCoroutine = StartCoroutine(TypeStory());
        continueButton.onClick.AddListener(CloseStory);
    }

    IEnumerator TypeStory()
    {
        isTyping = true;
        storyText.text = "";

        foreach (char c in story)
        {
            storyText.text += c;

            // play typing sound randomly for natural effect
            if (typingAudioSource && !char.IsWhiteSpace(c) && Random.value > 0.7f)
            {
                typingAudioSource.pitch = Random.Range(0.95f, 1.05f);
                typingAudioSource.PlayOneShot(typingAudioSource.clip);
            }

            yield return new WaitForSecondsRealtime(typingSpeed);
        }

        // finish typing - show continue button (x)
        isTyping = false;
        continueButton.gameObject.SetActive(true);
    }

    void Update()
    {
        // skip typing if player clicked
        if (isTyping && Input.GetMouseButtonDown(0))
        {
            StopCoroutine(typingCoroutine);
            storyText.text = story;
            isTyping = false;
            continueButton.gameObject.SetActive(true);
        }
    }

    void CloseStory()
    {
        // stop typing and hide panel
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }
        isTyping = false;
        if (typingAudioSource != null && typingAudioSource.isPlaying)
        {
            typingAudioSource.Stop();
        }
        storyPanel.SetActive(false);
        Time.timeScale = 1f; // resume game
    }
}