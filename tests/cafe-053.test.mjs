import test from 'node:test';
import assert from 'node:assert/strict';
import * as THREE from '../dist/vendor/three.module.js';
import {GameModel,newGame,validateSave} from '../dist/model.js';
import {CAFE_POINTS,CAFE_MENU,tickCafe,guestPatience,cafeCosts,cafeGuestIntent} from '../dist/cafe.js';
import {guestSeat} from '../dist/cafe-layout.js';
import {buildCafe,updateCafe} from '../dist/cafe-interior.js';
import {World} from '../dist/world.js';
function setup(staff=0,start=true){const s=newGame(true);s.businesses.cafe={stock:3,staff,staffLevels:[1,1,1],price:1,quality:1,marketing:false,open:true,profit:0,sales:0,costs:0};s.minute=600;s.position={x:63,z:-12};const m=new GameModel(s);m.enterInterior('cafe');s.position={...CAFE_POINTS.cafeOffice};if(start){assert.ok(m.cafeAction('start'));s.cafe.nextGuest=999;s.cafe.rng=1000}return m}
function ready(m){m.s.cafe.nextGuest=999;while(m.s.cafe.employees.some(e=>e.status==='entering'))tick(m,.1);}
function guest(m,recipe='espresso'){const c=m.s.cafe,g={id:++c.serial,party:c.serial,seat:0,side:-1,recipe,stage:'order',time:0,wait:0,patience:120,price:CAFE_MENU[recipe].price,paid:false,served:false,prepaid:false,tipPending:0,paymentWait:0};c.guests.push(g);return g}
function tick(m,seconds){for(let t=0;t<seconds;t+=.05)tickCafe(m,Math.min(.05,seconds-t))}
function serve(m,g){m.s.position={...CAFE_POINTS.cafeTable0};assert.ok(m.cafeAction('guest',g.id));m.s.position={...CAFE_POINTS.cafePrep};assert.ok(m.cafeAction('prepare',g.id));tick(m,CAFE_MENU[g.recipe].seconds+.1);assert.ok(m.cafeAction('collect'));m.s.position={...CAFE_POINTS.cafeTable0};assert.ok(m.cafeAction('guest',g.id))}

