Shader "Custom/FogOfWar"
{
    Properties
    {
        [HDR] _FogColor ("Unexplored Fog Color", Color) = (0.02, 0.03, 0.06, 1)
        [HDR] _ExploredColor ("Explored Fog Color", Color) = (0.08, 0.10, 0.16, 1)
        _FogDensity ("Unexplored Fog Density", Range(0, 1)) = 0.98
        _ExploredDensity ("Explored Fog Density", Range(0, 1)) = 0.55

        _InnerRadius ("Vision Inner Radius", Float) = 4
        _OuterRadius ("Vision Outer Radius", Float) = 7
        _EdgeSoftness ("Edge Softness", Range(0.01, 5)) = 1.5

        _NoiseScale ("Noise Scale", Float) = 0.5
        _NoiseStrength ("Edge Distortion Strength", Range(0, 3)) = 1.2
        _NoiseSpeed ("Noise Drift Speed (xy)", Vector) = (0.05, 0.03, 0, 0)
        _WispyAmount ("Wispy Alpha Variation", Range(0, 1)) = 0.15

        _PlayerPos ("Player Position", Vector) = (0, 0, 0, 0)

        _FlashPos ("Muzzle Flash Position", Vector) = (0, 0, 0, 0)
        _FlashDir ("Muzzle Flash Direction", Vector) = (1, 0, 0, 0)
        _FlashRadius ("Muzzle Flash Length", Float) = 3
        _FlashAngle ("Muzzle Flash Half Angle (deg)", Range(1, 90)) = 25
        _FlashStrength ("Muzzle Flash Strength", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _FogColor;
                float4 _ExploredColor;
                float _FogDensity;
                float _ExploredDensity;
                float _InnerRadius;
                float _OuterRadius;
                float _EdgeSoftness;
                float _NoiseScale;
                float _NoiseStrength;
                float4 _NoiseSpeed;
                float _WispyAmount;
                float4 _PlayerPos;
                float4 _FlashPos;
                float4 _FlashDir;
                float _FlashRadius;
                float _FlashAngle;
                float _FlashStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                return output;
            }

            // ---- Simplex-ish noise (Inigo Quilez) ----
            float2 hash2(float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)));
                return -1.0 + 2.0 * frac(sin(p) * 43758.5453123);
            }

            float noise(float2 p)
            {
                const float K1 = 0.366025404;
                const float K2 = 0.211324865;
                float2 i = floor(p + (p.x + p.y) * K1);
                float2 a = p - i + (i.x + i.y) * K2;
                float2 o = (a.x > a.y) ? float2(1.0, 0.0) : float2(0.0, 1.0);
                float2 b = a - o + K2;
                float2 c = a - 1.0 + 2.0 * K2;
                float3 h = max(0.5 - float3(dot(a, a), dot(b, b), dot(c, c)), 0.0);
                float3 n = h * h * h * h * float3(dot(a, hash2(i)), dot(b, hash2(i + o)), dot(c, hash2(i + 1.0)));
                return dot(n, float3(70.0, 70.0, 70.0));
            }

            float fbm(float2 p)
            {
                float f = 0.0;
                float amp = 0.5;
                [unroll]
                for (int k = 0; k < 3; k++)
                {
                    f += amp * noise(p);
                    p *= 2.02;
                    amp *= 0.5;
                }
                return f;
            }
            // -------------------------------------------

            half4 frag(Varyings input) : SV_Target
            {
                float2 worldPos = input.positionWS.xy;
                float dist = distance(worldPos, _PlayerPos.xy);

                // Noise trôi theo thời gian để sương "sống", không đứng yên
                float2 noiseUV = worldPos * _NoiseScale + _Time.y * _NoiseSpeed.xy;
                float edgeNoise = fbm(noiseUV);

                // Làm méo bán kính -> viền lởm chởm hữu cơ thay vì hình tròn hoàn hảo
                float distortedDist = dist + edgeNoise * _NoiseStrength;

                float vision = 1.0 - smoothstep(_InnerRadius, _OuterRadius, distortedDist);
                vision = saturate(vision);

                // Vùng "đã khám phá" giả lập: dải mờ hơn ngay ngoài outer radius
                float exploredRing = 1.0 - smoothstep(_OuterRadius, _OuterRadius + _EdgeSoftness * 4.0, distortedDist);

                float3 fogColor = lerp(_FogColor.rgb, _ExploredColor.rgb, exploredRing);
                float fogAlphaBase = lerp(_FogDensity, _ExploredDensity, exploredRing);

                // Gợn sóng nhẹ trên alpha để trông như mây/khói trôi
                float wispy = fbm(noiseUV * 1.7 - _Time.y * _NoiseSpeed.xy * 0.5);
                wispy = 1.0 - _WispyAmount * 0.5 + wispy * _WispyAmount;

                float fog = (1.0 - vision) * fogAlphaBase * saturate(wispy);

                // Ánh sáng từ nòng súng xua sương thành hình nón theo hướng bắn
                float2 toPoint = worldPos - _FlashPos.xy;
                float flashDist = length(toPoint);
                float2 flashDir = normalize(_FlashDir.xy);
                float dirAlign = dot(toPoint / max(flashDist, 1e-5), flashDir);
                float cone = smoothstep(cos(radians(_FlashAngle)), 1.0, dirAlign);
                float range = 1.0 - smoothstep(_FlashRadius * 0.25, _FlashRadius, flashDist);
                float flash = cone * range * _FlashStrength;
                fog *= 1.0 - flash;

                return half4(fogColor, saturate(fog));
            }
            ENDHLSL
        }
    }
    Fallback Off
}