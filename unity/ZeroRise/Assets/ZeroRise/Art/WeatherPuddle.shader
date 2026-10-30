Shader "ZeroRise/WeatherPuddle"
{
    SubShader
    {
        Tags { "RenderPipeline"="HDRenderPipeline" "Queue"="Transparent-10" "RenderType"="Transparent" }
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
            float _ZRWeatherPuddles,_ZRWeatherRain,_ZRWeatherDaylight;
            float3 _ZRWeatherSunDirection;
            struct Attributes { float3 positionOS:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR; };
            struct Varyings { float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;float3 positionWS:TEXCOORD1;float4 color:COLOR; };
            Varyings Vert(Attributes a) { Varyings o;float3 w=TransformObjectToWorld(a.positionOS);o.positionCS=TransformWorldToHClip(w);o.positionWS=GetAbsolutePositionWS(w);o.uv=a.uv;o.color=a.color;return o; }
            float4 Frag(Varyings i):SV_Target
            {
                float radius=length(i.uv);float extent=sqrt(saturate(_ZRWeatherPuddles));
                float alpha=saturate((extent-radius)*8)*saturate(_ZRWeatherPuddles*4)*.42;
                float2 ripple=float2(sin(i.positionWS.x*19+_Time.y*5),cos(i.positionWS.z*17-_Time.y*4))*.008*_ZRWeatherRain;
                float3 n=normalize(float3(ripple.x,1,ripple.y));
                float3 view=normalize(_WorldSpaceCameraPos-i.positionWS);
                float fresnel=pow(1-saturate(dot(n,view)),4);
                float daylight=lerp(.075,.52,_ZRWeatherDaylight);
                float3 sky=lerp(float3(.028,.037,.041),float3(.28,.32,.34)*daylight,fresnel);
                float highlight=pow(saturate(dot(reflect(-_ZRWeatherSunDirection,n),view)),160)*_ZRWeatherDaylight*.20;
                return float4((sky+highlight)*lerp(.20,1,_ZRWeatherDaylight),alpha);
            }
            ENDHLSL
        }
    }
}
