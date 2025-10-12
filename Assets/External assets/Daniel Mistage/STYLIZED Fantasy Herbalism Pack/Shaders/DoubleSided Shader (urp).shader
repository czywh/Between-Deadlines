Shader "Custom/DoubleSided_URP"
{
    Properties
    {
        _Color      ("Color", Color) = (1,1,1,1)
        _MainTex    ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic   ("Metallic", Range(0,1)) = 0.0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalRenderPipeline"
            "RenderType"="Opaque"
            "Queue"="Geometry"
        }

        LOD 200
        Cull Off             
        ZWrite On
        ZTest LEqual

        HLSLINCLUDE
        #pragma target 3.0

        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceData.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/BRDF.hlsl"

        TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);

        CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            float4 _Color;
            float  _Glossiness;
            float  _Metallic;
        CBUFFER_END

        struct Attributes
        {
            float3 positionOS : POSITION;
            float3 normalOS   : NORMAL;
            float4 tangentOS  : TANGENT;
            float2 uv         : TEXCOORD0;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct Varyings
        {
            float4 positionHCS : SV_POSITION;
            float3 positionWS  : TEXCOORD0;
            float3 normalWS    : TEXCOORD1;
            float4 tangentWS   : TEXCOORD2;
            float2 uv          : TEXCOORD3;
            UNITY_VERTEX_INPUT_INSTANCE_ID
            UNITY_VERTEX_OUTPUT_STEREO
        };

        Varyings vert (Attributes v)
        {
            Varyings o;
            UNITY_SETUP_INSTANCE_ID(v);
            UNITY_TRANSFER_INSTANCE_ID(v, o);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

            o.positionWS   = TransformObjectToWorld(v.positionOS);
            o.positionHCS  = TransformWorldToHClip(o.positionWS);

            VertexNormalInputs ni = GetVertexNormalInputs(v.normalOS, v.tangentOS);
            o.normalWS  = ni.normalWS;
            o.tangentWS = float4(ni.tangentWS, v.tangentOS.w);
            o.uv        = TRANSFORM_TEX(v.uv, _MainTex);
            return o;
        }

        void BuildSurfaceData(float2 uv, out SurfaceData sd)
        {
            half4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv) * _Color;
            sd.albedo     = c.rgb;
            sd.alpha      = 1;           
            sd.normalTS   = float3(0,0,1); 
            sd.metallic   = _Metallic;
            sd.smoothness = _Glossiness;
            sd.occlusion  = 1;
            sd.emission   = 0;
            sd.specColor  = 0;
        }

        // 由几何法线（双面支持）
        void BuildInputData(in Varyings i, bool isFrontFace, out InputData id)
        {
            id = (InputData)0;
            id.positionWS     = i.positionWS;

            // 基于几何法线；背面取反以获得正确光照
            float3 n = normalize(i.normalWS);
            n = isFrontFace ? n : -n;
            id.normalWS       = n;

            id.viewDirectionWS = normalize(GetWorldSpaceViewDir(i.positionWS));
            id.shadowCoord     = TransformWorldToShadowCoord(i.positionWS);
            id.bakedGI         = SAMPLE_GI(i.positionWS, n);
            id.fogCoord        = 0;
        }
        ENDHLSL

        // --------- Forward Lit ---------
        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode"="UniversalForward" }

            // 关键多编译
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            half4 frag (Varyings i
                        , bool isFrontFace : SV_IsFrontFace) : SV_Target
            {
                SurfaceData sd; BuildSurfaceData(i.uv, sd);
                InputData   id; BuildInputData(i, isFrontFace, id);

                half4 col = UniversalFragmentPBR(id, sd);
                return col;
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Off

            HLSLPROGRAM
            #pragma vertex   ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShadowCasterPass.hlsl"

            struct AttributesSC
            {
                float3 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct VaryingsSC
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS  : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
            };

            VaryingsSC ShadowPassVertex(AttributesSC v)
            {
                VaryingsSC o;
                float3 ws = TransformObjectToWorld(v.positionOS);
                float3 n  = TransformObjectToWorldNormal(v.normalOS);
                o.positionWS = ws;
                o.normalWS   = n;
                o.positionHCS = GetShadowPositionHClips(ws, n);
                return o;
            }

            half4 ShadowPassFragment(VaryingsSC i, bool isFrontFace:SV_IsFrontFace) : SV_Target
            {
                float3 n = normalize(i.normalWS);
                n = isFrontFace ? n : -n;
    
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ColorMask 0
            ZWrite On
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment fragDepth
            half4 fragDepth(Varyings i) : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "Meta"
            Tags { "LightMode"="Meta" }
            Cull Off

            HLSLPROGRAM
            #pragma vertex   UniversalVertexMeta
            #pragma fragment UniversalFragmentMeta
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);

            float4 _Color;
            float4 _MainTex_ST;
            float  _Metallic;
            float  _Glossiness;

            void InitializeStandardLitSurfaceData(float2 uv, out SurfaceData sd)
            {
                half4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv) * _Color;
                sd.albedo     = c.rgb;
                sd.alpha      = 1;
                sd.normalTS   = float3(0,0,1);
                sd.metallic   = _Metallic;
                sd.smoothness = _Glossiness;
                sd.occlusion  = 1;
                sd.emission   = 0;
                sd.specColor  = 0;
            }
            ENDHLSL
        }
    }

    FallBack Off
}