#pragma warning disable CS0672
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Portal stencil writer feature:
/// - Draws objects (usually the portal sphere) with an override material that ONLY writes Stencil=1.
/// - World-B materials then set Stencil { Ref 1, Comp Equal, Pass Keep } to render only inside the portal.
/// </summary>
public class PortalStencilFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public Material stencilMaskMaterial;

        public LayerMask layerMask = ~0;

        public RenderPassEvent evt = RenderPassEvent.BeforeRenderingOpaques;

        public string[] shaderPassNames = new[] { "UniversalForward", "SRPDefaultUnlit" };

        public RenderQueueRange queueRange = RenderQueueRange.all;
    }

    class PortalStencilPass : ScriptableRenderPass
    {
        private readonly string _profilerTag = "Portal Stencil Mask";
        private readonly Material _stencilMat;
        private readonly FilteringSettings _filteringSettings;
        private readonly ShaderTagId[] _shaderTagIds;

        public PortalStencilPass(Settings settings)
        {
            _stencilMat = settings.stencilMaskMaterial;
            _filteringSettings = new FilteringSettings(settings.queueRange, settings.layerMask);
            renderPassEvent = settings.evt;

            if (settings.shaderPassNames != null && settings.shaderPassNames.Length > 0)
            {
                _shaderTagIds = new ShaderTagId[settings.shaderPassNames.Length];
                for (int i = 0; i < settings.shaderPassNames.Length; i++)
                    _shaderTagIds[i] = new ShaderTagId(settings.shaderPassNames[i]);
            }
            else
            {
                _shaderTagIds = new[] { new ShaderTagId("UniversalForward"), new ShaderTagId("SRPDefaultUnlit") };
            }
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (_stencilMat == null)
                return;

            // Build DrawingSettings with our shader tags
            var sorting = SortingCriteria.CommonOpaque;
            var drawingSettings = CreateDrawingSettings(_shaderTagIds[0], ref renderingData, sorting);
            for (int i = 1; i < _shaderTagIds.Length; i++)
                drawingSettings.SetShaderPassName(i, _shaderTagIds[i]);

            // Override material to WRITE stencil (see PortalStencilMask shader)
            drawingSettings.overrideMaterial = _stencilMat;
            drawingSettings.overrideMaterialPassIndex = 0;

            var cmd = CommandBufferPool.Get(_profilerTag);
            using (new ProfilingScope(cmd, new ProfilingSampler(_profilerTag)))
            {
#if UNITY_2022_2_OR_NEWER
                // New URP path: RendererList API
                var rlp = new RendererListParams(renderingData.cullResults, drawingSettings, _filteringSettings);
                var rl  = context.CreateRendererList(ref rlp);
                cmd.DrawRendererList(rl);
#else
                // Legacy URP path
                context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref _filteringSettings);
#endif
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }

    public Settings settings = new Settings();
    private PortalStencilPass _pass;

    public override void Create()
    {
        _pass = new PortalStencilPass(settings);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (settings.stencilMaskMaterial == null)
            return;

        // We don't need camera color target for stencil-only writing.
        renderer.EnqueuePass(_pass);
    }
}
#pragma warning restore CS0672