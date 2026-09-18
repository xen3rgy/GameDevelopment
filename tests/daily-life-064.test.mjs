import test from 'node:test';
import assert from 'node:assert/strict';
import * as THREE from '../dist/vendor/three.module.js';
import {GameModel,newGame,validateSave} from '../dist/model.js';
import {timePreview,fatigue,actionOffer} from '../dist/daily-life.js';
import {walkingProfile} from '../dist/player-movement.js';
import {clockMinutes,deadlineView} from '../dist/contracts.js';
import {HOME_POINTS,LOCATIONS} from '../dist/data.js';
import {ROOMS,canWalkRoom} from '../dist/spatial.js';
import {itemCount,insertItem} from '../dist/inventory.js';
import {HomeScene} from '../dist/home-scene.js';
import {createCitizen} from '../dist/art.js';
import {timeCard,sleepPanel,dailyHints} from '../dist/home-ui.js';
const at=(m,id)=>m.s.position={...HOME_POINTS[id]};
function home(id='flat'){const m=new GameModel(newGame(true));m.s.home=id;m.s.inside=true;m.s.interior='home';m.s.rentDue=4;at(m,'bed');return m;}
function finish(m){for(let i=0;i<150&&m.s.dailyLife.action;i++)m.updateTime(.1);assert.equal(m.s.dailyLife.action,null);}

