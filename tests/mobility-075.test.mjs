import test from 'node:test';
import assert from 'node:assert/strict';
import {GameModel,newGame,validateSave} from '../dist/model.js';
import {BUILDINGS} from '../dist/data.js';
import {allVehicles,usedOffers,trunkLimits,repairPrice,nearestParkingSpot,parkingSpotOccupied,ensureMobility,PARKING_SPOTS} from '../dist/mobility.js';
import {TRANSIT_STOPS,transitOffer,displayMinutes,clockText,resolveTransitArrival} from '../dist/transit.js';
import {transitPanel} from '../dist/mobility-ui.js';
import {exteriorContains,DISTRICT_FIXTURES} from '../dist/city-layout.js';
import {NEIGHBORHOOD_FIXTURES} from '../dist/neighborhood-layout.js';
import {PROMENADE_FIXTURES} from '../dist/pedestrian-layout.js';
import {CITY_CHARACTER_FIXTURES} from '../dist/city-character-layout.js';

const blockers=[
 ...BUILDINGS.map(([x,z,w,d])=>({x,z,w:w/2,d:d/2})),
 ...DISTRICT_FIXTURES,...NEIGHBORHOOD_FIXTURES,...PROMENADE_FIXTURES,...CITY_CHARACTER_FIXTURES
];
const clear=(x,z,r=.35)=>exteriorContains(x,z,r)&&!blockers.some(b=>Math.abs(x-b.x)<b.w+r&&Math.abs(z-b.z)<b.d+r);
const button=(text,action,arg)=>'<button data-action="'+action+'" data-arg="'+arg+'">'+text+'</button>';

test('0.7.5 owns several vehicles and switches them at Mobilwerk',()=>{
 const s=newGame(true),m=new GameModel(s);s.position={x:76,z:12};
 assert.ok(m.buyVehicle('bike'));assert.ok(m.buyVehicle('car'));assert.equal(allVehicles(s).length,2);
 const car=s.garageVehicles.find(v=>v.id==='car');assert.ok(car);assert.ok(m.activateVehicle(car.uid));assert.equal(s.vehicle.id,'car');assert.equal(s.garageVehicles.length,1);assert.equal(s.garageVehicles[0].id,'bike');
});

test('0.7.5 used offers are deterministic and keep their actual purchase state',()=>{
 const s=newGame(true),m=new GameModel(s);s.position={x:76,z:12};const a=usedOffers(s),b=usedOffers(s);
 assert.deepEqual(a,b);assert.ok(a.every(o=>o.price>0&&o.price<({car:90000,van:180000,sport:650000}[o.id])));
 assert.ok(a.every(o=>o.mileage>=38000&&o.mileage<=183000));const offer=a[0];assert.ok(m.buyUsedVehicle(offer.offerId));assert.equal(s.vehicle.used,true);assert.equal(s.vehicle.condition,offer.condition);assert.equal(s.vehicle.purchasePrice,offer.price);assert.ok(!usedOffers(s).some(o=>o.offerId===offer.offerId));
});

test('0.7.5 trunk transfers are atomic and bike rack stays useful early',()=>{
 const s=newGame(true),m=new GameModel(s);s.position={x:76,z:9};m.buyVehicle('bike');m.add('water',2);
 assert.equal(trunkLimits(s.vehicle).slots,2);assert.ok(m.vehicleStorage(0,'deposit',2));assert.equal(s.inventory.length,0);assert.equal(s.vehicle.trunk[0].count,2);
 assert.ok(m.vehicleStorage(0,'withdraw',1));assert.equal(s.vehicle.trunk[0].count,1);assert.equal(m.count('water'),1);
});

test('0.7.5 insurance costs money daily and reduces repair price',()=>{
 const s=newGame(true),m=new GameModel(s);s.position={x:76,z:12};m.buyVehicle('car');const v=s.vehicle;v.condition=50;
 const uninsured=repairPrice(v);assert.ok(m.serviceVehicle('insurance',v.uid));assert.equal(v.insured,true);assert.ok(repairPrice(v)<uninsured);
 const before=s.money;m.advance(1440,true);assert.ok(s.money<before);assert.ok(s.mobility.insurancePaid>0);
});

test('0.7.5 parking snaps an exited vehicle to a marked space',()=>{
 const s=newGame(true),m=new GameModel(s);s.position={x:76,z:9};m.buyVehicle('car');const spot=nearestParkingSpot(s.vehicle,10);assert.ok(spot);assert.ok(m.parkVehicle(spot.id));assert.equal(s.vehicle.parkingSpot,spot.id);
});

