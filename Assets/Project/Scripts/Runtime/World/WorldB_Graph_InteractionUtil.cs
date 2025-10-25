using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Utility class for managing the rendering of "World B Only" objects
/// and synchronizing the portal camera with the main camera.
/// </summary>
public enum RenderPath { RT } // ★ Always RT mode

public struct RenderPathContext
{
    public Camera mainCamera;
    public Camera portalCamera;
    public int worldBLayer;

    public LayerMask portalCameraCulling;
    public string sphereRTProperty;
    public RenderTexture portalRT;                 // 仅用于传入旧值（不会在内部被写回）
    public Material sphereMaterialForRT;
    public float rtResolutionScale;
    public bool useDistanceFade;
    public bool useRenderTexture;
}

public static class WorldB_Graph_InteractionUtil
{
    /// <summary>
    /// Enable or disable physics interactions for "World B" objects.
    /// </summary>
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

    // ===========================================================
    // ★ Always use RenderTexture path (no Clip mode)
    // ===========================================================
    public static RenderPath UpdatePath_AlwaysRT(ref RenderTexture portalRT, in RenderPathContext ctx)
    {
        if (ctx.mainCamera == null || ctx.portalCamera == null)
        {
            // 在无法渲染时确保释放（避免泄漏）
            ReleaseRT(ref portalRT, ctx.portalCamera, ctx.sphereMaterialForRT, SafeRTProp(ctx.sphereRTProperty));
            return RenderPath.RT;
        }

        int worldBMask = 1 << ctx.worldBLayer;

        // Main camera never renders World B layer
        ctx.mainCamera.cullingMask &= ~worldBMask;

        // Portal camera only renders World B layer
        ctx.portalCamera.enabled = ctx.useRenderTexture;
        ctx.portalCamera.cullingMask = ctx.portalCameraCulling;

        // ★ Ensure RT creation and binding (含关闭时释放)
        CheckOrRecreateRT(
            ref portalRT,
            ctx.portalCamera,
            ctx.useRenderTexture,
            Mathf.Clamp01(ctx.rtResolutionScale <= 0 ? 1f : ctx.rtResolutionScale),
            ctx.sphereMaterialForRT,
            SafeRTProp(ctx.sphereRTProperty)
        );

        // ★ 用“最新”的 portalRT 进行绑定，而不是 ctx.portalRT
        EnsureRTBinding(portalRT, in ctx);

        return RenderPath.RT;
    }

    // ===========================================================
    // RT Management
    // ===========================================================
    /// <summary>
    /// 检查或重建 RT。若 useRenderTexture==false 或相机无效，则释放并解绑。
    /// 返回值表示是否发生了重建。
    /// </summary>
    public static bool CheckOrRecreateRT(
        ref RenderTexture rt,
        Camera portalCamera,
        bool useRenderTexture,
        float resolutionScale,
        Material sphereMaterialForRT = null,
        string sphereRTProperty = "_PortalRT",
        string rtName = "PortalSphere_RT",
        RenderTextureFormat format = RenderTextureFormat.ARGB32,
        int depthBufferBits = 16
    )
    {
        // 不使用 RT 或相机不可用：释放 + 解绑
        if (!useRenderTexture || !portalCamera)
        {
            if (sphereMaterialForRT && !string.IsNullOrEmpty(sphereRTProperty))
                sphereMaterialForRT.SetTexture(sphereRTProperty, null);

            ReleaseRT(ref rt, portalCamera);
            return false;
        }

        int w = Mathf.Max(1, Mathf.RoundToInt(Screen.width * resolutionScale));
        int h = Mathf.Max(1, Mathf.RoundToInt(Screen.height * resolutionScale));

        // 如需更严谨，可把 format / depthBufferBits / MSAA 变化也纳入判断
        bool needRecreate = (rt == null) || (rt.width != w) || (rt.height != h);

        if (!needRecreate)
        {
            // 确保已绑定
            if (sphereMaterialForRT && !string.IsNullOrEmpty(sphereRTProperty))
                sphereMaterialForRT.SetTexture(sphereRTProperty, rt);

            if (portalCamera.targetTexture != rt)
                portalCamera.targetTexture = rt;

            return false;
        }

        // 重建
        ReleaseRT(ref rt, portalCamera);

        rt = new RenderTexture(w, h, depthBufferBits, format)
        {
            name = rtName,
            antiAliasing = Mathf.Max(QualitySettings.antiAliasing, 1),
            useMipMap = false,
            autoGenerateMips = false
        };
        rt.Create();

        portalCamera.targetTexture = rt;

        if (sphereMaterialForRT && !string.IsNullOrEmpty(sphereRTProperty))
            sphereMaterialForRT.SetTexture(sphereRTProperty, rt);

        return true;
    }

    /// <summary>
    /// 使用“当前最新”的 rt 进行绑定（不要从 ctx.portalRT 读取旧值）。
    /// </summary>
    public static void EnsureRTBinding(RenderTexture rt, in RenderPathContext ctx)
    {
        if (!ctx.useRenderTexture || !ctx.portalCamera) return;

        if (ctx.sphereMaterialForRT && !string.IsNullOrEmpty(ctx.sphereRTProperty))
            ctx.sphereMaterialForRT.SetTexture(ctx.sphereRTProperty, rt);

        if (ctx.portalCamera.targetTexture != rt)
            ctx.portalCamera.targetTexture = rt;
    }

    /// <summary>
    /// 原有签名保留：释放 RT 并从相机解绑。
    /// </summary>
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

    /// <summary>
    /// 新增重载：释放时顺便把绑定到材质的属性清空（避免残留引用）。
    /// </summary>
    public static void ReleaseRT(ref RenderTexture rt, Camera portalCamera, Material boundMat, string prop = "_PortalRT")
    {
        if (boundMat && !string.IsNullOrEmpty(prop))
            boundMat.SetTexture(prop, null);

        ReleaseRT(ref rt, portalCamera);
    }

    // ===========================================================
    // Material Handling (Keep RT keyword enabled)
    // ===========================================================
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

    /// <summary>
    /// Utility for multiple sphere visuals: 把 RT 绑定到一批渲染器的材质上
    /// </summary>
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

    // ===========================================================
    // Helpers
    // ===========================================================
    private static string SafeRTProp(string prop) => string.IsNullOrEmpty(prop) ? "_PortalRT" : prop;
}