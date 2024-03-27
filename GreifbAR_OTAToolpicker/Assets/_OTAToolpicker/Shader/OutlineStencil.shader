Shader "NMYCollection/OutlineStencil"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (1,1,1,1)
        _OutlineThickness("Outline Thickness", Range(0,3)) = 0.5
        _OutlineTimer ("Outline Alpha Pulse Speed (1 = Default Speed)", Float) = 1
        _OutlineAlpha ("Outline Alpha", Range (0,1)) = 1
        [Toggle(OUTLINES_ON)] _Outline("Show Outline",Float) = 1
		[Toggle(ALPHA_PULSE_ON)] _Pulse("Animate Pulse",Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+1" }
        Cull Front
        Blend SrcAlpha OneMinusSrcAlpha
        LOD 100
        ZWrite Off
        Stencil 
        {
            Ref 2
            Comp notequal
        }
        Pass
        {
			Name "Outline"
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile OUTLINES_ON __
			#pragma multi_compile ALPHA_PULSE_ON __

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL; 
            };

			struct v2f
			{
				UNITY_FOG_COORDS(1)
				float4 vertex : SV_POSITION;
				UNITY_VERTEX_OUTPUT_STEREO
			};

            sampler2D _MainTex;
            float4 _MainTex_ST;
			fixed4 _OutlineColor;
			fixed _OutlineAlpha;
			half _OutlineTimer;
			half _OutlineThickness;
			v2f vert (appdata_base v)
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
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
				fixed4 c = _OutlineColor * _OutlineAlpha;
				#ifdef ALPHA_PULSE_ON
				c *= (sin(_Time.y * _OutlineTimer)*0.5 + 0.5);
				#else
				c *= _OutlineColor.a;
				#endif

				return c;
			}
            ENDCG
        }
    }
}
