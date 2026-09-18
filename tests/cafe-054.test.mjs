import test from 'node:test';
import assert from 'node:assert/strict';
import {guestServiceView,paintGuestBadge} from '../dist/cafe-service-view.js';
import {waitingCafeOrders,tickCafe} from '../dist/cafe.js';
import {GameModel,newGame} from '../dist/model.js';

const guest=(id,wait=0,patience=120)=>({id,seat:0,side:-1,recipe:'espresso',stage:'accepted',wait,patience,price:340});
test('service display reports the authoritative countdown and never invents patience after service',()=>{
 const g=guest(1);assert.equal(guestServiceView(g).value,'120 min');g.wait=119.3;const v=guestServiceView(g);assert.equal(v.value,'1 min');assert.ok(v.urgent);assert.ok(v.fraction<.01);
 g.wait=121;assert.equal(guestServiceView(g).fraction,0);assert.equal(guestServiceView(g).seconds,0);
 g.stage='paying';const pay=guestServiceView(g);assert.equal(pay.value,'3,40 €');assert.equal(pay.urgent,false);assert.equal(pay.detail,'RECHNUNG OFFEN');
 for(const stage of ['arriving','eating','finished','leaving']){g.stage=stage;assert.equal(guestServiceView(g).visible,false)}
});
test('panel and large progress fill are painted together in correct order on a reused canvas',()=>{
 const calls=[],ctx={fillStyle:'',fillRect(...args){calls.push(['fill',this.fillStyle,...args])},fillText(...args){calls.push(['text',...args])},clearRect(){calls.length=0},strokeRect(){},beginPath(){},moveTo(){},lineTo(){},stroke(){}};
 const canvas={width:512,height:128,getContext:()=>ctx},texture={image:canvas,needsUpdate:false};
 paintGuestBadge(texture,guestServiceView(guest(1,60)));assert.equal(texture.image,canvas);assert.equal(canvas.width,640);assert.equal(canvas.height,224);assert.equal(texture.needsUpdate,true);
 const fills=calls.filter(c=>c[0]==='fill');assert.deepEqual(fills[0],['fill','#10292f',0,0,640,224]);assert.deepEqual(fills.at(-1),['fill','#ffdc88',28,126,292,34]);assert.ok(calls.some(c=>c[0]==='text'&&c[1]==='60 min'));
 paintGuestBadge(texture,guestServiceView({...guest(1),stage:'paying'}));assert.equal(calls.filter(c=>c[0]==='fill').length,1);assert.ok(calls.some(c=>c[1]==='Bitte kassieren'));
});
test('display cache remains unchanged while paused and changes when seconds or stage change',()=>{
 const g=guest(1,31.25),first=guestServiceView(g);assert.equal(guestServiceView(g).key,first.key);g.wait+=1;assert.notEqual(guestServiceView(g).key,first.key);const second=guestServiceView(g).key;g.stage='paying';assert.notEqual(guestServiceView(g).key,second);
});
test('order queue selects the closest deadline without changing seats or interrupting preparation',()=>{
 const a=guest(1,10,120),b=guest(2,65,80),c=guest(3,30,100),guests=[a,b,c];assert.deepEqual(waitingCafeOrders({guests}).map(g=>g.id),[2,3,1]);assert.deepEqual(guests.map(g=>g.id),[1,2,3]);
 const s=newGame();s.inside=true;s.interior='cafe';s.cafe.phase='open';s.cafe.day=1;s.cafe.config={staff:1,staffLevels:[1,1,1],quality:1,price:1,marketing:false,economy:'Normal'};s.cafe.guests=guests;s.cafe.nextGuest=999;s.cafe.serial=3;
 const m=new GameModel(s);tickCafe(m,.05);assert.equal(s.cafe.prep.id,2);a.wait=119;tickCafe(m,.05);assert.equal(s.cafe.prep.id,2);assert.equal(s.cafe.guests[0].seat,0);
});
