using UnityEngine;

[DisallowMultipleComponent]
public class PortalRTAutoAssign : MonoBehaviour
{
    [Header("References")]
    public Material portalMaterial;        
    public string portalRTProperty = "_PortalRT"; 
    public RenderTexture portalRT;         

    [Header("Fallback")]
    public Texture fallbackTexture;        

    void Awake()
    {
        
        if (!portalMaterial)
        {
            var r = GetComponent<Renderer>();
            if (r && r.sharedMaterial) portalMaterial = r.sharedMaterial;
        }

        if (!fallbackTexture)
            fallbackTexture = Texture2D.blackTexture;

       
        ApplyTexture();
    }

    void Update()
    {
        ApplyTexture();
    }

    void ApplyTexture()
    {
        if (!portalMaterial) return;
        if (string.IsNullOrEmpty(portalRTProperty)) portalRTProperty = "_PortalRT";

        if (portalRT)
        {
            if (portalMaterial.GetTexture(portalRTProperty) != portalRT)
                portalMaterial.SetTexture(portalRTProperty, portalRT);
        }

        else
        {
            if (portalMaterial.GetTexture(portalRTProperty) != fallbackTexture)
                portalMaterial.SetTexture(portalRTProperty, fallbackTexture);
        }
    }
}
