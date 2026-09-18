import {routeLength} from './navigation.js?v=0.7.2';
import {PedestrianNavigation,PEDESTRIAN_RADIUS,pedestrianSegmentClear as segmentClear} from './pedestrian-navigation.js?v=0.7.2';
import {approach} from './movement.js?v=0.7.2';
import {PAVEMENTS,onRoad,CROSSINGS} from './street-layout.js?v=0.7.2-cornerfix1';
import {STATION_PLAZAS} from './city-layout.js?v=0.7.2';
import {pedestrianBlocks,routePose} from './traffic.js?v=0.7.2-cornerfix2';
import {CITIZEN_PORTALS,CITIZEN_DESTINATIONS,GARDEN_PATHS,citizenAwake,daytime} from './pedestrian-layout.js?v=0.7.2-cornerfix1';

const clamp=(n,a,b)=>Math.max(a,Math.min(b,n)),dist=(a,b)=>Math.hypot(a.x-b.x,a.z-b.z);
const rect=(p,x,z)=>Math.abs(x-p.x)<=p.w/2&&Math.abs(z-p.z)<=p.d/2;
const turn=(a,b,amount)=>a+clamp(Math.atan2(Math.sin(b-a),Math.cos(b-a)),-amount,amount);
const smooth=t=>{t=clamp(t,0,1);return t*t*(3-2*t);};
const random=p=>{p.seed=(Math.imul(p.seed,1664525)+1013904223)>>>0;return p.seed/4294967296;};

function surfacePoint(x,z){
 if(onRoad(x,z))return CROSSINGS.some(c=>Math.abs(c.axis==='x'?z-c.z:x-c.x)<1.45&&Math.abs(c.axis==='x'?x-c.x:z-c.z)<7);
 return PAVEMENTS.some(p=>rect(p,x,z))||STATION_PLAZAS.some(p=>rect(p,x,z))||GARDEN_PATHS.some(p=>rect(p,x,z));
}
export function pedestrianSurface(x,z,r=0){
 if(!surfacePoint(x,z))return false;
 if(!r)return true;
 return [[r,0],[-r,0],[0,r],[0,-r],[r*.707,r*.707],[-r*.707,r*.707],[r*.707,-r*.707],[-r*.707,-r*.707]].every(([dx,dz])=>surfacePoint(x+dx,z+dz));
}

