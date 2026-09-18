import test from 'node:test';
import assert from 'node:assert/strict';
import {updateCrowd} from '../dist/crowd.js';
import {Soundscape} from '../dist/soundscape.js';
import {newGame,validateSave} from '../dist/model.js';
import {parkedVehicleBlocks} from '../dist/traffic.js';
import {World} from '../dist/world.js';
const person=(x,dir=1,speed=1.3)=>({x,z:9,lane:9,dir,speed,yaw:dir*Math.PI/2});

test('oncoming walkers pass with separation at 20, 60 and 144 fps',()=>{
 for(const fps of [20,60,144]){const crowd=[person(-4),person(4,-1,1.2)];
  for(let i=0;i<fps*12;i++){const before=crowd.map(p=>({...p}));updateCrowd(crowd,1/fps);
   assert.ok(Math.hypot(crowd[0].x-crowd[1].x,crowd[0].z-crowd[1].z)>=.72);
   crowd.forEach((p,j)=>{assert.ok(Math.abs(p.z-9)<=1.05);assert.ok(p.distance<=p.speed/fps+1e-8);assert.ok(p.distance+1e-8>=Math.hypot(p.x-before[j].x,p.z-before[j].z))});
  }assert.ok(crowd[0].x>7&&crowd[1].x<-7);
 }
});
test('walkers pass a standing person and return to their route',()=>{
 const crowd=[person(-4)],obstacle={x:0,z:9,radius:.4};
 for(let i=0;i<1200;i++){updateCrowd(crowd,1/60,[obstacle]);assert.ok(Math.hypot(crowd[0].x,crowd[0].z-9)>=.76)}
 assert.ok(crowd[0].x>12);assert.ok(Math.abs(crowd[0].z-9)<.02);
});
test('blocked walkers stop their stride and resume after an obstacle clears',()=>{
 const crowd=[person(-4)];const canWalk=(x,z)=>x<0;
 for(let i=0;i<600;i++)updateCrowd(crowd,1/60,[],canWalk);
 assert.ok(crowd[0].x<0);assert.equal(crowd[0].distance,0);
 for(let i=0;i<180;i++)updateCrowd(crowd,1/60);
 assert.ok(crowd[0].x>3);
 const before={...crowd[0]};updateCrowd(crowd,0);assert.equal(crowd[0].x,before.x);assert.equal(crowd[0].distance,0);
});
test('twenty walkers circulate without overlapping or permanent crowd jams',()=>{
 const crowd=Array.from({length:20},(_,i)=>({...person(-98+i*17%196,i%3===0?-1:1,1.08+i%4*.12),z:i%2?-9:9,lane:i%2?-9:9})),travel=crowd.map(()=>0);
 for(let i=0;i<7200;i++){updateCrowd(crowd,1/30,[{x:23,z:-9},{x:-38,z:9},{x:61,z:-9}]);crowd.forEach((p,j)=>travel[j]+=p.distance);
  for(let a=0;a<crowd.length;a++)for(let b=a+1;b<crowd.length;b++)assert.ok(Math.hypot(crowd[a].x-crowd[b].x,crowd[a].z-crowd[b].z)>=.72-1e-8);
 }assert.ok(travel.every(d=>d>120),JSON.stringify(travel));
});
test('audio defaults and saved values preserve zero and sanitize invalid imports',()=>{
 const s=newGame();delete s.settings.ambienceVolume;delete s.settings.vehicleVolume;delete s.settings.effectsVolume;
 const migrated=validateSave(s);for(const key of ['ambienceVolume','vehicleVolume','effectsVolume'])assert.equal(migrated.settings[key],100);
 migrated.settings.ambienceVolume=0;migrated.settings.vehicleVolume=-40;migrated.settings.effectsVolume=400;
 const loaded=validateSave(JSON.parse(JSON.stringify(migrated)));assert.equal(loaded.settings.ambienceVolume,0);assert.equal(loaded.settings.vehicleVolume,0);assert.equal(loaded.settings.effectsVolume,100);
 loaded.settings.vehicleVolume='invalid';assert.equal(validateSave(loaded).settings.vehicleVolume,100);
});
function audioContext(){
 const nodes=[];const param=()=>({value:0,setTargetAtTime(v){this.value=v},setValueAtTime(v){this.value=v},exponentialRampToValueAtTime(v){this.value=v}});
 const node=()=>{const n={gain:param(),frequency:param(),Q:param(),pan:param(),threshold:param(),ratio:param(),connections:[],connect(other){this.connections.push(other);return other},start(){},stop(){},disconnect(){}};nodes.push(n);return n};
 return {nodes,state:'running',currentTime:0,sampleRate:100,destination:node(),createGain:node,createDynamicsCompressor:node,createOscillator:node,createBiquadFilter:node,createStereoPanner:node,createBufferSource:node,createBuffer:()=>({getChannelData:()=>new Float32Array(200)})};
}
test('all sound sources route through independently mutable buses',()=>{
 const sound=new Soundscape(),ctx=audioContext();sound.setVolumes({ambienceVolume:0,vehicleVolume:35,effectsVolume:75});sound.init(ctx);
 assert.equal(sound.environmentBus.gain.value,0);assert.equal(sound.vehicleBus.gain.value,.35);assert.equal(sound.effectsBus.gain.value,.75);
 for(const source of [sound.wind,sound.rain,sound.room,sound.humGain])assert.ok(source.connections.includes(sound.environmentBus));
 for(const voice of sound.voices)assert.ok(voice.pan.connections.includes(sound.vehicleBus));
 sound.tone();assert.ok(ctx.nodes.at(-1).connections.includes(sound.effectsBus));
 sound.burst(1400,.2,.1,0,'bandpass','environmentBus');assert.ok(ctx.nodes.at(-1).connections.includes(sound.environmentBus));
 sound.tone(1400,.2,.1,0,'environmentBus');assert.ok(ctx.nodes.at(-1).connections.includes(sound.environmentBus));
 sound.setVolumes({ambienceVolume:80,vehicleVolume:0,effectsVolume:0});assert.equal(sound.environmentBus.gain.value,.8);assert.equal(sound.vehicleBus.gain.value,0);assert.equal(sound.effectsBus.gain.value,0);
 sound.setEnabled(false);assert.equal(sound.master.gain.value,0);sound.setActive(false);assert.equal(sound.ambient.gain.value,0);sound.setEnabled(true);assert.equal(sound.effectsBus.gain.value,0);
});
test('parked car collision follows rotation, allows legacy overlap escape, and excludes driving',()=>{
 const vehicle={id:'car',x:0,z:0,angle:Math.PI/2};assert.equal(parkedVehicleBlocks(vehicle,0,0),true);assert.equal(parkedVehicleBlocks(vehicle,0,1.5),false);assert.equal(parkedVehicleBlocks(vehicle,2,0),true);
 assert.equal(parkedVehicleBlocks(vehicle,.1,0,.35,{x:0,z:0}),false);assert.equal(parkedVehicleBlocks(vehicle,1,0,.35,{x:1.1,z:0}),true);
 const world={model:{s:{vehicle,inside:false,riding:false,position:{x:0,z:3}}},canWalkExterior:()=>true,traffic:{blocks:()=>false}};
 assert.equal(World.prototype.canMove.call(world,0,0),false);world.model.s.riding=true;assert.equal(World.prototype.canMove.call(world,0,0),true);
});
