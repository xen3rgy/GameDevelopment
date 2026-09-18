import test from 'node:test';
import assert from 'node:assert/strict';
import * as THREE from '../dist/vendor/three.module.js';
import {GameModel,newGame,validateSave} from '../dist/model.js';
import {LOCATIONS} from '../dist/data.js';
import {WORKSHOP_POINTS,workshopClear,workshopWorkPoint} from '../dist/workshop-layout.js';
import {workshopSteps,workshopStep,workshopCarry} from '../dist/workshop.js';
import {supplyPlan} from '../dist/workshop-supply.js';
import {tickWorkshopLife,newWorkshopLife,CUSTOMER_ROUTE} from '../dist/workshop-life.js';
import {WorkshopScene} from '../dist/workshop-scene.js';
import {createCitizen} from '../dist/art.js';
import {animateCitizen} from '../dist/animation.js';
import {walkingProfile} from '../dist/player-movement.js';
import {workshopReceipt,supplyPanel} from '../dist/workshop-ui.js';
const near=(a,b,tol=1e-6)=>assert.ok(Math.abs(a-b)<tol,`${a} != ${b}`);
const button=(label,action)=>`<button data-action="${action}">${label}</button>`;
function fresh(){const m=new GameModel(newGame(true));m.s.minute=480;m.s.position={x:-174,z:43};assert.ok(m.enterInterior('workshop'));m.s.workshop.training=1;m.s.position={...WORKSHOP_POINTS.workshopDesk};return m;}
function step(m){const c=workshopStep(m.s);m.s.position={...WORKSHOP_POINTS[c.point]};assert.ok(m.workshopAction('step',c.correct));while(m.s.workshop.active?.action)m.updateTime(.1);}
function parts(m){m.leaveInterior();m.s.position={...LOCATIONS.find(l=>l.id==='depot')};assert.ok(m.workshopAction('supplyPickup'));}
function returnParts(m){m.s.position={x:-174,z:43};assert.ok(m.enterInterior('workshop'));m.s.position={...WORKSHOP_POINTS.workshopIntake};assert.ok(m.workshopAction('supplyDeliver'));}

