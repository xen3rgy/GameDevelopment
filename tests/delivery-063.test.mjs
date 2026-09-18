import {WORLD_BOUNDS,DISTRICT_FIXTURES,exteriorContains} from '../dist/city-layout.js';
import test from 'node:test';
import assert from 'node:assert/strict';
import * as THREE from '../dist/vendor/three.module.js';
import {GameModel,newGame,validateSave} from '../dist/model.js';
import {CONTRACTS,contractById,deadlineView,clockMinutes} from '../dist/contracts.js';
import {deliveryPlan,deliveryTarget,DELIVERY_SECONDS,DELIVERY_PEOPLE} from '../dist/delivery-routes.js';
import {CourierScene} from '../dist/courier-scene.js';
import {createCitizen} from '../dist/art.js';
import {BUILDINGS} from '../dist/data.js';
import {CityNavigation} from '../dist/navigation.js';
import {segmentClear} from '../dist/movement.js';
import {contractCards,itinerary,courierReceipt,deadlineMarkup} from '../dist/courier-ui.js';

function at(m,id){const p=deliveryTarget(id);m.s.position={x:p.x,z:p.z};}
function setup(speed=1,id='canal'){const m=new GameModel(newGame(true));m.s.settings.speed=speed;at(m,'jobs');assert.ok(m.acceptContract(id));return m;}
function advance(m,seconds){for(let t=0;t<seconds;t+=.05)m.updateTime(Math.min(.05,seconds-t));}
function complete(m){while(m.s.job){at(m,m.s.job.target);assert.ok(m.work('deliver'));advance(m,DELIVERY_SECONDS.deliver);}}
function kit(){return {mat:c=>new THREE.MeshStandardMaterial({color:c}),box(p,x,y,z,w,h,d,c){const m=new THREE.Mesh(new THREE.BoxGeometry(w,h,d),this.mat(c));m.position.set(x,y,z);p.add(m);return m;},sign(p,text,x,y,z){const g=new THREE.Group();g.name=text;g.position.set(x,y,z);p.add(g);return g;}};}
// Game asset factories destructure kit functions; avoid a this-bound helper.
function artKit(){const k=kit();k.box=(p,x,y,z,w,h,d,c)=>{const m=new THREE.Mesh(new THREE.BoxGeometry(w,h,d),k.mat(c));m.position.set(x,y,z);p.add(m);return m;};return k;}

