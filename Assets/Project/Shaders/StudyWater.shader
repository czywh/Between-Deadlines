Shader "Custom/StudyWater"
{
    Properties
    {
        _DeepColor          ("Deep Water Color", Color)    = (0.0, 0.1, 0.3, 1)
        _ShallowColor       ("Shallow Water Color", Color) = (0.2, 0.8, 0.9, 1)

        _RefractionScale    ("Refraction Scale",   Float) = 0.1
        _RefractionSpeed    ("Refraction Speed",   Float) = 0.04
        _RefractionStrength ("Refraction Strength",Float) = 0.008

        _MaxDepthDistance   ("Depth Fade Max Distance", Float) = 2.0
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalRenderPipeline"
            "RenderType"="Transparent"
            "Queue"="Transparent"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            Name "WaterRefraction"

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // ========= Uniform =========
            CBUFFER_START(UnityPerMaterial)
                float4 _DeepColor;           // 深水颜色
                float4 _ShallowColor;        // 浅水颜色

                float  _RefractionScale;     // 折射 Tiling 的 scale（越大图案越大）
                float  _RefractionSpeed;     // 噪声流动速度
                float  _RefractionStrength;  // 折射强度（UV 偏移量）

                float  _MaxDepthDistance;    // Depth Fade 最大距离（视空间）
            CBUFFER_END

            // 场景不透明颜色，用于折射采样（URP 中需要开启 Opaque Texture）
            TEXTURE2D_X(_CameraOpaqueTexture);
            SAMPLER(sampler_CameraOpaqueTexture);

            // 场景深度，用于 depth fade（在 Renderer 里勾选 Depth Texture）
            TEXTURE2D_X(_CameraDepthTexture);
            SAMPLER(sampler_CameraDepthTexture);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION; // 裁剪空间坐标，用于 ComputeScreenPos 和 深度计算
                float2 uv         : TEXCOORD0;
                float3 worldPos   : TEXCOORD1;   // 以后做 Foam 等可以用，这里暂时不用
            };

            // ====== Gradient Noise（输出大致在 -1~1） ======
            float2 unity_gradientNoise_dir(float2 p)
            {
                p = p % 289;
                float x = (34 * p.x + 1) * p.x % 289 + p.y;
                x = (34 * x + 1) * x % 289;
                x = frac(x / 41) * 2 - 1;
                return normalize(float2(x - floor(x + 0.5), abs(x) - 0.5));
            }

            float unity_gradientNoise(float2 p)
            {
                float2 ip = floor(p);
                float2 fp = frac(p);
                float d00 = dot(unity_gradientNoise_dir(ip), fp);
                float d01 = dot(unity_gradientNoise_dir(ip + float2(0, 1)), fp - float2(0, 1));
                float d10 = dot(unity_gradientNoise_dir(ip + float2(1, 0)), fp - float2(1, 0));
                float d11 = dot(unity_gradientNoise_dir(ip + float2(1, 1)), fp - float2(1, 1));
                fp = fp * fp * fp * (fp * (fp * 6 - 15) + 10);
                return lerp(lerp(d00, d01, fp.y), lerp(d10, d11, fp.y), fp.x); // ≈ -1~1
            }

            // ========= Clip Space -> Screen Space -> 0~1 UV =========
            float2 GetScreenUV(float4 positionCS)
            {
                float4 sp = ComputeScreenPos(positionCS);
                return sp.xy / sp.w;
            }

            float2 Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset)
            {
                return UV * Tiling + Offset;
            }

            // ========= 水下物体折射 UV 计算 =========
            float2 ComputeRefractionUV(float4 positionCS)
            {
                // 1. Screen Position → Screen UV (0~1)
                float2 screenUV = GetScreenUV(positionCS);

                // 2. Tiling = Reciprocal(scale) = 1 / scale
                float tilingScalar = (_RefractionScale != 0.0) ? 1.0 / _RefractionScale : 1.0;
                float2 tiling = float2(tilingScalar, tilingScalar);

                // 3. Offset = Speed * Time
                float timeOffset = _RefractionSpeed * _Time.y;
                float2 offset = float2(timeOffset, timeOffset);

                // 4. Tiling and Offset：Gradient Noise 的 UV
                float2 noiseUV = Unity_TilingAndOffset_float(screenUV, tiling, offset);

                // 5. Gradient Noise(noiseUV) [-1,1]
                float noiseSigned = unity_gradientNoise(noiseUV);

                // 6. 乘 Strength，当作 2D 偏移（这里 x、y 同方向偏移）
                float2 uvOffset = float2(noiseSigned * _RefractionStrength,
                                         noiseSigned * _RefractionStrength);

                // 7. Screen Position + 偏移
                float2 refractionUV = screenUV + uvOffset;

                return refractionUV;
            }

            // ========= Depth Fade（视空间线性深度差） =========
            // depthFade: 0 = 深水, 1 = 浅水
            float ComputeDepthFade(float4 positionCS)
            {
                // 1. 用“未折射”的 screenUV 去采深度（避免边缘错位伪像）
                float2 screenUV = GetScreenUV(positionCS);

                // 场景深度（从 depth texture）
                float rawSceneDepth = SAMPLE_TEXTURE2D_X(
                    _CameraDepthTexture,
                    sampler_CameraDepthTexture,
                    screenUV
                ).r;

                // 如果没 hit 到几何（天空等），直接当作“很浅”
                if (rawSceneDepth >= 0.999)
                    return 1.0;

                // 2. 把 depth texture 的深度值转换为线性视空间深度
                float sceneDepth = LinearEyeDepth(rawSceneDepth, _ZBufferParams);

                // 3. 当前水面像素的视空间深度
                //    positionCS.z / positionCS.w 是 clip space depth（0~1）
                float ndcDepth     = positionCS.z / positionCS.w;
                float surfaceDepth = LinearEyeDepth(ndcDepth, _ZBufferParams);

                // 4. 深度差：场景几何到水面的距离（沿视线方向）
                float diff = sceneDepth - surfaceDepth;
                diff = max(diff, 0.0);

                // 5. 指数衰减：差距越大 → fade 越小
                float fade = exp(-diff / max(_MaxDepthDistance, 0.0001));
                fade = saturate(fade);

                // fade: 0 = 很深, 1 = 很浅
                return fade;
            }

            // 顶点着色器
            Varyings vert (Attributes IN)
            {
                Varyings OUT;

                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionCS    = TransformWorldToHClip(positionWS);
                OUT.uv            = IN.uv;
                OUT.worldPos      = positionWS;

                return OUT;
            }

            // 片元着色器
            half4 frag (Varyings IN) : SV_Target
            {
                // 1. 计算折射 UV（屏幕扭曲）
                float2 refUV = ComputeRefractionUV(IN.positionCS);

                // 2. 用折射 UV 从 _CameraOpaqueTexture 采样场景颜色
                float3 sceneColor = SAMPLE_TEXTURE2D_X(
                    _CameraOpaqueTexture,
                    sampler_CameraOpaqueTexture,
                    refUV
                ).rgb;

                // 3. 计算 depth fade：0 = 深水，1 = 浅水
                float depthFade = ComputeDepthFade(IN.positionCS);

                // 4. 用 depthFade 对深/浅水颜色做 lerp
                float3 deepCol    = _DeepColor.rgb;
                float3 shallowCol = _ShallowColor.rgb;
                float3 waterColor = lerp(deepCol, shallowCol, depthFade);

                // 5. 用 waterColor 去 tint 折射后的场景颜色
                //    （你也可以改成 lerp(sceneColor, waterColor, 某个权重)）
                float3 finalColor = sceneColor * waterColor;

                // alpha 也随深度插值一下会更自然
                float alpha = lerp(_DeepColor.a, _ShallowColor.a, depthFade);

                return half4(finalColor, alpha);
            }

            ENDHLSL
        }
    }

    FallBack Off
}