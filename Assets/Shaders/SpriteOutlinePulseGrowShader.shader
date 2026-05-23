Shader "Custom/SpriteOutlinePulseGlowShader"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _OutlineColor ("Outline Color", Color) = (1,1,1,1)
        _OutlineWidth ("Outline Width", Range(0.0005, 0.02)) = 0.005
        _OutlineStrength ("Outline Strength", Range(0,1)) = 0

        _GlowColor ("Glow Color", Color) = (1,1,1,1)
        _GlowStrength ("Glow Strength", Range(0,5)) = 0

        _PulseSpeed ("Pulse Speed", Range(0,20)) = 0
        _PulseAmount ("Pulse Amount", Range(0,1)) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "RenderPipeline"="UniversalPipeline"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float4 _MainTex_ST;   // ⭐ 추가 (핵심)
            float4 _Color;

            float4 _OutlineColor;
            float _OutlineWidth;
            float _OutlineStrength;

            float4 _GlowColor;
            float _GlowStrength;

            float _PulseSpeed;
            float _PulseAmount;

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);

                // ⭐ UV transform 반드시 적용
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                // ⭐ vertex color 반드시 전달
                o.color = v.color * _Color;

                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                float2 uv = i.uv;

                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv) * i.color;
                float centerAlpha = col.a;

                float outline = 0;
                outline += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(_OutlineWidth, 0)).a;
                outline += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - float2(_OutlineWidth, 0)).a;
                outline += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(0, _OutlineWidth)).a;
                outline += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - float2(0, _OutlineWidth)).a;

                float outlineMask = step(0.01, outline) * (1 - centerAlpha);

                float pulse = 1.0;
                if (_PulseSpeed > 0 && _PulseAmount > 0)
                {
                    float s = sin(_Time.y * _PulseSpeed) * 0.5 + 0.5;
                    pulse = lerp(1.0, s, _PulseAmount);
                }

                float outlineFactor = outlineMask * _OutlineStrength * pulse;

                float3 color = lerp(col.rgb, _OutlineColor.rgb, outlineFactor);
                color += _GlowColor.rgb * (_GlowStrength * pulse);

                float alpha = max(col.a, outlineFactor);

                return float4(color, alpha);
            }
            ENDHLSL
        }
    }
}