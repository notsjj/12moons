Shader "TwelveMoons/UI/DeskNoisePulse"
{
    Properties
    {
        _MainTex ("UI Texture", 2D) = "white" {}
        _NoiseColor ("Noise Color", Color) = (0.45, 0.45, 0.45, 0.08)
        _BackgroundColor ("Background Color", Color) = (0, 0, 0, 0.025)
        _PixelSize ("Grain Pixel Size", Range(1, 4)) = 1
        _Speed ("Noise Speed", Float) = 3
        _Contrast ("Noise Contrast", Range(0.1, 6)) = 0.7
        _Alpha ("Overall Alpha", Range(0, 1)) = 0.28

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
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

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _NoiseColor;
            fixed4 _BackgroundColor;
            float _PixelSize;
            float _Speed;
            float _Contrast;
            float _Alpha;

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            v2f vert(appdata input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = TRANSFORM_TEX(input.texcoord, _MainTex);
                output.color = input.color;
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                float noiseTime = _Time.y * max(0.01, _Speed);
                float timeSlice = floor(noiseTime);
                float timeBlend = smoothstep(0.0, 1.0, frac(noiseTime));
                float2 cell = floor(input.vertex.xy / max(1.0, _PixelSize));
                float noiseA = Hash21(cell + timeSlice);
                float noiseB = Hash21(cell + timeSlice + 17.0);
                float noise = lerp(noiseA, noiseB, timeBlend);
                noise = saturate((noise - 0.5) * _Contrast + 0.5);
                fixed4 color = lerp(_BackgroundColor, _NoiseColor, noise);
                color.a *= _Alpha * input.color.a;
                return color;
            }
            ENDCG
        }
    }
}
