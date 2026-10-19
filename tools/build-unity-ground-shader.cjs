// Pinned HDRP terrain raster passes plus coherent stochastic surface sampling.
// Terrain geometry/collision remains Unity TerrainData, not shader displacement.
const fs = require('node:fs');
const path = require('node:path');
const pkg = process.argv[2] || 'C:/Program Files/Unity/Hub/Editor/6000.6.5f1/Editor/Data/Resources/PackageManager/BuiltInPackages/com.unity.render-pipelines.high-definition';
const target = path.resolve('unity/ZeroRise/Assets/ZeroRise/Art');
const terrain = path.join(pkg, 'Runtime/Material/TerrainLit');
let shader = fs.readFileSync(path.join(terrain, 'TerrainLit.shader'), 'utf8');
const second = shader.indexOf('    SubShader', shader.indexOf('    SubShader') + 1);
if(second<0) throw Error('Expected pinned terrain raster/ray tracing subshaders');
shader = shader.slice(0,second) + `    Dependency "BaseMapShader" = "Hidden/HDRP/TerrainLit_Basemap"
    Dependency "BaseMapGenShader" = "Hidden/HDRP/TerrainLit_BasemapGen"
    CustomEditor "UnityEditor.Rendering.HighDefinition.TerrainLitGUI"
    FallBack "Hidden/HDRP/FallbackError"
}
`;
shader = shader.replace('Shader "HDRP/TerrainLit"', 'Shader "ZeroRise/UrbanGround"');
shader = shader.replaceAll('"Packages/com.unity.render-pipelines.high-definition/Runtime/Material/TerrainLit/TerrainLit_Splatmap.hlsl"', '"UrbanGroundSplatmap.hlsl"');
let splat = fs.readFileSync(path.join(terrain, 'TerrainLit_Splatmap.hlsl'), 'utf8');
splat = splat.replace('void TerrainSplatBlend(float2 controlUV, float2 splatBaseUV, inout TerrainLitSurfaceData surfaceData)', 'void TerrainSplatBlend(float2 controlUV, float2 splatBaseUV, float2 baseDx, float2 baseDy, inout TerrainLitSurfaceData surfaceData)');
splat = splat.replace('float2 dxuv = ddx(splatBaseUV);', 'float2 dxuv = baseDx;').replace('float2 dyuv = ddy(splatBaseUV);', 'float2 dyuv = baseDy;');
const original=`void TerrainLitShade(float2 uv, inout TerrainLitSurfaceData surfaceData)
{
    TerrainSplatBlend(uv, uv, surfaceData);
}`;
if(!splat.includes(original)) throw Error('Pinned terrain shade hook changed');
splat = splat.replace(original, `float2 ZRGroundHash(float2 p)
{
    return frac(sin(float2(dot(p,float2(127.1,311.7)),dot(p,float2(269.5,183.3))))*43758.5453);
}
float ZRGroundNoise(float2 p)
{
    float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);
    return lerp(lerp(ZRGroundHash(i).x,ZRGroundHash(i+float2(1,0)).x,f.x),lerp(ZRGroundHash(i+float2(0,1)).x,ZRGroundHash(i+1).x,f.x),f.y);
}
void TerrainLitShade(float2 uv, inout TerrainLitSurfaceData surfaceData)
{
    // Layer zero is authored at 1.8 metres per tile. Keep control-map locations
    // fixed; stochastic offsets affect only surface color/normal/mask samples.
    float2 metres=uv*_Splat0_ST.xy*1.8;
    float2 p=metres*.25;
    float2 skew=float2(p.x-p.y*.57735027,p.y*1.15470054);
    float2 cell=floor(skew),f=frac(skew),a,b,c;float3 w;
    if(f.x+f.y<1){a=cell;b=cell+float2(1,0);c=cell+float2(0,1);w=float3(1-f.x-f.y,f.x,f.y);}
    else{a=cell+1;b=cell+float2(0,1);c=cell+float2(1,0);w=float3(f.x+f.y-1,1-f.x,1-f.y);}
    w=pow(max(w,.00001),3);w/=dot(w,1);
    float2 warp=float2(ZRGroundNoise(metres*.14),ZRGroundNoise(metres*.14+31.7))-.5;
    float2 sampleUV=uv+warp*.19/_Splat0_ST.xy;
    // Use un-offset derivatives: a hash-cell boundary must not become a mip seam.
    float2 dx=ddx(sampleUV),dy=ddy(sampleUV);
    TerrainLitSurfaceData sa,sb,sc;
    InitializeTerrainLitSurfaceData(sa);InitializeTerrainLitSurfaceData(sb);InitializeTerrainLitSurfaceData(sc);
    TerrainSplatBlend(uv,sampleUV+ZRGroundHash(a)*19.17/_Splat0_ST.xy,dx,dy,sa);
    TerrainSplatBlend(uv,sampleUV+ZRGroundHash(b)*19.17/_Splat0_ST.xy,dx,dy,sb);
    TerrainSplatBlend(uv,sampleUV+ZRGroundHash(c)*19.17/_Splat0_ST.xy,dx,dy,sc);
    surfaceData.albedo=sa.albedo*w.x+sb.albedo*w.y+sc.albedo*w.z;
    surfaceData.normalData=sa.normalData*w.x+sb.normalData*w.y+sc.normalData*w.z;
    surfaceData.smoothness=sa.smoothness*w.x+sb.smoothness*w.y+sc.smoothness*w.z;
    surfaceData.metallic=0;
    surfaceData.ao=sa.ao*w.x+sb.ao*w.y+sc.ao*w.z;
    float macro=lerp(.79,1.12,ZRGroundNoise(metres*.065))+lerp(-.045,.045,ZRGroundNoise(metres*.31));
    surfaceData.albedo*=macro;
}`);
fs.writeFileSync(path.join(target,'UrbanGround.shader'),'// Derived from Unity HDRP 17.6 TerrainLit; see HDRP-LICENSE.md.\n'+shader);
fs.writeFileSync(path.join(target,'UrbanGroundSplatmap.hlsl'),'// Derived from Unity HDRP 17.6 TerrainLit_Splatmap; see HDRP-LICENSE.md.\n'+splat);
console.log('Generated UrbanGround raster terrain shader with coherent stochastic PBR sampling.');
