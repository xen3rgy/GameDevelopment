import {recordWork} from './work-log.js?v=0.8.1';
import {DELIVERY_SECONDS} from './delivery-routes.js?v=0.8.1';
// Prices are integer cents; deadlines follow the simulation clock, including midnight.
export const CONTRACTS = [
 {id:'stationKiosk',name:'Morgenausgabe am Gleis',route:['deliveryKiosk'],base:3800,bonus:0,minutes:0,completed:0,reliability:0,description:'Eine Sendung für Yusuf am Bahnhofskiosk. Lerne das neue Viertel ohne Zeitdruck kennen.'},
 {id:'westLoop',name:'Zwischen Kiez und Gleisen',route:['deliveryKiosk','deliveryWorkshop','deliveryB'],base:7800,bonus:0,minutes:0,completed:2,reliability:65,description:'Vom Bahnhofskiosk zur Werkstatt, dann zum Atelier am Kanal. Eine Tour durch beide Stadtteile.'},
 {id:'workshopExpress',name:'Ersatzteil-Express',route:['deliveryWorkshop'],base:4200,bonus:1200,minutes:1,completed:1,reliability:60,description:'Tessa wartet auf ein dringend benötigtes Ersatzteil. Die Bonusfrist berücksichtigt deinen Anfahrtsweg.'},
 {id:'kontor',name:'Unterlagen fürs Nordkontor',route:['deliveryC'],base:2800,bonus:0,minutes:0,completed:0,reliability:0,description:'Eine Dokumententasche. Eine kurze Tour zum Kennenlernen der Hafenstraße.'},
 {id:'bookExpress',name:'Kapitel-Express',route:['deliveryA'],base:3000,bonus:1000,minutes:1,completed:1,reliability:60,description:'Eine dringend erwartete Buchsendung. Ein einzelner Empfänger und eine verbindliche Bonusfrist.'},
 {id:'chapter',name:'Ein guter Anfang',route:['deliveryA'],base:3200,bonus:0,minutes:0,completed:0,reliability:0,description:'Eine Buchsendung für Kapitel. Eine ruhige Tour ohne Zeitlimit.'},
 {id:'canal',name:'Die Kanalrunde',route:['deliveryB','deliveryA'],base:5800,bonus:0,minutes:0,completed:0,reliability:0,description:'Künstlermaterial fürs Atelier, danach Bücher für Kapitel. Zwei persönliche Übergaben.'},
 {id:'city',name:'Einmal durch Lindenstadt',route:['deliveryC','deliveryA','deliveryB'],base:8500,bonus:0,minutes:0,completed:2,reliability:65,description:'Bürounterlagen, Bücher und Material. Drei Empfänger in zwei Stadtvierteln.'},
 {id:'express',name:'Kanal-Express',route:['deliveryC','deliveryB'],base:7000,bonus:2000,minutes:105,completed:4,reliability:75,description:'Zuerst Unterlagen ins Nordkontor, dann zum Atelier. Pünktlichkeit bringt einen Extra-Bonus.'}
];
export const contractById=id=>CONTRACTS.find(c=>c.id===id);
export const newCourier=()=>({completed:0,onTime:0,late:0,cancelled:0,reliability:60,last:null});
export const clockMinutes=s=>s.day*1440+s.minute;
export const canAcceptContract=(c,career,mode='story')=>!!c&&(mode==='sandbox'||career.completed>=c.completed&&career.reliability>=c.reliability);
export const courierRank=c=>c.completed>=4&&c.reliability>=75?'Verlässlicher Kurier':c.completed>=2&&c.reliability>=65?'Im Viertel bekannt':'Neu im Lieferdienst';
export const contractQuote=(c,skill)=>Math.round(c.base*(1+Math.floor(skill/100)*.1));
export const jobStops=j=>j?.contract?contractById(j.contract)?.route: j?.type==='courier'?[j.target]:[];
export function deadlineView(s){
 const j=s.job,c=contractById(j?.contract);if(!c?.minutes||j?.interaction?.kind==='pickup')return null;
 const duration=j.deadlineMinutes??c.minutes,remaining=j.started+duration-clockMinutes(s),end=j.started+duration;
 const minute=Math.floor(end%1440),day=Math.floor(end/1440);
 const speed=[1,4,10].includes(s.settings.speed)?s.settings.speed:1,realSeconds=Math.max(0,remaining/speed);
 return {realSeconds,speed,changedSpeed:j.plan&&j.plan.speed!==speed,urgent:remaining>=0&&realSeconds<20,remaining,late:remaining<0,ratio:Math.max(0,Math.min(1,remaining/duration)),label:`Tag ${day} · ${String(Math.floor(minute/60)).padStart(2,'0')}:${String(minute%60).padStart(2,'0')}`,text:remaining<0?'Zeitbonus verfallen · Grundlohn bleibt':`${Math.ceil(remaining)} Spielmin. bis zum Zeitbonus-Ende`};
}
export function recordCourier(s,{contract=null,status,base=0,vehicleBonus=0,timeBonus=0,stops=0,duration=0,estimatedCosts=0,missedBonus=0,deliveries=[]}){
 const c=s.courier??=newCourier(),delta=status==='cancelled'?-6:status==='late'?-8:5;
 if(status==='cancelled')c.cancelled++;else {c.completed++;if(status==='late')c.late++;else c.onTime++;}
 const before=c.reliability;c.reliability=Math.max(0,Math.min(100,before+delta));
 c.last={estimatedCosts,net:base+vehicleBonus+timeBonus-estimatedCosts,missedBonus,deliveries:structuredClone(deliveries),contract,status,base,vehicleBonus,timeBonus,total:base+vehicleBonus+timeBonus,stops,duration:Math.round(duration),day:s.day,reliabilityDelta:c.reliability-before};
 recordWork(s,{ref:'courier-'+(c.completed+c.cancelled),type:'courier',status:status==='cancelled'?'cancelled':'paid',base,bonus:vehicleBonus+timeBonus,total:base+vehicleBonus+timeBonus,minutes:duration,late:status==='late'});
 return c.last;
}
export function validateCourier(s){
 s.courier??=newCourier();const c=s.courier;
 for(const key of ['completed','onTime','late','cancelled','reliability'])if(!Number.isSafeInteger(c[key])||c[key]<0||c[key]>1e9)throw Error('Ungültiger Kurierfortschritt.');
 if(c.reliability>100||c.completed!==c.onTime+c.late)throw Error('Ungültige Zuverlässigkeit.');
 if(c.last){const r=c.last;if(r.contract!==null&&!contractById(r.contract)||!['completed','late','cancelled'].includes(r.status))throw Error('Ungültige Lieferabrechnung.');
  for(const k of ['base','vehicleBonus','timeBonus','total','stops','duration','day'])if(!Number.isSafeInteger(r[k])||r[k]<0||r[k]>1e12)throw Error('Ungültige Lieferabrechnung.');
  if(r.estimatedCosts!=null&&(!Number.isSafeInteger(r.estimatedCosts)||r.estimatedCosts<0||r.net!==r.total-r.estimatedCosts||!Number.isSafeInteger(r.missedBonus)||r.missedBonus<0||!Array.isArray(r.deliveries)||r.deliveries.length>6||r.deliveries.some(d=>!Number.isFinite(d.at)||!Number.isInteger(d.index)||!contractById(r.contract)?.route.includes(d.target))))throw Error('Ungültige Fahrtkosten.');
  if(r.total!==r.base+r.vehicleBonus+r.timeBonus||!Number.isInteger(r.reliabilityDelta)||Math.abs(r.reliabilityDelta)>8)throw Error('Ungültige Lieferabrechnung.');
 }
 const j=s.job;if(!j?.contract)return;
 const def=contractById(j.contract);if(!def)throw Error('Ungültige Liefertour.');j.deadlineMinutes??=j.contract==='express'?330:0;
 if(!Number.isFinite(j.deadlineMinutes)||j.deadlineMinutes<0||j.deadlineMinutes>20000||j.contract==='express'&&j.deadlineMinutes===0)throw Error('Ungültige Lieferfrist.');
 if(j.protocol===2){
  const plan=j.plan;if(!plan||!['foot','bike','vehicle'].includes(plan.mode)||![1,4,10].includes(plan.speed)||!['distance','travelSeconds','serviceSeconds','bufferSeconds','realSeconds','deadlineMinutes'].every(k=>Number.isSafeInteger(plan[k])&&plan[k]>=0&&plan[k]<20000)||j.deadlineMinutes!==plan.deadlineMinutes||!!def.minutes!==!!j.deadlineMinutes||!Array.isArray(plan.legs)||plan.legs.length!==def?.route.length||plan.legs.some((l,i)=>l.to!==def.route[i]||l.from!==(i?def.route[i-1]:'jobs')||!Number.isSafeInteger(l.distance)||l.distance<0)||plan.distance!==plan.legs.reduce((n,l)=>n+l.distance,0))throw Error('Ungültige Tourplanung.');
  if(!Number.isFinite(j.vanDistance)||j.vanDistance<0||!Number.isFinite(j.operatingCosts)||j.operatingCosts<0||!Array.isArray(j.deliveries)||j.deliveries.length!==j.progress||j.deliveries.some((d,i)=>d.index!==i||d.target!==def.route[i]||!Number.isFinite(d.at)||d.at<j.started||d.at>clockMinutes(s)))throw Error('Ungültige Zustellnachweise.');
  const a=j.interaction;if(a&&(!['pickup','deliver'].includes(a.kind)||!Number.isFinite(a.elapsed)||a.elapsed<0||a.elapsed>=DELIVERY_SECONDS[a.kind]||a.target!==(a.kind==='pickup'?'jobs':j.target)||a.kind==='pickup'&&j.progress!==0||s.inside||s.riding))throw Error('Ungültige Paketübergabe.');
 }else if(j.protocol!=null)throw Error('Ungültige Lieferversion.');
 if(!def||j.type!=='courier'||!Number.isInteger(j.progress)||j.progress<0||j.progress>=def.route.length||j.target!==def.route[j.progress]||!Number.isFinite(j.started)||j.started<0||j.started>clockMinutes(s)||!Number.isSafeInteger(j.quotedBase)||j.quotedBase<def.base||j.quotedBase>1e12||typeof j.vehicleUsed!=='boolean'||s.inventory.filter(i=>i.id==='parcel').reduce((n,i)=>n+i.count,0)!==def.route.length-j.progress)throw Error('Ungültige Liefertour.');
}
