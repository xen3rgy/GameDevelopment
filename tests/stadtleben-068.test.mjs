import {CITY_CHARACTER_FIXTURES} from '../dist/city-character-layout.js';
import test from 'node:test';
import assert from 'node:assert/strict';
import {PedestrianLife,pedestrianSurface} from '../dist/pedestrian-life.js';
import {CITIZEN_PORTALS,CITIZEN_DESTINATIONS,PROMENADE_FIXTURES,citizenAwake,gardenPatches,streetSpawn} from '../dist/pedestrian-layout.js';
import {NEIGHBORHOOD_FIXTURES} from '../dist/neighborhood-layout.js';
import {BUILDINGS,PEOPLE,LOCATIONS} from '../dist/data.js';
import {DISTRICT_FIXTURES,exteriorContains} from '../dist/city-layout.js';
import {STREET_POSTS} from '../dist/city-addresses.js';
import {groundHeight} from '../dist/spatial.js';
import {streetSurface} from '../dist/street-layout.js';
import {segmentClear} from '../dist/movement.js';
import {Traffic,pedestrianBlocks} from '../dist/traffic.js';
import {streetSeatLegPose,citizenAppearance} from '../dist/pedestrian-scene.js';
import {DELIVERY_PEOPLE,stationFacing,deliveryLocation,deliveryLeg} from '../dist/delivery-routes.js';
import {surfaceAt} from '../dist/soundscape.js';

const blocks=BUILDINGS.map(([x,z,w,d])=>({x,z,w:w/2+.35,d:d/2+.35})).concat(CITY_CHARACTER_FIXTURES,DISTRICT_FIXTURES,NEIGHBORHOOD_FIXTURES,PROMENADE_FIXTURES,STREET_POSTS.map(p=>({...p,w:.06,d:.06})));
for(const id of Object.keys(DELIVERY_PEOPLE)){const l=deliveryLocation(id),face=stationFacing(id);blocks.push({x:l.x,z:l.z,w:.22,d:.22},{x:l.x-1.5*face,z:l.z-.18*face,w:.55,d:.27});}
const free=(x,z,r=.35)=>exteriorContains(x,z,r)&&!blocks.some(b=>Math.abs(x-b.x)<b.w+r&&Math.abs(z-b.z)<b.d+r);
const near=(a,b,e=.005)=>assert.ok(Math.abs(a-b)<e,`${a} != ${b}`);

test('pavement graph connects homes to varied activities without cutting through unmarked lanes',()=>{
 const life=new PedestrianLife(free,{count:0});let reachable=0;
 for(const home of CITIZEN_PORTALS.filter(p=>p.home))for(const d of life.destinations){
  const path=life.nav.find(home,d);if(path.length<2)continue;reachable++;
  for(let i=1;i<path.length;i++)assert.ok(segmentClear(path[i-1],path[i],life.canWalk,.35),d.id);
 }
 assert.ok(reachable>=70,`${reachable} connected home/activity pairs`);
 assert.equal(pedestrianSurface(50,0),false);assert.equal(pedestrianSurface(10,0),true);
 for(const id of ['deliveryA','deliveryB','deliveryC','deliveryKiosk','deliveryWorkshop'])assert.ok(deliveryLeg('jobs',id)?.distance>0);
});

test('garden path tiles are disjoint, do not cover roads/pavements and match support height',()=>{
 const patches=gardenPatches();assert.ok(patches.length>5&&patches.length<400);
 for(let i=0;i<patches.length;i++){
  const a=patches[i],x=(a.minX+a.maxX)/2,z=(a.minZ+a.maxZ)/2;assert.equal(streetSurface(x,z),null);near(groundHeight(x,z),.02);assert.equal(surfaceAt(x,z),'paving');
  for(const b of patches.slice(i+1))assert.ok(Math.min(a.maxX,b.maxX)<=Math.max(a.minX,b.minX)||Math.min(a.maxZ,b.maxZ)<=Math.max(a.minZ,b.minZ));
 }
});

test('sitting keeps scaled residents on the seat with feet on the floor during lowering',()=>{
 for(const scale of [.94,1,1.054])for(const height of [.5175,.52,.555]){
  let lastZ=null;
  for(let weight=.05;weight<=1.001;weight+=.05){
   const p=streetSeatLegPose(height,scale,weight),y=(-.05+p.hip-.38*Math.cos(p.angle)-.405*Math.cos(p.angle+p.knee)-.065)*scale;
   assert.ok(y>=-.001&&y<.008,`foot y=${y}`);
   const z=.35*(1-weight)+(-.38*Math.sin(p.angle)-.405*Math.sin(p.angle+p.knee))*scale;
   if(weight>.3&&lastZ!=null)near(z,lastZ,.004);lastZ=z;
   if(weight>.999)near((p.hip-.235)*scale,height);
  }
 }
 assert.equal(new Set(Array.from({length:32},(_,i)=>JSON.stringify(citizenAppearance(i)))).size,32);
});

