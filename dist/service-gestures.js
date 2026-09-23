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
  this.mechanic.position.set(-38,.30,-102);this.mechanic.rotation.y=Math.PI;
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
 update(s,player,dt){
  this.nozzle.visible=this.hose.visible=this.tool.visible=false;
  for(const dock of this.docked.values())dock.visible=true;
  const action=!s.inside&&s.vehicleService,v=action&&(s.fleet||[]).find(v=>v.uid===action.uid);
  const mechanic=this.mechanic;
  // State elapsed drives work so pause, reload, completion and cancellation agree.
  animateCitizen(mechanic,dt,0,{npc:true});
  if(!v||action.kind!=='repair'){
   const home=new THREE.Vector3(-38,.30,-102),distance=mechanic.position.distanceTo(home),step=Math.min(distance,dt*2.5);
   if(distance>.02){mechanic.rotation.y=Math.atan2(home.x-mechanic.position.x,home.z-mechanic.position.z);mechanic.position.lerp(home,distance?step/distance:1);animateCitizen(mechanic,0,step,{npc:true});}
  }
  if(!v)return;
  const t=action.elapsed,remaining=action.duration-t,weight=smooth(t/.8)*smooth(remaining/.65);
  if(action.kind==='repair'){
   const front=(v.id==='van'?2.4:v.id==='sport'?2.05:1.9),at=vehicleLocalPoint(v,0,front+.48);
   const travel=smooth(t/.85),old=mechanic.position.clone();
   mechanic.position.set(-38+(at.x+38)*travel,.30,-102+(at.z+102)*travel);
   mechanic.rotation.y=travel<1?Math.atan2(at.x+38,at.z+102):v.angle+Math.PI;
   if(travel<1)animateCitizen(mechanic,0,old.distanceTo(mechanic.position),{npc:true});
   else{
    const d=mechanic.userData,pulse=Math.sin(t*11)*.09;
    d.upper.rotation.x=.38*weight;d.arms[0].rotation.x=-.8*weight;d.elbows[0].rotation.x=-.55*weight;
    d.arms[1].rotation.x=(-.9+pulse)*weight;d.elbows[1].rotation.x=(-.55-pulse)*weight;this.tool.visible=true;
   }
   return;
  }
  if(action.kind!=='fuel'||!player)return;
  const bay=serviceBay(action.bay);if(!bay)return;
  const side=Math.cos(v.angle)>=0?-1:1,at=vehicleLocalPoint(v,side*1.43,-.74);
  // Only the rendered actor moves. The saved position/origin used by cancellation stays untouched.
  this.playerAngle=player.rotation.y;this.playerPose=true;
  player.position.x=s.position.x+(at.x-s.position.x)*weight;player.position.z=s.position.z+(at.z-s.position.z)*weight;
  const angle=v.angle-side*Math.PI/2;player.rotation.y+=Math.atan2(Math.sin(angle-player.rotation.y),Math.cos(angle-player.rotation.y))*weight;
  player.userData.arms[0].rotation.x=-.3*weight;
  const filler=vehicleLocalPoint(v,side*1.03,-1),target=new THREE.Vector3(filler.x,1.28,filler.z);
  reachCafeHand(player,target,weight);
  player.updateMatrixWorld(true);
  const hand=player.userData.elbows[1].localToWorld(new THREE.Vector3(0,-.22,.026));
  this.nozzle.position.copy(hand);this.nozzle.rotation.y=angle;this.nozzle.visible=this.hose.visible=true;
  this.docked.get(bay.id).visible=false;
  const start=new THREE.Vector3(bay.x-.64,2.22,bay.z),mid=start.clone().lerp(hand,.5);mid.y=.55;
  const curve=new THREE.QuadraticBezierCurve3(start,mid,hand);
  for(let i=0;i<this.hose.children.length;i++){
   curve.getPoint(i/20,this.a);curve.getPoint((i+1)/20,this.b);const m=this.hose.children[i];
   this.direction.subVectors(this.b,this.a);m.position.copy(this.a).add(this.b).multiplyScalar(.5);m.scale.y=this.direction.length();m.quaternion.setFromUnitVectors(this.up,this.direction.normalize());
  }
 }
}
