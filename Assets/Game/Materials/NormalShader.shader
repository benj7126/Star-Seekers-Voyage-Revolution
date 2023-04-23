Shader "Unlit/NormalShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _NormalTex ("Normal Map", 2D) = "white" {}
        _SpecularTex ("Specular Map", 2D) = "white" {}

        _SunPos ("Sun Position", Vector) = (0, 0, 0)

        _AmbientColor ("Abient Light Color", Color) = (1, 1, 0.95)
        _DiffuseColor ("Diffuse Light Color", Color) = (1, 1, 0.95)
        _SpecularColor ("Specular Light Color", Color) = (1, 1, 0.95)

        _SpecularStrength ("Specular Strength", float) = 32

        _NormalStrength ("Strength of Normal Map", float) = 1

        [MaterialToggle] _InvertSpecular ("Invert Specular Map", int) = 1
        
        _FresnelStrength ("Fresnel Strength", float) = 1
        
        _FresnelBias ("Fresnel Bias", float) = 0
        _FresnelScale ("Fresnel Scale", float) = 0
        _FresnelPower ("Fresnel Power", float) = 0
        _FresnelColor ("Fresnel Color", Color) = (1, 1, 1)
        
        [MaterialToggle] _DebugFresnel ("Debug Fresnel", int) = 1
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
        }
        LOD 100

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
                float3 tangent : TANGENT;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float3 normal : TEXCOORD1;
                float3 worldpos : TEXCOORD2;
                float3x3 TBN : TEXCOORD3;
                float R : TEXCOORD6;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            sampler2D _SpecularTex;
            float4 _SpecularTex_ST;

            sampler2D _NormalTex;
            float4 _NormalTex_ST;

            float3 _SunPos;

            float4 _AmbientColor;
            float4 _DiffuseColor;
            float4 _SpecularColor;

            float _SpecularStrength;

            float _NormalStrength;

            int _InvertSpecular;

            float _FresnelStrength;
            
            float _FresnelBias;
            float _FresnelScale;
            float _FresnelPower;
            
            float4 _FresnelColor;

            int _DebugFresnel;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldpos = mul(unity_ObjectToWorld, v.vertex);
                o.normal = v.normal;

                float3 bitangent = cross(v.normal, v.tangent.xyz);
                o.TBN = float3x3(v.tangent.xyz, bitangent, v.normal);
                o.TBN = transpose(o.TBN);

                
                float3 WorldNormal = normalize(mul(unity_ObjectToWorld, v.normal));

                float3 I = normalize(o.worldpos - _WorldSpaceCameraPos.xyz);
                o.R = _FresnelBias + _FresnelScale * pow(1.0 + dot(I, WorldNormal), _FresnelPower);

                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                if(_DebugFresnel)
                    return float4(i.R, -i.R, 0, 1);
                // LIGHTING
                // ========
                float3 ViewDir = normalize(_WorldSpaceCameraPos - i.worldpos);
                float3 LightDir = normalize(i.worldpos - _SunPos);

                // NORMAL MAPPING
                // --------------
                float3 Norm = UnpackNormal(tex2D(_NormalTex, i.uv)).rgb;
                //Norm = Norm * 2.0 - 1.0;
                Norm = mul(i.TBN, Norm);
                Norm = normalize(Norm);

                Norm = normalize(lerp(i.normal, Norm, _NormalStrength));

                

                float4 TexCol = tex2D(_MainTex, i.uv);

                // AMBIENT LIGHTING
                // ----------------
                float3 Ambient = _AmbientColor * 0.05 * TexCol.xyz;

                // DIFFUSE LIGHTING
                // ----------------
                float DiffuseLightAmount = saturate(dot(LightDir, Norm) + 0.05);
                float3 Diffuse = _DiffuseColor * DiffuseLightAmount * TexCol.xyz;

                // SPECULAR HIGHLIGHTING
                // ---------------------
                float3 ReflectDir = reflect(LightDir, Norm);

                float Spec = pow(max(dot(ViewDir, ReflectDir), 0.0), _SpecularStrength);
                Spec *= 0.5;

                float3 Specular = _SpecularColor * Spec * (_InvertSpecular - tex2D(_SpecularTex, i.uv));

                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                float4 FinalColor = float4(Diffuse + Specular + Ambient, 1);
                
                return lerp(FinalColor, _FresnelColor, i.R * _FresnelStrength);
            }
            ENDCG
        }
    }
}