test('residents walk, visit and sit with traffic, without body overlaps or visible teleports',()=>{
 const life=new PedestrianLife(free,{bystanders:PEOPLE}),traffic=new Traffic(),seen=new Set();let collisions=0;
 const active=life.people.filter(p=>citizenAwake(p.id,720));
 for(let frame=0;frame<6000;frame++){
  const previous=life.people.map(p=>({x:p.x,z:p.z,visible:p.visible,phase:p.phase}));
  life.update(.05,720,{bystanders:PEOPLE,cars:traffic.cars});traffic.update(.05,life.trafficPeople());
  for(const p of life.people){seen.add(p.phase);const before=previous[p.id];
   if(p.visible&&before.visible)assert.ok(Math.hypot(p.x-before.x,p.z-before.z)<.18,`teleport ${p.id} ${before.phase}/${p.phase}`);
   if(p.phase==='walk'){assert.ok(life.canWalk(p.x,p.z,.30),`walk through fixture ${p.id}`);for(const c of traffic.cars)if(pedestrianBlocks(c,p))collisions++;}
  }
  const visible=life.people.filter(p=>p.visible&&!['enter','exit'].includes(p.phase));
  for(let i=0;i<visible.length;i++)for(const b of visible.slice(i+1))assert.ok(Math.hypot(visible[i].x-b.x,visible[i].z-b.z)>.62,`overlap ${visible[i].id}/${b.id}`);
 }
 assert.equal(collisions,0);for(const phase of ['walk','sitDown','seated','standUp','wait','enter','exit'])assert.ok(seen.has(phase),phase);
 assert.ok(active.every(p=>p.travelled>20),JSON.stringify(active.filter(p=>p.travelled<=20).map(p=>({id:p.id,phase:p.phase,travel:p.travelled}))));
 assert.ok(life.people.some(p=>p.visits>0));
});

test('time of day reduces population and paused simulation does not move people',()=>{
 const day=new PedestrianLife(free,{minute:720}),night=new PedestrianLife(free,{minute:180});
 assert.ok(night.people.filter(p=>p.visible).length<day.people.filter(p=>p.visible).length/2);
 for(const p of night.people.filter(p=>p.visible))assert.ok(citizenAwake(p.id,180));
 const before=day.people.map(p=>({x:p.x,z:p.z,phase:p.phase,age:p.age}));day.update(0,720);
 assert.deepEqual(day.people.map(p=>({x:p.x,z:p.z,phase:p.phase,age:p.age})),before);
 const p=day.people.find(p=>p.visible&&p.phase==='walk');day.update(.05,180,{bystanders:PEOPLE});assert.equal(p.goal.kind,'home');
});

test('old saves inside newly solid street furniture get a nearby clear start, safe positions stay untouched',()=>{
 for(const p of PROMENADE_FIXTURES.filter(p=>['oldBench','trunk','oldPlanter'].includes(p.kind))){const start=streetSpawn({x:p.x,z:p.z},free);assert.ok(free(start.x,start.z));assert.ok(Math.hypot(start.x-p.x,start.z-p.z)<=4.01);}
 const start={x:30,z:8};assert.strictEqual(streetSpawn(start,free),start);
});


test('0.6.8.1 planned routes remain clear between coarse probes at kerbs and furniture corners',()=>{
 const life=new PedestrianLife(free,{count:0});let segments=0;
 for(const home of CITIZEN_PORTALS.filter(p=>p.home))for(const goal of life.destinations){
  const path=life.nav.find(home,goal);
  for(let i=1;i<path.length;i++){
   const a=path[i-1],b=path[i],length=Math.hypot(a.x-b.x,a.z-b.z),steps=Math.ceil(length/.025);
   for(let k=1;k<=steps;k++){const t=k/steps,x=a.x+(b.x-a.x)*t,z=a.z+(b.z-a.z)*t;
    assert.ok(life.canWalk(x,z,.4),`${home.id}/${goal.id}: blocked segment at ${x},${z}`);
   }
   segments++;
  }
 }
 assert.ok(segments>200);
});

test('0.6.8.1 residents yield into a pocket and both clear a one-person passage',()=>{
 const corridor=(x,z,r)=>x>-81+r&&x<-59-r&&(Math.abs(z+9.5)<.65-r||(x>-74+r&&x<-68-r&&z>-10.15+r&&z<-7+r));
 const life=new PedestrianLife(corridor,{count:2});
 for(const [id,x] of [-79,-72].entries()){
  const goal={id:'gap-'+id,kind:'wait',x:id?-79:-62,z:-9.5,angle:0};
  Object.assign(life.people[id],{x,z:-9.5,visible:true,phase:'walk',yaw:id?-Math.PI/2:Math.PI/2,maxSpeed:1.2,buddy:null,goal,path:[{x,z:-9.5},goal],index:1});
 }
 const arrived=new Set();
 for(let i=0;i<600;i++){
  const before=life.people.map(p=>({x:p.x,z:p.z}));life.update(.05,720);
  const [a,b]=life.people;assert.ok(Math.hypot(a.x-b.x,a.z-b.z)>=.739);
  for(const p of life.people){assert.ok(corridor(p.x,p.z,.4));assert.ok(Math.hypot(p.x-before[p.id].x,p.z-before[p.id].z)<.08);if(p.phase==='wait')arrived.add(p.id);}
 }
 assert.equal(arrived.size,2);assert.ok(life.people[1].yields>0);
});

