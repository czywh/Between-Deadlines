Shader "Custom/CustomCelToon"
{
    Properties
    {
		// texture & Base Color 
        _MainTex("Albedo", 2D) = "white" {}
		_Color ("Tint", Color) = (1, 1, 1, 1) // adjusting Color for the model 

		// Outline parameter 
		_OutlineColor ("Outline Color", Color) = (0, 0, 0, 1)
		_OutlineWidthPx ("Outline Width (pixels)", Range(0, 3)) = 0.35

		// Diffuse 
		_Steps ("Light Steps", Range(1, 5)) = 3
		_Feather ("Feather (0 = hard)", Range(0, 0.5)) = 0.05 // Control the mixing ratio

		// Specular
		_SpecColor ("Spec Color", Color) = (1, 1, 1, 1)
		_SpecStrength ("Spec Strength", Range(0, 2)) = 0.5
		_Gloss ("Glossiness (BlinnPow)", Range(8, 256)) = 64

		// Rim light 
		_RimColor ("Rim Color", Color) = (1, 1, 1, 1)
		_RimPower ("Rim Power", Range(0.5, 8)) = 2

		_Ramp ("Optional Ramp (grayscale)", 2D) = "gray" {}
 
    }

    SubShader
    {
        Tags { "RenderType"="Opaque"  "Queue" = "Geometry"}
        LOD 300

		// Pass 0: Outline (inverted hull)
		Pass
		{
			// Draw a uniform-colored line around the outer contour of the model.
			Name "OUTLINE"
			Cull Front 
			ZWrite On
			ZTest LEqual 
			ColorMask RGB

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			fixed4 _OutlineColor; 
			float _OutlineWidthPx;

			struct appdata{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
			};

			struct v2f
			{
				float4 pos: SV_POSITION;
			};

			v2f vert (appdata v)
			{
				v2f o;

				// Using clip postion to aviod Foreshortening effects
				float4 clipPos = UnityObjectToClipPos(v.vertex);

				float3 viewN = mul((float3x3)UNITY_MATRIX_IT_MV, v.normal);
				viewN.z = 0; // we dont need depth here 
				float2 dir = normalize(viewN.xy + 1e-6);

				float2 ndcPerPixel = 2.0 / _ScreenParams.xy;

				float2 ndcOffset = dir * (_OutlineWidthPx * ndcPerPixel.y);

				clipPos.xy += ndcOffset * clipPos.w;

				o.pos = clipPos;
				return o;
			}

			fixed4 frag (v2f i): SV_Target 
			{
				return _OutlineColor;
			}
			ENDCG
		}

		// Pass 1: Forward Base (main direction)
		Pass{
			// Quantify the continuous physical lighting (N·L, specular) into a small number of color gradation
			// forming "hard-edge shadows".
			Tags {"LightMode"="ForwardBase"}
			Cull Back
			ZWrite On 
			ZTest LEqual

			CGPROGRAM
			#pragma target 3.0
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fwdbase
			#pragma multi_compile_fog
			#pragma shader_feature _USE_RAMP

			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "AutoLight.cginc"

			sampler2D _MainTex; float4 _MainTex_ST;
            fixed4 _Color;
            float  _Steps; float _Feather;
            float _SpecStrength; float _Gloss;
            fixed4 _RimColor;  float _RimPower;
            sampler2D _Ramp;

			struct appdata
			{
				float4 vertex: POSITION;
				float3 normal: NORMAL;
				float4 tangent: TANGENT;
				float2 uv: TEXCOORD0;
			};

			struct v2f 
			{
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
				float3 wPos : TEXCOORD1;
				float3 wNormal : TEXCOORD2;
				UNITY_FOG_COORDS(3)
				SHADOW_COORDS(4)
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);

				o.wPos = mul(unity_ObjectToWorld, v.vertex).xyz;
				o.wNormal = UnityObjectToWorldNormal(v.normal);

				UNITY_TRANSFER_FOG(o, o.pos);
				TRANSFER_SHADOW(o);
				return o;
			};

			fixed4 frag(v2f i): SV_Target
			{
				// Texture & Base Color
				// _Color(1, 1, 1): Original Color
				// _Color(0.5, 0.5, 0.5): Texture more close to red 
				fixed3 albedo = tex2D(_MainTex, i.uv).rgb * _Color.rgb;

				// Lighting vectors 
				float3 n = normalize(i.wNormal);
				float3 l = normalize(_WorldSpaceLightPos0.xyz); // Direction of Main Light
				float3 v = normalize(_WorldSpaceCameraPos.xyz - i.wPos); // Direction of Views 
				float3 h = normalize(l + v); // half-angle vector

				// dot(N, L) with shadow attenuation 
				float ndl = saturate(dot(n, l));
				UNITY_LIGHT_ATTENUATION(atten, i, i.wPos); // shadow + distance decay 


				// Cel step quantization (core)
				// protect against Steps <= 1 
				float steps = max(_Steps, 1.0); 

				// Continuous values -> quantified into several fixed color blocks
				// Feature: Mixture ratio, (Feature = 0; Completely fixed color blocks)
				float q = floor(ndl * steps);
				float denom = max(steps - 1, 1.0); 
				float stepped = q/denom; // 0..1 in discrete bands
				stepped = lerp(stepped, ndl, saturate(_Feather * 2));

				// Optional ramp
				#if defined(_USE_RAMP)
				stepped = tex2D(_Ramp, float2(saturate(stepped), 0.5)).r;
				#endif

				// Diffuse with main light color & attenuation
				fixed3 diffuse = albedo * stepped *_LightColor0.rgb * atten;

				// Specular (Blin-Phong), lightly quantized to match toon feel
				float spec = pow(saturate(dot(n, h)), _Gloss);
				spec = step(0.5, spec) * _SpecStrength; // Crude toon spec step 
				fixed3 specCol = spec * _SpecColor.rgb * _LightColor0.rgb * atten;

				// Rim light (Frensnel-like)
				float rim = pow(1.0 - saturate(dot(n, v)), _RimPower);
				fixed3 rimColor = _RimColor.rgb * rim;

				// Ambient (legacy)
				fixed3 amb = UNITY_LIGHTMODEL_AMBIENT.rgb * albedo; 

				fixed3 color = amb + diffuse + specCol + rimColor; 
				fixed4 col = fixed4(color, 1);

				UNITY_APPLY_FOG(i.fogCoord, col);

				return col;
			}
			ENDCG
		}
	}
	FallBack "Diffuse"
}
