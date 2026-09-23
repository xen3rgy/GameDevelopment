import test from 'node:test';
import assert from 'node:assert/strict';
import {GameModel,newGame,validateSave} from '../dist/model.js';
import {beginService,endService,tickService,serviceQuote,fuelPrice,vehicleAtBay} from '../dist/vehicle-service.js';
import {SERVICE_BAYS,SERVICE_FIXTURES} from '../dist/service-layout.js';
import {fleetCommand,transferTrunk,parkVehicle,vehicleTravel,REPAIR_RATE} from '../dist/fleet.js';
import {canTravel} from '../dist/transit.js';
import {BUILDINGS} from '../dist/data.js';
import {DISTRICT_FIXTURES} from '../dist/city-layout.js';
import {PROMENADE_FIXTURES,streetSpawn} from '../dist/pedestrian-layout.js';
import {NEIGHBORHOOD_FIXTURES} from '../dist/neighborhood-layout.js';
import {CITY_CHARACTER_FIXTURES} from '../dist/city-character-layout.js';
import {TRANSIT_STOPS} from '../dist/mobility-layout.js';
import {World} from '../dist/world.js';
import {overlaps,vehicleBody} from '../dist/traffic.js';
import {groundHeight} from '../dist/spatial.js';
const setup=(index=0,type='car')=>{const m=new GameModel(newGame(true));m.s.position={x:76,z:12};assert.ok(m.buyVehicle(type));const b=SERVICE_BAYS[index];Object.assign(m.s.vehicle,b.vehicle,{fuel:40,condition:60,parking:null});m.s.position={x:b.x,z:b.z};return m;};
test('insured accident credit follows each vehicle class, excludes wear and preserves legacy credit',()=>{
 for(const id of ['car','van','sport']){
  const m=setup(2,id),v=m.s.vehicle;v.insured=true;v.claimCredit=123;v.condition=90;
  vehicleTravel(v,1000);assert.equal(v.claimCredit,123);const before=v.condition;
  vehicleTravel(v,0,8);const damage=before-v.condition;
  assert.ok(Math.abs(v.claimCredit-(123+damage*REPAIR_RATE[id]*.7))<1e-8);
  const restored=new GameModel(validateSave(JSON.parse(JSON.stringify(m.s))));
  assert.equal(restored.s.vehicle.claimCredit,v.claimCredit);
  const q=serviceQuote(restored.s,restored.s.vehicle,'repair');assert.equal(q.credit,Math.floor(v.claimCredit));
  beginService(restored,'autoWorkshop');tickService(restored,6);assert.equal(restored.s.vehicle.claimCredit,0);
 }
});
test('service cannot complete early; repair cancellation reports its own refund',()=>{
 for(const index of [0,2]){const m=setup(index),v=m.s.vehicle;beginService(m,SERVICE_BAYS[index].id);const money=m.s.money;
  assert.equal(endService(m),false);assert.equal(v.fuel,40);assert.equal(v.condition,60);assert.equal(m.s.money,money);
  tickService(m,2);assert.equal(endService(m),false);assert.ok(endService(m,true));
  if(index===2)assert.match(m.events.at(-1).text,/Reparatur abgebrochen.*vollständig erstattet/);
 }
});
test('old pedestrian saves inside new service geometry relocate locally without losing fleet or cash',()=>{
 for(const c of SERVICE_FIXTURES){const m=setup();m.s.position={x:c.x,z:c.z};delete m.s.vehicleService;
  const loaded=validateSave(JSON.parse(JSON.stringify(m.s))),money=loaded.money,vehicle=JSON.stringify(loaded.fleet);
  const free=(x,z)=>!SERVICE_FIXTURES.some(o=>Math.abs(x-o.x)<o.w+.35&&Math.abs(z-o.z)<o.d+.35);
  loaded.position=streetSpawn(loaded.position,free);assert.ok(free(loaded.position.x,loaded.position.z),c.kind);
  assert.ok(Math.hypot(loaded.position.x-c.x,loaded.position.z-c.z)<=4);assert.equal(loaded.money,money);assert.equal(JSON.stringify(loaded.fleet),vehicle);
  assert.doesNotThrow(()=>validateSave(loaded));
 }
});
test('fuel price follows day/economy and capacity; fuel reservation completes exactly once',()=>{
 const m=setup(),v=m.s.vehicle,q=serviceQuote(m.s,v,'fuel'),money=m.s.money;
 assert.notEqual(fuelPrice(m.s),fuelPrice({...m.s,day:2}));assert.ok(fuelPrice({...m.s,economy:'Aufschwung'})>fuelPrice(m.s));
 assert.ok(beginService(m,'pump1'));assert.equal(beginService(m,'pump1'),false);assert.equal(m.s.money,money-q.cost);
 tickService(m,8);assert.equal(v.fuel,100);assert.equal(m.s.money,money-q.cost);assert.equal(endService(m),false);assert.equal(beginService(m,'pump1'),false);
});
test('partial fuel cancellation refunds unused fuel exactly once; insufficient funds cannot start',()=>{
 const m=setup(),money=m.s.money,q=serviceQuote(m.s,m.s.vehicle,'fuel');beginService(m,'pump1');tickService(m,2);assert.ok(endService(m,true));
 assert.equal(m.s.vehicle.fuel,55);assert.equal(m.s.money,money-Math.ceil(q.cost/4));const after=m.s.money;assert.equal(endService(m,true),false);assert.equal(m.s.money,after);
 m.s.money=0;assert.equal(beginService(m,'pump1'),false);assert.equal(m.s.vehicle.fuel,55);
});
test('repair is atomic, type priced, honors credit, and cancellation preserves damage and credit',()=>{
 const m=setup(2),v=m.s.vehicle;v.claimCredit=1800;const q=serviceQuote(m.s,v,'repair'),money=m.s.money;
 assert.equal(q.cost,3000);assert.ok(serviceQuote(m.s,{...v,id:'sport'},'repair').cost>q.cost);
 beginService(m,'autoWorkshop');tickService(m,3);endService(m,true);assert.equal(v.condition,60);assert.equal(v.claimCredit,1800);assert.equal(m.s.money,money);
 beginService(m,'autoWorkshop');tickService(m,6);assert.equal(v.condition,100);assert.equal(v.claimCredit,0);assert.equal(m.s.money,money-q.cost);
});
test('service save resumes without charging again; old saves need no migration; invalid escrow rejected',()=>{
 const m=setup();beginService(m,'pump1');tickService(m,3);const money=m.s.money;
 const s=validateSave(JSON.parse(JSON.stringify(m.s))),n=new GameModel(s);assert.equal(n.s.vehicle,n.s.fleet[0]);tickService(n,5);assert.equal(n.s.money,money);assert.equal(n.s.vehicle.fuel,100);
 const bad=JSON.parse(JSON.stringify(m.s));bad.vehicleService.cost++;assert.throws(()=>validateSave(bad));
 delete n.s.vehicleService;assert.equal(validateSave(n.s).vehicleService,null);assert.equal(n.s.fleet.length,1);
});
test('bike repairs but never fuels; wrong orientation, distance, moving and stored vehicles are rejected',()=>{
 const bike=setup(0,'bike');assert.equal(beginService(bike,'pump1'),false);const m=setup(2,'bike');assert.ok(beginService(m,'autoWorkshop'));endService(m,true);
 for(const change of [{speed:.2},{stored:true},{angle:Math.PI/2},{x:-10}]){const n=setup();Object.assign(n.s.vehicle,change);assert.equal(beginService(n,'pump1'),false);}
 const n=setup();n.s.position.x+=4;assert.equal(beginService(n,'pump1'),false);
});
test('saved repairs and midnight fuel prices retain their original reservation at every game speed',()=>{
 for(const speed of [1,4,10]){
  const m=setup(2);m.s.settings.speed=speed;m.s.vehicle.claimCredit=9999;const money=m.s.money;assert.ok(beginService(m,'autoWorkshop'));m.updateTime(2);
  const n=new GameModel(validateSave(JSON.parse(JSON.stringify(m.s))));n.updateTime(4);assert.equal(n.s.vehicle.condition,100);assert.equal(n.s.vehicle.claimCredit,0);assert.equal(n.s.money,money);
  const f=setup();f.s.minute=1439;f.s.settings.speed=speed;const q=serviceQuote(f.s,f.s.vehicle,'fuel'),before=f.s.money;beginService(f,'pump1');f.updateTime(1);
  const loaded=new GameModel(validateSave(JSON.parse(JSON.stringify(f.s))));loaded.updateTime(7);assert.equal(loaded.s.vehicle.fuel,100);assert.equal(loaded.s.money,before-q.cost);
 }
});
test('mutual exclusions block service and other actions at model boundary',()=>{
 for(const change of [{riding:true},{inside:true},{transit:{}},{job:{type:'cleaning'}},{dailyLife:{action:{}}},{workshop:{active:{}}}]){const m=setup();Object.assign(m.s,change);assert.equal(beginService(m,'pump1'),false);}
 const m=setup();beginService(m,'pump1');assert.equal(m.startJob('cleaning'),false);assert.equal(m.beginLifeAction('consume',0),false);assert.equal(m.workshopAction('accept'),false);assert.equal(fleetCommand(m,'insurance',m.s.vehicle.uid),false);assert.equal(transferTrunk(m,m.s.vehicle.uid,'deposit',0,1),false);assert.equal(canTravel(m.s,m.s.position),false);
 const q=m.s.money;m.s.position.x+=4;tickService(m,1);assert.equal(m.s.vehicleService,null);assert.ok(m.s.money>q);
});
test('two pumps detect individual vehicles and refuse overlapping bodies',()=>{
 const m=setup();const first=m.s.vehicle;m.s.position={x:76,z:12};m.buyVehicle('van');Object.assign(m.s.vehicle,SERVICE_BAYS[1].vehicle,{parking:null,fuel:20});
 assert.equal(vehicleAtBay(m.s,SERVICE_BAYS[0]),first);assert.equal(vehicleAtBay(m.s,SERVICE_BAYS[1]),m.s.vehicle);
 Object.assign(m.s.vehicle,first,{uid:'v2',x:first.x+1.5});m.s.position={x:-40,z:-87};assert.equal(beginService(m,'pump1'),false);
});
test('site, bays, van approaches, walking exits and collision bodies clear city fixtures',()=>{
 const blocks=[...BUILDINGS.map(([x,z,w,d,h])=>({x,z,w:w/2,d:d/2,h})),...DISTRICT_FIXTURES,...PROMENADE_FIXTURES,...NEIGHBORHOOD_FIXTURES,...CITY_CHARACTER_FIXTURES,...TRANSIT_STOPS.map(p=>({x:p.x-1.6,z:p.z,w:.08,d:.08})),...SERVICE_FIXTURES];
 for(const b of SERVICE_BAYS){
  const v={id:'van',...b.vehicle},w={model:{s:{fleet:[v]}},colliders:blocks,traffic:{blocksVehicle:()=>false}};
  assert.ok(!blocks.some(c=>Math.abs(b.x-c.x)<c.w+.4&&Math.abs(b.z-c.z)<c.d+.4),b.id+' exit');
  for(let z=-70;z>=v.z;z-=.5)assert.ok(World.prototype.canDrive.call(w,v,v.x,z,0),b.id+' approach '+z);
  assert.equal(groundHeight(v.x,v.z),.30);
  const m=setup(SERVICE_BAYS.indexOf(b),'van');m.s.riding=true;m.s.position={...v};assert.ok(parkVehicle(m,()=>true,()=>true));assert.equal(m.s.riding,false);
 }
 for(const c of SERVICE_FIXTURES)assert.ok(!BUILDINGS.some(([x,z,w,d])=>overlaps({x:c.x,z:c.z,width:c.w*2,length:c.d*2,angle:0},{x,z,width:w,length:d,angle:0})),c.kind+' building overlap');
 const m=setup(),v=m.s.vehicle,w={model:m,colliders:SERVICE_FIXTURES,traffic:{blocksVehicle:()=>false}};assert.equal(World.prototype.canDrive.call(w,v,-41.2,-87,0),false);
});
