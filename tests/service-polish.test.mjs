import test from 'node:test';
import assert from 'node:assert/strict';
import * as THREE from '../dist/vendor/three.module.js';
import {ServiceScene} from '../dist/service-scene.js';
import {ServiceGestures} from '../dist/service-gestures.js';
import {SERVICE_BAYS,serviceAligned} from '../dist/service-layout.js';
import {createCitizen} from '../dist/art.js';
import {GameModel,newGame} from '../dist/model.js';
import {beginService,tickService,endService,vehicleAtBay} from '../dist/vehicle-service.js';
const geometry=new THREE.BoxGeometry(),materials=new Map();
const kit={mat(c){if(!materials.has(c))materials.set(c,new THREE.MeshStandardMaterial({color:c}));return materials.get(c);}};
kit.box=(p,x,y,z,w,h,d,c,m)=>{const mesh=new THREE.Mesh(geometry,m||kit.mat(c));mesh.position.set(x,y,z);mesh.scale.set(w,h,d);p.add(mesh);return mesh;};
kit.cylinder=(p,x,y,z,r,h,c)=>kit.box(p,x,y,z,r*2,h,r*2,c);
kit.sign=(p,t,x,y,z,w,h)=>kit.box(p,x,y,z,w,h,.01,0xffffff);
function setup(index=0){const model=new GameModel(newGame(true));model.s.position={x:76,z:12};assert.ok(model.buyVehicle('car'));const b=SERVICE_BAYS[index];Object.assign(model.s.vehicle,b.vehicle,{fuel:40,condition:60,parking:null});model.s.position={x:b.x,z:b.z};return model;}
function actors(){const root=new THREE.Group(),docked=new Map(SERVICE_BAYS.filter(b=>b.kind==='fuel').map(b=>[b.id,new THREE.Group()]));return {view:new ServiceGestures(root,kit,docked),player:createCitizen(kit)};}
function frame(view,player,s,dt){view.resetPose(player);player.position.set(s.position.x,.25,s.position.z);view.update(s,player,dt);}
test('service alignment is forgiving, orientation-aware and still collision/occupancy gated',()=>{
 for(let i=0;i<3;i++)for(const angle of [.24,Math.PI+.24]){const m=setup(i),b=SERVICE_BAYS[i];m.s.vehicle.x+=1.25;m.s.vehicle.angle=angle;assert.ok(serviceAligned(m.s.vehicle,b));assert.ok(beginService(m,b.id));}
 const m=setup(),b=SERVICE_BAYS[0];m.s.vehicle.x+=1.5;assert.equal(vehicleAtBay(m.s,b),null);m.s.vehicle.x=b.vehicle.x;m.s.vehicle.angle=.3;assert.equal(vehicleAtBay(m.s,b),null);
 m.s.vehicle.angle=0;m.s.fleet.push({...m.s.vehicle,uid:'other'});assert.equal(beginService(m,b.id),false);
 const blocked=setup();blocked.canServiceVehicle=()=>false;assert.equal(beginService(blocked,b.id),false);
});
test('fuel remains held through 100%, finishes afterwards, freezes on pause and leaves model untouched',()=>{
 const m=setup(),{view,player}=actors();assert.ok(beginService(m,'pump1'));tickService(m,1);frame(view,player,m.s,.016);const held=player.position.clone();
 tickService(m,6.99);frame(view,player,m.s,.016);assert.ok(player.position.distanceTo(held)<1e-8);assert.equal(view.finish,null);assert.equal(view.nozzle.visible,true);
 tickService(m,.01);assert.equal(m.s.vehicleService,null);const saved=JSON.stringify(m.s);frame(view,player,m.s,.016);assert.ok(view.finish);assert.equal(view.nozzle.visible,true);assert.equal(view.docked.get('pump1').visible,false);
 const age=view.finish.age;frame(view,player,m.s,0);assert.equal(view.finish.age,age);
 for(let i=0;i<80;i++)frame(view,player,m.s,.016);assert.equal(view.finish,null);assert.equal(view.nozzle.visible,false);assert.equal(view.docked.get('pump1').visible,true);assert.equal(JSON.stringify(m.s),saved);
});
test('fuel cancellation clears presentation without a false completion sequence',()=>{
 const m=setup(),{view,player}=actors();beginService(m,'pump1');tickService(m,3);frame(view,player,m.s,.016);endService(m,true);frame(view,player,m.s,.016);assert.equal(view.finish,null);assert.equal(view.hose.visible,false);assert.equal(m.s.vehicle.fuel,62.5);
});
test('mechanic walks at bounded speed both ways with moving legs and works only on arrival',()=>{
 const m=setup(2),{view,player}=actors();beginService(m,'autoWorkshop');let work=false,outbound=false,returning=false;
 for(let i=0;i<360;i++){const before=view.mechanic.position.clone();tickService(m,1/60);frame(view,player,m.s,1/60);const distance=before.distanceTo(view.mechanic.position);assert.ok(distance<=1.65/60+1e-8);if(distance>.001&&Math.abs(view.mechanic.userData.legs[0].rotation.x)>.03)outbound=true;work||=view.tool.visible;}
 if(m.s.vehicleService)tickService(m,.01);
 for(let i=0;i<240;i++){const before=view.mechanic.position.clone();frame(view,player,m.s,1/60);const distance=before.distanceTo(view.mechanic.position);assert.ok(distance<=1.65/60+1e-8);if(distance>.001&&Math.abs(view.mechanic.userData.legs[0].rotation.x)>.03)returning=true;}
 assert.ok(work&&outbound&&returning);assert.ok(view.mechanic.position.distanceTo(view.home)<.03);
});
test('pump display reuses its texture, skips unchanged values and retains final delivered totals',()=>{
 let paints=0;const display={canvas:{getContext:()=>({fillRect(){paints++;},fillText(){}})},texture:{},key:null,action:null};
 const scene={displays:new Map([['pump1',display]]),drawDisplay:ServiceScene.prototype.drawDisplay};const m=setup();beginService(m,'pump1');
 const update=dt=>ServiceScene.prototype.updateDisplays.call(scene,m.s,dt);
 update(0);const initial=paints;update(.1);assert.equal(paints,initial);tickService(m,4);update(.1);assert.equal(display.key,'13.5 L|24.03 €');
 tickService(m,4);update(0);assert.equal(display.key,'27.0 L|48.06 €');const final=paints;update(.2);assert.equal(paints,final);
});
test('parking guides turn green only for a unique stopped, collision-clear vehicle',()=>{
 const scene=Object.create(ServiceScene.prototype);scene.root=new THREE.Group();scene.guides=[];for(const b of SERVICE_BAYS)scene.addGuide(b,kit);
 scene.world={canDrive:()=>true};scene.lights=[];scene.gestures={update(){}};scene.updateDisplays=()=>{};scene.price=178;
 const m=setup();m.s.riding=true;scene.update(m.s,null,0);assert.equal(scene.guides[0].material.color.getHex(),0x6be6a0);
 m.s.vehicle.speed=.1;scene.update(m.s,null,0);assert.equal(scene.guides[0].labels[1].visible,true);
 m.s.vehicle.speed=0;scene.world.canDrive=()=>false;scene.update(m.s,null,0);assert.equal(scene.guides[0].material.color.getHex(),0xe7bc62);
});
