import test from 'node:test';
import assert from 'node:assert/strict';
import * as THREE from '../dist/vendor/three.module.js';
import {cityBackdrop,BACKDROP_BUILDINGS,BACKDROP_GROUND,BACKDROP_TILES} from '../dist/district-architecture.js';
import {buildNeighborhoodDetails} from '../dist/neighborhood-scene.js';
import {NEIGHBORHOOD_FIXTURES} from '../dist/neighborhood-layout.js';
import {BUILDINGS,LOCATIONS} from '../dist/data.js';
import {DISTRICT_FIXTURES,WORLD_BOUNDS,exteriorContains} from '../dist/city-layout.js';
import {CROSSINGS,CROSSWALK_STRIPE_WIDTH,HALF_ROAD,onRoad} from '../dist/street-layout.js';
import {buildStreets} from '../dist/street-scene.js';
import {groundHeight} from '../dist/spatial.js';
import {CityNavigation} from '../dist/navigation.js';
import {deliveryLeg} from '../dist/delivery-routes.js';

function kit(){
 const materials=new Map(),geometry=new THREE.BoxGeometry();
 const k={mat:c=>{if(!materials.has(c))materials.set(c,new THREE.MeshStandardMaterial({color:c}));return materials.get(c);}};
 k.box=(p,x,y,z,w,h,d,c,m)=>{const v=new THREE.Mesh(geometry,m||k.mat(c));v.position.set(x,y,z);v.scale.set(w,h,d);p.add(v);return v;};
 k.cylinder=(p,x,y,z,r,h,c)=>k.box(p,x,y,z,r*2,h,r*2,c);
 k.sphere=(p,x,y,z,r,c)=>k.box(p,x,y,z,r*2,r*2,r*2,c);
 k.sign=(p,text,x,y,z,w,h)=>{const v=k.box(p,x,y,z,w,h,.001,0xffffff);v.name=text;return v;};return k;
}
const near=(a,b)=>assert.ok(Math.abs(a-b)<1e-5,`${a} differs from ${b}`);
const fakeCanvas=fn=>{const prev=globalThis.document;globalThis.document={createElement:()=>({getContext:()=>({fillRect(){}})})};try{return fn();}finally{globalThis.document=prev;}};

test('rendered crossings have nine symmetric stripes with identical kerb margins',()=>fakeCanvas(()=>{
 const world={scene:new THREE.Scene(),staticGroups:[],atmosphere:{materials:[]}},g=buildStreets(world,kit());
 for(const c of CROSSINGS){
  const stripes=g.children.filter(o=>Math.abs(o.scale.y-.005)<1e-8&&Math.abs(o.position.x-c.x)<=HALF_ROAD&&Math.abs(o.position.z-c.z)<=HALF_ROAD&&(c.axis==='x'?Math.abs(o.position.z-c.z)<1e-6&&o.scale.z===3.2:Math.abs(o.position.x-c.x)<1e-6&&o.scale.x===3.2));
  assert.equal(stripes.length,9);
  const centers=stripes.map(o=>c.axis==='x'?o.position.x-c.x:o.position.z-c.z).sort((a,b)=>a-b);
  for(let i=0;i<centers.length;i++)near(centers[i],-centers.at(-i-1));
  near(HALF_ROAD+centers[0]-CROSSWALK_STRIPE_WIDTH/2,HALF_ROAD-centers.at(-1)-CROSSWALK_STRIPE_WIDTH/2);
  assert.ok(Math.abs(centers.at(-1))+CROSSWALK_STRIPE_WIDTH/2<HALF_ROAD);
 }
}));

test('every scenery building has soil beneath all corners and foundations overlapping that soil',()=>{
 const k=kit(),world={scene:new THREE.Scene(),staticGroups:[],art:{windows:Array.from({length:5},()=>k.mat(0x667771))}},g=cityBackdrop(world,k);g.updateMatrixWorld(true);
 const soil=g.getObjectByName('Continuous scenery ground'),ray=new THREE.Raycaster(),down=new THREE.Vector3(0,-1,0);
 const roots=g.children.filter(o=>o.name.startsWith('Scenery · '));assert.equal(roots.length,BACKDROP_BUILDINGS.length);
 for(let i=0;i<roots.length;i++){
  const b=BACKDROP_BUILDINGS[i],foundation=roots[i].children[0],bounds=new THREE.Box3().setFromObject(foundation);
  assert.ok(bounds.min.y<BACKDROP_GROUND.top&&bounds.max.y>0);
  for(const sideX of [-1,1])for(const sideZ of [-1,1]){
   ray.set(new THREE.Vector3(b.x+sideX*(b.w/2+.18),1,b.z+sideZ*(b.d/2+.18)),down);
   const hit=ray.intersectObject(soil);assert.ok(hit.length);near(hit[0].point.y,BACKDROP_GROUND.top);
  }
  assert.ok(!exteriorContains(b.x,b.z),'scenery must not replace playable streets');
  assert.equal(roots[i].children.filter(o=>o.isGroup).length,4,'all four facades have windows');
 }
 const bounds=new THREE.Box3().setFromObject(g);assert.ok(bounds.max.x<200,'background hides with exterior in interior mode');
 assert.ok(BACKDROP_BUILDINGS.filter(b=>b.x<-120).every(b=>b.h<=12),'station outskirts stay low-rise');
 let materialMatches=0;g.traverse(o=>{if(world.art.windows.includes(o.material))materialMatches++;});assert.ok(materialMatches>100,'scenery windows follow existing night lighting');
});

