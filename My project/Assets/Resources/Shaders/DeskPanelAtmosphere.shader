Shader "TwelveMoons/UI/DeskPanelAtmosphere"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _EffectColor ("Effect Color", Color) = (0,0,0,0.75)
        _InnerRadius ("Inner Radius", Range(0,1)) = 0.45
        _Softness ("Softness", Range(0.001,1)) = 0.35
        _BaseDimAlpha ("Base Dim Alpha", Range(0,0.8)) = 0
        _LightClearAmount ("Light Clear Amount", Range(0,1)) = 0.68
        _LightEdgeSoftness ("Light Edge Softness", Range(0.001,0.8)) = 0.46
        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
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

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _EffectColor;
            float _InnerRadius;
            float _Softness;
            float _BaseDimAlpha;
            float _LightClearAmount;
            float _LightEdgeSoftness;
            int _LightRectCount;
            float4 _LightRects[8];

            v2f vert(appdata_t input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.texcoord = input.texcoord;
                output.color = input.color * _Color;
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                float softness = max(0.001, _Softness);
                float distanceToNearestEdge = min(min(input.texcoord.x, 1.0 - input.texcoord.x), min(input.texcoord.y, 1.0 - input.texcoord.y)) * 2.0;
                float edgeAlpha = 1.0 - smoothstep(_InnerRadius, _InnerRadius + softness, distanceToNearestEdge);
                float alpha = saturate(_BaseDimAlpha + edgeAlpha * (1.0 - _BaseDimAlpha));
                float lightMask = 0.0;

                [unroll]
                for (int index = 0; index < 8; index++)
                {
                    if (index >= _LightRectCount)
                    {
                        break;
                    }

                    float4 lightRect = _LightRects[index];
                    float2 halfSize = max(lightRect.zw * 0.5, float2(0.001, 0.001));
                    float2 local = abs(input.texcoord - lightRect.xy) / halfSize;
                    float rectDistance = max(local.x, local.y);
                    float edgeSoftness = max(0.001, _LightEdgeSoftness);
                    float fadeStart = saturate(1.0 - edgeSoftness * 1.65);
                    float fadeEnd = 1.0 + edgeSoftness;
                    float currentMask = 1.0 - smoothstep(fadeStart, fadeEnd, rectDistance);
                    lightMask = max(lightMask, currentMask);
                }

                alpha *= 1.0 - saturate(lightMask * _LightClearAmount);
                fixed4 color = _EffectColor * input.color;
                color.a *= alpha;
                return color;
            }
            ENDCG
        }
    }
}
