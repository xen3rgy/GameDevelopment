import test from 'node:test';
import assert from 'node:assert/strict';
import {moveWithCollision} from '../dist/movement.js';
import {interactionRange,interactionScore,interactionVerb,withInteractionDistance} from '../dist/interaction.js';
import {CameraRig} from '../dist/spatial.js';

test('0.7.4 collision slide keeps useful tangential movement at a wall',()=>{
  const canWalk=(x,z)=>x<.5;
  const r=moveWithCollision({x:.4,z:0},.8,1.2,canWalk,0,true);
  assert.equal(r.blocked,true);
  assert.ok(r.x<.5);
  assert.ok(r.z>.9,'movement should continue along the free wall direction');
});

test('0.7.4 interaction ranges are shared and work targets win ambiguous focus',()=>{
  const p={x:0,z:0};
  const location=withInteractionDistance({type:'location',x:2.2,z:0},p);
  const trash=withInteractionDistance({type:'trash',x:1.9,z:0},p);
  assert.equal(interactionRange(location),3.1);
  assert.equal(interactionRange({type:'bed'},true),1.9);
  assert.ok(interactionScore(trash,p)<interactionScore(location,p));
  assert.equal(interactionVerb(trash),'ARBEITEN');
  assert.equal(interactionVerb({type:'bottle'}),'AUFHEBEN');
  assert.equal(interactionVerb({type:'vehicle'}),'FAHRZEUG');
});

test('0.7.4 indoor camera stays compact and extends smoothly after an obstacle',()=>{
  const rig=new CameraRig(),room={minX:-5,maxX:5,minZ:-5,maxZ:5,ceiling:3.6};
  const pos={x:0,y:0,z:0};
  const blocked=rig.update(pos,0,.3,8,1/60,[{x:0,z:1,w:.8,d:.2,h:3}],room);
  assert.ok(blocked.distance<4);
  const before=blocked.distance;
  const open=rig.update(pos,Math.PI,.3,8,1/60,[],room);
  assert.ok(open.distance>=before);
  assert.ok(open.distance<=4.01);
  assert.ok(open.distance-before<.5,'camera should not pop outward in one frame');
});
