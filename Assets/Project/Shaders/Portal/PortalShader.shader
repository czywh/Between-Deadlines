Shader "Custom/PortalSphere"
{
    Properties
    {
        // ---- Base look ----
        _Tint       ("Tint", Color)   = (1,1,1,1)
        _Opacity    ("Opacity", Range(0,1)) = 1

        _RampTex("Ramp (Gradient 1xN)", 2D) = "white" {}
        _RampU("Ramp U Offset", Range(0,1)) = 0.0
        _RimBands("Rim Bands", Range(1,10)) = 8

        _FresnelPow("Fresnel Power", Range(0.1,10)) = 6
        _FresnelBoost("Fresnel Boost", Range(0,5)) = 3

        _DepthFadeDist("Depth Fade Distance", Range(0.01, 5)) = 3

        // ---- Reflection / Noise ----
        _NoiseTex("Noise 2D", 2D) = "gray" {}
        _ReflectTex("Reflection Cubemap", CUBE) = "" {}
        _ReflectStrength("Reflect Strength", Range(0,1)) = 0.6
        _ReflectNoiseAmt("Reflect Noise Amount", Range(0,0.2)) = 0.03
        _WorldNoiseScale("World Noise Scale", Float) = 0.1
        _WorldNoiseTime("World Noise Time", Float) = 0.05

        // ---- Rim color & sparkles ----
        _RimColor("Rim Color (a=strength)", Color) = (0.6,0.9,1.0,0.6)
        _SparkleLo("Sparkle Smooth Lo", Range(0,1)) = 0.91
        _SparkleHi("Sparkle Smooth Hi", Range(0,1)) = 0.911
        _SparkleColor("Sparkle Color", Color) = (1,1,1,1)
        _SparkleIntensity("Sparkle Intensity", Range(0, 2)) = 1

        // ---- Portal render target ----
        _PortalRT   ("Portal RT", 2D) = "black" {}
        _PortalColorBlend ("Portal Blend", Range(0,1)) = 1
        _EdgeFeather("Edge Feather (world units)", Range(0,0.5)) = 0.08

        // ---- Additional art controls ----
        _InnerFogColor("Inner Fog Color", Color) = (0.25,0.55,0.9,1)
        _InnerFogDensity("Inner Fog Density", Range(0, 6)) = 1.25
        _RimSoftAdd("Soft Add Rim", Range(0,1)) = 0.65

        _DistortStrength("RT Distortion Strength", Range(0,0.05)) = 0.02
        _DistortFreq("RT Distortion Frequency", Range(0.1,10)) = 2
        _ChromaticAberration("Chromatic Aberration", Range(0,1)) = 0.25

        // ---- Hit ripple (script-driven) ----
        _HitCenter("Hit Center (WS)", Vector) = (0,0,0,0)
        _HitTime("Hit Time", Float) = 0
        _HitAmp("Hit Amplitude", Range(0,0.5)) = 0.08
        _HitFreq("Hit Frequency", Range(0,30)) = 12
        _HitDecay("Hit Decay", Range(0,5)) = 1.2

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

            // Textures
            TEXTURE2D_X(_CameraDepthTexture);  SAMPLER(sampler_CameraDepthTexture);
            TEXTURE2D(_RampTex);      SAMPLER(sampler_RampTex);
            TEXTURE2D(_NoiseTex);     SAMPLER(sampler_NoiseTex);
            TEXTURECUBE(_ReflectTex); SAMPLER(sampler_ReflectTex);
            TEXTURE2D(_PortalRT);     SAMPLER(sampler_PortalRT);

            // Per-Material
            CBUFFER_START(UnityPerMaterial)
                float4 _Tint; float _Opacity;

                float4 _RampTex_ST; float _RampU;
                float _RimBands;

                float _FresnelPow; float _FresnelBoost;
                float _DepthFadeDist;

                float _ReflectStrength, _ReflectNoiseAmt, _WorldNoiseScale, _WorldNoiseTime;

                float4 _RimColor, _SparkleColor;
                float _SparkleLo, _SparkleHi;
                float _SparkleIntensity;

                float _PortalColorBlend;
                float _EdgeFeather;

                float4 _InnerFogColor; float _InnerFogDensity; float _RimSoftAdd;
                float _DistortStrength; float _DistortFreq; float _ChromaticAberration;

                float4 _HitCenter; float _HitTime; float _HitAmp; float _HitFreq; float _HitDecay;

                // Ambient ripple
                float _AmbientRippleOn; float _AmbientRippleAmp; float _AmbientRippleFreq; float _AmbientRippleSpeed; float _AmbientRippleTiling;
            CBUFFER_END

            // Portal sphere global info (set from script)
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
                float3 viewDirWS  : TEXCOORD4;
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
                o.viewDirWS  = GetWorldSpaceViewDir(posWS);
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
                return saturate((scene - mine) / max(dist, 1e-5));
            }

            // Two-channel scrolling noise (cheap FBM)
            float2 FlowNoise(float3 ws, float scale, float time, float freq){
                float2 uv1 = ws.xy * scale + float2( time * 0.11 * freq, time * 0.07 * freq);
                float2 uv2 = ws.zy * (scale*1.73) + float2(-time * 0.09 * freq, time * 0.13 * freq);
                float n1 = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, uv1).r;
                float n2 = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, uv2).g;
                return float2(n1, n2) * 2 - 1; // [-1,1]
            }

            // Hit ripple in WS around _HitCenter
            float Ripple(float3 wsPos){
                float d = distance(wsPos, _HitCenter.xyz);
                float phase = max(_HitTime, 0.0) * _HitFreq - d * _HitFreq; // outward wave
                float envelope = exp(-d * _HitDecay);
                return sin(phase) * envelope * _HitAmp;
            }

            // Always-on ambient ripple over the sphere surface using spherical coordinates
            float AmbientRipple(float3 wsPos){
                float3 p = wsPos - _PortalCenter.xyz;
                float r = max(length(p), 1e-4);
                float theta = acos(saturate(p.y / r));                 // [0,pi]
                float phi = atan2(p.z, p.x);                            // [-pi,pi]
                float t = _Time.x * _AmbientRippleSpeed;
                float wA = sin(theta * _AmbientRippleTiling - t * _AmbientRippleFreq);
                float wB = sin((phi + theta) * 0.5 * _AmbientRippleTiling - t * _AmbientRippleFreq * 1.31);
                return (wA + wB) * 0.5 * _AmbientRippleAmp;
            }

            half4 frag (Varyings i) : SV_Target
            {
                float3 n = normalize(i.normalWS);
                float3 v = normalize(i.viewDirWS);

                // Fresnel toon ramp base
                float fresnelRaw     = 1.0 - saturate(dot(n, v));
                float fresnelPow     = pow(fresnelRaw, _FresnelPow);
                float fresnelBoosted = saturate(fresnelPow * _FresnelBoost);

                float rimForRamp = Quantize01(fresnelBoosted, _RimBands);
                float2 rampUV    = float2(saturate(rimForRamp + _RampU), 0.0);
                float3 rampCol   = SAMPLE_TEXTURE2D(_RampTex, sampler_RampTex, rampUV).rgb;
                float3 baseCol   = _Tint.rgb * rampCol;

                // Subtle world noise-driven reflection
                float2 wNoiseUV  = i.positionWS.xy * _WorldNoiseScale + _Time.x * _WorldNoiseTime;
                float  Tnoise    = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, wNoiseUV).r;
                float3 R         = reflect(-v, n);
                R.xy            += (Tnoise * _ReflectNoiseAmt);
                float3 reflectCol= SAMPLE_TEXTURECUBE(_ReflectTex, sampler_ReflectTex, normalize(R)).rgb;
                baseCol          = lerp(baseCol, reflectCol, _ReflectStrength * fresnelPow);

                // Soft additive rim color for punchy edge
                baseCol += _RimColor.rgb * (fresnelPow * _RimColor.a * _RimSoftAdd);

                // Sparkles along rim (band-pass using smoothstep window)
                float2 flow = FlowNoise(i.positionWS, _WorldNoiseScale * 2.5, _Time.x, 1.0);
                float sparkleMask = smoothstep(_SparkleLo, _SparkleHi, dot(flow, float2(0.707,0.707)) + fresnelRaw*0.25);
                float3 sparkleCol = _SparkleColor.rgb * sparkleMask * _SparkleIntensity;
                baseCol += sparkleCol;

                // ===== Portal RT sampling =====
                float2 uvScreen  = i.screenPos.xy / i.screenPos.w;

                // Distortion (toward rim stronger)
                float2 flow2 = FlowNoise(i.positionWS, _WorldNoiseScale * 3.2, _Time.x, _DistortFreq);
                float rim01 = saturate(fresnelRaw); // 0 center, 1 edge
                float2 distort = flow2 * _DistortStrength * (0.25 + rim01);

                // Hit ripple modulates distortion outward
                float ripple = Ripple(i.positionWS);
                float ambRipple = (_AmbientRippleOn > 0.5) ? AmbientRipple(i.positionWS) : 0.0;
                float rippleAll = ripple + ambRipple;
                distort += normalize(flow2 + 1e-4) * rippleAll * 0.35;

                // Chromatic split (RGB sample with radial offsets)
                float2 radial = (uvScreen - 0.5) * (0.5 / max(length(uvScreen - 0.5), 1e-3));
                float ca = _ChromaticAberration * rim01;
                float3 rtCol;
                rtCol.r = SAMPLE_TEXTURE2D(_PortalRT, sampler_PortalRT, uvScreen + distort + radial * ca *  0.50).r;
                rtCol.g = SAMPLE_TEXTURE2D(_PortalRT, sampler_PortalRT, uvScreen + distort                           ).g;
                rtCol.b = SAMPLE_TEXTURE2D(_PortalRT, sampler_PortalRT, uvScreen + distort - radial * ca *  0.50).b;

                // World-space spherical mask with feather and extra hit ripple push
                float r          = max(_PortalRadius, 1e-4);
                float featherW   = max(_EdgeFeather, 1e-4);
                float dWS        = distance(i.positionWS, _PortalCenter.xyz);
                float maskCore   = saturate(1.0 - smoothstep(r - featherW, r + featherW, dWS));
                float mask       = saturate(maskCore + ripple + ambRipple * 0.5);

                // Inner fog (gives thickness)
                float inside01   = saturate(1.0 - dWS / r);
                float fogW       = pow(inside01, 1.5) * _InnerFogDensity * mask;
                float3 fogCol    = _InnerFogColor.rgb * fogW;

                // Blend RT over base toon
                float w          = _PortalColorBlend * mask;
                float3 col       = lerp(baseCol, rtCol, w) + fogCol * w;

                // Alpha handling
                float alpha = _Opacity;
                if (_UsePortalDistanceFade > 0.5)
                    alpha *= DepthFade(i.positionCS, _DepthFadeDist);
                alpha = max(alpha, w); // ensure portal area stays visible

                return half4(col, alpha);
            }
            ENDHLSL
        }
    }
}
