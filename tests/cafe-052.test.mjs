import test from 'node:test';
import assert from 'node:assert/strict';
import * as THREE from '../dist/vendor/three.module.js';
import {GameModel,newGame,validateSave} from '../dist/model.js';
import {CAFE_MENU,CAFE_POINTS,newCafe,guestPlace,tickCafe,guestWalkDuration} from '../dist/cafe.js';
import {buildCafe,poseCafeSeated,updateCafe,attachCafeTray} from '../dist/cafe-interior.js';
import {createCafeMeal,animateCafeMeal,diningMotion,setMealConsumed} from '../dist/cafe-food.js';
import {createCitizen} from '../dist/art.js';
function kit(){const mats=new Map(),mat=c=>{if(!mats.has(c))mats.set(c,new THREE.MeshStandardMaterial({color:c}));return mats.get(c)};return {mat,box(p,x,y,z,w,h,d,c,m){const o=new THREE.Mesh(new THREE.BoxGeometry(w,h,d),m||mat(c));o.position.set(x,y,z);p.add(o);return o},cylinder(p,x,y,z,r,h,c){const o=new THREE.Mesh(new THREE.CylinderGeometry(r,r,h,16),mat(c));o.position.set(x,y,z);p.add(o);return o},sign(){return new THREE.Group()}}}
function model(staff=0,seed=1000){const s=newGame(true);s.businesses.cafe={stock:3,staff,price:1,quality:1,marketing:false,open:true,profit:0,sales:0,costs:0};s.position={x:63,z:-12};const m=new GameModel(s);assert.ok(m.enterInterior('cafe'));s.position={...CAFE_POINTS.cafeOffice};assert.ok(m.cafeAction('start'));s.cafe.rng=seed;return m}
function tick(m,t){for(let n=0;n<t;n+=.05)tickCafe(m,Math.min(.05,t-n))}
function guest(id,side,recipe,party=1){return {id,side,recipe,party,seat:0,stage:'order',time:0,wait:0,patience:120,price:CAFE_MENU[recipe].price,paid:false,served:false,tipPending:0,paymentWait:0,prepaid:false}}
function serve(m,g){m.s.position={...CAFE_POINTS['cafeTable'+g.seat]};if(g.stage==='order')assert.ok(m.cafeAction('guest',g.id));m.s.position={...CAFE_POINTS.cafePrep};assert.ok(m.cafeAction('prepare',g.id));tick(m,CAFE_MENU[g.recipe].seconds+.1);assert.ok(m.cafeAction('collect'));m.s.position={...CAFE_POINTS['cafeTable'+g.seat]};assert.ok(m.cafeAction('guest',g.id))}
test('oak floor has physically separate surfaces and the visible top matches the foot origin',()=>{
 const world={scene:new THREE.Scene(),exitDoor(){}};buildCafe(world,kit());
 const sub=world.cafe.getObjectByName('cafe-subfloor'),base=new THREE.Box3().setFromObject(sub),boards=[];world.cafe.traverse(m=>{if(m.name==='cafe-floorboard')boards.push(m)});
 assert.equal(boards.length,256);
 for(const board of boards){const b=new THREE.Box3().setFromObject(board);assert.ok(b.max.y-base.max.y>.019);assert.ok(Math.abs(b.max.y-.07)<1e-7)}
 const ray=new THREE.Raycaster(new THREE.Vector3(300.25,2,83),new THREE.Vector3(0,-1,0));world.cafe.updateMatrixWorld(true);const hits=ray.intersectObjects([sub,...boards],false);
 assert.ok(Math.abs(hits[0].point.y-.07)<1e-6);assert.equal(hits[0].object.name,'cafe-floorboard');assert.ok(hits.at(-1).point.y<.051);
});
test('espresso, milk coffee and breakfast have different serving geometry and fit their place setting',()=>{
 const espresso=createCafeMeal('espresso'),latte=createCafeMeal('latte'),breakfast=createCafeMeal('breakfast');
 assert.ok(latte.userData.height>espresso.userData.height*1.5);
 assert.notEqual(espresso.userData.liquid.material,latte.userData.liquid.material);assert.equal(breakfast.userData.portions.length,6);
 for(const meal of [espresso,latte,breakfast]){const b=new THREE.Box3().setFromObject(meal);assert.ok(b.max.x-b.min.x<.5);assert.ok(b.max.z-b.min.z<.47);setMealConsumed(meal,1);assert.ok(meal.userData.portions.every(m=>!m.visible));if(meal.userData.liquid)assert.equal(meal.userData.liquid.visible,false)}
});
test('each meal lifts to the mouth with its hand attached and returns to its original position',()=>{
 for(const recipe of Object.keys(CAFE_MENU)){
  const actor=createCitizen(kit()),meal=createCafeMeal(recipe),scene=new THREE.Scene();scene.add(actor,meal);meal.position.set(.1,.815,recipe==='breakfast'?.68:.59);
  let peak=0;
  for(let t=0;t<=6;t+=.05){poseCafeSeated(actor);animateCafeMeal(actor,meal,{id:3,recipe,stage:'eating',time:CAFE_MENU[recipe].dining-t});scene.updateMatrixWorld(true);peak=Math.max(peak,meal.userData.movable.getWorldPosition(new THREE.Vector3()).y);
   if(diningMotion(t).reach>.999){const hand=actor.userData.elbows[1].localToWorld(new THREE.Vector3(0,-.22,.026)),grip=meal.userData.movable.localToWorld(meal.userData.grip.clone());assert.ok(hand.distanceTo(grip)<.002,recipe)}
  }
  assert.ok(peak>1.2);assert.ok(meal.userData.movable.position.distanceTo(meal.userData.restPosition)<1e-6);
  const g={id:3,recipe,stage:'eating',time:CAFE_MENU[recipe].dining-2.6};poseCafeSeated(actor);animateCafeMeal(actor,meal,g);const position=meal.userData.movable.position.clone();poseCafeSeated(actor);animateCafeMeal(actor,meal,g);assert.deepEqual(meal.userData.movable.position,position);
 }
});
test('seeded arrivals vary party size, seat, order and interval without double-booking seats',()=>{
 const sizes=new Set(),recipes=new Set(),intervals=new Set();
 for(let seed=1;seed<160000;seed+=7919){const m=model(0,seed);tick(m,3.2);const c=m.s.cafe;sizes.add(c.guests.length);c.guests.forEach(g=>recipes.add(g.recipe));intervals.add(Math.round(c.nextGuest));assert.equal(new Set(c.guests.map(guestPlace)).size,c.guests.length);if(c.guests.length===2){assert.equal(c.guests[0].party,c.guests[1].party);assert.ok(c.guests[1].time-guestWalkDuration(c.guests[1].seat,c.guests[1].side)>.3)}assert.doesNotThrow(()=>validateSave(m.s))}
 assert.deepEqual([...sizes].sort(),[1,2]);assert.equal(recipes.size,3);assert.ok(intervals.size>4);
});
test('two people have separate orders, dishes and receipts; wrong or repeated service never credits money',()=>{
 const m=model(),c=m.s.cafe,a=guest(1,-1,'espresso'),b=guest(2,1,'breakfast');c.guests=[a,b];c.serial=2;c.nextGuest=999;
 m.s.position={...CAFE_POINTS.cafeTable0};assert.ok(m.cafeAction('guest',1));assert.ok(m.cafeAction('guest',2));m.s.position={...CAFE_POINTS.cafePrep};assert.ok(m.cafeAction('prepare',1));tick(m,4.1);assert.ok(m.cafeAction('collect'));m.s.position={...CAFE_POINTS.cafeTable0};
 assert.equal(m.cafeAction('guest',2),false);assert.equal(c.revenue,0);assert.equal(c.tray,1);assert.ok(m.cafeAction('guest',1));assert.equal(c.revenue,0);assert.equal(m.cafeAction('guest',1),false);
 tick(m,17);assert.equal(a.stage,'paying');assert.ok(m.cafeAction('guest',1));assert.equal(a.stage,'finished');assert.equal(b.stage,'accepted');serve(m,b);
 assert.equal(c.revenue,340);tick(m,CAFE_MENU.breakfast.dining+.1);assert.equal(b.stage,'paying');assert.ok(m.cafeAction('guest',2));
 assert.equal(c.revenue,1230);assert.equal(c.served,2);assert.deepEqual(c.receipts.map(r=>r.amount),[340,890]);assert.equal(c.dishes[0],'espresso');assert.equal(c.dishes[1],'breakfast');assert.doesNotThrow(()=>validateSave(m.s));
 tick(m,guestWalkDuration(0)+1);assert.ok(c.dirty[0]>0&&c.dirty[1]>0);assert.ok(m.cafeAction('clean',0));assert.equal(c.dishes[0],null);assert.equal(c.dishes[1],'breakfast');
 const bad=structuredClone(m.s);bad.cafe.receipts[0].amount++;assert.throws(()=>validateSave(bad),/Einzelrechnungen/);
});
test('legacy pair orders split into two people without charging ingredients twice or changing old takings',()=>{
 const m=model(),c=m.s.cafe;c.guests=[{id:1,seat:0,recipe:'espresso',stage:'accepted',time:0,wait:2,patience:75,price:680}];c.serial=1;c.prep={id:1,remaining:2};c.ingredients=120;c.dirty=[0,0,0];c.revenue=2000;c.tips=100;c.served=2;
 for(const key of ['schema','rng','dishes','receipts','openingRevenue','openingTips'])delete c[key];
 const saved=validateSave(m.s),resumed=new GameModel(saved);assert.equal(saved.cafe.guests.length,2);assert.deepEqual(saved.cafe.guests.map(g=>g.price),[340,340]);assert.equal(saved.cafe.served,4);saved.cafe.nextGuest=999;
 tick(resumed,2.1);saved.position={...CAFE_POINTS.cafePrep};assert.ok(resumed.cafeAction('collect'));saved.position={...CAFE_POINTS.cafeTable0};assert.ok(resumed.cafeAction('guest',1));serve(resumed,saved.cafe.guests[1]);
 tick(resumed,CAFE_MENU.espresso.dining+.1);for(const g of saved.cafe.guests)if(g.stage==='paying')assert.ok(resumed.cafeAction('guest',g.id));
 assert.equal(saved.cafe.ingredients,120);assert.equal(saved.cafe.revenue,2680);assert.equal(saved.cafe.openingRevenue,2000);assert.equal(saved.cafe.receipts.length,2);assert.doesNotThrow(()=>validateSave(saved));
});
test('mid-meal save and reload preserve random arrival sequence, receipts and staff work',()=>{
 const m=model(3,90210);tick(m,100);assert.ok(m.s.cafe.receipts.length>0);const restored=new GameModel(validateSave(m.s));tick(m,20);tick(restored,20);assert.deepEqual(restored.s.cafe,m.s.cafe);
});
test('rendered dishes and carried trays use the correct person recipe, without duplicate guest actors',()=>{
 const k=kit(),world={scene:new THREE.Scene(),exitDoor(){}};buildCafe(world,k);world.player=createCitizen(k);world.scene.add(world.player);attachCafeTray(world);
 const m=model(),c=m.s.cafe;c.guests=[guest(1,-1,'espresso'),guest(2,1,'breakfast')];c.serial=2;c.nextGuest=999;serve(m,c.guests[0]);serve(m,c.guests[1]);updateCafe(world,m.s,.016);
 assert.equal(world.cafeGuests.size,2);assert.equal(world.cafeDishes[0].userData.meals.espresso.visible,true);assert.equal(world.cafeDishes[1].userData.meals.breakfast.visible,true);assert.equal(world.cafeDishes[1].userData.meals.espresso.visible,false);
 const g=guest(3,-1,'latte',3);g.seat=1;g.stage='accepted';c.guests.push(g);c.serial=3;m.s.position={...CAFE_POINTS.cafePrep};m.cafeAction('prepare',3);tick(m,6.1);m.cafeAction('collect');updateCafe(world,m.s,.016);
 assert.equal(world.cafeTray.visible,true);assert.equal(world.cafeTray.userData.meals.latte.visible,true);assert.equal(world.cafeTray.userData.meals.breakfast.visible,false);
});
