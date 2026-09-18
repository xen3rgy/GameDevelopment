import test from 'node:test';
import assert from 'node:assert/strict';
import * as THREE from '../dist/vendor/three.module.js';
import {CityCharacter,buildHorizonLinks} from '../dist/city-character-scene.js';
import {CITY_CHARACTER_FIXTURES,CITY_CHARACTER_OVERHEAD,HORIZON_ROADS} from '../dist/city-character-layout.js';
import {BACKDROP_BUILDINGS,BACKDROP_GROUND,modernBuilding} from '../dist/district-architecture.js';
import {districtLampStyle} from '../dist/district-materials.js';
import {groundHeight,CameraRig} from '../dist/spatial.js';
import {BUILDINGS,LOCATIONS} from '../dist/data.js';
import {onRoad,ROAD_HEIGHT,streetSurface} from '../dist/street-layout.js';
import {streetSpawn} from '../dist/pedestrian-layout.js';

function kit(){
 const materials=new Map(),geometry=new THREE.BoxGeometry();
 const k={mat:c=>{if(!materials.has(c))materials.set(c,new THREE.MeshStandardMaterial({color:c}));return materials.get(c);}};
 k.box=(p,x,y,z,w,h,d,c,m)=>{const v=new THREE.Mesh(geometry,m||k.mat(c));v.position.set(x,y,z);v.scale.set(w,h,d);p.add(v);return v;};
 k.cylinder=(p,x,y,z,r,h,c)=>{const v=new THREE.Mesh(new THREE.CylinderGeometry(1,1,1,8),k.mat(c));v.position.set(x,y,z);v.scale.set(r,h,r);p.add(v);return v;};
 k.sphere=(p,x,y,z,r,c)=>{const v=new THREE.Mesh(new THREE.IcosahedronGeometry(1,1),k.mat(c));v.position.set(x,y,z);v.scale.setScalar(r);p.add(v);return v;};
 k.sign=(p,text,x,y,z,w,h,c,b,angle=0)=>{const v=k.box(p,x,y,z,w,h,.001,0xffffff);v.name=text;v.rotation.y=angle;return v;};return k;
}
function scene(){const lights=[];return {scene:new THREE.Scene(),staticGroups:[],colliders:[],overheadColliders:[],atmosphere:{registerLamp:(x,z,m,options)=>lights.push({x,z,m,...options})},lights};}
const near=(a,b)=>assert.ok(Math.abs(a-b)<1e-5,`${a} != ${b}`);

test('district furniture is grounded, fits its blockers and preserves streets and entrances',()=>{
 const w=scene(),c=new CityCharacter(w,kit());c.root.updateMatrixWorld(true);
 assert.equal(w.colliders.length,CITY_CHARACTER_FIXTURES.length);
 for(const f of CITY_CHARACTER_FIXTURES){
  const root=c.root.getObjectByName(f.id),b=new THREE.Box3().setFromObject(root),h=groundHeight(f.x,f.z);
  near(root.position.y,h);assert.ok(b.min.y>=h-.011&&b.min.y<=h+.025,`ground ${f.id}`);
  assert.ok(b.min.x>=f.x-f.w-.012&&b.max.x<=f.x+f.w+.012,`width ${f.id}`);
  assert.ok(b.min.z>=f.z-f.d-.012&&b.max.z<=f.z+f.d+.012,`depth ${f.id}`);
  assert.ok(b.max.y<=h+f.h+.01,`height ${f.id}`);
  for(const x of [f.x-f.w,f.x,f.x+f.w])for(const z of [f.z-f.d,f.z,f.z+f.d])assert.equal(onRoad(x,z),false,`road ${f.id}`);
  for(const [x,z,bw,bd] of BUILDINGS)assert.ok(Math.abs(f.x-x)>=f.w+bw/2||Math.abs(f.z-z)>=f.d+bd/2,`inside building ${f.id}`);
  for(const l of LOCATIONS)assert.ok(Math.abs(f.x-l.x)>f.w+1||Math.abs(f.z-l.z)>f.d+1,`entrance ${l.id}`);
 }
 for(const o of c.root.children)if(o.isMesh){assert.ok([...o.position.toArray(),...o.scale.toArray()].every(Number.isFinite));}
 const free=(x,z)=>!w.colliders.some(f=>Math.abs(x-f.x)<f.w+.35&&Math.abs(z-f.z)<f.d+.35);
 for(const f of CITY_CHARACTER_FIXTURES){const p=streetSpawn({x:f.x,z:f.z},free);assert.ok(free(p.x,p.z),`save recovery ${f.id}`);}
 assert.equal(free(55.2,21.5),true);assert.equal(free(55.2,36),true);
 const up=new CameraRig().update({x:55.2,y:0,z:21.5},0,1.2,8,1/60,CITY_CHARACTER_OVERHEAD);assert.ok(up.position.y<2.75);
});

