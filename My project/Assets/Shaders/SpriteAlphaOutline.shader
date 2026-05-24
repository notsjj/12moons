Shader "TwelveMoons/SpriteAlphaOutline"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Sprite Tint", Color) = (1, 1, 1, 1)
        _OutlineColor ("轮廓颜色", Color) = (1, 0.62, 0.12, 1)
        _OutlinePixelWidth ("轮廓像素宽度", Float) = 3
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
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
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 _Color;
            fixed4 _OutlineColor;
            float _OutlinePixelWidth;

            v2f vert(appdata input)
            {
                v2f output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.texcoord = input.texcoord;
                output.color = input.color * _Color;
                return output;
            }

            fixed SampleAlpha(float2 uv)
            {
                return tex2D(_MainTex, uv).a;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                fixed4 spriteColor = tex2D(_MainTex, input.texcoord) * input.color;
                float width = clamp(round(_OutlinePixelWidth), 1.0, 6.0);
                fixed nearbyAlpha = 0;

                [loop]
                for (int x = -6; x <= 6; x++)
                {
                    [loop]
                    for (int y = -6; y <= 6; y++)
                    {
                        if (abs(x) <= width && abs(y) <= width)
                        {
                            float2 offset = float2(x, y) * _MainTex_TexelSize.xy;
                            nearbyAlpha = max(nearbyAlpha, SampleAlpha(input.texcoord + offset));
                        }
                    }
                }

                fixed outlineMask = saturate(nearbyAlpha - spriteColor.a);
                fixed4 outlineColor = _OutlineColor;
                outlineColor.a *= outlineMask;
                return lerp(outlineColor, spriteColor, spriteColor.a);
            }
            ENDCG
        }
    }

    FallBack "Sprites/Default"
}
