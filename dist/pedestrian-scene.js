import {attachStreetProps,resetStreetArms,animateStreetIdle} from './street-idle.js?v=0.7.2';
import * as THREE from './vendor/three.module.js';
import {createCitizen} from './art.js?v=0.7.2';
import {animateCitizen} from './animation.js?v=0.7.2';
import {groundHeight} from './spatial.js?v=0.7.2';
import {PedestrianLife} from './pedestrian-life.js?v=0.7.2-bollardfix1';
import {PEOPLE} from './data.js?v=0.7.2';

export function citizenAppearance(id){return {
 color:[0x496e79,0x936347,0x5f7053,0x344955,0xa08e74,0x754f56,0x747d85,0x8c814e][id%8],
 skin:[0xc39b7a,0x947158,0xd6b496,0x705043,0xb17e5d][id%5],
 trousers:[0x3a4651,0x5a5148,0x343d3e,0x657278,0x555953][id%5],
 hairColor:[0x312b27,0x6a4933,0x9c8966,0x42403a,0x858580][Math.floor(id/3)%5],
 hair:['short','long','bun','short','cap'][id%5],accent:[0xa78e63,0x566f6a,0x87564a,0x4e677b][id%4],
 shoes:id%3?0x303c40:0x8d8270,coat:id%4===2,bag:id%6===3
};}

export function streetSeatLegPose(height,s,weight){
 const endHip=.235+height/s,hip=.9+(endHip-.9)*weight,down=hip-.115;
 // Solve feet back to the floor even when the person's scale changes.
 const endZ=Math.min(.39/s,Math.sqrt(Math.max(0,.778**2-(endHip-.115)**2))*.98),z=endZ-.35/s*(1-weight),a=.38,b=.405,r=Math.min(.784,Math.hypot(z,down)),acos=v=>Math.acos(Math.max(-1,Math.min(1,v)));
 const knee=Math.PI-acos((a*a+b*b-r*r)/(2*a*b)),angle=-Math.atan2(z,down)-acos((a*a+r*r-b*b)/(2*a*r));
 return {hip,knee,angle};
}
export function seatedStreetPose(actor,weight,height){
 const d=actor.userData,{hip,knee,angle}=streetSeatLegPose(height,actor.scale.y,weight);
 for(let i=0;i<2;i++){
  const leg=d.legs[i];leg.position.y=hip;leg.rotation.x=angle;leg.rotation.z=0;d.knees[i].rotation.x=knee;d.feet[i].rotation.x=-(angle+knee);
  d.arms[i].rotation.x=-.43*weight;d.arms[i].rotation.z=(i===0?-.06:.06)*weight;d.elbows[i].rotation.x=-.68*weight;
 }
 d.upper.position.y=hip;d.upper.rotation.x=.035*weight;d.upper.rotation.z=0;
}

export class PedestrianScene{
 constructor(world,kit){
  this.world=world;this.root=new THREE.Group();this.root.name='Residents of Lindenstadt';world.scene.add(this.root);
  this.life=new PedestrianLife((x,z,r)=>world.canWalkExterior(x,z,r),{minute:world.model.s.minute,bystanders:PEOPLE});
  for(const p of this.life.people){const look=citizenAppearance(p.id),m=createCitizen(kit,look.color,look.skin,p.id+1,look);m.scale.set(p.height*p.width,p.height,p.height);m.visible=p.visible;m.userData.citizenId=p.id;p.mesh=m;attachStreetProps(m,kit);this.root.add(m);}
 }
 update(state,dt,position){
  const bystanders=[...PEOPLE.map(p=>({x:p.x,z:p.z})),...(!state.inside&&!state.riding?[{...position,radius:.4}]:[])];
  const owned=state.vehicle?{...state.vehicle,length:state.vehicle.id==='van'?4.8:state.vehicle.id==='bike'?1.8:3.8,width:state.vehicle.id==='bike'?.65:1.85,speed:Math.abs(state.vehicle.speed||0),angle:state.vehicle.angle??Math.PI}:null;
  this.life.update(dt,state.minute,{speed:state.settings.speed,bystanders,cars:[...this.world.traffic.cars,...(owned?[owned]:[])]});
  this.root.visible=!state.inside;
  for(const p of this.life.people){
   const m=p.mesh,ground=['enter','exit'].includes(p.phase)?groundHeight(p.portal.x,p.portal.z):groundHeight(p.x,p.z);m.visible=p.visible;m.position.set(p.x,ground-.05*p.height,p.z);m.rotation.y=p.yaw;
   if(!p.visible)continue;
   if(!(dt>0)&&m.userData.animation)continue;
   if(!state.inside&&Math.hypot(p.x-position.x,p.z-position.z)<80){
    resetStreetArms(m);
    animateCitizen(m,dt,p.distance/p.height,{npc:true,backwards:p.backwards});
    if(p.sit>0)seatedStreetPose(m,p.sit,p.goal.seat.height);
    const idle=['seated','wait'].includes(p.phase),data=m.userData;
    if(data.headRoot)data.headRoot.rotation.y=idle?Math.sin(this.life.time*.4+p.id)*.16:Math.sin(this.life.time*.65+p.id)*.025;
    if(idle){data.upper.rotation.y=Math.sin(this.life.time*.33+p.id)*.025;}
    else data.upper.rotation.y=0;
    if(data.style?.bag&&!p.sit){data.arms[0].rotation.x*=.25;data.elbows[0].rotation.x=-.12;}
    animateStreetIdle(m,p,state.settings.speed);
   }
  }
 }
}
