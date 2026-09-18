import {WORLD_BOUNDS,DISTRICT_FIXTURES,exteriorContains} from '../dist/city-layout.js';
import test from 'node:test';
import assert from 'node:assert/strict';
import {GameModel,newGame,validateSave} from '../dist/model.js';
import {LOCATIONS,BUILDINGS} from '../dist/data.js';
import {CONTRACTS,contractById,canAcceptContract,deadlineView,newCourier} from '../dist/contracts.js';
import {CityNavigation} from '../dist/navigation.js';
import {segmentClear} from '../dist/movement.js';
import {addressOf,routeGuidance} from '../dist/orientation.js';
import {entranceForBuilding,STREET_POSTS,decorateAddress,addStreetSigns} from '../dist/city-addresses.js';
import {contractCards,itinerary,courierReceipt} from '../dist/courier-ui.js';
import * as THREE from '../dist/vendor/three.module.js';
function at(m,id){const l=LOCATIONS.find(l=>l.id===id);m.s.position={x:l.x,z:l.z};}
function adapt(m){const accept=m.acceptContract.bind(m),work=m.work.bind(m);m.acceptContract=(...args)=>{const ok=accept(...args);if(ok)m.tickCourier(2.2);return ok;};m.work=(...args)=>{const ok=work(...args);if(ok&&m.s.job?.interaction)m.tickCourier(2.6);return ok;};return m;}
function worker(){const m=adapt(new GameModel());at(m,'jobs');return m;}
function experienced(){const m=worker();m.s.courier={...newCourier(),completed:4,onTime:4,reliability:80};return m;}
function finish(m){while(m.s.job){at(m,m.s.job.target);assert.equal(m.work('deliver'),true);}}

