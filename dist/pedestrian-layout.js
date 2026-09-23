import {NEIGHBORHOOD_FIXTURES} from './neighborhood-layout.js?v=0.8.1';
import {SERVICE_FIXTURES} from './service-layout.js?v=0.8.1';
import {STREET_TREES,STREET_PLANTERS} from './public-realm-layout.js?v=0.8.1';
import {STREET_LAMPS} from './lighting.js?v=0.8.1';
import {PAVEMENTS,streetSurface} from './street-layout.js?v=0.8.1';
import {ROADS} from './city-layout.js?v=0.8.1';
import {CITY_CHARACTER_FIXTURES,CITY_COURTYARD_PATH} from './city-character-layout.js?v=0.8.1';

// Existing street furniture now has the same physical footprint for people and navigation.
export const STREET_SEATS=[-81,-44,-13,21,52,92.5];
export function streetSpawn(position,canWalk){
 if(canWalk(position.x,position.z)||![...PROMENADE_FIXTURES,...CITY_CHARACTER_FIXTURES,...SERVICE_FIXTURES].some(p=>Math.abs(p.x-position.x)<p.w+.35&&Math.abs(p.z-position.z)<p.d+.35))return position;
 // Old saves could stand inside formerly decorative furniture. Relocate only during load.
 for(let radius=.25;radius<=4;radius+=.25)for(let i=0;i<32;i++){
  const angle=i*Math.PI/16,x=position.x+Math.sin(angle)*radius,z=position.z+Math.cos(angle)*radius;
  if(streetSurface(x,z)?.kind!=='road'&&canWalk(x,z))return {...position,x,z};
 }
 return position;
}
export const PROMENADE_BOLLARDS=Array.from({length:25},(_,i)=>-98+i*8).flatMap(x=>[-7,7].map(z=>({x,z}))).filter(p=>streetSurface(p.x,p.z)?.kind!=='road');
export const PROMENADE_FIXTURES=[
 ...STREET_SEATS.map(x=>({kind:'oldBench',x,z:10,w:1.1,d:.36,h:1.4})),
 ...STREET_TREES.map(({x,z})=>({kind:'trunk',x,z,w:.24,d:.24,h:3.7})),
 ...STREET_LAMPS.map(p=>({...p,kind:'lamp',w:.08,d:.08,h:5.25})),
 ...PROMENADE_BOLLARDS.map(p=>({...p,kind:'bollard',w:.10,d:.10,h:1.03})),
 {kind:'shelterBack',x:-59,z:10.7,w:2.65,d:.07,h:2.9},
 ...[-61.5,-56.5].map(x=>({kind:'post',x,z:10,w:.05,d:.05,h:2.9})),
 {kind:'shelterSeat',x:-59,z:9.9,w:1.1,d:.36,h:1.4},
 ...STREET_PLANTERS.map(p=>({...p,kind:'oldPlanter',h:1.5})),
 ...[57,69].flatMap(x=>[{kind:'terraceTable',x,z:-11,w:.7,d:.7,h:1},...[-1.1,1.1].map(dx=>({kind:'terraceChair',x:x+dx,z:-11,w:.27,d:.33,h:1.25}))])
];
export const CITIZEN_PORTALS=[
 {id:'linden',x:-34,z:12.6,doorZ:14.6,angle:Math.PI,label:'LINDENHÖFE',home:true},
 {id:'north',x:-68,z:-12.6,doorZ:-14.6,angle:0,label:'ANLAUFSTELLE NORD',home:true},
 {id:'west',x:-179,z:17.1,doorZ:19.6,angle:Math.PI,label:'GLEISHÖFE',home:true},
 {id:'market',x:-36,z:-12.6,doorZ:-14.6,angle:0,label:'MARKT 24',open:0,close:1440},
 {id:'bank',x:31,z:12.6,doorZ:14.6,angle:Math.PI,label:'STADTBANK',open:540,close:1080}
];
const seats=(id,x,z,height,angle)=>[-.56,.56].map((dx,i)=>({id:`${id}-${i}`,cluster:id,kind:'seat',x:x+dx,z:z+Math.cos(angle)*1.12,angle,seat:{x:x+dx,z,height},open:390,close:1320}));
export const COURTYARD_SEATS=CITY_CHARACTER_FIXTURES.filter(p=>p.kind==='courtSeat').flatMap(p=>[-.56,.56].map((offset,i)=>{
 const x=p.x+Math.cos(p.angle)*offset,z=p.z-Math.sin(p.angle)*offset;
 return {id:`${p.id}-${i}`,cluster:'lichthof',kind:'seat',x:x+Math.sin(p.angle)*1.12,z:z+Math.cos(p.angle)*1.12,angle:p.angle,seat:{x,z,height:.5575},open:450,close:1260};
}));
export const CITIZEN_DESTINATIONS=[
 ...COURTYARD_SEATS,
 ...CITIZEN_PORTALS.filter(p=>!p.home).map(p=>({...p,kind:'visit',portal:p.id,cluster:p.id})),
 ...STREET_SEATS.flatMap(x=>seats(`lindenbank${x}`,x,10,.52,Math.PI)),
 ...[-191,-159].flatMap(x=>seats(`stationbank${x}`,x,-29,.5175,0)),
 ...NEIGHBORHOOD_FIXTURES.filter(p=>p.kind==='seat').flatMap(p=>seats(`garden${p.x}:${p.z}`,p.x,p.z,.555,0)),
 {id:'station-clock',cluster:'station',kind:'wait',x:-185,z:-29.4,angle:Math.PI,open:300,close:1380},
 {id:'station-east',cluster:'station',kind:'wait',x:-166,z:-29.4,angle:Math.PI,open:300,close:1380},
 {id:'kiosk-window',cluster:'kiosk',kind:'browse',x:-181.4,z:-12.3,angle:Math.PI,open:360,close:1260},
 {id:'market-window',cluster:'market',kind:'browse',x:-42,z:-12.3,angle:Math.PI,open:0,close:1440},
 {id:'cafe-window',cluster:'cafe',kind:'browse',x:73,z:-12.3,angle:Math.PI,open:420,close:1200},
 {id:'noticeboard',cluster:'station',kind:'browse',x:-190,z:18.25,angle:Math.PI,open:420,close:1320}
];
// Small park-side paths link the existing northern/southern pocket gardens to sidewalks.
export const GARDEN_PATHS=[{x:0,z:-44.7,w:196,d:3.3},{x:0,z:50.3,w:196,d:3.3},
 ...[-54,44,94].map(x=>({x,z:-49.5,w:2.2,d:11})),...[-73,31,77].map(x=>({x,z:53,w:2.2,d:6})),
 {x:-145,z:-22,w:5,d:18},{x:-145,z:22,w:5,d:18},CITY_COURTYARD_PATH];
