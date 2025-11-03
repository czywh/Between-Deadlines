Shader "Custom/URP/CustomCelToon"
{
    Properties
    {
        _MainTex("Albedo", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        // Outline
        _OutlineColor   ("Outline Color", Color) = (0,0,0,1)
        _OutlineWidthPx ("Outline Width (pixels)", Range(0,3)) = 0.35

        // Diffuse toon ramp
        _Steps   ("Light Steps", Range(1,5)) = 3
        _Feather ("Feather (0 = hard edge)", Range(0,0.5)) = 0.05

        // Specular
        _SpecColor    ("Spec Color", Color) = (1,1,1,1)
        _SpecStrength ("Spec Strength", Range(0,2)) = 0.5
        _SpecThreshold("Spec Threshold", Range(0,1)) = 0.5
        _SpecFeather  ("Spec Feather", Range(0,0.5)) = 0.08
        _Gloss        ("Glossiness (BlinnPow)", Range(8,256)) = 64

        // Rim lighting
        _RimColor ("Rim Color", Color) = (1,1,1,1)
        _RimPower ("Rim Power", Range(0.5,8)) = 2

        // Shadow parameters
        _ShadowFadeStart ("Shadow Fade Start (m)", Range(0,200)) = 35
        _ShadowFadeEnd   ("Shadow Fade End (m)",   Range(0,300)) = 50
        _MinShadow       ("Min Shadow Brightness", Range(0,0.6)) = 0.25
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Opaque"
            "Queue"="Geometry"
        }
        LOD 300

        // Pass 1 : Outline
        Pass
        {
            Name "OUTLINE"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Front
            ZWrite On
            ZTest LEqual
            ColorMask RGB

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor;
                float  _OutlineWidthPx;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            Varyings vert (Attributes v)
            {
                Varyings o;
                float4 clipPos = TransformObjectToHClip(v.positionOS.xyz);

                // View space normal to screen offset direction
                float3 viewN = mul((float3x3)UNITY_MATRIX_IT_MV, v.normalOS);
                viewN.z = 0;
                float2 dir = normalize(viewN.xy + 1e-6);

                float2 ndcPerPixel = 2.0 / _ScreenParams.xy;
                float2 ndcOffset   = dir * (_OutlineWidthPx * ndcPerPixel.y);

                clipPos.xy += ndcOffset * clipPos.w;
                o.positionHCS = clipPos;
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }

        // Pass 2 : Toon Shading (Main light only)
        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode"="UniversalForward" }
            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex   vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;

                float  _Steps;
                float  _Feather;

                float4 _SpecColor;
                float  _SpecThreshold;
                float  _SpecStrength;
                float  _SpecFeather;
                float  _Gloss;

                float4 _RimColor;
                float  _RimPower;

                float  _ShadowFadeStart;
                float  _ShadowFadeEnd;
                float  _MinShadow;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS  : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float2 uv          : TEXCOORD2;
            };

			// vertex shader
            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionWS  = TransformObjectToWorld(v.positionOS.xyz);
                o.normalWS    = TransformObjectToWorldNormal(v.normalOS);
                o.positionHCS = TransformWorldToHClip(o.positionWS);
                o.uv          = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            // Toon intensity (0~1)
            half CelIntensity(half ndl)
            {
                half steps   = max(_Steps, 1.0);
                half q       = floor(ndl * steps);
                half denom   = max(steps - 1.0, 1.0);
                half stepped = q / denom;
                stepped = lerp(stepped, ndl, saturate(_Feather * 2.0));
                return saturate(stepped);
            }

            // Single light toon shading
            half3 ShadeOneLight(half3 n, half3 v, float3 posWS, Light light, half3 albedo, out half3 specCol)
            {
                // L = N·L, basic Lambert 
                float ndl = saturate(dot(n, light.direction));
                float atten = light.distanceAttenuation * light.shadowAttenuation;

                // Minimum brightness under soft shadow
                float softShadow = lerp(_MinShadow, 1.0, light.shadowAttenuation);

                // Apply toon steps
                float litNL  = ndl * softShadow;
                half  k = CelIntensity((half)litNL);

                // Simple distance-based fade (always active, no macro)
                float dCam = distance(_WorldSpaceCameraPos, posWS);
                float fade = 1.0 - smoothstep(_ShadowFadeStart, _ShadowFadeEnd, dCam);
                k *= fade;

                // Diffuse 
                half3 diff = albedo * k * light.color * (half)atten;

                // Specular (Blinn-Phong Highlight)
                float3 h = normalize(light.direction + v);
                float  s = pow(saturate(dot(n, h)), _Gloss);

                float  w = max(_SpecFeather, 1e-4);
                s = smoothstep(_SpecThreshold - w, _SpecThreshold + w, s);
                s *= step(0.001, litNL);
                s *= softShadow;

                // Specular = s × SpecStrength × SpecColor × LightColor × Attenuation
                specCol = (half3)(s * _SpecStrength) * _SpecColor.rgb * light.color * (half)atten;
                return diff;
            }

            // Fragment shader
            half4 frag (Varyings i) : SV_Target
            {
                half3 n = normalize(i.normalWS);
                half3 v = normalize(GetWorldSpaceViewDir(i.positionWS));
                half3 baseCol = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).rgb * _Color.rgb;

                // Main light only
                Light mainLight = GetMainLight();

                half3 specMain;
                half3 diffuseCol = ShadeOneLight(n, v, i.positionWS, mainLight, baseCol, specMain);

                // Ambient from spherical harmonics
                half3 ambient = SampleSH(n) * baseCol;

                // Rim light
                half rim = pow(1.0h - saturate(dot(n, v)), _RimPower);
                half3 rimCol = _RimColor.rgb * rim;

                // Final color
                half3 finalCol = ambient + diffuseCol + specMain + rimCol;
                return half4(finalCol, 1);
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
    }

    FallBack Off
}