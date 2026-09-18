import {crossingObstacles} from './street-layout.js?v=0.7.2-cornerfix1';
import {ROAD_X} from './city-layout.js?v=0.7.2';
// Deterministic lane routes and braking, independent of rendering and game time speed.
const clamp=(v,a,b)=>Math.max(a,Math.min(b,v));
const distance=(a,b)=>Math.hypot(a.x-b.x,a.z-b.z);
export function makeRoute(corners,lane=2.8){
 const points=[];
 for(let i=0;i<corners.length;i++){
  const p=corners[(i+corners.length-1)%corners.length],v=corners[i],n=corners[(i+1)%corners.length];
  const a=distance(p,v),b=distance(v,n),incoming={x:(v.x-p.x)/a,z:(v.z-p.z)/a},outgoing={x:(n.x-v.x)/b,z:(n.z-v.z)/b};
  const corner={x:v.x-lane*(incoming.z+outgoing.z),z:v.z+lane*(incoming.x+outgoing.x)},radius=6;
  const start={x:corner.x-incoming.x*radius,z:corner.z-incoming.z*radius},end={x:corner.x+outgoing.x*radius,z:corner.z+outgoing.z*radius};
  for(let j=0;j<=16;j++){const t=j/16,u=1-t;points.push({x:u*u*start.x+2*u*t*corner.x+t*t*end.x,z:u*u*start.z+2*u*t*corner.z+t*t*end.z,angle:Math.atan2(u*incoming.x+t*outgoing.x,u*incoming.z+t*outgoing.z)})}
 }
 let length=0;const segments=points.map((p,i)=>{const q=points[(i+1)%points.length],len=distance(p,q),s={a:p,b:q,start:length,length:len};length+=len;return s});
 return {segments,length};
}
export function routePose(route,progress){
 const d=((progress%route.length)+route.length)%route.length,s=route.segments.find(s=>d<=s.start+s.length)||route.segments.at(-1),t=clamp((d-s.start)/s.length,0,1);
 const turn=Math.atan2(Math.sin(s.b.angle-s.a.angle),Math.cos(s.b.angle-s.a.angle));
 return {x:s.a.x+(s.b.x-s.a.x)*t,z:s.a.z+(s.b.z-s.a.z)*t,angle:s.a.angle+turn*t};
}
const rect=(x1,x2,z1,z2)=>[{x:x1,z:z1},{x:x2,z:z1},{x:x2,z:z2},{x:x1,z:z2}];
export const TRAFFIC_ROUTES=[rect(-110,110,-65,65),rect(-110,0,-65,0),rect(0,110,0,65),rect(-110,110,0,65).reverse(),rect(0,110,-65,0).reverse(),rect(-110,0,0,65),rect(-110,110,-65,0).reverse(),rect(-202,-110,-65,0),rect(-202,-110,0,65).reverse()].map(p=>makeRoute(p));
const bodyAxes=p=>[{x:Math.sin(p.angle||0),z:Math.cos(p.angle||0)},{x:Math.cos(p.angle||0),z:-Math.sin(p.angle||0)}];
const projectedRadius=(p,ownAxes,axis)=>Math.abs(axis.x*ownAxes[0].x+axis.z*ownAxes[0].z)*(p.length||1)/2+Math.abs(axis.x*ownAxes[1].x+axis.z*ownAxes[1].z)*(p.width||1)/2;
export function overlapDepth(a,b,padding=0){
 const aa=bodyAxes(a),bb=bodyAxes(b),dx=b.x-a.x,dz=b.z-a.z;let depth=Infinity;
 for(const axis of [...aa,...bb]){const penetration=projectedRadius(a,aa,axis)+projectedRadius(b,bb,axis)+padding-Math.abs(dx*axis.x+dz*axis.z);if(penetration<0)return null;depth=Math.min(depth,penetration)}
 return depth;
}
export function overlaps(a,b,padding=0){return overlapDepth(a,b,padding)!==null}
export function vehicleBody(vehicle,x=vehicle?.x??0,z=vehicle?.z??0,angle=vehicle?.angle??Math.PI){
 const id=vehicle?.id,length=id==='van'?4.8:id==='sport'?4.1:id==='bike'?1.8:3.8,width=id==='bike'?.65:1.85;
 return {x,z,angle,length,width};
}
// Pedestrians use their body radius, not an oversized square around the car.
export function pedestrianBlocks(car,p,time=0){
 const hit=(x,z)=>{const dx=x-car.x,dz=z-car.z,forward=dx*Math.sin(car.angle)+dz*Math.cos(car.angle),side=dx*Math.cos(car.angle)-dz*Math.sin(car.angle);
  return Math.hypot(Math.max(0,Math.abs(forward)-car.length/2),Math.max(0,Math.abs(side)-car.width/2))<.38;
 };
 return hit(p.x,p.z)||hit(p.x+(p.vx||0)*Math.min(time,1.2),p.z+(p.vz||0)*Math.min(time,1.2));
}
export function parkedVehicleBlocks(vehicle,x,z,r=.35,from=null){
 if(!vehicle)return false;
 const body={...vehicle,angle:vehicle.angle??Math.PI,length:vehicle.id==='van'?4.8:vehicle.id==='bike'?1.8:3.8,width:vehicle.id==='bike'?.65:1.85};
 const hit=(x,z)=>overlaps({x,z,angle:0,length:r*2,width:r*2},body,.03);
 if(!hit(x,z))return false;
 // Older saves could place a walking player inside their parked vehicle: allow escape.
 if(from&&hit(from.x,from.z)&&Math.hypot(x-vehicle.x,z-vehicle.z)>Math.hypot(from.x-vehicle.x,from.z-vehicle.z)+1e-7)return false;
 return true;
}
export class Traffic {
 constructor(count=9){this.time=0;this.junctions=[];for(const x of ROAD_X)for(const z of [-65,0,65])this.junctions.push({x,z,owner:null});this.cars=Array.from({length:count},(_,i)=>{const route=TRAFFIC_ROUTES[i%TRAFFIC_ROUTES.length],progress=route.length*(.12+i*.137)%route.length;return {id:i,route,progress,...routePose(route,progress),speed:0,maxSpeed:5.2+(i%4)*.5,length:i===4?4.8:3.8,width:1.85,braking:false}})}
 update(dt,pedestrians=[],owned=null){
  let remaining=clamp(dt,0,.2);while(remaining>1e-8){const step=Math.min(remaining,1/30);this.tick(step,pedestrians,owned);remaining-=step}
 }
 tick(dt,pedestrians,owned){
  this.time+=dt;const snapshot=this.cars.map(c=>({...c}));
  for(const junction of this.junctions){
   const owner=snapshot.find(c=>c.id===junction.owner);if(owner&&distance(owner,junction)>18)junction.owner=null;
   if(junction.owner==null){const waiting=snapshot.filter(c=>distance(c,junction)<18).sort((a,b)=>distance(a,junction)-distance(b,junction)||a.id-b.id);if(waiting.length)junction.owner=waiting[0].id}
  }
  // Yield at the near lane only when someone is actually approaching the crossing.
  const crossings=crossingObstacles(pedestrians);
  for(const car of this.cars){
   // Let a car already over a crossing clear it; approaching people stop the next car.
   // Actual body collisions are still checked independently below.
   const obstacles=[...snapshot.filter(c=>c.id!==car.id),...crossings.filter(c=>!overlaps(car,c,.25)),...(owned?[owned]:[])].filter(o=>distance(o,car)<38),people=pedestrians.filter(p=>distance(p,car)<24);
   const look=4+car.speed*car.speed/8.4+car.speed*.4;
   const closed=this.junctions.filter(j=>j.owner!=null&&j.owner!==car.id&&distance(car,j)<38);
   let gap=look;for(let d=0;d<=look;d+=.5){const pose={...routePose(car.route,car.progress+d),length:car.length,width:car.width};if(obstacles.some(o=>overlaps(pose,o,.25))||people.some(p=>pedestrianBlocks(pose,p,d/Math.max(1,car.speed)))||closed.some(j=>Math.abs(pose.x-j.x)<12&&Math.abs(pose.z-j.z)<12)){gap=Math.max(0,d-.5);break}}
   const desired=Math.min(car.maxSpeed,Math.sqrt(8.4*Math.max(0,gap-1.1)));
   car.braking=desired<car.speed-.15||gap<1.8;
   car.speed=desired<car.speed?Math.max(desired,car.speed-dt*4.2):Math.min(desired,car.speed+dt*1.8);
   let travel=Math.min(car.speed*dt,Math.max(0,gap-.15));if(!travel&&gap<.5)car.speed=0;
   // The coarse braking lookahead can miss a shallow contact while turning.
   // Validate the actual next body pose before committing a physical move.
   const next={...routePose(car.route,car.progress+travel),length:car.length,width:car.width};
   if(people.some(p=>pedestrianBlocks(next,p))){travel=0;car.speed=0;car.braking=true;}
   car.progress=(car.progress+travel)%car.route.length;Object.assign(car,routePose(car.route,car.progress));
  }
 }
 blocks(x,z,r=.35){return this.cars.some(c=>overlaps({x,z,angle:0,length:r*2,width:r*2},c,.05))}
 blocksVehicle(body,from=null,padding=.015){
  return this.cars.some(c=>{
   const next=overlapDepth(body,c,padding);if(next===null)return false;
   const current=from&&overlapDepth(from,c,padding);
   if(current!==null&&next<current-1e-5)return false;
   return true;
  });
 }
}
