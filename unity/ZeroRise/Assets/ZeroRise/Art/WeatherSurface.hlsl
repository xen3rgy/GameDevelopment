#ifndef ZERO_RISE_WEATHER_SURFACE
#define ZERO_RISE_WEATHER_SURFACE
TEXTURE2D(_ZRWeatherHeightMap);SAMPLER(sampler_ZRWeatherHeightMap);
float4 _ZRWeatherBounds;
float _ZRWeatherWetness,_ZRWeatherSoilMoisture,_ZRWeatherEnabled;
float2 _ZRWeatherTerrainOrigin;
float ZRWeatherExposure(float3 absolutePosition)
{
    float2 uv=(absolutePosition.xz-_ZRWeatherBounds.xy)*_ZRWeatherBounds.zw;
    float inside=step(0,uv.x)*step(uv.x,1)*step(0,uv.y)*step(uv.y,1);
    float roof=SAMPLE_TEXTURE2D_LOD(_ZRWeatherHeightMap,sampler_ZRWeatherHeightMap,saturate(uv),0).r;
    return lerp(1,smoothstep(roof-.5,roof-.08,absolutePosition.y),inside)*_ZRWeatherEnabled;
}
float ZRWeatherTerrainExposure(float2 absoluteXZ)
{
    float2 uv=(absoluteXZ-_ZRWeatherBounds.xy)*_ZRWeatherBounds.zw;
    float2 heights=SAMPLE_TEXTURE2D_LOD(_ZRWeatherHeightMap,sampler_ZRWeatherHeightMap,saturate(uv),0).rg;
    return (1-smoothstep(.4,.9,heights.r-heights.g))*_ZRWeatherEnabled;
}
#endif
