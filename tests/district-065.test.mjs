import test from 'node:test';
import assert from 'node:assert/strict';
import * as THREE from '../dist/vendor/three.module.js';
import {WORLD_BOUNDS,ROADS,STATION_PLAZAS,VIADUCT,DISTRICT_FIXTURES,exteriorContains,districtOf} from '../dist/city-layout.js';
import {GameModel,newGame,validateSave} from '../dist/model.js';
import {homeLocation,outsidePosition,housingPanel,movePanel} from '../dist/housing.js';
import {LOCATIONS,HOMES,BUILDINGS} from '../dist/data.js';
import {CityNavigation,routeLength} from '../dist/navigation.js';
import {segmentClear} from '../dist/movement.js';
import {groundHeight,CameraRig,homeFixtures} from '../dist/spatial.js';
import {buildStationDistrict} from '../dist/station-district.js';
import {deliveryLeg,deliveryTarget,DELIVERY_PEOPLE,deliveryPlan} from '../dist/delivery-routes.js';
import {CONTRACTS} from '../dist/contracts.js';
import {updateCrowd} from '../dist/crowd.js';
import {STREET_LAMPS,selectLamps} from '../dist/lighting.js';
import {surfaceAt} from '../dist/soundscape.js';
const at=(m,id)=>{const p=LOCATIONS.find(l=>l.id===id);m.s.position={x:p.x,z:p.z};};
const blocks=[...BUILDINGS.map(([x,z,w,d,h])=>({x,z,w:w/2+.35,d:d/2+.35,h})),...DISTRICT_FIXTURES];
const free=(x,z,r=.35)=>exteriorContains(x,z,r)&&!blocks.some(c=>Math.abs(x-c.x)<c.w+r&&Math.abs(z-c.z)<c.d+r);

test('connected western footprint supports three road links without admitting blank outer corners or interiors',()=>{
 for(const z of [-65,0,65])for(let x=-217;x<115;x+=.5){assert.ok(exteriorContains(x,z));assert.ok(free(x,z,.35));}
 for(const p of [[-223,0],[-190,90],[-190,-90],[0,121],[300,0]])assert.equal(exteriorContains(...p),false);
 assert.equal(districtOf({x:-179,z:17}),'BAHNHOFSVIERTEL');assert.equal(districtOf({x:-34,z:12}),'LINDENHÖFE');
 const nav=new CityNavigation(free,4,WORLD_BOUNDS);
 for(const id of ['station','stationHome','deliveryKiosk','deliveryWorkshop']){
  const target=deliveryTarget(id),route=nav.find({x:26,z:-10},target);assert.ok(route.length>1,id);
  for(let i=1;i<route.length;i++)assert.ok(segmentClear(route[i-1],route[i],free,.4),id);
 }
});

test('new and old saves preserve western positions, parked vehicles, dropped items and household contents',()=>{
 const m=new GameModel(newGame(true));at(m,'stationHome');assert.equal(m.moveHome('stationRoom'),true);
 m.s.storage=[{id:'bread',count:2}];m.s.fridge=[{id:'water',count:2}];m.s.vehicle={id:'bike',x:-199,z:17,angle:0,speed:0,fuel:100,condition:95};
 m.add('cheese');m.drop(0);const loaded=validateSave(JSON.parse(JSON.stringify(m.s)));assert.deepEqual(loaded,m.s);
 assert.equal(m.enterInterior('home'),true);assert.deepEqual(outsidePosition(m.s),{x:-179,z:15});
 assert.equal(validateSave(m.s).home,'stationRoom');assert.equal(m.leaveInterior(),true);assert.deepEqual(m.s.position,{x:-179,z:15});
 assert.deepEqual(m.s.storage,[{id:'bread',count:2}]);assert.deepEqual(m.s.fridge,[{id:'water',count:2}]);
 const bad=structuredClone(m.s);bad.position={x:-190,z:105};assert.throws(()=>validateSave(bad),/Außenposition/);
 const vehicleBad=structuredClone(m.s);vehicleBad.vehicle.z=100;assert.throws(()=>validateSave(vehicleBad),/Fahrzeug/);
 const old=newGame(true);old.home='flat';old.rentDue=4;old.inside=true;old.interior='home';old.position={x:300,z:4};
 const prior=new GameModel(validateSave(old));assert.equal(homeLocation(prior.s).id,'home');prior.leaveInterior();assert.deepEqual(prior.s.position,{x:-34,z:10});
});

test('moving is a paid on-site choice, can downsize, never refunds deposits or resets possessions',()=>{
 const m=new GameModel(newGame(true));assert.equal(m.moveHome('stationRoom'),false);at(m,'home');assert.equal(m.moveHome('flat'),true);
 m.s.storage=[{id:'medicine',count:1}];m.s.fridge=[{id:'meal',count:2}];const before=m.s.money;
 assert.equal(m.moveHome('stationRoom'),false);at(m,'stationHome');assert.equal(m.enterInterior('home'),false);assert.equal(m.moveHome('stationRoom'),true);
 assert.equal(before-m.s.money,8500);assert.equal(m.s.rentDue,m.s.day+3);const once=m.s.money;assert.equal(m.moveHome('stationRoom'),false);assert.equal(m.s.money,once);
 assert.equal(homeLocation(m.s).id,'stationHome');assert.equal(m.s.storage[0].id,'medicine');assert.equal(m.s.fridge[0].count,2);
 assert.equal(homeFixtures('stationRoom').some(c=>c.id==='desk'),false);assert.equal(homeFixtures('flat').some(c=>c.id==='desk'),true);
 m.s.money=0;at(m,'home');assert.equal(m.moveHome('room'),false);assert.equal(m.s.home,'stationRoom');
 const button=(text,a,id)=>`<button data-action="${a}" data-arg="${id}">${text}</button>`;
 assert.match(housingPanel(m.s,'stationHome',button),/Fußweg zum Café/);assert.doesNotMatch(housingPanel(m.s,'stationHome',button),/undefined|NaN/);
 assert.match(movePanel(m.s,'flat',button),/Keine Rückerstattung/);
 assert.ok(deliveryLeg('stationHome','cafe').distance>deliveryLeg('home','cafe').distance+100);
 assert.ok(deliveryLeg('stationHome','market').distance>deliveryLeg('home','market').distance+100);
});

