import test from 'node:test';
import assert from 'node:assert/strict';
import {GameModel,newGame,validateSave} from '../dist/model.js';
import {CAFE_POINTS,waitingCafeOrders} from '../dist/cafe.js';
import {STAFF_HOME,staffPath,pathLength,samplePath,staffWalkable} from '../dist/cafe-layout.js';
import {cafeHours,simulationMinutes} from '../dist/game-time.js';
import {deadlineView} from '../dist/contracts.js';
function cafe(level=1,minute=600){const s=newGame(true);s.minute=minute;s.businesses.cafe={open:true,stock:3,staff:3,staffLevels:[level,level,level],price:1,quality:1,marketing:false,profit:0,sales:0,costs:0};s.inside=true;s.interior='cafe';s.position={...CAFE_POINTS.cafeOffice};const m=new GameModel(s);return m;}
function ready(m){m.s.cafe.nextGuest=999;while(m.s.cafe.employees.some(e=>e.status==='entering'))m.updateTime(.1);}
function guest(c,id,wait,stage='order'){const g={id,party:id,seat:id-1,side:-1,recipe:'espresso',stage,time:0,wait,patience:100,price:340,paid:false,served:false,prepaid:false,tipPending:0,paymentWait:0};c.serial=Math.max(c.serial,id);c.guests.push(g);return g;}
test('one clock drives street, indoor, shift and accelerated play, including speed changes',()=>{
 assert.equal(simulationMinutes(1440),1440);assert.equal(simulationMinutes(360,4),1440);
 for(const location of ['street','home','shop','cafe']){const m=cafe();if(location!=='cafe'){m.s.interior=location==='street'?null:location;m.s.inside=location!=='street';}else assert.ok(m.cafeAction('start'));const before=m.s.minute;m.updateTime(10);assert.ok(Math.abs(m.s.minute-before-10)<1e-7,location);m.s.settings.speed=4;m.updateTime(10);assert.ok(Math.abs(m.s.minute-before-50)<1e-7,location);}
 const normal=cafe(),fast=cafe();for(const m of [normal,fast]){m.cafeAction('start');m.s.cafe.rng=1000;}fast.s.settings.speed=4;normal.updateTime(40);fast.updateTime(10);assert.deepEqual(fast.s.cafe,normal.s.cafe);assert.equal(fast.s.minute,normal.s.minute);
 const before=structuredClone(normal.s);normal.updateTime(0);assert.deepEqual(normal.s,before);
});
test('opening hours and closing clock agree; clock does not change speed after shift',()=>{
 const early=cafe(1,479);assert.equal(cafeHours(early.s).canOpen,false);assert.equal(early.cafeAction('start'),false);early.updateTime(1);assert.equal(cafeHours(early.s).canOpen,true);
 assert.equal(cafe(1,1190).cafeAction('start'),false);const m=cafe(1,1080);assert.ok(m.cafeAction('start'));assert.equal(m.s.cafe.closeMinute,1200);assert.equal(m.s.cafe.duration,120);m.updateTime(120);assert.equal(m.s.cafe.phase,'closed');assert.equal(m.s.minute,1200);m.updateTime(10);assert.equal(m.s.minute,1210);assert.equal(m.cafeAction('start'),false);assert.match(cafeHours(m.s).detail,/08:00|Team/);
});
test('employees enter before guests, return through the exit, and commute survives saving',()=>{
 let m=cafe();assert.ok(m.cafeAction('start'));assert.ok(m.s.cafe.employees.every(e=>e.status==='entering'&&e.z>87));m.updateTime(1);assert.equal(m.s.cafe.guests.length,0);
 m=new GameModel(validateSave(m.s));ready(m);assert.ok(m.s.cafe.employees.every(e=>e.status==='working'));for(const e of m.s.cafe.employees)assert.ok(Math.hypot(e.x-STAFF_HOME[e.role].x,e.z-STAFF_HOME[e.role].z)<.001);
 m.s.cafe.nextGuest=0;m.updateTime(.1);assert.ok(m.s.cafe.guests.length);assert.ok(m.cafeAction('finish'));assert.ok(m.s.cafe.employees.every(e=>e.status==='leaving'));m=new GameModel(validateSave(m.s));m.updateTime(30);assert.ok(m.s.cafe.employees.every(e=>e.status==='gone'&&e.z>87));assert.doesNotThrow(()=>validateSave(m.s));
});
test('service picks the longest-waiting order ahead of a bill and does not roam when idle',()=>{
 const m=cafe();m.cafeAction('start');ready(m);const c=m.s.cafe;
 const younger=guest(c,1,5),older=guest(c,2,40);m.updateTime(.05);assert.equal(c.employees[1].task.id,older.id);assert.equal(younger.stage,'order');
 c.guests=[];c.employees[1].task=null;c.employees[1].route=[];const p={x:c.employees[1].x,z:c.employees[1].z};m.updateTime(2);assert.equal(c.employees[1].task,null);assert.equal(c.employees[1].x,p.x);assert.equal(c.employees[1].z,p.z);
 younger.stage='accepted';older.stage='accepted';c.guests=[younger,older];assert.equal(waitingCafeOrders(c)[0].id,older.id);
});
test('all service and barista commute routes stay clear of fixtures',()=>{
 const targets=[...STAFF_HOME,...Object.values(CAFE_POINTS),{x:300,z:87.65}];
 for(const from of targets)for(const to of targets){if(from===to)continue;const route=staffPath(from,to);assert.ok(route.length>1);for(let d=0;d<pathLength(route);d+=.08){const p=samplePath(route,d);assert.ok(staffWalkable(p.x,p.z),JSON.stringify(p));}}
});
test('trained teams serve more guests with less waiting in equivalent seeded shifts',()=>{
 const results=[];for(const level of [1,2,3]){let served=0,lost=0,wait=0;for(const seed of [1000,77,331,9001]){const m=cafe(level);m.cafeAction('start');m.s.cafe.rng=seed;m.updateTime(210);served+=m.s.cafe.served;lost+=m.s.cafe.lost;wait+=m.s.cafe.service.totalWait;}results.push({served,lost,wait:wait/served});}
 assert.ok(results[0].lost>0);assert.ok(results[1].served>results[0].served);assert.ok(results[2].served>=results[1].served);assert.ok(results[2].lost<results[0].lost);assert.ok(results[2].wait<results[1].wait);assert.ok(results[1].wait<results[0].wait);
});
test('legacy shift clock accounting and active courier deadlines migrate without lost progress',()=>{
 const m=cafe();m.cafeAction('start');ready(m);const old=structuredClone(m.s);old.cafe.elapsed=100;delete old.cafe.clockRate;delete old.cafe.legacyMinutes;delete old.cafe.duration;delete old.cafe.closeMinute;for(const e of old.cafe.employees)delete e.status;
 const loaded=validateSave(old);assert.equal(loaded.cafe.elapsed,100);assert.equal(loaded.cafe.legacyMinutes,-50);assert.equal(loaded.cafe.clockRate,1);assert.doesNotThrow(()=>validateSave(loaded));
 const courier=new GameModel(newGame(true));courier.s.position={x:26,z:-12};assert.ok(courier.acceptContract('express'));delete courier.s.job.deadlineMinutes;delete courier.s.job.protocol;delete courier.s.job.interaction;const saved=validateSave(courier.s);assert.equal(saved.job.deadlineMinutes,330);assert.equal(deadlineView(saved).remaining,330);
});
test('sandbox starts with 20000 euros and can test express immediately, story still earns unlocks',()=>{
 const sandbox=new GameModel(newGame(true));assert.equal(sandbox.s.money,2000000);sandbox.s.position={x:26,z:-12};assert.ok(sandbox.acceptContract('express'));sandbox.tickCourier(2.2);const deadline=sandbox.s.job.deadlineMinutes;assert.equal(deadlineView(sandbox.s).remaining,deadline);sandbox.s.settings.speed=4;sandbox.updateTime(20);assert.equal(deadlineView(sandbox.s).remaining,deadline-80);sandbox.updateTime(deadline/4);assert.equal(deadlineView(sandbox.s).late,true);
 const story=new GameModel();story.s.position={x:26,z:-12};assert.equal(story.acceptContract('express'),false);
});
