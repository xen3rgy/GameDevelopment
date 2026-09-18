import test from 'node:test';
import assert from 'node:assert/strict';
import {readFileSync} from 'node:fs';
import {GameModel,newGame,validateSave} from '../dist/model.js';
import {CAFE_POINTS,CAFE_MENU,cafeCosts,cafeDayResult} from '../dist/cafe.js';
import {simulationMinutes} from '../dist/game-time.js';
import {BUSINESSES} from '../dist/data.js';

function setup({level=3,quality=3,price=1.2,economy='Normal',minute=480,staff=3,seed=1000}={}){
 const s=newGame(true);Object.assign(s,{minute,economy,inside:true,interior:'cafe',position:{...CAFE_POINTS.cafeOffice}});
 s.businesses.cafe={stock:3,staff,staffLevels:[level,level,level],quality,price,marketing:false,open:true,profit:0,sales:0,costs:0};
 const m=new GameModel(s);assert.ok(m.cafeAction('start'));s.cafe.rng=seed;return m;
}
test('10x uses identical cafe simulation and survives save/reload; time skips also simulate the team',()=>{
 assert.equal(simulationMinutes(144,10),1440);assert.equal(simulationMinutes(1,9),1);
 const normal=setup(),fast=setup(),skip=setup();fast.s.settings.speed=10;
 normal.updateTime(120);fast.updateTime(12);skip.advance(120);
 assert.deepEqual(fast.s.cafe,normal.s.cafe);assert.deepEqual(skip.s.cafe,normal.s.cafe);
 assert.equal(fast.s.minute,normal.s.minute);assert.equal(validateSave(fast.s).settings.speed,10);
 const resumed=new GameModel(validateSave(fast.s));fast.updateTime(36);resumed.updateTime(36);
 assert.deepEqual(resumed.s.cafe,fast.s.cafe);assert.equal(fast.s.cafe.phase,'closed');assert.equal(fast.s.minute,960);
 assert.match(readFileSync(new URL('../dist/app.js',import.meta.url),'utf8'),/option value="10"/);
});
test('leaving a staffed cafe or skipping time does not replace actual service with artificial revenue',()=>{
 const inside=setup(),outside=setup();assert.ok(outside.leaveInterior());assert.equal(outside.s.cafe.phase,'open');
 inside.updateTime(480);outside.advance(480,true);assert.deepEqual(outside.s.cafe,inside.s.cafe);
 assert.ok(outside.s.cafe.served>0);assert.doesNotThrow(()=>validateSave(outside.s));
 const s=outside.s,c=s.cafe,b=s.businesses.cafe,report=cafeDayResult(c,b,BUSINESSES.cafe,0),cash=s.money;
 outside.advance(1440-s.minute,true);
 assert.equal(b.sales,c.revenue+c.tips);assert.equal(b.cafeReport.automaticSales,0);
 assert.equal(b.profit,c.lastReport.profit);assert.equal(s.money,cash+report.cashMovement);
 assert.equal(report.cashMovement,report.profit+833);assert.equal(b.stock,2);
 outside.advance(1440,true);assert.equal(b.sales,0);assert.equal(b.costs,2450);assert.equal(b.stock,2);
 assert.ok(c.employees.every(e=>e.status==='gone'));
});
test('fixed costs, call-out minimum and actual hours agree; firing after closing cannot erase wages',()=>{
 for(const minutes of [0,60,120,240,480]){
  const m=setup();m.s.cafe.nextGuest=999;m.updateTime(minutes);if(m.s.cafe.phase==='open')m.cafeAction('finish');
  const r=m.s.cafe.lastReport,expectedWage=Math.round(3*375*Math.max(2,minutes/60));
  assert.equal(r.breakdown.wages,expectedWage);assert.equal(r.costs,2450+833+expectedWage);assert.equal(r.profit,-r.costs);
  m.manage('cafe','fire');m.advance(1440-m.s.minute,true);assert.equal(m.s.businesses.cafe.cafeReport.wages,expectedWage);
 }
});
test('a barista prepares another meal while the server carries one; serving does not erase the next tray',()=>{
 const m=setup();m.s.cafe.nextGuest=999;m.updateTime(20);const c=m.s.cafe;
 const guest=id=>({id,party:id,seat:id-1,side:-1,recipe:id===1?'breakfast':'espresso',stage:'accepted',time:0,wait:0,patience:200,price:CAFE_MENU[id===1?'breakfast':'espresso'].price,paid:false,served:false,tipPending:0,paymentWait:0,prepaid:false});
 c.guests=[guest(1),guest(2)];c.serial=2;c.tray=1;c.ingredients=260;
 const e=c.employees[1];Object.assign(e,CAFE_POINTS.cafePrep);e.task=null;e.route=[];
 let concurrent=false,delivered=false;
 for(let n=0;n<400;n++){m.updateTime(.05);concurrent ||= c.staffCarry===1&&(c.prep?.id===2||c.tray===2);if(c.guests[0]?.served){delivered=true;assert.ok(c.tray===2||c.prep?.id===2);break}}
 assert.ok(concurrent);assert.ok(delivered);assert.equal(c.ingredients,320);assert.doesNotThrow(()=>validateSave(m.s));
});
test('old active shifts keep their duration/accounting and migrate a carried tray without duplicate preparation',()=>{
 const m=setup();const old=structuredClone(m.s);delete old.cafe.economyModel;old.cafe.duration=210;old.cafe.closeMinute=690;
 old.cafe.serial=1;old.cafe.guests=[{id:1,party:1,seat:0,side:-1,recipe:'espresso',stage:'accepted',time:0,wait:0,patience:200,price:340,paid:false,served:false,tipPending:0,paymentWait:0,prepaid:false}];old.cafe.tray=1;old.cafe.staffCarry=1;Object.assign(old.cafe.employees[1],{status:'working',task:{kind:'serve',id:1,seat:0,place:null},route:[],wait:.5});
 const migrated=validateSave(old);assert.equal(migrated.cafe.tray,null);assert.equal(migrated.cafe.staffCarry,1);assert.equal(migrated.cafe.economyModel,1);assert.equal(migrated.cafe.duration,210);
 const restored=new GameModel(migrated);restored.updateTime(210);assert.equal(restored.s.cafe.phase,'closed');assert.ok(restored.s.cafe.lastReport);
 assert.ok(cafeDayResult(restored.s.cafe,restored.s.businesses.cafe,BUSINESSES.cafe,0).automaticSales>0);
 restored.advance(1440-restored.s.minute,true);restored.advance(480,true);restored.s.position={...CAFE_POINTS.cafeOffice};assert.ok(restored.cafeAction('start'));
 assert.equal(restored.s.cafe.economyModel,2);assert.equal(restored.s.cafe.duration,480);
});
test('a timed-out pickup cannot erase a different guest meal carried by the service',()=>{
 const m=setup();m.s.cafe.nextGuest=999;m.updateTime(20);const c=m.s.cafe;
 c.serial=2;c.guests=[1,2].map(id=>({id,party:id,seat:id-1,side:-1,recipe:'espresso',stage:'accepted',time:0,wait:id===2?99.99:0,patience:100,price:340,paid:false,served:false,tipPending:0,paymentWait:0,prepaid:false}));
 c.tray=2;c.staffCarry=1;Object.assign(c.employees[1],{task:{kind:'serve',id:1,seat:0,place:null},route:[{...CAFE_POINTS.cafeTable0}],wait:.65});
 m.updateTime(.05);assert.equal(c.tray,null);assert.equal(c.staffCarry,1);assert.equal(c.lost,1);assert.doesNotThrow(()=>validateSave(m.s));
});
test('balance: training improves results, viable pricing earns modest profit, discounts and recessions remain risky',()=>{
 function mean(options){let profit=0,served=0,lost=0;for(let seed=1;seed<=16;seed++){const m=setup({...options,seed});m.updateTime(480);const r=m.s.cafe.lastReport;profit+=r.profit;served+=r.served;lost+=r.lost;assert.equal(r.sales,m.s.cafe.receipts.reduce((v,r)=>v+r.amount,0));}return {profit:profit/16,served:served/16,lost:lost/16};}
 const trained=mean({}),novice=mean({level:1}),discount=mean({price:.8}),recession=mean({economy:'Rezession'});
 assert.ok(trained.profit>1000&&trained.profit<6000,JSON.stringify(trained));
 assert.ok(novice.profit<trained.profit);assert.ok(novice.served<trained.served);assert.ok(novice.lost>trained.lost);
 assert.ok(discount.profit<0);assert.ok(recession.profit<trained.profit);
});
