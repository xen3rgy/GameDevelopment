import test from 'node:test';
import assert from 'node:assert/strict';
import * as THREE from '../dist/vendor/three.module.js';
import {ROADS,ROAD_X,ROAD_Z,VIADUCT} from '../dist/city-layout.js';
import {ROAD_HEIGHT,PAVEMENT_HEIGHT,HALF_ROAD,CORNER_RADIUS,RAMP_WIDTH,CROSSINGS,RAMPS,streetPatches,streetSurface,crossingObstacles} from '../dist/street-layout.js';
import {PROMENADE_BOLLARDS,PROMENADE_FIXTURES} from '../dist/pedestrian-layout.js';
import {buildStreets} from '../dist/street-scene.js';
import {stepJump} from '../dist/jump-motion.js';
import {groundHeight} from '../dist/spatial.js';
import {buildingStyle,modernBuilding,cityBackdrop} from '../dist/district-architecture.js';
import {buildStationDistrict} from '../dist/station-district.js';
import {BUILDINGS} from '../dist/data.js';

const near=(a,b,t=1e-5)=>assert.ok(Math.abs(a-b)<t,`${a} != ${b}`);
function kit(){
 const mat=c=>new THREE.MeshStandardMaterial({color:c}),boxGeo=new THREE.BoxGeometry(),k={mat};
 k.box=(p,x,y,z,w,h,d,c,m)=>{const v=new THREE.Mesh(boxGeo,m||mat(c));v.position.set(x,y,z);v.scale.set(w,h,d);p.add(v);return v;};
 k.sign=(p,text,x,y,z,w,h,col,bg,rot=0)=>{const v=new THREE.Mesh(new THREE.PlaneGeometry(w,h),mat(0xffffff));v.name=text;v.position.set(x,y,z);v.rotation.y=rot;p.add(v);return v;};
 k.cylinder=(p,x,y,z,r,h,c)=>k.box(p,x,y,z,r*2,h,r*2,c);k.sphere=(p,x,y,z,r,c)=>k.box(p,x,y,z,r*2,r*2,r*2,c);
 return k;
}
const withCanvas=fn=>{const prior=globalThis.document;globalThis.document={createElement:()=>({getContext:()=>({fillRect(){}})})};try{return fn();}finally{globalThis.document=prior;}};

test('every lane and intersection stays level; pavement patches never occupy road interiors',()=>{
 for(const x of ROAD_X)for(const z of ROAD_Z)for(const dx of [-6,-3,0,3,6])for(const dz of [-6,-3,0,3,6])near(groundHeight(x+dx,z+dz),ROAD_HEIGHT);
 const patches=streetPatches();assert.ok(patches.length<12000);
 for(const p of patches){const x=(p.minX+p.maxX)/2,z=(p.minZ+p.maxZ)/2;
  assert.equal(p.kind,streetSurface(x,z).kind);
  if(p.kind!=='road')assert.ok(!ROADS.some(r=>Math.abs(x-r.x)<r.w/2&&Math.abs(z-r.z)<r.d/2));
 }
 near(PAVEMENT_HEIGHT-ROAD_HEIGHT,.16);
});
test('intersection corners are rounded, fully surfaced and curb ramps no longer overlap',()=>withCanvas(()=>{
 for(const ix of ROAD_X)for(const iz of ROAD_Z)for(const sx of [-1,1])for(const sz of [-1,1]){
  const point=(u,v)=>({x:ix+sx*(HALF_ROAD+u),z:iz+sz*(HALF_ROAD+v)});
  const inner=point(.2,.2),outer=point(CORNER_RADIUS-.2,CORNER_RADIUS-.2);
  assert.equal(streetSurface(inner.x,inner.z).kind,'road');near(groundHeight(inner.x,inner.z),ROAD_HEIGHT);
  assert.notEqual(streetSurface(outer.x,outer.z).kind,'road');near(groundHeight(outer.x,outer.z),PAVEMENT_HEIGHT);
 }
 for(let i=0;i<RAMPS.length;i++)for(let j=i+1;j<RAMPS.length;j++){const a=RAMPS[i],b=RAMPS[j];if(a.axis===b.axis)continue;const ox=Math.min(a.x+a.w/2,b.x+b.w/2)-Math.max(a.x-a.w/2,b.x-b.w/2),oz=Math.min(a.z+a.d/2,b.z+b.d/2)-Math.max(a.z-a.d/2,b.z-b.d/2);assert.ok(ox<=1e-8||oz<=1e-8,'perpendicular curb ramps overlap');}
 assert.equal(RAMP_WIDTH,3);
 const world={scene:new THREE.Scene(),staticGroups:[],atmosphere:{materials:[]}},g=buildStreets(world,kit()),surfaces=g.children.filter(m=>m.name.startsWith('Street · ')&&!m.name.endsWith('kerb')),ray=new THREE.Raycaster(),down=new THREE.Vector3(0,-1,0);
 for(const [u,v,y] of [[.2,.2,ROAD_HEIGHT],[CORNER_RADIUS-.2,CORNER_RADIUS-.2,PAVEMENT_HEIGHT]]){const x=ROAD_X[1]+HALF_ROAD+u,z=ROAD_Z[1]+HALF_ROAD+v;ray.set(new THREE.Vector3(x,2,z),down);const hits=ray.intersectObjects(surfaces,false);assert.equal(hits.length,1,`corner gap/overlap at ${x},${z}`);near(hits[0].point.y,y,2e-5);}
}));