test('all saved variants have consistent clues, achievable steps and distinct diagnostics',()=>{
 const diagnoses=new Set();for(const type of ['stock','check','tube'])for(let variant=0;variant<12;variant++){
  const m=fresh();assert.ok(m.workshopAction('accept',type));m.s.workshop.active.variant=variant;const card=workshopSteps(m.s.workshop.active);assert.equal(card.filter(c=>c.choices.filter(v=>v[0]===c.correct).length===1).length,card.length);
  if(type==='check')diagnoses.add(card[0].correct);
  while(workshopStep(m.s)){step(m);assert.doesNotThrow(()=>validateSave(m.s));}m.s.position={...WORKSHOP_POINTS.workshopDesk};assert.ok(m.workshopAction('finish'));assert.equal(m.s.workshop.last.mistakes,0);assert.ok(m.s.workshop.last.workMinutes>25);assert.match(workshopReceipt(m.s.workshop),/Spielminuten insgesamt/);
 }assert.deepEqual([...diagnoses].sort(),['cable','center','pads','pressure']);
});
test('0.7.0 interrupted work cards keep their legacy meaning after save migration',()=>{
 const m=fresh();m.workshopAction('accept','stock');const a=m.s.workshop.active;delete a.revision;delete a.workMinutes;a.variant=1;assert.equal(workshopStep(m.s).correct,'short');step(m);step(m);assert.equal(workshopStep(m.s).correct,'actual');const restored=new GameModel(validateSave(JSON.parse(JSON.stringify(m.s))));assert.equal(workshopStep(restored.s).correct,'actual');step(restored);assert.equal(restored.s.workshop.active.step,3);
});
test('parts chain requires physical handoffs, saves its cargo and pays separately once',()=>{
 for(const late of [false,true]){const m=fresh(),money=m.s.money;assert.ok(m.workshopAction('acceptSupply','tube'));step(m);m.s.position={...WORKSHOP_POINTS.workshopStock};assert.equal(m.workshopAction('step',workshopStep(m.s).correct),false);assert.equal(m.workshopAction('supplyPickup'),false);parts(m);assert.equal(m.workshopAction('supplyPickup'),false);assert.equal(m.workshopAction('supplyDeliver'),false);
  assert.equal(workshopCarry(m.s),'supply');assert.equal(walkingProfile(m.s,true).running,false);assert.equal(walkingProfile(m.s,true).canJump,false);const saved=validateSave(JSON.parse(JSON.stringify(m.s)));assert.equal(saved.workshop.active.supply.phase,'carrying');if(late)m.advance(supplyPlan(m.s).returnMinutes+1);returnParts(m);
  assert.equal(m.s.money-money,late?800:1000);assert.equal(m.s.workshop.lastDelivery.late,late);assert.equal(m.workshopAction('supplyDeliver'),false);assert.equal(m.s.money-money,late?800:1000);assert.equal(m.s.workshop.active.supply.phase,'received');while(workshopStep(m.s))step(m);m.s.position={...WORKSHOP_POINTS.workshopDesk};assert.ok(m.workshopAction('finish'));assert.equal(m.s.money-money,(late?800:1000)+m.s.workshop.last.total);assert.doesNotThrow(()=>validateSave(m.s));
 }
});
test('parts trips respect closing, speed-dependent travel budget, pause and cancellation',()=>{
 for(const speed of [1,4,10]){const m=fresh();m.s.settings.speed=speed;const plan=supplyPlan(m.s);assert.equal(plan.returnMinutes,90*speed);assert.equal(m.workshopAction('acceptSupply','tube'),speed===1);}
 const m=fresh();m.workshopAction('acceptSupply','tube');parts(m);const copy=JSON.stringify(m.s);m.updateTime(0);assert.equal(JSON.stringify(m.s),copy);m.s.position={x:-174,z:43};m.enterInterior('workshop');m.s.position={...WORKSHOP_POINTS.workshopDesk};const money=m.s.money;assert.ok(m.workshopAction('cancel'));assert.equal(m.workshopAction('supplyDeliver'),false);assert.equal(m.s.money,money);
 const expired=fresh();expired.workshopAction('acceptSupply','tube');parts(expired);const before=expired.s.money;expired.advance(660);assert.equal(expired.s.workshop.active,null);assert.equal(expired.workshopAction('supplyDeliver'),false);assert.equal(expired.s.money,before);
});
test('customers complete dropoff/pickup at every speed; routes stay clear and no ambient wages appear',()=>{
 for(const speed of [1,4,10]){const m=fresh(),s=m.s,money=s.money;s.settings.speed=speed;s.position={x:340,z:83};s.workshop.active={action:{}};const kinds=new Set(),phases=new Set(),served=new Set();let furthest=0;
  for(let i=0;i<24*60*2/.25/speed;i++){s.minute+=.25*speed;if(s.minute>=1440){s.minute-=1440;s.day++;}tickWorkshopLife(s,.25);const l=s.workshop.life;furthest=Math.max(furthest,l.tessa.x);assert.ok(workshopClear(l.tessa.x,l.tessa.z,.28),JSON.stringify(l.tessa));assert.ok(l.parked.length<=3);assert.ok(l.people.length<=1);for(const p of l.people){kinds.add(p.kind);phases.add(p.phase);if(p.phase==='leaving')served.add(p.id);assert.ok(workshopClear(p.x,p.z,.28),JSON.stringify(p));}}
  assert.deepEqual([...kinds].sort(),['dropoff','pickup']);assert.ok(phases.has('leaving'));assert.ok(furthest>339);assert.ok(served.size>=4);if(speed===1)assert.ok(furthest>=342.5);assert.equal(s.money,money);s.workshop.active=null;assert.doesNotThrow(()=>validateSave(s));
 }
});
test('bad saved receipts and duplicate customer assignments are rejected',()=>{
 const m=fresh();m.workshopAction('accept','stock');while(workshopStep(m.s))step(m);m.s.position={...WORKSHOP_POINTS.workshopDesk};m.workshopAction('finish');const bad=structuredClone(m.s);bad.workshop.last.minutes='<img src=x>';assert.throws(()=>validateSave(bad));
 const other=structuredClone(m.s);other.workshop.life.parked=[{id:1,owner:0,bike:0,ready:100},{id:1,owner:0,bike:0,ready:100}];assert.throws(()=>validateSave(other));
});

