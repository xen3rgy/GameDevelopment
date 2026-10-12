import test from 'node:test';
import assert from 'node:assert/strict';
import {GameModel,newGame,validateSave} from '../dist/model.js';
import {LOCATIONS,ITEMS,WEST_KIOSK_ITEMS,QUESTS} from '../dist/data.js';
import {WEST_SPAWN,WEST_BINS,WEST_BOTTLE_SPOTS,WEST_BUILDING_FIXTURES} from '../dist/west-edge-layout.js';
import {bottleLayout,dailyBottles,newScavenge,beginBinSearch,tickBinSearch,binReward} from '../dist/scavenge.js';
import {actionOffer} from '../dist/daily-life.js';
const at=(m,id)=>m.s.position={...LOCATIONS.find(p=>p.id===id)};
const load=m=>new GameModel(validateSave(JSON.parse(JSON.stringify(m.s))));
const close=(a,b)=>assert.ok(Math.abs(a-b)<1e-7,`${a} != ${b}`);

test('only new story starts in Bruchfeld; sandbox and existing save locations survive',()=>{
 const story=newGame(),sandbox=newGame(true);
 assert.deepEqual(story.position,WEST_SPAWN);assert.equal(story.money,0);assert.deepEqual(story.inventory,[]);assert.equal(story.home,null);assert.equal(story.vehicle,null);
 assert.deepEqual(story.needs,{health:100,hunger:65,thirst:60,energy:60,hygiene:40,stress:22});
 assert.deepEqual(sandbox.position,{x:-27,z:-4});assert.equal(sandbox.money,10000000);assert.equal(sandbox.needs.energy,85);
 assert.deepEqual(validateSave(story).position,WEST_SPAWN);
 for(const version of [1,2,3]){const old=newGame();old.version=version;old.position={x:-32,z:9};old.money=4312;delete old.scavenge;const migrated=validateSave(old);assert.deepEqual(migrated.position,old.position);assert.equal(migrated.money,4312);assert.equal(migrated.scavenge.layout,1);}
});

test('every day-one seed affords roll and water through eight dispersed reachable western bottles',()=>{
 for(let seed=0;seed<128;seed++){
  const points=dailyBottles(seed,1,true),west=points.filter(p=>p.x<-260);
  assert.ok(west.length>=8);assert.ok(points.length<=15);
  assert.ok(west.length*ITEMS.bottle.price>=ITEMS.roll.price+ITEMS.water.price);
  for(const p of west){assert.ok(WEST_BOTTLE_SPOTS.some(q=>q.x===p.x&&q.z===p.z));assert.ok(Math.hypot(p.x-WEST_SPAWN.x,p.z-WEST_SPAWN.z)>15);assert.ok(!WEST_BUILDING_FIXTURES.some(b=>Math.abs(p.x-b.x)<b.w+.35&&Math.abs(p.z-b.z)<b.d+.35));}
  for(let i=0;i<points.length;i++)for(let j=0;j<i;j++)assert.ok(Math.hypot(points[i].x-points[j].x,points[i].z-points[j].z)>7);
 }
 const m=new GameModel();m.s.scavenge=newScavenge(54);const before=bottleLayout(m.s);m.collect(before[0].id);const reloaded=load(m);assert.deepEqual(bottleLayout(reloaded.s),before);assert.equal(reloaded.collect(before[0].id),false);
 // Existing seed/ID layouts remain on the old candidate set after loading.
 delete m.s.scavenge.layout;const old=bottleLayout(m.s);assert.ok(old.every(p=>p.x>=-260));assert.deepEqual(bottleLayout(load(m).s),old);
});

