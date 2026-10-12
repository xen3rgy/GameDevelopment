import {WEST_BUILDINGS,WEST_PATHS,westPathHeight} from '../dist/west-edge-layout.js';
import {westTreeClear,westShrub} from '../dist/west-vegetation.js';
import {meadowPlacements} from '../dist/meadow.js';
import test from 'node:test';
import assert from 'node:assert/strict';
import * as THREE from '../dist/vendor/three.module.js';
import {WestEdgeScene,westPathPatches} from '../dist/west-edge-scene.js';
import {WEST_SPAWN,WEST_LOCATIONS,WEST_BINS,WEST_BOTTLE_SPOTS,WEST_HOMES,WEST_ACTIVITIES,WEST_BUS_STOP} from '../dist/west-edge-layout.js';
import {WORLD_BOUNDS,exteriorContains,districtOf} from '../dist/city-layout.js';
import {streetSurface} from '../dist/street-layout.js';
import {groundHeight} from '../dist/spatial.js';
import {CityNavigation,CollisionIndex} from '../dist/navigation.js';
import {pedestrianSurface} from '../dist/pedestrian-life.js';
import {mapTransform,mapPlaces} from '../dist/city-map.js';
import {segmentClear} from '../dist/movement.js';
import {westBox,batchWestGeometry} from '../dist/west-materials.js';
import {westParkedColliders} from '../dist/west-parked.js';
import {CameraRig} from '../dist/spatial.js';

test('parked scenery cars have a close-fitting footprint and lower bonnet collider',()=>{
 for(const type of ['car','van','sport'])for(const angle of [0,.18,Math.PI/2]){
  const parts=westParkedColliders(0,0,angle,type),idx=new CollisionIndex(parts);
  assert.ok(idx.blocked(0,0,.1));
  const side={x:Math.cos(angle)*1.55,z:-Math.sin(angle)*1.55};
  assert.equal(idx.blocked(side.x,side.z,.35),false,type+' side clearance');
  assert.ok(parts.at(-1).h<1.1);assert.ok(parts.some(p=>p.h>1.4));
 }
});
test('walking and orbiting beside a parked car retain third-person distance',()=>{
 for(const type of ['car','van','sport']){
  const parts=westParkedColliders(0,0,.18,type,-.08),rig=new CameraRig();let previous=null;
  for(let i=0;i<420;i++){
   const x=5-Math.min(i/90,1)*3.4,view=rig.update({x,y:-.05,z:0},Math.PI/2+i*.02,.22,8,1/60,parts);
   assert.ok(view.distance>3,type+' camera collapsed at '+i);
   if(previous)assert.ok(Math.abs(view.anchor.y-previous.anchor.y)<.06,'height must ease');
   previous=view;
  }
 }
});

const mats=new Map(),geo=new THREE.BoxGeometry();
const kit={mat:c=>{if(!mats.has(c))mats.set(c,new THREE.MeshStandardMaterial({color:c}));return mats.get(c);}};
kit.box=(p,x,y,z,w,h,d,c,m)=>{const q=new THREE.Mesh(geo,m||kit.mat(c));q.position.set(x,y,z);q.scale.set(w,h,d);p.add(q);return q;};
kit.cylinder=(p,x,y,z,r,h,c)=>kit.box(p,x,y,z,r*2,h,r*2,c);
kit.sphere=(p,x,y,z,r,c)=>kit.box(p,x,y,z,r*2,r*2,r*2,c);
kit.sign=(p,text,x,y,z,w,h,c,b,angle=0)=>{const q=kit.box(p,x,y,z,w,h,.01,0xffffff);q.rotation.y=angle;return q;};
const placedTrees=[];
const world={exterior:new THREE.Group(),art:{windows:[]},colliders:[],overheadColliders:[],tree(x,z,r,options){placedTrees.push({x,z,r,options});},batchStaticGroup(){},atmosphere:{registerLamp(){},addPuddle(){}}};
const westScene=new WestEdgeScene(world,kit);
const index=new CollisionIndex(world.colliders),canWalk=(x,z,r=.4)=>exteriorContains(x,z,r)&&!index.blocked(x,z,r);
const nav=new CityNavigation(canWalk,4,WORLD_BOUNDS);

