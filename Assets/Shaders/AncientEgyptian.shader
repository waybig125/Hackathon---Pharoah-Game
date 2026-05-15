Shader "Custom/AncientEgyptian"
{
    Properties
    {
        _MainTex ("Base Texture", 2D) = "white" {}
        [Normal] _BumpMap ("Normal Map", 2D) = "bump" {}
        _Color ("Main Color", Color) = (1, 0.9, 0.7, 1)
        _Warmth ("Warmth Tint", Color) = (1, 0.8, 0.6, 1)
        _Contrast ("Shadow Contrast", Range(0, 2)) = 1.2
        _CrackScale ("Crack Scale", Float) = 20.0
        _CrackIntensity ("Crack Intensity", Range(0, 1)) = 0.5
        _SandAmount ("Sand Accumulation", Range(0, 1)) = 0.3
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD3;
                float4 tangentWS : TEXCOORD4;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_BumpMap);
            SAMPLER(sampler_BumpMap);

            CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            float4 _Color;
            float4 _Warmth;
            float _Contrast;
            float _CrackScale;
            float _CrackIntensity;
            float _SandAmount;
            CBUFFER_END

            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453123);
            }

            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float a = hash(i);
                float b = hash(i + float2(1.0, 0.0));
                float c = hash(i + float2(0.0, 1.0));
                float d = hash(i + float2(1.0, 1.0));
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(a, b, u.x) + (c - a) * u.y * (1.0 - u.x) + (d - b) * u.x * u.y;
            }

            float cracks(float2 uv)
            {
                float2 p = uv * _CrackScale;
                float v = noise(p * 2.0);
                v = abs(v - 0.5) * 2.0;
                return pow(saturate(1.0 - v), 10.0) * _CrackIntensity;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                
                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                output.normalWS = normInputs.normalWS;
                output.tangentWS = float4(normInputs.tangentWS, input.tangentOS.w);
                
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Basic Texture
                float4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * _Color;
                
                // Normal Mapping
                float4 normalSample = SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.uv);
                float3 normalTS = UnpackNormal(normalSample);
                
                float3 bitangent = cross(input.normalWS, input.tangentWS.xyz) * input.tangentWS.w;
                float3x3 tbn = float3x3(input.tangentWS.xyz, bitangent, input.normalWS);
                float3 normalWS = normalize(mul(normalTS, tbn));
                
                // Procedural Cracks
                float crack = cracks(input.uv);
                texColor.rgb *= (1.0 - crack);
                
                // Lighting
                Light light = GetMainLight();
                float3 lightDir = light.direction;
                float diffuse = saturate(dot(normalWS, lightDir));
                
                // Shadow Crushing
                diffuse = pow(diffuse, _Contrast);
                
                // Warm Tint
                float3 ambient = half3(0.2, 0.15, 0.1) * _Warmth.rgb;
                float3 finalColor = texColor.rgb * (diffuse * light.color + ambient);
                
                // Sand Accumulation
                float sand = saturate(dot(input.normalWS, float3(0, 1, 0)));
                sand = pow(sand, 4.0) * _SandAmount;
                finalColor = lerp(finalColor, float3(0.8, 0.7, 0.5) * _Warmth.rgb, sand);
                
                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
    Fallback "Universal Render Pipeline/Lit"
}
