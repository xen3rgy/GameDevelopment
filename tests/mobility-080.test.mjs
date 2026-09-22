import test from 'node:test';
import assert from 'node:assert/strict';
import {GameModel,newGame,validateSave} from '../dist/model.js';
import {fleetCommand,transferTrunk,trunkPoint,vehicleTravel,usedOffers,parkVehicle,freeParking} from '../dist/fleet.js';
import {PARKING_SPACES,TRANSIT_STOPS,safeArrival} from '../dist/mobility-layout.js';
import {tripQuote,bookTrip,tickTransit} from '../dist/transit.js';
import {transitPose} from '../dist/mobility-scene.js';
import {clockLabel} from '../dist/game-time.js';
import {beginFieldWork,tickFieldWork} from '../dist/field-work.js';
import {WORK} from '../dist/data.js';
import {meadowPlacements,meadowPoint} from '../dist/meadow.js';
import {moveWithCollision} from '../dist/movement.js';
import {FrameMetrics} from '../dist/performance.js';
import {BUILDINGS} from '../dist/data.js';
import {DISTRICT_FIXTURES,exteriorContains,WORLD_BOUNDS} from '../dist/city-layout.js';
import {PROMENADE_FIXTURES} from '../dist/pedestrian-layout.js';
import {NEIGHBORHOOD_FIXTURES} from '../dist/neighborhood-layout.js';
import {CITY_CHARACTER_FIXTURES} from '../dist/city-character-layout.js';
import {vehicleBody,overlaps} from '../dist/traffic.js';
import {CityNavigation} from '../dist/navigation.js';
import {World} from '../dist/world.js';
import {CameraRig} from '../dist/spatial.js';
const garage=()=>{const m=new GameModel(newGame(true));m.s.position={x:76,z:12};return m;};
const atStop=(m,id='busCity')=>{const p=TRANSIT_STOPS.find(p=>p.id===id);m.s.position={x:p.x,z:p.z};return p;};
test('six individually owned vehicles retain UID, trunks and parking through save; full pickups cannot charge',()=>{
 const m=garage();for(let i=0;i<3;i++)assert.ok(m.buyVehicle('car'));assert.equal(new Set(m.s.fleet.map(v=>v.parking)).size,3);const money=m.s.money;assert.equal(m.buyVehicle('bike'),false);assert.equal(m.s.money,money);
 for(let i=0;i<3;i++){assert.ok(fleetCommand(m,'store',m.s.vehicle.uid));assert.ok(m.buyVehicle('bike'));}assert.equal(m.s.fleet.length,6);assert.equal(m.buyVehicle('car'),false);const loaded=validateSave(JSON.parse(JSON.stringify(m.s)));assert.equal(loaded.fleet.length,6);assert.equal(loaded.vehicle,loaded.fleet.at(-1));assert.equal(new Set(loaded.fleet.map(v=>v.uid)).size,6);
});
test('stale parking is released; duplicate physical occupancy is recovered into storage; IDs never reuse',()=>{
 const m=garage();m.buyVehicle('car');m.buyVehicle('bike');const [a,b]=m.s.fleet;Object.assign(b,{x:a.x,z:a.z,parking:a.parking});m.s.fleetSerial=0;a.uid='v19';const loaded=validateSave(m.s);assert.equal(loaded.fleet[1].stored,true);assert.equal(loaded.vehicle,null);assert.equal(loaded.fleetSerial,19);const n=new GameModel(loaded);assert.ok(n.buyVehicle('bike'));assert.equal(n.s.vehicle.uid,'v20');vehicleTravel(n.s.vehicle,1);assert.equal(n.s.vehicle.parking,null);a.x+=20;assert.equal(validateSave(m.s).fleet[0].parking,null);
});
test('used cars have realistic mileage, disclosed damage and a once-only daily offer',()=>{
 const m=garage(),o=usedOffers(1)[0];assert.ok(o.odometer>=38000&&o.odometer<=183000);assert.ok(m.buyVehicle(o.id,o.key));assert.equal(m.s.vehicle.odometer,o.odometer);const money=m.s.money;assert.equal(m.buyVehicle(o.id,o.key),false);assert.equal(m.s.money,money);m.s.position={x:0,z:0};assert.equal(m.buyVehicle('bike'),false);
});
test('trunk transfers are atomic, physical and reject mission parcels; stored cars cannot be looted',()=>{
 const m=garage();m.buyVehicle('bike');const v=m.s.vehicle;m.s.position=trunkPoint(v);m.add('water',3);assert.ok(transferTrunk(m,v.uid,'deposit',0,2));assert.equal(m.count('water'),1);assert.equal(v.trunk[0].count,2);m.add('parcel');assert.equal(transferTrunk(m,v.uid,'deposit',1,1),false);m.s.position.x+=10;assert.equal(transferTrunk(m,v.uid,'withdraw',0,2),false);assert.equal(v.trunk[0].count,2);m.s.position=trunkPoint(v);assert.ok(transferTrunk(m,v.uid,'withdraw',0,2));v.stored=true;assert.equal(transferTrunk(m,v.uid,'deposit',0,1),false);
});
test('insurance pays daily, wear remains gradual, accident credit cannot pay cash and sale requires empty trunk',()=>{
 const m=garage();m.buyVehicle('car');const v=m.s.vehicle,before=m.s.money;assert.ok(fleetCommand(m,'insurance',v.uid));assert.equal(m.s.money,before-250);vehicleTravel(v,1000,8);assert.equal(v.odometer,1);assert.ok(v.condition<100&&v.condition>90);assert.ok(v.claimCredit>0);assert.ok(fleetCommand(m,'repair',v.uid));assert.equal(v.condition,100);assert.equal(v.claimCredit,0);v.trunk=[{id:'water',count:1}];assert.equal(fleetCommand(m,'sell',v.uid),false);m.s.money=0;m.advance(1440-m.s.minute);assert.equal(v.insured,false);
});
test('parking is unique, rejects blocked exit and gives a safe pedestrian position',()=>{
 const m=garage();m.buyVehicle('car');const v=m.s.vehicle,p=PARKING_SPACES[0];v.x=p.x+1;v.parking=null;m.s.riding=true;m.s.position={x:v.x,z:v.z};assert.equal(parkVehicle(m,()=>false,()=>true),false);assert.ok(parkVehicle(m,()=>true,()=>true));assert.equal(m.s.riding,false);assert.deepEqual(m.s.position,p.exit);assert.equal(v.parking,p.id);assert.equal(freeParking(m.s,[p]),undefined);
});
test('fractional schedules display integer minutes and correct clock across midnight',()=>{
 const s=newGame(true);s.minute=481.234567;const q=tripQuote(s,'busCity','busPark');assert.ok(Number.isInteger(q.waitLabel));assert.ok(Number.isInteger(q.totalLabel));assert.equal(clockLabel(s.minute),'08:01');assert.equal(clockLabel(1440+481.2),'08:01');s.minute=1400;const next=tripQuote(s,'station','railSouth');assert.equal(Math.floor(next.departure/1440),s.day+1);assert.equal(clockLabel(next.departure),'06:05');
});
test('transit charges once, pauses clock during boarding and resumes saved ticket exactly once at any speed',()=>{
 for(const speed of [1,4,10]){let m=garage();atStop(m);m.s.minute=481.25;m.s.settings.speed=speed;const q=tripQuote(m.s,'busCity','busPark'),before=m.s.money;assert.ok(bookTrip(m,'busCity','busPark'));assert.equal(bookTrip(m,'busCity','busPark'),false);m.updateTime(3);assert.equal(m.s.minute,481.25);m=new GameModel(validateSave(JSON.parse(JSON.stringify(m.s))));m.findTransitArrival=p=>({...p});m.updateTime(5.5);assert.equal(m.s.transit,null);assert.equal(m.s.money,before-q.fare);assert.equal(m.s.minute,481.25+q.total);assert.deepEqual(m.s.position,TRANSIT_STOPS.find(p=>p.id==='busPark').arrival);assert.equal(tickTransit(m,10),false);assert.doesNotThrow(()=>validateSave(m.s));}
});
test('transit rejects remote, busy and forged tickets; blocked arrival refunds without advancing time',()=>{
 const m=garage();assert.equal(bookTrip(m,'busCity','busPark'),false);atStop(m);m.s.job={carrying:true};assert.equal(bookTrip(m,'busCity','busPark'),false);m.s.job=null;const money=m.s.money,minute=m.s.minute;bookTrip(m,'busCity','busPark');const corrupt=structuredClone(m.s);corrupt.transit.total+=100;assert.throws(()=>validateSave(corrupt));m.findTransitArrival=()=>null;m.updateTime(9);assert.equal(m.s.money,money);assert.equal(m.s.minute,minute);assert.equal(m.s.transit,null);
});
test('dynamic arrival searches nearby, returns null if blocked; cinematic ends without retaining an actor',()=>{
 const p=safeArrival({x:9,z:9},(x,z)=>Math.hypot(x-9,z-9)>.7);assert.ok(Math.hypot(p.x-9,p.z-9)>.7);assert.equal(safeArrival({x:0,z:0},()=>false),null);assert.equal(transitPose(0).approach,1);assert.equal(transitPose(2).door,1);assert.equal(transitPose(8.5).visible,false);assert.equal(transitPose(8.5).fade,1);
});
test('cleaning accepts any remaining sack, timed work pays only after completion and survives reload',()=>{
 const m=new GameModel();m.startJob('cleaning');for(const i of [5,2,0,4,1,3]){m.s.position={x:WORK.cleaning[i][0],z:WORK.cleaning[i][1]};assert.ok(beginFieldWork(m,'trash',i));tickFieldWork(m,.5);assert.equal(m.s.money,0);assert.equal(beginFieldWork(m,'trash',i),false);tickFieldWork(m,.8);if(m.s.job){assert.equal(m.work('trash',i),false);assert.doesNotThrow(()=>validateSave(m.s));}}assert.equal(m.s.job,null);assert.equal(m.s.money,4200);tickFieldWork(m,20);assert.equal(m.s.money,4200);
});
test('warehouse rejects wrong shelf and simultaneous repeated scans',()=>{const m=new GameModel();m.startJob('warehouse');m.s.position={...WORK.crate};assert.ok(beginFieldWork(m,'crate'));assert.equal(beginFieldWork(m,'crate'),false);tickFieldWork(m,2);assert.equal(m.s.job.carrying,true);m.s.position={...WORK.shelves[1]};assert.equal(beginFieldWork(m,'shelf',1),false);m.s.position={...WORK.shelves[0]};assert.ok(beginFieldWork(m,'shelf',0));tickFieldWork(m,1.1);assert.equal(m.s.job.progress,1);assert.equal(m.s.job.carrying,false);});
test('meadow placements are deterministic, bounded and keep paved surfaces clear',()=>{const groups=meadowPlacements(),again=meadowPlacements();assert.deepEqual(groups,again);const all=[...groups.values()].flat();assert.ok(all.length>1000&&all.length<50000);for(const p of all){assert.ok(meadowPoint(p.x,p.z));assert.ok(p.cell>=0&&p.cell<12);assert.ok(p.size<.7);}assert.equal(meadowPoint(0,0),false);});
test('invalid movement never hangs or corrupts position; measured frame statistics are deterministic',()=>{assert.equal(moveWithCollision({x:1,z:2},Infinity,0,()=>true).distance,0);const m=new FrameMetrics();for(let i=0;i<100;i++)m.record(i*20,4,{calls:50,triangles:200});assert.equal(m.summary().fps,50);assert.equal(m.summary().p95,20);assert.equal(m.summary().drawCalls,50);});
test('every transit stop, arrival, parking body and exit clears the complete town layout',()=>{
 const blocks=[...BUILDINGS.map(([x,z,w,d])=>({x,z,w:w/2+.35,d:d/2+.35})),...DISTRICT_FIXTURES,...NEIGHBORHOOD_FIXTURES,...PROMENADE_FIXTURES,...CITY_CHARACTER_FIXTURES,...TRANSIT_STOPS.map(p=>({x:p.x-1.6,z:p.z,w:.08,d:.08}))];
 const free=(x,z,r=.4)=>exteriorContains(x,z,r)&&!blocks.some(c=>Math.abs(x-c.x)<c.w+r&&Math.abs(z-c.z)<c.d+r),nav=new CityNavigation(free,4,WORLD_BOUNDS);
 for(const p of TRANSIT_STOPS){assert.ok(free(p.x,p.z),p.id+' prompt');assert.ok(free(p.arrival.x,p.arrival.z),p.id+' arrival');assert.ok(nav.find({x:-27,z:-4},p).length>=2,p.id+' route');}
 for(const p of PARKING_SPACES){assert.ok(free(p.exit.x,p.exit.z),p.id+' exit');const body=vehicleBody({...p,id:'van'});assert.ok(!blocks.some(c=>overlaps(body,{x:c.x,z:c.z,width:c.w*2,length:c.d*2,angle:0},.08)),p.id+' body');}
 for(const [x,z] of WORK.cleaning)assert.ok(free(x,z,.1),'sack inside fixture');
});
test('full van body and other owned cars block driving even when the centre is clear',()=>{
 const van={id:'van',x:0,z:0,angle:0};const w={model:{s:{fleet:[van]}},colliders:[{x:0,z:3,w:1,d:.1}],traffic:{blocksVehicle:()=>false}};
 assert.equal(World.prototype.canDrive.call(w,van,0,1,0),false);assert.equal(World.prototype.canDrive.call(w,van,0,-1,0),true);w.colliders=[];w.model.s.fleet.push({id:'car',x:0,z:4,angle:0});assert.equal(World.prototype.canDrive.call(w,van,0,1,0),false);
});
test('indoor camera clears round tables and never crosses the floor looking down',()=>{const room={minX:-10,maxX:10,minZ:-10,maxZ:10,ceiling:3.5};const view=new CameraRig().update({x:0,y:0,z:0},0,0,4,.016,[{x:0,z:2,radius:.6,h:2}],room);assert.ok(view.position.z<1.2);const down=new CameraRig().update({x:0,y:0,z:0},0,-2,6,.016,[],room);assert.ok(down.position.y>=.24);});