test('forecourt, western pavements and road feet use rendered heights and matching footsteps',()=>{
 for(const [x,z,y] of [[-180,-28,.04],[-174,43,.04],[-202,30,.14],[-173,0,.14],[-173,9,.3],[-212,30,.3]])assert.ok(Math.abs(groundHeight(x,z)-y)<1e-8,`${x},${z}`);
 assert.equal(surfaceAt(-180,-28),'paving');assert.equal(surfaceAt(-202,30),'asphalt');
 assert.equal(groundHeight(300,0,'home'),.07);
});

test('bridge deck limits upward camera travel while street-level views remain unobstructed',()=>{
 const deck={x:VIADUCT.x,z:0,w:VIADUCT.w/2,d:VIADUCT.d/2,minY:VIADUCT.bottom,h:VIADUCT.top};
 const flat=new CameraRig().update({x:-145,y:0,z:0},Math.PI/2,0,8,1/60,[deck]);assert.ok(flat.distance>7.8);
 const up=new CameraRig().update({x:-145,y:0,z:0},0,1.2,12,1/60,[deck]);assert.ok(up.position.y<VIADUCT.bottom);assert.ok(up.distance>3);
 assert.ok(free(-145,0));assert.equal(free(-149,-32),false);
});

test('western residents keep their own walking range and turn naturally without teleporting east',()=>{
 const npc={x:-121.01,z:9,lane:9,yaw:Math.PI/2,dir:1,speed:1.2,minX:-189,maxX:-121};let walked=0,last=npc.x;
 for(let i=0;i<1200;i++){updateCrowd([npc],1/60,[],free);walked+=npc.distance;assert.ok(Math.abs(npc.x-last)<.03);assert.ok(npc.x>=-190&&npc.x<-119);last=npc.x;}
 assert.ok(walked>10);assert.equal(npc.dir,-1);
});

test('new district keeps the eight-light budget and mixed tours retain achievable mode-specific deadlines',()=>{
 const selected=selectLamps(STREET_LAMPS,{x:-179,z:-13},[],8);assert.equal(selected.length,8);assert.ok(selected.every(l=>l.x<-120));
 for(const id of ['stationKiosk','workshopExpress','westLoop'])for(const mode of ['foot','bike','vehicle']){
  const c=CONTRACTS.find(c=>c.id===id),s=newGame(true);s.settings.speed=10;const plan=deliveryPlan(c,s,mode);
  assert.ok(plan.distance>180);assert.ok(plan.realSeconds>plan.travelSeconds+plan.serviceSeconds);assert.equal(plan.deadlineMinutes,c.minutes?plan.realSeconds*10:0);
 }
 assert.equal(Object.keys(DELIVERY_PEOPLE).length,6);
});

test('station geometry has finite surfaces, a separate animated clock, solid supports and covered camera volumes',()=>{
 const original=globalThis.document;globalThis.document={createElement:()=>({getContext:()=>({fillRect(){}})})};
 try{
  const mat=c=>new THREE.MeshStandardMaterial({color:c}),geo=new THREE.BoxGeometry(),kit={};
  kit.box=(p,x,y,z,w,h,d,c,m)=>{const mesh=new THREE.Mesh(geo,m||mat(c));mesh.position.set(x,y,z);mesh.scale.set(w,h,d);p.add(mesh);return mesh;};
  kit.cylinder=(p,x,y,z,r,h,c)=>kit.box(p,x,y,z,r,h,r,c);kit.sphere=(p,x,y,z,r,c)=>kit.box(p,x,y,z,r,r,r,c);
  kit.sign=(p,t,x,y,z)=>{const g=new THREE.Group();g.name=t;g.position.set(x,y,z);p.add(g);return g;};
  const world={scene:new THREE.Scene(),staticGroups:[],colliders:[],tree(){},atmosphere:{addPuddle(){}}};buildStationDistrict(world,kit);
  assert.deepEqual(world.colliders,DISTRICT_FIXTURES);assert.equal(world.overheadColliders.length,2);assert.ok(world.stationClock.minute.parent===world.stationClock.root);
  assert.ok(!world.staticGroups.includes(world.stationClock.root));assert.ok(world.staticGroups.length>0);
  world.scene.updateMatrixWorld(true);world.scene.traverse(o=>{assert.ok(o.matrixWorld.elements.every(Number.isFinite));if(o.isMesh)assert.ok(new THREE.Box3().setFromObject(o).isEmpty()===false);});
  const labels=[];world.scene.traverse(o=>o.name&&labels.push(o.name));assert.ok(labels.includes('BAHNHOFSVIERTEL'));assert.ok(labels.includes('GLEISHÖFE · ZIMMER'));
 }finally{globalThis.document=original;}
});
