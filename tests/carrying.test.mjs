import test from 'node:test';
import assert from 'node:assert/strict';
import {newGame,GameModel} from '../dist/model.js';
import {walkingProfile,PARCEL_WALK_SPEED} from '../dist/player-movement.js';
import {deliveryTarget} from '../dist/delivery-routes.js';

test('a real courier pickup blocks sprint and jump; cancellation restores unladen movement',()=>{
 const m=new GameModel(newGame(true));m.s.position=deliveryTarget('jobs');
 const empty=walkingProfile(m.s,true);assert.equal(empty.running,true);assert.equal(empty.canJump,true);
 assert.ok(m.acceptContract('chapter'));m.tickCourier(2.2);
 for(const speed of [1,4,10]){m.s.settings.speed=speed;const carry=walkingProfile(m.s,true);assert.equal(carry.speed,PARCEL_WALK_SPEED);assert.equal(carry.running,false);assert.equal(carry.canJump,false);assert.ok(carry.speed<walkingProfile(newGame(true)).speed*.7);}
 m.s.needs.energy=5;assert.equal(walkingProfile(m.s,true).speed,2.25);
 m.s.needs.energy=100;m.s.riding=true;assert.equal(walkingProfile(m.s).load,null);
 m.s.riding=false;m.cancelJob();assert.deepEqual(walkingProfile(m.s,true),empty);
});
test('the visible load determines restrictions, with no lingering cafe or courier load indoors',()=>{
 const s=newGame(true);s.job={type:'warehouse',carrying:true};assert.equal(walkingProfile(s,true).load,'crate');assert.equal(walkingProfile(s,true).running,false);
 s.job.carrying=false;assert.equal(walkingProfile(s,true).running,true);
 s.job=null;s.inside=true;s.interior='cafe';s.cafe.carrying={};assert.equal(walkingProfile(s,true).load,'tray');assert.equal(walkingProfile(s,true).canJump,false);
 s.interior='home';assert.equal(walkingProfile(s,true).load,null);
 s.job={type:'courier'};assert.equal(walkingProfile(s,true).load,null);
 s.inside=false;s.interior=null;assert.equal(walkingProfile(s,true).load,'parcel');
});
