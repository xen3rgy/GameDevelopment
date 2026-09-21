import {ITEMS,VEHICLES} from './data.js?v=0.7.3-map1';
import {inventoryWeight} from './inventory.js?v=0.7.3-map1';

export const MAX_OWNED_VEHICLES=5;
export const GARAGE_EXIT={x:76,z:9,angle:Math.PI};
export const GARAGE_SPOTS=[
 {id:'garage-1',name:'Mobilwerk · Stellplatz 1',x:68,z:8.4,angle:Math.PI,garage:true},
 {id:'garage-2',name:'Mobilwerk · Stellplatz 2',x:73.3,z:8.4,angle:Math.PI,garage:true},
 {id:'garage-3',name:'Mobilwerk · Stellplatz 3',x:78.6,z:8.4,angle:Math.PI,garage:true},
 {id:'garage-4',name:'Mobilwerk · Stellplatz 4',x:83.9,z:8.4,angle:Math.PI,garage:true}
];
export const PARKING_SPOTS=[
 ...GARAGE_SPOTS,
 {id:'market-parking',name:'MARKT 24 · Kurzparken',x:-48,z:-8.6,angle:Math.PI/2},
 {id:'station-parking',name:'Bahnhof West · Parkplatz',x:-158,z:-8.6,angle:Math.PI/2},
 {id:'cafe-parking',name:'Café Morgen · Straßenplatz',x:86,z:-8.6,angle:Math.PI/2},
 {id:'depot-parking',name:'Westhafen · Parkplatz',x:-50,z:-73,angle:0}
];
export const VEHICLE_EXTRAS={
 bike:{trunkSlots:2,trunkWeight:5,insurance:0,wear:.0012,fuelRate:0,repairUnit:10,storageName:'Gepäckträger'},
 car:{trunkSlots:8,trunkWeight:35,insurance:220,wear:.0018,fuelRate:.012,repairUnit:120,storageName:'Kofferraum'},
 van:{trunkSlots:16,trunkWeight:90,insurance:340,wear:.0022,fuelRate:.015,repairUnit:145,storageName:'Laderaum'},
 sport:{trunkSlots:4,trunkWeight:18,insurance:690,wear:.0028,fuelRate:.020,repairUnit:230,storageName:'Kofferraum'}
};
export const newMobility=()=>({serial:0,usedBought:[],transitTrips:0,lastTransit:null,insurancePaid:0});

const safeInt=n=>Number.isSafeInteger(n)&&n>=0;
const nextUid=s=>{s.mobility.serial=(s.mobility.serial||0)+1;return 'veh-'+s.mobility.serial};
export function ensureMobility(s){
 s.mobility??=newMobility();s.garageVehicles??=[];
 if(!safeInt(s.mobility.serial))s.mobility.serial=0;
 if(!Array.isArray(s.mobility.usedBought))s.mobility.usedBought=[];
 if(!safeInt(s.mobility.transitTrips))s.mobility.transitTrips=0;
 if(!safeInt(s.mobility.insurancePaid))s.mobility.insurancePaid=0;
 const normalize=v=>{
  if(!v)return null;
  v.uid??=nextUid(s);v.trunk??=[];v.purchasePrice??=VEHICLES[v.id]?.cost??0;v.mileage??=0;v.used=!!v.used;
  v.insured=v.id==='bike'?true:!!v.insured;v.parkingSpot??=null;v.speed=Number.isFinite(v.speed)?v.speed:0;
  return v;
 };
 if(s.vehicle)normalize(s.vehicle);
 s.garageVehicles=s.garageVehicles.map(normalize).filter(Boolean);
 // Garage vehicles always receive unique physical bays. This also repairs saves produced by
 // early 0.7.5 builds that could assign the same bay twice.
 const used=new Set();
 for(const v of s.garageVehicles){
  let spot=GARAGE_SPOTS.find(p=>p.id===v.parkingSpot&&!used.has(p.id));
  if(!spot)spot=GARAGE_SPOTS.find(p=>!used.has(p.id));
  if(spot){placeInSpot(v,spot);used.add(spot.id)}
 }
 // An active vehicle only owns a parking reservation while it is actually still on that marker.
 // Driving away from an older save must not leave a ghost-occupied bay behind.
 if(s.vehicle?.parkingSpot){
  const spot=PARKING_SPOTS.find(p=>p.id===s.vehicle.parkingSpot);
  if(!spot||used.has(spot.id)||Math.hypot(s.vehicle.x-spot.x,s.vehicle.z-spot.z)>1.5)s.vehicle.parkingSpot=null;
 }
 return s;
}
export const allVehicles=s=>[s.vehicle,...(s.garageVehicles||[])].filter(Boolean);
export const vehicleByUid=(s,uid)=>allVehicles(s).find(v=>v.uid===uid)||null;
export const ownedVehicleCount=s=>allVehicles(s).length;
export const vehicleExtra=v=>VEHICLE_EXTRAS[v?.id]||VEHICLE_EXTRAS.car;
export const trunkLimits=v=>({slots:vehicleExtra(v).trunkSlots,weight:vehicleExtra(v).trunkWeight,name:vehicleExtra(v).storageName});
export const insurancePremium=v=>v?.id==='bike'?0:vehicleExtra(v).insurance;
export const repairPrice=v=>Math.ceil(Math.max(0,100-v.condition)*vehicleExtra(v).repairUnit*(v.insured?.62:1));
export const resaleValue=v=>Math.max(0,Math.round((v.purchasePrice||VEHICLES[v.id].cost)*(.48+.27*v.condition/100)));
export const dailyInsuranceCost=s=>allVehicles(s).reduce((n,v)=>n+(v.insured?insurancePremium(v):0),0);
export const nearestParkingSpot=(position,maxDistance=7)=>PARKING_SPOTS.map(p=>({...p,distance:Math.hypot(position.x-p.x,position.z-p.z)})).filter(p=>p.distance<=maxDistance).sort((a,b)=>a.distance-b.distance)[0]||null;