test('pickup, delivery and final payout occur only after their animation, including save/resume',()=>{
 let m=setup(1,'express');assert.equal(deadlineView(m.s),null);assert.equal(m.work('deliver'),false);
 advance(m,1);const before=structuredClone(m.s);m.updateTime(0);assert.deepEqual(m.s,before);
 m=new GameModel(validateSave(JSON.parse(JSON.stringify(m.s))));advance(m,1.2);assert.equal(m.s.job.interaction,null);assert.ok(deadlineView(m.s));
 assert.ok(Math.abs(m.s.job.started-clockMinutes(m.s))<.001);const cash=m.s.money;at(m,m.s.job.target);
 assert.ok(m.work('deliver'));assert.equal(m.work('deliver'),false);assert.equal(m.s.job.progress,0);assert.equal(m.count('parcel'),2);
 advance(m,1.3);m=new GameModel(validateSave(m.s));advance(m,1.3);assert.equal(m.count('parcel'),1);assert.equal(m.s.job.progress,1);assert.equal(m.s.money,cash);
 complete(m);assert.equal(m.s.job,null);assert.equal(m.s.courier.last.stops,2);assert.equal(m.s.courier.last.deliveries.length,2);assert.equal(m.s.money,cash+9000);
 const earned=m.s.money;m.tickCourier(10);assert.equal(m.work('deliver'),false);assert.equal(m.s.money,earned);assert.doesNotThrow(()=>validateSave(m.s));
});
test('all route offers have reachable inputs and sufficient walking budget at 1x, 4x and 10x',()=>{
 for(const c of CONTRACTS)for(const speed of [1,4,10]){
  const m=setup(speed,c.id),plan=m.s.job.plan;assert.equal(plan.legs.length,c.route.length);assert.ok(plan.distance>0);
  assert.equal(plan.deadlineMinutes,c.minutes?plan.realSeconds*speed:0);
  const expected=plan.distance/2.25+c.route.length*DELIVERY_SECONDS.deliver;assert.ok(plan.realSeconds>expected);
  advance(m,2.2);for(const leg of plan.legs){m.updateTime(leg.distance/2.25);at(m,leg.to);assert.ok(m.work('deliver'));advance(m,2.6);}
  assert.equal(m.s.courier.last.status,'completed',c.id+' @ '+speed);assert.equal(m.s.courier.last.timeBonus,c.bonus);assert.doesNotThrow(()=>validateSave(m.s));
 }
});
test('speed switches never rewrite agreed deadlines and HUD explains real-time impact',()=>{
 const m=setup(4,'express');advance(m,2.2);const before=deadlineView(m.s),deadline=m.s.job.deadlineMinutes;
 m.s.settings.speed=10;const after=deadlineView(m.s);assert.equal(after.remaining,before.remaining);assert.ok(after.realSeconds<before.realSeconds);assert.equal(after.changedSpeed,true);
 assert.match(deadlineMarkup(m.s),/10×/);assert.match(deadlineMarkup(m.s),/seit Annahme geändert/);assert.equal(m.s.job.deadlineMinutes,deadline);
 m.advance(deadline-100);assert.equal(deadlineView(m.s).urgent,true);m.advance(101);assert.equal(deadlineView(m.s).late,true);complete(m);
 assert.equal(m.s.courier.last.timeBonus,0);assert.equal(m.s.courier.last.missedBonus,2000);assert.equal(m.s.courier.last.base,7000);
});
test('vehicle quote requires a usable vehicle; one metre of driving no longer grants the full van bonus',()=>{
 const m=new GameModel(newGame(true));at(m,'jobs');assert.equal(m.acceptContract('canal','vehicle'),false);
 m.s.vehicle={id:'van',fuel:100,condition:100,x:26,z:-5,angle:0,speed:0};assert.ok(m.acceptContract('canal','vehicle'));advance(m,2.2);
 const base=m.s.job.quotedBase;m.recordCourierTravel(1,m.s.vehicle);assert.ok(m.s.job.operatingCosts>0);complete(m);
 assert.ok(m.s.courier.last.vehicleBonus<base*.01);assert.equal(m.s.courier.last.net,m.s.courier.last.total-m.s.courier.last.estimatedCosts);assert.doesNotThrow(()=>validateSave(m.s));
 at(m,'jobs');m.s.vehicle.fuel=0;assert.equal(m.acceptContract('express','vehicle'),false);assert.ok(m.acceptContract('express','foot'));
});
test('interrupted handoffs cannot pay or lose a parcel and cancellation removes reserved packages once',()=>{
 const m=setup();advance(m,2.2);at(m,m.s.job.target);assert.ok(m.work('deliver'));m.s.position.x+=6;advance(m,.1);
 assert.equal(m.s.job.interaction,null);assert.equal(m.s.job.progress,0);assert.equal(m.count('parcel'),2);assert.equal(m.s.courier.last,null);
 at(m,m.s.job.target);assert.ok(m.work('deliver'));m.cancelJob();const c=structuredClone(m.s.courier);advance(m,5);assert.deepEqual(m.s.courier,c);assert.equal(m.count('parcel'),0);assert.equal(m.s.courier.last.total,0);
 const pickup=setup();pickup.s.position.x+=10;advance(pickup,.1);assert.equal(pickup.s.job,null);assert.equal(pickup.count('parcel'),0);
});
test('legacy active tours keep their deadline and instant-delivery contract',()=>{
 const m=setup(1,'express');for(const key of ['protocol','interaction','plan','deliveries','operatingCosts','vanDistance'])delete m.s.job[key];m.s.job.deadlineMinutes=105;
 const loaded=new GameModel(validateSave(m.s));assert.equal(deadlineView(loaded.s).remaining,105);at(loaded,loaded.s.job.target);assert.ok(loaded.work('deliver'));assert.equal(loaded.s.job.progress,1);
 const bad=setup();bad.s.job.plan.legs[0].distance+=10;assert.throws(()=>validateSave(bad.s),/Tourplanung/);
});
test('physical reception points remain walkable after adding people and parcel racks',()=>{
 const k=artKit(),world={scene:new THREE.Scene(),colliders:BUILDINGS.map(([x,z,w,d,h])=>({x,z,w:w/2+.35,d:d/2+.35,h})).concat(DISTRICT_FIXTURES)};world.player=createCitizen(k);const scene=new CourierScene(world,k);
 const free=(x,z,r=.35)=>exteriorContains(x,z,r)&&!world.colliders.some(b=>Math.abs(x-b.x)<b.w+r&&Math.abs(z-b.z)<b.d+r);const nav=new CityNavigation(free,4,WORLD_BOUNDS);
 for(const id of Object.keys(DELIVERY_PEOPLE)){const to=deliveryTarget(id);assert.ok(free(to.x,to.z),id);const path=nav.find(deliveryTarget('jobs'),to);assert.ok(path.length>1,id);for(let i=1;i<path.length;i++)assert.ok(segmentClear(path[i-1],path[i],free,.4),id);}
 assert.equal(scene.stations.size,6);
});
test('handoff pose holds parcel, fixes roots and pauses with menus; receipt and cards stay coherent',()=>{
 const k=artKit(),world={scene:new THREE.Scene(),colliders:[],player:createCitizen(k)},visual=new CourierScene(world,k),m=setup();advance(m,2.2);at(m,m.s.job.target);world.player.position.set(m.s.position.x,.25,m.s.position.z);assert.ok(m.work('deliver'));
 const root=world.player.position.clone();m.s.job.interaction.elapsed=1.1;visual.update(m.s,.016);assert.deepEqual(world.player.position,root);assert.equal(visual.parcel.visible,true);assert.ok(world.player.userData.arms.some(a=>Math.abs(a.rotation.x)>.2));
 const pos=visual.parcel.position.clone(),elapsed=m.s.job.interaction.elapsed;visual.update(m.s,0);assert.deepEqual(visual.parcel.position,pos);assert.equal(m.s.job.interaction.elapsed,elapsed);
 const button=(label,action,arg,cls,disabled)=>`<button ${disabled?'disabled':''}>${label}</button>`;
 const fresh=newGame(true);assert.match(contractCards(fresh,button),/Paketausgabe markieren/);assert.match(contractCards(fresh,button,'foot','express'),/Kapitel-Express/);assert.doesNotMatch(contractCards(fresh,button,'foot','express'),/>Die Kanalrunde</);
 assert.match(itinerary(m.s,button),/Milan/);m.cancelJob();assert.match(courierReceipt(m.s),/Abgebrochen/);visual.update(m.s,.016);assert.equal(visual.stations.get('deliveryB').received.visible,false);
});
