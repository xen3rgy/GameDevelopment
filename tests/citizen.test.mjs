import test from 'node:test';
import assert from 'node:assert/strict';
import * as THREE from '../dist/vendor/three.module.js';
import {createCitizen,citizenAppearance} from '../dist/citizen.js';
import {animateCitizen,playGesture} from '../dist/animation.js';
import {seatedStreetPose} from '../dist/pedestrian-scene.js';
import {poseCafeSeated} from '../dist/cafe-interior.js';

test('all archetypes keep articulated hand/foot anchors and grounded soles',()=>{
 for(let id=0;id<24;id++){
  const look=citizenAppearance(id),a=createCitizen({},look.color,look.skin,id+1,look),d=a.userData;
  for(const k of ['arms','elbows','legs','knees','feet'])assert.equal(d[k].length,2);
  assert.equal(d.headRoot.parent,d.upper);assert.equal(d.backpack.parent,d.upper);
  for(let i=0;i<2;i++){assert.equal(d.elbows[i].parent,d.arms[i]);assert.equal(d.knees[i].parent,d.legs[i]);assert.equal(d.feet[i].parent,d.knees[i]);assert.equal(d.elbows[i].position.y,-.29);assert.equal(d.knees[i].position.y,-.38);assert.equal(d.feet[i].position.y,-.405);}
  a.updateMatrixWorld(true);for(const foot of d.feet){const b=new THREE.Box3().setFromObject(foot);assert.ok(Math.abs(b.min.y-.048)<.006);}
  const geometries=[];a.traverse(m=>{if(m.isMesh)geometries.push(m.geometry);});
  for(const options of [{},{running:true},{npc:true},{carrying:true},{basket:true},{riding:true},{entering:true}])for(let f=0;f<30;f++)animateCitizen(a,1/60,.03,options);
  for(const kind of ['shop','pickup','cafeServe','cafeDrink']){playGesture(a,kind);animateCitizen(a,.2,.01);}
  seatedStreetPose(a,1,.46);poseCafeSeated(a,1,true);a.updateMatrixWorld(true);
  let i=0;a.traverse(o=>{assert.ok(o.matrixWorld.elements.every(Number.isFinite));if(o.isMesh)assert.equal(o.geometry,geometries[i++]);});
 }
});
test('identity, independent skin variety, and silhouette combinations are stable',()=>{
 const sets=new Map();for(let id=0;id<128;id++){const a=citizenAppearance(id);assert.deepEqual(a,citizenAppearance(id));if(!sets.has(a.archetype))sets.set(a.archetype,new Set());sets.get(a.archetype).add(a.skin);}
 assert.equal(sets.size,8);for(const skins of sets.values())assert.ok(skins.size>=4);
});
test('player and population stay within mesh/triangle budgets and share materials',()=>{
 let shared=null;for(let id=0;id<9;id++){
  const a=id?createCitizen({},undefined,undefined,id,citizenAppearance(id-1)):createCitizen({});let draws=0,triangles=0;
  a.traverse(m=>{if(m.isMesh){draws++;triangles+=(m.geometry.index?.count??m.geometry.attributes.position.count)/3;assert.equal(m.material.metalness,0);}});
  assert.ok(draws<=48,`${draws} draws`);assert.ok(triangles<8500,`${triangles} triangles`);
  if(!id){shared=a.userData.feet[0].children[0].material;const b=createCitizen({});assert.equal(shared,b.userData.feet[0].children[0].material);assert.ok(a.userData.backpack.children.length);}
 }
});
