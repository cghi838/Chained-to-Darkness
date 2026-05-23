Shader "Custom/GlitchShader" {
    Properties {
        _DispTex ("Displacement Map", 2D) = "white" {}
        _Intensity ("Glitch Intensity", Range(0.0, 1.0)) = 1.0
        _ColorIntensity ("Color Intensity", Range(0.0, 1.0)) = 0.2
    }

    SubShader {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100
        ZTest Always Cull Off ZWrite Off

        Pass {
            Name "Glitch"
            HLSLPROGRAM
            #pragma vertex Vert // use default Blit.hlsl internal Vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            TEXTURE2D(_DispTex);
            SAMPLER(sampler_DispTex);

            float _Intensity, _ColorIntensity;
            float4 direction;
            float filterRadius, displace, scale;

            half4 frag(Varyings input) : SV_Target {
                float2 uv = input.texcoord;

                // Displacement
                half4 normal = SAMPLE_TEXTURE2D(_DispTex, sampler_DispTex, uv * scale);
                uv.xy += (normal.xy - 0.5) * displace * _Intensity;

                // Screen
                half4 color      = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv);

                // Chromatic Aberration
                float2 offset    = direction.xy * 0.01 * filterRadius * _ColorIntensity;
                half4 redcolor   = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv + offset);
                half4 greencolor = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv - offset);

                if (filterRadius < -0.001)
                    color = half4(redcolor.r, redcolor.b, redcolor.g, color.a) * 0.5 + color * 0.5;
                else if (filterRadius > 0.001)
                    color = half4(greencolor.g, greencolor.b, greencolor.r, color.a) * 0.5 + color * 0.5;

                return color;
            }
            ENDHLSL
        }
    }
    Fallback Off
}