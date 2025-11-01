Shader "Custom/URP/CustomCelToon_Full"
{
    Properties
    {
        // 主纹理/颜色（兼容高亮）
        [MainTexture] _MainTex("Albedo", 2D) = "white" {}
        [MainColor]   _Color ("Tint", Color) = (1,1,1,1)

        // Outline
        _OutlineColor   ("Outline Color", Color) = (0,0,0,1)
        _OutlineWidthPx ("Outline Width (pixels)", Range(0,3)) = 0.35

        // 漫反射 Toon 阶梯/羽化
        _Steps   ("Light Steps", Range(1,5)) = 3
        _Feather ("Feather (0 = hard)", Range(0,0.5)) = 0.05

        // 高光（软阈值避免跳变）
        _SpecColor    ("Spec Color", Color) = (1,1,1,1)
        _SpecStrength ("Spec Strength", Range(0,2)) = 0.5
        _SpecThreshold("Spec Threshold", Range(0,1)) = 0.5
        _SpecFeather  ("Spec Feather",   Range(0,0.5)) = 0.08
        _Gloss        ("Glossiness (BlinnPow)", Range(8,256)) = 64

        // Rim
        _RimColor ("Rim Color", Color) = (1,1,1,1)
        _RimPower ("Rim Power", Range(0.5,8)) = 2

        // Ramp
        [NoScaleOffset]_Ramp ("Ramp (1D Strip)", 2D) = "gray" {}
        [KeywordEnum(Procedural,RampGray,RampColor)] _DiffuseMode("Diffuse Mode", Float) = 0

        // 阴影距离淡出
        [Toggle(_USE_SHADOW_FADE)] _UseShadowFade ("Use Shadow Fade", Float) = 0
        _ShadowFadeStart ("Shadow Fade Start (m)", Range(0,200)) = 35
        _ShadowFadeEnd   ("Shadow Fade End (m)",   Range(0,300)) = 50

        // 软阴影最小亮度
        _MinShadow     ("Min Shadow", Range(0,0.6)) = 0.25

        // 高光只来自主光
        [Toggle(_SPEC_MAIN_ONLY)] _SpecMainOnly ("Specular From Main Light Only", Float) = 0
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
        // Pass 0 : Outline
        // =========================
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

                // 视空间法线 -> 屏幕像素偏移方向
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

        // =========================
        // Pass 1 : UniversalForward
        // =========================
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

            // URP 关键字
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT

            // 本地功能开关
            #pragma shader_feature_local _USE_SHADOW_FADE
            #pragma shader_feature_local _SPEC_MAIN_ONLY
            #pragma shader_feature_local _DIFFUSEMODE_PROCEDURAL _DIFFUSEMODE_RAMPGRAY _DIFFUSEMODE_RAMPCOLOR

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
                float  _SpecThreshold;
                float  _SpecStrength;
                float  _SpecFeather;
                float  _Gloss;

                float4 _RimColor;
                float  _RimPower;

                float  _ShadowFadeStart;
                float  _ShadowFadeEnd;
                float  _MinShadow;

                float  _DiffuseMode; // 仅用于面板显示，无需在代码中使用
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
                o.positionWS  = TransformObjectToWorld(v.positionOS.xyz);
                o.normalWS    = TransformObjectToWorldNormal(v.normalOS);
                o.positionHCS = TransformWorldToHClip(o.positionWS);
                o.uv          = TRANSFORM_TEX(v.uv, _MainTex);
                #if defined(MAIN_LIGHT_CALCULATE_SHADOWS)
                    o.shadowCoord = TransformWorldToShadowCoord(o.positionWS);
                #endif
                return o;
            }

            // —— 计算分段强度（0~1），支持三种模式
            half CelIntensity(half ndl)
            {
                // 先做“程序化阶梯”
                half steps   = max(_Steps, 1.0);
                half q       = floor(ndl * steps);
                half denom   = max(steps - 1.0, 1.0);
                half stepped = q / denom;
                // 羽化软化边缘
                stepped = lerp(stepped, ndl, saturate(_Feather * 2.0));
                stepped = saturate(stepped);

                #if defined(_DIFFUSEMODE_RAMPGRAY) || defined(_DIFFUSEMODE_RAMPCOLOR)
                    // 用 Ramp 的灰度作为强度（彩色模式也需要这个强度用于“亮面定义”）
                    float2 uv = float2(stepped, 0.5);
                    stepped = SAMPLE_TEXTURE2D(_Ramp, sampler_Ramp, uv).r;
                    stepped = saturate(stepped);
                #endif

                return stepped;
            }

            // —— 单盏光 Toon 着色（输出漫反射 & 高光）
            half3 ShadeOneLight(half3 n, half3 v, float3 posWS, Light light, half3 albedo, out half3 specCol)
            {
                float ndl   = saturate(dot(n, light.direction));
                float atten = light.distanceAttenuation * light.shadowAttenuation;

                // 软阴影：给阴影下限，减少级联割裂
                float softShadow = lerp(_MinShadow, 1.0, light.shadowAttenuation);

                // 让阴影参与 N·L，再做分段
                float litNL  = ndl * softShadow;
                half  k      = CelIntensity((half)litNL);

                #if defined(_USE_SHADOW_FADE)
                    float dCam = distance(_WorldSpaceCameraPos, posWS);
                    float fade = 1.0 - smoothstep(_ShadowFadeStart, _ShadowFadeEnd, dCam);
                    k *= fade;
                #endif

                half3 diff;

                // —— 漫反射颜色来源
                #if defined(_DIFFUSEMODE_RAMPCOLOR)
                    // 彩色 Ramp：用 ramp 颜色与 Albedo 相乘（保留贴图细节）
                    float2 ruv = float2(k, 0.5);
                    half3 rampRGB = SAMPLE_TEXTURE2D(_Ramp, sampler_Ramp, ruv).rgb;
                    diff = (rampRGB * albedo) * light.color * (half)atten;
                #else
                    // 程序化 or 灰度 Ramp：强度 k 决定亮部权重
                    diff = albedo * k * light.color * (half)atten;
                #endif

                // —— 高光（软阈值）
                float3 h   = normalize(light.direction + v);
                float  ndh = saturate(dot(n, h));
                float  s   = pow(ndh, _Gloss);

                float  w   = max(_SpecFeather, 1e-4);
                s = smoothstep(_SpecThreshold - w, _SpecThreshold + w, s);

                // 仅亮面出高光
                s *= step(0.001, litNL);
                // 阴影软影响
                s *= softShadow;

                specCol = (half3)(s * _SpecStrength) * _SpecColor.rgb * light.color * (half)atten;

                return diff;
            }

            half4 frag (Varyings i) : SV_Target
            {
                half3 n = normalize(i.normalWS);
                half3 v = normalize(GetWorldSpaceViewDir(i.positionWS));

                half3 baseCol = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).rgb * _Color.rgb;

                // 主光
                #if defined(MAIN_LIGHT_CALCULATE_SHADOWS)
                    Light mainLight = GetMainLight(i.shadowCoord);
                #else
                    Light mainLight = GetMainLight();
                #endif

                half3 specMain;
                half3 col = ShadeOneLight(n, v, i.positionWS, mainLight, baseCol, specMain);

                half3 specSum = specMain;

                // 附加光
                #if defined(_ADDITIONAL_LIGHTS)
                    uint addCount = GetAdditionalLightsCount();
                    for (uint li = 0u; li < addCount; li++)
                    {
                        Light l = GetAdditionalLight(li, i.positionWS);
                        half3 specA;
                        half3 addDiff = ShadeOneLight(n, v, i.positionWS, l, baseCol, specA);
                        col += addDiff;
                        #if !defined(_SPEC_MAIN_ONLY)
                            specSum += specA;
                        #endif
                    }
                #endif

                // 环境光（SH）
                half3 sh = SampleSH(n);
                half3 ambient = sh * baseCol;

                // Rim
                half rim = pow(1.0h - saturate(dot(n, v)), _RimPower);
                half3 rimCol = _RimColor.rgb * rim;

                half3 finalCol = ambient + col + specSum + rimCol;

                return half4(finalCol, 1);
            }
            ENDHLSL
        }

        // 投影 Pass
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
    }

    FallBack Off
}