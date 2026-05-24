Shader "TwelveMoons/CityHoverOutline"
{
    Properties
    {
        _OutlineColor ("轮廓颜色", Color) = (1, 0.62, 0.12, 1)
        _OutlinePixelWidth ("轮廓像素宽度", Float) = 3
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "City Hover Screen Space Outline"
            ZWrite Off
            ZTest Always
            Cull Off
            Blend One Zero

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            TEXTURE2D(_CityBuildingOutlineMaskTex);
            SAMPLER(sampler_CityBuildingOutlineMaskTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _OutlineColor;
                float _OutlinePixelWidth;
            CBUFFER_END

            half SampleMask(float2 uv)
            {
                return SAMPLE_TEXTURE2D(_CityBuildingOutlineMaskTex, sampler_CityBuildingOutlineMaskTex, uv).r;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half4 source = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.texcoord);
                half center = SampleMask(input.texcoord);

                float2 texel = 1.0 / _ScreenParams.xy;
                int width = clamp((int)round(_OutlinePixelWidth), 1, 6);
                half neighbor = 0;

                [loop]
                for (int x = -6; x <= 6; x++)
                {
                    [loop]
                    for (int y = -6; y <= 6; y++)
                    {
                        if (abs(x) <= width && abs(y) <= width)
                        {
                            float2 offset = float2(x, y) * texel;
                            neighbor = max(neighbor, SampleMask(input.texcoord + offset));
                        }
                    }
                }

                half outline = saturate(neighbor - center);
                return lerp(source, _OutlineColor, outline * _OutlineColor.a);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Unlit"
}
