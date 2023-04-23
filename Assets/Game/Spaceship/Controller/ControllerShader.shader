// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Unlit/ControllerShade"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _TargetDistance ("Target Distance", float) = 0
        _Scale ("Cur Scale", Vector) = (1, 1, 1)
        _MaxDist ("_MaxDist", float) = 0
        _MinDist ("_MinDist", float) = 0

        _FresnelBias ("Fresnel Bias", float) = 0
        _FresnelScale ("Fresnel Scale", float) = 0
        _FresnelPower ("Fresnel Power", float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue" = "Transparent" }

        //CULL OFF
        BLEND SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float R : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float3 _Scale;
            float _TargetDistance;
            float _MaxDist;
            float _MinDist;

            float _FresnelBias;
            float _FresnelScale;
            float _FresnelPower;

            v2f vert (appdata v)
            {
                v2f o;
                float weight = length(v.vertex * _Scale);
                weight = (weight-_MinDist)*1/(_MaxDist-_MinDist);
                v.vertex += float4(0, 0, _TargetDistance/_MinDist * -weight * 2, 0);// * weight; float4(_TargetPos/_Scale, 0);
                
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                float3 posWorld = mul(unity_ObjectToWorld, v.vertex).xyz;
                float3 normWorld = normalize(mul(unity_ObjectToWorld, v.normal));

                float3 I = normalize(posWorld - _WorldSpaceCameraPos.xyz);
                o.R = _FresnelBias + _FresnelScale * pow(1.0 + dot(I, normWorld), _FresnelPower);

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                /*
                fixed4 col = tex2D(_MainTex, i.uv * 5);

                clip(col-0.8);

                return fixed4(0.3, 0.3, 1, 0.5);
                */

                return fixed4(0, 0, 1, i.R);
            }
            ENDCG
        }
    }
}
