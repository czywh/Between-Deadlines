using UnityEngine;
using System.Collections.Generic;

/// Utility class for managing the rendering of "World B Only" objects
/// and synchronizing the portal camera with the main camera.
public enum RenderPath { RT } 

public struct RenderPathContext
{
    public Camera mainCamera;
    public Camera portalCamera;
    public int worldBLayer;

    public LayerMask portalCameraCulling;
    public string sphereRTProperty;
    public RenderTexture portalRT;                 
    public Material sphereMaterialForRT;
    public float rtResolutionScale;
    public bool useDistanceFade;
    public bool useRenderTexture;
}

public static class WorldB_Graph_InteractionUtil
{
    // ===== Fallback helpers ===================================================

    private static readonly Texture2D s_BlackTex = Texture2D.blackTexture;

    private static string SafeRTProp(string prop) => string.IsNullOrEmpty(prop) ? "_PortalRT" : prop;

    private static void EnsureFallbackBinding(Material mat, string propName)
    {
        if (!mat || string.IsNullOrEmpty(propName)) return;
        if (mat.GetTexture(propName) == null)
            mat.SetTexture(propName, s_BlackTex);
    }

    private static void EnsureFallbackBinding(IEnumerable<Renderer> renderers, string propName)
    {
        if (renderers == null || string.IsNullOrEmpty(propName)) return;
        foreach (var r in renderers)
        {
            if (!r) continue;
            foreach (var m in r.sharedMaterials)
            {
                if (!m) continue;
                if (m.GetTexture(propName) == null)
                    m.SetTexture(propName, s_BlackTex);
            }
        }
    }

    /// Enable or disable physics interactions for "World B" objects.
    public static void SetInteractive(Transform root, bool interactive)
    {
        if (!root) return;

        var colliders = root.GetComponentsInChildren<Collider>(true);
        var rigidbodies = root.GetComponentsInChildren<Rigidbody>(true);

        foreach (var c in colliders)
        {
            if (!c) continue;
            c.enabled = true;
            c.isTrigger = !interactive;
        }

        foreach (var rb in rigidbodies)
        {
            if (!rb) continue;
            rb.isKinematic = interactive;
            if (!interactive)
            {
                rb.angularVelocity = Vector3.zero;
                rb.Sleep();
            }
            else rb.WakeUp();
        }
    }

    //  Always use RenderTexture path 
    public static RenderPath UpdatePath_AlwaysRT(ref RenderTexture portalRT, in RenderPathContext ctx)
    {
        string prop = SafeRTProp(ctx.sphereRTProperty);

        if (ctx.mainCamera == null || ctx.portalCamera == null)
        {
            // Camera 不可用时，确保材质上有合法贴图（黑贴图）
            EnsureFallbackBinding(ctx.sphereMaterialForRT, prop);
            ReleaseRT(ref portalRT, ctx.portalCamera, ctx.sphereMaterialForRT, prop);
            return RenderPath.RT;
        }

        int worldBMask = 1 << ctx.worldBLayer;

        // Main camera never renders World B layer
        ctx.mainCamera.cullingMask &= ~worldBMask;

        // Portal camera only renders World B layer
        ctx.portalCamera.enabled = ctx.useRenderTexture;
        ctx.portalCamera.cullingMask = ctx.portalCameraCulling;

        // Ensure RT creation and binding 
        CheckOrRecreateRT(
            ref portalRT,
            ctx.portalCamera,
            ctx.useRenderTexture,
            Mathf.Clamp01(ctx.rtResolutionScale <= 0 ? 1f : ctx.rtResolutionScale),
            ctx.sphereMaterialForRT,
            prop
        );

        EnsureRTBinding(portalRT, in ctx);

        // 如果仍然没有 RT（例如 useRenderTexture=false），也保证材质不为空
        if (portalRT == null)
            EnsureFallbackBinding(ctx.sphereMaterialForRT, prop);

        return RenderPath.RT;
    }

