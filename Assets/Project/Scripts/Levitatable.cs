using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Levitatable : MonoBehaviour
{
    [Header("Grab Limits")]
    [Tooltip("Maximum distance from the wand/camera where this object can be grabbed.")]
    public float maxGrabDistance = 100f;

    [Tooltip("Maximum distance you can hold it in front of you.")]
    public float maxHoldDistance = 100f;

    [Header("Mass Scaling")]
    [Tooltip("Scales the joint force. Heavy objects can use larger values.")]
    public float massScale = 1f;

    [Header("Optional: Visual feedback")]
    public Renderer[] highlightRenderers;
    public Color highlightColor = new Color(1f, 0.9f, 0.2f, 1f);
    private Color[] _orig;

    void Awake()
    {
        if (highlightRenderers != null && highlightRenderers.Length > 0)
        {
            _orig = new Color[highlightRenderers.Length];
            for (int i = 0; i < highlightRenderers.Length; i++)
                _orig[i] = highlightRenderers[i].material.color;
        }
    }

    public void SetHighlighted(bool on)
    {
        if (_orig == null) return;
        for (int i = 0; i < highlightRenderers.Length; i++)
            highlightRenderers[i].material.color = on ? highlightColor : _orig[i];
    }
}