test('horizon streets have support soil, aligned road heights and no buildings in their corridors',()=>{
 const w=scene(),g=buildHorizonLinks(w,kit());g.updateMatrixWorld(true);
 for(const r of HORIZON_ROADS){
  assert.ok(r.x-r.w/2>=BACKDROP_GROUND.minX&&r.x+r.w/2<=BACKDROP_GROUND.maxX);
  assert.ok(r.z-r.d/2>=BACKDROP_GROUND.minZ&&r.z+r.d/2<=BACKDROP_GROUND.maxZ);
  for(const b of BACKDROP_BUILDINGS)assert.ok(Math.abs(r.x-b.x)>=r.w/2+b.w/2+3.2||Math.abs(r.z-b.z)>=r.d/2+b.d/2+3.2,`road intersects scenery ${b.x}/${b.z}`);
  const road=g.children.find(o=>o.position.x===r.x&&o.position.z===r.z&&o.scale.x===r.w&&o.scale.z===r.d);
  near(road.position.y+road.scale.y/2,ROAD_HEIGHT);
 }
 assert.ok(new THREE.Box3().setFromObject(g).max.x<200);
});

test('street continuations have no raised pavement cap at the old road endpoints',()=>{
 for(const z of [-65,0,65])for(const x of [-224,-218,118,123,124.9])assert.equal(streetSurface(x,z)?.height,ROAD_HEIGHT);
 for(const x of [-110,0,110])for(const z of [-124.9,-119,119,124.9])assert.equal(streetSurface(x,z)?.height,ROAD_HEIGHT);
 for(const z of [-79.9,-75,75,79.9])assert.equal(streetSurface(-202,z)?.height,ROAD_HEIGHT);
});

test('sheltering residents remain grounded on their crate seats and pause inside',()=>{
 const w=scene(),c=new CityCharacter(w,kit());c.initResidents();assert.equal(c.residents.length,2);
 for(const m of c.residents){near((m.userData.upper.position.y-.235)*m.scale.y,.46);assert.ok(m.userData.legs.every(l=>Number.isFinite(l.rotation.x)));}
 c.update({inside:true},0,{x:0,z:0});assert.equal(c.residentRoot.visible,false);assert.equal(c.time,0);
 c.update({inside:false},1,{x:-145,z:38});assert.equal(c.residentRoot.visible,true);assert.equal(c.time,1);
 assert.equal(w.lights.length,5);assert.ok(w.lights.every(l=>l.height>=2.75&&l.power<=120&&l.distance<=14));
});

test('downtown display lighting and warm station lamps retain distinct controlled output',()=>{
 const old=districtLampStyle(-175),modern=districtLampStyle(70);assert.ok(old.color.b<modern.color.b);assert.ok(old.power>=240&&modern.power<=280);
 const w=scene(),k=kit(),art={w,k,windows:Array.from({length:5},()=>k.mat(0x677977))};
 const g=modernBuilding(art,65,-26,32,25,18,0x999999,'CAFÉ MORGEN',1);g.updateMatrixWorld(true);
 assert.equal(w.lights.length,2);assert.ok(w.lights.every(l=>l.power<100&&l.height>3));
 assert.ok(g.getObjectByName('RÖSTUNG DES MONATS'));
 assert.equal(w.colliders.length,1);
});
