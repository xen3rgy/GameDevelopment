import {WORLD_BOUNDS,DISTRICT_FIXTURES,exteriorContains} from '../dist/city-layout.js';
import test from 'node:test';
import assert from 'node:assert/strict';
import {GameModel,newGame,validateSave,SAVE_KEY} from '../dist/model.js';
import {insertItem,itemCount} from '../dist/inventory.js';
import {WORK,LOCATIONS,BUILDINGS} from '../dist/data.js';
import {moveWithCollision,stepVehicle,segmentClear} from '../dist/movement.js';
import {CityNavigation} from '../dist/navigation.js';
import {readSavedGame,writeSavedGame,BACKUP_KEY} from '../dist/persistence.js';
function atHome(){const m=new GameModel(newGame(true));m.rent('room');m.s.inside=true;m.s.interior='home';m.s.position={x:300,z:3};return m}

test('storage transfers preserve counts and failed withdrawals lose no items',()=>{
 const m=atHome();m.add('water',6);assert.ok(m.transfer(0,'deposit',4));assert.equal(m.count('water'),2);assert.equal(itemCount(m.s.storage,'water'),4);m.add('bottle',189);const before=JSON.stringify(m.s);assert.equal(m.transfer(0,'withdraw',4),false);assert.equal(JSON.stringify(m.s.inventory),JSON.stringify(JSON.parse(before).inventory));assert.equal(itemCount(m.s.storage,'water'),4);
 m.remove('bottle',189);assert.ok(m.transfer(0,'withdraw',4));assert.equal(m.count('water'),6);assert.equal(m.s.storage.length,0);
 m.add('parcel');assert.equal(m.transfer(1,'deposit'),false);m.s.inside=false;assert.equal(m.transfer(0,'deposit'),false)
});
test('cooking consumes combined inventories, advances time, and produces two meals',()=>{
 const m=atHome();m.add('pasta');insertItem(m.s.storage,'vegetables',1,32,100);const time=m.s.minute;assert.ok(m.cook('pasta'));assert.equal(m.count('meal'),2);assert.equal(m.count('pasta'),0);assert.equal(itemCount(m.s.storage,'vegetables'),0);assert.equal(m.s.minute,time+30);assert.equal(m.s.stats.cooked,1);assert.doesNotThrow(()=>validateSave(m.s))
});
test('cooking failure is atomic and overflow goes to the fridge',()=>{
 const m=atHome();m.add('pasta');const before=structuredClone(m.s);assert.equal(m.cook('pasta'),false);assert.deepEqual(m.s.inventory,before.inventory);assert.equal(m.s.minute,before.minute);
 m.remove('pasta');m.add('bottle',200);insertItem(m.s.storage,'pasta',1,32,100);insertItem(m.s.storage,'vegetables',1,32,100);assert.ok(m.cook('pasta'));assert.equal(itemCount(m.s.fridge,'meal'),2);assert.equal(m.count('meal'),0)
});
test('legacy saves migrate without losing progression, drops, active jobs or vehicles',()=>{
 const m=new GameModel(newGame(true));m.buyVehicle('car');m.startJob('warehouse');m.s.position={...WORK.crate};m.work('crate');m.add('water');m.drop(0);const legacy=structuredClone(m.s);legacy.version=1;delete legacy.storage;delete legacy.stats.cooked;delete legacy.vehicle.angle;delete legacy.vehicle.speed;delete legacy.job.vehicleUsed;
 const updated=validateSave(legacy);assert.equal(updated.version,3);assert.equal(updated.money,legacy.money);assert.equal(updated.vehicle.id,'car');assert.equal(updated.job.carrying,true);assert.equal(updated.drops.length,1);assert.deepEqual(updated.storage,[]);assert.equal(legacy.version,1);assert.equal(updated.vehicle.speed,0)
});
test('local backup restores damaged primary while a failed write preserves the existing save',()=>{
 const values=new Map(),storage={getItem:k=>values.get(k)||null,setItem:(k,v)=>values.set(k,v)};const s=newGame();writeSavedGame(storage,s);s.money=1000;writeSavedGame(storage,s);assert.equal(JSON.parse(values.get(BACKUP_KEY)).money,0);values.set(SAVE_KEY,'bad json');const restored=readSavedGame(storage);assert.ok(restored.recovered);assert.equal(restored.state.money,0);const bad=structuredClone(s);bad.storage=[{id:'parcel',count:1}];assert.throws(()=>writeSavedGame(storage,bad));assert.equal(values.get(SAVE_KEY),'bad json')
});
test('job payments require the actual work and reject repeated or remote steps',()=>{
 const m=new GameModel();m.startJob('warehouse');assert.equal(m.finishJob(),false);assert.equal(m.work('shelf',0),false);assert.equal(m.work('crate'),false);m.s.position={...WORK.crate};assert.ok(m.work('crate'));assert.equal(m.work('crate'),false);m.s.position={...WORK.shelves[0]};assert.ok(m.work('shelf',0));assert.equal(m.work('shelf',0),false);assert.equal(m.s.job.progress,1);assert.equal(m.s.money,0);
 m.cancelJob();m.startJob('courier');assert.equal(m.work('deliver'),false);const target=LOCATIONS.find(l=>l.id===m.s.job.target);m.s.position={x:target.x,z:target.z};m.remove('parcel');assert.equal(m.work('deliver'),false)
});
test('starvation is independent of frame size and only damages after reserve depletion',()=>{
 const one=new GameModel(),many=new GameModel();for(const m of [one,many]){m.s.needs.hunger=.5;m.s.needs.thirst=.5}one.advance(20);for(let i=0;i<200;i++)many.advance(.1);assert.ok(Math.abs(one.s.needs.health-many.s.needs.health)<1e-8);assert.ok(one.s.needs.health>99.7);assert.ok(one.s.needs.health<100)
});
test('collision substeps stop tunnelling, while walkers slide along walls',()=>{
 const free=(x,z,r)=>!(x+r>1&&x-r<2&&Math.abs(z)<10+r);let result=moveWithCollision({x:0,z:0},20,0,free,.35,false);assert.ok(result.x<=.65);assert.ok(result.blocked);result=moveWithCollision({x:0,z:0},2,3,free,.35,true);assert.ok(result.x<=.65);assert.ok(result.z>2.9)
});
test('vehicles accelerate, steer only while moving, brake and stop without fuel',()=>{
 const free=()=>true;let car={id:'car',x:0,z:0,angle:Math.PI,speed:0,fuel:100,condition:100};let r=stepVehicle(car,{left:true},.1,free,17);assert.equal(r.angle,Math.PI);r=stepVehicle(car,{forward:true},.1,free,17);assert.equal(r.speed,.5);assert.ok(r.z<0);car={...car,...r};r=stepVehicle(car,{left:true,forward:true},.1,free,17);assert.ok(r.angle>Math.PI);car={...car,...r};r=stepVehicle(car,{brake:true},.2,free,17);assert.equal(r.speed,0);car={...car,speed:0,fuel:0};r=stepVehicle(car,{forward:true},.1,free,17);assert.equal(r.speed,0)
});
test('vehicle steering cannot rotate the body into a blocked neighbour while stopped at its edge',()=>{
 const car={id:'car',x:0,z:0,angle:0,speed:2,fuel:100,condition:100},can=(x,z,r,angle)=>Math.abs(angle)<1e-10;
 const r=stepVehicle(car,{left:true,forward:true},.1,can,17);assert.equal(r.angle,0);assert.ok(r.z>0);
});
test('pedestrian navigation reaches every game location without crossing buildings',()=>{
 const colliders=BUILDINGS.map(([x,z,w,d])=>({x,z,w:w/2+.35,d:d/2+.35})).concat(DISTRICT_FIXTURES);const free=(x,z,r=.35)=>exteriorContains(x,z,r)&&!colliders.some(c=>Math.abs(x-c.x)<c.w+r&&Math.abs(z-c.z)<c.d+r);const nav=new CityNavigation(free,4,WORLD_BOUNDS);
 for(const target of LOCATIONS){const route=nav.find({x:-27,z:-4},target);assert.ok(route.length>1,target.id+' has no route');for(let i=1;i<route.length;i++)assert.ok(segmentClear(route[i-1],route[i],free,.4),target.id+' crosses a building')}
});
test('partial business stock orders charge only for actual delivered days',()=>{const m=new GameModel(newGame(true));m.acquire('cafe');m.s.businesses.cafe.stock=6;const cash=m.s.money;m.manage('cafe','stock');assert.equal(m.s.businesses.cafe.stock,7);assert.equal(cash-m.s.money,834)});
