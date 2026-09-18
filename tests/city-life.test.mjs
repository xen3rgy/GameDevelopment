import {ROAD_X} from '../dist/city-layout.js';
import test from 'node:test';
import assert from 'node:assert/strict';
import {Traffic,makeRoute,routePose,TRAFFIC_ROUTES,overlaps} from '../dist/traffic.js';
import {surfaceAt,soundMix,stereoAt,Soundscape} from '../dist/soundscape.js';
import {CameraRig,ROOMS} from '../dist/spatial.js';
import {VehicleTransition,gaitPose} from '../dist/animation.js';
import {newGame} from '../dist/model.js';

test('all traffic routes close continuously and stay on the road network',()=>{
 for(const route of TRAFFIC_ROUTES){const a=routePose(route,0),b=routePose(route,route.length-.001);assert.ok(Math.hypot(a.x-b.x,a.z-b.z)<.002);for(let d=0;d<route.length;d+=.5){const p=routePose(route,d);assert.ok(Number.isFinite(p.angle));assert.ok(Math.min(...ROAD_X.map(x=>Math.abs(x-p.x)))<6.5||Math.min(...[-65,0,65].map(z=>Math.abs(z-p.z)))<6.5)}}
});
test('traffic circulates through junctions without collisions or permanent deadlocks',()=>{
 const traffic=new Traffic(),travel=traffic.cars.map(()=>0);for(let i=0;i<6000;i++){const before=traffic.cars.map(c=>c.progress);traffic.update(1/30);traffic.cars.forEach((c,j)=>travel[j]+=(c.progress-before[j]+c.route.length)%c.route.length);for(let a=0;a<traffic.cars.length;a++)for(let b=a+1;b<traffic.cars.length;b++)assert.equal(overlaps(traffic.cars[a],traffic.cars[b],-.05),false)}assert.ok(travel.every(d=>d>650));
});
test('a car brakes before an occupied crossing, waits, and resumes after the pedestrian leaves',()=>{
 for(const fps of [20,60]){const traffic=new Traffic(1),car=traffic.cars[0];car.route=makeRoute([{x:-100,z:0},{x:100,z:0},{x:100,z:65},{x:-100,z:65}]);
 let best=Infinity;for(let d=0;d<car.route.length;d+=.1){const p=routePose(car.route,d),distance=Math.hypot(p.x+28,p.z+2.8);if(distance<best){best=distance;car.progress=d}}Object.assign(car,routePose(car.route,car.progress));car.speed=6;
 const pedestrian={x:-10,z:-7.5,vz:1};for(let i=0;i<fps*6;i++)traffic.update(1/fps,[pedestrian]);assert.ok(car.x<-12.5);assert.ok(car.speed<.1);const stopped=car.x;for(let i=0;i<fps*3;i++)traffic.update(1/fps);assert.ok(car.x>stopped+4);
 }
});
test('footsteps identify actual ground surfaces and stereo follows the camera',()=>{
 assert.equal(surfaceAt(340,40,'shop'),'tile');assert.equal(surfaceAt(300,0,'home'),'wood');assert.equal(surfaceAt(30,0),'asphalt');assert.equal(surfaceAt(30,9),'paving');assert.equal(surfaceAt(70,88),'grass');assert.equal(surfaceAt(76,88),'gravel');
 const listener={x:0,z:0};assert.ok(stereoAt({x:10,z:0},listener,0).pan>0);assert.ok(stereoAt({x:10,z:0},listener,Math.PI).pan<0);assert.ok(stereoAt({x:50,z:0},listener,0).gain<stereoAt({x:5,z:0},listener,0).gain);
 const state=newGame();state.minute=12*60;const day=soundMix(state);state.minute=23*60;assert.ok(soundMix(state).city<day.city);state.inside=true;state.interior='shop';assert.equal(soundMix(state).city,0);assert.equal(soundMix(state).market,1);state.settings.sound=false;assert.ok(Object.values(soundMix(state)).every(v=>v===0));state.settings.sound=true;assert.ok(Object.values(soundMix(state,false)).every(v=>v===0));
});
test('camera orbit takes the short path around angle wrap and smooths zoom within room boundaries',()=>{
 const p={x:340,y:.02,z:47},rig=new CameraRig();rig.update(p,Math.PI-.01,.35,5.5,0,[],ROOMS.shop);const a=rig.angle;rig.update(p,-Math.PI+.01,.35,5.5,1/60,[],ROOMS.shop);assert.ok(Math.abs(rig.angle-a)<.02);
 for(let i=0;i<100;i++){const view=rig.update(p,-Math.PI+.01,.8,14,1/60,[],ROOMS.shop);assert.ok(view.position.y<ROOMS.shop.ceiling);assert.ok(view.position.z<ROOMS.shop.maxZ);assert.ok(view.position.z>ROOMS.shop.minZ)}
});
test('vehicle transition is continuous, completes once and closes the door',()=>{
 for(const entering of [true,false]){const transition=new VehicleTransition({x:0,z:0},{x:2,z:1},entering,.5);let previous=0,view;for(let i=0;i<60;i++){view=transition.update(1/60);assert.ok(view.x>=previous);assert.ok(view.x-previous<.1);assert.ok(view.door>=0);previous=view.x}assert.equal(view.done,true);assert.equal(view.x,2);assert.ok(Math.abs(view.door)<1e-8);assert.equal(view.visible,!entering)}
 for(let p=0;p<Math.PI*2;p+=.1)for(const leg of gaitPose(p,1,true)){const foot=leg.y-.38*Math.cos(leg.hip)-.405*Math.cos(leg.hip+leg.knee)-.065;assert.ok(foot>=.0499&&foot<.16)}
});

