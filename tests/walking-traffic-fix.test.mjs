import test from 'node:test';
import assert from 'node:assert/strict';
import {npcGaitPose,NPC_STRIDE} from '../dist/animation.js';
import {Traffic,makeRoute,routePose,pedestrianBlocks} from '../dist/traffic.js';

test('NPC support foot stays planted in world space throughout its stance',()=>{
 for(const speed of [1.08,1.44])for(const fps of [20,60,144]){
  let reference=null;
  for(let t=0;t<.59;t+=speed/fps/NPC_STRIDE){const p=npcGaitPose(t*Math.PI*2,1)[0];
   const footZ=-.38*Math.sin(p.hip)-.405*Math.sin(p.hip+p.knee)+t*NPC_STRIDE;
   const footY=p.y-.38*Math.cos(p.hip)-.405*Math.cos(p.hip+p.knee)-.065;
   reference??=footZ;assert.ok(Math.abs(footZ-reference)<1e-8);assert.ok(Math.abs(footY-.05)<1e-8);
  }
 }
});
test('NPC feet lift during swing and stride gives normal walking cadence',()=>{
 for(let t=0;t<1;t+=.01){const p=npcGaitPose(t*Math.PI*2,1)[0],y=p.y-.38*Math.cos(p.hip)-.405*Math.cos(p.hip+p.knee)-.065;assert.ok(y>=.04999&&y<.14)}
 assert.ok(1.08/NPC_STRIDE*2>1.8);assert.ok(1.44/NPC_STRIDE*2<2.5);
});
test('pedestrian clearance distinguishes beside, in front, and walking into the lane',()=>{
 const car={x:0,z:0,angle:Math.PI/2,length:3.8,width:1.85};
 assert.equal(pedestrianBlocks(car,{x:0,z:1.5}),false);
 assert.equal(pedestrianBlocks(car,{x:2.1,z:0}),true);
 assert.equal(pedestrianBlocks(car,{x:2,z:2,vz:-2},1),true);
 assert.equal(pedestrianBlocks(car,{x:2,z:2,vz:2},1),false);
});
test('cars pass stationary people beside the lane even at a marked crossing',()=>{
 for(const z of [-4.3,-.8,2.8]){const traffic=new Traffic(1),car=traffic.cars[0];car.route=makeRoute([{x:-100,z:0},{x:100,z:0},{x:100,z:65},{x:-100,z:65}]);let best=Infinity;
  for(let d=0;d<car.route.length;d+=.1){const p=routePose(car.route,d),error=Math.hypot(p.x+28,p.z+2.8);if(error<best){best=error;car.progress=d}}
  Object.assign(car,routePose(car.route,car.progress));car.speed=5;
  for(let i=0;i<360;i++)traffic.update(1/60,[{x:-8,z,vx:0,vz:0}]);
  assert.ok(car.x>0,`stopped beside pedestrian at ${z}: ${car.x}`);
 }
});
