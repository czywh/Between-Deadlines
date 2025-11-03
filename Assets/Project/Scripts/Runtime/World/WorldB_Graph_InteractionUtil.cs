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
		if (ctx.mainCamera == null || ctx.portalCamera == null)
		{

			ReleaseRT(ref portalRT, ctx.portalCamera, ctx.sphereMaterialForRT, SafeRTProp(ctx.sphereRTProperty));
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
			SafeRTProp(ctx.sphereRTProperty)
		);

		EnsureRTBinding(portalRT, in ctx);

		return RenderPath.RT;
	}

    // Check or rebuild RT. 
	// If useRenderTexture == false or the camera is invalid, release and unbind it
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
        if (!useRenderTexture || !portalCamera)
        {
            if (sphereMaterialForRT && !string.IsNullOrEmpty(sphereRTProperty))
                sphereMaterialForRT.SetTexture(sphereRTProperty, null);

            ReleaseRT(ref rt, portalCamera);
            return false;
        }

        int w = Mathf.Max(1, Mathf.RoundToInt(Screen.width * resolutionScale));
        int h = Mathf.Max(1, Mathf.RoundToInt(Screen.height * resolutionScale));

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

    public static void EnsureRTBinding(RenderTexture rt, in RenderPathContext ctx)
    {
        if (!ctx.useRenderTexture || !ctx.portalCamera) return;

        if (ctx.sphereMaterialForRT && !string.IsNullOrEmpty(ctx.sphereRTProperty))
            ctx.sphereMaterialForRT.SetTexture(ctx.sphereRTProperty, rt);

        if (ctx.portalCamera.targetTexture != rt)
            ctx.portalCamera.targetTexture = rt;
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
        if (boundMat && !string.IsNullOrEmpty(prop))
            boundMat.SetTexture(prop, null);

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
                    m.SetTexture(propertyName, rt);
                else if (!keepWhenNull)
                    m.SetTexture(propertyName, null);
            }
        }
    }


    private static string SafeRTProp(string prop) => string.IsNullOrEmpty(prop) ? "_PortalRT" : prop;
}