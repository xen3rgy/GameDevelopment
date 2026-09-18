import {WORKSHOP_ROOM,WORKSHOP_FIXTURES} from './workshop-layout.js?v=0.7.2';
import {streetSurface} from './street-layout.js?v=0.7.2';
import {onGardenPath} from './pedestrian-layout.js?v=0.7.2';
import {STATION_PLAZAS} from './city-layout.js?v=0.7.2';
import {CAFE_FIXTURES} from './cafe.js?v=0.7.2';
// World-space surface heights match the top faces of the rendered geometry.
export function groundHeight(x,z,interior=null){
 if(interior||x>290)return .07;
 const street=streetSurface(x,z);if(street)return street.height;
 if(onGardenPath(x,z))return .02;
 let y=STATION_PLAZAS.some(p=>Math.abs(x-p.x)<=p.w/2&&Math.abs(z-p.z)<=p.d/2)?.18:-.05;
 if(Math.abs(x-76)<=19.5&&Math.abs(z-93)<=18){y=Math.max(y,.18);if(Math.abs(x-76)<=2.5||Math.abs(z-93)<=2)y=Math.max(y,.275)}
 return y;
}
export const ROOMS={
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
export const HOME_FIXTURES=[
 {id:'bed',x:296,z:-3,w:1.5,d:2,h:1.05},
 {id:'kitchen',x:303,z:-4.5,w:2,d:.55,h:1.72},
 {id:'fridge',x:305.8,z:-4.55,w:.6,d:.6,h:2.5},
 {id:'storage',x:295,z:2.3,w:.9,d:.38,h:1.4},
 {id:'table',x:299,z:1,w:1,d:.6,h:.7},
 {id:'sofa',x:298,z:3,w:1.5,d:.5,h:1.05},
 {id:'shower',x:305.8,z:2,w:.76,d:.76,h:.55},
 {id:'nightstand',x:294.1,z:-4.8,w:.375,d:.35,h:1.6}
];
export const homeFixtures=home=>['flat','penthouse'].includes(home)?[...HOME_FIXTURES,{id:'desk',x:302,z:1.8,w:1.3,d:.6,h:1.4},{id:'chair',x:301,z:3.2,w:.375,d:.375,h:.9}]:HOME_FIXTURES;
export function canWalkRoom(interior,x,z,r=.35,home='room'){const b=ROOMS[interior];return !!b&&x>b.minX+r&&x<b.maxX-r&&z>b.minZ+r&&z<b.maxZ-r&&!((interior==='shop'?SHOP_FIXTURES:interior==='cafe'?CAFE_FIXTURES:interior==='home'?homeFixtures(home):interior==='workshop'?WORKSHOP_FIXTURES:[]).some(c=>c.radius!=null?Math.hypot(x-c.x,z-c.z)<c.radius+r:Math.abs(x-c.x)<c.w+r&&Math.abs(z-c.z)<c.d+r))}
// Slab intersection gives a continuous boom limit, without discrete ray samples.
export function rayBox(origin,dir,b,max){let near=0,far=max;for(const axis of ['x','y','z']){const low=b['min'+axis.toUpperCase()],high=b['max'+axis.toUpperCase()];if(Math.abs(dir[axis])<1e-9){if(origin[axis]<low||origin[axis]>high)return max;continue}let a=(low-origin[axis])/dir[axis],c=(high-origin[axis])/dir[axis];if(a>c)[a,c]=[c,a];near=Math.max(near,a);far=Math.min(far,c);if(near>far)return max}return far>=0?Math.max(0,near):max}
export class CameraRig{
 reset(){this.height=null;this.boom=null;this.angle=null;this.pitch=null;this.zoom=null}
 constructor(){this.reset()}
 update(position,angle,pitch,distance,dt,colliders=[],room=null){
  dt=Math.max(0,dt);const orbitEase=1-Math.exp(-dt*24),desiredPitch=room?.06+(pitch-.06)*.48:pitch,desiredDistance=room?Math.min(distance,4.3):distance;
  this.angle=this.angle==null?angle:this.angle+Math.atan2(Math.sin(angle-this.angle),Math.cos(angle-this.angle))*orbitEase;
  this.pitch=this.pitch==null?desiredPitch:this.pitch+(desiredPitch-this.pitch)*orbitEase;
  this.zoom=this.zoom==null?desiredDistance:this.zoom+(desiredDistance-this.zoom)*(1-Math.exp(-dt*12));
  angle=this.angle;pitch=this.pitch;distance=this.zoom;
  const ease=1-Math.exp(-Math.max(0,dt)*12),targetY=position.y+1.4;
  this.height=this.height==null?targetY:this.height+(targetY-this.height)*ease;
  const anchor={x:position.x,y:room?Math.min(this.height,room.ceiling-.35):this.height,z:position.z};
  const dir={x:Math.sin(angle)*Math.cos(pitch),y:Math.sin(pitch),z:Math.cos(angle)*Math.cos(pitch)};
  let limit=distance;
  for(const c of colliders)limit=Math.min(limit,rayBox(anchor,dir,{minX:c.x-c.w-.15,maxX:c.x+c.w+.15,minZ:c.z-c.d-.15,maxZ:c.z+c.d+.15,minY:c.minY??-1,maxY:c.h+.2},distance));
  if(room){for(const axis of ['X','Z']){let k=axis.toLowerCase();if(dir[k]>1e-9)limit=Math.min(limit,(room['max'+axis]-.15-anchor[k])/dir[k]);if(dir[k]<-1e-9)limit=Math.min(limit,(room['min'+axis]+.15-anchor[k])/dir[k])}if(dir.y>0)limit=Math.min(limit,(room.ceiling-.15-anchor.y)/dir.y)}
  limit=Math.max(.15,limit-.05);
  this.boom=this.boom==null?limit:Math.min(limit,this.boom+(limit-this.boom)*(1-Math.exp(-dt*7)));
  return {anchor,position:{x:anchor.x+dir.x*this.boom,y:anchor.y+dir.y*this.boom,z:anchor.z+dir.z*this.boom},distance:this.boom};
 }
}
