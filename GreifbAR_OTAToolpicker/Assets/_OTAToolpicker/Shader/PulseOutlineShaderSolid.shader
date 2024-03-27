// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "NMYCollection/PulseShader/PulseOutlineShaderSolid"
{
	Properties
	{
		_Color ("Main Color", Color) = (1,1,1,1)
		_OutlineColor ("Outline Color", Color) = (1,1,1,1)
		_RimColor ("Rim Color", Color) = (1,1,1,1)
		_MainTex ("Texture", 2D) = "white" {}
		_MSTex ("Metallic(R) Smoothness(A)", 2D) = "white"{}
		_MS ("MetallicSmoothness", Range(0,1)) = 0
		//_NRMTex ("Normal Map", 2D) = "bump" {}
		_OutlineThickness("Outline Thickness", Range(0,3)) = 0.5
		_OutlineTimer ("Outline Alpha Pulse Speed (1 = Default Speed)", Float) = 1
		_RimStr("Rim Strength", Range(0,9)) = 1
		_RimInt("Rim Intensity", Range (0,5)) = 1
		_Alpha ("Outline Alpha", Range (0,1)) = 1
		[Toggle(OUTLINES_ON)] _Outline("Show Outline",Float) = 1
		[Toggle(ALPHA_PULSE_ON)] _Pulse("Animate Pulse",Float) = 1

	}
	SubShader
	{
		Tags { "Queue" = "Transparent" "RenderType"="Transparent" }
		//Blend One One
		Blend SrcAlpha OneMinusSrcAlpha
		LOD 100
		Cull Front
		Pass
		{
			Name "Outline"
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile OUTLINES_ON __
			#pragma multi_compile ALPHA_PULSE_ON __
			// make fog work
			#pragma multi_compile_fog

			#include "UnityCG.cginc"


			struct v2f
			{
				float2 uv : TEXCOORD0;
				UNITY_FOG_COORDS(1)
				float4 vertex : SV_POSITION;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			fixed4 _OutlineColor;
			fixed _Alpha;
			half _OutlineTimer;
			half _OutlineThickness;


			v2f vert (appdata_base v)
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
			#ifdef OUTLINES_ON
				//half4 projVertex = UnityObjectToClipPos(v.vertex);
				//half4 projNormal = (UnityObjectToClipPos(half4(v.normal, 0.0)));
				//half4 scaleNormal = 0.05 * _OutlineThickness * projNormal;
				//scaleNormal.z += 0.0001;
				v.vertex.xyz += v.normal * _OutlineThickness *0.05;
				o.vertex = UnityObjectToClipPos(v.vertex);
				//o.vertex += projVertex + scaleNormal;
			#else
				o.vertex = UnityObjectToClipPos(v.vertex);
			#endif
				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
				// apply fog
				UNITY_APPLY_FOG(i.fogCoord, _OutlineColor);
				fixed4 c = _OutlineColor * _Alpha;
				#ifdef ALPHA_PULSE_ON
				c *= (sin(_Time.y * _OutlineTimer)*0.5 + 0.55);
				#else
				c *= _OutlineColor.a;
				#endif

				return c;
			}
			ENDCG
		}

			Tags{ "Queue" = "Geometry" "RenderType" = "Opaque" }
		Cull Back
			CGPROGRAM
#pragma surface surf Standard fullforwardshadows vertex:vert
#pragma target 3.0

			struct Input
			{
				float2 uv_MainTex;
				float2 uv_NRMTex;
				float2 uv_MSTex;
				float3 viewDir;
				float3 normalDir;
			};

			void vert(inout appdata_full v, out Input o) {
				UNITY_INITIALIZE_OUTPUT(Input, o)
					o.normalDir = normalize(mul(float4(v.normal, 0.0), unity_WorldToObject).xyz);
			}

			sampler2D _MainTex;
			sampler2D _MSTex;
			sampler2D _NRMTex;
			fixed4 _RimColor;
			half _RimInt;
			half _RimStr;
			fixed4 _Color;
			//fixed _Alpha;
			fixed _MS;
			void surf(Input i, inout SurfaceOutputStandard o)
			{
				float rim = 1 - saturate(dot(i.normalDir, i.viewDir));
				float3 rimComb = _RimColor.rgb *_RimInt* pow(rim, _RimStr);
				fixed4 c = tex2D(_MainTex, i.uv_MainTex);
				o.Albedo = _Color * c.rgb + rimComb;
				//o.Albedo = rim;
				// Metallic and smoothness come from slider variables
				o.Metallic = tex2D(_MSTex, i.uv_MSTex).r;
				o.Smoothness = tex2D(_MSTex, i.uv_MSTex).a * _MS;
				//o.Metallic = _MS;
				//o.Smoothness = _MS;
				o.Alpha =  _Color.a;
				//o.Normal = UnpackNormal(tex2D(_NRMTex, i.uv_NRMTex));
			}
			ENDCG
	}
			Fallback "Diffuse"
}