test('sound activation and mute keep persistent ambience silent while paused',()=>{
 const audio=new Soundscape(),calls=[];audio.context={currentTime:5};audio.master={gain:{setTargetAtTime:v=>calls.push(['master',v])}};audio.ambient={gain:{setTargetAtTime:v=>calls.push(['ambient',v])}};
 audio.setEnabled(false);assert.deepEqual(calls.at(-1),['master',0]);audio.setEnabled(true);assert.ok(calls.at(-1)[1]>0);audio.stepDistance=.9;audio.setActive(false);assert.deepEqual(calls.at(-1),['ambient',0]);assert.equal(audio.stepDistance,0);audio.setActive(true);assert.deepEqual(calls.at(-1),['ambient',1]);
});

test('world batching keeps home geometry separate and hides exterior meshes indoors',async()=>{
 const THREE=await import('../dist/vendor/three.module.js'),{World}=await import('../dist/world.js');
 const scene=new THREE.Scene(),sky=new THREE.Mesh(new THREE.SphereGeometry(450),new THREE.MeshBasicMaterial()),shop=new THREE.Group();scene.add(sky,shop);const light=new THREE.PointLight();light.position.set(300,3,0);scene.add(light);
 const world={scene,atmosphere:{sky},shop,interiorLight:light,staticGroups:[],trash:[]};
 World.prototype.bench.call(world,-30,8);World.prototype.bench.call(world,300,0);World.prototype.batchStatic.call(world);World.prototype.isolateWorld.call(world);
 const homeBounds=new THREE.Box3().setFromObject(world.homeShell),exteriorBounds=new THREE.Box3().setFromObject(world.exterior);assert.ok(homeBounds.min.x>290);assert.ok(exteriorBounds.max.x<200);assert.ok(light.parent===world.homeShell);assert.ok(sky.parent===scene);
 world.exterior.visible=false;world.homeShell.visible=true;let exteriorDraws=0;scene.traverseVisible(o=>{if(o.isMesh&&o!==sky&&new THREE.Box3().setFromObject(o).max.x<200)exteriorDraws++});assert.equal(exteriorDraws,0);
});
