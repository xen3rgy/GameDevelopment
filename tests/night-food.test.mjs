import test from 'node:test';
import assert from 'node:assert/strict';
import * as THREE from '../dist/vendor/three.module.js';
import {createBread,createProduce} from '../dist/food-models.js';
import {lightingAt,selectLamps,STREET_LAMPS,streetLampHead} from '../dist/lighting.js';

test('bread remains shelf-sized rather than overlapping adjacent loaves or rows',()=>{
 for(let i=0;i<9;i++){const loaf=createBread(i),b=new THREE.Box3().setFromObject(loaf),size=b.getSize(new THREE.Vector3());assert.ok(size.x<.30,`width ${size.x}`);assert.ok(size.y<.22,`height ${size.y}`);assert.ok(size.z<.49,`depth ${size.z}`);assert.ok(b.min.y>=0);assert.ok(size.x>.20&&size.z>.40)}
});
test('all produce including stems and leaves fits the crate cells without oversized scaling',()=>{
 for(const type of ['tomato','apple','pepper'])for(let i=0;i<18;i++){const fruit=createProduce(type,i);fruit.rotation.y=i*.71;const b=new THREE.Box3().setFromObject(fruit,true),size=b.getSize(new THREE.Vector3());assert.ok(size.x<.23&&size.z<.23,type+' footprint');assert.ok(size.y<.27,type+' height');assert.ok(b.min.y>=-.001);fruit.traverse(m=>{if(m.isMesh){assert.ok(m.userData.staticBatch);assert.ok(Array.from(m.geometry.attributes.position.array).every(Number.isFinite))}})}
});
test('night keeps a dark sky and no sunlight while preserving readable ambient fill',()=>{
 const noon=lightingAt(12*60);for(const m of [20*60,23*60+47,0,4*60]){const night=lightingAt(m);assert.equal(night.sun,0);assert.equal(night.night,1);assert.equal(night.lamp,1);assert.ok(night.hemisphere>=.35&&night.hemisphere<noon.hemisphere*.4);assert.ok(night.environment>=.1&&night.environment<noon.environment*.5);assert.ok(night.moon>=.3)}assert.ok(noon.sun>2.5);assert.equal(noon.lamp,0)
});
test('dusk fades smoothly and interiors preserve usable lighting at midnight',()=>{
 let previous=lightingAt(1080);for(let m=1081;m<=1200;m++){const now=lightingAt(m);assert.ok(now.night>=previous.night);assert.ok(now.night-previous.night<.013);previous=now}assert.ok(lightingAt(1140).night>.4);assert.equal(lightingAt(1439.999).hemisphere,lightingAt(0).hemisphere);const interior=lightingAt(0,true);assert.equal(interior.hemisphere,1.1);assert.equal(interior.environment,.32);assert.equal(interior.lamp,0);assert.equal(interior.sun,0);assert.equal(interior.moon,0)
});
test('lamp selection keeps nearly tied assignments stable and stays within its budget',()=>{
 const lamps=Array.from({length:20},(_,i)=>({x:i*10,z:0}));let selected=selectLamps(lamps,{x:35,z:10},[],4);assert.equal(selected.length,4);const prior=new Set(selected);selected=selectLamps(lamps,{x:35.01,z:10},selected,4);assert.ok(selected.every(p=>prior.has(p)));const moved=selectLamps(lamps,{x:180,z:10},selected,4);assert.equal(moved.length,4);assert.ok(moved.every(p=>p.x>=160));assert.equal(new Set(moved).size,4)
});

test('main road and footways never fall into large gaps between street lamps',()=>{
 for(let x=-100;x<=100;x++)for(const z of [-11,0,11]){const nearest=Math.min(...STREET_LAMPS.map(p=>Math.hypot(p.x-x,p.z-z)));assert.ok(nearest<13,`unlit gap at ${x},${z}: ${nearest}`)}
});

test('streetlight origins sit outside the shadow-casting pole',()=>{
 for(const p of STREET_LAMPS){const head=streetLampHead(p.x,p.z);assert.ok(Math.hypot(head.x-p.x,head.z-p.z)>.9)}
});