test('western bin claims resume without rerolls or duplicate rewards',()=>{
 for(const bin of WEST_BINS){
  let m=new GameModel();m.s.position={x:bin.x,z:bin.z};m.s.scavenge=newScavenge(19);const reward=binReward(19,1,bin.id);
  assert.ok(beginBinSearch(m,bin.id));tickBinSearch(m,1);m=load(m);tickBinSearch(m,1.3);
  if(reward)assert.equal(m.count(reward.id),reward.count);else assert.equal(m.s.inventory.length,0);
  const inventory=structuredClone(m.s.inventory);assert.equal(beginBinSearch(m,bin.id),false);assert.equal(beginBinSearch(load(m),bin.id),false);tickBinSearch(m,20);assert.deepEqual(m.s.inventory,inventory);
 }
});

test('local return and kiosk reuse prices, restrict stock and require a free nearby player',()=>{
 const m=new GameModel();m.s.scavenge=newScavenge(9);for(const p of bottleLayout(m.s).slice(0,8))m.collect(p.id);
 assert.equal(m.s.money,0);assert.equal(m.s.quest,1);assert.equal(m.recycleWest(),false);
 at(m,'westRecycle');assert.ok(m.recycleWest());assert.equal(m.s.money,200);assert.equal(m.s.stats.returned,8);assert.equal(m.s.quest,2);assert.equal(m.recycleWest(),false);
 assert.equal(m.buyWestKiosk('roll'),false);at(m,'westKiosk');assert.ok(m.buyWestKiosk('roll'));assert.ok(m.buyWestKiosk('water'));assert.equal(m.s.money,0);
 assert.deepEqual(WEST_KIOSK_ITEMS,['roll','water','sandwich']);m.s.money=10000;assert.equal(m.buyWestKiosk('medicine'),false);m.s.riding=true;assert.equal(m.buyWestKiosk('roll'),false);m.s.riding=false;
 m.s.money=0;assert.ok(m.beginLifeAction('consume',0));m.tickLifeAction(10);assert.equal(m.s.quest,3);assert.equal(m.s.money,300);assert.equal(QUESTS[3].target,'jobs');
 assert.deepEqual(load(m).s.inventory,m.s.inventory);
});

test('rough sleep is free, inferior to a bed and advances needs; resumes and cancels correctly',()=>{
 let m=new GameModel();assert.equal(m.beginLifeAction('roughSleep'),false);at(m,'roughSleep');m.s.needs.energy=10;
 const start=structuredClone(m.s.needs),minute=m.s.minute,offer=actionOffer(m.s,'roughSleep');assert.equal(offer.minutes,360);assert.equal(offer.cost,0);
 assert.ok(m.beginLifeAction('roughSleep'));m.tickLifeAction(3);m=load(m);m.tickLifeAction(7);
 close(m.s.needs.energy,65);close(m.s.needs.hunger,start.hunger-9);close(m.s.needs.thirst,start.thirst-12.24);close(m.s.needs.hygiene,start.hygiene-7.04);close(m.s.needs.stress,start.stress-4);
 assert.equal(m.s.needs.health,100);assert.equal(m.s.money,0);assert.equal(m.s.minute,minute+360);assert.equal(m.s.stats.slept,0);assert.equal(m.s.dailyLife.lastRest.kind,'roughSleep');
 m.beginLifeAction('roughSleep');m.tickLifeAction(10);close(m.s.needs.energy,80);
 m.s.needs.energy=95;m.beginLifeAction('roughSleep');m.tickLifeAction(10);close(m.s.needs.energy,95);
 m.s.needs.energy=10;m.beginLifeAction('roughSleep');m.tickLifeAction(2);const energy=m.s.needs.energy;m.cancelLifeAction();m.tickLifeAction(10);close(m.s.needs.energy,energy);assert.equal(m.s.dailyLife.lastRest.minutes,72);
 assert.doesNotThrow(()=>validateSave(m.s));
});

test('a 20-minute unhurried first exploration matters without killing the player',()=>{
 const m=new GameModel();m.advance(1200);
 assert.ok(m.s.needs.hunger>=34);assert.ok(m.s.needs.thirst>=19);assert.ok(m.s.needs.energy>=32);assert.equal(m.s.needs.health,100);
});
