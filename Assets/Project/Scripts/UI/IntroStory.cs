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
        "After a long night of coding and coffee,\n" +
        "you wake up to find yourself shrunk inside your own apartment.\n\n" +
        "Navigate the miniature world and finish your deadlines\n" +
        "before sunrise.";

    public float typingSpeed = 0.03f;

    private bool isTyping;
    private Coroutine typingCoroutine;

    void Start()
    {
        // Freeze gameplay while intro runs
        Time.timeScale = 0f;
        storyPanel.SetActive(true);
        continueButton.gameObject.SetActive(true);
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

            // Play typing sound occasionally
            if (typingAudioSource && !char.IsWhiteSpace(c) && Random.value > 0.7f)
            {
                typingAudioSource.pitch = Random.Range(0.95f, 1.05f);
                typingAudioSource.PlayOneShot(typingAudioSource.clip);
            }

            yield return new WaitForSecondsRealtime(typingSpeed);
        }

        isTyping = false;
        continueButton.gameObject.SetActive(true);
    }

    void Update()
    {
        // Skip typing if clicked
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