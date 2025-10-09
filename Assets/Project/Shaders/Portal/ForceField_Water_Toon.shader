Shader "Custom/ForceField_Toon"
{
    Properties
    {
        // ---- 原有 ----
        _Tint("Tint", Color) = (1,1,1,1)
        _Opacity("Opacity", Range(0,1)) = 0.7
        _RampTex("Ramp (Gradient 1xN)", 2D) = "white" {}
        _RampU("Ramp U Offset", Range(0,1)) = 0.0

        _Bands("Bands (>=1)", Range(1,8)) = 3
        _RimBands("Rim Bands", Range(3,10)) = 8

        _FresnelPow("Fresnel Power", Range(0.1,10)) = 6
        _FresnelBoost("Fresnel Boost", Range(0,5)) = 3

        _AppearProgress("Appear 0-1", Range(0,1)) = 1
        _AppearRadius("Appear Radius", Float) = 3
        _AppearSoftness("Edge Softness", Range(0.001,1)) = 0.18
        _AppearCenterWS("Appear Center WS", Vector) = (0,0,0,1)

        _DepthFadeDist("Depth Fade Distance", Range(0.01, 5)) = 3

        _NoiseTex("Noise 2D", 2D) = "gray" {}
        _NoiseTiling("Noise Tiling", Float) = 3
        _NoiseSpeed("Noise Scroll", Float) = 0.5
        _DistortAmount("UV Distort", Range(0,1)) = 0.18
        _NoiseStrength("Noise Strength", Float) = 6.283
        _NoiseInfluence("Noise Influence", Range(0,2)) = 1

        _ScreenDistort("Screen Distort", Range(0,1)) = 0
        _ScreenDistortScale("Screen Distort Scale", Float) = 0.5

        // ---- 反射 & 边缘闪烁 ----
        _ReflectTex("Reflection Cubemap", CUBE) = "" {}
        _ReflectStrength("Reflect Strength", Range(0,1)) = 0.6
        _ReflectNoiseAmt("Reflect Noise Amount", Range(0,0.2)) = 0.03
        _WorldNoiseScale("World Noise Scale", Float) = 0.1
        _WorldNoiseTime("World Noise Time", Float) = 0.05

        _RimColor("Rim Color (a=strength)", Color) = (0.6,0.9,1.0,0.6)
        _SparkleLo("Sparkle Smooth Lo", Range(0,1)) = 0.91
        _SparkleHi("Sparkle Smooth Hi", Range(0,1)) = 0.911
		_SparkleColor("Sparkle Color", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        LOD 200
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "FORCEFIELD_TOON"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_CameraOpaqueTexture);           SAMPLER(sampler_CameraOpaqueTexture);
            TEXTURE2D_X(_CameraDepthTexture);          SAMPLER(sampler_CameraDepthTexture);
            TEXTURE2D(_RampTex);                       SAMPLER(sampler_RampTex);
            TEXTURE2D(_NoiseTex);                      SAMPLER(sampler_NoiseTex);
            TEXTURECUBE(_ReflectTex);                  SAMPLER(sampler_ReflectTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _Tint; float _Opacity;
                float4 _RampTex_ST; float _RampU;

                float _Bands; float _RimBands;
                float _FresnelPow; float _FresnelBoost;

                float _AppearProgress, _AppearRadius, _AppearSoftness; 
				float4 _AppearCenterWS;
                float _DepthFadeDist;

                float4 _NoiseTex_ST; float _NoiseTiling, _NoiseSpeed, _DistortAmount;
                float _NoiseStrength, _NoiseInfluence;

                float _ScreenDistort, _ScreenDistortScale;

                float _ReflectStrength, _ReflectNoiseAmt, _WorldNoiseScale, _WorldNoiseTime;
                float4 _RimColor, _SparkleColor;
                float _SparkleLo, _SparkleHi;
            CBUFFER_END

            struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; float2 uv:TEXCOORD0; };
            struct v2f { float4 vertex:SV_POSITION; float3 posWS:TEXCOORD0; float3 normalWS:TEXCOORD1; float2 uv:TEXCOORD2; float4 screenPos:TEXCOORD3; };

            v2f vert(appdata v){
                v2f o;
                VertexPositionInputs p = GetVertexPositionInputs(v.vertex.xyz);
                VertexNormalInputs n = GetVertexNormalInputs(v.normal);
                o.vertex = p.positionCS;
                o.posWS = p.positionWS;
                o.normalWS = normalize(n.normalWS);
                o.uv = TRANSFORM_TEX(v.uv, _RampTex);
                o.screenPos = p.positionCS;
                return o;
            }

            // 分级
            float Quantize(float x, float bands){
                bands = max(1.0, bands);
                return floor(saturate(x)*bands) / (bands - 1.0 + 1e-4);
            }

            // 深度融合
            float ComputeDepthFade(float4 positionCS, float dist){
                float2 suv = GetNormalizedScreenSpaceUV(positionCS);
                float raw = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, suv).r;
                float scene = Linear01Depth(raw, _ZBufferParams);
                float mine  = Linear01Depth(positionCS.z/positionCS.w, _ZBufferParams);
                return saturate((scene - mine) / dist);
            }

            half4 frag(v2f i):SV_Target
            {
                float3 n = normalize(i.normalWS);
                float3 v = normalize(GetWorldSpaceViewDir(i.posWS));

                // ===== 单次 Fresnel（统一来源）=====
                // 原始：1 - N·V，越刮边越大；提升感知用幂次；再做整体 Boost
                float fresnelRaw = 1.0 - saturate(dot(n, v));
                float fresnelPow = pow(fresnelRaw, _FresnelPow);
                float fresnelBoosted = saturate(fresnelPow * _FresnelBoost);

                // 1) 用它驱动 Toon 的 Ramp（且量化出卡通分层）
                float rimForRamp = Quantize(fresnelBoosted, _RimBands);
                float2 rampUV = float2(saturate(rimForRamp + _RampU), 0.0);
                float3 rampCol = SAMPLE_TEXTURE2D(_RampTex, sampler_RampTex, rampUV).rgb;

                float3 col = _Tint.rgb * rampCol;

                // 2) 用同一个 Fresnel 驱动反射权重（不再单独再算一次）
                // 世界噪声（取代正弦水波）：给反射向量加细微抖动
                float2 wNoiseUV = i.posWS.xy * _WorldNoiseScale + _Time.xx * _WorldNoiseTime;
                float T = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, wNoiseUV).r;

                float3 R = reflect(-v, n);
                R.xy += (T * _ReflectNoiseAmt);
                R = normalize(R);
                float3 reflectCol = SAMPLE_TEXTURECUBE(_ReflectTex, sampler_ReflectTex, R).rgb;

                float reflectW = _ReflectStrength * fresnelPow;  // 单 Fresnel → 反射权重
                col = lerp(col, reflectCol, reflectW);

                // 3) 噪声闪烁点（能量粒子味）
                float sparkle = smoothstep(_SparkleLo, _SparkleHi, T);
                col = lerp(col, _SparkleColor, sparkle);

                // 4) UV 半径描边（与 Cocos 一样的思路）
                float2 d = i.uv - 0.5;
                float rimMask = smoothstep(0.0, 0.3, dot(d,d));
                col = lerp(col, _RimColor.rgb, _RimColor.a * rimMask);

                // 透明度：深度融合
                float alpha = _Opacity * ComputeDepthFade(i.screenPos, _DepthFadeDist);
                return half4(col, alpha);
            }
            ENDHLSL
        }
    }
}