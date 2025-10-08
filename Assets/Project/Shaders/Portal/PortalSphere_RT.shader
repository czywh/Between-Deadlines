Shader "Custom/PortalSphere_RT"
{
    Properties
    {
        _PortalRT   ("Portal RT", 2D) = "black" {}
        _Tint       ("Tint", Color)   = (1,1,1,1)
        _Opacity    ("Opacity", Range(0,1)) = 1
        _EdgeFeather("Edge Feather (0-0.5)", Range(0,0.5)) = 0.08
    }

    SubShader
    {
       
        Tags { 
            "RenderPipeline"="UniversalRenderPipeline"
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
        }

        Cull Back
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // ---- Textures ----
            TEXTURE2D(_PortalRT);
            SAMPLER(sampler_PortalRT);

            // ---- Per-Material (SRP Batcher) ----
            CBUFFER_START(UnityPerMaterial)
                float4 _Tint;
                float  _Opacity;
                float  _EdgeFeather; // 以半径为基准的比例
            CBUFFER_END

            // ---- Global uniforms (由你的控制器全局设置) ----
            float3 _PortalCenter;   
            float  _PortalRadius;   

            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float4 screenPos  : TEXCOORD1; 
            };

            Varyings vert (Attributes v)
            {
                Varyings o;
                float3 posWS = TransformObjectToWorld(v.positionOS);
                o.positionWS = posWS;
                o.positionCS = TransformWorldToHClip(posWS);
                o.screenPos  = ComputeScreenPos(o.positionCS);
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                // 屏幕UV（与主相机匹配）
                float2 uv = i.screenPos.xy / i.screenPos.w;

                // 采样 Portal RT
                float4 col = SAMPLE_TEXTURE2D(_PortalRT, sampler_PortalRT, uv);

                // 世界空间球形遮罩
                float r  = max(_PortalRadius, 0.0001);
                float fe = max(_EdgeFeather * r, 0.0001);
                float d  = distance(i.positionWS, _PortalCenter);

                // mask: 0=外部, 1=内部；在 r±fe 范围平滑过渡
                float mask = saturate(1.0 - smoothstep(r - fe, r + fe, d));

                float4 outCol = col * _Tint;
                outCol.a *= _Opacity * mask;

                return outCol;
            }
            ENDHLSL
        }
    }
}