function kit(){const geometry=new THREE.BoxGeometry(),materials=new Map(),k={mat:c=>{if(!materials.has(c))materials.set(c,new THREE.MeshStandardMaterial({color:c}));return materials.get(c);}};k.box=(p,x,y,z,w,h,d,c,m)=>{const v=new THREE.Mesh(geometry,m||k.mat(c));v.position.set(x,y,z);v.scale.set(w,h,d);p.add(v);return v;};k.cylinder=(p,x,y,z,r,h,c)=>{const v=new THREE.Mesh(new THREE.CylinderGeometry(1,1,1,8),k.mat(c));v.position.set(x,y,z);v.scale.set(r,h,r);p.add(v);return v;};k.sphere=(p,x,y,z,r,c)=>{const v=new THREE.Mesh(new THREE.IcosahedronGeometry(1,1),k.mat(c));v.position.set(x,y,z);v.scale.setScalar(r);p.add(v);return v;};k.sign=(p,text,x,y,z,w,h,c,b,angle=0)=>{const v=k.box(p,x,y,z,w,h,.001,0xffffff);v.name=text;v.rotation.y=angle;return v;};return k;}
function fixture(){const k=kit(),world={scene:new THREE.Scene(),batchStaticGroup:()=>{},player:createCitizen(k)},scene=new WorkshopScene(world,k);return {world,scene};}
function pose(world,scene,m,dt=.1){world.player.position.set(m.s.position.x,.02,m.s.position.z);scene.resetPose(world.player);animateCitizen(world.player,dt,0);scene.update(m.s,dt);world.scene.updateMatrixWorld(true);}
test('work posture keeps soles planted; pump hand reaches the handle and props reset',()=>{
 const {world,scene}=fixture(),m=fresh();m.workshopAction('accept','check');const a=m.s.workshop.active;a.variant=3;a.step=2;m.s.position={...WORKSHOP_POINTS.workshopBike};m.workshopAction('step','adjust');for(let i=0;i<10;i++){m.updateTime(.1);pose(world,scene,m);}
 assert.equal(scene.pump.visible,true);const hand=world.player.userData.elbows[1].localToWorld(new THREE.Vector3(0,-.22,.026)),handle=scene.pumpHandle.localToWorld(new THREE.Vector3(0,.94,0));assert.ok(hand.distanceTo(handle)<.045,`pump grip distance ${hand.distanceTo(handle)}`);
 for(const foot of world.player.userData.feet){const sole=foot.localToWorld(new THREE.Vector3(0,-.065,0));near(sole.y,.07,.008);}
 m.s.workshop.active.action=null;m.s.workshop.active.step=4;pose(world,scene,m);assert.equal(scene.pump.visible,false);assert.equal(scene.tools.visible,false);assert.ok(world.player.userData.upper.position.y>.89);
});
test('customers push grounded bicycles with real hand contact and independent paint',()=>{
 const {world,scene}=fixture(),m=fresh();m.s.workshop.life=newWorkshopLife();tickWorkshopLife(m.s,.1);const p=m.s.workshop.life.people[0];p.x=337.1;p.z=86;p.angle=0;pose(world,scene,m);const v=scene.customers.get(p.id),hand=v.actor.userData.elbows[1].localToWorld(new THREE.Vector3(0,-.22,.026)),grip=v.bike.localToWorld(new THREE.Vector3(.43,1.61,.29));assert.ok(hand.distanceTo(grip)<.04,`push grip distance ${hand.distanceTo(grip)}`);
 const tire=v.bike.children.find(o=>o.isGroup).localToWorld(new THREE.Vector3(0,-.408,0));near(tire.y,.07);
 const paint=v.bike.children.find(o=>o.userData.frame).material.color.getHex();m.workshopAction('accept','check');m.s.workshop.active.variant=2;pose(world,scene,m);assert.equal(v.bike.children.find(o=>o.userData.frame).material.color.getHex(),paint);assert.notEqual(v.bike.children.find(o=>o.userData.frame).material,scene.frameParts[0].material);
});
test('wheel removal moves only the correct wheel and saved mid-task stays finite',()=>{
 for(const variant of [0,1]){const {world,scene}=fixture(),m=fresh();m.workshopAction('accept','tube');const a=m.s.workshop.active;a.variant=variant;m.s.position={...WORKSHOP_POINTS.workshopBike};m.workshopAction('step',workshopStep(m.s).correct);const t=a.action;t.elapsed=t.duration*.85;t.age=6;m.s.position=workshopWorkPoint(a,workshopStep(m.s));pose(world,scene,m);assert.ok(scene.wheels[variant?0:1].position.z>.1);assert.equal(scene.wheels[variant?1:0].position.z,0);scene.root.traverse(o=>assert.ok(o.matrixWorld.elements.every(Number.isFinite)));assert.doesNotThrow(()=>validateSave(m.s));a.action=null;a.step=1;pose(world,scene,m);assert.equal(scene.wheels[variant?0:1].visible,false);assert.equal(scene.wheels[variant?0:1].rotation.y,0);}
});

