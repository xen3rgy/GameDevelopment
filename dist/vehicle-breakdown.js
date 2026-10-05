import {ITEMS,VEHICLES,euro} from './data.js?v=0.8.1';
import {insertItem,removeItem,itemCount} from './inventory.js?v=0.8.1';
import {serviceBay} from './service-layout.js?v=0.8.1';

// Ignition is runtime-only; condition remains the single saved source of damage.
const ignitions=new WeakMap();
export function resetIgnition(v){ignitions.delete(v)}
export function breakdownStatus(v){
 if(!v||v.id==='bike')return {level:0,text:'',top:1,acceleration:1};
 if(v.condition<=5)return {level:3,text:'⛔ MOTOR AUSGEFALLEN · Pannenhilfe nötig',top:0,acceleration:0};
 if(v.condition<20&&ignitions.get(v)?.starting)return {level:2,text:'⚠ STARTPROBLEM · Gas halten',top:.5,acceleration:.55};
 if(v.condition<20)return {level:2,text:'⚠ MOTOR · Notlauf / Startprobleme',top:.5,acceleration:.55};
 if(v.condition<35)return {level:1,text:'⚠ MOTOR · Leistung reduziert',top:.72,acceleration:.8};
 if(v.condition<50)return {level:1,text:'⚠ MOTOR · Service empfohlen',top:1,acceleration:1};
 return {level:0,text:'',top:1,acceleration:1};
}
export function engineReady(v,throttle,dt){
 if(v.id==='bike')return true;
 if(v.fuel<=0||v.condition<=5){resetIgnition(v);return false;}
 if(v.condition>=20){resetIgnition(v);return true;}
 let ignition=ignitions.get(v);
 if(!ignition){ignition={elapsed:0,running:Math.abs(v.speed||0)>.1,starting:false};ignitions.set(v,ignition);}
 if(ignition.running)return true;
 ignition.starting=!!throttle;
 if(throttle)ignition.elapsed+=dt;else ignition.elapsed=0;
 if(ignition.elapsed>=2.5){ignition.running=true;ignition.starting=false;}
 return ignition.running;
}
export const roadsidePrice=(v,action)=>Math.round(({kit:3500,aid:9000,tow:7500}[action]??0)*(action==='kit'?1:({car:1,van:1.3,sport:2}[v?.id]??1)));
export function roadsideAvailable(s,v){return !!v&&['car','van','sport'].includes(v.id)&&!v.stored&&Math.abs(v.speed)<.01&&!s.inside&&!s.transit&&!s.vehicleService&&!s.scavenge?.action&&!s.dailyLife?.action&&!s.workshop?.active?.action&&s.workshop?.active?.supply?.phase!=='carrying'&&!s.job?.fieldAction&&!s.job?.interaction&&!s.job?.carrying&&!s.cafe?.carrying&&s.cafe?.phase!=='open'&&(!s.riding||s.vehicle===v)&&Math.hypot(s.position.x-v.x,s.position.z-v.z)<=6;}
export function roadsideAction(model,uid,action,space={}){
 const s=model.s,v=s.fleet?.find(v=>v.uid===uid);
 if(!roadsideAvailable(s,v)||!['kit','repair','aid','tow'].includes(action))return false;
 if(action==='kit'){
  const inventory=s.inventory.map(slot=>({...slot}));
  if(!insertItem(inventory,'toolkit')||!model.spend(roadsidePrice(v,action)))return false;
  s.inventory=inventory;return true;
 }
 if(action==='repair'){
  if(s.riding||v.condition>=25||!removeItem(s.inventory,'toolkit'))return false;
  v.condition=Math.min(25,v.condition+15);resetIgnition(v);return true;
 }
 if(action==='aid'){
  if(v.condition>=40||!model.spend(roadsidePrice(v,action)))return false;
  v.condition=40;resetIgnition(v);return true;
 }
 const bay=serviceBay(v.fuel<=0?'pump1':'autoWorkshop'),destination=bay.vehicle;
 if(Math.hypot(v.x-destination.x,v.z-destination.z)<1.4||!space.vehicle?.(v,destination)||!space.player?.(bay)||!model.spend(roadsidePrice(v,action)))return false;
 Object.assign(v,destination,{speed:0,parking:null});s.vehicle=v;s.riding=false;s.position={x:bay.x,z:bay.z};s.angle=0;resetIgnition(v);return true;
}
export function roadsidePanel(s,uid,button){
 const v=s.fleet?.find(v=>v.uid===uid);if(!v||v.id==='bike')return '<p>Kein motorisiertes Fahrzeug ausgewählt.</p>';
 const available=roadsideAvailable(s,v),buy=(label,action,disabled=false)=>button(label,'roadsideAction',uid+'|'+action,'secondary',!available||disabled||s.money<roadsidePrice(v,action));
 return `<p class="lead">${VEHICLES[v.id].name} · Zustand ${Math.round(v.condition)} %</p><p>${breakdownStatus(v).text||'Motor betriebsbereit.'}${v.fuel<=0?' · Tank leer: zum Tanken abschleppen lassen.':''}</p><div class="info-box">Am stehenden Fahrzeug (max. 6 m). Laufende Arbeit zuerst beenden. Unter 20 %: Gas 2,5 Sekunden zum Starten halten. Bis 5 %: Motorausfall.</div><div class="cards"><article class="card"><h3>Werkzeug & Ersatzteile</h3><p>Einmaliges Set · +15 Zustand, höchstens 25 %. Zum Reparieren aussteigen. Im Rucksack: ${itemCount(s.inventory,'toolkit')}.</p>${buy('Set kaufen · '+euro(ITEMS.toolkit.price),'kit')}${button('Mit Set reparieren','roadsideAction',uid+'|repair','secondary',!available||s.riding||v.condition>=25||!itemCount(s.inventory,'toolkit'))}</article><article class="card"><h3>Mobile Notfallhilfe</h3><p>Soforthilfe vor Ort: Zustand auf 40 %. Kein Kraftstoff, keine vollständige Reparatur.</p>${buy('Hilfe bestellen · '+euro(roadsidePrice(v,'aid')),'aid',v.condition>=40)}</article><article class="card"><h3>Abschleppdienst</h3><p>Soforttransport mit dir zum Hafenwerk; bei leerem Tank zur Hafenenergie-Zapfsäule. Reparatur und Kraftstoff separat. Eine belegte Bucht verhindert den Transport ohne Kosten.</p>${buy('Abschleppen · '+euro(roadsidePrice(v,'tow')),'tow')}</article></div>${button('Service markieren','findService','autoWorkshop','secondary')}`;
}
