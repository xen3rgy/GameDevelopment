import test from 'node:test';
import assert from 'node:assert/strict';
import {GameModel,newGame,validateSave} from '../dist/model.js';
import {newWorkLog,recordWork,validateWorkLog} from '../dist/work-log.js';
import {workdayPanel} from '../dist/workday-ui.js';
import {workshopStep,workshopEstimate} from '../dist/workshop.js';
import {WORKSHOP_POINTS,workshopWorkPoint,workshopClear} from '../dist/workshop-layout.js';
import {WORK,LOCATIONS} from '../dist/data.js';
import {deliveryTarget} from '../dist/delivery-routes.js';
import {workshopHud,supplyPanel} from '../dist/workshop-ui.js';
const near=(a,b)=>assert.ok(Math.abs(a-b)<1e-6,`${a} != ${b}`);
const button=(label,action,arg='',cls='')=>`<button class="${cls}" data-action="${action}" data-arg="${arg}">${label}</button>`;
function workshop(speed=1){const m=new GameModel(newGame(true));m.s.minute=480;m.s.settings.speed=speed;m.s.position={x:-174,z:43};assert.ok(m.enterInterior('workshop'));m.s.position={...WORKSHOP_POINTS.workshopDesk};m.s.workshop.training=1;return m;}
function step(m){const c=workshopStep(m.s);m.s.position={...WORKSHOP_POINTS[c.point]};assert.ok(m.workshopAction('step',c.correct));for(let i=0;i<1000&&m.s.workshop.active.action;i++)m.updateTime(.1);assert.equal(m.s.workshop.active.action,null);}

