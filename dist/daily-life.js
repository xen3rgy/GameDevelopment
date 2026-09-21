import {HOME_POINTS,HOMES,ITEMS,RECIPES,LOCATIONS,clamp} from './data.js?v=0.7.3-map1';
import {clockMinutes,deadlineView} from './contracts.js?v=0.7.3-map1';
import {heldLoad,fatigue} from './player-movement.js?v=0.7.3-map1';
import {itemCount,insertItem,removeItem,inventoryWeight} from './inventory.js?v=0.7.3-map1';
export const SLEEP_HOURS=[2,4,6,8,10];
export const FRIDGE_SLOTS=12,FRIDGE_WEIGHT=20;
export const fridgeItem=id=>['Nahrung','Getränke','Zutaten'].includes(ITEMS[id]?.category);
export const newDailyLife=()=>({awakeMinutes:600,action:null,lastRest:null});
export {fatigue};
export const clockAt=total=>{total=Math.floor(total+1e-7);const day=Math.floor(total/1440),m=total%1440;return {day,time:String(Math.floor(m/60)).padStart(2,'0')+':'+String(m%60).padStart(2,'0')}};
export function timePreview(s,minutes){
 const start=clockMinutes(s),end=start+minutes,d=deadlineView(s),warnings=[];
 if(d){warnings.push(d.late?'Der Express-Zeitbonus ist bereits verfallen.':minutes>=d.remaining?'Die Expressfrist läuft währenddessen ab. Der Zeitbonus geht verloren.':'Deine Expressfrist läuft weiter: danach bleiben etwa '+Math.floor(d.remaining-minutes)+' Spielminuten.');}
 const c=s.cafe;
 if(c?.phase==='open'){const left=Math.max(0,Math.min(c.duration-c.elapsed,1200-s.minute));warnings.push(minutes>=left?'Dein Café schließt währenddessen. Das Team arbeitet bis zum Schichtende weiter.':'Dein laufender Cafébetrieb wird währenddessen weiter simuliert.');}
 if(Math.floor(end/1440)>s.day)warnings.push('Mitternacht: Tagesabrechnung und gegebenenfalls Miete werden gebucht.');
 if(s.needs.hunger-minutes*.025<10||s.needs.thirst-minutes*.034<10)warnings.push('Deine Vorräte an Sättigung oder Flüssigkeit werden knapp. Iss und trink möglichst vorher.');
 return {start:clockAt(start),end:clockAt(end),minutes,warnings};
}
export function prepareRecipe(s,id){
 const r=Object.hasOwn(RECIPES,id)?RECIPES[id]:null;if(!r)return null;
 const lists=[s.inventory,s.storage,s.fridge||[]].map(v=>structuredClone(v));
 for(const [id,n] of Object.entries(r.ingredients)){let left=n;for(const list of lists){const take=Math.min(itemCount(list,id),left);if(take)removeItem(list,id,take);left-=take;}if(left)return null;}
 let where='Rucksack';if(!insertItem(lists[0],r.result,r.count)){where='Kühlschrank';if(!insertItem(lists[2],r.result,r.count,FRIDGE_SLOTS,FRIDGE_WEIGHT)){where='Wohnungslager';if(!insertItem(lists[1],r.result,r.count,32,100))return null;}}
 return {inventory:lists[0],storage:lists[1],fridge:lists[2],where};
}
export function actionOffer(s,kind,arg){
 const at=p=>p&&Math.hypot(s.position.x-p.x,s.position.z-p.z)<=1.9;
 const home=s.inside&&s.interior==='home'&&s.home;
 const outside=id=>!s.inside&&!s.riding&&Math.hypot(s.position.x-LOCATIONS.find(l=>l.id===id).x,s.position.z-LOCATIONS.find(l=>l.id===id).z)<3.1;
 if(s.dailyLife?.action||s.job?.interaction||s.riding||heldLoad(s)&&!(['sleep','shelterSleep','rest'].includes(kind)&&heldLoad(s)==='parcel'))return null;
 let minutes=0,duration=0,label='',cost=0,item=null;
 if(kind==='sleep'){if(!home||!at(HOME_POINTS.bed)||!SLEEP_HOURS.includes(Number(arg)))return null;minutes=Number(arg)*60;duration=10;label='Schlafen';}
 else if(kind==='shelterSleep'){if(!outside('shelter'))return null;minutes=480;duration=10;label='Im Nachtquartier schlafen';cost=500;}
 else if(kind==='shower'){if(!home||!at(HOME_POINTS.shower))return null;minutes=15;duration=7;label='Duschen';}
 else if(kind==='wash'){if(!outside('shelter'))return null;minutes=20;duration=5;label='Frisch machen';cost=200;}
 else if(kind==='rest'){if(!outside('park'))return null;minutes=120;duration=7;label='Im Park ausruhen';}
 else if(kind==='cook'){if(!home||!at(HOME_POINTS.kitchen)||!prepareRecipe(s,arg))return null;minutes=RECIPES[arg].minutes;duration=6;label=RECIPES[arg].name;}
 else if(kind==='consume'){item=s.inventory[Number(arg)]?.id;if(!ITEMS[item]?.effects)return null;duration=ITEMS[item].category==='Nahrung'?4:3;label=ITEMS[item].name;}
 else return null;
 if(s.money<cost)return null;
 return {kind,arg:kind==='sleep'?Number(arg):kind==='cook'?arg:null,item,label,minutes,duration,cost};
}
export function applyRest(s,minutes,kind){
 const d=s.dailyLife,n=s.needs,sleeping=kind==='sleep'||kind==='shelterSleep';
 if(sleeping){
  const comfort=kind==='shelterSleep'?78:(HOMES.find(h=>h.id===s.home)?.energy||80),fed=n.hunger>=15&&n.thirst>=15,rate=(comfort===100?.17:.14)*(fed?1:.6);
  // Sleep cannot lower an already higher energy value, nor exceed the normal cap.
  n.energy=Math.min(100,n.energy+minutes*rate);n.stress=clamp(n.stress-minutes*.055);
  if(fed)n.health=clamp(n.health+minutes*.012);
  d.awakeMinutes=Math.max(0,d.awakeMinutes-minutes*2);
 }else if(kind==='rest'){n.energy=clamp(n.energy+minutes*.17);n.stress=clamp(n.stress-minutes*.12);d.awakeMinutes=Math.max(0,d.awakeMinutes-minutes*.35);}
 else if(kind==='shower'||kind==='wash'){n.hygiene=clamp(n.hygiene+minutes*(kind==='shower'?7:5));n.stress=clamp(n.stress-minutes*.5);}
}
export function validateDailyLife(s){
 s.fridge??=[];
 if(!Array.isArray(s.fridge)||s.fridge.length>FRIDGE_SLOTS||s.fridge.some(v=>!v||!fridgeItem(v.id)||!Number.isInteger(v.count)||v.count<1||v.count>ITEMS[v.id].stack)||inventoryWeight(s.fridge)>FRIDGE_WEIGHT+.00001)throw Error('Ungültiger Kühlschrank.');
 s.dailyLife??=newDailyLife();const d=s.dailyLife;
 if(!Number.isFinite(d.awakeMinutes)||d.awakeMinutes<0||d.awakeMinutes>2880)throw Error('Ungültige Erholung.');
 if(d.lastRest!=null&&(!Number.isFinite(d.lastRest.minutes)||d.lastRest.minutes<0||d.lastRest.minutes>600||!Number.isFinite(d.lastRest.day)))throw Error('Ungültiger Schlafbericht.');
 const a=d.action;if(!a)return;
 if(!['sleep','shelterSleep','shower','wash','rest','cook','consume'].includes(a.kind)||!Number.isFinite(a.elapsed)||a.elapsed<0||!Number.isFinite(a.duration)||a.duration<3||a.duration>10||a.elapsed>a.duration||!Number.isFinite(a.applied)||a.applied<0||a.applied>a.minutes||!Number.isFinite(a.minutes)||a.minutes<0||a.minutes>600||typeof a.interrupted!=='boolean'||!a.origin||!Number.isFinite(a.origin.x)||!Number.isFinite(a.origin.z)||Math.hypot(a.origin.x-s.position.x,a.origin.z-s.position.z)>.01||!Number.isFinite(a.yaw)||s.riding||s.job?.interaction)throw Error('Ungültige Alltagsaktion.');
 const cash=s.money;let offer;
 try{d.action=null;s.money=Math.max(cash,500);offer=actionOffer(s,a.kind,a.kind==='consume'?s.inventory.findIndex(v=>v.id===a.item):a.arg);}
 finally{d.action=a;s.money=cash;}
 if(!offer||offer.minutes!==a.minutes||offer.duration!==a.duration||offer.item!==a.item)throw Error('Alltagsaktion passt nicht zum Aufenthaltsort oder zur Dauer.');
 if(!a.interrupted&&Math.abs(a.applied-(a.elapsed+1e-8>=a.duration?a.minutes:Math.floor((a.minutes*a.elapsed/a.duration+1e-8)*4)/4))>.00001)throw Error('Ungültiger Aktionsfortschritt.');
 a.label=offer.label;a.cost=offer.cost;
}
