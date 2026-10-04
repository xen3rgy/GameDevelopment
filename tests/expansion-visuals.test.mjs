import test from 'node:test';
import assert from 'node:assert/strict';
import * as THREE from '../dist/vendor/three.module.js';
import {expansionBuilding} from '../dist/district-architecture.js';
import {EXPANSION_BUILDINGS,EXPANSION_FIXTURES,EXPANSION_PATHS} from '../dist/expansion-layout.js';
import {meadowPoint,meadowPlacements,OUTER_MEADOW_TILES} from '../dist/meadow.js';
import {TreeSystem} from '../dist/tree-system.js';
import {TUNING_SITE} from '../dist/tuning-layout.js';
const geometry=new THREE.BoxGeometry(),materials=new Map();
const mat=c=>{if(!materials.has(c))materials.set(c,new THREE.MeshStandardMaterial({color:c}));return materials.get(c);};
const box=(g,x,y,z,w,h,d,c,m)=>{const mesh=new THREE.Mesh(geometry,m||mat(c));mesh.position.set(x,y,z);mesh.scale.set(w,h,d);g.add(mesh);return mesh;};
const kit={box,mat,sign(){},cylinder(g,x,y,z,r,h,c){return box(g,x,y,z,r*2,h,r*2,c);}};
test('all outer facades stay grounded and roofs remain in their existing collision height allowance',()=>{
 for(const b of EXPANSION_BUILDINGS){const root=new THREE.Group();expansionBuilding(root,kit,b);const bounds=new THREE.Box3().setFromObject(root);assert.ok(bounds.min.y<=0);assert.ok(bounds.max.y<=b.h+2,b.style);assert.ok(bounds.min.x>=b.x-b.w/2-1.1);assert.ok(bounds.max.x<=b.x+b.w/2+1.1);assert.ok(bounds.min.z>=b.z-b.d/2-1.1);assert.ok(bounds.max.z<=b.z+b.d/2+1.1);let textured=false;root.traverse(m=>{if(m.material?.name.startsWith('District '))textured=true;});assert.ok(textured);}
});
test('outer meadow tiles are disjoint and planting excludes streets, plazas, buildings and tuning bay',()=>{
 for(let i=0;i<OUTER_MEADOW_TILES.length;i++)for(let j=i+1;j<OUTER_MEADOW_TILES.length;j++){const [a,b,c,d]=OUTER_MEADOW_TILES[i],[A,B,C,D]=OUTER_MEADOW_TILES[j];assert.ok(Math.min(b,B)<=Math.max(a,A)||Math.min(d,D)<=Math.max(c,C));}
 assert.equal(meadowPoint(220,0,EXPANSION_FIXTURES),false);assert.equal(meadowPoint(TUNING_SITE.x,TUNING_SITE.z,EXPANSION_FIXTURES),false);
 for(const p of [...EXPANSION_PATHS,...EXPANSION_BUILDINGS])assert.equal(meadowPoint(p.x,p.z,EXPANSION_FIXTURES),false);
 const groups=meadowPlacements(EXPANSION_FIXTURES),points=[...groups.values()].flat();assert.ok(points.some(p=>p.x>300));assert.ok(points.some(p=>p.z<-180));assert.ok(points.some(p=>p.z>180));assert.ok(points.length<100000);
});
test('trees share geometry and material while distant blocks can be independently culled',()=>{
 const exterior=new THREE.Group(),trees=new TreeSystem({exterior});trees.add(0,0);trees.add(4,4);trees.add(200,200);trees.mount(trees.near,geometry,mat(0xffffff),true);assert.equal(trees.meshes.length,2);assert.equal(trees.meshes.reduce((n,m)=>n+m.count,0),3);assert.ok(trees.meshes.every(m=>m.geometry===geometry&&m.frustumCulled&&m.boundingSphere.radius<30));
});
