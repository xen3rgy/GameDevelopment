import {attachStreetProps,resetStreetArms,animateStreetIdle} from './street-idle.js?v=0.8.1';
import {availableVehicles} from './fleet.js?v=0.8.1';
import {vehicleBody} from './traffic.js?v=0.8.1';
import * as THREE from './vendor/three.module.js';
import {createCitizen} from './art.js?v=0.8.1';
import {animateCitizen} from './animation.js?v=0.8.1';
import {groundHeight} from './spatial.js?v=0.8.1';
import {PedestrianLife} from './pedestrian-life.js?v=0.8.1';
import {PEOPLE} from './data.js?v=0.8.1';

import {citizenAppearance} from './citizen.js?v=0.8.1';
import {districtAppearance,DISTRICT_POPULATION} from './district-identity.js?v=0.8.1';
export {citizenAppearance};

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
 constructor(world,kit,options={}){
  const district=options.portals?.[0]?.district;
  if(options.count===4&&DISTRICT_POPULATION[district])options={...options,count:DISTRICT_POPULATION[district]};
  this.world=world;this.root=new THREE.Group();this.root.name='Residents of Lindenstadt';world.scene.add(this.root);
  this.life=new PedestrianLife((x,z,r)=>world.canWalkExterior(x,z,r),{minute:world.model.s.minute,bystanders:PEOPLE,...options});
  for(const p of this.life.people){const id=p.id+(options.appearanceOffset||0),look=districtAppearance(citizenAppearance(id),p.residence,id),m=createCitizen(kit,look.color,look.skin,p.id+1,look);m.scale.set(p.height*p.width,p.height,p.height);m.visible=p.visible;m.userData.citizenId=p.id;p.mesh=m;attachStreetProps(m,kit);this.root.add(m);}
 }
 update(state,dt,position){
  const bystanders=[...PEOPLE.map(p=>({x:p.x,z:p.z})),...(!state.inside&&!state.riding?[{...position,radius:.4}]:[])];
  const owned=availableVehicles(state).map(v=>({...vehicleBody(v),speed:Math.abs(v.speed||0)}));
  this.life.update(dt,state.minute,{speed:state.settings.speed,bystanders,cars:[...this.world.traffic.cars,...owned]});
  this.root.visible=!state.inside;
  for(const p of this.life.people){
   const m=p.mesh,ground=['enter','exit'].includes(p.phase)?groundHeight(p.portal.x,p.portal.z):groundHeight(p.x,p.z);m.visible=p.visible;m.position.set(p.x,ground-.05*p.height,p.z);m.rotation.y=p.yaw;
   if(!p.visible)continue;
   m.userData.details.visible=Math.hypot(p.x-position.x,p.z-position.z)<18;
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