test('all western interactions and bottle candidates are physically reachable from the story start',()=>{
 assert.ok(canWalk(WEST_SPAWN.x,WEST_SPAWN.z));
 for(const p of [...WEST_LOCATIONS,...WEST_BINS,...WEST_BOTTLE_SPOTS,WEST_BUS_STOP,WEST_BUS_STOP.arrival]){
  assert.ok(canWalk(p.x,p.z),`blocked ${p.id||JSON.stringify(p)}`);
  const route=nav.find(WEST_SPAWN,p);assert.ok(route.length>1,`unreachable ${p.id||JSON.stringify(p)}`);
  for(let i=1;i<route.length;i++)assert.ok(segmentClear(route[i-1],route[i],canWalk,.4));
 }
});
test('western road and old boundary stay clear in both directions',()=>{
 for(let x=-533;x<=-225;x+=.5){assert.ok(canWalk(x,0,1.35),`road obstruction ${x}`);assert.equal(groundHeight(x,0),.14);}
 for(const end of [{x:-202,z:0},{x:-27,z:-4}])assert.ok(nav.find(WEST_SPAWN,end).length>1);
 assert.deepEqual(WORLD_BOUNDS,{minX:-550,maxX:440,minZ:-255,maxZ:255});
 assert.equal(districtOf(WEST_SPAWN),'BRUCHFELD');assert.equal(districtOf({x:-380,z:0}),'ALTGLEIS');assert.equal(districtOf({x:-290,z:0}),'WESTBRUCH');
});
test('forecourt meshes agree with support heights and never overlap street faces',()=>{
 const patches=westPathPatches();
 for(const [i,p] of patches.entries()){
  assert.equal(streetSurface(p.x,p.z),null);assert.equal(groundHeight(p.x,p.z),westPathHeight(p,p.x,p.z));
  for(const q of patches.slice(0,i))assert.ok(Math.min(p.x+p.w/2,q.x+q.w/2)-Math.max(p.x-p.w/2,q.x-q.w/2)<1e-6||Math.min(p.z+p.d/2,q.z+q.d/2)-Math.max(p.z-p.d/2,q.z-q.d/2)<1e-6);
 }
});
test('gravel entrance ramps and newly connected rear paths have rendered support',()=>{
 world.exterior.updateMatrixWorld(true);
 for(const [x,z] of [[-468,-17],[-472,-10],[-381,-61],[-381,-63],[-534,-40],[-520,-48],[-475,-37],[-480,67],[-318,61]]){
  const ray=new THREE.Raycaster(new THREE.Vector3(x,.8,z),new THREE.Vector3(0,-1,0)),hits=ray.intersectObject(world.exterior,true);
  assert.ok(hits.length,'missing rendered path '+x+','+z);assert.ok(Math.abs(hits[0].point.y-groundHeight(x,z))<.025,'support mismatch '+x+','+z);assert.ok(canWalk(x,z,.35),'blocked connected path '+x+','+z);
 }
 assert.equal(groundHeight(-468,-17),-.03);assert.ok(groundHeight(-384,-61)>groundHeight(-384,-63));
});
test('rear lane connects continuously to the main street at both ends',()=>{
 for(const [a,b] of [[[-534,-7],[-534,-43]],[[-534,-43],[-475,-43]],[[-475,-43],[-475,-7]],[[-381,-59],[-381,-68]],[[-381,-68],[-393,-68]],[[-393,-68],[-393,-80.2]]]){
  const length=Math.hypot(b[0]-a[0],b[1]-a[1]);for(let n=0;n<=length*2;n++){const t=n/(length*2),x=a[0]+(b[0]-a[0])*t,z=a[1]+(b[1]-a[1])*t;assert.ok(canWalk(x,z,.35),'rear route obstruction '+x+','+z);assert.equal(groundHeight(x,z),.14);}
 }
});
test('loading dock is accessible on a continuous ramp rather than an invisible solid block',()=>{
 world.exterior.updateMatrixWorld(true);for(let x=-389;x>=-405;x-=.2){assert.ok(canWalk(x,-47.6,.35));const ray=new THREE.Raycaster(new THREE.Vector3(x,1.2,-47.6),new THREE.Vector3(0,-1,0));const hit=ray.intersectObject(world.exterior,true)[0];assert.ok(hit);assert.ok(Math.abs(hit.point.y-groundHeight(x,-47.6))<.025);}
 assert.equal(groundHeight(-399,-47.6),.57);assert.ok(Math.abs(groundHeight(-390.5,-47.6)-.27)<.001);
});
test('container and shed colliders match their detailed models; bus entrance stays open',()=>{
 const containers=world.colliders.filter(c=>c.westContainer),stores=world.colliders.filter(c=>c.westStore);assert.equal(containers.length,2);assert.equal(stores.length,3);
 for(const c of [...containers,...stores]){assert.ok(!canWalk(c.x,c.z,.35));assert.ok(canWalk(c.x+c.w+.55,c.z,.35),'oversized prop collision');}
 assert.ok(canWalk(-278,14.0,.35));assert.ok(canWalk(-276,10,.4));assert.ok(world.overheadColliders.some(c=>c.westBusRoof));
 const roof=world.overheadColliders.find(c=>c.westBusRoof),rig=new CameraRig();for(let n=0;n<64;n++){
  const position={x:-278,y:.25,z:13.7},view=rig.update(position,n*Math.PI/32,.22,8,1/60,[...world.colliders,...world.overheadColliders],null,westScene.cameraShelter(position));assert.ok(view.distance>2.5,'bus shelter collapsed to first person');
 }
});
test('resident portals connect to at least one local destination on supported walking surfaces',()=>{
 const walk=(x,z,r)=>canWalk(x,z,r)&&pedestrianSurface(x,z,r);
 for(const home of WEST_HOMES){assert.ok(walk(home.x,home.z,.4),home.id);const destinations=WEST_ACTIVITIES.filter(p=>p.district===home.district&&walk(p.x,p.z,.4));assert.ok(destinations.length,home.id+' destinations');const local=new CityNavigation(walk,1,{minX:home.x-45,maxX:home.x+75,minZ:-65,maxZ:65});assert.ok(destinations.some(d=>local.find(home,d).length>1),home.id+' disconnected');}
});
test('map fits western landmarks while minimap keeps its local scale',()=>{
 const t=mapTransform(900,600,WEST_SPAWN,true),small=mapTransform(180,180,WEST_SPAWN,false);
 for(const x of [WORLD_BOUNDS.minX,WORLD_BOUNDS.maxX])assert.ok(t.point(x,0)[0]>=20&&t.point(x,0)[0]<=880);
 assert.equal(small.scale,180/135);for(const id of ['roughSleep','westKiosk','westRecycle','busWestbruch','oldRailTriangle'])assert.ok(mapPlaces({}).some(p=>p.id===id),id);
});

