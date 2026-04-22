Shader "Custom/Dither"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (1,1,1,1)
        _Smoothness("Smoothness", Range(0,1)) = 0.5
        _Metallic("Metallic", Range(0,1)) = 0.0
        _DitherFade("Dither Fade", Range(0,1)) = 0.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "AlphaTest"
        }
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float4 screenPos : TEXCOORD2;
                float fogFactor : TEXCOORD3;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _Smoothness;
                float _Metallic;
                float _DitherFade;
            CBUFFER_END

            // Simple dither pattern - WebGL compatible
            float DitherPattern(float2 screenPos)
            {
                // Use a simple ordered dithering approach
                float2 uv = screenPos * 0.25; // Scale down
                float2 c = floor(uv);

                // Simple 4x4 Bayer-like pattern using modulo arithmetic
                float x = c.x - floor(c.x * 0.25) * 4.0;
                float y = c.y - floor(c.y * 0.25) * 4.0;

                // Compute pattern value using a simple formula
                float pattern = x * 4.0 + y;
                pattern = frac(pattern * 0.0625 + x * 0.25 + y * 0.5);

                return pattern;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;

                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.screenPos = positionInputs.positionNDC;

                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);
                output.normalWS = normalInputs.normalWS;

                output.fogFactor = ComputeFogFactor(output.positionCS.z);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Dithering alpha clip - SV_POSITION gives screen pixel coordinates in fragment shader
                float2 screenPos = input.positionCS.xy;
                float ditherThreshold = DitherPattern(screenPos);

                // Clip pixels based on dither pattern and fade value
                clip(1.0 - _DitherFade - ditherThreshold);

                // Get main light
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));

                // Calculate simple diffuse lighting
                float3 normalWS = normalize(input.normalWS);
                float ndotl = saturate(dot(normalWS, mainLight.direction));

                // Ambient lighting with minimum brightness
                float3 ambient = max(SampleSH(normalWS), 0.3); // Ensure minimum 30% ambient

                // Combine lighting
                float3 lighting = ambient + mainLight.color * ndotl * mainLight.distanceAttenuation * mainLight.shadowAttenuation;

                // Apply to base color
                float3 color = _BaseColor.rgb * lighting;

                // Add specular if needed (simple Blinn-Phong)
                if (_Smoothness > 0.01)
                {
                    float3 viewDir = normalize(GetWorldSpaceNormalizeViewDir(input.positionWS));
                    float3 halfDir = normalize(mainLight.direction + viewDir);
                    float spec = pow(saturate(dot(normalWS, halfDir)), _Smoothness * 100.0);
                    color += mainLight.color * spec * (1.0 - _Metallic) * mainLight.distanceAttenuation;
                }

                // Apply fog
                color = MixFog(color, input.fogFactor);

                return half4(color, 1.0);
            }
            ENDHLSL
        }

        // Shadow caster pass
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ColorMask 0

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 screenPos : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float _DitherFade;
            CBUFFER_END

            // Simple dither pattern - WebGL compatible
            float DitherPattern(float2 screenPos)
            {
                // Use a simple ordered dithering approach
                float2 uv = screenPos * 0.25; // Scale down
                float2 c = floor(uv);

                // Simple 4x4 Bayer-like pattern using modulo arithmetic
                float x = c.x - floor(c.x * 0.25) * 4.0;
                float y = c.y - floor(c.y * 0.25) * 4.0;

                // Compute pattern value using a simple formula
                float pattern = x * 4.0 + y;
                pattern = frac(pattern * 0.0625 + x * 0.25 + y * 0.5);

                return pattern;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.screenPos = output.positionCS;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // SV_POSITION gives screen pixel coordinates in fragment shader
                float2 screenPos = input.positionCS.xy;
                float ditherThreshold = DitherPattern(screenPos);
                clip(1.0 - _DitherFade - ditherThreshold);
                return 0;
            }
            ENDHLSL
        }

        // Depth only pass
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ColorMask 0

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 screenPos : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float _DitherFade;
            CBUFFER_END

            // Simple dither pattern - WebGL compatible
            float DitherPattern(float2 screenPos)
            {
                // Use a simple ordered dithering approach
                float2 uv = screenPos * 0.25; // Scale down
                float2 c = floor(uv);

                // Simple 4x4 Bayer-like pattern using modulo arithmetic
                float x = c.x - floor(c.x * 0.25) * 4.0;
                float y = c.y - floor(c.y * 0.25) * 4.0;

                // Compute pattern value using a simple formula
                float pattern = x * 4.0 + y;
                pattern = frac(pattern * 0.0625 + x * 0.25 + y * 0.5);

                return pattern;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.screenPos = output.positionCS;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // SV_POSITION gives screen pixel coordinates in fragment shader
                float2 screenPos = input.positionCS.xy;
                float ditherThreshold = DitherPattern(screenPos);
                clip(1.0 - _DitherFade - ditherThreshold);
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack Off
}
