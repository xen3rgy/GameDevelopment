import test from 'node:test';
import assert from 'node:assert/strict';
import {CameraRig,groundHeight,ROOMS,canWalkRoom,SHOP_FIXTURES,rayBox} from '../dist/spatial.js';
import {GameModel,newGame,validateSave,SAVE_KEY} from '../dist/model.js';
import {SHOP_POINTS,LOCATIONS} from '../dist/data.js';
import {readSavedGame} from '../dist/persistence.js';
const near=(a,b,tolerance=1e-8)=>assert.ok(Math.abs(a-b)<tolerance,`${a} != ${b}`);
function inShop(){const m=new GameModel(newGame(true));const door=LOCATIONS.find(p=>p.id==='market');m.s.position={x:door.x,z:door.z};assert.ok(m.enterInterior('shop'));return m}
function pick(m,id,shelf){m.s.position={x:SHOP_POINTS[shelf].x,z:SHOP_POINTS[shelf].z};assert.ok(m.basketAdd(id))}

test('camera follows walking at a constant offset without alternating snaps or lag',()=>{
 for(const fps of [20,60,144]){const rig=new CameraRig();let old=null;for(let i=0;i<fps*3;i++){const p={x:i*4.25/fps,y:0,z:-4},v=rig.update(p,.7,.35,8,1/fps);near(v.position.x-p.x,Math.sin(.7)*Math.cos(.35)*7.95);if(old)near(v.position.x-old.x,4.25/fps);old=v.position}}
});
test('kerb camera height is smoothed and remains frame-rate independent',()=>{
 const heights=[];for(const fps of [20,60,144]){const rig=new CameraRig();rig.update({x:25,y:0,z:0},0,.35,8,0);let previous=1.4;for(let i=0;i<fps/2;i++){const v=rig.update({x:25,y:.25,z:9},0,.35,8,1/fps);assert.ok(v.anchor.y>=previous&&v.anchor.y<1.65);previous=v.anchor.y}heights.push(previous)}near(heights[0],heights[1]);near(heights[1],heights[2])
});
test('camera cannot pass walls and extends monotonically when obstruction clears',()=>{
 const rig=new CameraRig(),p={x:0,y:0,z:0},wall={x:0,z:4,w:10,d:.5,h:12};rig.update(p,0,.35,8,0);let v=rig.update(p,0,.35,8,1/60,[wall]);assert.ok(v.position.z<3.35);let previous=v.distance;for(let i=0;i<120;i++){v=rig.update(p,0,.35,8,1/60);assert.ok(v.distance>=previous&&v.distance<8);assert.ok(v.distance-previous<.55);previous=v.distance}assert.ok(v.distance>7.9);
 rig.reset();v=rig.update({x:340,y:.02,z:47},0,.35,5.5,0,[],ROOMS.shop);assert.ok(v.position.x>333&&v.position.z<49);assert.ok(v.position.y<ROOMS.shop.ceiling)
});
test('all room camera directions stay within walls and ceiling, including jumps',()=>{
 for(const room of Object.values(ROOMS))for(const position of [room.spawn,{x:room.minX+.36,z:room.minZ+.36},{x:room.maxX-.36,z:room.maxZ-.36}])for(let angle=0;angle<Math.PI*2;angle+=.08)for(const pitch of [.06,.35,.95])for(const y of [.02,.9]){const v=new CameraRig().update({...position,y},angle,pitch,14,0,[],room).position;assert.ok(v.x>room.minX&&v.x<room.maxX);assert.ok(v.z>room.minZ&&v.z<room.maxZ);assert.ok(v.y<room.ceiling)}
});
test('road, crossing, pavement, park and indoor feet match actual surface heights',()=>{
 near(groundHeight(25,0),.14);near(groundHeight(0,0),.14);near(groundHeight(25,9),.3);near(groundHeight(10,40),.3);near(groundHeight(70,88),.18);near(groundHeight(76,88),.275);near(groundHeight(300,0,'home'),.07);near(groundHeight(340,40,'shop'),.07);near(groundHeight(25,0)-.05,.09);
});
test('shop shelves and till are reachable by walking around fixtures from the entrance',()=>{
 const r=ROOMS.shop,start={...r.spawn},step=.25,queue=[start],seen=new Set(),reachable=[];
 for(let i=0;i<queue.length;i++){const p=queue[i];reachable.push(p);for(const [dx,dz] of [[step,0],[-step,0],[0,step],[0,-step]]){const n={x:p.x+dx,z:p.z+dz},key=n.x+','+n.z;if(!seen.has(key)&&canWalkRoom('shop',n.x,n.z)){seen.add(key);queue.push(n)}}}
 for(const point of Object.values(SHOP_POINTS)){assert.ok(canWalkRoom('shop',point.x,point.z),point.name+' is blocked');assert.ok(reachable.some(p=>Math.hypot(p.x-point.x,p.z-point.z)<.3),point.name+' cannot be reached')}
 for(const fixture of SHOP_FIXTURES)assert.equal(canWalkRoom('shop',fixture.x,fixture.z),false);
});
test('shopping requires physical shelves and payment at the till, with no double charge',()=>{
 const m=inShop(),cash=m.s.money;assert.equal(m.basketAdd('water'),false);pick(m,'water','drinks');pick(m,'sandwich','food');assert.equal(m.count('water'),0);assert.equal(m.s.money,cash);assert.equal(m.checkout(),false);assert.equal(m.basketTotal,350);m.s.position={x:SHOP_POINTS.checkout.x,z:SHOP_POINTS.checkout.z};assert.ok(m.checkout());assert.equal(m.s.money,cash-350);assert.equal(m.count('water'),1);assert.equal(m.count('sandwich'),1);assert.equal(m.checkout(),false);assert.equal(m.s.money,cash-350);assert.doesNotThrow(()=>validateSave(m.s));
});
test('failed checkout is atomic for cash, backpack and basket',()=>{
 const m=inShop();pick(m,'water','drinks');pick(m,'sandwich','food');m.s.position={x:SHOP_POINTS.checkout.x,z:SHOP_POINTS.checkout.z};m.s.money=349;const basket=structuredClone(m.s.basket);assert.equal(m.checkout(),false);assert.equal(m.s.money,349);assert.equal(m.s.inventory.length,0);m.s.money=500;m.add('bottle',200);const bag=structuredClone(m.s.inventory);assert.equal(m.checkout(),false);assert.equal(m.s.money,500);assert.deepEqual(m.s.inventory,bag);assert.deepEqual(m.s.basket,basket);
});
test('shop save roundtrip preserves unpaid basket and leaving never grants goods',()=>{
 const m=inShop();pick(m,'water','drinks');const raw=JSON.stringify(m.s),restored=readSavedGame({getItem:key=>key===SAVE_KEY?raw:null});assert.deepEqual(restored.state,m.s);const loaded=new GameModel(restored.state);assert.ok(loaded.leaveInterior());assert.equal(loaded.s.basket.length,0);assert.equal(loaded.count('water'),0);assert.equal(loaded.s.interior,null);assert.equal(loaded.s.inside,false);assert.deepEqual(loaded.s.position,ROOMS.shop.outside);assert.doesNotThrow(()=>validateSave(loaded.s));
});
test('home services stay at home and old home saves migrate without losing progress',()=>{
 const m=new GameModel(newGame(true));m.rent('room');m.s.position={x:-34,z:12};assert.ok(m.enterInterior('home'));m.add('water');assert.ok(m.transfer(0,'deposit'));const legacy=structuredClone(m.s);legacy.version=2;delete legacy.interior;delete legacy.basket;const loaded=validateSave(legacy);assert.equal(loaded.interior,'home');assert.deepEqual(loaded.storage,m.s.storage);assert.equal(loaded.money,m.s.money);assert.equal(legacy.version,2);m.leaveInterior();m.s.position={x:-36,z:-12};m.enterInterior('shop');assert.equal(m.transfer(0,'withdraw'),false);assert.equal(m.cook('pasta'),false);
});
test('collapsed shoppers recover outside with a valid save and no unpaid goods',()=>{
 const m=inShop();pick(m,'water','drinks');m.s.needs.health=0;m.advance(.01);assert.equal(m.s.inside,false);assert.equal(m.s.interior,null);assert.equal(m.s.basket.length,0);assert.equal(m.count('water'),0);assert.doesNotThrow(()=>validateSave(m.s));
});
test('invalid basket imports and remote or invalid interior entry are rejected',()=>{
 const m=inShop();for(const basket of [[{id:'parcel',count:1}],[{id:'water',count:21}],[{id:'water',count:1},{id:'water',count:1}],[{id:'__proto__',count:1}]]){const bad=structuredClone(m.s);bad.basket=basket;assert.throws(()=>validateSave(bad))}const outside=new GameModel();assert.equal(outside.enterInterior('shop'),false);assert.equal(outside.enterInterior('__proto__'),false);outside.s.position={x:-34,z:12};assert.equal(outside.enterInterior('home'),false);
});

test('vegetables and bread can be selected at either matching sales fixture',()=>{
 const m=inShop();for(const [id,point] of [['vegetables','produce'],['bread','bakery'],['vegetables','ingredients'],['bread','ingredients']])pick(m,id,point);
 assert.equal(m.s.basket.find(v=>v.id==='vegetables').count,2);assert.equal(m.s.basket.find(v=>v.id==='bread').count,2);m.s.position={x:SHOP_POINTS.checkout.x,z:SHOP_POINTS.checkout.z};assert.equal(m.basketAdd('bread'),false);const total=m.basketTotal,cash=m.s.money;assert.ok(m.checkout());assert.equal(m.s.money,cash-total);assert.equal(m.count('vegetables'),2);assert.equal(m.count('bread'),2);
});
test('older shopping saves inside new furniture recover at the entrance with their basket',()=>{
 const m=inShop();pick(m,'water','drinks');m.s.position={x:334.8,z:44.6};const loaded=validateSave(m.s);assert.deepEqual(loaded.position,ROOMS.shop.spawn);assert.deepEqual(loaded.basket,m.s.basket);assert.equal(loaded.money,m.s.money);assert.ok(canWalkRoom('shop',loaded.position.x,loaded.position.z));
});
