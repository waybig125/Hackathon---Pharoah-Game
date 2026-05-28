Shader "Custom/SkyboxGradient"
{
    Properties
    {
        _ColorBottom ("Color Bottom (Peach)", Color) = (0.98, 0.62, 0.42, 1)
        _ColorMiddle1 ("Color Middle Low (Rose)", Color) = (0.85, 0.44, 0.60, 1)
        _ColorMiddle2 ("Color Middle High (Twilight)", Color) = (0.24, 0.44, 0.74, 1)
        _ColorTop ("Color Top (Deep Blue)", Color) = (0.06, 0.12, 0.35, 1)
        
        [NoScaleOffset] _CloudTex ("Cloud Normal & Density (RGBA)", 2D) = "white" {}
        _CloudSpeed ("Cloud Scroll Speed (XY)", Vector) = (0.05, 0.03, 0, 0)
        _CloudColor ("Cloud Tint Color", Color) = (0.95, 0.85, 0.75, 1)
        _CloudThreshold ("Cloud Threshold", Range(0, 1)) = 0.35
        _CloudThickness ("Cloud Thickness Multiplier", Range(0, 5)) = 2.5
        _CloudScale ("Cloud Scale", Float) = 0.8
        _SunDir ("Sun Light Direction", Vector) = (-0.5, 0.3, 0.8, 0)
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

            sampler2D _CloudTex;
            float4 _CloudSpeed;
            fixed4 _CloudColor;
            float _CloudThreshold;
            float _CloudThickness;
            float _CloudScale;
            float4 _SunDir;

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

                // Render scrolling normal-mapped clouds in the sky hemisphere
                if (d.y > 0.0)
                {
                    // Project skybox sphere onto a flat horizontal plane (bias prevents infinite stretching at zenith)
                    float2 skyUV = d.xz / (d.y + 0.22);

                    // Dual-scrolling layered texture sampling to completely eliminate repeating grid patterns
                    // Layer 1: Base scale, scrolls in primary direction
                    float2 uv1 = skyUV * _CloudScale + _Time.x * _CloudSpeed.xy;
                    float4 sample1 = tex2D(_CloudTex, uv1);

                    // Layer 2: Rotated and slightly scaled-down layer, scrolls in secondary direction
                    float2 speed2 = float2(-_CloudSpeed.y, _CloudSpeed.x) * 0.7; // Rotated and scaled speed vector
                    float2 uv2 = skyUV * (_CloudScale * 1.63) + _Time.x * speed2; // 1.63x scale breaks grid alignment
                    float4 sample2 = tex2D(_CloudTex, uv2);

                    // Combine densities: multiplying them breaks up the grid and creates dynamic organic shapes
                    float density = sample1.a * sample2.a * 1.5; // Multiply by 1.5 to boost contrast and brightness
                    density = saturate(density);

                    // Blend normals from both scrolling layers
                    float3 normal1;
                    normal1.xy = (sample1.rg * 2.0 - 1.0);
                    normal1.z = sqrt(1.0 - saturate(dot(normal1.xy, normal1.xy)));
                    
                    float3 normal2;
                    normal2.xy = (sample2.rg * 2.0 - 1.0);
                    normal2.z = sqrt(1.0 - saturate(dot(normal2.xy, normal2.xy)));
                    
                    float3 normal = normalize(normal1 + normal2);

                    // Apply threshold and smoothstep for puffy organic cloud shapes
                    float edgeWeight = saturate((density - _CloudThreshold) / max(0.01, 1.0 - _CloudThreshold));
                    float cloudAlpha = smoothstep(0.0, 1.0, edgeWeight);
                    cloudAlpha = saturate(cloudAlpha * _CloudThickness);
                    
                    // Fade clouds out near the horizon line to prevent harsh clipping
                    float horizonFade = saturate(d.y * 5.0);
                    cloudAlpha *= horizonFade;

                    if (cloudAlpha > 0.0)
                    {
                        // Calculate lighting from the sun direction
                        float3 sunDir = normalize(_SunDir.xyz);
                        float diffuse = saturate(dot(normal, sunDir));

                        // Base shaded cloud color
                        fixed3 shadowCol = col.rgb * 0.7; // Blend slightly with sky behind it
                        fixed3 litCol = _CloudColor.rgb * (diffuse * 0.8 + 0.4);

                        // Sunset golden rim highlight on edges facing the sun
                        float rim = saturate(dot(normal, sunDir));
                        fixed3 rimCol = fixed3(1.0, 0.65, 0.25); // Bright gold/orange
                        litCol = lerp(litCol, rimCol, pow(rim, 6.0) * 0.9);

                        // Final cloud mix
                        fixed3 finalCloudColor = lerp(shadowCol, litCol, diffuse);
                        col.rgb = lerp(col.rgb, finalCloudColor, cloudAlpha);
                    }
                }
                
                return col;
            }
            ENDCG
        }
    }
}
