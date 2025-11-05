using UnityEngine;
using System.Collections.Generic;

/// Utility for managing "World B Only" rendering via a portal RenderTexture,
/// and for synchronizing the portal camera with the main camera.
public enum RenderPath { RT }

public struct RenderPathContext
{
    public Camera mainCamera;                 // Main camera rendering World A
    public Camera portalCamera;               // Portal camera rendering World B
    public int worldBLayer;                   // Layer index for "World B Only"
    public LayerMask portalCameraCulling;     // What the portal camera should render
    public string sphereRTProperty;           // Shader property name to bind the RT (e.g., "_PortalRT")
    public RenderTexture portalRT;            // Existing portal RT (will be recreated if size changes)
    public Material sphereMaterialForRT;      // Material used on the portal sphere that samples the RT
    public float rtResolutionScale;           // Scale relative to screen resolution (0~1)
    public bool useDistanceFade;              // (Not used here, reserved for effects)
    public bool useRenderTexture;             // Master switch to enable/disable RT path
}

public static class WorldB_Graph_InteractionUtil
{
    /// Enable or disable physics interactions for objects under a root transform.
    public static void SetInteractive(Transform root, bool interactive)
    {
        if (!root) return;

        var colliders = root.GetComponentsInChildren<Collider>(true);
        var rigidbodies = root.GetComponentsInChildren<Rigidbody>(true);

        foreach (var c in colliders)
        {
            if (!c) continue;
            // Always keep collider enabled; use trigger when non-interactive
            c.enabled = true;
            c.isTrigger = !interactive;
        }

        foreach (var rb in rigidbodies)
        {
            if (!rb) continue;
            // Kinematic when interactive so external physics won't affect it
            rb.isKinematic = interactive;
            if (!interactive)
            {
                rb.angularVelocity = Vector3.zero;
                rb.Sleep();
            }
            else rb.WakeUp();
        }
    }

    /// Always use the RenderTexture path. Disables HDR/MSAA on the portal camera,
    /// sets culling masks, ensures RT existence and binding, and returns the current path.
    public static RenderPath UpdatePath_AlwaysRT(ref RenderTexture portalRT, in RenderPathContext ctx)
    {
        if (ctx.mainCamera == null || ctx.portalCamera == null)
        {
            ReleaseRT(ref portalRT, ctx.portalCamera, ctx.sphereMaterialForRT, SafeRTProp(ctx.sphereRTProperty));
            return RenderPath.RT;
        }

        ctx.portalCamera.allowHDR = false;
        ctx.portalCamera.allowMSAA = false;

        int worldBMask = 1 << ctx.worldBLayer;

        // Main camera never renders World B layer
        ctx.mainCamera.cullingMask &= ~worldBMask;

        // Portal camera only renders what is specified (typically World B)
        ctx.portalCamera.enabled = ctx.useRenderTexture;
        ctx.portalCamera.cullingMask = ctx.portalCameraCulling;

        // Ensure RT creation and binding (WebGL-safe descriptor)
        CheckOrRecreateRT(
            ref portalRT,
            ctx.portalCamera,
            ctx.useRenderTexture,
            Mathf.Clamp01(ctx.rtResolutionScale <= 0 ? 1f : ctx.rtResolutionScale),
            ctx.sphereMaterialForRT,
            SafeRTProp(ctx.sphereRTProperty),
            "PortalSphere_RT_WebGL"
        );

        EnsureRTBinding(portalRT, in ctx);

        return RenderPath.RT;
    }

