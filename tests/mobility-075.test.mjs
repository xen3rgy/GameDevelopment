import test from 'node:test';
import assert from 'node:assert/strict';
import {GameModel,newGame,validateSave} from '../dist/model.js';
import {allVehicles,usedOffers,trunkLimits,repairPrice,nearestParkingSpot} from '../dist/mobility.js';
import {transitOffer} from '../dist/transit.js';

test('0.7.5 owns several vehicles and switches them at Mobilwerk',()=>{
 const s=newGame(true),m=new GameModel(s);s.position={x:76,z:12};
 assert.ok(m.buyVehicle('bike'));assert.ok(m.buyVehicle('car'));assert.equal(allVehicles(s).length,2);
 const car=s.garageVehicles.find(v=>v.id==='car');assert.ok(car);assert.ok(m.activateVehicle(car.uid));assert.equal(s.vehicle.id,'car');assert.equal(s.garageVehicles.length,1);assert.equal(s.garageVehicles[0].id,'bike');
});

test('0.7.5 used offers are deterministic and keep their actual purchase state',()=>{
 const s=newGame(true),m=new GameModel(s);s.position={x:76,z:12};const a=usedOffers(s),b=usedOffers(s);
 assert.deepEqual(a,b);assert.ok(a.every(o=>o.price>0&&o.price<({car:90000,van:180000,sport:650000}[o.id])));
 const offer=a[0];assert.ok(m.buyUsedVehicle(offer.offerId));assert.equal(s.vehicle.used,true);assert.equal(s.vehicle.condition,offer.condition);assert.equal(s.vehicle.purchasePrice,offer.price);assert.ok(!usedOffers(s).some(o=>o.offerId===offer.offerId));
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

test('0.7.5 scheduled transit advances the shared clock and charges the fare',()=>{
 const s=newGame(true),m=new GameModel(s);s.position={x:-175,z:-31};s.minute=481;const offer=transitOffer(s,'bus2','station'),before=s.money;
 assert.ok(offer.wait>=0);assert.ok(m.travelTransit('bus2','station'));assert.equal(s.money,before-offer.fare);assert.equal(s.mobility.transitTrips,1);assert.deepEqual(s.position,{x:9,z:9});assert.doesNotThrow(()=>validateSave(JSON.parse(JSON.stringify(s))));
});
