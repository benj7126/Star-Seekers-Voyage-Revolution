Shader "Hidden/AtmosphereShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "Assets/Game/Noise.compute"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 ViewDir : TEXCOORD1;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;

                o.ViewDir = mul(unity_CameraInvProjection, float4(v.uv * 2 - 1, 0, -1));
                o.ViewDir = mul(unity_CameraToWorld, float4(o.ViewDir, 0));

                return o;
            }

            sampler2D _MainTex;
            sampler2D _CameraDepthTexture;

            float3 _SunPos;

            float3 _PlanetCenter[128];
            float _PlanetRadius[128];
            
            float4 _AtmosphereColor[128];

            int _PlanetAmount = 0;
            

            // Stolen from here: https://gist.github.com/wwwtyro/beecc31d65d1004f5a9d?permalink_comment_id=2982787#gistcomment-2982787
            // Converted from GLSL to HLSL
            float2 RaySphere(float3 RayOrigin, float3 RayDir, float3 SphereOrigin, float SphereRadius)
            {
                float a = dot(RayDir, RayDir);
                float3 s0_r0 = RayOrigin - SphereOrigin;
                float b = 2.0 * dot(RayDir, s0_r0);
                float c = dot(s0_r0, s0_r0) - (SphereRadius * SphereRadius);
                float disc = b * b - 4.0 * a * c;

                if (disc < 0.0)
                {
                    return float2(-1.0, -1.0);
                }

                return float2(-b - sqrt(disc), -b + sqrt(disc)) / (2.0 * a);
            }

            fixed4 CalculatePlanet(fixed4 BaseCol, float3 CamPos, float3 RayDir, float PixelDepth, float3 PlanetCenter, float4 PlanetRadius, fixed4 AtmosphereColor)
            {
                float2 RayOutput = RaySphere(CamPos, RayDir, PlanetCenter, PlanetRadius);

                RayOutput.x = max(RayOutput.x, 0);
                if (min(PixelDepth - RayOutput.x, RayOutput.y) > 0)
                {
                    float3 WorldPos = (CamPos + normalize(RayDir) * RayOutput.x) - PlanetCenter;
                    float3 WorldPosBack = WorldPos + (RayDir * (RayOutput.y - RayOutput.x));
                    
                    float ClosestPoint = min(PixelDepth, RayOutput.y);

                    float LengthInside = ClosestPoint - RayOutput.x;
                    LengthInside /= PlanetRadius * 2;
                    LengthInside = pow(LengthInside, 1.3);

                    // Sun Stuff
                    // ========
                    float3 Norm = normalize(WorldPos - PlanetCenter);
                    float3 LightDir = normalize(_SunPos - WorldPos);
                    
                    float3 LightDirBack = normalize(_SunPos - WorldPosBack);
                    
                    float SunAngleFront = saturate(dot(LightDir, Norm) + 0.3);
                    float SunAngleBack = saturate(dot(LightDirBack, Norm) + 0.3);

                    float SunAngle = SunAngleFront;
                    //if(PixelDepth < 132412341)
                    //    SunAngle = (SunAngleFront + SunAngleBack)/2;

                    return lerp(BaseCol, AtmosphereColor, LengthInside * SunAngle );
                }
                return fixed4(0, 0, 0, 0);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 BaseCol = tex2D(_MainTex, i.uv);

                float3 RayOrigin = _WorldSpaceCameraPos;
                float3 RayDir = i.ViewDir;
                float Depth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv));

                fixed4 WaterCol = fixed4(0, 0, 0, 0);
                for(int j = 0; j < _PlanetAmount; j++)
                    WaterCol += CalculatePlanet(BaseCol, RayOrigin, RayDir, Depth, _PlanetCenter[j], _PlanetRadius[j] * 1.25, _AtmosphereColor[j]);

                if(WaterCol.w == 0)
                    return BaseCol;
                
                return WaterCol;
            }
            ENDCG
        }



    }
}