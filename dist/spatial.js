import {CLOTHING_ROOM,CLOTHING_FIXTURES} from './clothing.js?v=0.8.1';
import {EXPANSION_PATHS} from './expansion-layout.js?v=0.8.1';
import {homeFixtures} from './home-layout.js?v=0.8.1';
export {homeFixtures} from './home-layout.js?v=0.8.1';
import {onServiceApron} from './service-layout.js?v=0.8.1';
import {WORKSHOP_ROOM,WORKSHOP_FIXTURES} from './workshop-layout.js?v=0.8.1';
import {frontageSurface} from './frontage-layout.js?v=0.8.1';
import {streetSurface} from './street-layout.js?v=0.8.1';
import {onGardenPath} from './pedestrian-layout.js?v=0.8.1';
import {STATION_PLAZAS,STATION_YARD,STATION_STEPS,stationStepOpen} from './city-layout.js?v=0.8.1';
import {CAFE_FIXTURES} from './cafe.js?v=0.8.1';
// World-space surface heights match the top faces of the rendered geometry.
export function groundHeight(x,z,interior=null){
 if(interior)return .07;
 if(onServiceApron(x,z))return .30;
 const street=streetSurface(x,z);if(street)return street.height;
 if(onGardenPath(x,z)||EXPANSION_PATHS.some(p=>Math.abs(x-p.x)<=p.w/2&&Math.abs(z-p.z)<=p.d/2))return .02;
 const frontage=frontageSurface(x,z);if(frontage)return frontage.height;
 const inStationYard=x>=STATION_YARD.minX&&x<=STATION_YARD.maxX&&z>=STATION_YARD.minZ&&z<=STATION_YARD.maxZ;
 let y=inStationYard?STATION_YARD.height:-.05;
 if(inStationYard&&stationStepOpen(z)&&x>=STATION_STEPS.minX&&x<STATION_STEPS.maxX)y=STATION_STEPS.mid;
 if(Math.abs(x-76)<=19.5&&Math.abs(z-93)<=18){y=Math.max(y,.18);if(Math.abs(x-76)<=2.5||Math.abs(z-93)<=2)y=Math.max(y,.275)}
 return y;
}
export const ROOMS={
 clothing:CLOTHING_ROOM,
 workshop:WORKSHOP_ROOM,
 cafe:{minX:292.25,maxX:307.75,minZ:72.25,maxZ:87.75,ceiling:3.8,spawn:{x:300,z:86},outside:{x:63,z:-11},angle:0},
 home:{minX:293.25,maxX:306.75,minZ:-5.75,maxZ:5.75,ceiling:3.35,spawn:{x:300,z:4},outside:{x:-34,z:10},angle:0},
 shop:{minX:333.25,maxX:346.75,minZ:31.25,maxZ:48.75,ceiling:3.65,spawn:{x:340,z:47},outside:{x:-36,z:-11},angle:0}
};
export const SHOP_FIXTURES=[
 {id:'drinks',x:334.3,z:38.5,w:.78,d:3.54,h:2.65},
 {id:'food',x:338.3,z:38.5,w:.69,d:3.54,h:1.7},
 {id:'ingredients',x:342.3,z:38.5,w:.7,d:3.54,h:2.05},
 {id:'medicine',x:344,z:32,w:2,d:.57,h:2.2},
 {id:'checkout',x:344.6,z:45.5,w:1.48,d:.68,h:1.7},
 {id:'produce',x:334.8,z:44.6,w:1.32,d:.84,h:1.4},
 {id:'bakery',x:337,z:32,w:1.62,d:.57,h:2.1},
 {id:'baskets',x:341.6,z:47.45,w:.34,d:.28,h:.8}
];
export const HOME_FIXTURES=homeFixtures('room');
export function canWalkRoom(interior,x,z,r=.35,home='room'){const b=ROOMS[interior];return !!b&&x>b.minX+r&&x<b.maxX-r&&z>b.minZ+r&&z<b.maxZ-r&&!((interior==='clothing'?CLOTHING_FIXTURES:interior==='shop'?SHOP_FIXTURES:interior==='cafe'?CAFE_FIXTURES:interior==='home'?homeFixtures(home):interior==='workshop'?WORKSHOP_FIXTURES:[]).some(c=>(c.minY??0)<1.85&&(c.radius!=null?Math.hypot(x-c.x,z-c.z)<c.radius+r:Math.abs(x-c.x)<c.w+r&&Math.abs(z-c.z)<c.d+r)))}
// Slab intersection gives a continuous boom limit, without discrete ray samples.
export function rayBox(origin,dir,b,max){let near=0,far=max;for(const axis of ['x','y','z']){const low=b['min'+axis.toUpperCase()],high=b['max'+axis.toUpperCase()];if(Math.abs(dir[axis])<1e-9){if(origin[axis]<low||origin[axis]>high)return max;continue}let a=(low-origin[axis])/dir[axis],c=(high-origin[axis])/dir[axis];if(a>c)[a,c]=[c,a];near=Math.max(near,a);far=Math.min(far,c);if(near>far)return max}return far>=0?Math.max(0,near):max}
export class CameraRig{
 reset(){this.height=null;this.boom=null;this.angle=null;this.pitch=null;this.zoom=null}
 constructor(){this.reset()}
 update(position,angle,pitch,distance,dt,colliders=[],room=null,shelter=null){
  dt=Number.isFinite(dt)?Math.max(0,Math.min(dt,.25)):0;const orbitEase=1-Math.exp(-dt*24),desiredPitch=shelter?Math.min(pitch,Math.atan2(shelter.ceiling-position.y-1.65,Math.max(distance,shelter.minBoom))):room?.06+(pitch-.06)*.48:pitch,desiredDistance=room?Math.min(distance,4.3):distance;
  this.angle=this.angle==null?angle:this.angle+Math.atan2(Math.sin(angle-this.angle),Math.cos(angle-this.angle))*orbitEase;
  this.pitch=this.pitch==null?desiredPitch:this.pitch+(desiredPitch-this.pitch)*orbitEase;
  this.zoom=this.zoom==null?desiredDistance:this.zoom+(desiredDistance-this.zoom)*(1-Math.exp(-dt*12));
  angle=this.angle;pitch=this.pitch;distance=this.zoom;
  const ease=1-Math.exp(-Math.max(0,dt)*12),targetY=position.y+1.4;
  this.height=this.height==null?targetY:this.height+(targetY-this.height)*ease;
  const anchor={x:position.x,y:room?Math.min(this.height,room.ceiling-.35):this.height,z:position.z};
  let dir={x:Math.sin(angle)*Math.cos(pitch),y:Math.sin(pitch),z:Math.cos(angle)*Math.cos(pitch)};
  let limit=distance;
  for(const c of colliders)limit=Math.min(limit,rayBox(anchor,dir,{minX:c.x-(c.w??c.radius)-.22,maxX:c.x+(c.w??c.radius)+.22,minZ:c.z-(c.d??c.radius)-.22,maxZ:c.z+(c.d??c.radius)+.22,minY:c.minY??-1,maxY:c.h+.2},distance));
  if(room){for(const axis of ['X','Z']){let k=axis.toLowerCase();if(dir[k]>1e-9)limit=Math.min(limit,(room['max'+axis]-.15-anchor[k])/dir[k]);if(dir[k]<-1e-9)limit=Math.min(limit,(room['min'+axis]+.15-anchor[k])/dir[k])}if(dir.y>0)limit=Math.min(limit,(room.ceiling-.22-anchor.y)/dir.y);if(dir.y<0)limit=Math.min(limit,(.25-anchor.y)/dir.y)}
  // Under an open workshop canopy, preserve a third-person clearance. If a
  // wall blocks the requested orbit, slide around it to the nearest clear arc.
  if(shelter&&limit<shelter.minBoom+.05){let best=null;for(let step=1;step<=32&&!best;step++)for(const side of [-1,1]){const yaw=angle+side*step*Math.PI/32,candidate={x:Math.sin(yaw)*Math.cos(pitch),y:Math.sin(pitch),z:Math.cos(yaw)*Math.cos(pitch)};let clear=distance;for(const c of colliders)clear=Math.min(clear,rayBox(anchor,candidate,{minX:c.x-(c.w??c.radius)-.22,maxX:c.x+(c.w??c.radius)+.22,minZ:c.z-(c.d??c.radius)-.22,maxZ:c.z+(c.d??c.radius)+.22,minY:c.minY??-1,maxY:c.h+.2},distance));if(clear>=shelter.minBoom+.05){best={dir:candidate,limit:clear};break;}}if(best){dir=best.dir;limit=best.limit;}}
  limit=Math.max(.15,limit-.05);
  let anticipated=limit;
  if(shelter){for(const offset of [-.3,-.15,.15,.3]){const yaw=Math.atan2(dir.x,dir.z)+offset,probe={x:Math.sin(yaw)*Math.cos(pitch),y:dir.y,z:Math.cos(yaw)*Math.cos(pitch)};let clear=distance;for(const c of colliders)clear=Math.min(clear,rayBox(anchor,probe,{minX:c.x-(c.w??c.radius)-.22,maxX:c.x+(c.w??c.radius)+.22,minZ:c.z-(c.d??c.radius)-.22,maxZ:c.z+(c.d??c.radius)+.22,minY:c.minY??-1,maxY:c.h+.2},distance));anticipated=Math.min(anticipated,Math.max(shelter.minBoom,clear-.05));}}
  this.boom=this.boom==null?limit:Math.min(limit,this.boom+(anticipated-this.boom)*(1-Math.exp(-dt*(shelter?12:7))));
  return {anchor,position:{x:anchor.x+dir.x*this.boom,y:anchor.y+dir.y*this.boom,z:anchor.z+dir.z*this.boom},distance:this.boom};
 }
}
