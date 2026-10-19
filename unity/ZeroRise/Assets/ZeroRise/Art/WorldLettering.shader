Shader "ZeroRise/WorldLettering"
{
    Properties { _MainTex("Font alpha",2D)="white"{} }
    SubShader
    {
        Tags { "RenderPipeline"="HDRenderPipeline" "RenderType"="TransparentCutout" "Queue"="AlphaTest" }
        Pass
        {
            Tags { "LightMode"="ForwardOnly" }
            Cull Off ZWrite On
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 4.5
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariablesFunctions.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            struct Attributes {float3 positionOS:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
            struct Varyings {float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
            Varyings Vert(Attributes input)
            {
                Varyings output;output.positionCS=TransformWorldToHClip(TransformObjectToWorld(input.positionOS));output.uv=input.uv;output.color=input.color;return output;
            }
            float4 Frag(Varyings input):SV_Target
            {
                float alpha=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,input.uv).a;clip(alpha-.4);
                return float4(input.color.rgb,1);
            }
            ENDHLSL
        }
    }
}
