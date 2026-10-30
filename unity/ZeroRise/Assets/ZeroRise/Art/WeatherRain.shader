Shader "ZeroRise/WeatherRain"
{
    SubShader
    {
        Tags { "RenderPipeline"="HDRenderPipeline" "Queue"="Transparent+10" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="ForwardOnly" }
            Cull Off ZWrite Off ZTest LEqual Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariablesFunctions.hlsl"
            struct Attributes { float3 positionOS:POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            Varyings Vert(Attributes a) { Varyings o; o.positionCS=TransformWorldToHClip(TransformObjectToWorld(a.positionOS));o.uv=a.uv;o.color=a.color;return o; }
            float4 Frag(Varyings i):SV_Target
            {
                float edge=saturate(1-abs(i.uv.x*2-1));
                float tip=saturate(min(i.uv.y,1-i.uv.y)*5);
                return float4(i.color.rgb,i.color.a*edge*edge*tip);
            }
            ENDHLSL
        }
    }
}