test('chosen sleep crosses midnight exactly, pauses and resumes, independent of clock multiplier',()=>{
 for(const speed of [1,4,10]){let m=home();m.s.minute=23*60;m.s.settings.speed=speed;m.s.needs.energy=15;m.s.dailyLife.awakeMinutes=1300;
  const start=clockMinutes(m.s),cash=m.s.money,p=timePreview(m.s,480);assert.equal(p.end.time,'07:00');assert.equal(p.end.day,2);
  assert.ok(m.beginLifeAction('sleep',8));m.updateTime(3);const before=structuredClone(m.s);m.updateTime(0);assert.deepEqual(m.s,before);
  m=new GameModel(validateSave(m.s));finish(m);assert.ok(Math.abs(clockMinutes(m.s)-start-480)<.00001);assert.ok(m.s.needs.energy>90);assert.equal(m.s.stats.slept,1);assert.equal(m.s.money,cash);assert.ok(m.s.dailyLife.awakeMinutes<400);assert.doesNotThrow(()=>validateSave(m.s));
 }
});
test('early waking preserves only elapsed rest, poor nutrition lowers recovery, coffee does not remove sleep pressure',()=>{
 const m=home();m.s.needs.energy=10;m.s.dailyLife.awakeMinutes=1500;const start=clockMinutes(m.s);m.beginLifeAction('sleep',8);m.updateTime(2);const energy=m.s.needs.energy;m.cancelLifeAction();finish(m);assert.equal(m.s.needs.energy,energy);assert.ok(Math.abs(clockMinutes(m.s)-start-96)<.00001);assert.equal(m.s.dailyLife.lastRest.minutes,96);
 const healthy=home(),hungry=home();for(const x of [healthy,hungry]){x.s.needs.energy=10;x.s.needs.health=100;}hungry.s.needs.hunger=3;hungry.s.needs.thirst=3;
 healthy.beginLifeAction('sleep',4);hungry.beginLifeAction('sleep',4);finish(healthy);finish(hungry);assert.ok(healthy.s.needs.energy>hungry.s.needs.energy+15);
 m.s.dailyLife.awakeMinutes=1600;m.s.needs.energy=15;m.add('coffee');m.use(0);assert.ok(m.s.needs.energy>30);assert.equal(fatigue(m.s),1);assert.equal(walkingProfile(m.s,true).running,false);
});
test('fridge accepts food only, transfer failure is atomic and kitchen uses all three inventories',()=>{
 const m=home();at(m,'fridge');m.add('water',6);assert.ok(m.fridgeTransfer(0,'deposit'));assert.equal(m.count('water'),5);assert.equal(itemCount(m.s.fridge,'water'),1);
 m.add('medicine');assert.equal(m.fridgeTransfer(1,'deposit'),false);m.add('parcel');assert.equal(m.fridgeTransfer(2,'deposit'),false);
 m.s.inventory=[];m.add('bottle',200);const before=structuredClone(m.s.fridge);assert.equal(m.fridgeTransfer(0,'withdraw'),false);assert.deepEqual(m.s.fridge,before);
 m.s.inventory=[];m.s.fridge=[];m.add('pasta');insertItem(m.s.fridge,'vegetables',1,12,20);at(m,'kitchen');const minute=m.s.minute;
 assert.ok(m.beginLifeAction('cook','pasta'));m.updateTime(2);assert.equal(m.count('meal'),0);const saved=validateSave(m.s);const loaded=new GameModel(saved);finish(loaded);assert.equal(loaded.count('meal'),2);assert.equal(itemCount(loaded.s.fridge,'vegetables'),0);assert.ok(Math.abs(loaded.s.minute-minute-30)<.00001);assert.equal(loaded.s.stats.cooked,1);
});
test('food is applied once at the end of an animation and an aborted meal remains in inventory',()=>{
 let m=home();m.s.needs.thirst=20;m.add('water',2);assert.ok(m.beginLifeAction('consume',0));assert.equal(m.beginLifeAction('consume',0),false);m.updateTime(1);assert.equal(m.count('water'),2);m=new GameModel(validateSave(m.s));finish(m);assert.equal(m.count('water'),1);assert.equal(m.count('bottle'),1);assert.equal(m.s.stats.consumed,1);assert.ok(m.s.needs.thirst>60);
 m.beginLifeAction('consume',0);m.updateTime(1);m.cancelLifeAction();finish(m);assert.equal(m.count('water'),1);assert.equal(m.s.stats.consumed,1);m.updateTime(10);assert.equal(m.count('bottle'),1);
 m.s.inside=false;m.s.interior=null;m.s.job={type:'courier'};assert.equal(m.beginLifeAction('consume',0),false);
});
test('time previews expose deadline, closing and midnight consequences while actions use the common clock',()=>{
 const m=new GameModel(newGame(true));m.s.position={x:26,z:-12};m.acceptContract('bookExpress');m.tickCourier(2.2);const deadline=deadlineView(m.s).remaining;m.s.home='flat';m.s.inside=true;m.s.interior='home';m.s.rentDue=4;at(m,'bed');const p=timePreview(m.s,480);assert.ok(p.warnings.some(v=>v.includes('Expressfrist')));assert.match(timeCard(m.s,480),/Zeitbonus geht verloren/);
 m.beginLifeAction('sleep',8);finish(m);assert.ok(deadlineView(m.s).late);assert.ok(deadline<480);assert.equal(m.s.job.progress,0);
 m.s.minute=1190;m.s.cafe={...m.s.cafe,phase:'open',duration:480,elapsed:470};assert.ok(timePreview(m.s,20).warnings.some(v=>v.includes('schließt')));
 m.s.minute=1430;assert.ok(timePreview(m.s,20).warnings.some(v=>v.includes('Mitternacht')));
});
test('older saves preserve supplies and migrate gently; impossible saved routines are rejected',()=>{
 const m=home();m.add('sandwich');m.s.storage=[{id:'cheese',count:2}];const old=structuredClone(m.s);delete old.fridge;delete old.dailyLife;const migrated=validateSave(old);assert.deepEqual(migrated.fridge,[]);assert.deepEqual(migrated.storage,old.storage);assert.equal(migrated.dailyLife.awakeMinutes,600);
 m.beginLifeAction('sleep',8);const bad=structuredClone(m.s);bad.dailyLife.action.minutes=600;assert.throws(()=>validateSave(bad));bad.dailyLife.action.minutes=480;bad.dailyLife.action.origin.x=NaN;assert.throws(()=>validateSave(bad));
 const invalid=structuredClone(m.s);invalid.fridge=[{id:'parcel',count:1}];assert.throws(()=>validateSave(invalid));
});
test('home interactions remain reachable around real fixtures in all three home tiers',()=>{
 for(const home of ['room','flat','penthouse']){const seen=new Set(),queue=[ROOMS.home.spawn],step=.25;
  for(let i=0;i<queue.length;i++){const p=queue[i];for(const [dx,dz] of [[step,0],[-step,0],[0,step],[0,-step]]){const n={x:p.x+dx,z:p.z+dz},key=n.x+','+n.z;if(!seen.has(key)&&canWalkRoom('home',n.x,n.z,.35,home)){seen.add(key);queue.push(n);}}}
  for(const point of Object.values(HOME_POINTS)){assert.ok(canWalkRoom('home',point.x,point.z,.35,home),point.name);assert.ok(queue.some(p=>Math.hypot(p.x-point.x,p.z-point.z)<.4),home+' '+point.name);}
  assert.equal(canWalkRoom('home',296,-3,.35,home),false);
 }
});
function kit(){const mat=c=>new THREE.MeshStandardMaterial({color:c});return {mat,box(p,x,y,z,w,h,d,c){const m=new THREE.Mesh(new THREE.BoxGeometry(w,h,d),mat(c));m.position.set(x,y,z);p.add(m);return m;},cylinder(p,x,y,z,r,h,c){const m=new THREE.Mesh(new THREE.CylinderGeometry(r,r,h,8),mat(c));m.position.set(x,y,z);p.add(m);return m;},sign(){return new THREE.Group();}};}
test('sleep pose, refrigerator door and consumable props follow the actual saved action',()=>{
 const k=kit(),world={scene:new THREE.Scene(),homeShell:new THREE.Group(),player:createCitizen(k)},visual=new HomeScene(world,k),m=home();world.scene.add(world.homeShell,world.player);
 m.beginLifeAction('sleep',8);m.updateTime(5);const origin=structuredClone(m.s.position);visual.update(m.s,.1);assert.deepEqual(m.s.position,origin);assert.ok(world.player.rotation.x<-1);assert.equal(world.player.userData.backpack.visible,false);assert.equal(visual.blanket.visible,true);const pos=world.player.position.clone();visual.update(m.s,0);assert.deepEqual(world.player.position,pos);
 finish(m);visual.update(m.s,.1);assert.equal(world.player.rotation.x,0);assert.equal(world.player.userData.backpack.visible,true);assert.equal(visual.blanket.visible,false);
 world.homeScreen='fridge';m.s.fridge=[{id:'water',count:2}];visual.update(m.s,1);assert.ok(visual.door.rotation.y<-1.5);assert.equal(visual.chilled.filter(v=>v.visible).length,2);
 m.add('water');m.beginLifeAction('consume',0);m.updateTime(1.5);visual.update(m.s,.1);assert.equal(visual.items.get('water').visible,true);assert.equal(visual.items.get('meal').visible,false);
 assert.match(sleepPanel(m.s,8,(label)=>label),/Stunden/);assert.ok(dailyHints({...m.s,dailyLife:{awakeMinutes:1700,action:null}}).some(h=>h.text.includes('Übermüdet')));
});

