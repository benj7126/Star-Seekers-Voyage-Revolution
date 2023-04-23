Shader "Unlit/SunShader"
{
    Properties
    {
        _SunColor ("Sun Color", Color) = (1, 1, 1)
        _SpotColor ("Spot Color", Color) = (1, 1, 1)
        
        _SpotSize ("Spot Size", float) = 10
        _SpotCutoff ("Spot Cutoff", float) = 0.3
        _SpotCoefficient ("Spot Coefficient", float) = 2
        
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
            #include "Assets/Game/Noise.compute"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float3 normal : TEXCOORD1;
                float3 worldpos : TEXCOORD2;
                float R : TEXCOORD6;
            };
            
            float4 _SunColor;
            float4 _SpotColor;

            float _SpotSize;
            float _SpotCutoff;
            float _SpotCoefficient;
            
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
                o.worldpos = mul(unity_ObjectToWorld, v.vertex);
                o.normal = v.normal;
                
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

                float SpotNoise = saturate((snoise((i.worldpos+(_Time.y*0.2) * (_SinTime.x*0.04))/_SpotSize)-_SpotCutoff)*_SpotCoefficient);
                
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                float4 FinalColor = lerp(_SunColor, _SpotColor, SpotNoise);
                
                return lerp(FinalColor, _FresnelColor, i.R * _FresnelStrength);
            }
            ENDCG
        }
    }
}