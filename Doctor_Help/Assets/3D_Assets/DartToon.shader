// Flat two-tone shading for the 3D dart; unlit so it renders the same with the 2D Renderer.
Shader "DoctorHelp/DartToon"
{
    Properties
    {
        _BaseColor ("Color", Color) = (1, 1, 1, 1)
        _ShadeColor ("Shade", Color) = (0.55, 0.45, 0.5, 1)
        _LightDirection ("Light Direction (world)", Vector) = (0.45, -0.7, 0.55, 0)
        _RimColor ("Rim (alpha = strength)", Color) = (1, 0.95, 0.85, 0.35)
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Pass
        {
            Cull Back
            ZWrite On
            ZTest LEqual
            Blend Off

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _ShadeColor;
                float4 _LightDirection;
                half4 _RimColor;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 viewDirectionWS : TEXCOORD1;
            };

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                VertexPositionInputs position = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = position.positionCS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.viewDirectionWS = GetWorldSpaceNormalizeViewDir(position.positionWS);
                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                float3 normal = normalize(input.normalWS);
                float light = dot(normal, -normalize(_LightDirection.xyz)) * 0.5 + 0.5;
                half3 color = lerp(_BaseColor.rgb * _ShadeColor.rgb, _BaseColor.rgb, smoothstep(0.42, 0.58, light));
                half rim = pow(1.0 - saturate(dot(normal, normalize(input.viewDirectionWS))), 3.0) * _RimColor.a;
                return half4(color + _RimColor.rgb * rim, 1.0);
            }
            ENDHLSL
        }
    }
}
