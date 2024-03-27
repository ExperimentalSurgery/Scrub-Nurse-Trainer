Shader "NMYCollection/StandardOutlineStencil"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _MetallicGlossMap("Metallic(R) Smoothness (A)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _BumpMap ("Normal Map (RGB)", 2D) = "bump"{}
        _NormalInt ("Normal Intensity", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Transparent" }
        LOD 200
        Stencil
        {
            Ref 2
            Comp Always
            Pass Replace
        }

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert alpha:fade
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _MetallicGlossMap;
        sampler2D _BumpMap;

        struct Input
        {
            float2 uv_MainTex;
            float3 viewDir;
            float3 normalDir;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
        half _NormalInt;
        fixed4 _RimColor;
        half _RimInt;
        half _RimStr;
        void vert(inout appdata_full v, out Input o) {
            UNITY_INITIALIZE_OUTPUT(Input, o)
            o.normalDir = normalize(mul(float4(v.normal, 0.0), unity_WorldToObject).xyz);
        }
        UNITY_INSTANCING_BUFFER_START(Props)

        UNITY_INSTANCING_BUFFER_END(Props)

        void surf(Input i, inout SurfaceOutputStandard o)
        {
            float rim = 1 - saturate(dot(i.normalDir, i.viewDir));
            float3 rimComb = _RimColor.rgb *_RimInt* pow(rim, _RimStr);
            fixed4 c = tex2D(_MainTex, i.uv_MainTex);
            o.Albedo = _Color * c.rgb + rimComb;
            o.Metallic = tex2D(_MetallicGlossMap, i.uv_MainTex).r * _Metallic;
            o.Smoothness = tex2D(_MetallicGlossMap, i.uv_MainTex).a * _Glossiness;
            o.Alpha =  _Color.a;
            o.Normal = UnpackNormal(tex2D(_BumpMap, i.uv_MainTex));
            o.Normal.xy *= _NormalInt;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