test('old saves gain an empty work ledger without inventing income or losing existing receipts',()=>{
 const s=newGame(true);delete s.workLog;const before=s.money;const migrated=validateSave(s);assert.deepEqual(migrated.workLog,newWorkLog());assert.equal(migrated.money,before);assert.deepEqual(new GameModel(s).s.workLog,newWorkLog());assert.match(workdayPanel(s,button),/Frühere Auszahlungen werden nicht/);
});
test('workshop receipts match actual cash, include cancellations and do not repay on reload',()=>{
 const m=workshop(),before=m.s.money;m.workshopAction('accept','stock');while(workshopStep(m.s))step(m);m.s.position={...WORKSHOP_POINTS.workshopDesk};assert.ok(m.workshopAction('finish'));const paid=m.s.money-before;assert.equal(m.s.workLog.entries.length,1);assert.equal(m.s.workLog.entries[0].total,paid);assert.equal(m.s.workLog.days[0].paid,paid);
 const restored=new GameModel(validateSave(JSON.parse(JSON.stringify(m.s))));assert.equal(restored.workshopAction('finish'),false);assert.equal(restored.s.money,m.s.money);restored.workshopAction('accept','check');restored.workshopAction('cancel');assert.equal(restored.s.workLog.entries[0].status,'cancelled');assert.equal(restored.s.workLog.days[0].paid,paid);assert.equal(restored.s.workLog.days[0].unpaid,1);restored.workshopAction('accept','stock');restored.advance(660);assert.equal(restored.s.workLog.entries[0].status,'expired');assert.equal(restored.s.workLog.days[0].unpaid,2);assert.doesNotThrow(()=>validateSave(restored.s));
});
test('courier, street cleaning and warehouse wages share the journal without double counting',()=>{
 for(const type of ['cleaning','warehouse']){const m=new GameModel(newGame(true)),before=m.s.money;m.s.skills.logistics=100;assert.ok(m.startJob(type));while(m.s.job){const i=m.s.job.progress;if(type==='cleaning'){const [x,z]=WORK.cleaning[i];m.s.position={x,z};assert.ok(m.work('trash',i));}else{m.s.position={...WORK.crate};m.work('crate');m.s.position={...WORK.shelves[i]};assert.ok(m.work('shelf',i));}}assert.equal(m.s.workLog.entries[0].type,type);assert.equal(m.s.workLog.entries[0].base,type==='cleaning'?4200:6500);assert.equal(m.s.workLog.entries[0].bonus,type==='cleaning'?420:650);assert.equal(m.s.workLog.days[0].paid,m.s.money-before);m.startJob(type);m.cancelJob();m.startJob(type);m.cancelJob();assert.equal(m.s.workLog.days[0].unpaid,2);assert.doesNotThrow(()=>validateSave(m.s));}
 const m=new GameModel(newGame(true));m.s.position={...deliveryTarget('jobs')};const before=m.s.money;assert.ok(m.acceptContract('stationKiosk'));m.updateTime(3);m.s.position={...deliveryTarget(m.s.job.target)};assert.ok(m.work('deliver'));m.updateTime(3);assert.equal(m.s.job,null);assert.equal(m.s.workLog.entries.length,1);assert.equal(m.s.workLog.days[0].paid,m.s.money-before);assert.equal(m.finishJob(),false);assert.equal(m.s.workLog.entries.length,1);assert.doesNotThrow(()=>validateSave(m.s));
});
test('parts wages are recorded separately even if the associated repair is returned',()=>{
 const m=workshop(),before=m.s.money;m.workshopAction('acceptSupply','tube');m.leaveInterior();m.s.position={...LOCATIONS.find(p=>p.id==='depot')};m.workshopAction('supplyPickup');m.s.settings.speed=4;assert.match(supplyPanel(m.s,button),/Tempo geändert/);m.s.position={x:-174,z:43};m.enterInterior('workshop');m.s.position={...WORKSHOP_POINTS.workshopIntake};assert.ok(m.workshopAction('supplyDeliver'));assert.equal(m.s.workLog.entries[0].type,'parts');assert.equal(m.s.workLog.entries[0].total,m.s.money-before);m.s.position={...WORKSHOP_POINTS.workshopDesk};m.workshopAction('cancel');assert.equal(m.s.workLog.days[0].paid,1000);assert.equal(m.s.workLog.days[0].unpaid,1);assert.doesNotThrow(()=>validateSave(m.s));
});
test('receipt retention is bounded while daily totals include receipts beyond the visible history',()=>{
 const s=newGame(true),cash=s.money;for(let i=0;i<60;i++)recordWork(s,{ref:'sample-'+i,type:'cleaning',base:100,total:100});assert.equal(s.workLog.entries.length,40);assert.equal(s.workLog.days[0].paid,6000);assert.equal(s.workLog.days[0].settled,60);assert.equal(recordWork(s,{ref:'sample-59',type:'cleaning',base:100,total:100}),false);assert.equal(s.money,cash);assert.doesNotThrow(()=>validateWorkLog(s));for(let d=2;d<12;d++){s.day=d;recordWork(s,{ref:'day-'+d,type:'warehouse',base:200,total:200});}assert.equal(s.workLog.days.length,7);assert.doesNotThrow(()=>validateSave(s));
 for(const change of [b=>b.workLog.entries[0].total++,b=>b.workLog.entries[0].ref='<script>',b=>b.workLog.days[0].paid=0,b=>b.workLog.entries.push(b.workLog.entries[0]),b=>b.workLog.entries[0].minute=1440]){const b=structuredClone(s);change(b);assert.throws(()=>validateSave(b));}
});
test('walking to a station follows a clear route at physical speed before work starts at 1x, 4x and 10x',()=>{
 for(const speed of [1,4,10]){const m=workshop(speed);m.workshopAction('accept','check');m.s.workshop.active.variant=0;m.s.position={x:343.5,z:83.3};const a=m.s.workshop.active,c=workshopStep(m.s);assert.ok(m.workshopAction('step',c.correct));const t=a.action,begin=m.s.minute;assert.ok(t.approachDuration>1);
  for(let i=0;i<10;i++){const p={...m.s.position};m.updateTime(.1);assert.ok(Math.hypot(m.s.position.x-p.x,m.s.position.z-p.z)<=.1650001);assert.ok(workshopClear(m.s.position.x,m.s.position.z));assert.equal(t.elapsed,0);}near(m.s.minute-begin,speed);assert.match(workshopHud(m.s),/Geht zum Arbeitsplatz/);
  const restored=new GameModel(validateSave(JSON.parse(JSON.stringify(m.s))));restored.updateTime(.5);near(restored.s.workshop.active.action.elapsed,(1.5-t.approachDuration)*speed);const target=workshopWorkPoint(a,c);near(restored.s.position.x,target.x);near(restored.s.position.z,target.z);const frozen=JSON.stringify(restored.s);restored.updateTime(0);assert.equal(JSON.stringify(restored.s),frozen);
 }
});
test('0.7.1 active operations keep their elapsed time and invalid saved approach paths are rejected',()=>{
 const m=workshop();m.workshopAction('accept','check');m.s.position={...WORKSHOP_POINTS.workshopBike};m.workshopAction('step',workshopStep(m.s).correct);const old=structuredClone(m.s);delete old.workshop.active.action.approachDuration;old.workshop.active.action.elapsed=2;old.workshop.active.action.age=2;const restored=new GameModel(validateSave(old));restored.updateTime(.1);near(restored.s.workshop.active.action.elapsed,2.1);
 const bad=structuredClone(m.s);bad.workshop.active.action.approachDuration=-1;assert.throws(()=>validateSave(bad));
});
test('workbook exposes safe navigation, clear time estimates and no remote payout controls',()=>{
 const m=workshop();m.workshopAction('accept','tube');m.s.interior=null;m.s.inside=false;m.s.position={x:-174,z:43};const html=workdayPanel(m.s,button);assert.match(html,/data-action="navigate"/);assert.doesNotMatch(html,/data-action="workshopFinish"|data-action="workshopStep"/);assert.match(html,/ohne Anreise/);const slow=workshopEstimate(m.s);m.s.settings.speed=10;const fast=workshopEstimate(m.s);assert.ok(fast.minutes>slow.minutes);assert.equal(fast.work,slow.work);m.s.minute=1135;assert.equal(workshopEstimate(m.s).tight,true);assert.match(workdayPanel(m.s,button),/Nur laufende Arbeiten abschließen/);
 const c=new GameModel(newGame(true));c.s.position={...deliveryTarget('jobs')};c.acceptContract('workshopExpress');assert.match(workdayPanel(c.s,button),/Bonusfrist beginnt nach der Paketübernahme/);
});

test('standing on the work marker starts immediately instead of walking backwards and returning',()=>{
 const m=workshop();m.workshopAction('accept','check');const a=m.s.workshop.active,c=workshopStep(m.s),target=workshopWorkPoint(a,c);m.s.position={x:target.x,z:target.z};assert.ok(m.workshopAction('step',c.correct));near(a.action.approachDuration,0);m.updateTime(.1);near(a.action.elapsed,.1);near(m.s.position.x,target.x);near(m.s.position.z,target.z);
});
