import test from 'node:test';
import assert from 'node:assert/strict';
import {WORLD_BOUNDS,exteriorContains,districtOf,DISTRICT_FIXTURES,ROADS} from '../dist/city-layout.js';
import {EXPANSION_BUILDINGS,EXPANSION_DISTRICTS,EXPANSION_FIXTURES,EXPANSION_PLACES,EXPANSION_STOPS,EXPANSION_LANDMARKS} from '../dist/expansion-layout.js';
import {GameModel,newGame,validateSave} from '../dist/model.js';
import {BUILDINGS,LOCATIONS} from '../dist/data.js';
import {CollisionIndex,CityNavigation,routeLength} from '../dist/navigation.js';
import {segmentClear} from '../dist/movement.js';
import {discoverNearby} from '../dist/exploration.js';
import {markerLabel,mapTransform,mapPlaces,bindCityMapControls} from '../dist/city-map.js';
import {bookTrip,tickTransit} from '../dist/transit.js';
import {stopById} from '../dist/mobility-layout.js';
import {CONTRACTS} from '../dist/contracts.js';
import {deliveryPlan,deliveryTarget} from '../dist/delivery-routes.js';
import {BOTTLE_CANDIDATES,PUBLIC_BINS} from '../dist/scavenge-layout.js';
import {beginBinSearch,tickBinSearch,SEARCH_SECONDS} from '../dist/scavenge.js';
import {streetSurface,onRoad} from '../dist/street-layout.js';
import {groundHeight} from '../dist/spatial.js';
import {streetAt} from '../dist/orientation.js';

const blocks=[...BUILDINGS.map(([x,z,w,d])=>({x,z,w:w/2+.35,d:d/2+.35})),...DISTRICT_FIXTURES,...EXPANSION_FIXTURES],index=new CollisionIndex(blocks);
const canWalk=(x,z,r=.4)=>exteriorContains(x,z,r)&&!index.blocked(x,z,r);
test('expanded footprint preserves core coordinates, old saves, and outdoor positions beyond hidden rooms',()=>{
 const b=WORLD_BOUNDS,area=(b.maxX-b.minX)*(b.maxZ-b.minZ)/(337*238);assert.ok(area>=4&&area<=5);
 assert.equal(LOCATIONS.find(l=>l.id==='market').x,-36);assert.equal(LOCATIONS.find(l=>l.id==='station').x,-175);
 const old=newGame();delete old.discovered;assert.deepEqual(validateSave(old).position,old.position);assert.deepEqual(validateSave(old).discovered,[]);
 for(const p of [{x:430,z:240},{x:-240,z:-240},{x:220,z:0}]){assert.ok(exteriorContains(p.x,p.z,.4));const s=newGame();s.position=p;assert.deepEqual(validateSave(s).position,p);}
 assert.equal(groundHeight(400,0),streetSurface(400,0).height);assert.equal(groundHeight(300,0,'home'),.07);
});
test('all five districts are distinct and building plots are grounded, clear of roads and each other',()=>{
 assert.equal(new Set(EXPANSION_DISTRICTS.map(d=>d.style)).size,5);assert.ok(EXPANSION_BUILDINGS.length>=50);
 for(const [i,b] of EXPANSION_BUILDINGS.entries()){
  assert.ok(exteriorContains(b.x,b.z));assert.ok(groundHeight(b.x,b.z)<=.02);
  for(const r of ROADS)assert.ok(Math.abs(b.x-r.x)>=(b.w+r.w)/2||Math.abs(b.z-r.z)>=(b.d+r.d)/2,`building on road ${b.x},${b.z}`);
  for(const l of EXPANSION_LANDMARKS)assert.ok(Math.abs(b.x-l.x)>=(b.w+l.w)/2||Math.abs(b.z-l.z)>=(b.d+l.d)/2,`building covers landmark ${l.id}`);
  for(const q of EXPANSION_BUILDINGS.slice(i+1))assert.ok(Math.abs(b.x-q.x)>=(b.w+q.w)/2||Math.abs(b.z-q.z)>=(b.d+q.d)/2,`overlap ${b.x},${b.z}`);
 }
 for(const p of EXPANSION_PLACES){assert.ok(streetAt(p).length);assert.ok(EXPANSION_DISTRICTS.some(d=>d.name===districtOf(p)));}
});
test('cached coarse navigation reaches every district from Westbahnhof with collision-safe smoothing',()=>{
 const nav=new CityNavigation(canWalk,6,WORLD_BOUNDS);assert.ok(nav.nodes.length<11000);
 for(const p of EXPANSION_PLACES){const route=nav.find({x:-175,z:-31},deliveryTarget(p.id));assert.ok(route.length>1,p.id);for(let i=1;i<route.length;i++)assert.ok(segmentClear(route[i-1],route[i],canWalk,.45));}
 assert.ok(routeLength(nav.find({x:-175,z:-31},deliveryTarget('deliveryHoehen')))>500);
 // The index is conservative across bucket borders, including van-sized queries.
 for(let x=-230;x<430;x+=11)for(let z=-240;z<250;z+=13)for(const r of [.4,1.3,4.5])assert.equal(index.blocked(x,z,r),blocks.some(b=>Math.abs(x-b.x)<b.w+r&&Math.abs(z-b.z)<b.d+r));
});
test('new main streets carry a car between core and all district stops without old bounds',()=>{
 for(const p of EXPANSION_STOPS){assert.ok(onRoad(p.vehicle.x,p.vehicle.z),p.id);assert.ok(canWalk(p.arrival.x,p.arrival.z,.5),p.id);}
 for(const [a,b] of [[{x:110,z:0},{x:400,z:0}],[{x:0,z:-65},{x:0,z:-230}],[{x:0,z:65},{x:0,z:230}]])for(let t=0;t<=1;t+=.005){const p={x:a.x+(b.x-a.x)*t,z:a.z+(b.z-a.z)*t};assert.ok(canWalk(p.x,p.z,1.4));assert.ok(onRoad(p.x,p.z));}
});
test('map fits actual bounds, markers pass Z, local minimap keeps its viewing range',()=>{
 const t=mapTransform(1000,720,{x:0,z:0},true);for(const x of [WORLD_BOUNDS.minX,WORLD_BOUNDS.maxX])for(const z of [WORLD_BOUNDS.minZ,WORLD_BOUNDS.maxZ]){const [px,pz]=t.point(x,z);assert.ok(px>=20&&px<=980&&pz>=20&&pz<=700);}
 assert.deepEqual([25,26,27,51,52].map(markerLabel),['Z','AA','AB','AZ','BA']);assert.equal(mapTransform(200,200,{x:400,z:200}).scale,200/135);
 const s=newGame();assert.equal(mapPlaces(s).length,LOCATIONS.length);assert.ok(mapPlaces(s).some(p=>p.id==='deliveryTech'));
});
test('landmarks discover once, persist, and do not hide required destinations',()=>{
 const m=new GameModel();m.s.position={...EXPANSION_LANDMARKS[0]};discoverNearby(m);const events=m.events.length;discoverNearby(m);assert.equal(m.events.length,events);assert.deepEqual(validateSave(m.s).discovered,m.s.discovered);assert.ok(mapPlaces(m.s).some(p=>p.id==='nordCourt'));
});
test('extended bus tickets finish normally and preserve a reload in progress',()=>{
 for(const p of EXPANSION_STOPS){const m=new GameModel(newGame(true));m.s.position={x:stopById('busCity').x,z:stopById('busCity').z};assert.ok(bookTrip(m,'busCity',p.id));const restored=new GameModel(validateSave(m.s));tickTransit(restored,9);assert.equal(restored.s.transit,null);assert.deepEqual(restored.s.position,p.arrival);}
});
test('longer contracts use expanded destinations, pay more, and stay gated for beginners',()=>{
 const s=newGame();for(const c of CONTRACTS.filter(c=>c.route.some(id=>id.startsWith('delivery')&&EXPANSION_PLACES.some(p=>p.id===id)))){assert.ok(c.completed>=2);assert.ok(c.base>6000);for(const mode of ['foot','bike','vehicle'])assert.ok(deliveryPlan(c,s,mode)?.distance>200);}
});
test('outer districts have searchable bins and curated bottle candidates',()=>{
 for(const p of EXPANSION_STOPS)assert.ok(BOTTLE_CANDIDATES.some(b=>Math.hypot(p.x-b.x,p.z-b.z)<15));
 for(const b of PUBLIC_BINS.filter(b=>b.id>=100)){assert.ok(canWalk(b.x,b.z));const m=new GameModel();m.s.position={x:b.x,z:b.z};assert.ok(beginBinSearch(m,b.id));tickBinSearch(m,SEARCH_SECONDS);assert.ok(m.s.scavenge.searched.includes(b.id));assert.ok(validateSave(m.s));}
});