export function parkingSpotOccupied(s,spotId,ignoreUid=null){
 return allVehicles(s).some(v=>v.uid!==ignoreUid&&v.parkingSpot===spotId);
}
export function freeGarageSpot(s,ignoreUid=null){
 return GARAGE_SPOTS.find(p=>!parkingSpotOccupied(s,p.id,ignoreUid))||null;
}
export function placeInSpot(v,spot){v.x=spot.x;v.z=spot.z;v.angle=spot.angle;v.speed=0;v.parkingSpot=spot.id;return v}
export function createOwnedVehicle(s,id,options={}){
 const def=VEHICLES[id];if(!def||ownedVehicleCount(s)>=MAX_OWNED_VEHICLES)return null;
 const v={uid:nextUid(s),id,fuel:id==='bike'?100:(options.fuel??100),condition:options.condition??100,x:GARAGE_EXIT.x,z:GARAGE_EXIT.z,angle:GARAGE_EXIT.angle,speed:0,mileage:options.mileage??0,trunk:[],insured:id==='bike',used:!!options.used,purchasePrice:options.purchasePrice??def.cost,parkingSpot:null};
 if(!s.vehicle){s.vehicle=v;return v}
 const spot=freeGarageSpot(s);if(!spot)return null;placeInSpot(v,spot);s.garageVehicles.push(v);return v;
}
function rand(seed){let x=Math.sin(seed*12.9898+78.233)*43758.5453;return x-Math.floor(x)}
export function usedOffers(s){
 const day=s.day||1,types=['car','van','sport'];
 return types.map((id,i)=>{const def=VEHICLES[id],r=rand(day*17+i*31),r2=rand(day*29+i*47),condition=Math.round(48+r*38),mileage=Math.round((38000+r2*145000)*10)/10,price=Math.round(def.cost*(.42+r2*.27)/100)*100,fuel=Math.round(22+r*63),offerId='d'+day+'-'+id+'-'+i;return{offerId,id,condition,mileage,price,fuel,name:def.name};}).filter(o=>!s.mobility.usedBought.includes(o.offerId));
}
export function validateMobility(s){
 ensureMobility(s);const vehicles=allVehicles(s);
 if(vehicles.length>MAX_OWNED_VEHICLES)throw Error('Zu viele eigene Fahrzeuge.');
 const ids=new Set(),spots=new Set();
 for(const [index,v] of vehicles.entries()){
  if(!v||!Object.hasOwn(VEHICLES,v.id)||typeof v.uid!=='string'||!v.uid||ids.has(v.uid))throw Error('Ungültiges Fahrzeug.');ids.add(v.uid);
  if(!Number.isFinite(v.fuel)||v.fuel<0||v.fuel>100||!Number.isFinite(v.condition)||v.condition<0||v.condition>100||!Number.isFinite(v.x)||!Number.isFinite(v.z)||Math.abs(v.x)>350||Math.abs(v.z)>125||!Number.isFinite(v.angle)||!Number.isFinite(v.speed)||Math.abs(v.speed)>35||!Number.isFinite(v.mileage)||v.mileage<0||v.mileage>1e7||!Number.isSafeInteger(v.purchasePrice)||v.purchasePrice<0||typeof v.insured!=='boolean'||typeof v.used!=='boolean')throw Error('Ungültige Fahrzeugdaten.');
  if(!Array.isArray(v.trunk))throw Error('Ungültiger Fahrzeugstauraum.');const lim=trunkLimits(v);if(v.trunk.length>lim.slots||inventoryWeight(v.trunk)>lim.weight+.00001)throw Error('Fahrzeugstauraum überladen.');
  for(const item of v.trunk)if(!item||!Object.hasOwn(ITEMS,item.id)||item.id==='parcel'||!Number.isInteger(item.count)||item.count<1||item.count>ITEMS[item.id].stack)throw Error('Ungültiger Gegenstand im Fahrzeug.');
  if(v.parkingSpot){if(!PARKING_SPOTS.some(p=>p.id===v.parkingSpot)||spots.has(v.parkingSpot))throw Error('Ungültiger Stellplatz.');spots.add(v.parkingSpot);}
  if(index&&v.parkingSpot&&!GARAGE_SPOTS.some(p=>p.id===v.parkingSpot))throw Error('Garagenfahrzeug steht außerhalb des Mobilwerks.');
 }
 const m=s.mobility;if(!Array.isArray(m.usedBought)||m.usedBought.some(v=>typeof v!=='string')||!safeInt(m.transitTrips)||!safeInt(m.insurancePaid)||!safeInt(m.serial))throw Error('Ungültige Mobilitätsdaten.');
 return s;
}
