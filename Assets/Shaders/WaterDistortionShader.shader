Shader "Custom/WaterDistortionShader" {
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _NormalMap ("Normal Map", 2D) = "bump" {}

        _Strength ("Distortion Strength", Range(0, 0.1)) = 0.05
        _Speed ("Flow Speed", Range(0, 5)) = 0.6
        _Tiling ("Noise Tiling", Range(1, 20)) = 5

        _WaveStrength ("Wave Strength", Range(0, 0.05)) = 0.02
        _WaveFreq ("Wave Frequency", Range(1, 20)) = 8

        _Alpha ("Transparency", Range(0,1)) = 0.4   // ⭐ 낮게!
        _Tint ("Water Tint", Color) = (0.2, 0.4, 0.6, 1)
        _TintStrength ("Tint Strength", Range(0,1)) = 0.2
    }

    SubShader
    {
        Tags 
        { 
            "RenderPipeline"="UniversalPipeline"
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

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
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);

            float _Strength;
            float _Speed;
            float _Tiling;
            float _WaveStrength;
            float _WaveFreq;

            float _Alpha;
            float4 _Tint;
            float _TintStrength;

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS);
                o.uv = v.uv;
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                float2 uv = i.uv;

                // 1️⃣ normal 흐름
                float2 nUV = uv * _Tiling + float2(_Time.y * _Speed, _Time.y * _Speed * 0.5);

                float3 normal = UnpackNormal(
                    SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, nUV)
                );

                float2 flow = normal.xy * _Strength;

                // wave
                float waveX = sin(uv.y * _WaveFreq + _Time.y * 2.0) * _WaveStrength;
                float waveY = cos(uv.x * _WaveFreq + _Time.y * 1.5) * _WaveStrength;

                float2 wave = float2(waveX, waveY);

                float2 finalUV = uv + flow + wave;

                // base texture sampling
                half4 baseCol = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, finalUV);

                // tint
                half3 finalColor = lerp(baseCol.rgb, _Tint.rgb, _TintStrength);

                // flow
                float distort = normal.x + normal.y;
                finalColor += distort * 0.1;

                // transparency
                return half4(finalColor, _Alpha);
            }

            ENDHLSL
        }
    }
}