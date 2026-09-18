import {CAFE_GESTURE_DURATION,applyCafeWorkerPose} from './cafe-gestures.js?v=0.7.2';
const blend=(a,b,t)=>a+(b-a)*t;
export const NPC_STRIDE=1.18;
// A planted foot moves backwards at exactly the body's forward speed.
export function npcGaitPose(phase,weight){
 return [0,.5].map(offset=>{
  const t=((phase/(Math.PI*2)+offset)%1+1)%1,stance=.6,span=NPC_STRIDE*stance;
  const u=(t-stance)/(1-stance),z=(t<stance?span/2-NPC_STRIDE*t:-span/2+span*(u*u*(3-2*u)))*weight;
  const lift=t<stance?0:Math.sin(Math.PI*u)**2*.085*weight,y=.9-.09*weight,down=y-.115-lift;
  const a=.38,b=.405,r=Math.min(a+b,Math.hypot(z,down)),acos=v=>Math.acos(Math.max(-1,Math.min(1,v)));
  const knee=Math.PI-acos((a*a+b*b-r*r)/(2*a*b)),hip=-Math.atan2(z,down)-acos((a*a+r*r-b*b)/(2*a*r));
  return {hip,knee,y,lift};
 });
}
export function gaitPose(phase,weight,running=false,carrying=false){
 const amplitude=running?.65:carrying?.33:.43;
 return [0,Math.PI].map(offset=>{const swing=Math.sin(phase+offset),hip=-swing*amplitude*weight,knee=Math.max(0,swing)*.65*weight;
  return {hip,knee,lift:Math.max(0,swing)*.1*weight,y:.9-.38*(1-Math.cos(hip))-.405*(1-Math.cos(hip+knee))+Math.max(0,swing)*.1*weight};
 });
}
export function playGesture(actor,kind){actor.userData.gesture={kind,age:0,duration:CAFE_GESTURE_DURATION[kind]||(kind==='shop'?.75:.6)}}
export function animateCitizen(actor,dt,distance,{running=false,carrying=false,basket=false,riding=false,entering=false,grounded=true,npc=false,backwards=false}={}){
 const data=actor.userData,a=data.animation||(data.animation={phase:0,weight:0,time:0});a.time+=dt;
 a.phase+=(backwards?-1:1)*Math.max(0,distance)/(npc?NPC_STRIDE:running?3.4:carrying?1.75:2.3)*Math.PI*2;
 a.weight=blend(a.weight,distance>1e-5&&grounded?1:0,1-Math.exp(-dt*14));
 const pose=npc?npcGaitPose(a.phase,a.weight):gaitPose(a.phase,a.weight,running,carrying);
 let reach=0,pick=0,cafeGesture=null;if(data.gesture){const g=data.gesture;g.age+=dt;const strength=Math.sin(Math.PI*Math.min(1,g.age/g.duration));if(CAFE_GESTURE_DURATION[g.kind])cafeGesture={kind:g.kind,weight:strength,phase:g.age/g.duration};else if(g.kind==='shop')reach=strength;else pick=strength;if(g.age>=g.duration)data.gesture=null}
 for(let i=0;i<2;i++){const leg=data.legs[i],p=pose[i];if(pick){p.hip-=pick*.4;p.knee+=pick*.7;p.y=.9-.38*(1-Math.cos(p.hip))-.405*(1-Math.cos(p.hip+p.knee))+p.lift}leg.rotation.x=p.hip;leg.position.y=p.y;leg.rotation.z=0;if(data.knees)data.knees[i].rotation.x=p.knee;if(data.feet)data.feet[i].rotation.x=-p.hip-p.knee;
  const arm=data.arms[i],swing=Math.sin(a.phase+i*Math.PI)*.28*a.weight;
  const target=carrying?-.95:riding?-1.02:entering?-.65:reach&&i===1?-reach*1.35:basket&&i===0?-.12:swing-pick*.65;
  arm.rotation.x=blend(arm.rotation.x,target,1-Math.exp(-dt*18));arm.rotation.z=carrying?(i===0?-.1:.1):basket&&i===0?-.16:0;
  if(data.elbows)data.elbows[i].rotation.x=carrying?-.55:riding?-.25:reach&&i===1?-reach*.35:running?-.65*a.weight:-.12*a.weight;
 }
 if(data.upper){data.upper.rotation.x=(running?.10:.025)*a.weight+pick*.65+(entering?.23:0);data.upper.rotation.z=Math.sin(a.phase)*.022*a.weight;data.upper.position.y=.9-(npc?.09*a.weight:0)+Math.sin(a.time*1.8)*.006*(1-a.weight)-pick*.04}
 if(cafeGesture&&!carrying&&!riding)applyCafeWorkerPose(actor,cafeGesture.kind,cafeGesture.weight,cafeGesture.phase);
 if(riding){data.legs.forEach((leg,i)=>{leg.rotation.x=-.7+(distance?Math.sin(a.phase+i*Math.PI)*.45:0);leg.position.y=.9;if(data.knees)data.knees[i].rotation.x=1.15})}
}
export function vehicleLocalPoint(vehicle,lateral=0,forward=0){
 const angle=vehicle?.angle??Math.PI,c=Math.cos(angle),s=Math.sin(angle);
 return {x:(vehicle?.x??0)+c*lateral+s*forward,z:(vehicle?.z??0)-s*lateral+c*forward};
}
export const vehicleDriverDoorPoint=(vehicle,clearance=1.46,forward=.16)=>vehicleLocalPoint(vehicle,clearance,forward);
export const vehicleDriverSeatPoint=vehicle=>vehicleLocalPoint(vehicle,.42,.18);
export class VehicleTransition {
 constructor(from,to,entering,angle,via=null){this.from={...from};this.to={...to};this.via=via?{...via}:null;this.entering=entering;this.angle=angle;this.age=0;this.duration=1.05}
 update(dt){
  this.age=Math.min(this.duration,this.age+dt);const t=this.age/this.duration,u=Math.max(0,Math.min(1,(t-.08)/.84)),ease=u*u*(3-2*u);
  let x=blend(this.from.x,this.to.x,ease),z=blend(this.from.z,this.to.z,ease);
  if(this.via){const split=this.entering?.42:.58;if(ease<split){const q=ease/split,k=q*q*(3-2*q);x=blend(this.from.x,this.via.x,k);z=blend(this.from.z,this.via.z,k)}else{const q=(ease-split)/(1-split),k=q*q*(3-2*q);x=blend(this.via.x,this.to.x,k);z=blend(this.via.z,this.to.z,k)}}
  const doorT=Math.max(0,Math.min(1,(t-.03)/.94));
  return {x,z,angle:this.angle,door:Math.sin(Math.PI*doorT)*1.02,visible:this.entering?t<.86:t>.10,done:t>=1};
 }
}
