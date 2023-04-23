Shader "Unlit/WaterShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _PlanetCenter ("Planet Center", Vector) = (0, 0, 0)
        _PlanetRadius ("Planet Radius", float) = 1000
        _TopColor ("Shallow Color", Vector) = (0, 0, 0)
        _BottomColor ("Deep Color", Vector) = (0, 0, 0)
        
        _NormalMap ("Normal Map", 2D) = "white" {}
        _NormalSize ("Scale of normal map", float) = 0
        _NormalStrength ("Strength of normal map", float) = 0
    }
    SubShader
    {
        Tags
        {
            "RenderType"    = "Transparent"
            "Queue"         = "Transparent" 
        }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        CULL off

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
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float2 depth : TEXCOORD1;
                float4 worldpos : TEXCOORD2;
                float4 screenPos : TEXCOORD3;
            };

            sampler2D _NormalMap;
            
            sampler2D _MainTex;
            float4 _MainTex_ST;

            //sampler2D _CameraDepthTexture;
            UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);

            float3 _SunPos = float3(-2000, 3000, 0);

            float3 _PlanetCenter;
            float _PlanetRadius;
            float4 _TopColor;
            float4 _BottomColor;

            float _NormalSize;
            float _NormalStrength;
            
            float map(float X, float A, float B, float C, float D)
            {
                return (X - A) / (B - A) * (D - C) + C;
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.screenPos = ComputeScreenPos(o.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o, o.vertex);

                o.worldpos = mul(unity_ObjectToWorld, v.vertex);

                return o;
            }


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

            fixed4 CalculatePlanet(fixed4 BaseCol, float3 CamPos, float3 RayDir, float PixelDepth, float3 PlanetCenter,
                                   float4 PlanetRadius, float4 TopColor, float4 BottomColor)
            {
                //CamPos = float3(2200, 0, 0);
                //RayDir = float3(-1, 0, 0);
                float2 RayOutput = RaySphere(CamPos, normalize(RayDir), PlanetCenter, PlanetRadius);
                RayOutput.x = length(RayDir);
                //return float4(RayOutput.x, RayOutput.y, 0, 1);

                if (min(PixelDepth - RayOutput.x, RayOutput.y) > 0)
                {
                    float3 WorldPos = (CamPos + normalize(RayDir) * RayOutput.x) - PlanetCenter;

                    float WaterFoamBlend = 0;
                    if (PixelDepth - RayOutput.x < 20)
                    {
                        WaterFoamBlend = 1 - ((PixelDepth - RayOutput.x) / 20);
                        float FoamMask = map(
                            snoise((WorldPos + float3(sin(_Time.w) * 10 + _Time.w * 5, sin(_Time.w * 0.1) * 100,
                                                      cos(_Time.w * 0.15) * 100)) / 20), -1, 1, 0, 1.0);
                        FoamMask = saturate(FoamMask * FoamMask * FoamMask * 0.95);
                        WaterFoamBlend += WaterFoamBlend * 0.2;
                        WaterFoamBlend -= FoamMask;
                        WaterFoamBlend = saturate(WaterFoamBlend);
                    }


                    float4 WaterColor;
                    float ClosestPoint = min(PixelDepth, RayOutput.y);
                    float alpha = (ClosestPoint - RayOutput.x) / 400;
                    alpha = exp(alpha) - 0.5;

                    float mixer = saturate(alpha - 0.6);

                    alpha = saturate(alpha);

                    WaterColor = lerp(TopColor, BottomColor, mixer);
                    float3 WaterMixCol = BaseCol * (1 - alpha) + WaterColor * alpha;

                    // LIGHTING
                    // ========
                    float3 ViewDir = normalize(_WorldSpaceCameraPos - WorldPos);
                    float3 Norm = normalize(WorldPos - PlanetCenter);
                    float3 LightDir = normalize(WorldPos - _SunPos);

                    // DIFFUSE LIGHTING
                    // ----------------
                    float DiffuseLightAmount = saturate(dot(LightDir, Norm) + 0.05);
                    float3 DiffuseCol = WaterMixCol * DiffuseLightAmount;

                    // SPECULAR HIGHLIGHTING
                    // ---------------------
                    float3 ReflectDir = reflect(LightDir, Norm);

                    float spec = pow(max(dot(ViewDir, ReflectDir), 0.0), 32);
                    spec *= 0.5;

                    return lerp(float4(DiffuseCol + spec, 1), float4(1, 1, 1, 1) * DiffuseLightAmount, WaterFoamBlend);
                }
                return fixed4(1, 1, 0, 0);
            }
            
            // Reoriented Normal Mapping
            // http://blog.selfshadow.com/publications/blending-in-detail/
            // Altered to take normals (-1 to 1 ranges) rather than unsigned normal maps (0 to 1 ranges)
            float3 blend_rnm(float3 n1, float3 n2)
            {
                n1.z += 1;
                n2.xy = -n2.xy;

                return n1 * dot(n1, n2) / n1.z - n2;
            }
            
            // Sample normal map with triplanar coordinates
            // Returned normal will be in obj/world space (depending whether pos/normal are given in obj or world space)
            // Based on: medium.com/@bgolus/normal-mapping-for-a-triplanar-shader-10bf39dca05a
            float3 triplanarNormal(float3 vertPos, float3 normal, float3 scale, float2 offset, sampler2D normalMap)
            {
                float3 absNormal = abs(normal);

                // Calculate triplanar blend
                float3 blendWeight = saturate(pow(normal, 4));
                // Divide blend weight by the sum of its components. This will make x + y + z = 1
                blendWeight /= dot(blendWeight, 1);

                // Calculate triplanar coordinates
                float2 uvX = vertPos.zy * scale + offset;
                float2 uvY = vertPos.xz * scale + offset;
                float2 uvZ = vertPos.xy * scale + offset;

                // Sample tangent space normal maps
                // UnpackNormal puts values in range [-1, 1] (and accounts for DXT5nm compression)
                float3 tangentNormalX = UnpackNormal(tex2D(normalMap, uvX));
                float3 tangentNormalY = UnpackNormal(tex2D(normalMap, uvY));
                float3 tangentNormalZ = UnpackNormal(tex2D(normalMap, uvZ));

                // Swizzle normals to match tangent space and apply reoriented normal mapping blend
                tangentNormalX = blend_rnm(half3(normal.zy, absNormal.x), tangentNormalX);
                tangentNormalY = blend_rnm(half3(normal.xz, absNormal.y), tangentNormalY);
                tangentNormalZ = blend_rnm(half3(normal.xy, absNormal.z), tangentNormalZ);

                // Apply input normal sign to tangent space Z
                float3 axisSign = sign(normal);
                tangentNormalX.z *= axisSign.x;
                tangentNormalY.z *= axisSign.y;
                tangentNormalZ.z *= axisSign.z;

                // Swizzle tangent normals to match input normal and blend together
                float3 outputNormal = normalize(
                    tangentNormalX.zyx * blendWeight.x +
                    tangentNormalY.xzy * blendWeight.y +
                    tangentNormalZ.xyz * blendWeight.z
                );

                return outputNormal;
            }

            float3 DepthToWorld(float2 uv, float depth)
            {
                float z = (1 - depth) * 2.0 - 1.0;

                float4 clipSpacePosition = float4(uv * 2.0 - 1.0, z, 1.0);

                float4 viewSpacePosition = mul(unity_CameraInvProjection, clipSpacePosition);
                viewSpacePosition /= viewSpacePosition.w;

                float4 worldSpacePosition = mul(unity_ObjectToWorld, viewSpacePosition);

                return worldSpacePosition.xyz;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 BaseCol = _TopColor;

                float3 RayDir = i.worldpos - _WorldSpaceCameraPos;
                float3 RayOrigin = _WorldSpaceCameraPos;
                float2 DepthUV = i.screenPos.xy / i.screenPos.w;
                //float Depth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, DepthUV));
                float Depth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, DepthUV));

                float DepthMix = saturate((Depth - length(RayDir))/200);
                float DepthMixLight = saturate((Depth - length(RayDir*0.9))/300);

                //return float4(DepthMix.xxx, 1);
                

                float3 WaterBaseCol = lerp(_TopColor, _BottomColor, DepthMix);
                
                // LIGHTING
                // ========
                float3 ViewDir = normalize(_WorldSpaceCameraPos - i.worldpos);
                float3 Norm = normalize(i.worldpos- _PlanetCenter);
                float3 LightDir = normalize(i.worldpos - _SunPos);

                float3 MappedNormal = triplanarNormal(i.worldpos - _PlanetCenter, Norm, _NormalSize / _PlanetRadius, float2(_Time.x, _Time.x*-1.2), _NormalMap);
                MappedNormal = normalize(lerp(Norm, MappedNormal, _NormalStrength));
                
                // DIFFUSE LIGHTING
                // ----------------
                float DiffuseLightAmount = dot(-LightDir, MappedNormal);
                DiffuseLightAmount = exp(DiffuseLightAmount*0.47)-0.62;
                DiffuseLightAmount *= DiffuseLightAmount;
                DiffuseLightAmount -= DepthMixLight * 0.15;
                float3 DiffuseCol = WaterBaseCol * DiffuseLightAmount;

                // SPECULAR HIGHLIGHTING
                // ---------------------
                float3 ReflectDir = reflect(LightDir, MappedNormal);

                float spec = pow(max(dot(ViewDir, ReflectDir), 0.0), 32);
                spec *= 0.5;
                
                return float4(DiffuseCol + spec, 0.9);
                
                //Depth /= normalize(i.vertex.xyz).z;

                fixed4 WaterCol = CalculatePlanet(BaseCol, RayOrigin, RayDir, Depth, _PlanetCenter, _PlanetRadius,
                                                  _TopColor, _BottomColor);

                if (WaterCol.w == 0)
                    discard;

                return WaterCol;
            }
            ENDCG
        }
    }
}