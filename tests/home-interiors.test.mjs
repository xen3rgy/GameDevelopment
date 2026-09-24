import test from 'node:test';
import assert from 'node:assert/strict';
import {ROOMS,canWalkRoom,CameraRig,homeFixtures} from '../dist/spatial.js';
import {HOME_POINTS,HOMES} from '../dist/data.js';
import {homeLocation,outsidePosition} from '../dist/housing.js';
import {newGame,GameModel,validateSave} from '../dist/model.js';
import * as THREE from '../dist/vendor/three.module.js';
import {HomeScene} from '../dist/home-scene.js';

const homes=['stationRoom','room','flat','penthouse'];
const walk=(home,x,z)=>canWalkRoom('home',x,z,.35,home);
function reachable(home){
 const start=ROOMS.home.spawn,queue=[{...start}],seen=new Set(['0,0']),step=.15;
 for(let n=0;n<queue.length;n++){
  const p=queue[n],ix=Math.round((p.x-start.x)/step),iz=Math.round((p.z-start.z)/step);
  for(const [dx,dz] of [[1,0],[-1,0],[0,1],[0,-1]]){
   const key=`${ix+dx},${iz+dz}`,x=start.x+(ix+dx)*step,z=start.z+(iz+dz)*step;
   if(!seen.has(key)&&walk(home,x,z)){seen.add(key);queue.push({x,z});}
  }
 }
 return queue;
}
for(const home of homes){
 test(`${home}: all interaction anchors reachable from entry`,()=>{
  assert.ok(walk(home,ROOMS.home.spawn.x,ROOMS.home.spawn.z),'entry blocked');
  const points=reachable(home);
  for(const [id,p] of Object.entries(HOME_POINTS)){
   assert.ok(walk(home,p.x,p.z),`${id} inside fixture`);
   assert.ok(points.some(q=>Math.hypot(q.x-p.x,q.z-p.z)<.16),`${id} unreachable`);
  }
  for(const fixture of homeFixtures(home).filter(f=>(f.minY??0)<1.85))assert.ok(!walk(home,fixture.x,fixture.z),`${fixture.id} missing collision`);
 });
 test(`${home}: 360 degree camera and zoom stay below ceiling and inside walls`,()=>{
  const room=ROOMS.home,fixtures=homeFixtures(home);
  const corners=[room.minX+.4,room.maxX-.4].flatMap(x=>[room.minZ+.4,room.maxZ-.4].map(z=>({x,z})));
  const positions=[room.spawn,...Object.values(HOME_POINTS),...corners].filter(p=>walk(home,p.x,p.z));
  for(const p of positions)for(const pitch of [-.25,.06,.35,1.1])for(const zoom of [2,4.3,12]){
   const rig=new CameraRig();
   for(let i=0;i<=72;i++){
    const result=rig.update({...p,y:.07},i*Math.PI/36,pitch,zoom,1/30,fixtures,room),v=result.position;
    assert.ok(Object.values(v).every(Number.isFinite),'non-finite camera');
    assert.ok(v.y<=room.ceiling-.19,`camera crosses ceiling at ${JSON.stringify(p)}`);
    assert.ok(v.y>=.19,'camera crosses floor');
    assert.ok(v.x>=room.minX+.1&&v.x<=room.maxX-.1&&v.z>=room.minZ+.1&&v.z<=room.maxZ-.1,'camera crosses wall');
    assert.ok(result.distance>0,'camera boom trapped');
   }
  }
 });
 test(`${home}: entry, bed, shower, exit and save preserve household state`,()=>{
  const model=new GameModel(newGame()),s=model.s;
  s.home=home;s.rentDue=9;s.storage=[{id:'water',count:2}];s.fridge=[{id:'water',count:1}];
  const loc=homeLocation(s);s.position={x:loc.x,z:loc.z};
  assert.equal(model.enterInterior('home'),true);assert.equal(s.interior,'home');
  assert.deepEqual(s.position,ROOMS.home.spawn);
  s.position={x:HOME_POINTS.bed.x,z:HOME_POINTS.bed.z};
  assert.equal(model.beginLifeAction('sleep',8),true);model.cancelLifeAction();model.tickLifeAction(2);
  s.position={x:HOME_POINTS.shower.x,z:HOME_POINTS.shower.z};
  assert.equal(model.beginLifeAction('shower'),true);model.cancelLifeAction();model.tickLifeAction(2);
  const saved=validateSave(JSON.parse(JSON.stringify(s)));
  assert.equal(saved.home,home);assert.equal(saved.rentDue,9);
  assert.deepEqual(saved.storage,s.storage);assert.deepEqual(saved.fridge,s.fridge);
  assert.equal(HOMES.find(h=>h.id===saved.home).rent,HOMES.find(h=>h.id===home).rent);
  const loaded=new GameModel(saved);loaded.s.position={x:HOME_POINTS.exit.x,z:HOME_POINTS.exit.z};
  const outside=outsidePosition(loaded.s);assert.equal(loaded.leaveInterior(),true);
  assert.equal(loaded.s.interior,null);assert.deepEqual(loaded.s.position,outside);
 });
}
test('constructed home variants have solid ceilings and stable shower camera anchors',()=>{
 const mat=color=>new THREE.MeshStandardMaterial({color});
 const mesh=(g,x,y,z,geometry,color,material)=>{const m=new THREE.Mesh(geometry,material||mat(color));m.position.set(x,y,z);g.add(m);return m;};
 const kit={mat,
  box:(g,x,y,z,w,h,d,c,m)=>mesh(g,x,y,z,new THREE.BoxGeometry(w,h,d),c,m),
  cylinder:(g,x,y,z,r,h,c)=>mesh(g,x,y,z,new THREE.CylinderGeometry(r,r,h,8),c),
  sphere:(g,x,y,z,r,c)=>mesh(g,x,y,z,new THREE.SphereGeometry(r,8,6),c),
  sign:(g,text,x,y,z)=>{const p=new THREE.Group();p.position.set(x,y,z);g.add(p);return p;}
 };
 const player=new THREE.Group();player.userData={arms:[new THREE.Group(),new THREE.Group()],elbows:[new THREE.Group(),new THREE.Group()],upper:new THREE.Group()};
 const world={scene:new THREE.Scene(),homeShell:new THREE.Group(),player,interiorLight:new THREE.PointLight(),batchStaticGroup:()=>{}};
 world.scene.add(world.homeShell,player);
 const scene=new HomeScene(world,kit);
 for(const home of homes){
  const state={home,interior:'home',fridge:[],dailyLife:{action:null}};
  scene.update(state,1/60);
  for(const [id,group] of scene.variants)assert.equal(group.visible,id===home);
  const selected=scene.variants.get(home);let ceiling=false;
  selected.traverse(o=>{
   if(o.geometry?.type==='BoxGeometry'){
    const p=o.geometry.parameters;
    if(p.width>=13.5&&p.depth>=11.5&&Math.abs(o.position.y-p.height/2-ROOMS.home.ceiling)<1e-6)ceiling=true;
   }
  });
  assert.ok(ceiling,`${home} ceiling missing or disagrees with camera limit`);
  const origin={x:HOME_POINTS.shower.x,z:HOME_POINTS.shower.z};
  for(const elapsed of [0,1,3.5,6,7]){
   state.dailyLife.action={kind:'shower',elapsed,duration:7,origin,yaw:Math.PI};scene.update(state,1/60);
   assert.equal(scene.cameraAnchor.x,origin.x);assert.equal(scene.cameraAnchor.z,origin.z);
   assert.ok(walk(home,scene.cameraAnchor.x,scene.cameraAnchor.z),'shower camera anchor enters enclosure');
  }
  world.scene.updateMatrixWorld(true);
  world.scene.traverse(o=>assert.ok(o.matrixWorld.elements.every(Number.isFinite),'non-finite scene transform'));
 }
});
