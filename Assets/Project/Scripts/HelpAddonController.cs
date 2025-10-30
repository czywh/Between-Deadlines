using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

// HelpAddonController.cs
public class HelpAddonController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Root object of the world-space prompt (shows 'Press E...')")]
    public GameObject promptCanvas;            // HelpAddon/PromptCanvas
    [Tooltip("Root object of the full-screen help page (Screen Space Overlay)")]
    public GameObject helpCanvas;              // HelpAddon/HelpCanvas (or global UI)
    [Tooltip("TMP text inside the help page (optional)")]
    public TextMeshProUGUI helpText;           // HelpCanvas/Panel/Text (TMP)
    [Tooltip("Close button inside the help page (optional)")]
    public Button closeButton;                 // HelpCanvas/Panel/CloseButton

    [Header("Settings")]
    [Tooltip("Tag of the player object; must match the Player object's tag")]
    public string playerTag = "Player";
    [TextArea(2, 4)]
    public string instructionMessage = "Press E to view the instruction!";

    [Header("Input")]
    [Tooltip("Key to toggle the help page")]
    public KeyCode toggleKey = KeyCode.E;      
    [Tooltip("Key to close the help page (ESC)")]
    public KeyCode closeKey  = KeyCode.Escape; 

    [Header("Events (Optional)")]
    [Tooltip("Event called when the help page opens (e.g. to freeze player movement)")]
    public UnityEvent onHelpOpen;              
    [Tooltip("Event called when the help page closes (e.g. to resume movement)")]
    public UnityEvent onHelpClose;             

    // ===== Internal State =====
    private bool _inRange = false;
    private bool _helpOpen = false;

    void Reset()
    {
        // Automatically assign references when the script is first added
        if (promptCanvas == null) promptCanvas = transform.Find("PromptCanvas")?.gameObject;
        if (helpCanvas   == null) helpCanvas   = transform.Find("HelpCanvas")?.gameObject;
        if (helpText     == null) helpText     = GetComponentInChildren<TextMeshProUGUI>(true);
        if (closeButton  == null) closeButton  = GetComponentInChildren<Button>(true);
    }

    void Awake()
    {
        SafeSetActive(promptCanvas, false);
        SafeSetActive(helpCanvas, false);

        if (helpText != null && !string.IsNullOrWhiteSpace(instructionMessage))
            helpText.text = instructionMessage;

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(CloseHelp);
        }

        Debug.Log($"[HelpAddon] bound helpText = {helpText?.name}", this);
    }

    void Update()
    {
        // Press E: open if in range; close if already open
        if (Input.GetKeyDown(toggleKey))
        {
            if (_helpOpen) CloseHelp();
            else if (_inRange) OpenHelp();
        }

        // ESC always closes
        if (_helpOpen && Input.GetKeyDown(closeKey))
            CloseHelp();
    }

    // ===== Trigger Events =====
    public void NotifyEnter()
    {
        _inRange = true;
        SafeSetActive(promptCanvas, !_helpOpen); // Show prompt only when help is closed
    }

    public void NotifyExit()
    {
        _inRange = false;
        SafeSetActive(promptCanvas, false);
        // Uncomment the next line if you want to auto-close help when leaving range
        // if (_helpOpen) CloseHelp();
    }
    // ===========================

    public void OpenHelp()
    {
        _helpOpen = true;
        SafeSetActive(helpCanvas, true);
        SafeSetActive(promptCanvas, false);
        onHelpOpen?.Invoke();
    }

    public void CloseHelp()
    {
        _helpOpen = false;
        SafeSetActive(helpCanvas, false);
        SafeSetActive(promptCanvas, _inRange); // Restore "Press E..." if still in range
        onHelpClose?.Invoke();
    }

    private void SafeSetActive(GameObject go, bool v)
    {
        if (go != null && go.activeSelf != v) go.SetActive(v);
    }

    void Start()
    {
        if (helpText != null)
        {
            helpText.text = instructionMessage;
        }
    }
}
