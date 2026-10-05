import test from 'node:test';import assert from 'node:assert/strict';import * as THREE from '../dist/vendor/three.module.js';
import {Traffic,TRAFFIC_TYPES,makeRoute,routePose,overlaps} from '../dist/traffic.js';
import {SIGNAL_JUNCTIONS,signalPhase,signalBlocks} from '../dist/traffic-signals.js';
import {createTrafficVehicle,TrafficSignals} from '../dist/traffic-scene.js';
import {weatherAt,blendWeather} from '../dist/weather.js';import {lightingAt} from '../dist/lighting.js';
import {GameModel,newGame,validateSave} from '../dist/model.js';
test('signals have exclusive greens, amber and clearance intervals at every controlled intersection',()=>{
 assert.ok(SIGNAL_JUNCTIONS.length>=8);for(const j of SIGNAL_JUNCTIONS){const phases=new Set();for(let t=0;t<96;t+=.25){const x=signalPhase(j,t,'x'),z=signalPhase(j,t,'z');assert.ok(x==='red'||z==='red');phases.add(x+'/'+z);}for(const phase of ['green/red','amber/red','red/red','red/green','red/amber'])assert.ok(phases.has(phase));}
 const j={x:0,z:0},v={x:-18,z:2.8,angle:Math.PI/2,length:3.8};assert.ok(signalBlocks(v,{x:-13,z:2.8},j,30));assert.equal(signalBlocks(v,{x:-13,z:2.8},j,5),false);assert.equal(signalBlocks({...v,x:-10},{x:-5,z:2.8},j,30),false);
});
const place=(c,x,z)=>{let best=Infinity;for(let p=0;p<c.route.length;p+=.05){const v=routePose(c.route,p),d=Math.hypot(v.x-x,v.z-z);if(d<best){best=d;c.progress=p;}}Object.assign(c,routePose(c.route,c.progress));};
test('real traffic stops before a red crossing, restarts on green and keeps pedestrians safe',()=>{
 const traffic=new Traffic(1),c=traffic.cars[0];c.route=makeRoute([{x:-100,z:0},{x:100,z:0},{x:100,z:65},{x:-100,z:65}]);place(c,-28,2.8);c.speed=5;traffic.time=25;
 for(let i=0;i<18*30;i++)traffic.update(1/30);assert.ok(c.x<-13.9);assert.equal(c.speed,0);
 for(let i=0;i<15*30;i++)traffic.update(1/30);assert.ok(c.x>0);
 const bus=new Traffic(5),b=bus.cars.find(v=>v.type==='bus');bus.cars=[b];b.route=c.route;place(b,-28,2.8);b.speed=5;bus.time=0;for(let i=0;i<8*30;i++)bus.update(1/30,[{x:-20,z:2.8,vx:0,vz:0}]);assert.ok(b.x+b.length/2<-20);assert.equal(b.speed,0);
});
test('mixed traffic uses matching dimensions and maintains separation over several signal cycles',()=>{
 const traffic=new Traffic();assert.deepEqual(new Set(traffic.cars.map(v=>v.type)),new Set(Object.keys(TRAFFIC_TYPES)));
 const progress=traffic.cars.map(v=>v.progress);for(let i=0;i<1200;i++){traffic.update(.1);for(let a=0;a<traffic.cars.length;a++)for(let b=a+1;b<traffic.cars.length;b++)assert.equal(overlaps(traffic.cars[a],traffic.cars[b],-.03),false,`cars ${a}/${b} at ${i}`);}
 for(const c of traffic.cars){assert.ok(Number.isFinite(c.x+c.z+c.speed));assert.notEqual(c.progress,progress[c.id]);assert.equal(c.length,TRAFFIC_TYPES[c.type].length);}
});
const boxGeo=new THREE.BoxGeometry(),kit={mat:c=>new THREE.MeshStandardMaterial({color:c}),sign:()=>{},box(g,x,y,z,w,h,d,c,m){const mesh=new THREE.Mesh(boxGeo,m||new THREE.MeshStandardMaterial({color:c}));mesh.position.set(x,y,z);mesh.scale.set(w,h,d);g.add(mesh);return mesh;}};
test('six traffic appearances retain wheel and lamp rigs; signal lamps display their actual phase',()=>{
 for(const type of Object.keys(TRAFFIC_TYPES)){const mesh=createTrafficVehicle(kit,type);assert.equal(mesh.userData.trafficType,type);assert.equal(mesh.userData.wheels.length,4);assert.ok(mesh.userData.vehicleLights.headlights);const size=new THREE.Box3().setFromObject(mesh).getSize(new THREE.Vector3());assert.ok(size.z<TRAFFIC_TYPES[type].length+.3);assert.ok(size.x<2.4);}
 const world={scene:new THREE.Scene(),colliders:[]},signals=new TrafficSignals(world,kit);signals.update(30,false,1);assert.equal(signals.heads.length,SIGNAL_JUNCTIONS.length*4);for(const head of signals.heads){const phase=signalPhase(head.junction,30,head.axis);assert.ok(head.bulbs.find(b=>b.color===phase).mesh.material.color.getHex()!==0x26302b);}signals.update(30,true,0);assert.equal(signals.root.visible,false);
});
test('weather changes during the day, survives reload and time jumps; initial morning remains clear',()=>{
 assert.equal(weatherAt(1,480),'clear');assert.deepEqual(new Set(Array.from({length:24},(_,i)=>weatherAt(1,i*60))),new Set(['clear','cloudy','rain','fog']));
 const m=new GameModel(newGame(true));m.advance(200);assert.equal(m.s.weather,weatherAt(m.s.day,m.s.minute));const loaded=new GameModel(validateSave(JSON.parse(JSON.stringify(m.s))));assert.equal(loaded.s.weather,m.s.weather);m.advance(1500);assert.equal(m.s.weather,weatherAt(m.s.day,m.s.minute));
});
test('rain and fog blend gradually, wet streets dry slowly, pause preserves weather and nights retain visibility',()=>{
 let w=blendWeather(null,'clear',0);const start={...w};w=blendWeather(w,'rain',1);assert.ok(w.rain>0&&w.rain<.2&&w.wet>0&&w.wet<.1);assert.deepEqual(blendWeather(w,'fog',0),w);
 w=blendWeather(w,'rain',100);const dry=blendWeather(w,'clear',10);assert.ok(dry.wet>.85);assert.ok(dry.rain<.3);const fog=blendWeather(start,'fog',100);assert.ok(fog.fog>.99);
 const day=lightingAt(720),night=lightingAt(0);assert.ok(day.hemisphere>night.hemisphere*4.5);assert.ok(night.hemisphere+night.moon>=.65-1e-8);assert.equal(night.lamp,1);assert.equal(day.lamp,0);
});
