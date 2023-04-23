Shader "Unlit/PlanetShader"
{
    Properties
    {
        _NormalMap ("Normal Map", 2D) = "white" {}
        _SunPos ("Sun Position", Vector) = (0, 0, 0)
        _NormalSize ("Scale of normal map", float) = 0
        _NormalStrength ("Strength of normal map", float) = 0
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
            // Upgrade NOTE: excluded shader from DX11; has structs without semantics (struct v2f members worldpos)
            #pragma exclude_renderers d3d11
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
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float3 normal : TEXCOORD1;
                float4 worldpos : TEXCOORD2;
                float4 vertpos : TEXCOORD3;
            };
            
            sampler2D _NormalMap;

            float _SteepnessThreshold[256];
            float4 _Colors[256];
            float _LayerBleed[256];
            int _ColorsLen = 0;

            float4 _MainTex_ST;
            float3 _SunPos;

            float _BaseHeight;
            float3 _PlanetCenter;

            float _MixRadius = 2;
            float _MixNoiseAmount = 0.15;

            float _NormalSize;
            float _NormalStrength;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.normal = v.normal;
                o.worldpos = mul(unity_ObjectToWorld, v.vertex);
                o.vertpos = v.vertex;
                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }

            float4 Triplanar(float3 VertPos, float3 Normal, float Scale, float BlendSharpness, sampler2D Tex)
            {
                float2 TexCoordX = VertPos.zy * Scale;
                float2 TexCoordY = VertPos.xz * Scale;
                float2 TexCoordZ = VertPos.xy * Scale;

                float4 ColX = tex2D(Tex, TexCoordX);
                float4 ColY = tex2D(Tex, TexCoordY);
                float4 ColZ = tex2D(Tex, TexCoordZ);

                float3 blendWeight = saturate(pow(abs(Normal), BlendSharpness));

                // Make so x+y+z = 1
                blendWeight /= dot(blendWeight, 1);

                return ColX * blendWeight.x + ColY * blendWeight.y + ColZ * blendWeight.z;
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

            fixed4 frag(v2f i) : SV_Target
            {
                float3 MappedNormal = triplanarNormal(i.vertpos, i.normal, _NormalSize * 10 / _BaseHeight, 0, _NormalMap);
                MappedNormal = normalize(lerp(i.normal, MappedNormal, _NormalStrength));
                
                float3 lightDir = normalize(_SunPos - i.worldpos);
                float lightAmount = dot(lightDir, MappedNormal);
                fixed4 col;
                float MixNoiseVal = snoise(i.vertpos / 200) * 1 + snoise(i.vertpos / 25) / 4;
                MixNoiseVal /= (1 + 0.25);

                //float Dist = length(i.worldpos - _PlanetCenter) + MixNoiseVal * _MixNoiseAmount;

                float Dist = (dot(normalize(i.worldpos - _PlanetCenter), normalize(i.normal))) + MixNoiseVal; // * _MixNoiseAmount;

                float BleedNoiseVal = (snoise(i.vertpos / 20) + 1) / 2;

                for (int Layer = 0; Layer < _ColorsLen; Layer++)
                {
                    if (Dist > _SteepnessThreshold[Layer])
                    {
                        col = _Colors[Layer];

                        if (Dist - (_SteepnessThreshold[Layer]) < _MixRadius)
                        {
                            float mixer = (Dist - (_SteepnessThreshold[Layer])) / _MixRadius;
                            mixer = saturate(mixer + _LayerBleed[Layer + 1]);
                            col = lerp(_Colors[Layer + 1], col, mixer / 2 + 0.5);
                        }
                        if (Layer == 0)
                            break;

                        if ((_SteepnessThreshold[Layer - 1]) - Dist < _MixRadius)
                        {
                            float mixer = ((_SteepnessThreshold[Layer - 1]) - Dist) / _MixRadius;
                            mixer = saturate(mixer);
                            col = lerp(_Colors[Layer - 1], _Colors[Layer], mixer / 2 + 0.5);
                        }

                        if (_LayerBleed[Layer])
                        {
                            float MixerBetweenLayer = (Dist - _SteepnessThreshold[Layer]) / (_SteepnessThreshold[Layer -
                                1] - _SteepnessThreshold[Layer]);
                            MixerBetweenLayer *= MixerBetweenLayer * MixerBetweenLayer * MixerBetweenLayer *
                                MixerBetweenLayer;
                            col = lerp(col, _Colors[Layer - 1], BleedNoiseVal * _LayerBleed[Layer] * MixerBetweenLayer);
                        }

                        break;
                    }
                }

                col *= lightAmount;
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }


        Pass
        {
            Name "ShadowCaster"
            Tags
            {
                "LightMode" = "ShadowCaster"
            }

            Fog
            {
                Mode Off
            }
            ZWrite On ZTest LEqual Cull Off
            Offset 1, 1

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_shadowcaster
            #include "UnityCG.cginc"

            struct v2f
            {
                V2F_SHADOW_CASTER;
            };

            v2f vert(appdata_base v)
            {
                v2f o;
                TRANSFER_SHADOW_CASTER(o)
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG

        }
    }
}