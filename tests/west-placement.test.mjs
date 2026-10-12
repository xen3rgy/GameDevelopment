import test from 'node:test';
import assert from 'node:assert/strict';
import * as THREE from '../dist/vendor/three.module.js';
import {auditScene,placementFindings} from '../tools/west-placement-audit.mjs';
import {WEST_BUILDINGS,WEST_ROADS,WEST_PATHS,WEST_SPAWN} from '../dist/west-edge-layout.js';
import {westKerbPlacements} from '../dist/west-details.js';
import {westPathEdges,westPathPatches} from '../dist/west-edge-scene.js';
import {CameraRig,groundHeight} from '../dist/spatial.js';
import {CollisionIndex,CityNavigation} from '../dist/navigation.js';
import {streetSurface} from '../dist/street-layout.js';

const {world,scene,trees}=auditScene(),index=new CollisionIndex(world.colliders);
const walk=(x,z,r=.35)=>!index.blocked(x,z,r),supported=(x,z,r)=>walk(x,z,r)&&(!!streetSurface(x,z)||WEST_PATHS.some(p=>Math.abs(x-p.x)<=p.w/2&&Math.abs(z-p.z)<=p.d/2));
const findings=placementFindings({world});

test('the full western scene has no building penetrations, blocked fronts or solid road obstacles',()=>{
 assert.deepEqual(findings.overlaps,[]);assert.deepEqual(findings.propOverlaps,[]);assert.deepEqual(findings.roadObstacles,[]);assert.deepEqual(findings.blockedDoors,[]);
 for(let i=0;i<WEST_BUILDINGS.length;i++)for(const b of WEST_BUILDINGS.slice(i+1)){const a=WEST_BUILDINGS[i];assert.ok(Math.abs(a.x-b.x)>=(a.w+b.w)/2+2-1e-6||Math.abs(a.z-b.z)>=(a.d+b.d)/2+2-1e-6,a.id+' cramped against '+b.id);}
});
test('each of the 41 closed-building fronts connects to the western street network',()=>{
 const nav=new CityNavigation(supported,1,{minX:-550,maxX:-260,minZ:-125,maxZ:125});
 for(const b of WEST_BUILDINGS.filter(b=>b.style!=='shelter')){const p={x:b.x,z:b.z+b.face*(b.d/2+1.1)};assert.ok(supported(p.x,p.z,.35),'unsupported '+b.id);assert.ok(nav.find(WEST_SPAWN,p).length>1,'disconnected '+b.id);}
});
test('individual kerbstones never cross any of the six western road footprints',()=>{
 assert.equal(findings.junctionKerbs.length,0);
 for(const p of westKerbPlacements())for(const r of WEST_ROADS){const dx=Math.min(p.x+p.w/2,r.x+r.w/2)-Math.max(p.x-p.w/2,r.x-r.w/2),dz=Math.min(p.z+p.d/2,r.z+r.d/2)-Math.max(p.z-p.d/2,r.z-r.d/2);assert.ok(dx<=.001||dz<=.001);}
});
test('path meshes stay outside building bodies and skirts close only actual level changes',()=>{
 for(const p of westPathPatches())for(const b of WEST_BUILDINGS.filter(b=>b.style!=='shelter')){const dx=Math.min(p.x+p.w/2,b.x+b.w/2)-Math.max(p.x-p.w/2,b.x-b.w/2),dz=Math.min(p.z+p.d/2,b.z+b.d/2)-Math.max(p.z-p.d/2,b.z-b.d/2);assert.ok(dx<.001||dz<.001,p.id+' under '+b.id);}
 for(const e of westPathEdges()){const x=(e.a.x+e.b.x)/2,z=(e.a.z+e.b.z)/2,neighbor=groundHeight(x+(e.alongX?0:e.side*.025),z+(e.alongX?e.side*.025:0));assert.ok((e.a.y+e.b.y)/2>neighbor+.035);assert.ok(e.bottom<=neighbor);}
});
test('playground exits and the rear-home gate are open across their entire centreline',()=>{
 for(let x=-497;x<=-487;x+=.2)assert.ok(walk(x,49,.35),'east play exit '+x);
 for(let z=59.2;z<=69.5;z+=.2)assert.ok(walk(-514,z,.35),'rear gate '+z);
});
test('the wagon wheels sit on the actual track gauge and the slide meets its landing',()=>{
 const wheels=[];let slide;world.exterior.traverse(m=>{if(m.userData.westRailWheel)wheels.push(m);if(m.userData.westSlide)slide=m;});
 assert.equal(wheels.length,4);for(const w of wheels){assert.ok(Math.abs(Math.abs(w.position.z+55)-.72)<1e-5);const bounds=new THREE.Box3().setFromObject(w);assert.ok(Math.abs(bounds.min.y-.0925)<.005);}
 const tip=new THREE.Vector3(0,.5,.5).applyMatrix4(slide.matrixWorld);assert.ok(Math.abs(tip.y-.148)<.002);
});
test('shrub roots, tires and pipes have physical support instead of offset floating pieces',()=>{
 const meshes=[],roots=[],pipes=[],tires=[];world.exterior.traverse(o=>{if(o.isMesh)meshes.push(o);if(o.userData.westRoot)roots.push(o);if(o.userData.westPipe)pipes.push(o);if(o.userData.westStackTire)tires.push(o);});
 for(const root of roots){const p=root.getWorldPosition(new THREE.Vector3());if(p.y>2)continue;const own=new Set();root.traverse(o=>own.add(o));const ray=new THREE.Raycaster(new THREE.Vector3(p.x,p.y+.002,p.z),new THREE.Vector3(0,-1,0)),hit=ray.intersectObjects(meshes,false).find(h=>!own.has(h.object));assert.ok(p.y-Math.max(groundHeight(p.x,p.z),hit?.point.y??-Infinity)<.015,'floating shrub '+JSON.stringify(p));}
 const bounds=tires.map(t=>new THREE.Box3().setFromObject(t)).sort((a,b)=>a.min.y-b.min.y);assert.equal(bounds.length,4);assert.ok(Math.abs(bounds[0].min.y-groundHeight(-357,-60))<.002);for(let n=1;n<4;n++)assert.ok(Math.abs(bounds[n].min.y-bounds[n-1].max.y)<.002);
 assert.equal(pipes.length,3);for(const p of pipes.slice(0,2))assert.ok(Math.abs(p.position.distanceTo(pipes[2].position)-1.33)<.002);
 assert.deepEqual(findings.floating,[]);
});
test('porch and sleeping-shelter orbits retain third person with the real overhead colliders',()=>{
 const bodies=[...world.colliders,...world.overheadColliders];for(const p of [{x:-511,y:.09,z:27},{x:-509.5,y:.09,z:-12},{x:-533,y:.09,z:42.6},{x:-296,y:.25,z:-15}]){
  const shelter=scene.cameraShelter(p);assert.ok(shelter,'missing canopy '+JSON.stringify(p));const rig=new CameraRig();for(let n=0;n<64;n++){const view=rig.update(p,n*Math.PI/32,.22,8,1/30,bodies,null,shelter);assert.ok(view.distance>=2.15,'camera collapsed '+JSON.stringify(p));}
 }
});
test('garage roofs and embankment ends no longer have tall invisible camera walls',()=>{
 const shed=world.colliders.find(c=>c.x===-465&&c.z===-33);assert.ok(shed.h<3.4);
 const banks=world.colliders.filter(c=>c.westBank);assert.equal(banks.length,24);assert.ok(banks.filter(c=>Math.abs(c.z)>54).every(c=>c.h<.5));
 const nearby=[...world.colliders,...world.overheadColliders].filter(c=>c.westBank||c.westRetainingWall),rig=new CameraRig();for(let n=0;n<32;n++){const view=rig.update({x:-428,y:-.05,z:57},n*Math.PI/16,.22,8,1/30,nearby);assert.ok(view.distance>7.8,'invisible bank wall truncated orbit');}
});