test('multi-stop delivery hands over individual parcels, advances targets, pays exactly once',()=>{
 const m=worker();assert.ok(m.acceptContract('canal'));assert.equal(m.count('parcel'),2);assert.equal(m.weight,4);
 assert.equal(m.work('deliver'),false);at(m,'deliveryA');assert.equal(m.work('deliver'),false);assert.equal(m.count('parcel'),2);
 at(m,'deliveryB');assert.ok(m.work('deliver'));assert.equal(m.s.money,0);assert.equal(m.s.job.target,'deliveryA');assert.equal(m.count('parcel'),1);
 assert.equal(m.work('deliver'),false);assert.equal(m.finishJob(),false);
 at(m,'deliveryA');assert.ok(m.work('deliver'));assert.equal(m.count('parcel'),0);assert.equal(m.s.money,5800);assert.equal(m.s.stats.jobs,1);
 assert.equal(m.s.courier.completed,1);assert.equal(m.s.courier.reliability,65);assert.equal(m.s.courier.last.stops,2);
 assert.equal(m.work('deliver'),false);assert.equal(m.finishJob(),false);assert.equal(m.s.money,5800);
});
test('contract pickup checks place, transport, energy and atomic capacity',()=>{
 const m=new GameModel();assert.equal(m.acceptContract('canal'),false);at(m,'jobs');m.s.riding=true;assert.equal(m.acceptContract('canal'),false);m.s.riding=false;
 m.s.inside=true;assert.equal(m.acceptContract('canal'),false);m.s.inside=false;m.s.needs.energy=0;assert.equal(m.acceptContract('canal'),false);m.s.needs.energy=80;
 for(let i=0;i<15;i++)m.s.inventory.push({id:'sandwich',count:1});const before=structuredClone(m.s.inventory);
 assert.equal(m.acceptContract('canal'),false);assert.deepEqual(m.s.inventory,before);assert.equal(m.s.job,null);
 m.s.inventory=[];assert.ok(m.add('bottle',170));assert.equal(m.acceptContract('canal'),false);assert.equal(m.count('parcel'),0);
 m.s.inventory=[];assert.ok(m.acceptContract('canal'));assert.equal(m.acceptContract('chapter'),false);
});
test('remaining packages survive a save; old saves and classic jobs migrate without fabricated history',()=>{
 let m=worker();m.acceptContract('canal');at(m,'deliveryB');m.work('deliver');
 m=adapt(new GameModel(validateSave(JSON.parse(JSON.stringify(m.s)))));assert.equal(m.s.job.target,'deliveryA');assert.equal(m.count('parcel'),1);finish(m);assert.equal(m.s.money,5800);
 let old=newGame();delete old.courier;old.stats.jobs=37;let loaded=validateSave(old);assert.deepEqual(loaded.courier,newCourier());assert.equal(loaded.stats.jobs,37);
 m=new GameModel(old);m.startJob('courier');delete m.s.courier;const legacy=validateSave(m.s);m=new GameModel(legacy);finish(m);assert.equal(m.s.money,3200);assert.equal(m.s.courier.completed,1);
});
test('cancelled tour removes all remaining parcels without pay or completion; repeat cancellation is inert',()=>{
 const m=worker();m.acceptContract('canal');const inventory=m.s.inventory;assert.ok(m.add('water'));assert.ok(m.cancelJob());assert.equal(m.count('parcel'),0);assert.equal(m.count('water'),1);assert.equal(m.s.money,0);assert.equal(m.s.courier.completed,0);assert.equal(m.s.courier.reliability,54);
 assert.equal(m.cancelJob(),false);assert.equal(m.s.courier.cancelled,1);assert.equal(m.s.courier.last.total,0);assert.doesNotThrow(()=>validateSave(m.s));
});
test('reliability unlocks larger tours; training pay is quoted at pickup',()=>{
 const m=worker();assert.equal(m.acceptContract('city'),false);assert.equal(m.acceptContract('express'),false);
 for(let i=0;i<2;i++){at(m,'jobs');m.s.needs.energy=80;assert.ok(m.acceptContract('chapter'));finish(m);}
 assert.equal(canAcceptContract(contractById('city'),m.s.courier),true);assert.equal(canAcceptContract(contractById('express'),m.s.courier),false);
 at(m,'jobs');m.s.skills.logistics=100;assert.ok(m.acceptContract('city'));assert.equal(m.s.job.quotedBase,9350);m.s.skills.logistics=200;finish(m);assert.equal(m.s.courier.last.base,9350);
 m.s.courier.reliability=99;at(m,'jobs');m.acceptContract('chapter');finish(m);assert.equal(m.s.courier.reliability,100);assert.equal(m.s.courier.last.reliabilityDelta,1);
});
test('express deadline spans midnight, preserves base pay when late and itemizes van bonus',()=>{
 const m=experienced();m.s.minute=1380;m.acceptContract('express');let d=deadlineView(m.s);assert.equal(d.remaining,m.s.job.plan.deadlineMinutes);assert.match(d.label,/Tag 2/);const end=m.s.job.started+m.s.job.deadlineMinutes;
 m.s.day=Math.floor(end/1440);m.s.minute=end%1440;assert.equal(deadlineView(m.s).late,false);m.s.job.vehicleUsed=true;m.s.job.vanDistance=m.s.job.plan.distance;finish(m);
 assert.equal(m.s.courier.last.total,10750);assert.equal(m.s.courier.last.timeBonus,2000);assert.equal(m.s.courier.last.vehicleBonus,1750);
 at(m,'jobs');m.s.needs.energy=80;m.acceptContract('express');m.s.minute+=331;assert.equal(deadlineView(m.s).late,true);finish(m);
 assert.equal(m.s.courier.last.total,7000);assert.equal(m.s.courier.last.status,'late');assert.equal(m.s.courier.last.reliabilityDelta,-8);assert.doesNotThrow(()=>validateSave(m.s));
});
test('untimed routes remain payable after sleeping; parcel progress cannot be imported inconsistently',()=>{
 const m=worker();m.acceptContract('canal');m.advance(500,true);assert.equal(deadlineView(m.s),null);
 for(const change of [s=>s.job.target='deliveryA',s=>s.job.contract='unknown',s=>s.job.progress=2,s=>s.inventory.pop(),s=>s.job.started+=100000,s=>s.job.quotedBase=-1,s=>s.courier.reliability=101,s=>s.courier.completed=4]){const bad=structuredClone(m.s);change(bad);assert.throws(()=>validateSave(bad));}
 finish(m);assert.equal(m.s.courier.last.base,5800);assert.equal(m.s.courier.last.status,'completed');
});
test('navigation uses the walkable path, distinguishes vehicles, stale routes and arrivals',()=>{
 const start={x:0,z:0},target={x:10,z:10},route=[start,{x:0,z:10},target];
 const g=routeGuidance(start,target,route);assert.equal(g.distance,20);assert.match(g.instruction,/Süden/);
 assert.match(routeGuidance(target,target,route).instruction,/E zum/);assert.match(routeGuidance(target,target,route,true).instruction,/aussteigen/);
 assert.match(routeGuidance(start,target,route,true).instruction,/Straßen/);assert.match(routeGuidance(start,{x:20,z:20},route).instruction,/gesucht/);
 assert.equal(routeGuidance(null,null,[]),null);
});
test('every named facade maps to one real address; all delivery legs avoid buildings and new sign posts',()=>{
 for(const [x,z,w,d,h,color,name,face] of BUILDINGS.filter(b=>b[6])){const l=entranceForBuilding(x,z,d,face);assert.ok(l,name);assert.ok(addressOf(l.id),name);}
 const colliders=[...BUILDINGS.map(([x,z,w,d])=>({x,z,w:w/2+.35,d:d/2+.35})),...STREET_POSTS.map(p=>({...p,w:.06,d:.06})),...DISTRICT_FIXTURES];
 const free=(x,z,r=.35)=>exteriorContains(x,z,r)&&!colliders.some(c=>Math.abs(x-c.x)<c.w+r&&Math.abs(z-c.z)<c.d+r);const nav=new CityNavigation(free,4,WORLD_BOUNDS);
 for(const contract of CONTRACTS){let from=LOCATIONS.find(l=>l.id==='jobs');for(const id of contract.route){const to=LOCATIONS.find(l=>l.id===id),route=nav.find(from,to);assert.ok(route.length>1,contract.id+': '+id);for(let i=1;i<route.length;i++)assert.ok(segmentClear(route[i-1],route[i],free,.4),id);from=to;}}
});
test('address geometry places functional signs at each entrance without new ground surfaces',()=>{
 const labels=[],kit={box:(p,x,y,z,w,h,d)=>{const m=new THREE.Mesh(new THREE.BoxGeometry(w,h,d));m.position.set(x,y,z);p.add(m);assert.ok(!(Math.abs(y-.3)<.06&&h<.1&&w>1&&d>1),'no additional floor plane');return m;},cylinder:(p,x,y,z,r,h)=>{const m=new THREE.Mesh(new THREE.CylinderGeometry(r,r,h));m.position.set(x,y,z);p.add(m);return m;},sign:(p,text,x,y,z)=>{labels.push(text);const g=new THREE.Group();g.position.set(x,y,z);p.add(g);return g;}};
 for(const id of ['jobs','deliveryA','deliveryB','deliveryC']){const g=new THREE.Group();decorateAddress(g,kit,LOCATIONS.find(l=>l.id===id),0);assert.ok(labels.includes(addressOf(id)));}
 const world={scene:new THREE.Scene(),staticGroups:[],colliders:[]};addStreetSigns(world,kit);assert.equal(world.colliders.length,6);assert.equal(world.staticGroups.length,6);assert.ok(labels.includes('Bahnhofstraße'));
});
test('contract paperwork shows capacity reasons, current stop, remaining time and earned amount',()=>{
 const button=(label,action,id,cls,disabled)=>`<button data-action="${action}" ${disabled?'disabled':''}>${label}</button>`;
 const m=worker();for(let i=0;i<16;i++)m.s.inventory.push({id:'sandwich',count:1});assert.match(contractCards(m.s,button),/Benötigt 2 freie Plätze/);
 m.s.inventory=[];m.acceptContract('canal');assert.match(itinerary(m.s,button),/Am Kanal 3/);at(m,'deliveryB');m.work('deliver');assert.match(itinerary(m.s,button),/1 \/ 2 übergeben/);finish(m);assert.match(courierReceipt(m.s),/58,00/);
 const express=experienced();express.acceptContract('express');assert.match(itinerary(express.s,button),/Spielmin/);
});
