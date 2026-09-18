import test from 'node:test';
import assert from 'node:assert/strict';
import * as THREE from '../dist/vendor/three.module.js';
import {createCitizen,createCar} from '../dist/art.js';
import {animateCitizen,playGesture} from '../dist/animation.js';
import {VehicleLights} from '../dist/vehicle-lights.js';
import {newGame,validateSave} from '../dist/model.js';
const materials=new Map();const mat=(color=0xffffff)=>{if(!materials.has(color))materials.set(color,new THREE.MeshStandardMaterial({color}));return materials.get(color)};
const kit={mat,box(parent,x,y,z,w,h,d,color,material){const m=new THREE.Mesh(new THREE.BoxGeometry(w,h,d),material||mat(color));m.position.set(x,y,z);parent.add(m);return m},cylinder(parent,x,y,z,r,h,color){const m=new THREE.Mesh(new THREE.CylinderGeometry(r,r,h,8),mat(color));m.position.set(x,y,z);parent.add(m);return m},sign(){return new THREE.Group()}};
test('new character batching retains animated joints and the established foot origin',()=>{const actor=createCitizen(kit);const bounds=new THREE.Box3().setFromObject(actor);assert.ok(Math.abs(bounds.min.y-.05)<.001);assert.ok(bounds.max.y<1.95);assert.equal(actor.userData.legs.length,2);assert.equal(actor.userData.arms.length,2);for(const joint of [...actor.userData.legs,...actor.userData.arms]){assert.ok(joint.parent===(actor.userData.arms.includes(joint)?actor.userData.upper:actor));assert.ok(joint.children.length>0)}let count=0;actor.traverse(m=>{if(m.isMesh){count++;assert.ok(Array.from(m.geometry.attributes.position.array).every(Number.isFinite));assert.equal(m.geometry.attributes.normal.count,m.geometry.attributes.position.count)}});assert.ok(count<30,'merged character stays below 30 draw calls');actor.userData.legs[0].rotation.x=.4;assert.doesNotThrow(()=>actor.updateMatrixWorld(true))});
test('all new car bodies keep existing wheel contact and finite render geometry',()=>{for(const id of ['car','sport','van']){const car=createCar(kit,id);const bounds=new THREE.Box3().setFromObject(car);assert.ok(Math.abs(bounds.min.y-.03)<.001,id);assert.ok(bounds.max.x<1.15&&bounds.min.x>-1.15,id);car.traverse(m=>{if(m.isMesh)assert.ok(Array.from(m.geometry.attributes.position.array).every(Number.isFinite))})}});
test('0.3 saves receive view defaults while progress and inventories remain intact',()=>{const old=newGame(true);delete old.settings.fov;delete old.settings.brightness;delete old.settings.sensitivity;old.inventory=[{id:'water',count:3}];old.money=12345;const loaded=validateSave(old);assert.equal(loaded.version,3);assert.equal(loaded.money,12345);assert.deepEqual(loaded.inventory,old.inventory);assert.equal(loaded.settings.fov,55);assert.equal(loaded.settings.brightness,1);assert.equal(loaded.settings.sensitivity,1);loaded.settings.fov=120;loaded.settings.sensitivity=NaN;loaded.settings.brightness=-1;const safe=validateSave(loaded);assert.equal(safe.settings.fov,75);assert.equal(safe.settings.sensitivity,1);assert.equal(safe.settings.brightness,.7)});

test('headlight beams face forward and dip onto the road in both traffic directions',()=>{
 for(const angle of [0,Math.PI/2,-Math.PI/2,Math.PI]){
  const scene=new THREE.Scene(),car=createCar(kit,'van');scene.add(car);car.position.set(20,0,-3);car.rotation.y=angle;
  const pool=new VehicleLights(scene);for(let i=0;i<3;i++)pool.update([car],null,car.position,1,.1);
  const slot=pool.slots.find(s=>s.car===car);assert.ok(slot);assert.equal(slot.lights.length,2);
  for(const light of slot.lights){const local=car.worldToLocal(light.position.clone()),target=car.worldToLocal(light.target.position.clone());assert.ok(local.z>2.4,'origin outside body');assert.ok(target.z>local.z+11);assert.ok(target.y<local.y);assert.ok(light.intensity>100);}
  const first=slot.lights[0].position.clone();car.position.x+=4;pool.update([car],null,car.position,1,.1);assert.ok(Math.abs(slot.lights[0].position.x-first.x-4)<1e-8);
  pool.update([car],null,car.position,0,.1);assert.ok(pool.slots.every(s=>s.lights.every(l=>l.intensity===0)));
 }
});
test('vehicle light pool prioritizes driving, disables indoor lights and keeps the low quality budget',()=>{
 const scene=new THREE.Scene(),cars=Array.from({length:7},(_,i)=>{const c=createCar(kit);c.position.x=i*6;scene.add(c);return c}),owned=createCar(kit,'sport');scene.add(owned);owned.position.x=40;const pool=new VehicleLights(scene),observer=new THREE.Vector3();
 for(let i=0;i<4;i++)pool.update(cars,owned,observer,1,.1,false,'low',true);assert.equal(pool.slots.filter(s=>s.lights.some(l=>l.intensity>0)).length,1);assert.ok(pool.slots.some(s=>s.car===owned));assert.ok(owned.userData.vehicleLights.headlights.emissiveIntensity>4);
 for(let i=0;i<6;i++)pool.update(cars,owned,observer,1,.1,false,'high',false);assert.ok(pool.slots.every(s=>s.car!==owned));assert.equal(owned.userData.vehicleLights.headlights.emissiveIntensity,.15);assert.equal(pool.slots.flatMap(s=>s.lights).length,4);
 pool.update(cars,owned,observer,1,.1,true);assert.ok(pool.slots.every(s=>s.lights.every(l=>l.intensity===0)));assert.equal(cars[0].userData.vehicleLights.taillights.emissiveIntensity,.1);
});

test('animated feet remain above ground and carrying/reaching leaves the camera root unchanged',()=>{
 const actor=createCitizen(kit);for(let i=0;i<180;i++){animateCitizen(actor,1/60,.09,{running:i>90,carrying:i<60});actor.updateMatrixWorld(true);for(const foot of actor.userData.feet){const b=new THREE.Box3().setFromObject(foot,true);assert.ok(b.min.y>=.049,`foot ${b.min.y}`)}assert.equal(actor.position.y,0)}
 playGesture(actor,'shop');for(let i=0;i<60;i++)animateCitizen(actor,1/60,0,{basket:true});assert.equal(actor.userData.gesture,null);assert.equal(actor.position.y,0);assert.ok(Math.abs(actor.userData.legs[0].rotation.x)<.01);
});
