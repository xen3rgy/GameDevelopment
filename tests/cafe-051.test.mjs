import test from 'node:test';
import assert from 'node:assert/strict';
import * as THREE from '../dist/vendor/three.module.js';
import {GameModel,newGame,validateSave} from '../dist/model.js';
import {tickCafe,CAFE_POINTS,CAFE_MENU,guestPlace} from '../dist/cafe.js';
import {CAFE_TABLES,CAFE_FIXTURES,guestPath,guestSeat,pathLength,samplePath,staffPath} from '../dist/cafe-layout.js';
import {canWalkRoom} from '../dist/spatial.js';
import {buildCafe,updateCafe,poseCafeSeated} from '../dist/cafe-interior.js';
import {createCitizen} from '../dist/art.js';
import {World} from '../dist/world.js';
function model(staff=3){const s=newGame(true);s.businesses.cafe={stock:3,staff,price:1,quality:1,marketing:false,open:true,profit:0,sales:0,costs:0};s.minute=600;s.position={x:63,z:-12};const m=new GameModel(s);m.enterInterior('cafe');s.position={...CAFE_POINTS.cafeOffice};assert.ok(m.cafeAction('start'));m.s.cafe.nextGuest=999;tick(m,25);m.s.cafe.nextGuest=3;m.s.cafe.rng=1000;return m}
function tick(m,time){for(let t=0;t<time;t+=.05)tickCafe(m,Math.min(.05,time-t))}
function kit(){const cache=new Map(),mat=c=>{if(!cache.has(c))cache.set(c,new THREE.MeshStandardMaterial({color:c}));return cache.get(c)},boxGeo=new THREE.BoxGeometry(1,1,1),cyl=new THREE.CylinderGeometry(1,1,1,8);return {mat,box(p,x,y,z,w,h,d,c,m){const o=new THREE.Mesh(boxGeo,m||mat(c));o.userData.staticBatch=true;o.position.set(x,y,z);o.scale.set(w,h,d);p.add(o);return o},cylinder(p,x,y,z,r,h,c){const o=new THREE.Mesh(cyl,mat(c));o.userData.staticBatch=true;o.position.set(x,y,z);o.scale.set(r,h,r);p.add(o);return o},sign(){return new THREE.Group()}}}
test('table collisions follow circles, chairs stay solid, and service aisles are clear',()=>{
 for(const [i,p] of CAFE_TABLES.entries()){
  assert.equal(canWalkRoom('cafe',p.x,p.z),false);
  assert.equal(canWalkRoom('cafe',p.x,p.z+1.04),true);
  assert.equal(canWalkRoom('cafe',p.x+.75,p.z+.78),true);
  assert.equal(canWalkRoom('cafe',p.x+1.12,p.z),false);
  const path=staffPath(CAFE_POINTS.cafePrep,CAFE_POINTS['cafeTable'+i]);
  for(let d=0;d<pathLength(path);d+=.1){const q=samplePath(path,d);assert.ok(canWalkRoom('cafe',q.x,q.z),JSON.stringify(q))}
 }
});
test('guest routes avoid every table and all other chairs, then align with their own chair',()=>{
 for(let i=0;i<3;i++)for(const side of [-1,1]){
  const path=guestPath(i,side),seat=guestSeat(i,side),own=CAFE_FIXTURES.find(f=>Math.abs(f.x-seat.x)<.001&&f.z===seat.z);
  for(let d=0;d<pathLength(path);d+=.08){const q=samplePath(path,d);for(const f of CAFE_FIXTURES){if(f===own)continue;assert.equal(f.radius!=null?Math.hypot(q.x-f.x,q.z-f.z)<f.radius+.2:Math.abs(q.x-f.x)<f.w+.2&&Math.abs(q.z-f.z)<f.d+.2,false,JSON.stringify({i,side,q,f}))}}
  assert.deepEqual(path.at(-1),{x:seat.x,z:seat.z});
 }
});
test('sitting and standing transitions keep both feet above the floor',()=>{
 const actor=createCitizen(kit());
 for(let w=0;w<=1.001;w+=.025){poseCafeSeated(actor,w);actor.updateMatrixWorld(true);for(const foot of actor.userData.feet){const b=new THREE.Box3().setFromObject(foot);assert.ok(b.min.y>=.0699,'foot '+b.min.y)}}
 poseCafeSeated(actor);assert.ok(actor.position.y<0);assert.ok(actor.userData.knees.every(k=>k.rotation.x>1.2));
});
test('three visible roles run a complete service, including preparation and physical cleanup',()=>{
 const m=model();let sawCarry=false,sawClean=false;
 for(let t=0;t<185;t+=.1){tickCafe(m,.1);sawCarry ||= m.s.cafe.staffCarry!=null;sawClean ||= m.s.cafe.employees[2]?.task?.kind==='clean';for(const e of m.s.cafe.employees.slice(1).filter(e=>e.status==='working'))assert.ok(canWalkRoom('cafe',e.x,e.z,.25));if(Math.round(t*10)%20===0)assert.doesNotThrow(()=>validateSave(m.s))}
 assert.ok(m.s.cafe.served>=3);assert.ok(sawCarry);assert.ok(sawClean);assert.ok(m.s.cafe.ingredients>0);assert.ok(m.s.cafe.nextGuest>=0);
 tick(m,m.s.cafe.duration-m.s.cafe.elapsed+.1);assert.equal(m.s.cafe.phase,'closed');assert.doesNotThrow(()=>validateSave(m.s));
});
test('a barista makes orders, but cannot remotely clean or take the player tray',()=>{
 const m=model(1);m.s.cafe.dirty[2]=1;tick(m,18);const g=m.s.cafe.guests[0];m.s.position={...CAFE_POINTS['cafeTable'+g.seat]};m.cafeAction('table',g.seat);tick(m,9);
 assert.equal(m.s.cafe.ingredients,CAFE_MENU[g.recipe].cost);m.s.position={...CAFE_POINTS.cafePrep};assert.ok(m.cafeAction('collect'));tick(m,2);assert.equal(m.s.cafe.carrying,true);assert.equal(m.s.cafe.staffCarry,null);assert.ok(m.s.cafe.dirty[2]>0);
 m.s.position={...CAFE_POINTS['cafeTable'+g.seat]};assert.ok(m.cafeAction('table',g.seat));const money=m.s.cafe.revenue;assert.equal(m.cafeAction('guest',g.id),false);assert.equal(m.s.cafe.revenue,money);
});
test('mid-service saves resume without duplicate ingredients or stolen trays; old saves get staff',()=>{
 const m=model();for(let n=0;n<1500&&m.s.cafe.staffCarry==null;n++)tickCafe(m,.05);assert.ok(m.s.cafe.staffCarry);
 m.s.position={...CAFE_POINTS.cafePrep};assert.equal(m.cafeAction('collect'),false);
 const restored=new GameModel(validateSave(m.s));tick(m,15);tick(restored,15);assert.deepEqual(restored.s.cafe,m.s.cafe);
 const old=structuredClone(m.s);delete old.cafe.employees;delete old.cafe.staffCarry;old.cafe.nextGuest=-12;
 const migrated=validateSave(old);assert.equal(migrated.cafe.employees.length,3);assert.equal(migrated.cafe.ingredients,old.cafe.ingredients);assert.equal(migrated.cafe.nextGuest,0);
 const bad=structuredClone(m.s);bad.cafe.employees[0].x=NaN;assert.throws(()=>validateSave(bad),/Café/);
});
test('dynamic crockery survives static batching and visible people match the real team and chairs',()=>{
 const k=kit(),world={scene:new THREE.Scene(),exitDoor(){}};buildCafe(world,k);
 const counts=world.cafeDishes.map(g=>g.children.length);World.prototype.batchStaticGroup.call(world,world.cafe);assert.deepEqual(world.cafeDishes.map(g=>g.children.length),counts);
 const m=model();tick(m,18);updateCafe(world,m.s,.016);assert.equal(world.cafeStaff.filter(m=>m.visible).length,3);assert.equal(world.cafeGuests.size,m.s.cafe.guests.length);
 const g=m.s.cafe.guests[0],actor=world.cafeGuests.get(g.id);
 const p=guestSeat(g.seat,g.side);assert.equal(actor.position.x,p.x);assert.equal(actor.position.z,p.z);assert.equal(actor.rotation.y,p.angle);assert.ok(actor.position.y<0);
 m.s.cafe.dirty[guestPlace(g)]=1;m.s.cafe.dishes[guestPlace(g)]=g.recipe;g.stage='leaving';updateCafe(world,m.s,.016);assert.equal(world.cafeDishes[guestPlace(g)].visible,true);
 m.s.cafe.guests=[];updateCafe(world,m.s,.016);assert.equal(world.cafeGuests.size,0);assert.ok(world.cafeGuestPool.length>0);
 m.s.position={...CAFE_POINTS.cafeOffice};m.cafeAction('finish');tick(m,30);updateCafe(world,m.s,.016);assert.equal(world.cafeStaff.filter(m=>m.visible).length,0);
});
