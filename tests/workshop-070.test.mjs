import test from 'node:test';
import assert from 'node:assert/strict';
import * as THREE from '../dist/vendor/three.module.js';
import {GameModel,newGame,validateSave} from '../dist/model.js';
import {WORKSHOP_ROOM,WORKSHOP_POINTS,WORKSHOP_FIXTURES,atWorkshop,workshopClear} from '../dist/workshop-layout.js';
import {WORKSHOP_TYPES,workshopStep,workshopSteps,workshopPayout,newWorkshop} from '../dist/workshop.js';
import {workshopDeskPanel,workshopStationPanel,workshopHud} from '../dist/workshop-ui.js';
import {WorkshopScene} from '../dist/workshop-scene.js';
import {World} from '../dist/world.js';
import {createCitizen} from '../dist/art.js';
import {animateCitizen} from '../dist/animation.js';
import {canWalkRoom,CameraRig} from '../dist/spatial.js';
import {readSavedGame,writeSavedGame} from '../dist/persistence.js';
const near=(a,b)=>assert.ok(Math.abs(a-b)<1e-7,`${a} != ${b}`);
function fresh(){const m=new GameModel(newGame(true));m.s.minute=480;m.s.position={x:-174,z:42.8};assert.ok(m.enterInterior('workshop'));m.s.position={...WORKSHOP_POINTS.workshopDesk};return m;}
function doStep(m,choice){const c=workshopStep(m.s);m.s.position={x:WORKSHOP_POINTS[c.point].x,z:WORKSHOP_POINTS[c.point].z};assert.ok(m.workshopAction('step',choice??c.correct));const duration=m.s.workshop.active.action.duration+(m.s.workshop.active.action.approachDuration??0)*m.s.settings.speed;for(let i=0;i<Math.ceil(duration*10/m.s.settings.speed)+1&&m.s.workshop.active?.action;i++)m.updateTime(.1);assert.equal(m.s.workshop.active?.action,null);}
function finish(m){while(workshopStep(m.s))doStep(m);m.s.position={...WORKSHOP_POINTS.workshopDesk};assert.ok(m.workshopAction('finish'));}
test('all three workshop jobs run through physical stations and pay once only after acceptance',()=>{
 for(const type of Object.keys(WORKSHOP_TYPES))for(const variant of [0,1]){const m=fresh();m.s.workshop.training=WORKSHOP_TYPES[type].level;assert.ok(m.workshopAction('accept',type));m.s.workshop.active.variant=variant;const money=m.s.money;assert.equal(m.workshopAction('finish'),false);assert.equal(m.workshopAction('step','made-up'),false);
  while(workshopStep(m.s)){const old=m.s.workshop.active.step;doStep(m);assert.equal(m.s.workshop.active.step,old+1);assert.equal(m.s.money,money);assert.doesNotThrow(()=>validateSave(m.s));}
  const pay=workshopPayout(m.s.workshop.active,m.s.workshop.training);assert.equal(m.workshopAction('finish'),false);m.s.position={...WORKSHOP_POINTS.workshopDesk};assert.ok(m.workshopAction('finish'));assert.equal(m.s.money-money,pay.total);assert.equal(m.s.workshop.xp,WORKSHOP_TYPES[type].xp);assert.equal(m.s.stats.jobs,1);assert.equal(m.workshopAction('finish'),false);assert.equal(m.s.money-money,pay.total);assert.doesNotThrow(()=>validateSave(m.s));
 }
});
test('incorrect choices require rework, remove bonus and cap deductions without negative wages',()=>{
 const m=fresh();m.workshopAction('accept','stock');const c=workshopStep(m.s),wrong=c.choices.find(v=>v[0]!==c.correct)[0];for(let i=0;i<8;i++)doStep(m,wrong);assert.equal(m.s.workshop.active.step,0);assert.equal(m.s.workshop.active.mistakes,8);finish(m);assert.equal(m.s.workshop.last.bonus,0);assert.equal(m.s.workshop.last.deduction,263);assert.equal(m.s.workshop.last.total,487);
});
test('workshop hours, daily limit and cancellation cannot farm cash or training progress',()=>{
 const m=fresh();m.leaveInterior();m.s.position={x:-174,z:42.8};m.s.minute=479;assert.equal(m.enterInterior('workshop'),false);m.s.minute=480;assert.ok(m.enterInterior('workshop'));m.s.position={...WORKSHOP_POINTS.workshopDesk};const money=m.s.money;
 for(let i=0;i<4;i++){assert.ok(m.workshopAction('accept','stock'));assert.ok(m.workshopAction('cancel'));}assert.equal(m.workshopAction('accept','stock'),false);assert.equal(m.s.money,money);assert.equal(m.s.workshop.xp,0);m.s.day++;m.s.minute=1020;assert.equal(m.workshopAction('accept','stock'),false);m.s.minute=480;assert.ok(m.workshopAction('accept','stock'));assert.equal(m.s.workshop.taken,1);m.advance(660);assert.equal(m.s.workshop.active,null);assert.equal(m.s.workshop.last.status,'expired');assert.equal(m.workshopAction('finish'),false);assert.ok(m.leaveInterior());assert.equal(m.enterInterior('workshop'),false);
});
test('courses require actual practice, charge once, advance time and unlock shorter higher-level work',()=>{
 const m=fresh(),money=m.s.money;assert.equal(m.workshopAction('accept','tube'),false);assert.equal(m.workshopAction('train'),false);assert.equal(m.s.money,money);
 for(let i=0;i<3;i++){m.s.position={...WORKSHOP_POINTS.workshopDesk};m.workshopAction('accept','check');finish(m);}assert.equal(m.s.workshop.xp,48);const before=m.s.money,time=m.s.minute;assert.ok(m.workshopAction('train'));assert.equal(m.s.money,before-6000);near(m.s.minute,time+90);assert.equal(m.s.workshop.training,1);assert.equal(m.workshopAction('train'),false);assert.ok(m.workshopAction('accept','tube'));
 const c=workshopStep(m.s);m.s.position={...WORKSHOP_POINTS[c.point]};assert.ok(m.workshopAction('step',c.correct));near(m.s.workshop.active.action.duration,c.minutes*.92);assert.equal(m.workshopAction('train'),false);
});
test('clock and work progress agree at 1x, 4x and 10x, with no double time advance',()=>{
 for(const speed of [1,4,10]){const m=fresh();m.s.settings.speed=speed;m.workshopAction('accept','stock');const c=workshopStep(m.s);m.s.position={...WORKSHOP_POINTS[c.point]};m.workshopAction('step',c.correct);const before=m.s.minute;m.updateTime(.4);near(m.s.minute-before,.4*speed);near(m.s.workshop.active.action.elapsed,.4*speed);const snapshot=JSON.stringify(m.s);m.updateTime(0);assert.equal(JSON.stringify(m.s),snapshot);}
});
test('running work cannot overlap another job, course, movement exit or daily activity',()=>{
 const m=fresh();m.workshopAction('accept','stock');assert.equal(m.startJob('warehouse'),false);const c=workshopStep(m.s);m.s.position={...WORKSHOP_POINTS[c.point]};m.workshopAction('step',c.correct);m.add('water');assert.equal(m.beginLifeAction('consume',0),false);assert.equal(m.leaveInterior(),false);assert.equal(m.workshopAction('cancel'),false);assert.equal(m.cafeAction('start'),false);assert.equal(m.workshopAction('step',c.correct),false);
});
test('existing saves migrate, interrupted work resumes and malformed task imports are rejected',()=>{
 const old=newGame(true);delete old.workshop;assert.deepEqual(validateSave(old).workshop,newWorkshop());assert.deepEqual(new GameModel(old).s.workshop,newWorkshop());
 const m=fresh();m.workshopAction('accept','check');const c=workshopStep(m.s);m.s.position={...WORKSHOP_POINTS[c.point]};m.workshopAction('step',c.correct);m.updateTime(.2);
 const data=new Map(),store={getItem:k=>data.get(k)||null,setItem:(k,v)=>data.set(k,v)};writeSavedGame(store,m.s);const restored=new GameModel(readSavedGame(store).state);assert.deepEqual(restored.s,m.s);restored.updateTime(15);assert.equal(restored.s.workshop.active.step,1);assert.equal(restored.s.workshop.active.action,null);
 for(const edit of [s=>s.workshop.training=3,s=>s.workshop.active.action.duration=NaN,s=>s.workshop.active.action.choice='bad',s=>s.workshop.active.action.origin={x:340,z:88},s=>s.workshop.active.step=99,s=>s.workshop.active.action.elapsed=-1,s=>s.workshop.taken=8]){const bad=structuredClone(m.s);edit(bad);assert.throws(()=>validateSave(bad));}
});
test('collapse interrupts tools safely and expiry while sleeping never grants delayed pay',()=>{
 const m=fresh();m.workshopAction('accept','stock');const c=workshopStep(m.s);m.s.position={...WORKSHOP_POINTS[c.point]};m.workshopAction('step',c.correct);m.s.needs.health=0;m.updateTime(.1);assert.equal(m.s.inside,false);assert.equal(m.s.workshop.active.action,null);assert.doesNotThrow(()=>validateSave(m.s));const money=m.s.money;m.advance(1440,true);assert.equal(m.s.workshop.active,null);assert.equal(m.s.money,money);assert.equal(m.s.workshop.last.status,'expired');
});
test('every workshop station has a connected walking approach; furniture cannot be reached through',()=>{
 const room=WORKSHOP_ROOM,queue=[room.spawn],seen=new Set(),delta=.2;for(let i=0;i<queue.length;i++){const p=queue[i];for(const [dx,dz] of [[delta,0],[-delta,0],[0,delta],[0,-delta]]){const n={x:Math.round((p.x+dx)*10)/10,z:Math.round((p.z+dz)*10)/10},key=n.x+','+n.z;if(!seen.has(key)&&canWalkRoom('workshop',n.x,n.z)){seen.add(key);queue.push(n);}}}
 for(const [id,p]of Object.entries(WORKSHOP_POINTS)){assert.ok(workshopClear(p.x,p.z),id);assert.ok(queue.some(v=>Math.hypot(v.x-p.x,v.z-p.z)<.25),id);const s=fresh().s;s.position={x:p.x,z:p.z};assert.ok(atWorkshop(s,id));for(let angle=0;angle<Math.PI*2;angle+=.4){const view=new CameraRig().update({...p,y:.02},angle,.35,6,0,WORKSHOP_FIXTURES,room);assert.ok(view.distance>.15);}}
 const s=fresh().s;s.position={x:336,z:81.1};assert.equal(atWorkshop(s,'workshopTest'),false);for(const f of WORKSHOP_FIXTURES)assert.equal(workshopClear(f.x,f.z),false);
});
function kit(){const geometry=new THREE.BoxGeometry(),materials=new Map(),k={mat:c=>{if(!materials.has(c))materials.set(c,new THREE.MeshStandardMaterial({color:c}));return materials.get(c);}};k.box=(p,x,y,z,w,h,d,c,m)=>{const v=new THREE.Mesh(geometry,m||k.mat(c));v.position.set(x,y,z);v.scale.set(w,h,d);p.add(v);return v;};k.cylinder=(p,x,y,z,r,h,c)=>{const v=new THREE.Mesh(new THREE.CylinderGeometry(1,1,1,8),k.mat(c));v.position.set(x,y,z);v.scale.set(r,h,r);p.add(v);return v;};k.sphere=(p,x,y,z,r,c)=>{const v=new THREE.Mesh(new THREE.IcosahedronGeometry(1,1),k.mat(c));v.position.set(x,y,z);v.scale.setScalar(r);p.add(v);return v;};k.sign=(p,text,x,y,z,w,h,c,b,angle=0)=>{const v=k.box(p,x,y,z,w,h,.001,0xffffff);v.name=text;v.rotation.y=angle;return v;};return k;}
test('workshop scene builds without invalid geometry; tools follow hands and disappear outside',()=>{
 const k=kit(),world={scene:new THREE.Scene(),batchStaticGroup:World.prototype.batchStaticGroup,player:createCitizen(k)},scene=new WorkshopScene(world,k),m=fresh();m.workshopAction('accept','check');const c=workshopStep(m.s);m.s.position={...WORKSHOP_POINTS[c.point]};m.workshopAction('step',c.correct);
 for(let i=0;i<12;i++){m.updateTime(.1);world.player.position.set(m.s.position.x,.02,m.s.position.z);animateCitizen(world.player,.1,0);scene.update(m.s,.1);world.scene.updateMatrixWorld(true);scene.root.traverse(o=>assert.ok(o.matrixWorld.elements.every(Number.isFinite)));}
 assert.equal(scene.root.visible,true);assert.equal(scene.tools.visible,true);assert.equal(scene.tools.parent,world.player.userData.elbows[1]);assert.ok(scene.root.children.filter(v=>v.isLight).length<=2);m.s.interior=null;scene.update(m.s,.1);assert.equal(scene.root.visible,false);assert.equal(scene.tools.visible,false);
});
test('work cards explain actual current stage and expose no remote completion action',()=>{
 const m=fresh(),b=(text,action,arg,cls,disabled)=>`<button data-action="${action}" ${disabled?'disabled':''}>${text}</button>`;assert.match(workshopDeskPanel(m.s,b),/7,50/);m.workshopAction('accept','stock');assert.match(workshopStationPanel(m.s,'workshopIntake',b),/Lieferschein/);assert.doesNotMatch(workshopStationPanel(m.s,'workshopStock',b),/data-action="workshopStep"/);assert.match(workshopHud(m.s),/Abgabe bis 19:00/);
});