test('western batching preserves transformed silhouettes, surface UVs and hidden variants',()=>{
 const root=new THREE.Group();root.position.set(8,0,4);const parent=new THREE.Group();parent.rotation.y=.4;root.add(parent);
 for(let i=0;i<4;i++)westBox(parent,'concrete',i*3,1,0,1+i*.1,2,1);
 const hidden=westBox(parent,'concrete',20,1,0,1,2,1);hidden.visible=false;
 root.updateMatrixWorld(true);const before=new THREE.Box3().setFromObject(root),ray=new THREE.Raycaster(new THREE.Vector3(8,10,4),new THREE.Vector3(0,-1,0));const y=ray.intersectObject(root,true)[0].point.y;
 batchWestGeometry(root);root.updateMatrixWorld(true);const after=new THREE.Box3().setFromObject(root),meshes=[];root.traverse(m=>{if(m.isMesh&&m.visible)meshes.push(m);});
 assert.equal(meshes.length,1);assert.equal(hidden.parent,parent);assert.equal(hidden.visible,false);assert.ok(before.min.distanceTo(after.min)<1e-5);assert.ok(before.max.distanceTo(after.max)<1e-5);
 assert.ok(Math.abs(ray.intersectObject(root,true)[0].point.y-y)<1e-5);assert.equal(meshes[0].geometry.attributes.uv.count,meshes[0].geometry.attributes.position.count);assert.ok(Array.from(meshes[0].geometry.attributes.uv.array).every(Number.isFinite));
});


test('western tree crowns clear every building, including the previously intersected house',()=>{
 assert.ok(placedTrees.length>20);
 for(const tree of placedTrees)assert.ok(westTreeClear(tree.x,tree.z),JSON.stringify(tree));
 assert.ok(!placedTrees.some(t=>Math.abs(t.x+458)<.1&&Math.abs(t.z-53)<.1));
});
test('new playground and rear passage remain reachable around equipment',()=>{
 for(const p of [{x:-507,z:39},{x:-507,z:48},{x:-516.5,z:51},{x:-507,z:55},{x:-514,z:61},{x:-525,z:35}]){
  assert.ok(canWalk(p.x,p.z),'blocked yard '+JSON.stringify(p));
  assert.ok(nav.find(WEST_SPAWN,p).length>1,'unreachable yard '+JSON.stringify(p));
 }
});
test('western undergrowth is sparse non-flowering vegetation while core plants remain varied',()=>{
 const plants=[...meadowPlacements().values()].flat(),west=plants.filter(p=>p.x<-260);
 assert.ok(west.length>100&&west.length<3800);
 assert.ok(west.every(p=>[1,5,6,11].includes(p.cell)&&p.size<=.66));
 assert.ok(plants.some(p=>p.x>-125&&p.cell===2));
});
test('leafy shrub variants reuse detailed geometry rather than solid spheres',()=>{
 const root=new THREE.Group(),a=westShrub(root,0,0,0,1,0),b=westShrub(root,3,0,0,1,4);
 assert.equal(a.children.length,4);assert.equal(a.children[0].geometry,b.children[0].geometry);
 assert.ok(a.children[0].geometry.attributes.position.count>500);
 assert.ok(a.children.every(m=>m.geometry.type==='BufferGeometry'));
});

test('new street-wall buildings do not overlap another building footprint',()=>{
 for(const b of WEST_BUILDINGS.filter(b=>b.seed>=42))for(const other of WEST_BUILDINGS){if(b===other)continue;assert.ok(Math.abs(b.x-other.x)>=(b.w+other.w)/2+.6||Math.abs(b.z-other.z)>=(b.d+other.d)/2+.6,b.id+' overlaps '+other.id);}
});
test('balcony camera slabs match the rendered floor heights',()=>{
 world.exterior.updateMatrixWorld(true);const slabs=world.overheadColliders.filter(c=>c.westBalcony);assert.ok(slabs.length>25);
 for(const c of slabs){const ray=new THREE.Raycaster(new THREE.Vector3(c.x,c.h+.4,c.z),new THREE.Vector3(0,-1,0),0,1);const hit=ray.intersectObject(world.exterior,true)[0];assert.ok(hit,'missing balcony');assert.ok(Math.abs(hit.point.y-c.h)<.03,'camera slab disagrees with balcony floor');}
});