test('bollards stay on pavement edges and never occupy vehicle lanes or junctions',()=>{
 assert.ok(PROMENADE_BOLLARDS.length>0);
 const fixtures=PROMENADE_FIXTURES.filter(p=>p.kind==='bollard');assert.deepEqual(fixtures.map(({x,z})=>({x,z})),PROMENADE_BOLLARDS);
 for(const p of PROMENADE_BOLLARDS)assert.notEqual(streetSurface(p.x,p.z)?.kind,'road',`bollard on road at ${p.x},${p.z}`);
 for(const x of [-2,6])for(const z of [-7,7])assert.equal(PROMENADE_BOLLARDS.some(p=>p.x===x&&p.z===z),false,`central junction bollard at ${x},${z}`);
});

test('marked crossings have continuous dropped kerbs on both sides and shared traffic priority',()=>{
 for(const c of CROSSINGS)for(const side of [-1,1]){
  const p=t=>({x:c.x+(c.axis==='x'?side*t:0),z:c.z+(c.axis==='z'?side*t:0)});
  let previous=ROAD_HEIGHT;
  for(let d=6.45;d<=8.6;d+=.05){const q=p(d),y=groundHeight(q.x,q.z);assert.ok(y>=previous-1e-7);assert.ok(y-previous<.005);previous=y;}
  near(previous,PAVEMENT_HEIGHT);
  const waiting=p(7.5),moving={...waiting,vx:c.axis==='x'?-side:0,vz:c.axis==='z'?-side:0};
  assert.ok(crossingObstacles([moving]).length>0);assert.equal(crossingObstacles([{...waiting,vx:0,vz:0}]).length,0);
 }
});

test('road meshes and every ramp flare match the actual support height without duplicate ground faces',()=>withCanvas(()=>{
 const world={scene:new THREE.Scene(),staticGroups:[],atmosphere:{materials:[]}},g=buildStreets(world,kit());g.updateMatrixWorld(true);
 const surfaces=g.children.filter(m=>m.name.startsWith('Street · ')&&!m.name.endsWith('kerb'));
 const ray=new THREE.Raycaster(),down=new THREE.Vector3(0,-1,0);
 for(const r of RAMPS){for(const lateral of [-1.4,-1.1,0,1.1,1.4])for(const normal of [.17,.66,1.33,1.77]){
  const x=r.crossing.x+(r.axis==='z'?lateral:(6.5+normal)*r.side),z=r.crossing.z+(r.axis==='z'?(6.5+normal)*r.side:lateral);
  ray.set(new THREE.Vector3(x,2,z),down);const hits=ray.intersectObjects(surfaces,false);assert.ok(hits.length>0,`missing ${x},${z}`);
  near(hits[0].point.y,groundHeight(x,z),2e-5);assert.equal(hits.length,1,`overlap ${x},${z}`);
 }}
 for(const x of ROAD_X)for(const z of ROAD_Z){ray.set(new THREE.Vector3(x+.123,2,z+.321),down);const hits=ray.intersectObjects(surfaces,false);assert.equal(hits.length,1);near(hits[0].point.y,ROAD_HEIGHT);}
 assert.equal(world.atmosphere.materials.length,1);
}));

