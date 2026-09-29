import test from 'node:test';
import assert from 'node:assert/strict';
import {readFileSync} from 'node:fs';
import {newGame,GameModel,validateSave} from '../dist/model.js';
import {ITEMS,SHOP_POINTS} from '../dist/data.js';
import {newScavenge,bottleLayout,dailyBottles,binReward,beginBinSearch,tickBinSearch,cancelBinSearch,SEARCH_SECONDS} from '../dist/scavenge.js';
import {PUBLIC_BINS,BOTTLE_CANDIDATES,NIGHT_CLUSTERS} from '../dist/scavenge-layout.js';
const setup=(seed=123)=>{const m=new GameModel();m.s.scavenge=newScavenge(seed);m.s.position={x:PUBLIC_BINS[0].x,z:PUBLIC_BINS[0].z};return m;};
const winningSeed=()=>{for(let seed=0;seed<1000;seed++)if(binReward(seed,1,0)?.id==='bottle')return seed;};
test('new saves get independent seeds; layouts survive reload and vary by day/seed',()=>{
 const a=newGame(),b=newGame();assert.notEqual(a.scavenge.seed,b.scavenge.seed);
 const before=bottleLayout(a);assert.deepEqual(bottleLayout(validateSave(a)),before);
 a.day++;assert.notDeepEqual(bottleLayout(a),before);assert.notDeepEqual(dailyBottles(12,1),dailyBottles(13,1));
});
test('daily counts vary within bounds, story starts with five and locations are dispersed/curated',()=>{
 const counts=new Set();for(let seed=0;seed<300;seed++)for(const day of [1,2,3]){
  const points=dailyBottles(seed,day);counts.add(points.length);assert.ok(points.length>=(day===1?5:4)&&points.length<=15);
  for(const p of points){assert.ok(BOTTLE_CANDIDATES.some(q=>q.x===p.x&&q.z===p.z));assert.ok(Math.hypot(p.x+27,p.z+4)>8);}
  for(let i=0;i<points.length;i++)for(let j=0;j<i;j++)assert.ok(Math.hypot(points[i].x-points[j].x,points[i].z-points[j].z)>7);
 }assert.ok(counts.size>=8);
});
test('night bonus is rare, deterministic and never replaces or rerolls the daily set',()=>{
 let bonuses=0;for(let seed=0;seed<100;seed++){const s=setup(seed).s;s.minute=1199;const day=bottleLayout(s);s.minute=1200;const night=bottleLayout(s);assert.deepEqual(night.slice(0,day.length),day);assert.ok(night.length-day.length<=3);if(night.length>day.length)bonuses++;assert.deepEqual(bottleLayout(validateSave(s)),night);}
 assert.ok(bonuses>5&&bonuses<40);
});
test('collection is daily, unique and preserves cumulative quest/recycling statistics',()=>{
 const m=setup();assert.equal(m.collect(0),true);assert.equal(m.collect(0),false);assert.equal(m.collect(63),false);m.recycle();assert.equal(m.s.stats.returned,1);m.advance(1440-m.s.minute);assert.deepEqual(m.s.collected,[]);assert.equal(m.collect(0),true);assert.equal(m.s.stats.collected,2);
});
test('bins complete once, cannot reroll through save/cancel, and reset tomorrow',()=>{
 const seed=winningSeed(),m=setup(seed),expected=binReward(seed,1,0);assert.ok(beginBinSearch(m,0));assert.equal(beginBinSearch(m,0),false);tickBinSearch(m,1);
 const loaded=new GameModel(validateSave(m.s));tickBinSearch(loaded,1.3);assert.equal(loaded.count('bottle'),expected.count);assert.equal(beginBinSearch(loaded,0),false);tickBinSearch(loaded,10);assert.equal(loaded.count('bottle'),expected.count);
 const retry=setup(seed);beginBinSearch(retry,0);tickBinSearch(retry,1);cancelBinSearch(retry);beginBinSearch(retry,0);tickBinSearch(retry,SEARCH_SECONDS);assert.equal(retry.count('bottle'),expected.count);
 loaded.advance(1440-loaded.s.minute);assert.deepEqual(loaded.s.scavenge.searched,[]);assert.ok(beginBinSearch(loaded,0));
});
test('leaving, changing activity or midnight cancels without a reward',()=>{
 for(const change of [s=>s.position.x+=1,s=>s.riding=true,s=>s.inside=true,s=>s.job={type:'cleaning'}]){const m=setup(winningSeed());beginBinSearch(m,0);change(m.s);tickBinSearch(m,3);assert.equal(m.s.scavenge.action,null);assert.equal(m.count('bottle'),0);assert.deepEqual(m.s.scavenge.searched,[]);}
 const m=setup(winningSeed());m.s.minute=1439.99;beginBinSearch(m,0);m.updateTime(3);assert.equal(m.s.scavenge.action,null);assert.equal(m.count('bottle'),0);
});
test('full inventory preserves a saved claim, including across midnight; repeated collection cannot duplicate',()=>{
 const m=setup(winningSeed()),reward=binReward(m.s.scavenge.seed,1,0);m.s.inventory=Array.from({length:16},()=>({id:'medicine',count:3}));beginBinSearch(m,0);const hygiene=m.s.needs.hygiene;tickBinSearch(m,3);assert.equal(m.s.needs.hygiene,hygiene-2);assert.equal(m.s.scavenge.pending.length,1);assert.equal(beginBinSearch(m,0),false);
 const loaded=new GameModel(validateSave(m.s));loaded.advance(1440-loaded.s.minute);loaded.s.inventory=[];assert.ok(beginBinSearch(loaded,0));assert.equal(loaded.count('bottle'),reward.count);assert.equal(loaded.s.scavenge.pending.length,0);assert.ok(beginBinSearch(loaded,0));assert.equal(loaded.count('bottle'),reward.count);
});
test('empty bin has no trash item; packaged food is possible without cash windfalls',()=>{
 const outcomes=new Set();for(let seed=0;seed<100;seed++){const m=setup(seed),r=binReward(seed,1,0);outcomes.add(r?`${r.id}:${r.count}`:'empty');const money=m.s.money;beginBinSearch(m,0);tickBinSearch(m,3);assert.equal(m.s.money,money);if(!r)assert.deepEqual(m.s.inventory,[]);}
 assert.deepEqual([...outcomes].sort(),['bottle:1','bottle:2','empty','roll:1']);
});
test('cheap bakery food purchases and consumes through the real action flow',()=>{
 const m=setup();m.s.money=ITEMS.roll.price;m.s.needs.hunger=20;m.s.needs.health=60;assert.ok(SHOP_POINTS.bakery.items.includes('roll'));assert.ok(m.buy('roll'));assert.equal(m.s.money,0);assert.ok(m.beginLifeAction('consume',0));m.tickLifeAction(10);assert.equal(m.count('roll'),0);assert.ok(m.s.needs.hunger>32&&m.s.needs.hunger<=34);assert.equal(m.s.needs.health,60);
 assert.ok(ITEMS.roll.effects.hunger/ITEMS.roll.price<ITEMS.sandwich.effects.hunger/ITEMS.sandwich.price);
});
test('legacy saves migrate reproducibly without changing earned/collected history; forged state fails',()=>{
 const old=newGame();delete old.scavenge;old.collected=[2,19,44];old.stats.collected=28;const a=validateSave(old),b=validateSave(old);assert.deepEqual(a.scavenge,b.scavenge);assert.deepEqual(a.collected,old.collected);assert.equal(a.stats.collected,28);assert.deepEqual(bottleLayout(a),bottleLayout(b));
 for(const edit of [s=>s.scavenge.seed=-1,s=>s.scavenge.searched=[0,0],s=>s.scavenge.pending=[{bin:0,day:1,id:'medicine',count:3}],s=>s.scavenge.action={bin:99,day:1,elapsed:0,origin:s.position}]){const s=newGame();edit(s);assert.throws(()=>validateSave(s));}
});
test('map never renders bottles and Quick Food includes bakery food after better meals',()=>{
 const app=readFileSync(new URL('../dist/app.js',import.meta.url),'utf8');const map=app.slice(app.indexOf('function drawMap('),app.indexOf('function quickUse('));assert.ok(map.length>10);assert.doesNotMatch(map,/world\.bottles|type\s*===?\s*['"]bottle/);assert.match(app,/\['meal','sandwich','roll'\]/);assert.doesNotMatch(app,/Die grünen Flaschen vor dir/);
});

test('world reuses an 18-mesh pool and resyncs only when spawn state changes',async()=>{
 const {World}=await import('../dist/world.js');const m=setup(33);let moves=0;
 const pool=Array.from({length:18},()=>({mesh:{visible:false,position:{set(){moves++;}}}}));const world={bottles:pool};
 World.prototype.syncScavenge.call(world,m.s);const first=moves,meshes=pool.map(b=>b.mesh);assert.equal(pool.filter(b=>b.mesh.visible).length,bottleLayout(m.s).length);
 World.prototype.syncScavenge.call(world,m.s);assert.equal(moves,first);m.collect(0);World.prototype.syncScavenge.call(world,m.s);assert.equal(pool.find(b=>b.id===0).mesh.visible,false);
 m.s.minute=1200;World.prototype.syncScavenge.call(world,m.s);assert.deepEqual(pool.map(b=>b.mesh),meshes);assert.equal(pool.length,18);
});
