Shader "Custom/SkyboxGradient"
{
    Properties
    {
        _ColorBottom ("Color Bottom (Peach)", Color) = (0.98, 0.62, 0.42, 1)
        _ColorMiddle1 ("Color Middle Low (Rose)", Color) = (0.85, 0.44, 0.60, 1)
        _ColorMiddle2 ("Color Middle High (Twilight)", Color) = (0.24, 0.44, 0.74, 1)
        _ColorTop ("Color Top (Deep Blue)", Color) = (0.06, 0.12, 0.35, 1)
    }

    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 viewDir : TEXCOORD0;
            };

            fixed4 _ColorBottom;
            fixed4 _ColorMiddle1;
            fixed4 _ColorMiddle2;
            fixed4 _ColorTop;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.viewDir = v.vertex.xyz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Normalize view direction
                float3 d = normalize(i.viewDir);
                
                // Map y from [-1, 1] to [0, 1]
                float t = d.y * 0.5 + 0.5;
                
                fixed4 col;
                if (t < 0.25)
                {
                    col = lerp(_ColorBottom, _ColorMiddle1, t / 0.25);
                }
                else if (t < 0.65)
                {
                    col = lerp(_ColorMiddle1, _ColorMiddle2, (t - 0.25) / 0.40);
                }
                else
                {
                    col = lerp(_ColorMiddle2, _ColorTop, (t - 0.65) / 0.35);
                }
                
                return col;
            }
            ENDCG
        }
    }
}
