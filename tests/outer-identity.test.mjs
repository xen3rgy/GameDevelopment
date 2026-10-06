import test from 'node:test';
import assert from 'node:assert/strict';
import * as THREE from '../dist/vendor/three.module.js';
import {expansionBuilding} from '../dist/district-architecture.js';
import {EXPANSION_BUILDINGS,EXPANSION_FIXTURES} from '../dist/expansion-layout.js';
import {portBuilding} from '../dist/district-frontages.js';

const geometry=new THREE.BoxGeometry(),cylinderGeometry=new THREE.CylinderGeometry(1,1,1,8),materials=new Map();
const kit={mat(c){if(!materials.has(c))materials.set(c,new THREE.MeshStandardMaterial({color:c}));return materials.get(c);}};
kit.box=(p,x,y,z,w,h,d,c,m)=>{const mesh=new THREE.Mesh(geometry,m||kit.mat(c));mesh.position.set(x,y,z);mesh.scale.set(w,h,d);p.add(mesh);return mesh;};
kit.cylinder=(p,x,y,z,r,h,c)=>{const mesh=new THREE.Mesh(cylinderGeometry,kit.mat(c));mesh.position.set(x,y,z);mesh.scale.set(r,h,r);p.add(mesh);return mesh;};
kit.sign=(p,text,x,y,z,w,h)=>{const mesh=kit.box(p,x,y,z,w,h,.001,0xffffff);mesh.name=text;return mesh;};
const build=b=>{const root=new THREE.Group();expansionBuilding(root,kit,b);root.updateMatrixWorld(true);return root;};
const signature=root=>{const parts=[];root.traverse(o=>{if(o.isMesh)parts.push([o.position.toArray(),o.scale.toArray(),o.rotation.toArray(),o.geometry.getAttribute('position').count]);});return JSON.stringify(parts);};
test('port rebuild retains the original body and camera collision envelope',()=>{
 const world={scene:new THREE.Scene(),staticGroups:[],colliders:[]},art={w:world,k:kit,index:0,windows:Array.from({length:5},()=>kit.mat(0x43545a))};
 const g=portBuilding(art,-71,-91,32,26,11,0x888888,'WESTHAFEN LOGISTIK');
 // The reused entrance has its original 60 cm shallow doorstep beyond the facade.
 assert.ok(g.getObjectByName('Entrance · depot'));g.getObjectByName('Entrance · depot').removeFromParent();
 g.updateMatrixWorld(true);const bounds=new THREE.Box3().setFromObject(g),f=world.colliders[0];
 assert.deepEqual(f,{x:-71,z:-91,w:16.35,d:13.35,h:13.8});assert.ok(bounds.min.x>=f.x-f.w&&bounds.max.x<=f.x+f.w);assert.ok(bounds.min.z>=f.z-f.d-.03&&bounds.max.z<=f.z+f.d+.03);assert.ok(bounds.max.y<=f.h);
});

test('outer building details fit unchanged collision footprints and height allowances',()=>{
 for(const [i,b] of EXPANSION_BUILDINGS.entries()){
  const bounds=new THREE.Box3().setFromObject(build(b)),f=EXPANSION_FIXTURES[i];
  assert.ok(bounds.min.x>=f.x-f.w-.001&&bounds.max.x<=f.x+f.w+.001,`width ${b.style}/${b.seed}`);
  assert.ok(bounds.min.z>=f.z-f.d-.001&&bounds.max.z<=f.z+f.d+.001,`depth ${b.style}/${b.seed}`);
  assert.ok(bounds.min.y>=-.001&&bounds.max.y<=f.h+.001,`height ${b.style}/${b.seed}`);
 }
});

test('districts differ geometrically and seeded architectural variants remain deterministic',()=>{
 const base={x:260,z:-200,w:24,d:22,h:18,seed:3};
 const signatures=['residential','industrial','office','campus','townhouse'].map(style=>signature(build({...base,style})));
 assert.equal(new Set(signatures).size,5);
 for(const style of ['residential','industrial','office','campus','townhouse']){
  const b={...base,style};assert.equal(signature(build(b)),signature(build(b)));
  assert.notEqual(signature(build(b)),signature(build({...b,seed:0})));
 }
});
