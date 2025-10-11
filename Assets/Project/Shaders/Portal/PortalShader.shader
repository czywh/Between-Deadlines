Shader "Custom/PortalSphere"
{
    Properties
    {
        _Tint       ("Tint", Color)   = (1,1,1,1)
        _Opacity    ("Opacity", Range(0,1)) = 1

        _RampTex("Ramp (Gradient 1xN)", 2D) = "white" {}
        _RampU("Ramp U Offset", Range(0,1)) = 0.0
        _RimBands("Rim Bands", Range(1,10)) = 8

        _FresnelPow("Fresnel Power", Range(0.1,10)) = 6
        _FresnelBoost("Fresnel Boost", Range(0,5)) = 3

        _DepthFadeDist("Depth Fade Distance", Range(0.01, 5)) = 3

        _NoiseTex("Noise 2D", 2D) = "gray" {}
        _ReflectTex("Reflection Cubemap", CUBE) = "" {}
        _ReflectStrength("Reflect Strength", Range(0,1)) = 0.6
        _ReflectNoiseAmt("Reflect Noise Amount", Range(0,0.2)) = 0.03
        _WorldNoiseScale("World Noise Scale", Float) = 0.1
        _WorldNoiseTime("World Noise Time", Float) = 0.05

        _RimColor("Rim Color (a=strength)", Color) = (0.6,0.9,1.0,0.6)
        _SparkleLo("Sparkle Smooth Lo", Range(0,1)) = 0.91
        _SparkleHi("Sparkle Smooth Hi", Range(0,1)) = 0.911
        _SparkleColor("Sparkle Color", Color) = (1,1,1,1)

        _PortalRT   ("Portal RT", 2D) = "black" {}
        _PortalColorBlend ("Portal Blend", Range(0,1)) = 1
        _EdgeFeather("Edge Feather (world units)", Range(0,0.5)) = 0.08
    }

    SubShader
    {
        Tags {
            "RenderPipeline"="UniversalRenderPipeline"
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
        }

        Cull Off
        ZWrite Off
        ZTest LEqual
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "FORCEFIELD_TOON_PORTAL_RT"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // 
            TEXTURE2D_X(_CameraDepthTexture);  SAMPLER(sampler_CameraDepthTexture);

            // Texture Sampleing 
            TEXTURE2D(_RampTex);      SAMPLER(sampler_RampTex);
            TEXTURE2D(_NoiseTex);     SAMPLER(sampler_NoiseTex);
            TEXTURECUBE(_ReflectTex); SAMPLER(sampler_ReflectTex);
            TEXTURE2D(_PortalRT);     SAMPLER(sampler_PortalRT);

            // ---- Per-Material ----
            CBUFFER_START(UnityPerMaterial)
                float4 _Tint; float _Opacity;

                float4 _RampTex_ST; float _RampU;
                float _RimBands;

                float _FresnelPow; float _FresnelBoost;

                float _DepthFadeDist;

                float _ReflectStrength, _ReflectNoiseAmt, _WorldNoiseScale, _WorldNoiseTime;

                float4 _RimColor, _SparkleColor;
                float _SparkleLo, _SparkleHi;

                float _PortalColorBlend;
                float _EdgeFeather;
            CBUFFER_END

            // Portal Sphere's globl information 
            float4 _PortalCenter;   
            float  _PortalRadius;
            float  _UsePortalDistanceFade;

            struct Attributes {
                float3 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float2 uvRamp     : TEXCOORD2;
                float4 screenPos  : TEXCOORD3; 
            };

            Varyings vert (Attributes v)
            {
                Varyings o;
                float3 posWS = TransformObjectToWorld(v.positionOS);
                float3 nWS   = TransformObjectToWorldNormal(v.normalOS);
                o.positionWS = posWS;
                o.normalWS   = normalize(nWS);
                o.positionCS = TransformWorldToHClip(posWS);
                o.screenPos  = ComputeScreenPos(o.positionCS);   
                o.uvRamp     = TRANSFORM_TEX(v.uv, _RampTex);
                return o;
            }

            
            float Quantize01(float x, float bands){
                bands = max(1.0, bands);
                float sx = saturate(x);
                if (bands <= 1.0001) return sx;
                float steps = bands - 1.0;
                return floor(sx * steps + 0.5) / steps;
            }

            float DepthFade(float4 positionCS, float dist){
                float2 suv  = (ComputeScreenPos(positionCS).xy / ComputeScreenPos(positionCS).w);
                float raw   = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, suv).r;
                float scene = Linear01Depth(raw, _ZBufferParams);
                float mine  = Linear01Depth(positionCS.z/positionCS.w, _ZBufferParams);
                return saturate((scene - mine) / dist);
            }

            half4 frag (Varyings i) : SV_Target
            {
                // ===== 基础 toon+反射（作为 fallback/边缘色）=====
                float3 n = normalize(i.normalWS);
                float3 v = normalize(GetWorldSpaceViewDir(i.positionWS));

                float fresnelRaw     = 1.0 - saturate(dot(n, v));
                float fresnelPow     = pow(fresnelRaw, _FresnelPow);
                float fresnelBoosted = saturate(fresnelPow * _FresnelBoost);

                float rimForRamp = Quantize01(fresnelBoosted, _RimBands);
                float2 rampUV    = float2(saturate(rimForRamp + _RampU), 0.0);
                float3 rampCol   = SAMPLE_TEXTURE2D(_RampTex, sampler_RampTex, rampUV).rgb;
                float3 col       = _Tint.rgb * rampCol;

                float2 wNoiseUV  = i.positionWS.xy * _WorldNoiseScale + _Time.xx * _WorldNoiseTime;
                float  Tnoise    = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, wNoiseUV).r;

                float3 R         = reflect(-v, n);
                R.xy            += (Tnoise * _ReflectNoiseAmt);
                float3 reflectCol= SAMPLE_TEXTURECUBE(_ReflectTex, sampler_ReflectTex, normalize(R)).rgb;
                col              = lerp(col, reflectCol, _ReflectStrength * fresnelPow);
 

                // ===== Portal RT 采样（与工作版本一致的屏幕UV）=====
                float2 uvScreen  = i.screenPos.xy / i.screenPos.w;     
                float3 rtCol     = SAMPLE_TEXTURE2D(_PortalRT, sampler_PortalRT, uvScreen).rgb;

                // 世界空间球形遮罩（r 内为 1，边缘 feather 过渡）
                float r          = max(_PortalRadius, 0.0001);
                float featherW   = max(_EdgeFeather, 0.0001);         
                float dWS        = distance(i.positionWS, _PortalCenter.xyz);
                float mask       = saturate(1.0 - smoothstep(r - featherW, r + featherW, dWS));

                // 用 RT 覆盖 toon：由 _PortalColorBlend 控总强度
                float w          = _PortalColorBlend * mask;
                float3 finalCol  = lerp(col, rtCol, w);

                // 透明度（确保门户区域不被“吃掉”）
                float alpha = _Opacity;
                if (_UsePortalDistanceFade > 0.5)
                    alpha *= DepthFade(i.positionCS, _DepthFadeDist);

                // 提升门户区域可见性
                alpha = max(alpha, w);

                return half4(finalCol, alpha);
            }
            ENDHLSL
        }
    }
}