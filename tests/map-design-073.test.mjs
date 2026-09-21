import test from 'node:test';
import assert from 'node:assert/strict';
import * as THREE from '../dist/vendor/three.module.js';
import {STREET_TREES,STREET_PLANTERS,facadeSpans} from '../dist/public-realm-layout.js';
import {STREET_LAMPS} from '../dist/lighting.js';
import {BUILDINGS,LOCATIONS} from '../dist/data.js';
import {STREET_SEATS,PROMENADE_FIXTURES} from '../dist/pedestrian-layout.js';
import {streetSurface} from '../dist/street-layout.js';
import {frontageSurface,frontagePatches} from '../dist/frontage-layout.js';
import {buildFrontages} from '../dist/frontage-scene.js';
import {buildStreets} from '../dist/street-scene.js';
import {groundHeight} from '../dist/spatial.js';
import {sign,labelSize} from '../dist/signage.js';
import {fitFacadeUV} from '../dist/district-materials.js';
import {addStreetSigns,STREET_POSTS,decorateAddress} from '../dist/city-addresses.js';

const near=(a,b)=>assert.ok(Math.abs(a-b)<1e-5,`${a} != ${b}`);
const canvas=fn=>{const prior=globalThis.document;globalThis.document={createElement:()=>({width:0,height:0,getContext:()=>({fillRect(){},fillText(){},measureText:t=>({width:t.length*80})})})};try{return fn()}finally{globalThis.document=prior}};
function kit(){const k={};k.box=(p,x,y,z,w,h,d,c,m)=>{const o=new THREE.Mesh(new THREE.BoxGeometry(),m||new THREE.MeshStandardMaterial({color:c}));o.position.set(x,y,z);o.scale.set(w,h,d);p.add(o);return o};k.cylinder=(p,x,y,z,r,h,c)=>k.box(p,x,y,z,r*2,h,r*2,c);k.sign=sign;return k}

test('street tree pits, crowns and furniture have deliberate clearance with matching colliders',()=>{
 for(const t of STREET_TREES){
  for(const dx of [-.96,.96])for(const dz of [-.96,.96])assert.equal(streetSurface(t.x+dx,t.z+dz)?.kind,'pavement');
  assert.ok(STREET_LAMPS.every(p=>Math.hypot(t.x-p.x,t.z-p.z)>4.2),'crown through lamp');
  assert.ok(STREET_PLANTERS.every(p=>Math.abs(t.x-p.x)>p.w+.96+.25||Math.abs(t.z-p.z)>p.d+.96+.25),'pit through planter');
  if(t.z===10)assert.ok(STREET_SEATS.every(x=>Math.abs(t.x-x)>1.1+.96+.25),'pit through bench');
  assert.ok(BUILDINGS.every(([x,z,w,d])=>Math.abs(t.x-x)>w/2+3.2||Math.abs(t.z-z)>d/2+3.2),'crown through facade');
  assert.ok(PROMENADE_FIXTURES.some(p=>p.kind==='trunk'&&p.x===t.x&&p.z===t.z));
 }
});

test('entrance exclusion clips glazing and awnings without losing their remaining widths',()=>{
 for(const door of [-4,0,2,4])for(const center of [-5,-2,0,3,5]){
  const spans=facadeSpans(center,3,door,1.35);
  for(const p of spans){assert.ok(p.w>0);assert.ok(p.x+p.w/2<=door-1.35+1e-9||p.x-p.w/2>=door+1.35-1e-9);assert.ok(p.x-p.w/2>=center-1.5-1e-9&&p.x+p.w/2<=center+1.5+1e-9)}
 }
});

