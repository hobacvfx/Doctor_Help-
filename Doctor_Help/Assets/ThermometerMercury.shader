Shader "DoctorHelp/Thermometer Mercury"
{
    Properties
    {
        _MainTex("Sprite Texture",2D)="white"{}
        [HideInInspector] _Color("Tint",Color)=(1,1,1,1)
        _MercuryTop("Top in Sprite Local Space",Float)=10000
    }
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline"}
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
            #pragma vertex MercuryVertex
            #pragma fragment MercuryFragment
            #pragma multi_compile_instancing
            #pragma multi_compile _ DEBUG_DISPLAY SKINNED_SPRITE
            struct Attributes { COMMON_2D_INPUTS half4 color:COLOR; UNITY_SKINNED_VERTEX_INPUTS };
            struct Varyings { COMMON_2D_OUTPUTS half4 color:COLOR; float localY:TEXCOORD4; };
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/2DCommon.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _MercuryTop;
            CBUFFER_END
            Varyings MercuryVertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                input.positionOS=UnityFlipSprite(input.positionOS,unity_SpriteProps.xy);
                Varyings output=CommonUnlitVertex(input);
                output.color=input.color*_Color*unity_SpriteColor;
                output.localY=input.positionOS.y;
                return output;
            }
            half4 MercuryFragment(Varyings input):SV_Target
            {
                clip(_MercuryTop-input.localY);
                return CommonUnlitFragment(input,input.color);
            }
            ENDHLSL
        }
    }
}