test('patience pauses, survives reload, and expires individually with an unserved tray discarded',()=>{
 const m=setup(),g=guest(m);g.stage='accepted';g.wait=119.9;m.s.cafe.tray=g.id;m.s.cafe.carrying=true;
 const before=structuredClone(m.s.cafe);tickCafe(m,0);tickCafe(m,NaN);assert.deepEqual(m.s.cafe,before);
 const restored=new GameModel(validateSave(m.s));tick(restored,.2);const c=restored.s.cafe;
 assert.equal(c.guests[0].stage,'leaving');assert.equal(c.lost,1);assert.equal(c.tray,null);assert.equal(c.carrying,false);assert.equal(c.revenue,0);assert.equal(guestPatience(c.guests[0]),0);tick(restored,2);assert.equal(c.lost,1);
});
test('served guests eat past their former patience deadline and pay only after dining',()=>{
 const m=setup(),g=guest(m);g.wait=114;serve(m,g);assert.equal(g.served,true);assert.equal(g.paid,false);assert.equal(m.s.cafe.revenue,0);
 tick(m,16.1);assert.equal(g.stage,'paying');assert.equal(m.s.cafe.lost,0);assert.equal(cafeGuestIntent(m.s.cafe,g),'Kassieren');
 const restored=new GameModel(validateSave(m.s));assert.ok(restored.cafeAction('guest',g.id));const c=restored.s.cafe;assert.equal(c.revenue,340);assert.equal(c.receipts.length,1);assert.equal(restored.cafeAction('guest',g.id),false);assert.equal(c.revenue,340);assert.doesNotThrow(()=>validateSave(restored.s));
});
test('uncollected payment and early closing each settle a served bill once',()=>{
 const m=setup(),g=guest(m,'latte');serve(m,g);tick(m,24.1);assert.equal(g.stage,'paying');tick(m,19);assert.equal(m.s.cafe.revenue,0);tick(m,1.1);assert.equal(m.s.cafe.revenue,450);assert.equal(m.s.cafe.receipts.length,1);
 m.s.position={...CAFE_POINTS.cafeOffice};m.cafeAction('finish');assert.equal(m.s.cafe.revenue,450);assert.equal(m.s.cafe.lastReport.sales,450);
 const early=setup(),breakfast=guest(early,'breakfast');serve(early,breakfast);early.s.position={...CAFE_POINTS.cafeOffice};assert.ok(early.cafeAction('finish'));assert.equal(early.s.cafe.revenue,890);assert.equal(early.s.cafe.receipts.length,1);assert.equal(early.cafeAction('finish'),false);assert.equal(early.s.cafe.receipts.length,1);
});
test('training charges the correct employee once per level, respects cash and resets on dismissal',()=>{
 const m=setup(3,false),b=m.s.businesses.cafe,before=m.s.money;
 m.manage('cafe','train',1);assert.equal(m.s.money,before-15000);assert.deepEqual(b.staffLevels,[1,2,1]);
 m.manage('cafe','train',1);assert.equal(m.s.money,before-45000);m.manage('cafe','train',1);assert.equal(m.s.money,before-45000);assert.deepEqual(b.staffLevels,[1,3,1]);
 for(const value of [-1,3,1.5,'invalid'])m.manage('cafe','train',value);assert.equal(m.s.money,before-45000);
 m.s.money=14999;m.manage('cafe','train',0);assert.equal(b.staffLevels[0],1);assert.equal(m.s.money,14999);
 m.s.money=100000;m.cafeAction('start');m.manage('cafe','train',0);assert.equal(b.staffLevels[0],1);assert.deepEqual(m.s.cafe.config.staffLevels,[1,3,1]);assert.doesNotThrow(()=>validateSave(m.s));
 m.cafeAction('finish');m.manage('cafe','fire');m.manage('cafe','fire');assert.deepEqual(b.staffLevels,[1,1,1]);m.manage('cafe','hire');assert.equal(b.staffLevels[1],1);
});
test('trained barista prepares faster and trained service walks faster along the same route',()=>{
 const baseline=setup(3,false),trained=setup(3,false);trained.s.businesses.cafe.staffLevels=[3,3,3];
 for(const m of [baseline,trained]){m.cafeAction('start');ready(m);m.s.cafe.nextGuest=999;const g=guest(m);g.stage='accepted';tick(m,3.1)}
 assert.ok(baseline.s.cafe.prep);assert.equal(trained.s.cafe.prep,null);assert.equal(trained.s.cafe.staffCarry??trained.s.cafe.tray,1);
 const a=setup(2,false),b=setup(2,false);b.s.businesses.cafe.staffLevels=[1,3,1];
 for(const m of [a,b]){m.cafeAction('start');ready(m);m.s.cafe.nextGuest=999;guest(m);tick(m,.5)}
 const e=a.s.cafe.employees[1],f=b.s.cafe.employees[1];assert.equal(e.task.kind,'order');assert.deepEqual(e.route,f.route);assert.ok(Math.hypot(f.x-300,f.z-76.5)>Math.hypot(e.x-300,e.z-76.5));assert.doesNotThrow(()=>validateSave(b.s));
 const resumed=new GameModel(validateSave(trained.s));tick(trained,25);tick(resumed,25);assert.deepEqual(resumed.s.cafe,trained.s.cafe);
});
test('trained cleanup shortens work at the same table without remotely removing dishes',()=>{
 const models=[setup(3,false),setup(3,false)];models[1].s.businesses.cafe.staffLevels[2]=3;
 for(const m of models){m.cafeAction('start');ready(m);const c=m.s.cafe;c.nextGuest=999;c.dirty[0]=1;c.dishes[0]='espresso';const e=c.employees[2];Object.assign(e,CAFE_POINTS.cafeTable0);e.task={kind:'clean',id:null,seat:0,place:0};e.route=[];e.wait=1.8;tick(m,1.4)}
 assert.ok(models[0].s.cafe.dirty[0]>0);assert.equal(models[1].s.cafe.dirty[0],0);
});
test('actual cafe sales pay hourly wages and stock is not charged twice',()=>{
 const m=setup(3),s=m.s,c=s.cafe;c.elapsed=120;c.revenue=1000;c.openingRevenue=1000;c.tips=100;c.openingTips=100;c.ingredients=180;
 assert.equal(cafeCosts(c),5713);m.cafeAction('finish');assert.equal(c.lastReport.costs,5713);const money=s.money;
 m.advance(1440-s.minute);const b=s.businesses.cafe;assert.equal(b.stock,2);assert.equal(b.sales,1100);assert.equal(b.costs,5713);assert.equal(b.profit,-4613);assert.equal(s.money,money-4613+833);assert.equal(b.cafeReport.fixedCosts,4700);assert.equal(b.cafeReport.automaticSales,0);assert.equal(b.cafeReport.manualSales,1100);assert.doesNotThrow(()=>validateSave(s));
 m.advance(1440-s.minute);assert.equal(b.stock,2);assert.equal(b.cafeReport.manualSales,0);assert.equal(b.profit,-2450);
});
test('closing and dismissing staff cannot erase the two-hour call-out wage obligation',()=>{
 const m=setup(3);m.s.cafe.elapsed=60;m.cafeAction('finish');m.manage('cafe','open');m.manage('cafe','fire');m.manage('cafe','fire');m.advance(1440-m.s.minute);
 const b=m.s.businesses.cafe;assert.equal(b.sales,0);assert.equal(b.costs,5533);assert.equal(b.cafeReport.automaticCosts,0);assert.equal(b.stock,2);
});
test('0.5.2 paid meals migrate without a second bill and corrupt training is rejected',()=>{
 const m=setup(),g=guest(m);serve(m,g);const c=m.s.cafe;c.schema=2;g.paid=true;c.revenue=g.price;c.tips=15;c.receipts=[{id:g.id,seat:g.seat,side:g.side,recipe:g.recipe,amount:g.price,tip:15}];
 for(const field of ['served','tipPending','paymentWait'])delete g[field];delete c.config.staffLevels;delete m.s.businesses.cafe.staffLevels;
 const restored=new GameModel(validateSave(m.s));assert.equal(restored.s.cafe.schema,4);assert.deepEqual(restored.s.businesses.cafe.staffLevels,[1,1,1]);tick(restored,40);assert.equal(restored.s.cafe.revenue,340);assert.equal(restored.s.cafe.tips,15);assert.equal(restored.s.cafe.receipts.length,1);
 const bad=structuredClone(restored.s);bad.businesses.cafe.staffLevels=[1,4,1];assert.throws(()=>validateSave(bad),/Ausbildung/);
});
function kit(){const mat=c=>new THREE.MeshStandardMaterial({color:c}),boxGeo=new THREE.BoxGeometry(1,1,1),cyl=new THREE.CylinderGeometry(1,1,1,8);return {mat,box(p,x,y,z,w,h,d,c,m){const o=new THREE.Mesh(boxGeo,m||mat(c));o.position.set(x,y,z);o.scale.set(w,h,d);p.add(o);return o},cylinder(p,x,y,z,r,h,c){const o=new THREE.Mesh(cyl,mat(c));o.position.set(x,y,z);o.scale.set(r,h,r);p.add(o);return o},sign(){return new THREE.Group()}}}
test('head indicators show exact time, face the camera, switch to payment and reuse pooled graphics',()=>{
 const world={scene:new THREE.Scene(),camera:new THREE.PerspectiveCamera(),exitDoor(){}};buildCafe(world,kit());world.camera.rotation.set(.2,1,.1);
 const m=setup(),g=guest(m);updateCafe(world,m.s,.016);const actor=world.cafeGuests.get(g.id),badge=actor.userData.badge,panel=badge.userData.panel;
 assert.equal(badge.visible,true);assert.equal(badge.userData.view.seconds,120);assert.ok(badge.quaternion.angleTo(world.camera.quaternion)<1e-7);
 g.wait=96;updateCafe(world,m.s,0);assert.equal(badge.userData.view.seconds,24);assert.equal(badge.userData.view.urgent,true);
 g.stage='eating';g.served=true;updateCafe(world,m.s,0);assert.equal(badge.visible,false);
 g.stage='paying';updateCafe(world,m.s,0);assert.equal(badge.visible,true);assert.equal(badge.userData.view.paying,true);
 m.s.cafe.guests=[];updateCafe(world,m.s,0);assert.equal(badge.visible,false);guest(m);updateCafe(world,m.s,0);assert.equal(world.cafeGuests.get(2).userData.badge,badge);assert.equal(badge.userData.panel,panel);
});
test('direct interaction targets the looked-at guest and refuses distant or backward targets',()=>{
 const m=setup(),g=guest(m),seat=guestSeat(0,-1),actor=new THREE.Group();actor.position.set(seat.x,-.25,seat.z);const camera=new THREE.PerspectiveCamera(55,16/9,.1,100);camera.position.set(295,3,85);camera.lookAt(seat.x,1.18,seat.z);camera.updateMatrixWorld(true);
 m.s.position={...CAFE_POINTS.cafeTable0};const world={model:m,camera,cafeGuests:new Map([[g.id,actor]]),drops:[]};let n=World.prototype.nearest.call(world);assert.equal(n.type,'cafeGuest');assert.equal(n.id,g.id);
 camera.lookAt(295,3,100);camera.updateMatrixWorld(true);assert.notEqual(World.prototype.nearest.call(world)?.type,'cafeGuest');
 camera.lookAt(seat.x,1.18,seat.z);camera.updateMatrixWorld(true);m.s.position={...CAFE_POINTS.cafeOffice};assert.notEqual(World.prototype.nearest.call(world)?.type,'cafeGuest');
 g.stage='accepted';assert.equal(cafeGuestIntent(m.s.cafe,g),null);m.s.cafe.carrying=true;m.s.cafe.tray=g.id;assert.equal(cafeGuestIntent(m.s.cafe,g),'Servieren');m.s.cafe.tray=999;assert.equal(cafeGuestIntent(m.s.cafe,g),null);
});