    // Check or rebuild RT. 
    // If useRenderTexture == false or the camera is invalid, release and bind fallback
    public static bool CheckOrRecreateRT(
        ref RenderTexture rt,
        Camera portalCamera,
        bool useRenderTexture,
        float resolutionScale,
        Material sphereMaterialForRT = null,
        string sphereRTProperty = "_PortalRT",
        string rtName = "PortalSphere_RT",
        RenderTextureFormat format = RenderTextureFormat.ARGB32,
        int depthBufferBits = 24 // WebGL-friendly depth
    )
    {
        string prop = SafeRTProp(sphereRTProperty);

        if (!useRenderTexture || !portalCamera)
        {
            // 关闭 RT 路径或相机无效时，强制材质使用黑贴图
            EnsureFallbackBinding(sphereMaterialForRT, prop);
            ReleaseRT(ref rt, portalCamera);
            return false;
        }

        // 强制 WebGL 友好设置
        portalCamera.allowHDR  = false;
        portalCamera.allowMSAA = false;

        int w = Mathf.Max(1, Mathf.RoundToInt(Screen.width * resolutionScale));
        int h = Mathf.Max(1, Mathf.RoundToInt(Screen.height * resolutionScale));

        bool needRecreate = (rt == null) || (rt.width != w) || (rt.height != h);

        if (!needRecreate)
        {
            if (sphereMaterialForRT)
                sphereMaterialForRT.SetTexture(prop, rt);

            if (portalCamera.targetTexture != rt)
                portalCamera.targetTexture = rt;

            return false;
        }

        ReleaseRT(ref rt, portalCamera);

        rt = new RenderTexture(w, h, depthBufferBits, format)
        {
            name = rtName,
            antiAliasing = 1,           // MSAA off (WebGL safe)
            useMipMap = false,
            autoGenerateMips = false,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };
        rt.Create();

        portalCamera.targetTexture = rt;

        if (sphereMaterialForRT)
            sphereMaterialForRT.SetTexture(prop, rt);

        return true;
    }

    public static void EnsureRTBinding(RenderTexture rt, in RenderPathContext ctx)
    {
        if (!ctx.portalCamera) return;

        string prop = SafeRTProp(ctx.sphereRTProperty);

        if (ctx.useRenderTexture && rt)
        {
            if (ctx.sphereMaterialForRT)
                ctx.sphereMaterialForRT.SetTexture(prop, rt);

            if (ctx.portalCamera.targetTexture != rt)
                ctx.portalCamera.targetTexture = rt;
        }
        else
        {
            // 无 RT 时也要保证材质不为空
            EnsureFallbackBinding(ctx.sphereMaterialForRT, prop);
            if (ctx.portalCamera.targetTexture != null)
                ctx.portalCamera.targetTexture = null;
        }
    }

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

    public static void ReleaseRT(ref RenderTexture rt, Camera portalCamera, Material boundMat, string prop = "_PortalRT")
    {
        // 释放 RT 的同时，确保材质槽有黑贴图而不是空
        EnsureFallbackBinding(boundMat, SafeRTProp(prop));
        ReleaseRT(ref rt, portalCamera);
    }

    // Material Handling (Keep RT keyword enabled)
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

    public static void EnableRTPath(Material mat)
    {
        if (!mat) return;
        mat.EnableKeyword("_RENDERPATH_RT");
        mat.DisableKeyword("_RENDERPATH_CLIP");
    }

    /// Utility for multiple sphere visuals: 把 RT 绑定到一批渲染器的材质上
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
                {
                    m.SetTexture(propertyName, rt);
                }
                else
                {
                    if (!keepWhenNull)
                        m.SetTexture(propertyName, null);

                    // 无论 keepWhenNull 与否，都兜底保证非空
                    if (m.GetTexture(propertyName) == null)
                        m.SetTexture(propertyName, s_BlackTex);
                }
            }
        }
    }
}