Shader "Custom/PortalSphere"
{
    Properties
    {
        _Tint       ("Tint", Color)   = (1,1,1,1)
        _Opacity    ("Opacity", Range(0,1)) = 1

        // Toon Ramp
        _RampTex("Ramp (Gradient 1xN)", 2D) = "white" {}
        _RampU("Ramp U Offset", Range(0,1)) = 0.0
        _RimBands("Rim Bands", Range(1,10)) = 8

		// Fresnel
        _FresnelPow("Fresnel Power", Range(0.1,10)) = 6
        _FresnelBoost("Fresnel Boost", Range(0,5)) = 2.5

        _NoiseTex("Noise 2D", 2D) = "gray" {}
        _PortalRT("Portal RT", 2D) = "black" {}

        _PortalColorBlend ("Portal Blend", Range(0,1)) = 1
        _EdgeFeather("Edge Feather (world units)", Range(0,2)) = 0.08

        // portal move wave param
        _WaveAmp("Wave Amplitude (m)", Range(0,0.2)) = 0.05
        _WaveFreq("Wave Frequency", Range(0.1,10)) = 3
        _WaveSpeed("Wave Speed", Range(0,10)) = 2
        _WaveDir1("Wave Dir1 (x,z)", Vector) = (1,0,0,0)
        _WaveDir2("Wave Dir2 (x,z)", Vector) = (0.3,0,0.95,0)
        _WaveSizeComp("Wave Size Compensation", Range(-2,2)) = 1

        // portal inside pixel shake strength 
        _InsideShakeStrength ("Inside Shake Strength", Range(0,3)) = 1
        _ShakePixels ("Inside Shake Pixels", Range(0,16)) = 4
    }

    SubShader
    {
        Tags {
            "RenderPipeline"="UniversalPipeline"
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
            Tags { "LightMode"="SRPDefaultUnlit" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // Textures
            TEXTURE2D(_RampTex);     SAMPLER(sampler_RampTex);
            TEXTURE2D(_NoiseTex);    SAMPLER(sampler_NoiseTex);
            TEXTURE2D(_PortalRT);    SAMPLER(sampler_PortalRT);

            float4 _PortalRT_TexelSize;

            CBUFFER_START(UnityPerMaterial)
                float4 _Tint; 
				float _Opacity;

                float4 _RampTex_ST; 
				float _RampU;
                float _RimBands;

                float _FresnelPow;
				float _FresnelBoost;

                float _PortalColorBlend;
				float _EdgeFeather;

                // Wave
                float _WaveAmp, _WaveFreq, _WaveSpeed;
                float4 _WaveDir1, _WaveDir2;
                float _WaveSizeComp;

                // Shake
                float _InsideShakeStrength;
                float _ShakePixels;
            CBUFFER_END

            float4 _PortalCenter;   
            float  _PortalRadius;

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

            // Wave 
            void ApplyWave(
                inout float3 posWS, inout float3 nWS,
                float4 waveDir1, float4 waveDir2,
                float  waveAmp, float  waveFreq,
                float  waveSpeed, float waveSizeComp,
                float  portalRadius)
            {

                // scale x,z for consistent wave size across different portal radius
                float2 xz = posWS.xz * pow(max(portalRadius, 0.001), -waveSizeComp);

                // moving around x,z plane
                float2 d1 = normalize(waveDir1.xz);
                float2 d2 = normalize(waveDir2.xz);
                float t = _Time.y * waveSpeed;

                float s1 = sin(dot(xz, d1) * waveFreq + t);
                float s2 = sin(dot(xz, d2) * (waveFreq * 1.18) + t * 1.3);
                float wave = (s1 + s2) * 0.5;

                posWS += nWS * (wave * waveAmp);

                // normal fix (reduce fake shading)
                float eps = 0.03;
                float hX = sin(dot(xz + d1 * eps, d1) * waveFreq + t);
                float hY = sin(dot(xz + d2 * eps, d2) * waveFreq + t);
                float3 tangent   = normalize(float3(1, (hX - wave), 0));
                float3 bitangent = normalize(float3(0, (hY - wave), 1));

                nWS = normalize(lerp(nWS, normalize(cross(tangent, bitangent)), 0.4));
            }

            Varyings vert (Attributes v)
            {
                Varyings o;

                float3 posWS = TransformObjectToWorld(v.positionOS);
                float3 nWS   = normalize(TransformObjectToWorldNormal(v.normalOS));

                ApplyWave(posWS, nWS, _WaveDir1, _WaveDir2, _WaveAmp, _WaveFreq, _WaveSpeed, _WaveSizeComp, _PortalRadius);

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

                // Fresnel:  F = ((1 - N·V) ^ _FresnelPow) × _FresnelBoost
				// The more inclined the angle, the brighter the edges.
                float fres = pow(i.fresnelRaw, _FresnelPow) * _FresnelBoost;
                float rim  = saturate(fres);

				// Toon Ramp: C_base = Tint * Ramp(rim + U)
				// control the brightness distribution of the edges, achieving a color gradient.
                float2 rampUV = float2(saturate(rim + _RampU), 0);
                float3 rampCol = SAMPLE_TEXTURE2D(_RampTex, sampler_RampTex, rampUV).rgb;
                float3 baseCol = _Tint.rgb * rampCol;

                // screen uv
                float2 uvScreen = i.screenPos.xy / i.screenPos.w;

                // noise → shake
                float2 noiseUV = i.positionWS.xz * 0.6 + _Time.yy * 0.6;
                float2 nrg = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, noiseUV).rg * 2 - 1;

                // camera inside check
                float camDist = distance(_WorldSpaceCameraPos.xyz, _PortalCenter.xyz);
                float inside  = step(camDist, _PortalRadius);

                // Apply shake to PortalRT sampling
				// shake = nrg * (InsideShakeStrength * ShakePixels) * TexelSize * inside
                float2 texel = _PortalRT_TexelSize.xy;
                float2 shake = nrg * (_InsideShakeStrength * _ShakePixels) * texel * inside;
                float3 srcCol = SAMPLE_TEXTURE2D(_PortalRT, sampler_PortalRT, uvScreen + shake).rgb;

                // Spherical edge feather mask
                float r = max(_PortalRadius, 0.0001);
                float featherW = max(_EdgeFeather, 0.0001);
                float d = distance(i.positionWS, _PortalCenter.xyz);
				
				// mask = 1 - smoothstep( r - w, r + w, d )
                float mask = saturate(1 - smoothstep(r - featherW, r + featherW, d));

                // blend:  result = (1 - w) C_base + w * C_RT
                float w = _PortalColorBlend * mask;
                float3 result = lerp(baseCol, srcCol, w);

                // alpha
                float alpha = _Opacity * lerp(0.3f, 1.0f, saturate(dot(n, v)));
                alpha = max(alpha, w);

                return float4(result, alpha);
            }
            ENDHLSL
        }
    }
}