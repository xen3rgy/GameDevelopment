import test from 'node:test';
import assert from 'node:assert/strict';
import {readFileSync} from 'node:fs';
import {runInNewContext} from 'node:vm';
import * as THREE from '../dist/vendor/three.module.js';
import {GameModel,newGame,validateSave} from '../dist/model.js';
import {CAFE_POINTS,CAFE_MENU,tickCafe} from '../dist/cafe.js';
import {cafeServiceSummary} from '../dist/cafe-report.js';
import {applyCafeGuestSignal,cafeGuestSignal,staffGesture,applyCafeWorkerPose} from '../dist/cafe-gestures.js';
import {animateCitizen,playGesture} from '../dist/animation.js';
import {createCitizen} from '../dist/art.js';
import {poseCafeSeated,buildCafe,updateCafe,attachCafeTray} from '../dist/cafe-interior.js';
function model(staff=0){const s=newGame(true);s.businesses.cafe={stock:3,staff,staffLevels:[1,1,1],price:1,quality:1,marketing:false,open:true,profit:0,sales:0,costs:0};s.position={x:63,z:-12};const m=new GameModel(s);m.enterInterior('cafe');s.position={...CAFE_POINTS.cafeOffice};m.cafeAction('start');s.cafe.nextGuest=999;return m}
function guest(m,seat=0,side=-1){const c=m.s.cafe,g={id:++c.serial,party:c.serial,seat,side,recipe:'espresso',stage:'order',time:0,wait:0,patience:120,price:340,paid:false,served:false,prepaid:false,tipPending:0,paymentWait:0};c.guests.push(g);return g}
function tick(m,seconds){for(let t=0;t<seconds;t+=.05)tickCafe(m,Math.min(.05,seconds-t))}
function serve(m,g,wait){m.s.position={...CAFE_POINTS['cafeTable'+g.seat]};assert.ok(m.cafeAction('guest',g.id));m.s.position={...CAFE_POINTS.cafePrep};assert.ok(m.cafeAction('prepare',g.id));tick(m,4.1);m.cafeAction('collect');m.s.position={...CAFE_POINTS['cafeTable'+g.seat]};g.wait=wait;assert.ok(m.cafeAction('guest',g.id))}
test('service report measures actual servings once and distinguishes timeout from unfinished closing',()=>{
 const m=model(),a=guest(m),b=guest(m,0,1);serve(m,a,10);serve(m,b,80);assert.equal(m.cafeAction('guest',b.id),false);
 const impatient=guest(m,1);impatient.wait=119.99;tick(m,.05);guest(m,2);
 m.s.position={...CAFE_POINTS.cafeOffice};assert.ok(m.cafeAction('finish'));const r=m.s.cafe.lastReport;
 assert.equal(r.service.served,2);assert.equal(r.service.averageWait,45);assert.equal(r.service.maxWait,80);assert.equal(r.service.satisfied,1);assert.equal(r.service.satisfaction,50);assert.equal(r.service.completion,50);
 assert.equal(r.service.timedOut,1);assert.equal(r.service.closedUnserved,1);assert.equal(r.service.playerServed,2);assert.equal(r.service.staffServed,0);assert.equal(r.service.payments.closing,2);
 assert.equal(Object.values(r.breakdown).reduce((a,b)=>a+b,0),r.costs);assert.equal(r.profit,r.sales+r.tips-r.costs);assert.doesNotThrow(()=>validateSave(m.s));
 const copy=structuredClone(r);assert.equal(m.cafeAction('finish'),false);assert.deepEqual(m.s.cafe.lastReport,copy);
});
test('payment sources record actual collections and give a seated guest time to acknowledge',()=>{
 const m=model(),g=guest(m);serve(m,g,12);tick(m,16.1);assert.equal(g.stage,'paying');m.s.position={...CAFE_POINTS.cafeTable0};assert.ok(m.cafeAction('guest',g.id));assert.equal(g.time,.9);
 tick(m,.4);assert.equal(g.stage,'finished');assert.ok(cafeGuestSignal(g).weight>0);tick(m,.6);assert.equal(g.stage,'leaving');assert.equal(m.s.cafe.service.payments.player,1);
 const other=model(),h=guest(other);serve(other,h,15);tick(other,36.2);assert.equal(other.s.cafe.service.payments.table,1);assert.equal(other.s.cafe.service.payments.player,0);
});
test('measurements persist during service and reset per shift; old saves are explicitly partial',()=>{
 const m=model(),g=guest(m);serve(m,g,20);const restored=new GameModel(validateSave(m.s));tick(m,20);tick(restored,20);assert.deepEqual(restored.s.cafe,m.s.cafe);
 const old=structuredClone(m.s);old.cafe.schema=3;delete old.cafe.service;const migrated=validateSave(old);assert.equal(migrated.cafe.service.partial,true);assert.equal(migrated.cafe.service.served,0);assert.equal(cafeServiceSummary(migrated.cafe).averageWait,null);assert.equal(migrated.cafe.revenue,old.cafe.revenue);
 m.s.position={...CAFE_POINTS.cafeOffice};m.cafeAction('finish');const report=structuredClone(m.s.cafe.lastReport);m.s.day++;assert.ok(m.cafeAction('start'));assert.equal(m.s.cafe.service.partial,false);assert.equal(m.s.cafe.service.served,0);assert.deepEqual(m.s.cafe.lastReport,report);
 const bad=structuredClone(restored.s);bad.cafe.service.satisfied=999;assert.throws(()=>validateSave(bad),/Servicemessung/);
});
test('empty shifts show no made-up satisfaction and paused simulations collect no time',()=>{
 const m=model(),before=structuredClone(m.s.cafe.service);tickCafe(m,0);assert.deepEqual(m.s.cafe.service,before);m.cafeAction('finish');const r=m.s.cafe.lastReport;
 assert.equal(r.service.averageWait,null);assert.equal(r.service.satisfaction,null);assert.equal(r.service.completion,null);assert.equal(r.costs,3283);assert.equal(r.profit,-3283);
});
test('three employees report preparation, service and actual cleanup without claiming player work',()=>{
 const m=model(3);m.s.cafe.nextGuest=3;m.s.cafe.rng=1000;tick(m,m.s.cafe.duration+.1);const r=m.s.cafe.lastReport;
 assert.equal(r.team.staff,3);assert.deepEqual(r.team.levels,[1,1,1]);assert.ok(r.service.preparedByStaff>0);assert.ok(r.service.ordersByStaff>0);assert.ok(r.service.staffServed>0);assert.ok(r.service.clearedByStaff>0);assert.equal(r.service.playerServed,0);assert.equal(r.service.served,r.served);assert.doesNotThrow(()=>validateSave(m.s));
});
function kit(){const mat=c=>new THREE.MeshStandardMaterial({color:c});return {mat,box(p,x,y,z,w,h,d,c,m){const o=new THREE.Mesh(new THREE.BoxGeometry(w,h,d),m||mat(c));o.position.set(x,y,z);p.add(o);return o},cylinder(p,x,y,z,r,h,c){const o=new THREE.Mesh(new THREE.CylinderGeometry(r,r,h,8),mat(c));o.position.set(x,y,z);p.add(o);return o},sign(){return new THREE.Group()}}}
test('report buttons dispatch to the actual report screen',async()=>{
 const source=readFileSync(new URL('../dist/app.js',import.meta.url),'utf8'),start=source.indexOf('async function action('),end=source.indexOf('\ndocument.addEventListener',start),calls=[];
 assert.ok(start>0&&end>start);const dispatch=runInNewContext('('+source.slice(start,end)+')',{model:{s:{dailyLife:{action:null}}},open:(...args)=>calls.push(args)});await dispatch('cafeReport');assert.deepEqual(calls,[['cafeReport','']]);
});
test('report content renders complete, empty and legacy reports without invented numbers',()=>{
 const source=readFileSync(new URL('../dist/app.js',import.meta.url),'utf8'),format=(...args)=>args.join(' | '),render=runInNewContext(source.slice(source.indexOf('function cafeEconomyNote('),source.indexOf('function cafePatienceMarkup('))+';('+source.slice(source.indexOf('function cafeReportContent('))+')',{stat:format,row:format,button:format,esc:s=>s,euro:n=>(n/100).toFixed(2)+' €'});
 const m=model();m.cafeAction('finish');const html=render(m.s.cafe.lastReport);assert.match(html,/Noch keine abgeschlossenen/);assert.doesNotMatch(html,/NaN|undefined|null/);
 const legacy={day:1,served:2,lost:0,sales:680,tips:80,costs:140,profit:620,reason:'Feierabend'};const old=render(legacy);assert.match(old,/noch keine Wartezeiten/);assert.match(old,/6.20 €/);assert.doesNotMatch(old,/NaN|undefined|null/);
});
test('guest signals keep feet and body origin fixed, differ by guest and never override dining',()=>{
 const actor=createCitizen(kit()),g={id:4,stage:'order',wait:.8,paymentWait:.8};poseCafeSeated(actor);const root=actor.position.clone(),legs=actor.userData.legs.map(l=>l.rotation.x);applyCafeGuestSignal(actor,g);
 assert.ok(actor.userData.arms[1].rotation.x<-1);assert.deepEqual(actor.position,root);assert.deepEqual(actor.userData.legs.map(l=>l.rotation.x),legs);assert.notEqual(cafeGuestSignal(g).weight,cafeGuestSignal({...g,id:3}).weight);
 const pose=actor.userData.arms.map(a=>a.rotation.toArray());applyCafeGuestSignal(actor,{...g,stage:'eating'});assert.deepEqual(actor.userData.arms.map(a=>a.rotation.toArray()),pose);
 poseCafeSeated(actor);applyCafeGuestSignal(actor,{...g,stage:'paying'});assert.ok(actor.userData.arms[1].rotation.x<-1);assert.equal(cafeGuestSignal({...g,stage:'leaving'}),null);
});
test('player cafe gestures do not trigger pickup crouches or move the camera root',()=>{
 for(const kind of ['cafeOrder','cafePay','cafeServe','cafeClean']){
  const actor=createCitizen(kit()),position=actor.position.clone();playGesture(actor,kind);for(let n=0;n<15;n++)animateCitizen(actor,.03,0);
  assert.deepEqual(actor.position,position);assert.ok(actor.userData.legs.every(l=>Math.abs(l.rotation.x)<1e-8));assert.ok(actor.userData.arms.some(a=>a.rotation.x<-.3));
  const before=actor.userData.gesture.age;animateCitizen(actor,0,0);assert.equal(actor.userData.gesture.age,before);
 }
 assert.equal(staffGesture({task:{kind:'order'},route:[{x:1,z:1}],wait:.7}),null);const gesture=staffGesture({task:{kind:'order'},route:[],wait:.7});assert.equal(gesture.kind,'cafeOrder');assert.ok(gesture.weight>.99);
});
test('order and receipt props follow their hands and disappear when work ends',()=>{
 const k=kit(),world={scene:new THREE.Scene(),exitDoor(){}};buildCafe(world,k);world.player=createCitizen(k);attachCafeTray(world);const m=model(2);guest(m);const e=m.s.cafe.employees[1];e.task={kind:'order',id:1,seat:0,place:null};e.route=[];e.wait=.7;updateCafe(world,m.s,.016);
 const actor=world.cafeStaff[1],tools=actor.userData.cafeTools;assert.equal(tools.book.parent,actor.userData.elbows[0]);assert.equal(tools.pen.parent,actor.userData.elbows[1]);assert.equal(tools.book.visible,true);assert.equal(tools.bill.visible,false);
 actor.updateMatrixWorld(true);const page=tools.book.localToWorld(new THREE.Vector3(Math.sin(.5*18)*.035,.025,.02)),tip=tools.pen.localToWorld(new THREE.Vector3(0,0,.075));assert.ok(page.distanceTo(tip)<.005,'pen should touch the page: '+page.distanceTo(tip));
 e.task={kind:'pay',id:1,seat:0,place:null};e.wait=.55;updateCafe(world,m.s,0);assert.equal(tools.bill.visible,true);assert.equal(tools.book.visible,false);e.task=null;updateCafe(world,m.s,0);assert.equal(tools.bill.visible,false);assert.equal(tools.pen.visible,false);
 playGesture(world.player,'cafeOrder');updateCafe(world,m.s,0);assert.equal(world.player.userData.cafeTools.book.visible,true);m.s.interior=null;updateCafe(world,m.s,0);assert.equal(world.player.userData.cafeTools.book.visible,false);
});
