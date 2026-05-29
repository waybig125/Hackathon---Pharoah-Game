// Egyptian Gradient Skybox — URP compatible
// Draws a clean top-to-bottom gradient using world Y direction.
// Optimized for Unity 6 / URP.

Shader "Egyptian/GradientSkybox"
{
    Properties
    {
        _TopColor    ("Zenith Color",    Color) = (0.12, 0.18, 0.38, 1)
        _MidColor    ("Horizon Color",   Color) = (0.82, 0.45, 0.12, 1)
        _BottomColor ("Ground Color",    Color) = (0.42, 0.22, 0.08, 1)
        _HorizonLine ("Horizon Sharpness", Range(0.1, 10.0)) = 2.0
    }

    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 worldPos     : TEXCOORD0;
            };

            half4 _TopColor;
            half4 _MidColor;
            half4 _BottomColor;
            float _HorizonLine;

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.worldPos = vertexInput.positionWS;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 viewDir = normalize(input.worldPos - _WorldSpaceCameraPos);
                float t = viewDir.y;

                half4 upper = lerp(_MidColor, _TopColor, pow(max(t, 0.0), 1.0 / _HorizonLine));
                half4 lower = lerp(_BottomColor, _MidColor, pow(max(-t, 0.0), _HorizonLine));

                half4 col = t >= 0.0 ? upper : lower;
                return col;
            }
            ENDHLSL
        }
    }
}

