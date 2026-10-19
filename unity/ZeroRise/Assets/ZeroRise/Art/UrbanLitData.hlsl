// Extend the pinned HDRP Lit path instead of replacing its lighting/shadows.
#define GetSurfaceAndBuiltinData ZRStockSurfaceData
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitData.hlsl"
#undef GetSurfaceAndBuiltinData

#include "VegetationBend.hlsl"

#if defined(_ZR_FOLIAGE) || defined(_ZR_TREE) || defined(_ZR_SHRUB)
AttributesMesh ApplyMeshModification(AttributesMesh input, float3 timeParameters)
{
#if defined(_ZR_SHRUB)
    float now=_ZRPlantTime+timeParameters.x-_TimeParameters.x;
    float3 root=GetAbsolutePositionWS(TransformObjectToWorld(float3(0,0,0)));
    float3 position=GetAbsolutePositionWS(TransformObjectToWorld(input.positionOS));
    float3 axis;float angle;
    position=ZRShrubDeform(position,root,now,axis,angle);
    input.positionOS=TransformWorldToObject(GetCameraRelativePositionWS(position));
    float s,c;sincos(angle,s,c);
    input.normalOS=TransformWorldToObjectNormal(ZRRotate(TransformObjectToWorldNormal(input.normalOS),axis,s,c));
    input.tangentOS.xyz=TransformWorldToObjectDir(ZRRotate(TransformObjectToWorldDir(input.tangentOS.xyz),axis,s,c));
    return input;
#elif defined(_ZR_TREE)
    float now = _ZRPlantTime + timeParameters.x - _TimeParameters.x;
    float3 origin=GetAbsolutePositionWS(TransformObjectToWorld(float3(0,0,0)));
    float phase=dot(origin.xz,float2(.137,.193));
    float h=max(0,input.positionOS.y),crown=saturate((h-1.6)*.35);
    float2 wave=float2(sin(now*.61+phase),cos(now*.48+phase*1.7))*.0012;
    float flutter=sin(now*1.9+dot(input.positionOS.xyz,float3(2.1,.8,1.7))+phase)*.012*crown;
    input.positionOS.xz+=wave*h*h+float2(flutter,flutter*.6);
    input.normalOS=normalize(float3(input.normalOS.x,input.normalOS.y-dot(input.normalOS.xz,wave*2*h),input.normalOS.z));
    return input;
#else
    float3 root = float3(input.uv3.x, input.uv2.x, input.uv3.y);
    float weight = input.uv2.y;
    // Use the simulation clock in every render pass, with the previous-frame
    // time offset supplied by HDRP's motion-vector pass.
    float now = _ZRPlantTime + timeParameters.x - _TimeParameters.x;
    float3 axis;float angle;ZRPlantRotation(root,weight,now,axis,angle);
    float s, c; sincos(angle, s, c);
    float3 offset = GetAbsolutePositionWS(TransformObjectToWorld(input.positionOS)) - root;
    // Bend around the planted root, not the center of a combined render chunk.
    offset = ZRRotate(offset,axis,s,c);
    input.positionOS = TransformWorldToObject(GetCameraRelativePositionWS(root + offset));
    float3 normal = ZRRotate(TransformObjectToWorldNormal(input.normalOS),axis,s,c);
    input.normalOS = TransformWorldToObjectNormal(normal);
    input.tangentOS.xyz = TransformWorldToObjectDir(ZRRotate(TransformObjectToWorldDir(input.tangentOS.xyz),axis,s,c));
    return input;
#endif
}
#endif

