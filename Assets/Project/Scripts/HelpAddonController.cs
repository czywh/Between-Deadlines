using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
//HelpAddonController.cs
public class HelpAddonController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("世界空间提示（显示“Press E…”）的根对象")]
    public GameObject promptCanvas;            // HelpAddon/PromptCanvas
    [Tooltip("全屏说明页的根对象（Screen Space Overlay）")]
    public GameObject helpCanvas;              // HelpAddon/HelpCanvas（或全局UI）
    [Tooltip("说明页里的TMP文字（可选）")]
    public TextMeshProUGUI helpText;           // HelpCanvas/Panel/Text (TMP)
    [Tooltip("说明页里的关闭按钮（可选）")]
    public Button closeButton;                 // HelpCanvas/Panel/CloseButton

    [Header("Settings")]
    [Tooltip("玩家的Tag；需要与Player物体一致")]
    public string playerTag = "Player";
    [TextArea(2, 4)]
    public string instructionMessage = "Press E to view the instruction!!";

    [Header("Input")]
    public KeyCode toggleKey = KeyCode.E;      // 按E开关说明页
    public KeyCode closeKey  = KeyCode.Escape; // ESC关闭

    [Header("Events (Optional)")]
    public UnityEvent onHelpOpen;              // 打开时回调（可冻结玩家移动等）
    public UnityEvent onHelpClose;             // 关闭时回调（恢复移动等）

    // 状态
    private bool _inRange = false;
    private bool _helpOpen = false;

    void Reset()
    {
        // 初次挂脚本时尽量自动抓引用
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
        // E键：在范围内→开；已开→关
        if (Input.GetKeyDown(toggleKey))
        {
            if (_helpOpen) CloseHelp();
            else if (_inRange) OpenHelp();
        }

        // ESC 始终可关
        if (_helpOpen && Input.GetKeyDown(closeKey))
            CloseHelp();
    }

    // ===== 供触发器转发调用 =====
    public void NotifyEnter()
    {
        _inRange = true;
        SafeSetActive(promptCanvas, !_helpOpen); // 说明页未开时才显示提示
    }

    public void NotifyExit()
    {
        _inRange = false;
        SafeSetActive(promptCanvas, false);
        // 如果希望离开范围就强制关闭说明页，取消下一行注释：
        // if (_helpOpen) CloseHelp();
    }
    // =================================

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
        SafeSetActive(promptCanvas, _inRange); // 仍在范围内则恢复“Press E…”
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
