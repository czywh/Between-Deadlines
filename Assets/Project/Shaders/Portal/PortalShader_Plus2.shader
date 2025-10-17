Shader "Custom/PortalSphere_RT_Toon_Simplified_ShakeDual"
{
    Properties
    {
        _Tint       ("Tint", Color)   = (1,1,1,1)
        _Opacity    ("Opacity", Range(0,1)) = 1

        // Toon Ramp
        _RampTex("Ramp (Gradient 1xN)", 2D) = "white" {}
        _RampU("Ramp U Offset", Range(0,1)) = 0.0
        _RimBands("Rim Bands", Range(1,10)) = 8

        _FresnelPow("Fresnel Power", Range(0.1,10)) = 6
        _FresnelBoost("Fresnel Boost", Range(0,5)) = 2.5
        _DepthFadeDist("Depth Fade Distance", Range(0.01, 5)) = 3

        _NoiseTex("Noise 2D", 2D) = "gray" {}
        _PortalRT("Portal RT", 2D) = "black" {}

        _PortalColorBlend ("Portal Blend", Range(0,1)) = 1
        _EdgeFeather("Edge Feather (world units)", Range(0,2)) = 0.08

        // 顶点波动参数
        _WaveAmp("Wave Amplitude (m)", Range(0,0.2)) = 0.05
        _WaveFreq("Wave Frequency", Range(0.1,10)) = 3
        _WaveSpeed("Wave Speed", Range(0,10)) = 2
        _WaveDir1("Wave Dir1 (x,z)", Vector) = (1,0,0,0)
        _WaveDir2("Wave Dir2 (x,z)", Vector) = (0.3,0,0.95,0)
        _WaveSizeComp("Wave Size Compensation", Range(-2,2)) = 1

        // 球内视角抖动（以像素为单位的幅度 + 强度）
        _InsideShakeStrength ("Inside Shake Strength", Range(0,1)) = 0.3
        _ShakePixels ("Inside Shake Pixels", Range(0,16)) = 4
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
            Name "PORTAL_MAIN"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // Textures
            TEXTURE2D_X(_CameraDepthTexture);  SAMPLER(sampler_CameraDepthTexture);
            TEXTURE2D(_RampTex);               SAMPLER(sampler_RampTex);
            TEXTURE2D(_NoiseTex);              SAMPLER(sampler_NoiseTex);
            TEXTURE2D(_PortalRT);              SAMPLER(sampler_PortalRT);

            // Clip 路径需要 Opaque Texture（URP Asset 勾选）
            #if defined(_RENDERPATH_CLIP)
            TEXTURE2D(_CameraOpaqueTexture);   SAMPLER(sampler_CameraOpaqueTexture);
            float4 _CameraOpaqueTexture_TexelSize;
            #endif

            // RT 路径会自动有 _PortalRT_TexelSize（Unity 自动注入）
            float4 _PortalRT_TexelSize;

            CBUFFER_START(UnityPerMaterial)
                float4 _Tint; float _Opacity;

                float4 _RampTex_ST; float _RampU;
                float _RimBands;

                float _FresnelPow; float _FresnelBoost;
                float _DepthFadeDist;

                float _PortalColorBlend; float _EdgeFeather;

                // Wave
                float _WaveAmp, _WaveFreq, _WaveSpeed;
                float4 _WaveDir1, _WaveDir2;
                float _WaveSizeComp;

                // Shake
                float _InsideShakeStrength;
                float _ShakePixels;
            CBUFFER_END

            // Global (from C#)
            float4 _PortalCenter;   // xyz
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
                float4 screenPos  : TEXCOORD2;
                float2 uvRamp     : TEXCOORD3;
                float  fresnelRaw : TEXCOORD4;
            };

            float DepthFade(float4 posCS, float dist) {
                float2 suv = (ComputeScreenPos(posCS).xy / posCS.w);
                float raw = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, suv).r;
                float scene = Linear01Depth(raw, _ZBufferParams);
                float mine  = Linear01Depth(posCS.z/posCS.w, _ZBufferParams);
                return saturate((scene - mine) / dist);
            }

            Varyings vert (Attributes v)
            {
                Varyings o;
                float3 posWS = TransformObjectToWorld(v.positionOS);
                float3 nWS   = normalize(TransformObjectToWorldNormal(v.normalOS));

                // 顶点波动（带半径补偿）
                float radius = max(_PortalRadius, 0.001);
                float2 xz = posWS.xz * pow(radius, -_WaveSizeComp);
                float2 d1 = normalize(_WaveDir1.xz);
                float2 d2 = normalize(_WaveDir2.xz);
                float t = _Time.y * _WaveSpeed;

                float s1 = sin(dot(xz, d1) * _WaveFreq + t);
                float s2 = sin(dot(xz, d2) * (_WaveFreq * 1.18) + t * 1.3);
                float wave = (s1 + s2) * 0.5;
                posWS += nWS * (wave * _WaveAmp);

                // 法线近似修正（减弱假阴影）
                float eps = 0.03;
                float hX = sin(dot(xz + d1*eps, d1) * _WaveFreq + t);
                float hY = sin(dot(xz + d2*eps, d2) * _WaveFreq + t);
                float3 tangent = normalize(float3(1, (hX-wave), 0));
                float3 bitangent = normalize(float3(0, (hY-wave), 1));
                nWS = normalize(lerp(nWS, normalize(cross(tangent, bitangent)), 0.4));

                o.positionWS = posWS;
                o.normalWS   = nWS;
                o.positionCS = TransformWorldToHClip(posWS);
                o.screenPos  = ComputeScreenPos(o.positionCS);
                o.uvRamp     = TRANSFORM_TEX(v.uv, _RampTex);

                float3 vdir = normalize(GetWorldSpaceViewDir(posWS));
                o.fresnelRaw = 1 - saturate(dot(nWS, vdir));
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                float3 n = normalize(i.normalWS);
                float3 v = normalize(GetWorldSpaceViewDir(i.positionWS));

                // Fresnel → toon ramp
                float fres = pow(i.fresnelRaw, _FresnelPow) * _FresnelBoost;
                float rim  = saturate(fres);
                float2 rampUV = float2(saturate(rim + _RampU), 0);
                float3 rampCol = SAMPLE_TEXTURE2D(_RampTex, sampler_RampTex, rampUV).rgb;
                float3 baseCol = _Tint.rgb * rampCol;

                // 屏幕 UV
                float2 uvScreen = i.screenPos.xy / i.screenPos.w;

                // inside 判定
                float camDist = distance(_WorldSpaceCameraPos.xyz, _PortalCenter.xyz);
                float inside  = step(camDist, _PortalRadius);

                // 噪声
                float2 nUV = i.positionWS.xz * 0.6 + _Time.yy * 0.6;
                float2 nrg = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, nUV).rg * 2 - 1;

                // 按“像素”换算的抖动偏移（两条渲染路径取不同源 & texel size）
                float2 texel;
                float2 shake;
                float3 srcCol;

                #if defined(_RENDERPATH_CLIP)
                    texel = _CameraOpaqueTexture_TexelSize.xy;
                    shake = nrg * (_InsideShakeStrength * _ShakePixels) * texel * inside;
                    srcCol = SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, uvScreen + shake).rgb;
                #else
                    texel = _PortalRT_TexelSize.xy;
                    shake = nrg * (_InsideShakeStrength * _ShakePixels) * texel * inside;
                    srcCol = SAMPLE_TEXTURE2D(_PortalRT, sampler_PortalRT, uvScreen + shake).rgb;
                #endif

                // 球形柔边
                float r = max(_PortalRadius, 0.0001);
                float featherW = max(_EdgeFeather, 0.0001);
                float d = distance(i.positionWS, _PortalCenter.xyz);
                float mask = saturate(1 - smoothstep(r - featherW, r + featherW, d));

                // 混合
                float w = _PortalColorBlend * mask;
                float3 finalCol = lerp(baseCol, srcCol, w);

                // 内向更透明
                float alpha = _Opacity * lerp(0.3f, 1.0f, saturate(dot(n, v)));
                if (_UsePortalDistanceFade > 0.5)
                    alpha *= DepthFade(i.positionCS, _DepthFadeDist);
                alpha = max(alpha, w);

                return float4(finalCol, alpha);
            }
            ENDHLSL
        }
    }
}