import test from 'node:test';
import assert from 'node:assert/strict';
import {GameModel,newGame,validateSave} from '../dist/model.js';
import {stepVehicle} from '../dist/movement.js';
import {VEHICLES} from '../dist/data.js';
import {breakdownStatus,resetIgnition,roadsideAction,roadsideAvailable,roadsidePrice,roadsidePanel} from '../dist/vehicle-breakdown.js?v=0.8.1';
import {serviceBay} from '../dist/service-layout.js';
import {beginService,tickService} from '../dist/vehicle-service.js';
const setup=(id='car')=>{const m=new GameModel(newGame(true));m.s.position={x:76,z:12};assert.ok(m.buyVehicle(id));m.s.position={x:m.s.vehicle.x,z:m.s.vehicle.z};m.s.vehicle.condition=10;return m;};
const drive=(v,input,seconds)=>{for(let i=0;i<seconds*60;i++)Object.assign(v,stepVehicle(v,input,1/60,()=>true,VEHICLES[v.id].speed));return v.speed;};
const space={vehicle:()=>true,player:()=>true};
test('healthy stock speed; all motor classes suffer delayed start and bounded limp speed; bike unaffected',()=>{
 for(const id of ['car','van','sport']){
  const v=setup(id).s.vehicle;assert.equal(drive(v,{forward:true},2),0);assert.match(breakdownStatus(v).text,/STARTPROBLEM/);
  assert.ok(drive(v,{forward:true},1)>0);drive(v,{forward:true},20);assert.ok(Math.abs(v.speed-VEHICLES[id].speed*.5)<1e-8);
  drive(v,{brake:true},2);assert.ok(drive(v,{forward:true},.1)>0,'traffic stop keeps ignition running');
  v.speed=0;resetIgnition(v);assert.equal(drive(v,{forward:true},1),0,'new entry restarts ignition');
  v.condition=100;assert.ok(Math.abs(drive(v,{forward:true},1)-5)<1e-8);
  v.condition=30;drive(v,{forward:true},20);assert.ok(Math.abs(v.speed-VEHICLES[id].speed*.72)<1e-8);
 }
 const v=setup('bike').s.vehicle;v.condition=0;assert.equal(breakdownStatus(v).level,0);assert.ok(drive(v,{forward:true},1)>0);
});
test('failed motor cannot accelerate, still coasts and brakes; releasing starter resets delay; no fuel cannot start',()=>{
 const v=setup().s.vehicle;drive(v,{forward:true},2);drive(v,{},.1);assert.equal(drive(v,{forward:true},1),0);
 v.condition=5;assert.equal(drive(v,{forward:true},5),0);v.speed=10;assert.ok(drive(v,{forward:true},1)<10);assert.equal(drive(v,{brake:true},1),0);
 v.condition=100;v.fuel=0;assert.equal(drive(v,{forward:true},5),0);
});
test('toolset purchase, one-use outside repair and caps are atomic and save-compatible',()=>{
 const m=setup(),v=m.s.vehicle,money=m.s.money;assert.ok(roadsideAction(m,v.uid,'kit'));assert.equal(m.s.money,money-3500);
 m.s.riding=true;assert.equal(roadsideAction(m,v.uid,'repair'),false);m.s.riding=false;
 assert.ok(roadsideAction(m,v.uid,'repair'));assert.equal(v.condition,25);assert.equal(m.s.inventory.length,0);assert.equal(roadsideAction(m,v.uid,'repair'),false);
 assert.ok(roadsideAction(m,v.uid,'kit'));const loaded=new GameModel(validateSave(JSON.parse(JSON.stringify(m.s))));assert.equal(loaded.s.vehicle.condition,25);assert.equal(loaded.count('toolkit'),1);
 m.s.money=0;const inventory=JSON.stringify(m.s.inventory);assert.equal(roadsideAction(m,v.uid,'kit'),false);assert.equal(JSON.stringify(m.s.inventory),inventory);
 m.s.money=100000;m.s.inventory=Array.from({length:16},()=>({id:'parcel',count:1}));assert.equal(roadsideAction(m,v.uid,'kit'),false);assert.equal(m.s.money,100000);
});
test('mobile aid is priced by class, limited, independent and restores movement without changing fuel, credit or tuning',()=>{
 for(const id of ['car','van','sport']){const m=setup(id),v=m.s.vehicle;v.condition=0;v.claimCredit=123;const other={...v,uid:'other',x:v.x+20};m.s.fleet.push(other);const before=JSON.stringify({fuel:v.fuel,customization:v.customization,upgrades:v.upgrades,trunk:v.trunk,claimCredit:v.claimCredit}),money=m.s.money;
 assert.ok(roadsideAction(m,v.uid,'aid'));assert.equal(v.condition,40);assert.equal(m.s.money,money-roadsidePrice(v,'aid'));assert.equal(other.condition,0);assert.ok(drive(v,{forward:true},1)>0);v.speed=0;m.s.position={x:v.x,z:v.z};assert.equal(roadsideAction(m,v.uid,'aid'),false);
 assert.equal(JSON.stringify({fuel:v.fuel,customization:v.customization,upgrades:v.upgrades,trunk:v.trunk,claimCredit:v.claimCredit}),before);
 }
 assert.equal(roadsidePrice({id:'sport'},'aid'),18000);
});
test('tow checks both arrival spaces before charging, transports player and car to repair bay, service cures fault',()=>{
 const m=setup(),v=m.s.vehicle,before=JSON.stringify(m.s);assert.equal(roadsideAction(m,v.uid,'tow',{...space,vehicle:()=>false}),false);assert.equal(JSON.stringify(m.s),before);
 assert.equal(roadsideAction(m,v.uid,'tow',{...space,player:()=>false}),false);assert.equal(JSON.stringify(m.s),before);
 const money=m.s.money;m.s.riding=true;assert.ok(roadsideAction(m,v.uid,'tow',space));assert.equal(m.s.money,money-7500);assert.equal(m.s.riding,false);assert.equal(v.condition,10);assert.equal(v.x,serviceBay('autoWorkshop').vehicle.x);
 assert.equal(roadsideAction(m,v.uid,'tow',space),false);assert.ok(beginService(m,'autoWorkshop'));tickService(m,6);assert.equal(v.condition,100);assert.equal(breakdownStatus(v).level,0);assert.ok(drive(v,{forward:true},.1)>0);
});
test('moving, remote, stored, busy and bicycle service are refused; ordinary courier job does not strand driver',()=>{
 const m=setup(),v=m.s.vehicle;for(const change of [{inside:true},{transit:{}},{vehicleService:{}},{job:{carrying:true}},{dailyLife:{action:{}}}]){const s={...m.s,...change};assert.equal(roadsideAvailable(s,v),false);}
 assert.ok(roadsideAvailable({...m.s,job:{type:'courier'}},v));v.speed=1;assert.equal(roadsideAction(m,v.uid,'aid'),false);v.speed=0;v.stored=true;assert.equal(roadsideAction(m,v.uid,'aid'),false);v.stored=false;m.s.position.x+=50;assert.equal(roadsideAction(m,v.uid,'aid'),false);
 const bike=setup('bike');assert.equal(roadsideAction(bike,bike.s.vehicle.uid,'aid'),false);
 const html=roadsidePanel(m.s,v.uid,(label)=>label);assert.match(html,/Abschleppdienst/);assert.match(html,/90,00/);assert.match(html,/35,00/);
});
test('empty fuel is towed to a pump, full repair resets a starter warning immediately, poor funds never move vehicle',()=>{
 const m=setup(),v=m.s.vehicle;drive(v,{forward:true},1);v.condition=100;assert.equal(breakdownStatus(v).level,0);
 v.fuel=0;m.s.money=0;const before=JSON.stringify(m.s);assert.equal(roadsideAction(m,v.uid,'tow',space),false);assert.equal(roadsideAction(m,v.uid,'aid'),false);assert.equal(JSON.stringify(m.s),before);
 m.s.money=100000;assert.ok(roadsideAction(m,v.uid,'tow',space));assert.equal(v.x,serviceBay('pump1').vehicle.x);assert.equal(v.fuel,0);assert.ok(beginService(m,'pump1'));tickService(m,8);assert.equal(v.fuel,100);
});
