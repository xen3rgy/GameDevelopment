import {NEIGHBORHOOD_FIXTURES} from './neighborhood-layout.js?v=0.7.3-map1';
import {PROMENADE_FIXTURES} from './pedestrian-layout.js?v=0.7.3-map1';
import {CITY_CHARACTER_FIXTURES} from './city-character-layout.js?v=0.7.3-map1';
import {WORLD_BOUNDS,ROAD_X,DISTRICT_FIXTURES,exteriorContains} from './city-layout.js?v=0.7.3-map1';
import {PARCEL_WALK_SPEED} from './player-movement.js?v=0.7.3-map1';
import {BUILDINGS,LOCATIONS} from './data.js?v=0.7.3-map1';
import {CityNavigation,routeLength} from './navigation.js?v=0.7.3-map1';
import {STREETS} from './orientation.js?v=0.7.3-map1';
export const DELIVERY_SECONDS={pickup:2.2,deliver:2.6};
export const DELIVERY_PEOPLE={jobs:{name:'Jonas',role:'Paketausgabe',color:0x9a714d},deliveryA:{name:'Nora',role:'Buchhandlung',color:0x477166},deliveryB:{name:'Milan',role:'Atelier',color:0x976448},deliveryC:{name:'Samira',role:'Warenannahme',color:0x536c88},deliveryKiosk:{name:'Yusuf',role:'Kiosk am Gleis',color:0x9d7545},deliveryWorkshop:{name:'Tessa',role:'Werkstatt West',color:0x56717b}};
export const deliveryLocation=id=>LOCATIONS.find(l=>l.id===id);
export function stationFacing(id){const l=deliveryLocation(id);const b=BUILDINGS.find(([x,z,w,d,,,,face=1])=>Math.abs(l.x-x)<w/2&&Math.abs(l.z-(z+face*d/2))<4);return b?.[7]||1;}
export function deliveryTarget(id){const l=deliveryLocation(id);return l&&DELIVERY_PEOPLE[id]?{...l,z:l.z+stationFacing(id)*1.2}:l;}
let navigation;
const cache=new Map();
function walkRoute(from,to){
 if(!navigation){const blocks=BUILDINGS.map(([x,z,w,d])=>({x,z,w:w/2+.35,d:d/2+.35})).concat(DISTRICT_FIXTURES,NEIGHBORHOOD_FIXTURES,PROMENADE_FIXTURES,CITY_CHARACTER_FIXTURES);for(const s of STREETS)for(const side of [-1,1])blocks.push({x:side*9,z:s.z+side*12,w:.06,d:.06});navigation=new CityNavigation((x,z,r=.45)=>exteriorContains(x,z,r)&&!blocks.some(b=>Math.abs(x-b.x)<b.w+r&&Math.abs(z-b.z)<b.d+r),4,WORLD_BOUNDS);}
 return navigation.find(from,to);
}
export function deliveryLeg(fromId,toId,mode='foot'){
 const key=[fromId,toId,mode].join(':');if(cache.has(key))return cache.get(key);
 const a=deliveryLocation(fromId),b=deliveryLocation(toId);if(!a||!b)return null;
 let distance;
 if(mode==='vehicle'){
  const road=p=>STREETS.reduce((best,s)=>Math.abs(p.z-s.z)<Math.abs(p.z-best)?s.z:best,0),az=road(a),bz=road(b);
  distance=Math.abs(a.z-az)+Math.abs(b.z-bz)+(az===bz?Math.abs(b.x-a.x):Math.min(...ROAD_X.map(x=>Math.abs(a.x-x)+Math.abs(b.x-x)))+Math.abs(az-bz));
 }else{const route=walkRoute(a,b);if(route.length<2)return null;distance=routeLength(route);}
 const result={from:fromId,to:toId,distance:Math.ceil(distance)};cache.set(key,result);return result;
}
export function transportAvailable(s,mode){if(mode==='foot')return true;const v=s.vehicle;if(!v||Math.hypot(v.x-26,v.z+12)>18||v.condition<=10)return false;return mode==='bike'?v.id==='bike':mode==='vehicle'&&v.id!=='bike'&&v.fuel>5;}
export const transportLabel=mode=>({foot:'Zu Fuß',bike:'Stadtrad',vehicle:'Auto / Lieferwagen'}[mode]||'Zu Fuß');
export function deliveryPlan(contract,s,mode='foot'){
 if(!['foot','bike','vehicle'].includes(mode))return null;
 let from='jobs';const legs=[];for(const id of contract.route){const leg=deliveryLeg(from,id,mode);if(!leg)return null;legs.push(leg);from=id;}
 const distance=legs.reduce((n,l)=>n+l.distance,0),speed=[1,4,10].includes(s.settings.speed)?s.settings.speed:1;
 const travelSeconds=distance/({foot:PARCEL_WALK_SPEED-.25,bike:5.2,vehicle:6}[mode]),serviceSeconds=contract.route.length*(DELIVERY_SECONDS.deliver+(mode==='foot'?1:7));
 const bufferSeconds=12+travelSeconds*.22,realSeconds=Math.ceil((travelSeconds+serviceSeconds+bufferSeconds)/5)*5;
 return {mode,speed,distance,legs,travelSeconds:Math.ceil(travelSeconds),serviceSeconds:Math.ceil(serviceSeconds),bufferSeconds:Math.ceil(bufferSeconds),realSeconds,deadlineMinutes:contract.minutes?realSeconds*speed:0};
}
