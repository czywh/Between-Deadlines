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

        // Specular（用软阈值替代硬 step，避免靠近/远离时高光跳变）
        _SpecColor ("Spec Color", Color) = (1,1,1,1)
        _SpecStrength ("Spec Strength", Range(0,2)) = 0.5
        _SpecThreshold ("Spec Threshold", Range(0,1)) = 0.5
        _SpecFeather   ("Spec Feather",   Range(0,0.5)) = 0.08
        _Gloss ("Glossiness (BlinnPow)", Range(8,256)) = 64

        // Rim
        _RimColor ("Rim Color", Color) = (1,1,1,1)
        _RimPower ("Rim Power", Range(0.5,8)) = 2

        // 可选灰度 Ramp（0~1）
        _Ramp ("Optional Ramp (grayscale)", 2D) = "gray" {}

        // 阴影距离淡出（解决接近 Shadow Distance 时“啪没了”）
        _ShadowFadeStart ("Shadow Fade Start (m)", Range(0,200)) = 35
        _ShadowFadeEnd   ("Shadow Fade End (m)",   Range(0,300)) = 50

        // 软阴影的最低亮度（避免纯黑块，削弱级联硬切换感）
        _MinShadow     ("Min Shadow", Range(0,0.6)) = 0.25
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

            // 可选功能开关（在材质面板 Keywords 里勾选即可）
            #pragma shader_feature_local _USE_RAMP              // 使用自定义灰度 Ramp
            #pragma shader_feature_local _USE_SHADOW_FADE       // 开启阴影距离淡出（需设置 FadeStart/End）
            #pragma shader_feature_local _SPEC_MAIN_ONLY        // 仅主光产生高光（避免附加光进/出导致跳变）

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
            // 教学：先把 N·L 等分成 _Steps 段，再用 _Feather 做羽化（避免硬台阶）。
            half CelStep(half ndl)
            {
                half steps = max(_Steps, 1.0);
                half q = floor(ndl * steps);
                half denom = max(steps - 1.0, 1.0);
                half stepped = q / denom;
                // _Feather>0 时向回插一点，柔化边缘
                stepped = lerp(stepped, ndl, saturate(_Feather * 2.0));

                #if defined(_USE_RAMP)
                    // 若使用自定义 Ramp：以 0~1 灰度映射分段结果，更易统一风格
                    stepped = SAMPLE_TEXTURE2D(_Ramp, sampler_Ramp, float2(saturate(stepped), 0.5)).r;
                #endif
                return saturate(stepped);
            }

            // 单盏光的 Toon 着色（关键：用软阴影参与分段，减少级联切换带来的割裂）
            half3 ShadeOneLight(half3 n, half3 v, float3 posWS, Light light, half3 albedo, out half3 specCol)
            {
                // —— 使用 float 以避免半精度量化造成的闪烁
                float ndl   = saturate(dot(n, light.direction));
                float atten = light.distanceAttenuation * light.shadowAttenuation;

                // 1) 软阴影：给阴影一个最小亮度，避免纯黑块（级联边界更不明显）
                float softShadow = lerp(_MinShadow, 1.0, light.shadowAttenuation);

                // 2) 让阴影影响进入 Toon 阶梯（而不是直接 0/1 乘）
                float litNL  = ndl * softShadow;
                half  stepNL = CelStep((half)litNL);

                // 3) 可选：接近 ShadowDistance 时做“距离淡出”，避免阴影突然消失
                #if defined(_USE_SHADOW_FADE)
                    float dCam = distance(_WorldSpaceCameraPos, posWS);
                    // 距离超过 FadeEnd 逐渐减弱到 0；FadeStart 之前保持 1
                    float fade = 1.0 - smoothstep(_ShadowFadeStart, _ShadowFadeEnd, dCam);
                    stepNL *= fade;
                #endif

                // —— 漫反射（先分段再乘颜色/衰减）
                half3 diff = albedo * (half)stepNL * light.color * atten;

                // —— 高光：Blinn-Phong -> 软阈值（smoothstep）替代硬 step
                float3 h   = normalize(light.direction + v);
                float  ndh = saturate(dot(n, h));
                float  s   = pow(ndh, _Gloss);

                // 教学：_SpecThreshold 控制“出现位置”，_SpecFeather 控制“高光带宽度”
                float  w = max(_SpecFeather, 1e-4);
                s = smoothstep(_SpecThreshold - w, _SpecThreshold + w, s);

                // 只在亮面给高光（若想要“背面反光”，把下一行改成：s *= 1.0 - step(0.001, litNL);）
                s *= step(0.001, litNL);

                // 阴影对高光也只做“软影响”（避免 0/1）
                s *= softShadow;

                specCol = (half3)(s * _SpecStrength) * _SpecColor.rgb * light.color * atten;
                return diff;
            }

            half4 frag (Varyings i) : SV_Target
            {
                half3 n = normalize(i.normalWS);
                half3 v = normalize(GetWorldSpaceViewDir(i.positionWS));

                half3 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).rgb * _Color.rgb;

                // 主光（若开了阴影，这里会带 shadowCoord）
                #if defined(MAIN_LIGHT_CALCULATE_SHADOWS)
                    Light mainLight = GetMainLight(i.shadowCoord);
                #else
                    Light mainLight = GetMainLight();
                #endif

                half3 specMain;
                half3 col = ShadeOneLight(n, v, i.positionWS, mainLight, albedo, specMain);

                // —— 高光累加策略：
                // 默认：主光 + 附加光都出高光；
                // 若勾选 _SPEC_MAIN_ONLY：只主光出高光，避免附加光进/出导致明暗跳变。
                half3 specSum = specMain;

                // 额外光源累加
                #if defined(_ADDITIONAL_LIGHTS)
                    uint addCount = GetAdditionalLightsCount();
                    for (uint li = 0u; li < addCount; li++)
                    {
                        Light l = GetAdditionalLight(li, i.positionWS);
                        half3 specA;
                        half3 addDiff = ShadeOneLight(n, v, i.positionWS, l, albedo, specA);
                        col += addDiff;

                        #if !defined(_SPEC_MAIN_ONLY)
                            specSum += specA;
                        #endif
                    }
                #endif

                // 环境（URP 基础环境：球谐近似）
                half3 sh = SampleSH(n);
                half3 ambient = sh * albedo;

                // Rim（保持不变）
                half rim = pow(1.0h - saturate(dot(n, v)), _RimPower);
                half3 rimCol = _RimColor.rgb * rim;

                half3 finalCol = ambient + col + specSum + rimCol;

                return half4(finalCol, 1);
            }
            ENDHLSL
        }

        // 直接复用 URP 的投影 Pass
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
    }

    FallBack Off
}