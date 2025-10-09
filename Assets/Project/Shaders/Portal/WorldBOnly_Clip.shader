Shader "Portal/WorldB_Clip_Unlit"
{
    Properties
    {
        _BaseMap ("Base Texture", 2D) = "white" {}
        _Color   ("Tint", Color) = (1,1,1,1)
        _EdgeSoftness ("Portal Edge Softness", Range(0.01, 2)) = 0.2
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalRenderPipeline"
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite On
        Cull Back


        // Pass 0: Portal Clip Unlit
        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex   vert
            #pragma fragment frag

			#pragma multi_compile_local _ _RENDERPATH_RT _RENDERPATH_CLIP

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"



			TEXTURE2D(_BaseMap);   SAMPLER(sampler_BaseMap);
            TEXTURE2D(_PortalRT);  SAMPLER(sampler_PortalRT);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _Color;
                float  _EdgeSoftness;
            CBUFFER_END

            // —— 由控制器每帧设置的全局参数 —— //
            float4 _PortalCenter;           // xyz=中心（世界空间）
            float  _PortalRadius;           // 半径
            float  _UsePortalDistanceFade;  // 0=硬边，1=软边

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS  : TEXCOORD0;
                float2 uv          : TEXCOORD1;
				float2 uvScreen    : TEXCOORD2;
            };



            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                float3 posWS     = TransformObjectToWorld(IN.positionOS);
                float4 posHCS    = TransformWorldToHClip(posWS);
                
				OUT.positionHCS  = posHCS;
                OUT.positionWS   = posWS;
                OUT.uv           = TRANSFORM_TEX(IN.uv, _BaseMap);


				// RT Path: uv screen [0 ... 1]
				float2 ndc = posHCS.xy / max(posHCS.w, 1e-5);
                OUT.uvScreen = ndc * 0.5f + 0.5f;
                return OUT;
            }

			// PortalMask: return mask coefficient [0 ... 1]
            float PortalMask(float3 worldPos, float radius,  float edgeSoft)
            {
                float d = distance(worldPos, _PortalCenter.xyz);
                if (_UsePortalDistanceFade < 0.5)
                {
                    // hard Clip (d < radius: 1; d > radius: 0)
                    return step(d, radius);
                }

				// apply soft clip within radius +/- edgeSoft
                float edgeStart = radius - edgeSoft;
                float edgeEnd   = radius + edgeSoft;
                return 1.0 - smoothstep(edgeStart, edgeEnd, d);
            }

            half4 frag (Varyings IN) : SV_Target
            {
                // Mask Clip 
                float maskCoefficient = PortalMask(IN.positionWS, _PortalRadius, _EdgeSoftness);
                if (maskCoefficient <= 0.001) discard;

				half4 col;

				#if defined(_RENDERPATH_RT)
                    // RT Path: Render the object on the sphere RT (Camera outside of portal sphere)
	                col = SAMPLE_TEXTURE2D(_PortalRT, sampler_PortalRT, IN.uvScreen) * _Color;   

                #elif defined(_RENDERPATH_CLIP)
                    // Clip Path: Render the object directly (Camera inside of portal sphere)
                    col = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _Color;
                #else
                    // default: Clip Path 
                    col = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _Color;
                   
                #endif

				col.a *= maskCoefficient;
				return col;
            }
            ENDHLSL
        }
    }

    FallBack Off
}