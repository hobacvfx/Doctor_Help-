Shader "DoctorHelp/ToolSelectionGlow"
{
    Properties { _MainTex ("Sprite", 2D) = "white" {} }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest Always
        Cull Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            float4 _MainTex_TexelSize, _SpriteRect;
            float _Thickness, _Brightness;
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            Varyings vert(Attributes i) { Varyings o; o.positionCS=TransformObjectToHClip(i.positionOS.xyz); o.uv=i.uv; return o; }
            float alphaAt(float2 uv)
            {
                float inside = step(_SpriteRect.x,uv.x)*step(_SpriteRect.y,uv.y)*step(uv.x,_SpriteRect.z)*step(uv.y,_SpriteRect.w);
                return SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,uv).a * inside;
            }
            half4 frag(Varyings i) : SV_Target
            {
                float glow = 0;
                [unroll] for (int d=0; d<16; d++)
                {
                    float angle = d * 6.2831853 / 16;
                    float2 direction = float2(cos(angle),sin(angle))*_MainTex_TexelSize.xy*_Thickness;
                    glow = max(glow, alphaAt(i.uv+direction)*0.28);
                    glow = max(glow, alphaAt(i.uv+direction*0.6)*0.65);
                    glow = max(glow, alphaAt(i.uv+direction*0.3));
                }
                return half4(1,1,1,saturate(glow*_Brightness)*(1-alphaAt(i.uv)));
            }
            ENDHLSL
        }
    }
}
