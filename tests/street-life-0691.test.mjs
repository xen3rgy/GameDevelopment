import test from 'node:test';
import assert from 'node:assert/strict';
import * as THREE from '../dist/vendor/three.module.js';
import {createCitizen} from '../dist/art.js';
import {animateCitizen} from '../dist/animation.js';
import {seatedStreetPose} from '../dist/pedestrian-scene.js';
import {attachStreetProps,animateStreetIdle,resetStreetArms,streetIdlePose} from '../dist/street-idle.js';
import {COURTYARD_SEATS,PROMENADE_FIXTURES} from '../dist/pedestrian-layout.js';
import {CITY_CHARACTER_FIXTURES,cityLandmarkAt} from '../dist/city-character-layout.js';
import {NEIGHBORHOOD_FIXTURES} from '../dist/neighborhood-layout.js';
import {BUILDINGS} from '../dist/data.js';
import {DISTRICT_FIXTURES,exteriorContains} from '../dist/city-layout.js';
import {PedestrianLife} from '../dist/pedestrian-life.js';
import {selectLamps} from '../dist/lighting.js';
function kit(){
 const materials=new Map(),geometry=new THREE.BoxGeometry();
 const k={mat:c=>{if(!materials.has(c))materials.set(c,new THREE.MeshStandardMaterial({color:c}));return materials.get(c);}};
 k.box=(p,x,y,z,w,h,d,c,m)=>{const v=new THREE.Mesh(geometry,m||k.mat(c));v.position.set(x,y,z);v.scale.set(w,h,d);p.add(v);return v;};
 k.cylinder=(p,x,y,z,r,h,c)=>{const v=new THREE.Mesh(new THREE.CylinderGeometry(1,1,1,8),k.mat(c));v.position.set(x,y,z);v.scale.set(r,h,r);p.add(v);return v;};
 k.sphere=(p,x,y,z,r,c)=>{const v=new THREE.Mesh(new THREE.IcosahedronGeometry(1,1),k.mat(c));v.position.set(x,y,z);v.scale.setScalar(r);p.add(v);return v;};
 k.sign=(p,text,x,y,z,w,h,c,b,angle=0)=>{const v=k.box(p,x,y,z,w,h,.001,0xffffff);v.name=text;v.rotation.y=angle;return v;};return k;
}

test('all four Lichthof seats have connected approaches, face the aisle and reserve individually',()=>{
 const blocks=BUILDINGS.map(([x,z,w,d])=>({x,z,w:w/2+.35,d:d/2+.35})).concat(DISTRICT_FIXTURES,NEIGHBORHOOD_FIXTURES,PROMENADE_FIXTURES,CITY_CHARACTER_FIXTURES);
 const free=(x,z,r=.4)=>exteriorContains(x,z,r)&&!blocks.some(b=>Math.abs(x-b.x)<b.w+r&&Math.abs(z-b.z)<b.d+r);
 const life=new PedestrianLife(free,{count:0});assert.equal(COURTYARD_SEATS.length,4);
 for(const d of COURTYARD_SEATS){
  assert.ok(life.destinations.some(p=>p.id===d.id));assert.ok(life.nav.find({x:31,z:12.6},d).length>1,d.id);
  assert.ok(d.x>52&&d.x<58);assert.ok(Math.abs(Math.hypot(d.x-d.seat.x,d.z-d.seat.z)-1.12)<1e-8);
 }
 assert.equal(new Set(COURTYARD_SEATS.map(d=>d.id)).size,4);
});
test('drinking and phone gestures retain hand contact, stay seated and clear IK before walking',()=>{
 for(const id of [0,1])for(const scale of [.94,1.054]){
  const actor=createCitizen(kit(),0x667777,0xc39b7a,1,{hair:'short',accent:0x887766});actor.scale.set(scale*1.045,scale,scale);attachStreetProps(actor,kit());
  const p={id,phase:'seated',age:0,wait:80,goal:{kind:'seat'}};
  for(let t=2;t<20;t+=.13){p.age=t;resetStreetArms(actor);animateCitizen(actor,.13,0,{npc:true});seatedStreetPose(actor,1,.5575);animateStreetIdle(actor,p,1);actor.updateMatrixWorld(true);
   const props=actor.userData.streetProps,object=id===0?props.cup:props.phone;
   const hand=actor.userData.elbows[1].localToWorld(new THREE.Vector3(0,-.22,.026)),grip=object.localToWorld(new THREE.Vector3(id===0?.064:.042,-.01,0));
   assert.ok(hand.distanceTo(grip)<.015,`hand gap ${hand.distanceTo(grip)}`);
   assert.ok(Math.abs((actor.userData.upper.position.y-.235)*scale-.5575)<1e-8);
   assert.deepEqual(actor.position.toArray(),[0,0,0]);
  }
  p.phase='walk';resetStreetArms(actor);animateCitizen(actor,.1,.1,{npc:true});animateStreetIdle(actor,p,1);
  assert.equal(actor.userData.streetProps.root.visible,false);
  assert.equal(actor.userData.arms[1].rotation.y,0);assert.equal(actor.userData.elbows[1].rotation.z,0);
 }
});
test('gestures finish before departure at every speed and do not run while browsing or walking',()=>{
 for(const speed of [1,4,10]){
  const p={id:0,phase:'seated',age:4,wait:30*speed};assert.equal(streetIdlePose(p,speed).weight,1);
  p.wait=0;assert.equal(streetIdlePose(p,speed).weight,0);
  p.wait=.2*speed;assert.ok(streetIdlePose(p,speed).weight<.1);
  p.phase='walk';assert.equal(streetIdlePose(p,speed).kind,null);
  p.phase='wait';p.goal={kind:'browse'};assert.equal(streetIdlePose(p,speed).kind,null);
 }
});
test('accent lights cannot displace nearby street coverage at either quality setting',()=>{
 const roads=[4,8,12,16].map(x=>({x,z:0,kind:'street'})),accents=Array.from({length:20},(_,i)=>({x:i*.1,z:0,kind:'accent',distance:14}));
 for(const count of [4,8]){
  const lights=selectLamps([...accents,...roads],{x:0,z:0},[],count);
  assert.equal(lights.length,count);assert.equal(new Set(lights).size,count);
  assert.ok(lights.filter(l=>l.kind==='street').length>=count/2);
 }
 assert.equal(selectLamps(accents,{x:40,z:0},[],4).length,0);
 assert.equal(cityLandmarkAt({x:55,z:33}),'LICHTHOF');assert.equal(cityLandmarkAt({x:55,z:0}),null);
});