test('0.6.8.1 a blocked activity releases its reservation instead of exhausting the town',()=>{
 const life=new PedestrianLife(free,{count:1}),p=life.people[0],home=CITIZEN_PORTALS[0];
 Object.assign(p,{phase:'wait',visible:true,x:home.x,z:home.z,goal:life.destinations[0]});
 life.reserved.set(p.goal.id,p.id);const old=p.goal.id;life.destinations=[];
 assert.equal(life.plan(p),true);assert.equal(p.goal.kind,'home');assert.equal(life.reserved.has(old),false);
});

test('0.6.8.1 a stationary player and parked vehicle trigger a collision-free detour',()=>{
 const life=new PedestrianLife(()=>true,{count:1}),p=life.people[0],goal={id:'blocked',kind:'wait',x:-60,z:-9.5,angle:0};
 Object.assign(p,{x:-80,z:-9.5,visible:true,phase:'walk',yaw:Math.PI/2,maxSpeed:1.2,buddy:null,goal,path:[{x:-80,z:-9.5},goal],index:1});
 const bystanders=[{x:-75,z:-9.5,radius:.4}],cars=[{x:-70,z:-9.5,length:3.8,width:1.85,angle:Math.PI/2,speed:0}];
 let arrived=false;
 for(let frame=0;frame<1400;frame++){
  life.update(.05,720,{bystanders,cars});assert.ok(!pedestrianBlocks(cars[0],p));assert.ok(Math.hypot(p.x+75,p.z+9.5)>=.74);
  if(p.phase==='wait'){arrived=true;break;}
 }
 assert.ok(arrived);assert.ok(p.replans>0);assert.equal(p.goal.id,'blocked');
});

test('0.6.8.1 30-minute traffic run has no permanent pavement stalls, overlaps or teleports',()=>{
 const life=new PedestrianLife(free,{bystanders:PEOPLE}),traffic=new Traffic(),checkpoints=life.people.map(p=>({x:p.x,z:p.z,stalled:0}));
 const maximum=new Map(),carDistance=new Map();
 for(let frame=0;frame<36000;frame++){
  const before=life.people.map(p=>({x:p.x,z:p.z,visible:p.visible}));
  life.update(.05,720,{bystanders:PEOPLE,cars:traffic.cars});
  const previousCars=traffic.cars.map(c=>({x:c.x,z:c.z}));traffic.update(.05,life.trafficPeople());
  for(const c of traffic.cars)carDistance.set(c.id,(carDistance.get(c.id)||0)+Math.hypot(c.x-previousCars[c.id].x,c.z-previousCars[c.id].z));
  for(const p of life.people){
   if(p.visible&&before[p.id].visible)assert.ok(Math.hypot(p.x-before[p.id].x,p.z-before[p.id].z)<.18,`jump ${p.id}`);
   if(p.phase==='walk'){assert.ok(life.canWalk(p.x,p.z,.39),`fixture ${p.id}`);const hit=traffic.cars.find(c=>pedestrianBlocks(c,p));assert.ok(!hit,`traffic contact ${JSON.stringify({frame,id:p.id,x:p.x,z:p.z,before:before[p.id],car:hit&&{id:hit.id,x:hit.x,z:hit.z,angle:hit.angle,speed:hit.speed,before:previousCars[hit.id]}})}`);}
  }
  if(frame%100===99){
   const visible=life.people.filter(p=>p.visible&&!['enter','exit'].includes(p.phase));
   for(let i=0;i<visible.length;i++)for(const q of visible.slice(i+1))assert.ok(Math.hypot(visible[i].x-q.x,visible[i].z-q.z)>.62);
   for(const p of life.people){const old=checkpoints[p.id],moving=['walk','exit','enter','sitDown','standUp'].includes(p.phase);
    old.stalled=moving&&Math.hypot(p.x-old.x,p.z-old.z)<.6?old.stalled+5:0;old.x=p.x;old.z=p.z;
    maximum.set(p.id,Math.max(maximum.get(p.id)||0,old.stalled));
    assert.ok(old.stalled<=25,`stalled ${p.id} ${p.phase} ${p.x},${p.z} ${old.stalled}s`);
   }
  }
 }
 assert.ok(life.people.filter(p=>citizenAwake(p.id,720)).every(p=>p.travelled>300));
 assert.ok([...carDistance.values()].every(d=>d>500));
});
