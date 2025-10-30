using UnityEngine;
//ProximityTriggerRelay.cs
[RequireComponent(typeof(Collider))]
public class ProximityTriggerRelay : MonoBehaviour
{
    [Tooltip("所在HelpAddon上的控制器")]
    public HelpAddonController controller;

    [Tooltip("玩家的Tag；需与Player物体一致")]
    public string playerTag = "Player";

    void Reset()
    {
        // 自动向上找一下控制器（父物体）
        if (controller == null)
            controller = GetComponentInParent<HelpAddonController>();
        
        // 确保是Trigger
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (controller == null) return;
        if (other.CompareTag(playerTag))
            controller.NotifyEnter();
    }

    void OnTriggerExit(Collider other)
    {
        if (controller == null) return;
        if (other.CompareTag(playerTag))
            controller.NotifyExit();
    }
}
