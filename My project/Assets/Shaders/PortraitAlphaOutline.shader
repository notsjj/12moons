Shader "TwelveMoons/UI/PortraitAlphaOutline"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("角色颜色", Color) = (1, 1, 1, 1)
        _OutlineColor ("细描边颜色", Color) = (1, 1, 1, 0)
        _GlowColor ("外侧光晕颜色", Color) = (1, 1, 1, 0.46)
        _OutlinePixelWidth ("细描边像素宽度", Float) = 0
        _GlowPixelWidth ("外侧光晕像素宽度", Float) = 30
        _GlowIntensity ("光晕强度", Range(0, 1)) = 0.72
        _GlowFalloffPower ("光晕边缘衰减", Range(1, 4)) = 2.4
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
            fixed4 _GlowColor;
            float _OutlinePixelWidth;
            float _GlowPixelWidth;
            float _GlowIntensity;
            float _GlowFalloffPower;

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
                fixed centerAlpha = spriteColor.a;
                float outlineWidth = clamp(round(_OutlinePixelWidth), 0.0, 12.0);
                float glowWidth = clamp(round(_GlowPixelWidth), 1.0, 32.0);
                float glowFalloffPower = clamp(_GlowFalloffPower, 1.0, 4.0);
                fixed outlineNearbyAlpha = 0;
                fixed glowNearbyAlpha = 0;

                [loop]
                for (int x = -32; x <= 32; x++)
                {
                    [loop]
                    for (int y = -32; y <= 32; y++)
                    {
                        float2 offsetPixels = float2(x, y);
                        float distanceToCenter = length(offsetPixels);
                        if (distanceToCenter <= glowWidth)
                        {
                            float2 offset = offsetPixels * _MainTex_TexelSize.xy;
                            fixed sampleAlpha = SampleAlpha(input.texcoord + offset);
                            fixed radialFade = saturate(1.0 - distanceToCenter / max(1.0, glowWidth));
                            fixed glowWeight = pow(radialFade, glowFalloffPower);
                            glowNearbyAlpha = max(glowNearbyAlpha, sampleAlpha * glowWeight);
                            if (outlineWidth > 0.0 && distanceToCenter <= outlineWidth)
                            {
                                outlineNearbyAlpha = max(outlineNearbyAlpha, sampleAlpha);
                            }
                        }
                    }
                }

                fixed outsideMask = saturate(1.0 - centerAlpha);
                fixed outlineMask = saturate(outlineNearbyAlpha - centerAlpha) * outsideMask;
                fixed glowMask = saturate(glowNearbyAlpha - centerAlpha) * outsideMask * _GlowIntensity;

                fixed glowAlpha = _GlowColor.a * glowMask;
                fixed outlineAlpha = _OutlineColor.a * outlineMask;
                fixed outsideAlpha = saturate(glowAlpha + outlineAlpha);
                fixed3 outsideColor = _GlowColor.rgb;

                fixed4 outputColor;
                outputColor.rgb = lerp(outsideColor, spriteColor.rgb, centerAlpha);
                outputColor.a = saturate(spriteColor.a + outsideAlpha * outsideMask);
                return outputColor;
            }
            ENDCG
        }
    }

    FallBack "UI/Default"
}
