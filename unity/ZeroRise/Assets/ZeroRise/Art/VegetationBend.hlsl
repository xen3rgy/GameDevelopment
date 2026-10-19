#ifndef ZERO_RISE_VEGETATION_BEND
#define ZERO_RISE_VEGETATION_BEND
// Shared by the actual HDRP vertex passes and the focused GPU verification.
float4 _ZRPlantFrom[24]; // xyz: segment start, w: contact time
float4 _ZRPlantTo[24];   // xyz: segment end, w: radius
float4 _ZRPlantBounds[2];
float _ZRPlantTime;

float3 ZRRotate(float3 p, float3 axis, float s, float c)
{
    return p*c + cross(axis,p)*s + axis*dot(axis,p)*(1-c);
}
void ZRPlantRotation(float3 root, float weight, float now, out float3 axis, out float angle)
{
    float phase=dot(root.xz,float2(1.71,2.37));
    float2 bend=float2(.055,.027)*(sin(now*1.45+phase)+.4*sin(now*2.73+phase*.43));
    float2 push=0;float total=0;
    [unroll] for(int actor=0;actor<2;actor++)
    {
        if(_ZRPlantBounds[actor].w<.5 || distance(root.xz,_ZRPlantBounds[actor].xy)>_ZRPlantBounds[actor].z)continue;
        [loop] for(int slot=0;slot<12;slot++)
        {
            int i=actor*12+slot;
            float age=now-_ZRPlantFrom[i].w;
            float3 a=_ZRPlantFrom[i].xyz,b=_ZRPlantTo[i].xyz;
            float2 segment=b.xz-a.xz;
            float along=saturate(dot(root.xz-a.xz,segment)/max(dot(segment,segment),.0001));
            float3 nearest=lerp(a,b,along);
            float2 delta=root.xz-nearest.xz;
            float radius=_ZRPlantTo[i].w;
            float vertical=1-smoothstep(.8,1.6,abs(root.y-nearest.y));
            float falloff=1-smoothstep(0,max(radius,.01),length(delta));
            // Ease in on contact and return gently once the actor has passed.
            float recovery=smoothstep(0,.38,max(age,0))*exp(-max(age,0)*2.8)*(1-smoothstep(1.1,1.45,age));
            float strength=falloff*recovery*vertical*step(0,age)*step(age,1.45)*step(.01,radius);
            // Travel bends stems forward; retain that impulse while it relaxes
            // instead of flipping it as the actor crosses the planted root.
            float2 radial=normalize(delta+float2(.0001,.0002));
            float2 travel=normalize(segment+float2(.0001,.0002));
            float2 direction=normalize(lerp(radial,normalize(travel*.85+radial*.15),smoothstep(.01,.1,length(segment))));
            push+=direction*strength;total+=strength;
        }
    }
    // Blend overlapping impulses instead of abruptly selecting a different one.
    bend+=push/max(total,1)*.85;
    angle=min(length(bend),.72)*weight;
    axis=normalize(float3(bend.y,.00001,-bend.x));
}
// Woody undergrowth yields around its planted base. Nearby branches receive
// slightly different contact samples, so the canopy parts rather than moving
// as a rigid miniature tree. The existing bounded history supplies smooth
// attack and recovery, without plant colliders or per-plant simulation.
float3 ZRShrubDeform(float3 position, float3 root, float now, out float3 axis, out float angle)
{
    float3 offset=position-root;
    float weight=smoothstep(.012,.85,max(0,offset.y));
    float3 branch=root+float3(offset.x*.55,0,offset.z*.55);
    ZRPlantRotation(branch,weight,now,axis,angle);
    float s,c;sincos(angle,s,c);
    float3 bent=ZRRotate(offset,axis,s,c);
    float flutter=sin(now*3.1+dot(position,float3(4.7,.9,3.2)))*.003*weight*weight;
    return root+bent+float3(flutter,0,flutter*.63);
}
#endif