    /// Set the created RT to the portal camera's targetTexture and bind to the sphere material.
    public static bool CheckOrRecreateRT(
        ref RenderTexture rt,
        Camera portalCamera,
        bool useRenderTexture,
        float resolutionScale,
        Material sphereMaterialForRT = null,
        string sphereRTProperty = "_PortalRT",
        string rtName = "PortalSphere_RT_WebGL")
    {
        if (!useRenderTexture || !portalCamera)
        {
            if (sphereMaterialForRT && !string.IsNullOrEmpty(sphereRTProperty))
                sphereMaterialForRT.SetTexture(sphereRTProperty, null);

            ReleaseRT(ref rt, portalCamera);
            return false;
        }

        // Force-disable HDR/MSAA on the portal camera (WebGL limitation)
        portalCamera.allowHDR = false;
        portalCamera.allowMSAA = false;

        int w = Mathf.Max(1, Mathf.RoundToInt(Screen.width  * (resolutionScale <= 0 ? 1f : resolutionScale)));
        int h = Mathf.Max(1, Mathf.RoundToInt(Screen.height * (resolutionScale <= 0 ? 1f : resolutionScale)));

        bool needRecreate = (rt == null) || (rt.width != w) || (rt.height != h);

        if (!needRecreate)
        {
            if (sphereMaterialForRT && !string.IsNullOrEmpty(sphereRTProperty))
                sphereMaterialForRT.SetTexture(sphereRTProperty, rt);

            if (portalCamera.targetTexture != rt)
                portalCamera.targetTexture = rt;

            return false;
        }

        ReleaseRT(ref rt, portalCamera);

        var desc = new RenderTextureDescriptor(w, h)
        {
            graphicsFormat     = UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm, 
            depthStencilFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.D24_UNorm_S8_UInt, 
            msaaSamples        = 1,                 // No MSAA for RT on WebGL
            useMipMap          = false,
            autoGenerateMips   = false,
            sRGB               = (QualitySettings.activeColorSpace == ColorSpace.Linear),
            dimension          = UnityEngine.Rendering.TextureDimension.Tex2D,
            enableRandomWrite  = false              // No compute on WebGL
        };

        rt = new RenderTexture(desc)
        {
            name = rtName
        };
        rt.Create();

        portalCamera.targetTexture = rt;

        if (sphereMaterialForRT && !string.IsNullOrEmpty(sphereRTProperty))
            sphereMaterialForRT.SetTexture(sphereRTProperty, rt);

        return true;
    }

    /// Ensure that the portal RT is bound both to the sphere material and the portal camera.
    public static void EnsureRTBinding(RenderTexture rt, in RenderPathContext ctx)
    {
        if (!ctx.useRenderTexture || !ctx.portalCamera) return;

        if (ctx.sphereMaterialForRT && !string.IsNullOrEmpty(ctx.sphereRTProperty))
            ctx.sphereMaterialForRT.SetTexture(ctx.sphereRTProperty, rt);

        if (ctx.portalCamera.targetTexture != rt)
            ctx.portalCamera.targetTexture = rt;
    }

    /// Release and destroy a RenderTexture and detach it from the camera if needed.
    public static void ReleaseRT(ref RenderTexture rt, Camera portalCamera = null)
    {
        if (portalCamera && portalCamera.targetTexture == rt)
            portalCamera.targetTexture = null;

        if (rt != null)
        {
            rt.Release();
            Object.Destroy(rt);
            rt = null;
        }
    }

    /// Release RT and also clear the bound texture from a material property.
    public static void ReleaseRT(ref RenderTexture rt, Camera portalCamera, Material boundMat, string prop = "_PortalRT")
    {
        if (boundMat && !string.IsNullOrEmpty(prop))
            boundMat.SetTexture(prop, null);

        ReleaseRT(ref rt, portalCamera);
    }

    /// Enable the RT rendering keyword on all renderers under a given root that are on the World B layer.
    public static void ApplyMaterialPath(Transform worldBOnlyRoot, int worldBLayer, RenderPath path)
    {
        if (!worldBOnlyRoot) return;

        foreach (var r in worldBOnlyRoot.GetComponentsInChildren<Renderer>(true))
        {
            if (!r || r.gameObject.layer != worldBLayer) continue;
            foreach (var m in r.sharedMaterials)
                if (m) EnableRTPath(m);
        }
    }

    /// Enable RT rendering keywords on a material (and disable clip-only path).
    public static void EnableRTPath(Material mat)
    {
        if (!mat) return;
        mat.EnableKeyword("_RENDERPATH_RT");
        mat.DisableKeyword("_RENDERPATH_CLIP");
    }

    /// Bind a portal RT to a batch of renderers' materials for a given property.
    public static void BindRTToSphereMaterials(IEnumerable<Renderer> renderers, RenderTexture rt, string propertyName = "_PortalRT", bool keepWhenNull = true)
    {
        if (renderers == null || string.IsNullOrEmpty(propertyName)) return;

        foreach (var r in renderers)
        {
            if (!r) continue;
            foreach (var m in r.sharedMaterials)
            {
                if (!m) continue;
                if (rt)
                    m.SetTexture(propertyName, rt);
                else if (!keepWhenNull)
                    m.SetTexture(propertyName, null);
            }
        }
    }

    /// Fallback for null/empty material property names.
    private static string SafeRTProp(string prop) => string.IsNullOrEmpty(prop) ? "_PortalRT" : prop;
}