import {ROADS,ROAD_X,ROAD_Z} from './city-layout.js?v=0.8.0';
export const ROAD_HEIGHT=.14, PAVEMENT_HEIGHT=.30, HALF_ROAD=6.5, CORNER_RADIUS=2, RAMP_WIDTH=3, RAMP_CORE=.9;
// Centered around the road axis: equal kerb margins, including an odd center stripe.
export const CROSSWALK_STRIPE_WIDTH=.72;
export const CROSSWALK_OFFSETS=Object.freeze(Array.from({length:9},(_,i)=>(i-4)*1.4));
export const inRect=(p,x,z,margin=0)=>Math.abs(x-p.x)<=p.w/2+margin&&Math.abs(z-p.z)<=p.d/2+margin;
export const CORNERS=ROAD_X.flatMap(ix=>ROAD_Z.flatMap(iz=>[-1,1].flatMap(sx=>[-1,1].map(sz=>({ix,iz,sx,sz})))));
export function cornerRoadCutout(x,z){
 for(const c of CORNERS){
  const u=(x-c.ix)*c.sx-HALF_ROAD,v=(z-c.iz)*c.sz-HALF_ROAD;
  if(u>=0&&v>=0&&u<=CORNER_RADIUS&&v<=CORNER_RADIUS&&Math.hypot(CORNER_RADIUS-u,CORNER_RADIUS-v)>CORNER_RADIUS+1e-8)return c;
 }
 return null;
}
export const onRoad=(x,z)=>ROADS.some(p=>inRect(p,x,z))||!!cornerRoadCutout(x,z);
export const PAVEMENTS=ROADS.flatMap(r=>r.w>r.d?[-1,1].map(side=>({x:r.x,z:r.z+side*10,w:r.w,d:7})):[-1,1].map(side=>({x:r.x+side*10,z:r.z,w:7,d:r.d})));
// Crossings are shared by road markings, dropped kerbs, map and traffic yielding.
export const CROSSINGS=ROAD_X.flatMap(x=>ROAD_Z.flatMap(z=>[
 ...[-1,1].filter(side=>x+side*10>-214&&x+side*10<114).map(side=>({x:x+side*10,z,axis:'z'})),
 ...[-1,1].filter(side=>x!==-202||Math.abs(z+side*10)<78).map(side=>({x,z:z+side*10,axis:'x'}))
]));
export const RAMPS=CROSSINGS.flatMap(c=>[-1,1].map(side=>({x:c.x+(c.axis==='x'?side*7.5:0),z:c.z+(c.axis==='z'?side*7.5:0),w:c.axis==='z'?RAMP_WIDTH:2,d:c.axis==='z'?2:RAMP_WIDTH,side,axis:c.axis,crossing:c})));
export function rampHeight(r,x,z){
 const normal=(r.axis==='z'?z-r.crossing.z:x-r.crossing.x)*r.side-HALF_ROAD;
 const lateral=Math.abs(r.axis==='z'?x-r.x:z-r.z),half=RAMP_WIDTH/2;
 const t=Math.max(0,Math.min(1,normal/2)),wing=Math.max(0,Math.min(1,(lateral-RAMP_CORE)/(half-RAMP_CORE)));
 return ROAD_HEIGHT+(PAVEMENT_HEIGHT-ROAD_HEIGHT)*Math.max(t,wing);
}
export function streetSurface(x,z){
 if(onRoad(x,z))return {kind:'road',height:ROAD_HEIGHT};
 if(!PAVEMENTS.some(p=>inRect(p,x,z)))return null;
 const ramp=RAMPS.find(r=>inRect(r,x,z));
 return ramp?{kind:'ramp',height:rampHeight(ramp,x,z),ramp}:{kind:'pavement',height:PAVEMENT_HEIGHT};
}
// Disjoint rectangles remove all coplanar road overlaps and pavements across junctions.
export function streetPatches(){
 const xs=new Set(),zs=new Set();
 for(const p of [...ROADS,...PAVEMENTS,...RAMPS]){xs.add(p.x-p.w/2);xs.add(p.x+p.w/2);zs.add(p.z-p.d/2);zs.add(p.z+p.d/2);}
 for(const c of CORNERS){xs.add(c.ix+c.sx*HALF_ROAD);xs.add(c.ix+c.sx*(HALF_ROAD+CORNER_RADIUS));zs.add(c.iz+c.sz*HALF_ROAD);zs.add(c.iz+c.sz*(HALF_ROAD+CORNER_RADIUS));}
 for(const r of RAMPS){const set=r.axis==='z'?xs:zs,c=r.axis==='z'?r.x:r.z;set.add(c-RAMP_CORE);set.add(c+RAMP_CORE);}
 const xx=[...xs].sort((a,b)=>a-b),zz=[...zs].sort((a,b)=>a-b),patches=[];
 for(let i=1;i<xx.length;i++)for(let j=1;j<zz.length;j++){
  const minX=xx[i-1],maxX=xx[i],minZ=zz[j-1],maxZ=zz[j],surface=streetSurface((minX+maxX)/2,(minZ+maxZ)/2);
  if(surface)patches.push({minX,maxX,minZ,maxZ,...surface});
 }
 return patches;
}
export function crossingObstacles(pedestrians){
 const obstacles=[];
 for(const p of pedestrians){if(p.crossing===false)continue;
  for(const c of CROSSINGS){const along=c.axis==='z'?p.z-c.z:p.x-c.x,lateral=c.axis==='z'?p.x-c.x:p.z-c.z,velocity=c.axis==='z'?(p.approachVz??p.vz):(p.approachVx??p.vx);
   if(Math.abs(lateral)<2&&Math.abs(along)>6.2&&Math.abs(along)<9.5&&(velocity||0)*-along>.2)obstacles.push({x:c.x+(c.axis==='x'?Math.sign(along)*3.25:0),z:c.z+(c.axis==='z'?Math.sign(along)*3.25:0),angle:c.axis==='z'?0:Math.PI/2,length:6.5,width:3.2});
  }
 }
 return obstacles;
}