test('sleep simulates an unattended cafe and its daily accounting exactly as the same shared time advance',()=>{
 const base=home();base.s.inside=false;base.s.interior=null;base.s.position={x:63,z:-11};base.acquire('cafe');const b=base.s.businesses.cafe;b.staff=3;b.staffLevels=[3,3,3];base.s.minute=480;base.s.inside=true;base.s.interior='cafe';base.s.position={x:294,z:85};assert.ok(base.cafeAction('start'));
 base.s.inside=true;base.s.interior='home';at(base,'bed');base.s.needs.hunger=100;base.s.needs.thirst=100;
 const animated=new GameModel(structuredClone(base.s)),direct=new GameModel(structuredClone(base.s));assert.ok(animated.beginLifeAction('sleep',8));finish(animated);direct.advance(480,true,'sleep');
 assert.equal(animated.s.cafe.phase,'closed');assert.equal(animated.s.cafe.served,direct.s.cafe.served);assert.equal(animated.s.cafe.revenue,direct.s.cafe.revenue);assert.equal(animated.s.cafe.lastReport.profit,direct.s.cafe.lastReport.profit);
 animated.advance(480);direct.advance(480);assert.equal(animated.s.money,direct.s.money);assert.deepEqual(animated.s.businesses.cafe.cafeReport,direct.s.businesses.cafe.cafeReport);
});
test('showers restore hygiene progressively and a paid shelter stay resumes without charging twice',()=>{
 const m=home();at(m,'shower');m.s.needs.hygiene=0;m.beginLifeAction('shower');m.updateTime(2);assert.ok(m.s.needs.hygiene>0&&m.s.needs.hygiene<100);finish(m);assert.ok(m.s.needs.hygiene>99);
 let shelter=new GameModel(newGame());shelter.s.position={...LOCATIONS.find(l=>l.id==='shelter')};shelter.s.money=500;assert.ok(shelter.beginLifeAction('shelterSleep'));assert.equal(shelter.s.money,0);shelter.updateTime(1);shelter=new GameModel(validateSave(shelter.s));finish(shelter);assert.equal(shelter.s.money,0);assert.equal(shelter.s.stats.slept,1);
});