#if defined(_ZR_ROAD)
float2 ZRHash(float2 p)
{
    return frac(sin(float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)))) * 43758.5453);
}
// Blend three independently offset texture samples on a triangular grid. All
// channels use the same samples, and explicit derivatives avoid mip seams.
void ZRRoadCoordinates(float2 world, out float2 a, out float2 b, out float2 c, out float3 weights)
{
    float2 warp = float2(sin(world.y*.083 + sin(world.x*.031)), sin(world.x*.067 + world.y*.021)) * .37;
    float2 uv = world*.23 + warp;
    float2 skew = float2(uv.x - uv.y*.57735027, uv.y*1.15470054);
    float2 cell = floor(skew), f = frac(skew);
    float2 i0, i1, i2;
    if (f.x + f.y < 1) { i0=cell; i1=cell+float2(1,0); i2=cell+float2(0,1); weights=float3(1-f.x-f.y,f.x,f.y); }
    else { i0=cell+1; i1=cell+float2(0,1); i2=cell+float2(1,0); weights=float3(f.x+f.y-1,1-f.x,1-f.y); }
    weights = pow(max(weights, .00001), 3); weights /= dot(weights, 1);
    a=uv+ZRHash(i0)*13.71; b=uv+ZRHash(i1)*13.71; c=uv+ZRHash(i2)*13.71;
}
#endif

void GetSurfaceAndBuiltinData(FragInputs input, float3 V, inout PositionInputs posInput, out SurfaceData surfaceData, out BuiltinData builtinData)
{
    ZRStockSurfaceData(input, V, posInput, surfaceData, builtinData);
#if defined(_ZR_FOLIAGE) || defined(_ZR_TREE) || defined(_ZR_SHRUB)
    surfaceData.baseColor *= input.color.rgb;
#endif
#if defined(_ZR_ROAD)
    float3 absolute = GetAbsolutePositionWS(input.positionRWS);
    float3 faceNormal = abs(input.tangentToWorld[2]);
    // Asphalt also covers roof slabs and their vertical returns. Project those
    // faces along their dominant axis instead of stretching one XZ texel row.
    // Horizontal roads retain exactly the existing nonrepeating coordinates.
    float2 world = faceNormal.y >= .5 ? absolute.xz
                 : faceNormal.x > faceNormal.z ? absolute.zy : absolute.xy;
    float2 a, b, c; float3 w;
    ZRRoadCoordinates(world, a, b, c, w);
    float2 dx=ddx(world*.23), dy=ddy(world*.23);
    float3 color = SAMPLE_TEXTURE2D_GRAD(_BaseColorMap,sampler_BaseColorMap,a,dx,dy).rgb*w.x
                 + SAMPLE_TEXTURE2D_GRAD(_BaseColorMap,sampler_BaseColorMap,b,dx,dy).rgb*w.y
                 + SAMPLE_TEXTURE2D_GRAD(_BaseColorMap,sampler_BaseColorMap,c,dx,dy).rgb*w.z;
    float macro = .91 + .09*sin(world.x*.051+sin(world.y*.09)) + .055*sin(world.y*.137+world.x*.021);
    surfaceData.baseColor = color * _BaseColor.rgb * macro;
    #if defined(_NORMALMAP)
    float3 n = UnpackNormalMapRGorAG(SAMPLE_TEXTURE2D_GRAD(_NormalMap,sampler_NormalMap,a,dx,dy),_NormalScale)*w.x
             + UnpackNormalMapRGorAG(SAMPLE_TEXTURE2D_GRAD(_NormalMap,sampler_NormalMap,b,dx,dy),_NormalScale)*w.y
             + UnpackNormalMapRGorAG(SAMPLE_TEXTURE2D_GRAD(_NormalMap,sampler_NormalMap,c,dx,dy),_NormalScale)*w.z;
    surfaceData.normalWS = normalize(mul(normalize(n), input.tangentToWorld));
    #endif
    #if defined(_MASKMAP)
    float4 mask = SAMPLE_TEXTURE2D_GRAD(_MaskMap,sampler_MaskMap,a,dx,dy)*w.x
                + SAMPLE_TEXTURE2D_GRAD(_MaskMap,sampler_MaskMap,b,dx,dy)*w.y
                + SAMPLE_TEXTURE2D_GRAD(_MaskMap,sampler_MaskMap,c,dx,dy)*w.z;
    surfaceData.ambientOcclusion = lerp(_AORemapMin,_AORemapMax,mask.g);
    surfaceData.perceptualSmoothness = lerp(_SmoothnessRemapMin,_SmoothnessRemapMax,mask.a);
    #endif
#endif
}