test('new fixtures are grounded, keep road space and entrances clear, and use matching collision extents',()=>{
 const world={scene:new THREE.Scene(),staticGroups:[],colliders:[]},g=buildNeighborhoodDetails(world,kit());g.updateMatrixWorld(true);
 assert.equal(world.colliders.length,NEIGHBORHOOD_FIXTURES.length);
 for(const root of g.children.filter(o=>o.userData.fixture)){
  const p=root.userData.fixture,bounds=new THREE.Box3().setFromObject(root);near(root.position.y,groundHeight(p.x,p.z));
  assert.ok(bounds.min.y>=root.position.y-.03&&bounds.min.y<=root.position.y+.03,`floating ${p.kind}`);
  assert.ok(bounds.min.x>=p.x-p.w-.01&&bounds.max.x<=p.x+p.w+.01,`width ${p.kind}`);
  assert.ok(bounds.min.z>=p.z-p.d-.01&&bounds.max.z<=p.z+p.d+.01,`depth ${p.kind}`);
  for(const dx of [-p.w,0,p.w])for(const dz of [-p.d,0,p.d])assert.equal(onRoad(p.x+dx,p.z+dz),false);
  for(const [x,z,w,d] of BUILDINGS)assert.ok(Math.abs(p.x-x)>=p.w+w/2||Math.abs(p.z-z)>=p.d+d/2,'fixture inside building');
  for(const l of LOCATIONS)assert.ok(Math.abs(p.x-l.x)>=p.w+1.5||Math.abs(p.z-l.z)>=p.d+1.5,`blocked ${l.id}`);
 }
});

test('all location approaches stay connected with new furniture, including western delivery tours',()=>{
 const blocks=BUILDINGS.map(([x,z,w,d])=>({x,z,w:w/2+.35,d:d/2+.35})).concat(DISTRICT_FIXTURES,NEIGHBORHOOD_FIXTURES);
 const canWalk=(x,z,r=.45)=>exteriorContains(x,z,r)&&!blocks.some(b=>Math.abs(x-b.x)<b.w+r&&Math.abs(z-b.z)<b.d+r);
 const nav=new CityNavigation(canWalk,4,WORLD_BOUNDS),start=LOCATIONS.find(l=>l.id==='jobs');
 for(const l of LOCATIONS){assert.ok(canWalk(l.x,l.z),`approach ${l.id}`);if(l!==start){const path=nav.find(start,l);assert.ok(path.length>=2,`route to ${l.id}`);}}
 for(const id of ['deliveryA','deliveryB','deliveryC','deliveryKiosk','deliveryWorkshop'])assert.ok(deliveryLeg('jobs',id)?.distance>0);
});


test('scenery ground fills only the apron, without overlapping existing street base meshes',()=>{
 const overlap=(a,b)=>Math.min(a[1],b[1])>Math.max(a[0],b[0])&&Math.min(a[3],b[3])>Math.max(a[2],b[2]);
 const original=[[-125,125,-125,125],[-225,-125,-80,80]];
 for(let i=0;i<BACKDROP_TILES.length;i++)for(const other of [...original,...BACKDROP_TILES.slice(i+1)])assert.equal(overlap(BACKDROP_TILES[i],other),false);
 for(let x=BACKDROP_GROUND.minX+.17;x<BACKDROP_GROUND.maxX;x+=5)for(let z=BACKDROP_GROUND.minZ+.31;z<BACKDROP_GROUND.maxZ;z+=5){
  const count=[...original,...BACKDROP_TILES].filter(([a,b,c,d])=>x>a&&x<b&&z>c&&z<d).length;assert.equal(count,1,`ground gap or overlap ${x},${z}`);
 }
});
