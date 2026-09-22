import * as THREE from './vendor/three.module.js';
import {ROAD_X,ROAD_Z} from './city-layout.js?v=0.8.0';
import {ROAD_HEIGHT,PAVEMENT_HEIGHT,HALF_ROAD,CORNER_RADIUS,CORNERS,RAMP_WIDTH,RAMP_CORE,CROSSINGS,CROSSWALK_OFFSETS,CROSSWALK_STRIPE_WIDTH,streetPatches,rampHeight,onRoad,streetSurface} from './street-layout.js?v=0.8.0';
import {surfaceMaterial,surfaceTileMeters} from './atmosphere.js?v=0.8.0';

export function buildStreets(world,kit){
 const g=new THREE.Group();g.name='Connected streets';world.scene.add(g);world.staticGroups.push(g);
 const batches=new Map();
 const quad=(key,a,b,c,d)=>{if(!batches.has(key))batches.set(key,[]);batches.get(key).push(...a,...b,...c,...a,...c,...d);};
 const tri=(key,a,b,c)=>{let bb=b,cc=c;const area=(bb[0]-a[0])*(cc[2]-a[2])-(bb[2]-a[2])*(cc[0]-a[0]);if(area>0)[bb,cc]=[cc,bb];if(!batches.has(key))batches.set(key,[]);batches.get(key).push(...a,...bb,...cc);};
 const cornerForPatch=p=>CORNERS.find(c=>{const xa=c.ix+c.sx*HALF_ROAD,xb=c.ix+c.sx*(HALF_ROAD+CORNER_RADIUS),za=c.iz+c.sz*HALF_ROAD,zb=c.iz+c.sz*(HALF_ROAD+CORNER_RADIUS);return Math.abs(p.minX-Math.min(xa,xb))<1e-7&&Math.abs(p.maxX-Math.max(xa,xb))<1e-7&&Math.abs(p.minZ-Math.min(za,zb))<1e-7&&Math.abs(p.maxZ-Math.max(za,zb))<1e-7;});
 for(const p of streetPatches()){
  const {minX:x,maxX:X,minZ:z,maxZ:Z}=p,key=p.kind==='road'?'asphalt':(x+X)/2<-120?'oldPaving':'paving',corner=cornerForPatch(p);
  if(corner){
   const topKey=(x+X)/2<-120?'oldPaving':'paving',segments=8,{ix,iz,sx,sz}=corner;
   const inner=[ix+sx*HALF_ROAD,ROAD_HEIGHT,iz+sz*HALF_ROAD],outer=[ix+sx*(HALF_ROAD+CORNER_RADIUS),PAVEMENT_HEIGHT,iz+sz*(HALF_ROAD+CORNER_RADIUS)],arcTop=[],arcRoad=[];
   for(let i=0;i<=segments;i++){const theta=Math.PI+i/segments*Math.PI/2,u=CORNER_RADIUS+Math.cos(theta)*CORNER_RADIUS,v=CORNER_RADIUS+Math.sin(theta)*CORNER_RADIUS,px=ix+sx*(HALF_ROAD+u),pz=iz+sz*(HALF_ROAD+v);arcTop.push([px,PAVEMENT_HEIGHT,pz]);arcRoad.push([px,ROAD_HEIGHT,pz]);}
   for(let i=0;i<segments;i++){tri(topKey,outer,arcTop[i],arcTop[i+1]);tri('asphalt',inner,arcRoad[i],arcRoad[i+1]);quad('kerb',arcTop[i],arcRoad[i],arcRoad[i+1],arcTop[i+1]);}
   continue;
  }
  const h=(px,pz)=>p.kind==='ramp'?rampHeight(p.ramp,px,pz):p.height;
  const a=[x,h(x,z),z],b=[x,h(x,Z),Z],c=[X,h(X,Z),Z],d=[X,h(X,z),z];
  // A ramp's side flare has a diagonal crease; match the analytic support surface.
  if(p.kind==='ramp'){
   const r=p.ramp,lateralCenter=r.axis==='z'?(x+X)/2-r.x:(z+Z)/2-r.z;
   if(Math.abs(lateralCenter)>RAMP_CORE){
    const side=Math.sign(lateralCenter),wingSpan=RAMP_WIDTH/2-RAMP_CORE,difference=v=>((r.axis==='z'?v[2]-r.crossing.z:v[0]-r.crossing.x)*r.side-6.5)/2-(((r.axis==='z'?v[0]-r.x:v[2]-r.z)*side-RAMP_CORE)/wingSpan);
    // Split at the actual crease, even when another street subdivides a flare.
    for(const sign of [-1,1]){
     const polygon=[a,b,c,d],clipped=[];
     for(let i=0;i<4;i++){const u=polygon[i],v=polygon[(i+1)%4],du=difference(u)*sign,dv=difference(v)*sign;
      if(du>=0)clipped.push(u);if((du<0)!==(dv<0)){const t=du/(du-dv),px=u[0]+(v[0]-u[0])*t,pz=u[2]+(v[2]-u[2])*t;clipped.push([px,rampHeight(r,px,pz),pz]);}
     }
     if(!batches.has(key))batches.set(key,[]);for(let i=1;i<clipped.length-1;i++)batches.get(key).push(...clipped[0],...clipped[i],...clipped[i+1]);
    }
   }else quad(key,a,b,c,d);
  }else quad(key,a,b,c,d);
  if(p.kind==='road')continue;
  for(const [u,v,px,pz] of [[a,b,x-.001,(z+Z)/2],[b,c,(x+X)/2,Z+.001],[c,d,X+.001,(z+Z)/2],[d,a,(x+X)/2,z-.001]]){
   const neighbor=streetSurface(px,pz);
   if(neighbor&&neighbor.kind!=='road')continue;
   // Close BOTH road-facing and garden/yard-facing edges. The base extends into soil
   // so the pavement cannot be seen from underneath at changes in ground level.
   const bottom=neighbor?ROAD_HEIGHT:-.08;
   quad('kerb',u,[u[0],bottom,u[2]],[v[0],bottom,v[2]],v);
  }
 }
 for(const [kind,vertices] of batches){
  const geo=new THREE.BufferGeometry(),pos=new Float32Array(vertices),uv=[];
  const tile=surfaceTileMeters(kind==='asphalt'?'asphalt':kind==='oldPaving'?'cobble':'paving');
  for(let i=0;i<vertices.length;i+=3)uv.push(vertices[i]/tile,vertices[i+2]/tile);
  geo.setAttribute('position',new THREE.BufferAttribute(pos,3));geo.setAttribute('uv',new THREE.Float32BufferAttribute(uv,2));geo.computeVertexNormals();geo.computeBoundingSphere();
  const material=kind==='kerb'?new THREE.MeshStandardMaterial({color:0xa5aaa3,roughness:.94,side:THREE.DoubleSide}):surfaceMaterial(kind==='asphalt'?'asphalt':kind==='oldPaving'?'cobble':'paving',6,6);
  if(material.map)material.map.repeat.set(1,1);
  if(kind==='asphalt')world.atmosphere.materials.push(material);
  const mesh=new THREE.Mesh(geo,material);mesh.name='Street · '+kind;mesh.receiveShadow=true;g.add(mesh);
 }
 const {box}=kit,y=ROAD_HEIGHT+.006,paint=0xd5d1bd;
 for(const c of CROSSINGS){
  for(const offset of CROSSWALK_OFFSETS)box(g,c.x+(c.axis==='x'?offset:0),y,c.z+(c.axis==='z'?offset:0),c.axis==='z'?3.2:CROSSWALK_STRIPE_WIDTH,.005,c.axis==='z'?CROSSWALK_STRIPE_WIDTH:3.2,paint);
 }
 // Lane markings stop before crossings and intersections; no stripe cuts across a kerb.
 for(const z of ROAD_Z)for(let x=-211;x<111;x+=7){if(ROAD_X.some(j=>Math.abs(j-x)<15))continue;box(g,x,y,z,2.8,.005,.12,paint);}
 for(const x of ROAD_X)for(let z=x===-202?-54:-111;z<=(x===-202?54:111);z+=7){if(ROAD_Z.some(j=>Math.abs(j-z)<15))continue;box(g,x,y,z,.12,.005,2.8,paint);}
 // Recessed drains at the road edge, never raised platforms in a driving lane.
 for(const z of ROAD_Z)for(const x of [-182,-151,-84,-48,42,87]){
  for(const side of [-1,1]){const dz=z+side*6.13;box(g,x,y,dz,.72,.006,.4,0x3c4749);for(let n=0;n<6;n++)box(g,x-.28+n*.11,y+.005,dz,.032,.004,.33,0x6d7674);}
 }
 return g;
}