test('jump trajectory is independent of the road or kerb below until actual landing',()=>{
 for(const hz of [20,30,60,120]){
  let a=null,b=null;
  for(let i=0;i<hz*.5;i++){
   a=stepJump(a,ROAD_HEIGHT,1/hz,i===0);b=stepJump(b,i>hz*.1?(i%2?PAVEMENT_HEIGHT:ROAD_HEIGHT):ROAD_HEIGHT,1/hz,i===0);
   near(a.y,b.y);near(a.velocity,b.velocity);assert.equal(a.airborne,true);
  }
  for(let i=0;i<hz;i++)b=stepJump(b,PAVEMENT_HEIGHT,1/hz,false);
  assert.equal(b.airborne,false);near(b.y,PAVEMENT_HEIGHT);near(b.velocity,0);
 }
});

test('jumping from pavement to road lands below takeoff; held or restricted jumps cannot retrigger',()=>{
 let jump=stepJump(null,PAVEMENT_HEIGHT,1/60,true);
 for(let i=0;i<100;i++)jump=stepJump(jump,ROAD_HEIGHT,1/60,true);
 assert.equal(jump.airborne,false);near(jump.y,ROAD_HEIGHT);
 jump=stepJump(jump,ROAD_HEIGHT,1/60,false);jump=stepJump(jump,ROAD_HEIGHT,1/60,true);assert.equal(jump.airborne,true);
 const paused=stepJump(jump,ROAD_HEIGHT,0,true);near(paused.y,jump.y);near(paused.velocity,jump.velocity);
 assert.equal(stepJump(null,ROAD_HEIGHT,1/60,true,false).airborne,false);
});

test('modern facades preserve business footprints while differing from historic western blocks',()=>{
 const k=kit(),world={scene:new THREE.Scene(),staticGroups:[],colliders:[]},art={w:world,k,windows:Array.from({length:5},()=>k.mat(0x78958a))};
 for(const b of BUILDINGS.filter(([x,z])=>x>0&&Math.abs(z)<125)){
  const g=modernBuilding(art,...b);g.updateMatrixWorld(true);assert.match(g.name,/Modern/);
  const [x,z,w,d]=b,c=world.colliders.at(-1);assert.equal(c.x,x);assert.equal(c.z,z);near(c.w,w/2+.35);near(c.d,d/2+.35);
  g.traverse(o=>{assert.ok(o.matrixWorld.elements.every(Number.isFinite));});assert.ok(g.getObjectByName(b[6]));
 }
 assert.equal(buildingStyle(65,-28,'CAFÉ MORGEN'),'modern');assert.equal(buildingStyle(-175,-42,'LINDENSTADT WEST'),'heritage');assert.equal(buildingStyle(-216,34,null),'heritage');
 cityBackdrop(world,k);const bounds=new THREE.Box3().setFromObject(world.staticGroups.at(-1));assert.ok(bounds.max.x<200,'background remains in the exterior visibility group');
});

test('the Gleishof sign is contained on its wall, and bridge signs have real backing panels',()=>withCanvas(()=>{
 const world={scene:new THREE.Scene(),staticGroups:[],colliders:[],tree(){},atmosphere:{addPuddle(){}}};buildStationDistrict(world,kit());world.scene.updateMatrixWorld(true);
 const label=world.scene.getObjectByName('WOHNEN & HANDWERK'),bounds=new THREE.Box3().setFromObject(label);
 assert.ok(bounds.min.x>-193&&bounds.max.x<-165);assert.ok(bounds.min.z>39.08&&bounds.max.z<39.3);
 const bridge=world.scene.getObjectByName('BAHNHOFSVIERTEL');assert.ok(bridge);assert.ok(Math.abs(bridge.position.x-(VIADUCT.x+VIADUCT.w/2))<.2);
}));
test('the full station quarter has textured cobblestone ground instead of a raw grey base',()=>withCanvas(()=>{
 const world={scene:new THREE.Scene(),staticGroups:[],colliders:[],tree(){},atmosphere:{addPuddle(){}}},g=buildStationDistrict(world,kit()),yard=g.getObjectByName('Bahnhofsviertel · Pflastergrund');
 assert.ok(yard);assert.ok(yard.material?.map,'station yard must have a repeating surface texture');assert.equal(yard.scale.x,100);assert.equal(yard.scale.z,160);
 const bounds=new THREE.Box3().setFromObject(yard);near(bounds.max.y,-.05);assert.ok(yard.material.roughness>=.95);assert.ok(yard.material.bumpScale>.02);
}));

