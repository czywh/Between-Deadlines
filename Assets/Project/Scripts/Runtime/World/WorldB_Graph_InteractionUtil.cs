using UnityEngine;
using System.Collections.Generic;
public enum RenderPath { RT, Clip }

public struct RenderPathContext
{
	public Camera mainCamera;              // Main Camera 
	public Camera portalCamera;            // Portal Camera  (World B Only）
	public int worldBLayer;                // World B Only Layer（LayerMask）

	public LayerMask portalCameraCulling;  // Portal 相机的 cullingMask（只包含 WorldBOnly）
	public string sphereRTProperty;        // name of Render Texture "_PortalRT"
	public RenderTexture portalRT;         // RT 对象（若为 null，主流程里创建）
	public Material sphereMaterialForRT;   // 球体材质（用于采样 RT）
	public float rtResolutionScale; 		// Resolution Scale ratio for RT 
	public bool useDistanceFade;           
	public bool useRenderTexture;  
}



public static class WorldB_Graph_InteractionUtil
{
	/// <summary>
	/// For every world B only Objects, its interatable only if it is in the sphere 
	/// Changing corresponding colliders/rigidbodies for world B objects 
	/// </summary>
	public static void SetInteractive(Transform root, bool interactive)
	{
		if (!root) return;

		var colliders = root.GetComponentsInChildren<Collider>(includeInactive: true);
		var rigidbodies = root.GetComponentsInChildren<Rigidbody>(includeInactive: true);

		// Colliders updating 
		foreach (var c in colliders)
		{
			if (!c) continue;
			c.enabled = true;

			// non-interatable -> is Trigger 
			c.isTrigger = !interactive;
		}

		// Rigidbodies updating 
		foreach (var rb in rigidbodies)
		{
			if (!rb) continue;

			rb.isKinematic = !interactive;
			if (!interactive)
			{
				// non-interatable -> None physical effects
				rb.linearVelocity = Vector3.zero;
				rb.angularVelocity = Vector3.zero;
				rb.Sleep();
			}
			else rb.WakeUp();
		}
	}

	// Call this function Only when World B is actived 
	public static RenderPath UpdatePathByCameraInside(bool cameraInsideSphere, RenderPath current, ref RenderTexture portalRT, in RenderPathContext ctx)
	{
		var path = cameraInsideSphere ? RenderPath.Clip : RenderPath.RT;

		// apply new render path 
		if (path != current)
		{
			int WorldBOnlyLayer = 1 << ctx.worldBLayer;

			if (path == RenderPath.RT) // Render Texture Method. (Camera is ouside of Portal Sphere.)
			{
				// main camera will not render "World B only Layer " Objects
				ctx.mainCamera.cullingMask &= ~WorldBOnlyLayer;

				// Enable Portal Camera to sampling “World B only Layer” RT
				ctx.portalCamera.enabled = ctx.useRenderTexture;
				ctx.portalCamera.cullingMask = ctx.portalCameraCulling;
				EnsureRTBinding(in ctx);
			}
			else  // Clip Method  (Camera is inside of Portal Sphere.)
			{
				// main Camera render World A + world B, close portal camer 
				ctx.mainCamera.cullingMask |= WorldBOnlyLayer;
				ctx.portalCamera.enabled = false;

				ClearRTBinding(ctx.portalCamera, ctx.sphereMaterialForRT, ctx.sphereRTProperty, clearMat: false);
			}
		}

		else if (path == RenderPath.RT && (portalRT == null || ctx.portalCamera == null || ctx.portalCamera.targetTexture == null))
		{
			// ★ 路径没变的兜底：一开始就是 RT，但还没创建/绑定时，要创建并绑定
			
			// reconstract render texure 
			CheckOrRecreateRT( ref portalRT, ctx.portalCamera, ctx.useRenderTexture, Mathf.Clamp01(ctx.rtResolutionScale <= 0 ? 1f : ctx.rtResolutionScale), ctx.sphereMaterialForRT,
				string.IsNullOrEmpty(ctx.sphereRTProperty) ? "_PortalRT" : ctx.sphereRTProperty);

			// give render texture to portal 
			if (ctx.portalCamera.targetTexture != portalRT)
				ctx.portalCamera.targetTexture = portalRT;


			// maker sure portal camera is enabled 
			ctx.portalCamera.enabled = true;
		}
		


		return path;
	}