test('eastern outdoor drops and legacy room drops remain separate after reload',()=>{
 const m=new GameModel(newGame(true));m.s.position={x:300,z:4};m.add('bread');m.drop(0);assert.equal(m.s.drops[0].interior,null);
 m.s.home='room';m.s.inside=true;m.s.interior='home';m.add('cheese');m.drop(0);assert.equal(m.s.drops[1].interior,'home');
 const saved=structuredClone(m.s);delete saved.drops[1].interior;const loaded=validateSave(saved);assert.equal(loaded.drops[0].interior,null);assert.equal(loaded.drops[1].interior,'home');
});

test('map controls zoom around the cursor, pan, center and fit without changing player state',()=>{
 const original=globalThis.document,buttons={'#mapFit':{},'#mapCenter':{}},player={x:355,z:210};let draws=0;
 globalThis.document={querySelector:id=>buttons[id]};try{
 const c={width:1000,height:720,getBoundingClientRect:()=>({left:0,top:0,width:1000,height:720}),setPointerCapture(){}};
 bindCityMapControls(c,()=>draws++,player);c.onwheel({preventDefault(){},clientX:500,clientY:360,deltaY:-400});assert.ok(c.mapView.zoom>1);
 const before={...c.mapView};c.onpointerdown({clientX:500,clientY:360,pointerId:1});c.onpointermove({clientX:550,clientY:400});c.onpointerup();assert.ok(c.mapView.x<before.x&&c.mapView.z<before.z);
 buttons['#mapCenter'].onclick();assert.deepEqual(c.mapView,{zoom:3,...player});buttons['#mapFit'].onclick();assert.deepEqual(c.mapView,{zoom:1});assert.equal(draws,4);assert.deepEqual(player,{x:355,z:210});
 }finally{globalThis.document=original;}
});
