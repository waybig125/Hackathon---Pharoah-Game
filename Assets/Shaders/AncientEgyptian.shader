Shader "Custom/AncientEgyptian"
{
    Properties
    {
        _MainTex ("Base Texture", 2D) = "white" {}
        [Normal] _BumpMap ("Normal Map", 2D) = "bump" {}
        _Color ("Main Color", Color) = (1, 1, 1, 1)
        _Warmth ("Warmth Tint", Color) = (1, 0.9, 0.8, 1)
        _Contrast ("Shadow Contrast", Range(0.1, 3)) = 1.2
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
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_BumpMap);
            SAMPLER(sampler_BumpMap);

            // Removing CBUFFER for maximum compatibility with all URP versions
            float4 _MainTex_ST;
            float4 _Color;
            float4 _Warmth;
            float _Contrast;
            float _CrackScale;
            float _CrackIntensity;
            float _SandAmount;

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * _Color;
                float3 normalWS = normalize(input.normalWS);
                
                // Simple Lighting
                Light light = GetMainLight();
                float diffuse = saturate(dot(normalWS, light.direction));
                diffuse = pow(diffuse, _Contrast);
                
                float3 ambient = half3(0.2, 0.18, 0.15) * _Warmth.rgb;
                float3 finalColor = texColor.rgb * (diffuse * light.color + ambient) * _Warmth.rgb;
                
                // Sand
                float sandMask = saturate(dot(normalWS, float3(0, 1, 0)));
                sandMask = pow(sandMask, 8.0) * _SandAmount;
                finalColor = lerp(finalColor, float3(0.8, 0.7, 0.5) * _Warmth.rgb, sandMask);
                
                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
    Fallback "Universal Render Pipeline/Lit"
}
