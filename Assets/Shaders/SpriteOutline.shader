Shader "Custom/SpriteOutline2D_Simple"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        [HDR]_Color ("Tint", Color) = (1,1,1,1)

        [Header(Outline)]
        [HDR]_OutlineColor ("Outline Color", Color) = (1,1,1,1)
        _OutlineWidth ("Outline Width", Range(0, 10)) = 1

        [Header(Rainbow Mode)]
        [Toggle] _UseRainbow ("Enable Rainbow", Float) = 0
        _RainbowSpeed ("Rainbow Speed", Range(0, 10)) = 2
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
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 _Color;
            fixed4 _OutlineColor;
            float _OutlineWidth;
            float _UseRainbow;
            float _RainbowSpeed;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color;
                return OUT;
            }

            // Hàm chuyển đổi Hue sang RGB 7 màu
            fixed3 HueToRGB(float h)
            {
                float3 rgb = saturate(abs(fmod(h * 6.0 + float3(0.0, 4.0, 2.0), 6.0) - 3.0) - 1.0);
                return rgb;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, IN.texcoord) * IN.color;

                // Nếu pixel hiện tại trong suốt, kiểm tra viền xung quanh (8 hướng)
                if (col.a <= 0.1 && _OutlineWidth > 0)
                {
                    float2 offset = _MainTex_TexelSize.xy * _OutlineWidth;

                    float outlineAlpha = 0;
                    outlineAlpha = max(outlineAlpha, tex2D(_MainTex, IN.texcoord + float2( offset.x, 0)).a);
                    outlineAlpha = max(outlineAlpha, tex2D(_MainTex, IN.texcoord + float2(-offset.x, 0)).a);
                    outlineAlpha = max(outlineAlpha, tex2D(_MainTex, IN.texcoord + float2(0,  offset.y)).a);
                    outlineAlpha = max(outlineAlpha, tex2D(_MainTex, IN.texcoord + float2(0, -offset.y)).a);
                    outlineAlpha = max(outlineAlpha, tex2D(_MainTex, IN.texcoord + float2( offset.x,  offset.y)).a);
                    outlineAlpha = max(outlineAlpha, tex2D(_MainTex, IN.texcoord + float2(-offset.x,  offset.y)).a);
                    outlineAlpha = max(outlineAlpha, tex2D(_MainTex, IN.texcoord + float2( offset.x, -offset.y)).a);
                    outlineAlpha = max(outlineAlpha, tex2D(_MainTex, IN.texcoord + float2(-offset.x, -offset.y)).a);

                    if (outlineAlpha > 0.1)
                    {
                        fixed4 outColor = _OutlineColor;

                        // Chế độ xoay vòng 7 màu
                        if (_UseRainbow > 0.5)
                        {
                            float hue = frac(_Time.y * _RainbowSpeed * 0.2);
                            outColor.rgb = HueToRGB(hue);
                        }

                        outColor.a *= outlineAlpha;
                        return outColor;
                    }
                }

                return col;
            }
            ENDCG
        }
    }
}