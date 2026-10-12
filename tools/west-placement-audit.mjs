import * as THREE from '../dist/vendor/three.module.js';
import {WestEdgeScene,westPathPatches} from '../dist/west-edge-scene.js';
import {WEST_BUILDINGS,WEST_BUILDING_FIXTURES,WEST_ROADS,WEST_PATHS} from '../dist/west-edge-layout.js';
import {streetSurface} from '../dist/street-layout.js';
import {groundHeight} from '../dist/spatial.js';

// Inspect only the western scene, without a browser or the rest of the city.
export function auditScene(){
 const mats=new Map(),boxGeo=new THREE.BoxGeometry(),cylGeo=new THREE.CylinderGeometry(1,1,1,8),sphereGeo=new THREE.IcosahedronGeometry(1,1),records=[],trees=[];
 const kit={mat(c){if(!mats.has(c))mats.set(c,new THREE.MeshStandardMaterial({color:c}));return mats.get(c);}};
 const shape=(geo,p,x,y,z,w,h,d,c,m)=>{const q=new THREE.Mesh(geo,m||kit.mat(c));q.position.set(x,y,z);q.scale.set(w,h,d);p.add(q);records.push(q);return q;};
 kit.box=(p,x,y,z,w,h,d,c,m)=>shape(boxGeo,p,x,y,z,w,h,d,c,m);
 kit.cylinder=(p,x,y,z,r,h,c)=>shape(cylGeo,p,x,y,z,r,h,r,c);
 kit.sphere=(p,x,y,z,r,c)=>shape(sphereGeo,p,x,y,z,r,r,r,c);
 kit.sign=(p,text,x,y,z,w,h,c,b,angle=0)=>{const q=kit.box(p,x,y,z,w,h,.01,0xffffff);q.rotation.y=angle;q.userData.auditSign=text;return q;};
 const world={exterior:new THREE.Group(),art:{windows:[]},colliders:[],overheadColliders:[],tree(x,z,r,options){trees.push({x,z,r,options});},batchStaticGroup(){},atmosphere:{registerLamp(){},addPuddle(){}}};
 const scene=new WestEdgeScene(world,kit,{batch:false});world.exterior.updateMatrixWorld(true);
 return {world,scene,records,trees};
}
export function placementFindings({world}){
 const overlaps=[],propOverlaps=[],tightBuildings=[],roadObstacles=[],junctionKerbs=[],blockedDoors=[],floating=[];
 const overlap=(a,b)=>({x:Math.min(a.x+a.w,b.x+b.w)-Math.max(a.x-a.w,b.x-b.w),z:Math.min(a.z+a.d,b.z+b.d)-Math.max(a.z-a.d,b.z-b.d)});
 const buildingFor=c=>WEST_BUILDINGS.find(b=>Math.abs(c.x-b.x)<.001&&Math.abs(c.z-b.z)<.001&&Math.abs(c.w-b.w/2-.35)<.001);
 for(let i=0;i<WEST_BUILDINGS.length;i++)for(const b of WEST_BUILDINGS.slice(i+1)){
  const a=WEST_BUILDINGS[i],dx=Math.abs(a.x-b.x)-(a.w+b.w)/2,dz=Math.abs(a.z-b.z)-(a.d+b.d)/2;
  if(dx<.9&&dz<.9)tightBuildings.push({a:a.id,b:b.id,dx:+dx.toFixed(2),dz:+dz.toFixed(2)});
 }
 for(const [i,c] of world.colliders.entries()){
  if(buildingFor(c)||c.westTree)continue;
  for(const b of WEST_BUILDINGS.filter(b=>b.style!=='shelter')){const o=overlap(c,{...b,w:b.w/2,d:b.d/2});if(o.x>.08&&o.z>.08)overlaps.push({collider:i,x:c.x,z:c.z,w:c.w,d:c.d,building:b.id,depth:[+o.x.toFixed(2),+o.z.toFixed(2)]});}
  for(const r of WEST_ROADS){const o=overlap(c,{...r,w:r.w/2,d:r.d/2});if(o.x>.04&&o.z>.04)roadObstacles.push({collider:i,x:c.x,z:c.z,road:r.name,depth:[+o.x.toFixed(2),+o.z.toFixed(2)]});}
 }
 for(const b of WEST_BUILDINGS.filter(b=>b.style!=='shelter')){const x=b.x,z=b.z+b.face*(b.d/2+1.0);const blocking=world.colliders.filter(c=>Math.abs(x-c.x)<c.w+.35&&Math.abs(z-c.z)<c.d+.35);if(blocking.length)blockedDoors.push({id:b.id,x,z,blocking:blocking.map(c=>({x:c.x,z:c.z,w:c.w,d:c.d}))});}
 const props=world.colliders.filter(c=>!buildingFor(c)&&!c.westTree&&c.w>.18&&c.d>.18);
 for(let i=0;i<props.length;i++)for(const b of props.slice(i+1)){const a=props[i];if(a.parkedScenery&&b.parkedScenery)continue;const o=overlap(a,b);if(o.x>.15&&o.z>.15)propOverlaps.push({a:{x:a.x,z:a.z},b:{x:b.x,z:b.z},depth:[o.x,o.z]});}
 const meshes=[];world.exterior.traverse(m=>{if(m.isMesh)meshes.push(m);});
 for(const m of meshes){const box=new THREE.Box3().setFromObject(m),size=box.getSize(new THREE.Vector3()),center=box.getCenter(new THREE.Vector3());
  if(m.material.name==='West concrete'&&size.y>.12&&size.y<.2&&Math.min(size.x,size.z)<.22&&Math.max(size.x,size.z)<1.02&&streetSurface(center.x,center.z)?.kind==='road')junctionKerbs.push({x:+center.x.toFixed(2),z:+center.z.toFixed(2)});
  let attached=false;for(let p=m.parent;p;p=p.parent)if(p.userData.westRoot||p.userData.wheels||p.userData.westAssembly)attached=true;
  if(attached||box.min.y<.03||box.min.y>1.5||size.y<.3||size.x<.3||size.z<.3||m.material.transparent||m.userData.auditSign)continue;
  const y=groundHeight(center.x,center.z);if(box.min.y-y<.065)continue;
  // Ground props with a gap below every sampled corner need a placement review.
  const support=[];for(const [dx,dz] of [[0,0],[-.35,-.35],[-.35,.35],[.35,-.35],[.35,.35]]){const x=center.x+dx*size.x,z=center.z+dz*size.z,ray=new THREE.Raycaster(new THREE.Vector3(x,box.min.y+.003,z),new THREE.Vector3(0,-1,0),0,1.6),hit=ray.intersectObjects(meshes,false).find(h=>h.object!==m);support.push(box.min.y-Math.max(groundHeight(x,z),hit?.point.y??-Infinity));}
  if(Math.min(...support)>.065)floating.push({x:+center.x.toFixed(2),z:+center.z.toFixed(2),base:+box.min.y.toFixed(3),gap:+Math.min(...support).toFixed(3),size:[size.x,size.y,size.z].map(n=>+n.toFixed(2)),material:m.material.name});
 }
 return {buildings:WEST_BUILDINGS.length,colliders:world.colliders.length,pathPatches:westPathPatches().length,tightBuildings,overlaps,propOverlaps,roadObstacles,junctionKerbs,blockedDoors,floating};
}
if(process.argv[1]?.endsWith('west-placement-audit.mjs'))console.log(JSON.stringify(placementFindings(auditScene()),null,2));
