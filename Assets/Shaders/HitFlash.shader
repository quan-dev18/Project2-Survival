Shader "Custom/HitFlash"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint Color", Color) = (1,1,1,1)

        // Màu flash - có thể chỉnh trong Inspector hoặc qua script
        _FlashColor ("Flash Color", Color) = (1,1,1,1)
        // Độ mạnh của flash: 0 = không có flash (hiện sprite gốc), 1 = full màu flash
        _FlashAmount ("Flash Amount", Range(0,1)) = 0

        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
            "RenderPipeline" = "UniversalPipeline"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                half4 color     : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_AlphaTex);
            SAMPLER(sampler_AlphaTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
                half4 _RendererColor;
                float2 _Flip;
                half4 _FlashColor;
                float _FlashAmount;
                float _EnableExternalAlpha;
            CBUFFER_END

            inline float4 UnityFlipSprite(in float3 pos, in float2 flip)
            {
                return float4(pos.xy * flip, pos.z, 1.0);
            }

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.vertex = UnityFlipSprite(IN.vertex.xyz, _Flip);
                OUT.vertex = TransformObjectToHClip(OUT.vertex.xyz);
                OUT.texcoord = TRANSFORM_TEX(IN.texcoord, _MainTex);
                OUT.color = IN.color * _Color * _RendererColor;

                #ifdef PIXELSNAP_ON
                float2 pixelSize = 1.0 / _ScreenParams.xy;
                OUT.vertex.xy = round(OUT.vertex.xy / pixelSize) * pixelSize;
                #endif

                return OUT;
            }

            half4 SampleSpriteTexture(float2 uv)
            {
                half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);

                #if ETC1_EXTERNAL_ALPHA
                half4 alpha = SAMPLE_TEXTURE2D(_AlphaTex, sampler_AlphaTex, uv);
                color.a = lerp(color.a, alpha.r, _EnableExternalAlpha);
                #endif

                return color;
            }

            half4 frag(v2f IN) : SV_Target
            {
                half4 c = SampleSpriteTexture(IN.texcoord) * IN.color;

                // Giữ nguyên alpha gốc, chỉ pha màu RGB sang màu flash theo _FlashAmount
                c.rgb = lerp(c.rgb, _FlashColor.rgb, _FlashAmount * _FlashColor.a);

                c.rgb *= c.a; // premultiply cho Blend One OneMinusSrcAlpha
                return c;
            }
            ENDHLSL
        }
    }
}