test('building aprons fill frontage gaps, never overlay streets, and agree with walking height',()=>canvas(()=>{
 const world={scene:new THREE.Scene(),staticGroups:[]},root=buildFrontages(world);root.updateMatrixWorld(true);
 const ray=new THREE.Raycaster(),down=new THREE.Vector3(0,-1,0);
 for(const p of frontagePatches()){
  const x=p.minX+(p.maxX-p.minX)*.37,z=p.minZ+(p.maxZ-p.minZ)*.61;assert.equal(streetSurface(x,z),null);near(groundHeight(x,z),.3);
  ray.set(new THREE.Vector3(x,1,z),down);const hits=ray.intersectObject(root,true);assert.equal(hits.filter(h=>Math.abs(h.point.y-.3)<1e-5).length,1,'duplicate paving surface');
 }
 const bank=BUILDINGS.find(b=>b[6]==='STADTBANK');assert.ok(frontageSurface(bank[0]+bank[2]/2+.4,bank[1]),'empty green strip at modern facade');
}));

test('outer sidewalk faces close the height gaps behind pavements and at the station',()=>canvas(()=>{
 const world={scene:new THREE.Scene(),staticGroups:[],atmosphere:{materials:[]}},g=buildStreets(world,kit());g.updateMatrixWorld(true);
 const ray=new THREE.Raycaster();
 for(const [origin,direction] of [[[50,.15,14],[0,0,-1]],[[-124,.24,18],[1,0,0]]]){ray.set(new THREE.Vector3(...origin),new THREE.Vector3(...direction));assert.ok(ray.intersectObject(g.getObjectByName('Street · kerb')).some(h=>h.distance<1),'open pavement edge')}
}));

test('street signs have correctly oriented front and rear text with solid backing',()=>canvas(()=>{
 const world={scene:new THREE.Scene(),staticGroups:[],colliders:[]};addStreetSigns(world,kit());world.scene.updateMatrixWorld(true);
 assert.equal(world.colliders.length,STREET_POSTS.length);
 for(const g of world.staticGroups){
  const labels=[];g.traverse(o=>{if(o.name.startsWith('Sign · '))labels.push(o)});assert.equal(labels.length,4);
  for(let i=0;i<4;i+=2){const a=new THREE.Vector3(0,0,1).applyQuaternion(labels[i].getWorldQuaternion(new THREE.Quaternion())),b=new THREE.Vector3(0,0,1).applyQuaternion(labels[i+1].getWorldQuaternion(new THREE.Quaternion()));near(a.dot(b),-1);assert.equal(labels[i].material.side,THREE.FrontSide);assert.equal(labels[i+1].material.side,THREE.FrontSide)}
 }
 for(const [w,h] of [[3,.4],[.5,.5],[15,.35]]){const s=labelSize(w,h);assert.ok(Math.abs(s.width/s.height-w/h)/(w/h)<.015);assert.ok(s.width<=2048)}
}));

test('functional facades create one door below the fascia and preserve address numbers',()=>canvas(()=>{
 for(const l of LOCATIONS.filter(l=>!['recycle','park'].includes(l.id))){const root=new THREE.Group();decorateAddress(root,kit(),l,0);const door=root.getObjectByName('Entrance · '+l.id);assert.ok(door);root.updateMatrixWorld(true);const bounds=new THREE.Box3().setFromObject(door);assert.ok(bounds.max.y<3.05);assert.ok(bounds.min.y>=0);assert.ok(bounds.max.z<=.61);
  const caption=door.children.find(o=>o.name.startsWith('Sign · ')),center=caption.getWorldPosition(new THREE.Vector3());
  for(const dy of [-.08,0,.08]){const origin=center.clone().add(new THREE.Vector3(0,dy,1));assert.equal(new THREE.Raycaster(origin,new THREE.Vector3(0,0,-1)).intersectObject(door,true)[0]?.object,caption,'door glass must not cover the lettering');}
 }
}));

test('wall UVs preserve brick size between deep side walls and short front walls',()=>{
 const mesh=new THREE.Mesh(new THREE.BoxGeometry()),original=mesh.geometry;fitFacadeUV(mesh,12,27);const uv=mesh.geometry.getAttribute('uv');assert.notEqual(mesh.geometry,original);near(Math.max(...Array.from({length:8},(_,i)=>uv.getX(i))),27/12);near(Math.max(...Array.from({length:8},(_,i)=>uv.getX(16+i))),1);
});
