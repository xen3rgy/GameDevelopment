import * as THREE from './vendor/three.module.js';
import {reachCafeHand} from './cafe-food.js?v=0.8.0';
const smooth=t=>{t=Math.max(0,Math.min(1,t));return t*t*(3-2*t);};
export function workMotion(task){const p=task.elapsed/task.duration;return {progress:p,weight:smooth(task.age/.5)*smooth((1-p)/.12),turn:Math.sin(task.age*6)};}
export function applyWorkshopPose(player,task,current,point){
 const d=player.userData,m=workMotion(task),bike=current.point==='workshopBike';
 const low=bike&&['inspect','wrench','pump'].includes(current.gesture)?m.weight:0;
 // Lower the body with planted feet, using the same two-bone proportions as walking.
 for(let i=0;i<2;i++){
  const height=.9-.29*low,ankle=.115,z=(i===0?.10:-.08)*low,a=.38,b=.405,r=Math.min(a+b,Math.hypot(z,height-ankle));
  const acos=x=>Math.acos(Math.max(-1,Math.min(1,x))),knee=Math.PI-acos((a*a+b*b-r*r)/(2*a*b)),hip=-Math.atan2(z,height-ankle)-acos((a*a+r*r-b*b)/(2*a*r));
  d.legs[i].position.y=height;d.legs[i].rotation.x=hip;d.knees[i].rotation.x=knee;d.feet[i].rotation.x=-hip-knee;
 }
 d.upper.position.y=.9-.29*low;d.upper.rotation.x=(bike?.5:.15)*m.weight;
 d.arms[0].rotation.x=-.8*m.weight;d.elbows[0].rotation.x=-.5*m.weight;
 if(bike){
  const grip=new THREE.Vector3(point.x-.2,.93+.035*m.turn,point.z-.61);
  reachCafeHand(player,grip,m.weight);
 }else{d.arms[1].rotation.x=(-1+Math.sin(task.age*2)*.10)*m.weight;d.elbows[1].rotation.x=-.65*m.weight;}
 return m;
}
