import {VEHICLES,ITEMS,LOCATIONS} from './data.js?v=0.8.1';
import {inventoryWeight,transferItem} from './inventory.js?v=0.8.1';
import {exteriorContains} from './city-layout.js?v=0.8.1';
import {PARKING_SPACES,parkingById} from './mobility-layout.js?v=0.8.1';
import {inReach} from './interactions.js?v=0.8.1';
import {overlaps,vehicleBody} from './traffic.js?v=0.8.1';
export const FLEET_LIMIT=6;
export const TRUNKS={bike:{slots:4,kg:6},car:{slots:12,kg:40},van:{slots:24,kg:120},sport:{slots:8,kg:25}};
export const PREMIUM={bike:0,car:250,van:400,sport:700};
export const REPAIR_RATE={bike:10,car:120,van:155,sport:240};
export const ownedVehicles=s=>s.fleet??(s.vehicle?[s.vehicle]:[]);
export const availableVehicles=s=>ownedVehicles(s).filter(v=>!v.stored);
export const trunkPoint=v=>({x:v.x-Math.sin(v.angle)*(v.id==='van'?2.7:v.id==='bike'?1.1:2.2),z:v.z-Math.cos(v.angle)*(v.id==='van'?2.7:v.id==='bike'?1.1:2.2)});
export function initFleet(s){
 if(!s.fleet||(Array.isArray(s.fleet)&&s.fleet.length===0&&s.vehicle&&!s.vehicle.uid)){s.fleet=s.vehicle?[s.vehicle]:[];s.fleetSerial=0;}
 s.fleetSerial=Math.max(s.fleetSerial||0,s.fleet.length,...s.fleet.map(v=>/^v\d+$/.test(v?.uid)?Number(v.uid.slice(1)):0));s.usedPurchases??=[];
 for(const [i,v] of s.fleet.entries()){v.uid??='v'+(i+1);v.trunk??=[];v.odometer??=0;v.purchasePrice??=VEHICLES[v.id]?.cost??0;v.insured??=false;v.claimCredit??=0;v.stored??=false;v.parking??=null;}
 if(s.vehicle){const v=s.fleet.find(v=>v.uid===s.vehicle.uid);if(v)s.vehicle=v;}
}
export function validateFleet(s){
 initFleet(s);if(!Array.isArray(s.fleet)||s.fleet.length>FLEET_LIMIT||!Number.isSafeInteger(s.fleetSerial)||s.fleetSerial<0||s.fleetSerial>1e9)throw Error('Ungültige Garage.');
 const ids=new Set(),parking=new Set();
 for(const v of s.fleet){
  if(!v||!Object.hasOwn(VEHICLES,v.id)||typeof v.uid!=='string'||!/^v\d+$/.test(v.uid)||ids.has(v.uid))throw Error('Ungültige Fahrzeug-ID.');ids.add(v.uid);
  if(!['x','z','angle','speed','fuel','condition','odometer','purchasePrice','claimCredit'].every(k=>Number.isFinite(v[k]))||!exteriorContains(v.x,v.z,-1)||Math.abs(v.speed)>35||v.fuel<0||v.fuel>100||v.condition<0||v.condition>100||v.odometer<0||v.odometer>1e7||!Number.isInteger(v.purchasePrice)||v.purchasePrice<0||v.purchasePrice>VEHICLES[v.id].cost||v.claimCredit<0||v.claimCredit>12000||typeof v.insured!=='boolean'||typeof v.stored!=='boolean')throw Error('Ungültige Fahrzeugdaten.');
  const t=TRUNKS[v.id];if(!Array.isArray(v.trunk)||v.trunk.length>t.slots||v.trunk.some(p=>!p||!Object.hasOwn(ITEMS,p.id)||p.id==='parcel'||!Number.isInteger(p.count)||p.count<1||p.count>ITEMS[p.id].stack)||inventoryWeight(v.trunk)>t.kg+.00001)throw Error('Ungültiger Kofferraum.');
  const spot=parkingById(v.parking);if(spot&&parking.has(v.parking)&&Math.hypot(v.x-spot.x,v.z-spot.z)<.6){v.stored=true;v.speed=0;v.parking=null;if(s.vehicle===v){s.vehicle=null;s.riding=false;}}if(!spot||v.stored||Math.hypot(v.x-spot.x,v.z-spot.z)>.6||Math.abs(v.speed)>.01||parking.has(v.parking))v.parking=null;else parking.add(v.parking);
 }
 if(s.vehicle&&!ids.has(s.vehicle.uid))throw Error('Aktives Fahrzeug fehlt.');
 if(s.riding&&(!s.vehicle||s.vehicle.stored||s.inside))throw Error('Ungültiger Fahrerzustand.');
 if(!Array.isArray(s.usedPurchases)||s.usedPurchases.length>64||s.usedPurchases.some(id=>typeof id!=='string'||!/^[0-9]+-(car|van|sport)$/.test(id)))throw Error('Ungültige Gebrauchtwagenkäufe.');
}
export function usedOffers(day){return ['car','van','sport'].map((id,i)=>{const n=((day*7919+i*104729)>>>0),condition=48+n%37,odometer=38000+n%145000,price=Math.round(VEHICLES[id].cost*(.30+condition*.0035));return {key:day+'-'+id,id,condition,odometer,price};});}
export const resale=v=>Math.round(Math.min(v.purchasePrice??VEHICLES[v.id].cost,VEHICLES[v.id].cost)*.65*v.condition/100);
export const atGarage=s=>!s.inside&&!s.riding&&!s.transit&&inReach(s.position,LOCATIONS.find(l=>l.id==='garage'));
export function freeParking(s,spaces=PARKING_SPACES,ignore=null){return spaces.find(p=>!availableVehicles(s).some(v=>v!==ignore&&overlaps(vehicleBody(v),vehicleBody({...p,id:'van'}),.25)));}
export function purchaseVehicle(model,id,offerKey=null){
 const s=model.s;initFleet(s);if(s.vehicleService||s.job?.fieldAction||s.dailyLife?.action||s.workshop?.active?.action||!atGarage(s)||!Object.hasOwn(VEHICLES,id)||s.fleet.length>=FLEET_LIMIT)return false;
 const offer=offerKey?usedOffers(s.day).find(v=>v.key===offerKey&&v.id===id):null;
 if(offerKey&&(!offer||s.usedPurchases.includes(offerKey)))return false;
 const spot=freeParking(s,PARKING_SPACES.filter(p=>p.garage));if(!spot){model.emit('Abholplätze belegt. Parke ein Fahrzeug um oder lagere es in der Garage ein.');return false;}
 const price=offer?.price??VEHICLES[id].cost;if(!model.spend(price)){model.emit('Nicht genug Bargeld.');return false;}
 const v={uid:'v'+(++s.fleetSerial),id,x:spot.x,z:spot.z,angle:spot.angle,speed:0,fuel:offer?45:100,condition:offer?.condition??100,odometer:offer?.odometer??0,purchasePrice:price,insured:false,claimCredit:0,trunk:[],parking:spot.id,stored:false};
 s.fleet.push(v);s.vehicle=v;if(offerKey)s.usedPurchases=[...s.usedPurchases,offerKey].slice(-64);model.emit(VEHICLES[id].name+' steht auf '+spot.name+'.','success');model.checkQuests();return true;
}
export function fleetCommand(model,action,uid,findExit=null){
 const s=model.s;initFleet(s);const v=s.fleet.find(v=>v.uid===uid);if(s.vehicleService||!v||s.riding||s.transit||s.job?.fieldAction||s.dailyLife?.action||s.workshop?.active?.action)return false;
 if(action==='select'){if(v.stored)return false;s.vehicle=v;return true;}
 if(action==='insurance'){if(v.id==='bike')return false;if(!v.insured&&!model.spend(PREMIUM[v.id]))return false;v.insured=!v.insured;model.emit(v.insured?'Versicherung aktiv. Erste Tagesprämie bezahlt; Folgetage um Mitternacht.':'Versicherung beendet.');return true;}
 if(!atGarage(s))return false;
 if(action==='retrieve'){if(!v.stored)return false;const p=freeParking(s,PARKING_SPACES.filter(p=>p.garage));if(!p)return false;Object.assign(v,{x:p.x,z:p.z,angle:p.angle,speed:0,parking:p.id,stored:false});s.vehicle=v;return true;}
 if(v.stored&&action!=='sell')return false;
 if(!v.stored&&Math.hypot(v.x-76,v.z-12)>18){model.emit('Bringe dieses Fahrzeug zum Mobilwerk.');return false;}
 if(action==='store'){v.stored=true;v.parking=null;v.speed=0;if(s.vehicle===v)s.vehicle=null;return true;}
 if(action==='sell'){if(v.trunk.length){model.emit('Leere zuerst den Kofferraum.');return false;}model.earn(resale(v));s.fleet=s.fleet.filter(o=>o!==v);if(s.vehicle===v)s.vehicle=null;model.checkQuests();return true;}
 if(action==='fuel'||action==='repair'){model.emit('Bitte nutze Hafenenergie / Hafenwerk am Westhafen.');return false;}
 return false;
}
export function transferTrunk(model,uid,direction,index,count){
 const s=model.s,v=ownedVehicles(s).find(v=>v.uid===uid);if(s.vehicleService||!v||v.stored||s.inside||s.riding||s.transit||s.dailyLife?.action||s.job?.interaction||s.job?.fieldAction||s.workshop?.active?.action||!inReach(s.position,trunkPoint(v),'trunk'))return false;
 const deposit=direction==='deposit';if(!deposit&&direction!=='withdraw')return false;const t=TRUNKS[v.id];return transferItem(deposit?s.inventory:v.trunk,deposit?v.trunk:s.inventory,index,count,deposit?t.slots:16,deposit?t.kg:20);
}
export function vehicleTravel(v,metres,impactSpeed=0){
 if(!Number.isFinite(metres)||metres<0)return;v.odometer=(v.odometer||0)+metres/1000;
 v.condition=Math.max(0,v.condition-metres*(v.id==='bike'?.00015:.0007));if(v.id!=='bike')v.fuel=Math.max(0,v.fuel-metres*.0015);
 if(impactSpeed>3){const damage=Math.min(v.condition,(impactSpeed-3)*.65);v.condition-=damage;if(v.insured)v.claimCredit=Math.min(12000,(v.claimCredit||0)+damage*REPAIR_RATE[v.id]*.7);}
 if(metres>.001)v.parking=null;
}
export function insuranceDay(model){for(const v of ownedVehicles(model.s))if(v.insured&&!model.spend(PREMIUM[v.id])){v.insured=false;model.emit('Versicherung für '+VEHICLES[v.id].name+' ruht: Tagesprämie nicht bezahlt.','warning');}}
export function parkVehicle(model,canStand,canPark){
 const s=model.s,v=s.vehicle;if(s.vehicleService||!s.riding||!v||Math.abs(v.speed)>.5||s.transit)return false;
 const spots=PARKING_SPACES.filter(p=>Math.hypot(v.x-p.x,v.z-p.z)<7).sort((a,b)=>Math.hypot(v.x-a.x,v.z-a.z)-Math.hypot(v.x-b.x,v.z-b.z));
 for(const p of spots){if(!freeParking(s,[p],v)||!canPark(v,p.x,p.z,p.angle)||!canStand(p.exit.x,p.exit.z,.4))continue;Object.assign(v,{x:p.x,z:p.z,angle:p.angle,speed:0,parking:p.id});s.riding=false;s.position={...p.exit};s.angle=p.angle-Math.PI;model.emit('Geparkt: '+p.name+'.','success');return true;}
 model.emit('Kein freier, sicher erreichbarer Stellplatz in der Nähe. Halte neben einer Markierung an.');return false;
}