test('visible workshop loads limit movement and restore free hands after placing parts',async()=>{
 const {walkingProfile}=await import('../dist/player-movement.js');const m=fresh();m.workshopAction('accept','stock');doStep(m);let gait=walkingProfile(m.s,true);assert.equal(gait.running,false);assert.equal(gait.canJump,false);assert.equal(gait.speed,2.45);doStep(m);gait=walkingProfile(m.s,true);assert.equal(gait.load,null);assert.equal(gait.running,true);
});
test('carried wheel has real hand contact and never remains visible in the city',()=>{
 const k=kit(),world={scene:new THREE.Scene(),batchStaticGroup:World.prototype.batchStaticGroup,player:createCitizen(k)},scene=new WorkshopScene(world,k),m=fresh();m.s.workshop.training=1;m.workshopAction('accept','tube');doStep(m);world.player.position.set(m.s.position.x,.02,m.s.position.z);animateCitizen(world.player,.1,.1);scene.update(m.s,.1);assert.equal(scene.carryRoot.visible,true);assert.equal(scene.carryWheel.visible,true);world.player.updateMatrixWorld(true);scene.carryRoot.updateMatrixWorld(true);
 for(const i of [0,1]){const hand=world.player.userData.elbows[i].localToWorld(new THREE.Vector3(0,-.22,.026)),grip=scene.carryRoot.localToWorld(new THREE.Vector3(i===0?-.265:.265,-.03,-.17));assert.ok(hand.distanceTo(grip)<.04);}
 m.s.interior=null;scene.resetPose(world.player);animateCitizen(world.player,.1,.1);scene.update(m.s,.1);assert.equal(scene.carryRoot.visible,false);assert.equal(scene.hadCarry,false);
});
