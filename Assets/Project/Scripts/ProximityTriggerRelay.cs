using UnityEngine;

// ProximityTriggerRelay.cs
[RequireComponent(typeof(Collider))]
public class ProximityTriggerRelay : MonoBehaviour
{
    [Tooltip("The HelpAddonController this trigger belongs to")]
    public HelpAddonController controller;

    [Tooltip("Tag of the player object; must match the Player object's tag")]
    public string playerTag = "Player";

    void Reset()
    {
        // Automatically find the controller from the parent object
        if (controller == null)
            controller = GetComponentInParent<HelpAddonController>();
        
        // Ensure the collider is set as a trigger
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
