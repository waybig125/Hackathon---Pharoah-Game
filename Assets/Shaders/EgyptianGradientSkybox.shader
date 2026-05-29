// Egyptian Gradient Skybox — URP compatible
// Draws a clean top-to-bottom gradient using world Y direction.
// Unlike NVJOB Dynamic Sky which uses Legacy CG shaders (pink in URP),
// this is a pure HLSL shader that works in both Built-in and URP.

Shader "Egyptian/GradientSkybox"
{
    Properties
    {
        _TopColor    ("Zenith Color",    Color) = (0.18, 0.28, 0.55, 1)
        _MidColor    ("Horizon Color",   Color) = (0.75, 0.42, 0.18, 1)
        _BottomColor ("Ground Color",    Color) = (0.55, 0.30, 0.10, 1)
        _HorizonLine ("Horizon Sharpness", Range(0.5, 8.0)) = 2.5
    }

    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0

            #include "UnityCG.cginc"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 worldDir   : TEXCOORD0;
            };

            float4 _TopColor;
            float4 _MidColor;
            float4 _BottomColor;
            float  _HorizonLine;

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = UnityObjectToClipPos(IN.positionOS);
                // World direction from object space
                OUT.worldDir = mul((float3x3)unity_ObjectToWorld, IN.positionOS.xyz);
                return OUT;
            }

            float4 Frag(Varyings IN) : SV_Target
            {
                // Normalise Y to -1..+1 range
                float3 dir = normalize(IN.worldDir);
                float t = dir.y; // -1 = directly below, +1 = directly above

                // Upper half: zenith → horizon
                float4 upper = lerp(_MidColor, _TopColor, pow(max(t, 0.0), 1.0 / _HorizonLine));
                // Lower half: ground → horizon
                float4 lower = lerp(_BottomColor, _MidColor, pow(max(-t, 0.0) * 0.5 + 0.5, _HorizonLine));

                // Blend at the equator
                float4 col = t >= 0.0 ? upper : lower;
                return col;
            }
            ENDHLSL
        }
    }
}
