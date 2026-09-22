import {recordWork} from './work-log.js?v=0.8.0';
import {queueWorkshopCollection,validateWorkshopLife} from './workshop-life.js?v=0.8.0';
import {variedWorkshopSteps} from './workshop-orders.js?v=0.8.0';
import {supplyPlan,supplyReceipt} from './workshop-supply.js?v=0.8.0';
import {LOCATIONS} from './data.js?v=0.8.0';
import {WORKSHOP_POINTS,workshopWorkPoint,workshopApproach,atWorkshop,workshopClear} from './workshop-layout.js?v=0.8.0';
export const WORKSHOP_TYPES={
 stock:{name:'Wareneingang',level:0,base:750,xp:12,description:'Lieferschein lesen, Karton kontrollieren und die passenden Teile einsortieren.'},
 check:{name:'Fahrradcheck',level:0,base:1100,xp:16,description:'Bremsen prüfen, die richtige Einstellung vornehmen und die Funktion nachweisen.'},
 tube:{name:'Schlauchwechsel',level:1,base:1650,xp:22,description:'Ein undichtes Rad prüfen, den passenden Schlauch montieren und Druck sowie Sitz kontrollieren.'}
};
export const WORKSHOP_TRAINING=[{name:'Zweirad-Basiskurs',cost:6000,xp:40,minutes:90},{name:'Sicheres Arbeiten',cost:14000,xp:120,minutes:120}];
export const newWorkshop=()=>({xp:0,training:0,serial:0,day:0,taken:0,completed:0,active:null,last:null,expressStock:0,lastDelivery:null,feedback:null,life:null});
export const workshopHours=s=>({open:s.minute>=480&&s.minute<1140,accept:s.minute>=480&&s.minute<1020});
export const workshopNow=s=>s.day*1440+s.minute;
const step=(point,title,hint,choices,correct,minutes,gesture)=>({point,title,hint,choices,correct,minutes,gesture});
export function workshopSteps(a){
 if(a.revision===2)return variedWorkshopSteps(a);
 const variant=a.variant;
 if(a.type==='stock')return [
 step('workshopIntake','Lieferung zählen',`Lieferschein: ${variant?8:6} Schläuche · 28 Zoll. Im Karton liegen ${variant?7:6} Packungen.`,[['complete','Als vollständig buchen'],['short','Fehlmenge dokumentieren']],variant?'short':'complete',12,'inspect'),
 step('workshopStock','Passendes Fach wählen','Auf den Packungen steht ETRTO 28/47–622. Das ist die 28-Zoll-Lieferung.',[['tube28','Fach: 28-Zoll-Schläuche'],['tube26','Fach: 26-Zoll-Schläuche'],['brake','Fach: Bremsbeläge']],'tube28',14,'sort'),
 step('workshopTest','Bestand abgleichen',`Lieferschein und tatsächlicher Zugang müssen übereinstimmen. Verbucht werden nur die ${variant?7:6} vorhandenen Packungen.`,[['actual','Tatsächliche Menge freigeben'],['ordered','Bestellmenge ungeprüft übernehmen']],'actual',10,'inspect')];
 if(a.type==='check')return [
 step('workshopBike','Bremse prüfen',variant?'Der Hebel lässt sich bis zum Lenker ziehen; beide Beläge sind noch ausreichend dick.':'Ein Bremsbelag schleift am Rad. Der Hebelweg und die Belagstärke sind in Ordnung.',[['cable','Bremszug nachstellen'],['center','Bremse zentrieren'],['tire','Reifendruck erhöhen']],variant?'cable':'center',14,'inspect'),
 step('workshopStock','Werkzeug holen','Arbeitskarte: Die Einstellschraube braucht einen 5-mm-Inbusschlüssel.',[['allen5','5-mm-Inbusschlüssel'],['allen3','3-mm-Inbusschlüssel'],['lever','Kunststoff-Reifenheber']],'allen5',7,'collect'),
 step('workshopBike','Bremse einstellen',variant?'Den Zug schrittweise spannen. Am Hebel muss Reserve bleiben.':'Beide Beläge brauchen gleichen Abstand zur Felge.',[['adjust',variant?'Zug schrittweise nachstellen':'Beläge mittig ausrichten'],['force','Schraube bis zum Anschlag anziehen']],'adjust',17,'wrench'),
 step('workshopBike','Sicherheitsprüfung', 'Erst freies Drehen prüfen, dann beide Bremsen unter Handdruck testen.',[['both','Freigang und Bremswirkung prüfen'],['look','Nur Sichtkontrolle']],'both',13,'test')];
 return [
 step('workshopBike','Undichte Stelle suchen',variant?'Am Hinterrad entweicht Luft. Reifenflanke: 37–622; das Vorderrad hält den Druck.':'Am Vorderrad entweicht Luft. Reifenflanke: 40–559; das Hinterrad hält den Druck.',[['front','Vorderrad untersuchen'],['rear','Hinterrad untersuchen']],variant?'rear':'front',14,'inspect'),
 step('workshopStock','Schlauch auswählen',`Reifenfreigabe: ${variant?'37–622 (28 Zoll)':'40–559 (26 Zoll)'}. Schlauchdurchmesser und Reifen müssen zusammenpassen.`,[['tube28','Schlauch 28/47–622'],['tube26','Schlauch 35/47–559']],variant?'tube28':'tube26',8,'collect'),
 step('workshopBench','Reifen vorbereiten','Vor dem Einsetzen muss die Ursache des Lochs entfernt sein. Ein kleines Glasstück steckt im Mantel.',[['remove','Glas entfernen, Mantel innen prüfen'],['insert','Neuen Schlauch sofort einsetzen']],'remove',16,'inspect'),
 step('workshopBike','Schlauch einsetzen','Schlauch leicht vorfüllen, Ventil gerade ausrichten und den Reifen ohne eingeklemmten Schlauch montieren.',[['seat','Sitz prüfen und gleichmäßig montieren'],['force','Reifen mit Metallwerkzeug aufhebeln']],'seat',22,'wrench'),
 step('workshopBike','Abschlussprüfung','Arbeitskarte: 3,5 bar. Radbefestigung, Reifensitz und Druckhaltung kontrollieren.',[['safe','3,5 bar · Befestigung und Dichtheit prüfen'],['max','6 bar · ohne Nachkontrolle']],'safe',16,'test')];
}
export function workshopCarry(s){const a=s.workshop?.active;if(a?.supply?.phase==='carrying'&&!s.riding&&!s.dailyLife?.action)return 'supply';if(s.interior!=='workshop'||!a||a.action&&(a.action.approachDuration==null||a.action.age>=a.action.approachDuration)||s.dailyLife?.action)return null;return a.type==='stock'&&a.step===1?'crate':a.type==='tube'&&a.step>=1&&a.step<=3?'wheel':a.type==='check'&&a.step===2?'tool':null;}
export const workshopStep=s=>s.workshop?.active?workshopSteps(s.workshop.active)[s.workshop.active.step]||null:null;
export function workshopPayout(a,training){const base=WORKSHOP_TYPES[a.type].base,bonus=a.mistakes===0?Math.round(base*(.1+training*.05)):0,deduction=Math.min(Math.round(base*.35),a.mistakes*75);return {base,bonus,deduction,total:base+bonus-deduction};}
function logWorkshop(s,a,r){recordWork(s,{ref:'workshop-'+a.id,type:a.type,status:r.status==='completed'?'paid':r.status,base:r.base,bonus:r.bonus,deduction:r.deduction,total:r.total,minutes:workshopNow(s)-a.started});}
export function expireWorkshop(model){const w=model.s.workshop,a=w?.active;if(!a||workshopNow(model.s)<a.deadline)return;w.last={type:a.type,day:model.s.day,status:'expired',base:0,bonus:0,deduction:0,total:0,mistakes:a.mistakes};logWorkshop(model.s,a,w.last);w.active=null;model.emit('Werkstatt West schließt um 19:00. Der offene Auftrag wurde ohne Auszahlung beendet.','warning');}
export function workshopCommand(model,action,arg){
 const s=model.s,w=s.workshop??=newWorkshop();expireWorkshop(model);
 if(s.dailyLife?.action||s.riding||w.active?.action)return false;
 if(action==='accept'||action==='acceptSupply'){
  const linked=action==='acceptSupply',plan=linked?supplyPlan(s):null;if(linked&&(arg!=='tube'||!plan||s.minute+plan.totalMinutes+110>1140)){model.emit('Für Teilefahrt und Reparatur bleibt heute bei diesem Tempo zu wenig Zeit. Beginne früher oder wähle einen Auftrag mit Lagerbestand.');return false;}
  const type=WORKSHOP_TYPES[arg];if(!Object.hasOwn(WORKSHOP_TYPES,arg)||!atWorkshop(s,'workshopDesk'))return false;
  if(w.active||s.job||s.cafe?.phase==='open'&&s.cafe.config.staff<2){model.emit('Beende zuerst den laufenden Auftrag oder deine unbesetzte Café-Schicht.');return false;}
  if(!workshopHours(s).accept){model.emit('Neue Aufträge gibt es von 08:00 bis 17:00. Abschluss bis 19:00.');return false;}
  if(w.training<type.level||s.needs.energy<15){model.emit(w.training<type.level?'Dafür brauchst du den Zweirad-Basiskurs.':'Du bist zu müde für sicheres Arbeiten.');return false;}
  if(w.day!==s.day){w.day=s.day;w.taken=0;}if(w.taken>=4){model.emit('Für heute sind alle vier Aushilfsaufträge vergeben.');return false;}
  w.taken++;w.serial++;w.active={type:arg,id:w.serial,variant:((w.serial*37+s.day*17+Math.floor(s.minute)*13)>>>0)%12,revision:2,workMinutes:0,step:0,mistakes:0,started:workshopNow(s),deadline:s.day*1440+1140,action:null};if(linked)w.active.supply={phase:'requested',started:null,deadline:null,returnMinutes:plan.returnMinutes,speed:plan.speed};
  if(!linked&&arg==='tube'&&w.expressStock>0){w.expressStock--;w.active.expressPart=true;}
  model.emit(type.name+' angenommen. Die Arbeitskarte zeigt deinen nächsten Arbeitsplatz.','success');return true;
 }
 if(action==='train'){
  const t=WORKSHOP_TRAINING[w.training];if(!atWorkshop(s,'workshopDesk')||!t||w.active||s.job||s.cafe?.phase==='open'||!workshopHours(s).open||s.minute+t.minutes>1140||w.xp<t.xp){model.emit('Für den Kurs fehlen Erfahrung, freie Zeit oder ein freier Arbeitsplatz.');return false;}
  if(!model.spend(t.cost)){model.emit('Für diesen Kurs fehlt das Geld.');return false;}w.training++;model.advance(t.minutes);model.emit(t.name+' abgeschlossen.','success');return true;
 }
 const a=w.active;if(!a)return false;
 if(action==='supplyPickup'){
  const depot=LOCATIONS.find(l=>l.id==='depot'),p=a.supply;
  if(!p||p.phase!=='requested'||s.inside||Math.hypot(s.position.x-depot.x,s.position.z-depot.z)>3.1)return false;
  const plan=supplyPlan(s);if(!plan||workshopNow(s)+plan.returnMinutes+95>a.deadline){model.emit('Für Rückweg und Reparatur bleibt bei diesem Tempo zu wenig Zeit. Du kannst das Tempo reduzieren oder den Auftrag zurückgeben.');return false;}
  p.phase='carrying';p.started=workshopNow(s);p.returnMinutes=plan.returnMinutes;p.speed=plan.speed;p.deadline=Math.min(a.deadline,p.started+p.returnMinutes);model.emit('Ersatzteil übernommen. Zurück zur Werkstatt: 8 € Fahrtlohn + 2 € bei pünktlicher Übergabe.','success');return true;
 }
 if(action==='supplyDeliver'){
  if(a.supply?.phase!=='carrying'||!atWorkshop(s,'workshopIntake'))return false;
  const r=supplyReceipt(s);a.supply.phase='received';w.lastDelivery=r;recordWork(s,{ref:'parts-'+a.id,type:'parts',base:r.base,bonus:r.bonus,total:r.total,minutes:r.duration,late:r.late});model.earn(r.total);model.emit('Teilefahrt separat bezahlt: '+(r.total/100).toFixed(2)+' €'+(r.late?' · Expressbonus verfallen.':'.')+' Jetzt kann die Reparatur weitergehen.','success');return true;
 }
 if(action==='cancel'){if(!atWorkshop(s,'workshopDesk'))return false;w.last={type:a.type,day:s.day,status:'cancelled',base:0,bonus:0,deduction:0,total:0,mistakes:a.mistakes};logWorkshop(s,a,w.last);w.active=null;model.emit('Auftrag zurückgegeben. Keine Auszahlung; der Tagesplatz bleibt belegt.');return true;}
 if(action==='finish'){
  if(workshopStep(s)||!atWorkshop(s,'workshopDesk'))return false;
  const result=workshopPayout(a,w.training);w.last={...result,type:a.type,day:s.day,status:'completed',mistakes:a.mistakes,minutes:Math.round(workshopNow(s)-a.started),workMinutes:Math.round(a.workMinutes??0),revision:a.revision??1,variant:a.variant};logWorkshop(s,a,w.last);queueWorkshopCollection(s,a);w.active=null;w.completed++;w.xp+=WORKSHOP_TYPES[a.type].xp;s.stats.jobs++;s.xp+=WORKSHOP_TYPES[a.type].xp;model.earn(result.total);model.checkQuests();model.emit('Tessa nimmt den Auftrag ab · '+(result.total/100).toFixed(2)+' € ausgezahlt.','success');return true;
 }
 const current=workshopStep(s);
 if(action!=='step'||!current||!atWorkshop(s,current.point)||!current.choices.some(c=>c[0]===arg))return false;
 if(current.point==='workshopStock'&&a.supply&&a.supply.phase!=='received'){model.emit('Dieses Ersatzteil fehlt. Hole die reservierte Packung am Westhafen und übergib sie am Wareneingang.');return false;}
 const correct=current.correct===arg;
 a.action={step:a.step,choice:arg,correct,elapsed:0,age:0,duration:(correct?current.minutes:6)*(1-w.training*.08),origin:{...s.position},approachDuration:workshopApproach(a,current,s.position).duration};
 model.onChange();return true;
}
export function tickWorkshop(model,seconds){
 const s=model.s,w=s.workshop,a=w?.active,task=a?.action;if(!task||!Number.isFinite(seconds)||!(seconds>0))return;
 if(s.interior!=='workshop'){a.action=null;return;}
 const current=workshopStep(s),point=workshopWorkPoint(a,current);const previousAge=task.age;task.age+=seconds;const working=task.approachDuration==null?seconds:Math.max(0,task.age-task.approachDuration)-Math.max(0,previousAge-task.approachDuration),before=task.elapsed;task.elapsed=Math.min(task.duration,task.elapsed+working*s.settings.speed);
 a.workMinutes=(a.workMinutes??0)+task.elapsed-before;
 if(task.approachDuration!=null){const approach=workshopApproach(a,current,task.origin,task.age);s.position={x:approach.x,z:approach.z};s.angle=approach.angle-Math.PI;}else {const t=Math.min(1,task.age/.35),u=t*t*(3-2*t),base=WORKSHOP_POINTS[current.point],pivot=current.point==='workshopBike'?base:point,first=Math.hypot(task.origin.x-pivot.x,task.origin.z-pivot.z),last=Math.hypot(point.x-pivot.x,point.z-pivot.z),travel=u*(first+last),start=travel<=first?task.origin:pivot,end=travel<=first?pivot:point,fraction=travel<=first?(first?travel/first:1):(last?(travel-first)/last:1);s.position={x:start.x+(end.x-start.x)*fraction,z:start.z+(end.z-start.z)*fraction};s.angle=point.angle-Math.PI;}
 if(task.elapsed<task.duration)return;
 w.feedback={title:current.title,status:task.correct?'correct':'rework',at:workshopNow(s)};
 a.action=null;if(task.correct){a.step++;s.needs.energy=Math.max(0,s.needs.energy-1.5);s.needs.hygiene=Math.max(0,s.needs.hygiene-.5);model.emit(workshopStep(s)?'Arbeitsschritt erledigt. Weiter zur nächsten Station.':'Fertig geprüft. Gehe für die Abnahme zu Tessa.','success');}
 else{a.mistakes=Math.min(100,a.mistakes+1);s.needs.stress=Math.min(100,s.needs.stress+1);model.emit('Tessa stoppt den Arbeitsschritt. '+current.hint+' · 0,75 € Nacharbeit, begrenzt auf 35 % des Grundlohns.','warning');}
}
export function validateWorkshop(s){
 const w=s.workshop??=newWorkshop(),integer=(n,max=1e8)=>Number.isInteger(n)&&n>=0&&n<=max;
 if(!w||typeof w!=='object'||!['xp','training','serial','day','taken','completed'].every(k=>integer(w[k]))||w.training>2||w.taken>4)throw Error('Ungültiger Werkstattfortschritt.');
 validateWorkshopLife(w);w.expressStock??=0;w.lastDelivery??=null;w.feedback??=null;
 if(!integer(w.expressStock,2))throw Error('Ungültiger Ersatzteilbestand.');
 if(w.feedback&&(!['correct','rework'].includes(w.feedback.status)||typeof w.feedback.title!=='string'||w.feedback.title.length>100||!Number.isFinite(w.feedback.at)))throw Error('Ungültige Arbeitsrückmeldung.');
 if(w.lastDelivery){const r=w.lastDelivery;if(!['order','day','base','bonus','total','duration'].every(k=>integer(r[k]))||r.base!==800||![0,200].includes(r.bonus)||r.total!==r.base+r.bonus||typeof r.late!=='boolean')throw Error('Ungültige Teilefahrt-Abrechnung.');}
 const a=w.active;if(a){
  if(!Object.hasOwn(WORKSHOP_TYPES,a.type)||!integer(a.id)||!integer(a.variant,a.revision===2?11:1)||a.revision!=null&&a.revision!==2||!integer(a.step,workshopSteps(a).length)||!integer(a.mistakes,100)||![a.started,a.deadline].every(Number.isFinite)||a.started<0||a.deadline<a.started||w.training<WORKSHOP_TYPES[a.type].level||s.job)throw Error('Ungültiger Werkstattauftrag.');
  if(a.workMinutes!=null&&(!Number.isFinite(a.workMinutes)||a.workMinutes<0||a.workMinutes>1e8))throw Error('Ungültige Arbeitszeit.');
  if(a.supply){const p=a.supply;if(a.type!=='tube'||a.revision!==2||!['requested','carrying','received'].includes(p.phase)||![1,4,10].includes(p.speed)||!Number.isFinite(p.returnMinutes)||p.returnMinutes<=0||p.returnMinutes>2000||p.phase!=='requested'&&(![p.started,p.deadline].every(Number.isFinite)||p.started<a.started||p.deadline<p.started||p.deadline>a.deadline)||p.phase!=='received'&&a.step>1)throw Error('Ungültige Ersatzteilfahrt.');}
  const t=a.action,c=workshopSteps(a)[a.step];if(t){
   if(t.approachDuration!=null&&(!Number.isFinite(t.approachDuration)||!t.origin||!c||Math.abs(t.approachDuration-workshopApproach(a,c,t.origin).duration)>1e-6))throw Error('Ungültiger Weg zum Arbeitsplatz.');
   if(!c||t.step!==a.step||!c.choices.some(v=>v[0]===t.choice)||t.correct!==(t.choice===c.correct)||!Number.isFinite(t.duration)||!Number.isFinite(t.elapsed)||!Number.isFinite(t.age)||t.age<0||t.elapsed<0||t.elapsed>t.duration||Math.abs(t.duration-(t.correct?c.minutes:6)*(1-w.training*.08))>1e-6||s.interior!=='workshop'||!t.origin||!workshopClear(t.origin.x,t.origin.z)||!atWorkshop({...s,position:t.origin},c.point)||s.dailyLife?.action)throw Error('Ungültiger Arbeitsschritt.');
  }
 }
 if(w.last){const r=w.last;if(!Object.hasOwn(WORKSHOP_TYPES,r.type)||!['completed','cancelled','expired'].includes(r.status)||!['day','base','bonus','deduction','total','mistakes'].every(k=>integer(r[k]))||r.total!==r.base+r.bonus-r.deduction||r.minutes!=null&&!integer(r.minutes)||r.workMinutes!=null&&!integer(r.workMinutes))throw Error('Ungültige Werkstattabrechnung.');}
}

export function workshopEstimate(s,a=s.workshop.active){
 if(!a)return null;const steps=workshopSteps(a).slice(a.step??0);let work=0,travel=0,from=s.interior==='workshop'?s.position:WORKSHOP_POINTS.workshopExit;
 for(let i=0;i<steps.length;i++){const c=steps[i],p=workshopWorkPoint(a,c);work+=i===0&&a.action?a.action.duration-a.action.elapsed:c.minutes*(1-s.workshop.training*.08);travel+=Math.abs(p.x-from.x)+Math.abs(p.z-from.z);from=p;}
 const desk=WORKSHOP_POINTS.workshopDesk;travel+=Math.abs(desk.x-from.x)+Math.abs(desk.z-from.z);
 const minutes=Math.ceil(work+(travel/2.45+steps.length*2)*s.settings.speed),remaining=(a.deadline??s.day*1440+1140)-workshopNow(s);
 return {work:Math.ceil(work),minutes,remaining:Math.max(0,Math.ceil(remaining)),tight:minutes>remaining};
}