export const onGardenPath=(x,z)=>GARDEN_PATHS.some(p=>Math.abs(x-p.x)<=p.w/2&&Math.abs(z-p.z)<=p.d/2);
export function gardenPatches(){
 const xs=new Set(),zs=new Set();
 for(const p of [...GARDEN_PATHS,...ROADS,...PAVEMENTS]){xs.add(p.x-p.w/2);xs.add(p.x+p.w/2);zs.add(p.z-p.d/2);zs.add(p.z+p.d/2);}
 const xx=[...xs].sort((a,b)=>a-b),zz=[...zs].sort((a,b)=>a-b),out=[];
 for(let i=1;i<xx.length;i++)for(let j=1;j<zz.length;j++){const x=(xx[i]+xx[i-1])/2,z=(zz[j]+zz[j-1])/2;if(onGardenPath(x,z)&&!streetSurface(x,z))out.push({minX:xx[i-1],maxX:xx[i],minZ:zz[j-1],maxZ:zz[j]});}
 return out;
}
export const daytime=(minute,open,close)=>open<=close?minute>=open&&minute<close:minute>=open||minute<close;
export function citizenAwake(id,minute){
 minute=((minute%1440)+1440)%1440;
 if(id%8===6)return daytime(minute,1140,360); // A few night workers, not an empty world.
 return daytime(minute,360+(Math.floor(id/2)%5)*23,1260-(Math.floor(id/2)%4)*25);
}
