import * as THREE from './vendor/three.module.js';
import {availableVehicles} from './fleet.js?v=0.8.0';
import {PARKING_SPACES,TRANSIT_STOPS,stopById} from './mobility-layout.js?v=0.8.0';
import {groundHeight} from './spatial.js?v=0.8.0';
export function transitPose(t){const e=Math.max(0,Math.min(8.5,t));return {approach:Math.max(0,1-e/1.5),boarding:Math.max(0,Math.min(1,(e-1.5)/2)),departure:Math.max(0,Math.min(1,(e-3.8)/2.7)),fade:Math.max(0,Math.min(1,(e-6.5)/1.2)),door:e>1.4&&e<3.8?1:0,visible:e<3.5};}
export class MobilityScene{
 constructor(world,kit){this.world=world;this.root=new THREE.Group();world.scene.add(this.root);const {box,cylinder,sign}=kit;
  for(const p of PARKING_SPACES){const y=groundHeight(p.x,p.z)+.012;for(const dz of [-1.15,1.15])box(this.root,p.x,y,p.z+dz,6.3,.016,.065,0xd0c9ac);for(const dx of [-3.15,3.15])box(this.root,p.x+dx,y,p.z,.065,.016,2.3,0xd0c9ac);}
  for(const p of TRANSIT_STOPS){const x=p.x-1.6,z=p.z,y=groundHeight(x,z);cylinder(this.root,x,y+1.5,z,.055,3,0x526763);sign(this.root,p.mode==='bus'?'H · B1':'S1',x,y+2.8,z,1.0,.6,'#173d32','#ecdb95');sign(this.root,p.mode==='bus'?'H · B1':'S1',x,y+2.8,z-.055,1,.6,'#173d32','#ecdb95',Math.PI);sign(this.root,p.name,x,y+2.25,z,2.7,.34);sign(this.root,p.name,x,y+2.25,z-.055,2.7,.34,undefined,undefined,Math.PI);world.colliders.push({x,z,w:.08,d:.08,h:3});}
  this.bus=new THREE.Group();this.root.add(this.bus);box(this.bus,0,1.6,0,2.35,2.65,7.4,0x285f59);box(this.bus,0,1.4,0,2.39,.55,7.42,0xd4c8a6);box(this.bus,0,2.35,0,2.38,.92,6.8,0x20383e);box(this.bus,0,2.1,3.73,2.1,1.1,.035,0x213c45);box(this.bus,0,.75,3.76,2.3,.2,.1,0xbfc6bc);sign(this.bus,'B1 · LINDENSTADT',0,2.93,3.76,2,.28,'#ffe5a5','#122b2c');for(const z of [-2.35,2.35])for(const x of [-1.15,1.15]){const w=cylinder(this.bus,x,.6,z,.48,.2,0x242d2c);w.rotation.z=Math.PI/2;}this.door=box(this.bus,-1.21,1.5,2.2,.04,2.25,1.15,0x71938d);
  this.train=new THREE.Group();this.root.add(this.train);for(const z of [-5,5]){box(this.train,0,1.7,z,2.65,2.6,9.6,0xbfc7b8);box(this.train,0,2.2,z,2.68,.95,9.1,0x29454c);box(this.train,0,.75,z,2.7,.4,9.5,0x337669);}sign(this.train,'S1 · LINDENSTADT',0,2.7,9.85,2.4,.3);this.bus.visible=this.train.visible=false;
 }
 syncFleet(s){const w=this.world,live=new Set();for(const v of availableVehicles(s)){live.add(v.uid);w.makeOwnedVehicle(v.id,v.uid);const mesh=w.vehicleMeshes.get(v.uid);mesh.visible=!s.inside;mesh.position.set(v.x,groundHeight(v.x,v.z)-(v.id==='bike'?.015:.03),v.z);mesh.rotation.y=v.angle;}for(const [uid,m] of w.vehicleMeshes)if(!live.has(uid))m.visible=false;w.vehicleMesh=s.vehicle?w.vehicleMeshes.get(s.vehicle.uid):null;w.vehicleId=s.vehicle?.id;}
 update(s){this.root.visible=!s.inside;this.bus.visible=this.train.visible=false;if(!s.transit)return;const t=s.transit,stop=stopById(t.from),pose=transitPose(t.elapsed),v=stop.vehicle,mesh=t.mode==='bus'?this.bus:this.train;mesh.visible=true;mesh.position.set(v.x+Math.sin(v.angle)*(-pose.approach*14+pose.departure*18),t.mode==='bus'?groundHeight(v.x,v.z):7.1,v.z+Math.cos(v.angle)*(-pose.approach*14+pose.departure*18));mesh.rotation.y=v.angle;this.door.position.z=2.2-pose.door*.85;
  const p=this.world.player,door={x:v.x-Math.cos(v.angle)*1.45+Math.sin(v.angle)*2.2,z:v.z+Math.sin(v.angle)*1.45+Math.cos(v.angle)*2.2};
  // The platform/stairs are abstracted for rail. Bus boarding is shown on the pavement.
  if(t.mode==='bus'){p.position.x=t.origin.x+(door.x-t.origin.x)*pose.boarding;p.position.z=t.origin.z+(door.z-t.origin.z)*pose.boarding;p.rotation.y=Math.atan2(door.x-t.origin.x,door.z-t.origin.z);p.position.y=groundHeight(t.origin.x,t.origin.z)-.05;}p.visible=pose.visible;
 }
}