test('0.7.5 cannot park two owned vehicles on the same marked space',()=>{
 const s=newGame(true),m=new GameModel(s);s.position={x:76,z:12};m.buyVehicle('bike');m.buyVehicle('car');
 const stored=s.garageVehicles[0],spot=stored.parkingSpot;assert.ok(parkingSpotOccupied(s,spot,s.vehicle.uid));
 s.vehicle.x=68.5;s.vehicle.z=8.4;s.position={x:68.5,z:10};
 assert.equal(m.parkVehicle(spot),false);assert.notEqual(s.vehicle.parkingSpot,spot);
 assert.doesNotThrow(()=>validateSave(JSON.parse(JSON.stringify(s))));
});


test('0.7.5 marked parking never places the walking player inside static geometry',()=>{
 for(const spot of PARKING_SPOTS){
  const exit={x:spot.x+Math.cos(spot.angle)*2,z:spot.z-Math.sin(spot.angle)*2};
  assert.ok(clear(exit.x,exit.z,.35),spot.id+' player exit blocked at '+exit.x+','+exit.z);
 }
});

test('0.7.5 mobility migration repairs duplicate garage bays and stale active reservations',()=>{
 const s=newGame(true),m=new GameModel(s);s.position={x:76,z:12};m.buyVehicle('bike');m.buyVehicle('car');m.buyVehicle('van');
 const spot=s.garageVehicles[0].parkingSpot;s.garageVehicles[1].parkingSpot=spot;s.garageVehicles[1].x=s.garageVehicles[0].x;s.garageVehicles[1].z=s.garageVehicles[0].z;
 s.vehicle.parkingSpot=spot;s.vehicle.x=40;s.vehicle.z=40;ensureMobility(s);
 const spots=s.garageVehicles.map(v=>v.parkingSpot);assert.equal(new Set(spots).size,spots.length);assert.equal(s.vehicle.parkingSpot,null);assert.doesNotThrow(()=>validateSave(JSON.parse(JSON.stringify(s))));
});

test('0.7.5 every public transport arrival is clear of static world collision',()=>{
 for(const stop of Object.values(TRANSIT_STOPS)){
  assert.ok(stop.arrival,stop.id+' has an arrival');
  assert.ok(clear(stop.arrival.x,stop.arrival.z,.35),stop.id+' arrival blocked at '+stop.arrival.x+','+stop.arrival.z);
  assert.ok(Math.hypot(stop.arrival.x-stop.x,stop.arrival.z-stop.z)<=3.4,stop.id+' arrival cannot reach its stop again');
 }
});

test('0.7.5 fractional game time never leaks long decimals into transit UI',()=>{
 const s=newGame(true);s.position={x:-175,z:-31};s.minute=481.234567890123;
 const offer=transitOffer(s,'bus2','station');assert.ok(offer.wait%1!==0);
 assert.equal(displayMinutes(offer.wait),19);assert.equal(clockText(500.333333333333),'08:20');
 const html=transitPanel(s,'station',button);
 assert.doesNotMatch(html,/\d+\.\d{2,}\s*Spielminuten/);
 assert.doesNotMatch(html,/481\.234/);
});

test('0.7.5 transit arrival moves off a dynamic obstacle instead of trapping the player',()=>{
 const start={x:12,z:12,angle:Math.PI},safe=resolveTransitArrival(start,(x,z)=>Math.hypot(x-12,z-12)>1.2&&x>8&&x<16&&z>8&&z<16);
 assert.ok(Math.hypot(safe.x-start.x,safe.z-start.z)>=1.2);assert.equal(safe.angle,start.angle);
});

test('0.7.5 scheduled transit advances the shared clock, charges fare and exits on clear pavement',()=>{
 const s=newGame(true),m=new GameModel(s);s.position={x:-175,z:-31};s.minute=481.23456789;const offer=transitOffer(s,'bus2','station'),before=s.money;
 assert.ok(offer.wait>=0);assert.ok(m.travelTransit('bus2','station'));assert.equal(s.money,before-offer.fare);assert.equal(s.mobility.transitTrips,1);
 assert.deepEqual(s.position,{x:offer.arrival.x,z:offer.arrival.z});assert.equal(s.angle,offer.arrival.angle);assert.ok(clear(s.position.x,s.position.z,.35));
 assert.doesNotThrow(()=>validateSave(JSON.parse(JSON.stringify(s))));
});
