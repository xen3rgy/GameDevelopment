import {overlaps,vehicleBody} from './traffic.js?v=0.8.1';
import {SERVICE_FIXTURES} from './service-layout.js?v=0.8.1';
import {serviceBay,serviceAligned} from './service-layout.js?v=0.8.1';
import {inReach} from './interactions.js?v=0.8.1';
import {REPAIR_RATE} from './fleet.js?v=0.8.1';
export const TANK_LITRES={car:45,van:70,sport:60};
export const fuelPrice=s=>179+((s.day*7)%17)-8+(s.economy==='Aufschwung'?12:s.economy==='Rezession'?-10:0);
export function serviceQuote(s,v,kind){
 if(!v)return null;
 if(kind==='fuel'){const litres=(100-v.fuel)/100*TANK_LITRES[v.id];return litres>0?{litres,unitPrice:fuelPrice(s),gross:Math.ceil(litres*fuelPrice(s)),credit:0,cost:Math.ceil(litres*fuelPrice(s)),duration:8}:null;}
 if(kind!=='repair'||v.condition>=100)return null;
 const gross=Math.ceil((100-v.condition)*REPAIR_RATE[v.id]),credit=Math.min(gross,Math.floor(v.claimCredit||0));
 return {gross,credit,cost:gross-credit,duration:6};
}
export const serviceBusy=s=>!!(s.vehicleService||s.inside||s.riding||s.transit||s.job||s.workshop?.active||s.dailyLife?.action||s.cafe?.phase==='open');
export function vehicleAtBay(s,bay){
 if(!bay)return null;
 const vehicles=(s.fleet||[]).filter(v=>Math.abs(v.speed)<.01&&serviceAligned(v,bay));
 return vehicles.length===1?vehicles[0]:null;
}
export function beginService(model,id){
 const s=model.s,bay=serviceBay(id),v=vehicleAtBay(s,bay);
 if(serviceBusy(s)||!bay||!inReach(s.position,bay)||!v||model.canServiceVehicle&&!model.canServiceVehicle(v))return false;
 if(SERVICE_FIXTURES.some(c=>overlaps(vehicleBody(v),{x:c.x,z:c.z,width:c.w*2,length:c.d*2,angle:0},.08))||(s.fleet||[]).some(o=>o!==v&&!o.stored&&overlaps(vehicleBody(v),vehicleBody(o),.08)))return false;
 const quote=serviceQuote(s,v,bay.kind);if(!quote||!model.spend(quote.cost))return false;
 s.vehicleService={bay:id,uid:v.uid,kind:bay.kind,...quote,elapsed:0,baseFuel:v.fuel,baseCondition:v.condition,baseCredit:v.claimCredit||0,day:s.day,economy:s.economy,origin:{...s.position}};
 model.emit('Service gestartet. Betrag reserviert; Abbruch erstattet nicht erbrachte Leistung.');return true;
}
export function endService(model,cancel=false){
 const s=model.s,a=s.vehicleService;if(!a||!cancel&&a.elapsed<a.duration)return false;
 const v=s.fleet.find(v=>v.uid===a.uid),fraction=cancel?Math.min(1,a.elapsed/a.duration):1;
 let charged=0;
 if(v&&a.kind==='fuel'){v.fuel=a.baseFuel+(100-a.baseFuel)*fraction;charged=Math.min(a.cost,Math.ceil(a.litres*fraction*a.unitPrice));}
 else if(v&&!cancel){v.condition=100;v.claimCredit=0;charged=a.cost;}
 s.money+=a.cost-charged;s.vehicleService=null;
 model.emit(cancel?(a.kind==='fuel'?'Tanken abgebrochen. Nur abgegebener Kraftstoff wurde berechnet.':'Reparatur abgebrochen. Reservierter Betrag vollständig erstattet; Fahrzeugzustand unverändert.'):'Fahrzeugservice abgeschlossen.','success');return true;
}
export function tickService(model,seconds){
 const s=model.s,a=s.vehicleService;if(!a||!Number.isFinite(seconds)||seconds<=0)return false;
 const bay=serviceBay(a.bay),v=vehicleAtBay(s,bay);
 if(!v||v.uid!==a.uid||serviceBusy({...s,vehicleService:null})||!inReach(s.position,bay)||Math.hypot(s.position.x-a.origin.x,s.position.z-a.origin.z)>.1)return endService(model,true);
 a.elapsed=Math.min(a.duration,a.elapsed+seconds);return a.elapsed>=a.duration?endService(model):false;
}
export function validateService(s){
 if(s.vehicleService==null){s.vehicleService=null;return;}
 const a=s.vehicleService,bay=serviceBay(a.bay),v=vehicleAtBay(s,bay),q=v&&serviceQuote({day:a.day,economy:a.economy},v,a.kind);
 if(!bay||bay.kind!==a.kind||!v||v.uid!==a.uid||serviceBusy({...s,vehicleService:null})||!Number.isInteger(a.day)||a.day<1||!['Normal','Aufschwung','Rezession'].includes(a.economy)||!q||!inReach(s.position,bay)||!inReach(a.origin,bay)||Math.hypot(s.position.x-a.origin.x,s.position.z-a.origin.z)>.1||!Number.isFinite(a.elapsed)||a.elapsed<0||a.elapsed>=a.duration||v.fuel!==a.baseFuel||v.condition!==a.baseCondition||v.claimCredit!==a.baseCredit||Object.keys(q).some(k=>a[k]!==q[k]))throw Error('Ungültiger Fahrzeugservice.');
}
