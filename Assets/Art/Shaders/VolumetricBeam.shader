Shader "Custom/VolumetricBeam"
{
    Properties
    {
        _Color ("Beam Color", Color) = (0.96, 0.75, 0.5, 0.12)
        _GlowIntensity ("Glow Intensity", Range(0.1, 10)) = 3.0
        _FresnelPower ("Fresnel Edge Power", Range(0.5, 5)) = 2.0
        _NoiseScale ("Noise Scale (Vertical)", Float) = 8.0
        _ScrollSpeed ("Scroll Speed", Float) = 2.0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        LOD 100

        Cull Off
        ZWrite Off
        Blend SrcAlpha One // Additive blending for gorgeous glowing energy look

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : TEXCOORD1;
                float3 viewDir : TEXCOORD3;
            };

            float4 _Color;
            float _GlowIntensity;
            float _FresnelPower;
            float _NoiseScale;
            float _ScrollSpeed;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.viewDir = normalize(WorldSpaceViewDir(v.vertex));
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Height fade-out at the top and bottom to avoid hard geometry clipping
                float heightFade = smoothstep(0.0, 0.15, i.uv.y) * smoothstep(1.0, 0.75, i.uv.y);

                // Silhouette/Fresnel edge fade to make the cylinder look like a soft, volumetric column
                float3 normal = normalize(i.normal);
                float3 viewDir = normalize(i.viewDir);
                float edgeFade = saturate(dot(normal, viewDir));
                edgeFade = pow(edgeFade, _FresnelPower);

                // High-performance procedural scrolling energy noise (no texture read required!)
                float pulse1 = sin(i.uv.y * _NoiseScale - _Time.y * _ScrollSpeed) * 0.5 + 0.5;
                float pulse2 = cos(i.uv.x * 6.28 + _Time.y * _ScrollSpeed * 1.3) * 0.5 + 0.5;
                float energyNoise = lerp(0.5, 1.0, pulse1 * pulse2);

                // Combine calculations
                float finalAlpha = heightFade * edgeFade * energyNoise * _Color.a;

                // Color output with emission glow multiplier
                fixed3 rgb = _Color.rgb * _GlowIntensity * energyNoise;

                return fixed4(rgb, finalAlpha);
            }
            ENDCG
        }
    }
}