test('actual express handoff stocks one part and a new repair consumes it without extra pay',async()=>{
 const {deliveryTarget}=await import('../dist/delivery-routes.js');const m=new GameModel(newGame(true));m.s.minute=480;m.s.position={...deliveryTarget('jobs')};assert.ok(m.acceptContract('workshopExpress'));for(let i=0;i<50&&m.s.job?.interaction;i++)m.updateTime(.1);m.s.position={...deliveryTarget('deliveryWorkshop')};assert.ok(m.work('deliver'));for(let i=0;i<50&&m.s.job;i++)m.updateTime(.1);assert.equal(m.s.job,null);assert.equal(m.s.workshop.expressStock,1);const money=m.s.money;assert.equal(m.finishJob(),false);assert.equal(m.s.money,money);assert.ok(m.enterInterior('workshop'));m.s.position={...WORKSHOP_POINTS.workshopDesk};m.s.workshop.training=1;assert.ok(m.workshopAction('accept','tube'));assert.equal(m.s.workshop.expressStock,0);assert.equal(m.s.workshop.active.expressPart,true);assert.equal(m.s.money,money);assert.doesNotThrow(()=>validateSave(m.s));
});

test('Tessa interrupts work for reception and customers wait until she is actually back',()=>{
 const m=fresh(),s=m.s,l=s.workshop.life=newWorkshopLife();s.position={x:340,z:83};l.serial=1;l.nextArrival=99999;l.tessa={x:342.5,z:78.4,segment:4,offset:0,phase:2,pause:18};l.people=[{id:1,kind:'dropoff',job:null,owner:1,bike:1,withBike:true,phase:'waiting',node:0,x:CUSTOMER_ROUTE.at(-1).x,z:CUSTOMER_ROUTE.at(-1).z,angle:0,wait:12}];
 tickWorkshopLife(s,.25);assert.equal(l.tessa.phase,1);assert.equal(l.people[0].wait,12);assert.ok(l.tessa.x<342.5);let served=false;
 for(let i=0;i<180;i++){s.minute+=.25;tickWorkshopLife(s,.25);if(l.people.some(p=>p.id===1&&p.phase==='leaving'))served=true;}
 assert.ok(served);assert.equal(l.people.some(p=>p.id===1),false);assert.equal(l.parked.length,1);assert.doesNotThrow(()=>validateSave(s));
});
