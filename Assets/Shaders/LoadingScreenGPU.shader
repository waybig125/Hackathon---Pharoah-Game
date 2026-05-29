Shader "Custom/LoadingScreenGPU"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Speed ("Rotation Speed", Float) = 2.0
        _Frequency ("Ripple Frequency", Float) = 15.0
        _GoldColor ("Gold Color", Color) = (0.95, 0.8, 0.2, 1.0)
        _DarkColor ("Dark Color", Color) = (0.04, 0.04, 0.04, 1.0)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Speed;
            float _Frequency;
            float4 _GoldColor;
            float4 _DarkColor;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Center-relative coordinates
                float2 uv = i.uv - 0.5;
                
                // Polar coordinates
                float r = length(uv);
                float theta = atan2(uv.y, uv.x);
                
                // Procedural alchemical ring
                float ring = smoothstep(0.45, 0.43, r) * smoothstep(0.35, 0.37, r);
                
                // Procedural inner glowing core
                float core = smoothstep(0.15, 0.0, r) * 0.7;
                
                // Dynamic golden liquid ripple effect based on Time
                float wave = sin(r * _Frequency - _Time.y * _Speed) * 0.5 + 0.5;
                
                // Rotate theta over time
                float rotation = theta + _Time.y * (_Speed * 0.5);
                float spokes = step(0.9, sin(rotation * 6.0)); // 6 magical runes/spokes
                
                // Combine ring, spokes, and ripples
                float mask = ring * (0.6 + 0.4 * wave) + spokes * ring * 0.5 + core * (0.8 + 0.2 * sin(_Time.y * 3.0));
                
                // Color interpolation
                fixed4 color = lerp(_DarkColor, _GoldColor, mask);
                
                // Add soft outer glow
                float glow = exp(-r * 6.0) * 0.8;
                color += _GoldColor * glow * (0.7 + 0.3 * sin(_Time.y * 5.0));
                
                // Transparency mask: make background transparent
                color.a = clamp(mask * 1.5 + glow, 0.0, 1.0);
                
                return color;
            }
            ENDCG
        }
    }
}
