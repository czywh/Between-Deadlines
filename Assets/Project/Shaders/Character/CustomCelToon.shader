Shader "Custom/URP/CustomCelToon"
{
    Properties
    {
        // 兼容内置命名，同时让材质面板有主贴图/主颜色高亮
        [MainTexture] _MainTex("Albedo", 2D) = "white" {}
        [MainColor]   _Color ("Tint", Color) = (1,1,1,1)

        // Outline
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineWidthPx ("Outline Width (pixels)", Range(0,3)) = 0.35

        // Diffuse(阶梯/羽化)
        _Steps  ("Light Steps", Range(1,5)) = 3
        _Feather("Feather (0 = hard)", Range(0,0.5)) = 0.05

        // Specular
        _SpecColor ("Spec Color", Color) = (1,1,1,1)
        _SpecStrength ("Spec Strength", Range(0,2)) = 0.5
        _Gloss ("Glossiness (BlinnPow)", Range(8,256)) = 64

        // Rim
        _RimColor ("Rim Color", Color) = (1,1,1,1)
        _RimPower ("Rim Power", Range(0.5,8)) = 2

        // 可选灰度 Ramp（0~1）
        _Ramp ("Optional Ramp (grayscale)", 2D) = "gray" {}
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

        // =========================
        // Pass 0 : Outline (反转外壳)
        // =========================
        Pass
        {
            Name "OUTLINE"
            Tags { "LightMode"="SRPDefaultUnlit" } // 简单颜色即可
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
                // 对象->裁剪
                float4 clipPos = TransformObjectToHClip(v.positionOS.xyz);

                // 视空间法线用于屏幕像素偏移（避免透视缩放）
                float3 viewN = mul((float3x3)UNITY_MATRIX_IT_MV, v.normalOS);
                viewN.z = 0;
                float2 dir = normalize(viewN.xy + 1e-6);

                // 以像素为单位的NDC偏移
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

        // ==========================================
        // Pass 1 : URP 前向主通道（主光+附加光）
        // ==========================================
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

            // 阴影/附加光等关键词
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT

            // 可选 Ramp
            #pragma shader_feature_local _USE_RAMP

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_MainTex);      SAMPLER(sampler_MainTex);
            TEXTURE2D(_Ramp);         SAMPLER(sampler_Ramp);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;

                float  _Steps;
                float  _Feather;

                float4 _SpecColor;
                float  _SpecStrength;
                float  _Gloss;

                float4 _RimColor;
                float  _RimPower;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS  : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float2 uv          : TEXCOORD2;
                #if defined(MAIN_LIGHT_CALCULATE_SHADOWS)
                    float4 shadowCoord : TEXCOORD3;
                #endif
            };

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.normalWS   = TransformObjectToWorldNormal(v.normalOS);
                o.positionHCS = TransformWorldToHClip(o.positionWS);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                #if defined(MAIN_LIGHT_CALCULATE_SHADOWS)
                    o.shadowCoord = TransformWorldToShadowCoord(o.positionWS);
                #endif
                return o;
            }

            // 计算分段漫反射：返回 0~1
            half CelStep(half ndl)
            {
                half steps = max(_Steps, 1.0);
                half q = floor(ndl * steps);
                half denom = max(steps - 1.0, 1.0);
                half stepped = q / denom;
                stepped = lerp(stepped, ndl, saturate(_Feather * 2.0));

                #if defined(_USE_RAMP)
                    stepped = SAMPLE_TEXTURE2D(_Ramp, sampler_Ramp, float2(saturate(stepped), 0.5)).r;
                #endif
                return saturate(stepped);
            }

            half3 ShadeOneLight(half3 n, half3 v, Light light, half3 albedo, out half3 specCol)
            {
                half ndl = saturate(dot(n, light.direction));
                half stepNDL = CelStep(ndl);
                half atten = light.distanceAttenuation * light.shadowAttenuation;

                half3 diff = albedo * stepNDL * light.color * atten;

                // Toon风格高光（Blinn-Phong 后做一次阈值）
                half3 h = normalize(light.direction + v);
                half spec = pow(saturate(dot(n, h)), _Gloss);
                spec = step(0.5, spec) * _SpecStrength;
                specCol = spec * _SpecColor.rgb * light.color * atten;

                return diff;
            }

            half4 frag (Varyings i) : SV_Target
            {
                half3 n = normalize(i.normalWS);
                half3 v = normalize(GetWorldSpaceViewDir(i.positionWS));

                half3 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).rgb * _Color.rgb;

                // 主光
                #if defined(MAIN_LIGHT_CALCULATE_SHADOWS)
                    Light mainLight = GetMainLight(i.shadowCoord);
                #else
                    Light mainLight = GetMainLight();
                #endif

                half3 specMain;
                half3 col = ShadeOneLight(n, v, mainLight, albedo, specMain);
                half3 specSum = specMain;

                // 额外光源累加
                #if defined(_ADDITIONAL_LIGHTS)
                    uint addCount = GetAdditionalLightsCount();
                    for (uint li = 0u; li < addCount; li++)
                    {
                        Light l = GetAdditionalLight(li, i.positionWS);
                        half3 specA;
                        col += ShadeOneLight(n, v, l, albedo, specA);
                        specSum += specA;
                    }
                #endif

                // 环境（URP 基础环境：球谐近似）
                half3 sh = SampleSH(n);
                half3 ambient = sh * albedo;

                // Rim
                half rim = pow(1.0h - saturate(dot(n, v)), _RimPower);
                half3 rimCol = _RimColor.rgb * rim;

                half3 finalCol = ambient + col + specSum + rimCol;

                return half4(finalCol, 1);
            }
            ENDHLSL
        }
    }

    FallBack Off
}