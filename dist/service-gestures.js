import * as THREE from './vendor/three.module.js';
import {createCitizen} from './art.js?v=0.8.1';
import {animateCitizen,vehicleLocalPoint} from './animation.js?v=0.8.1';
import {reachCafeHand} from './cafe-food.js?v=0.8.1';
import {serviceBay} from './service-layout.js?v=0.8.1';
const rubber=new THREE.MeshStandardMaterial({color:0x202b2b,roughness:.95});
const smooth=t=>{t=Math.max(0,Math.min(1,t));return t*t*(3-2*t);};
export function serviceHose(points){return new THREE.Mesh(new THREE.TubeGeometry(new THREE.CatmullRomCurve3(points),20,.035,6,false),rubber);}
export function serviceNozzle({box}){
 const g=new THREE.Group();
 box(g,0,0,0,.10,.19,.12,0x315f57);box(g,0,-.10,-.07,.07,.17,.07,0x202b2b);
 box(g,0,0,.14,.055,.055,.24,0xa9b6b2);box(g,0,-.07,.055,.035,.09,.025,0xabb4ac);
 return g;
}
export class ServiceGestures{
 constructor(root,kit,docked){
  this.docked=docked;this.mechanic=createCitizen(kit,0x315f57,0xc09173,2,{trousers:0x303f43,hair:'cap',accent:0xd1b677});root.add(this.mechanic);
  this.home=new THREE.Vector3(-37.4,.30,-100.8);this.mechanic.position.copy(this.home);this.mechanic.rotation.y=0;
  this.tool=new THREE.Group();kit.box(this.tool,0,-.29,.04,.045,.24,.045,0xaab7b5);kit.box(this.tool,0,-.42,.04,.13,.05,.05,0xaab7b5);
  this.mechanic.userData.elbows[1].add(this.tool);this.tool.visible=false;
  this.nozzle=serviceNozzle(kit);root.add(this.nozzle);this.nozzle.visible=false;
  // Reuse a short chain of cylinders; no per-frame hose geometry allocation.
  this.hose=new THREE.Group();root.add(this.hose);const geo=new THREE.CylinderGeometry(.035,.035,1,6);
  for(let i=0;i<20;i++){const m=new THREE.Mesh(geo,rubber);this.hose.add(m);}this.hose.visible=false;
  this.up=new THREE.Vector3(0,1,0);this.a=new THREE.Vector3();this.b=new THREE.Vector3();this.direction=new THREE.Vector3();
 }
 resetPose(player){
  if(!this.playerPose)return;
  player.rotation.y=this.playerAngle;
  for(const joint of [...player.userData.arms,...player.userData.elbows])joint.rotation.set(0,0,0);
  this.playerPose=false;
 }
 updateMechanic(s,action,v,dt){
  const mechanic=this.mechanic,repair=v&&action.kind==='repair';
  const side=v&&Math.cos(v.angle)>=0?-1:1;
  const at=repair?vehicleLocalPoint(v,side*1.5,-side*.8):this.home;
  const dx=at.x-mechanic.position.x,dz=at.z-mechanic.position.z,distance=Math.hypot(dx,dz),step=distance>.025?Math.min(distance,dt*1.65):0;
  let angle=mechanic.rotation.y;
  if(distance>.025){
   mechanic.position.x+=dx/distance*step;mechanic.position.z+=dz/distance*step;angle=Math.atan2(dx,dz);
  }else if(repair)angle=Math.atan2(v.x-mechanic.position.x,v.z-mechanic.position.z);
  else if(!s.inside&&Math.hypot(s.position.x-mechanic.position.x,s.position.z-mechanic.position.z)<10)angle=Math.atan2(s.position.x-mechanic.position.x,s.position.z-mechanic.position.z);
  else angle=0;
  mechanic.rotation.y+=Math.atan2(Math.sin(angle-mechanic.rotation.y),Math.cos(angle-mechanic.rotation.y))*(1-Math.exp(-dt*9));
  // One gait update with actual distance and real dt, on both outbound and return walks.
  animateCitizen(mechanic,dt,step,{npc:true});
  if(repair&&distance<.10){
   const d=mechanic.userData,pulse=Math.sin(action.elapsed*11)*.09;
   d.upper.rotation.x=.30;d.arms[0].rotation.x=-.8;d.elbows[0].rotation.x=-.55;
   d.arms[1].rotation.x=-.9+pulse;d.elbows[1].rotation.x=-.55-pulse;this.tool.visible=true;
  }
 }
 update(s,player,dt){
  this.nozzle.visible=this.hose.visible=this.tool.visible=false;
  for(const dock of this.docked.values())dock.visible=true;
  const live=!s.inside&&s.vehicleService,liveVehicle=live&&(s.fleet||[]).find(v=>v.uid===live.uid);
  this.updateMechanic(s,live,liveVehicle,dt);
  if(live?.kind==='fuel'&&liveVehicle){this.lastFuel={action:live,v:liveVehicle};this.finish=null;}
  else if(this.lastFuel){
   // endService sets elapsed to duration before clearing the action. Cancellation never does.
   if(this.lastFuel.action.elapsed>=this.lastFuel.action.duration)this.finish={...this.lastFuel,age:0};
   this.lastFuel=null;
  }
  if(this.finish&&(live||s.inside||s.riding||s.transit||Math.hypot(s.position.x-this.finish.action.origin.x,s.position.z-this.finish.action.origin.z)>.1))this.finish=null;
  if(this.finish){this.finish.age+=dt;if(this.finish.age>=1.25)this.finish=null;}
  const action=live?.kind==='fuel'?live:this.finish?.action,v=live?.kind==='fuel'?liveVehicle:this.finish?.v;
  if(!action||!v||!player)return;
  const returning=this.finish?smooth(this.finish.age/1.25):0;
  // Hold at the filler for every active frame, including the last fraction of a litre.
  const weight=smooth(action.elapsed/.8)*(1-returning);
  const bay=serviceBay(action.bay);if(!bay)return;
  const side=Math.cos(v.angle)>=0?-1:1,at=vehicleLocalPoint(v,side*1.43,-.74);
  // Only the rendered actor moves. The saved position/origin used by cancellation stays untouched.
  this.playerAngle=player.rotation.y;this.playerPose=true;
  player.position.x=s.position.x+(at.x-s.position.x)*weight;player.position.z=s.position.z+(at.z-s.position.z)*weight;
  const angle=v.angle-side*Math.PI/2;player.rotation.y+=Math.atan2(Math.sin(angle-player.rotation.y),Math.cos(angle-player.rotation.y))*weight;
  player.userData.arms[0].rotation.x=-.3*weight;
  const filler=vehicleLocalPoint(v,side*1.03,-1),dockPoint=new THREE.Vector3(bay.x-.55,1.86,bay.z-.45);
  const target=new THREE.Vector3(filler.x,1.28,filler.z).lerp(dockPoint,returning);
  reachCafeHand(player,target,this.finish?1:weight);
  player.updateMatrixWorld(true);
  const hand=player.userData.elbows[1].localToWorld(new THREE.Vector3(0,-.22,.026));
  this.nozzle.position.copy(hand);if(this.finish)this.nozzle.position.lerp(dockPoint,smooth((returning-.75)/.25));this.nozzle.rotation.y=angle*(1-returning);this.nozzle.visible=this.hose.visible=true;
  this.docked.get(bay.id).visible=false;
  const start=new THREE.Vector3(bay.x-.64,2.22,bay.z),mid=start.clone().lerp(this.nozzle.position,.5);mid.y=.55;
  const curve=new THREE.QuadraticBezierCurve3(start,mid,this.nozzle.position);
  for(let i=0;i<this.hose.children.length;i++){
   curve.getPoint(i/20,this.a);curve.getPoint((i+1)/20,this.b);const m=this.hose.children[i];
   this.direction.subVectors(this.b,this.a);m.position.copy(this.a).add(this.b).multiplyScalar(.5);m.scale.y=this.direction.length();m.quaternion.setFromUnitVectors(this.up,this.direction.normalize());
  }
 }
}