	public static void ApplyMaterialPath(Transform worldBOnlyRoot, int worldBLayer, RenderPath path)
	{
		if (!worldBOnlyRoot) return;

		foreach (var r in worldBOnlyRoot.GetComponentsInChildren<Renderer>(true))
		{
			if (!r || r.gameObject.layer != worldBLayer) continue;

			var mats = r.sharedMaterials;
			for (int i = 0; i < mats.Length; i++)
			{
				var m = mats[i];
				if (!m) continue;

				if (path == RenderPath.RT)
					EnableRTPath(m);
				else
					EnableClipPath(m);
			}
		}
	}

	public static bool CheckOrRecreateRT(ref RenderTexture rt, Camera portalCamera, bool useRenderTexture, float resolutionScale, Material sphereMaterialForRT = null,
		string sphereRTProperty = "_PortalRT", string rtName = "PortalSphere_RT", RenderTextureFormat format = RenderTextureFormat.ARGB32, int depthBufferBits = 16
	)
	{
		if (!useRenderTexture || !portalCamera)
		{
			if (portalCamera && portalCamera.targetTexture != null)
				portalCamera.targetTexture = null;
			return false;
		}

		int w = Mathf.Max(1, Mathf.RoundToInt(Screen.width * resolutionScale));
		int h = Mathf.Max(1, Mathf.RoundToInt(Screen.height * resolutionScale));

		bool needRecreate = (rt == null || rt.width != w || rt.height != h || portalCamera.targetTexture != rt);


		if (!needRecreate)
		{
			if (portalCamera.targetTexture != rt)
				portalCamera.targetTexture = rt;

			if (sphereMaterialForRT && !string.IsNullOrEmpty(sphereRTProperty))
				sphereMaterialForRT.SetTexture(sphereRTProperty, rt);

			return false;
		}

		// Re-create render texture 
		ReleaseRT(ref rt, portalCamera);

		rt = new RenderTexture(w, h, depthBufferBits, format) { name = rtName, antiAliasing = Mathf.Max(QualitySettings.antiAliasing, 1),
						useMipMap = false, autoGenerateMips = false };

		rt.Create();
		portalCamera.targetTexture = rt;

		if (sphereMaterialForRT && !string.IsNullOrEmpty(sphereRTProperty))
			sphereMaterialForRT.SetTexture(sphereRTProperty, rt);

		return true;
	}



	public static void BindRTToSphereMaterials(IEnumerable<Renderer> renderers, RenderTexture rt, string propertyName = "_PortalRT", bool keepWhenNull = true)
	{
		if (renderers == null || string.IsNullOrEmpty(propertyName)) return;

		foreach (var r in renderers)
		{
			if (!r) continue;
			var mats = r.sharedMaterials;
			for (int i = 0; i < mats.Length; i++)
			{
				var m = mats[i];
				if (!m) continue;

				if (rt)
				{
					m.SetTexture(propertyName, rt);
				}
				else
				{
					if (!keepWhenNull)
						m.SetTexture(propertyName, null);
				}
			}
		}
	}



	// Enable RT Method (World B active, camera outside of portal sphere)
	public static void EnableRTPath(Material mat)
	{
		mat.EnableKeyword("_RENDERPATH_RT");
		mat.DisableKeyword("_RENDERPATH_CLIP");
	}

	// Enable Clip Method (World B active, camera inside of portal sphere)
	public static void EnableClipPath(Material mat)
	{
		mat.EnableKeyword("_RENDERPATH_CLIP");
		mat.DisableKeyword("_RENDERPATH_RT");
	}

	public static void EnsureRTBinding(in RenderPathContext ctx)
	{
		if (!ctx.useRenderTexture || !ctx.portalCamera) return;

		if (ctx.sphereMaterialForRT && !string.IsNullOrEmpty(ctx.sphereRTProperty) && ctx.portalRT)
		{
			// 绑定到材质属性
			ctx.sphereMaterialForRT.SetTexture(ctx.sphereRTProperty, ctx.portalRT);
		}

		// 绑定到 Portal 相机
		if (ctx.portalCamera.targetTexture != ctx.portalRT)
			ctx.portalCamera.targetTexture = ctx.portalRT;
	}
	
	 public static void ClearRTBinding(Camera portalCamera, Material sphereMaterialForRT = null, string sphereRTProperty = "_PortalRT", bool clearMat = false)
	{
		if (portalCamera && portalCamera.targetTexture != null)
			portalCamera.targetTexture = null;

		if (clearMat && sphereMaterialForRT && !string.IsNullOrEmpty(sphereRTProperty))
			sphereMaterialForRT.SetTexture(sphereRTProperty, null);
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
}