// A small, persistent population. No per-frame pathfinding or visible street respawns.
export class PedestrianLife{
 constructor(canWalk,{count=32,minute=720,bystanders=[]}={}){
  this.canWalk=(x,z,r=PEDESTRIAN_RADIUS)=>canWalk(x,z,r)&&pedestrianSurface(x,z,r);
  this.nav=new PedestrianNavigation(this.canWalk);
  this.reserved=new Map();this.doors=new Map();this.time=0;this.minute=minute;
  this.destinations=CITIZEN_DESTINATIONS.filter(d=>this.canWalk(d.x,d.z,.4));
  const homes=CITIZEN_PORTALS.filter(p=>p.home);
  this.people=Array.from({length:count},(_,id)=>{
   const home=homes[id%homes.length];return {id,home,residence:home,seed:7183+id*739,height:.94+(id%7)*.019,width:.94+(id%4)*.035,maxSpeed:1.04+(id%5)*.075,
    x:home.x,z:home.doorZ,yaw:home.angle,phase:'inside',visible:false,cooldown:id*.7,age:0,sit:0,speed:0,vx:0,vz:0,intentX:0,intentZ:0,distance:0,blocked:0,wait:0,path:[],index:1,goal:null,
    buddy:id%8===0?id+1:id%8===1?id-1:null,visits:0,travelled:0};
  });
  // On first load only, distribute awake residents along valid routes, never into traffic.
  for(const p of this.people){
   if(!citizenAwake(p.id,minute)||!this.plan(p,bystanders))continue;
   const length=routeLength(p.path);let left=length*(.12+random(p)*.65),index=1;
   while(index<p.path.length-1&&left>dist(p.path[index-1],p.path[index])){left-=dist(p.path[index-1],p.path[index]);index++;}
   const a=p.path[index-1],b=p.path[index],len=dist(a,b),t=Math.min(1,left/Math.max(.001,len)),x=a.x+(b.x-a.x)*t,z=a.z+(b.z-a.z)*t;
   if(!onRoad(x,z)&&this.canWalk(x,z,PEDESTRIAN_RADIUS)&&[...this.people.filter(q=>q.visible),...bystanders].every(q=>Math.hypot(q.x-x,q.z-z)>1.1)){
    Object.assign(p,{x,z,index,phase:'walk',visible:true,yaw:Math.atan2(b.x-a.x,b.z-a.z)});
   }else{this.release(p);p.path=[];p.goal=null;}
  }
 }
 release(p){if(p.goal&&this.reserved.get(p.goal.id)===p.id)this.reserved.delete(p.goal.id);}
 route(p,goal,obstacles=[]){const path=this.nav.find(p.phase==='inside'?{x:p.home.x,z:p.home.z}:p,goal,obstacles);if(path.length<2)return false;this.release(p);p.goal=goal;p.path=path;p.index=1;p.blocked=0;p.progress=null;p.passing=null;if(goal.kind!=='home')this.reserved.set(goal.id,p.id);return true;}
 plan(p,bystanders=[],obstacles=[]){
  if(!citizenAwake(p.id,this.minute))return this.route(p,{...p.residence,kind:'home',portal:p.residence.id},obstacles);
  const buddy=this.people[p.buddy],pool=this.destinations.filter(d=>d.id!==p.goal?.id&&!this.reserved.has(d.id)&&daytime(this.minute,d.open,d.close)&&!bystanders.some(o=>dist(o,d)<.85));
  const ranked=pool.map(d=>({d,score:random(p)*100+(d.cluster===buddy?.goal?.cluster?100:0)-(dist(p,d)<5?60:0)})).sort((a,b)=>b.score-a.score);
  for(const {d} of ranked.slice(0,8))if(this.route(p,d,obstacles))return true;
  // All activity slots can be occupied. Going home releases the current slot;
  // waiting forever at a finished activity used to exhaust the entire population.
  return p.phase!=='inside'&&this.route(p,{...p.residence,kind:'home',portal:p.residence.id},obstacles);
 }
 doorFree(portal,p,obstacles){return !this.doors.has(portal.id)&&!obstacles.some(q=>q!==p&&q.visible!==false&&dist(q,portal)<.85);}
 enter(p,obstacles){
  const portal=CITIZEN_PORTALS.find(d=>d.id===p.goal?.portal);if(!portal)return false;
  if(!this.doorFree(portal,p,obstacles))return false;
  this.doors.set(portal.id,p.id);p.portal=portal;p.entryStart={x:p.x,z:p.z};p.phase='enter';p.age=0;p.speed=0;return true;
 }
 arrive(p,obstacles){
  p.speed=0;p.age=0;p.blocked=0;
  if(p.goal.kind==='home'||p.goal.kind==='visit'){this.enter(p,obstacles);return;}
  p.phase=p.goal.kind==='seat'?'sitDown':'wait';p.seatStart={x:p.x,z:p.z};p.wait=p.goal.kind==='seat'?40+random(p)*65:18+random(p)*35;
 }
 update(dt,minute,{speed=1,bystanders=[],cars=[]}={}){
  this.minute=((minute%1440)+1440)%1440;
  for(const p of this.people){p.distance=0;p.vx=0;p.vz=0;p.intentX=0;p.intentZ=0;p.backwards=false;}
  if(!(dt>0))return;
  let remaining=Math.min(dt,.2);const elapsed=remaining;
  const starts=this.people.map(p=>({x:p.x,z:p.z}));
  while(remaining>1e-8){const step=Math.min(remaining,1/30);remaining-=step;this.time+=step;
   for(const p of this.people){
    p.age+=step;const others=[...this.people.filter(q=>q!==p&&q.visible),...bystanders];
    const awake=citizenAwake(p.id,this.minute);
    if(p.phase==='inside'){
     p.cooldown=Math.max(0,p.cooldown-step*speed);
     if((!awake&&p.home.id===p.residence.id)||p.cooldown>0||!this.doorFree(p.home,p,others))continue;
     if(!this.plan(p,bystanders)){p.cooldown=5;continue;}
     p.portal=p.home;this.doors.set(p.portal.id,p.id);p.phase='exit';p.visible=true;p.age=0;p.x=p.portal.x;p.z=p.portal.doorZ;p.yaw=p.portal.angle;p.speed=0;
    }else if(p.phase==='enter'||p.phase==='exit'){
     const entering=p.phase==='enter',portal=p.portal,from=entering?p.entryStart.z:portal.doorZ,to=entering?portal.doorZ:portal.z;
     const wanted=entering?portal.angle+Math.PI:portal.angle;p.yaw=turn(p.yaw,wanted,step*2.8);
     if(entering&&Math.abs(Math.atan2(Math.sin(p.yaw-wanted),Math.cos(p.yaw-wanted)))>.1){p.age=0;continue;}
     const length=Math.abs(to-from),travel=Math.min(length,p.age*1.08),z=from+Math.sign(to-from)*travel;
     // Door passages are the only deliberate traversals through a building's collider.
     // Keep exits inside until the pavement immediately outside is clear.
     if(!entering&&Math.abs(z-portal.z)<.6&&others.some(q=>Math.hypot(q.x-portal.x,q.z-z)<.75)){p.age-=step;continue;}
     const x=entering?p.entryStart.x+(portal.x-p.entryStart.x)*travel/length:portal.x;p.distance+=Math.hypot(x-p.x,z-p.z);p.x=x;p.z=z;
     if(travel>=length){this.doors.delete(portal.id);p.age=0;
      if(entering){p.visits++;this.release(p);p.phase='inside';p.visible=false;p.home=portal;p.goal=null;p.cooldown=35+random(p)*60;p.path=[];}
      else{p.phase='walk';p.index=1;}
     }
    }else if(p.phase==='sitDown'||p.phase==='standUp'){
     const down=p.phase==='sitDown',seat=p.goal.seat;
     p.yaw=turn(p.yaw,p.goal.angle,step*3.5);
     if(down&&Math.abs(Math.atan2(Math.sin(p.yaw-p.goal.angle),Math.cos(p.yaw-p.goal.angle)))>.1){p.age=0;continue;}
     const t=down?p.age:1.3-p.age,weight=smooth((t-.6)/.7),offset=t<.6?1.12-(1.12-.35)*clamp(t/.6,0,1):.35*(1-weight);
     let x=seat.x+Math.sin(p.goal.angle)*offset,z=seat.z+Math.cos(p.goal.angle)*offset;
     if(down&&t<.6){const u=clamp(t/.6,0,1);x=p.seatStart.x+(seat.x+Math.sin(p.goal.angle)*.35-p.seatStart.x)*u;z=p.seatStart.z+(seat.z+Math.cos(p.goal.angle)*.35-p.seatStart.z)*u;}
     if(others.some(q=>Math.hypot(x-q.x,z-q.z)<.7&&Math.hypot(x-q.x,z-q.z)<dist(p,q)-1e-6)){p.age-=step;continue;}
     if(weight===0){p.distance+=Math.hypot(x-p.x,z-p.z);p.backwards=down;}
     p.sit=weight;p.x=x;p.z=z;
     if(p.age>=1.3){p.phase=down?'seated':'walk';p.age=0;if(!down&&!this.plan(p,bystanders)){p.phase='wait';p.wait=5;}}
    }else if(p.phase==='seated'||p.phase==='wait'){
     p.wait-=step*speed;p.yaw=turn(p.yaw,p.goal.angle,step*2);
     if(!awake||p.wait<=0){
      if(p.phase==='seated'){p.phase='standUp';p.age=0;}
      else if(this.plan(p,bystanders)){p.phase='walk';p.age=0;}else p.wait=5;
     }
    }else if(p.phase==='walk'){
     if(!awake&&p.goal?.kind!=='home'){this.route(p,{...p.residence,kind:'home',portal:p.residence.id});}
     this.walk(p,step,others,cars);
    }
   }
  }
  this.people.forEach((p,i)=>{p.vx=(p.x-starts[i].x)/elapsed;p.vz=(p.z-starts[i].z)/elapsed;p.travelled+=p.distance;});
 }
 walk(p,dt,others,cars){
  if(!p.path.length||!p.goal)return;
  p.waitingTraffic=false;
  // Give the yielding person room to reach their pocket instead of tailgating them.
  if(others.some(q=>q.passing?.yieldTo===p&&this.time<q.passing.until&&dist(q,q.passing)>.12)){
   p.speed=approach(p.speed,0,dt*4);if(p.progress)p.progress.age=0;return;
  }
  let target=p.path[p.index];if(!target)return;
  let last=p.index===p.path.length-1,remaining=dist(p,target);
  // Never cut a corner just because its waypoint is nearby. The next leg must be clear.
  if(!last&&remaining<.3&&segmentClear(p,p.path[p.index+1],this.canWalk,PEDESTRIAN_RADIUS)){
   p.index++;target=p.path[p.index];last=p.index===p.path.length-1;remaining=dist(p,target);p.progress=null;
  }
  if(last&&remaining<.08&&!p.passing){
   this.arrive(p,others);
   if(p.phase==='walk'){p.arrivalWait=(p.arrivalWait||0)+dt;if(p.arrivalWait>4){p.arrivalWait=0;this.plan(p,others);}}
   else p.arrivalWait=0;
   return;
  }
  if(!last&&remaining<.015){p.index++;target=p.path[p.index];remaining=dist(p,target);p.progress=null;}
  // Measure progress toward the waypoint, not tiny shuffles against an obstacle.
  if(!p.progress||p.progress.index!==p.index)p.progress={index:p.index,best:remaining,age:0};
  if(remaining<p.progress.best-.18){p.progress.best=remaining;p.progress.age=0;p.retries=0;}
  else if(!p.passing?.yieldTo)p.progress.age+=dt;
  const nearby=others.filter(q=>dist(p,q)<4);
  if(p.progress.age>3.5){
   const obstacles=[...nearby,...cars.filter(c=>c.speed<.3&&dist(p,c)<20).map(c=>({x:c.x,z:c.z,radius:Math.hypot(c.length,c.width)/2}))];
   p.retries=(p.retries||0)+1;p.progress.age=0;p.replans=(p.replans||0)+1;
   // A persistent local blocker (bus shelters, benches or a crowd pocket) can make a valid
   // A* route repeatedly select the same first segment. Before abandoning the activity,
   // deliberately route through a nearby clear pocket and continue from there.
   if(p.retries>=2){
    const heading=Math.atan2(target.x-p.x,target.z-p.z),angles=[Math.PI/2,-Math.PI/2,Math.PI*.35,-Math.PI*.35,Math.PI*.7,-Math.PI*.7];
    escape:for(const radius of [1.05,1.45,1.85])for(const offset of angles){
     const point={x:p.x+Math.sin(heading+offset)*radius,z:p.z+Math.cos(heading+offset)*radius};
     if(onRoad(point.x,point.z)||!this.canWalk(point.x,point.z,PEDESTRIAN_RADIUS)||obstacles.some(q=>Math.hypot(point.x-q.x,point.z-q.z)<(q.radius??.34)+PEDESTRIAN_RADIUS+.08))continue;
     if(!segmentClear(p,point,this.canWalk,PEDESTRIAN_RADIUS))continue;
     const tail=this.nav.find(point,p.goal,obstacles);if(tail.length<2)continue;
     p.path=[{x:p.x,z:p.z},point,...tail.slice(1)];p.index=1;p.progress=null;p.passing=null;p.retries=0;break escape;
    }
    if(!p.progress)return;
   }
   if(p.retries>=3&&this.plan(p,others,obstacles)){p.retries=0;return;}
   const path=this.nav.find(p,p.goal,obstacles);
   if(path.length>1){p.path=path;p.index=1;p.progress=null;return;}
   if(p.retries>=2&&this.plan(p,others,obstacles)){p.progress=null;return;}
  }
  let dx=target.x-p.x,dz=target.z-p.z,len=Math.hypot(dx,dz);dx/=Math.max(.001,len);dz/=Math.max(.001,len);
  const bodyClear=(x,z)=>nearby.every(q=>{
   const before=dist(p,q),after=Math.hypot(x-q.x,z-q.z);
   return after>=(q.radius??.34)+.4||after>before+1e-6;
  });
  const clear=(x,z,r=PEDESTRIAN_RADIUS)=>this.canWalk(x,z,r)&&bodyClear(x,z);
  const localCars=cars.filter(c=>dist(p,c)<9).map(c=>({now:c,next:{...c,...(c.route?routePose(c.route,c.progress+c.speed*.45):{x:c.x+Math.sin(c.angle)*c.speed*.45,z:c.z+Math.cos(c.angle)*c.speed*.45})}}));
  const carBlocks=(x,z)=>localCars.some(c=>pedestrianBlocks(c.now,{x,z})||pedestrianBlocks(c.next,{x,z}));
  // A committed passing point prevents the alternating left/right steering at corners.
  if(p.passing?.yieldTo){
   const q=p.passing.yieldTo,passed=!q.visible||(q.x-p.x)*p.passing.dx+(q.z-p.z)*p.passing.dz<-.5;
   if(passed||this.time>p.passing.until){
    p.passing=null;p.progress=null;const path=this.nav.find(p,p.goal,nearby);
    if(path.length>1){p.path=path;p.index=1;return;}
   }
   else if(dist(p,p.passing)<.12){p.speed=0;return;}
  }else if(p.passing&&(dist(p,p.passing)<.12||this.time>p.passing.until))p.passing=null;
  if(!p.passing&&this.time>=(p.passProbeAt||0)&&nearby.some(q=>{const x=q.x-p.x,z=q.z-p.z;return x*dx+z*dz>0&&x*dx+z*dz<1.7&&Math.abs(x*dz-z*dx)<.8;})){
   p.passProbeAt=this.time+.35;
   for(const side of [1,-1]){
    const point={x:p.x+dx*.9+dz*.95*side,z:p.z+dz*.9-dx*.95*side,until:this.time+3};
    if(segmentClear(p,point,clear,PEDESTRIAN_RADIUS)&&segmentClear(point,target,this.canWalk,PEDESTRIAN_RADIUS)&&!carBlocks(point.x,point.z)){p.passing=point;break;}
   }
  }
  // In a one-person gap, one resident deliberately steps back into a clear pocket.
  // Stable priority prevents both people from repeatedly choosing the same escape.
  if(!p.passing&&p.progress?.age>.8&&this.time>=(p.yieldProbeAt||0)){
   p.yieldProbeAt=this.time+.8;
   const blocker=nearby.find(q=>q.phase==='walk'&&q.id<p.id&&dist(p,q)<2&&(q.x-p.x)*dx+(q.z-p.z)*dz>0&&Math.sin(q.yaw)*dx+Math.cos(q.yaw)*dz<-.4);
   if(blocker){
    search:for(const back of [0,1,2,3,4])for(const side of [1,-1]){
     const point={x:p.x-dx*back+dz*1.05*side,z:p.z-dz*back-dx*1.05*side,until:this.time+12,yieldTo:blocker,dx,dz};
     if(!onRoad(point.x,point.z)&&clear(point.x,point.z)&&!carBlocks(point.x,point.z)){
      const escape=this.nav.find(p,point,nearby);if(escape.length<2)continue;
      point.path=escape.slice(1);point.index=0;p.passing=point;p.yields=(p.yields||0)+1;break search;
     }
    }
   }
  }
  if(p.passing){
   const pass=p.passing;
   if(pass.path&&pass.index<pass.path.length-1&&dist(p,pass.path[pass.index])<.02)pass.index++;
   const point=pass.path?pass.path[pass.index]:pass;
   dx=point.x-p.x;dz=point.z-p.z;len=Math.hypot(dx,dz);dx/=Math.max(.001,len);dz/=Math.max(.001,len);
  }
  const heading=Math.atan2(dx,dz),error=Math.atan2(Math.sin(heading-p.yaw),Math.cos(heading-p.yaw));
  p.yaw=turn(p.yaw,heading,dt*2.8);
  let desired=Math.min(p.maxSpeed,Math.sqrt(Math.max(0,len)*3))*Math.max(0,Math.cos(error));
  // Turn on the spot for sharp corners, then follow the tested segment exactly.
  // Rounded steering used to drift outside the graph into a strip from which no route existed.
  if(Math.abs(error)>.5)desired=0;
  const buddy=this.people[p.buddy];if(p.goal.cluster&&buddy?.visible&&buddy.phase==='walk'&&buddy.goal?.cluster===p.goal.cluster){desired=Math.min(desired,buddy.maxSpeed);if(dist(p,buddy)>4&&dist(p,p.goal)<dist(buddy,buddy.goal)){p.buddyWait=(p.buddyWait||0)+dt;if(p.buddyWait<8)desired=0;}else p.buddyWait=0;}
  p.speed=approach(p.speed,desired,dt*(desired<p.speed?2.8:1.65));
  const move=Math.abs(error)>.5?0:Math.min(len,p.speed*dt),x=p.x+dx*move,z=p.z+dz*move;
  p.intentX=dx*p.maxSpeed;p.intentZ=dz*p.maxSpeed;
  p.waitingTraffic=carBlocks(p.x+dx*Math.min(.3,len),p.z+dz*Math.min(.3,len));
  if(p.waitingTraffic||carBlocks(x,z)||!clear(x,z)){p.speed=approach(p.speed,0,dt*4);return;}
  p.x=x;p.z=z;p.distance+=move;
 }
 trafficPeople(){return this.people.filter(p=>p.visible).map(p=>({x:p.x,z:p.z,vx:p.vx,vz:p.vz,approachVx:p.waitingTraffic?p.intentX:p.vx,approachVz:p.waitingTraffic?p.intentZ:p.vz,crossing:p.phase==='walk'}));}
}
