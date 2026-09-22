import {ROAD_X} from './city-layout.js?v=0.8.0';
const smooth=(a,b,x)=>{const t=Math.max(0,Math.min(1,(x-a)/(b-a)));return t*t*(3-2*t)};
export function lightingAt(minute,inside=false){
 const time=((minute%1440)+1440)%1440;
 const daylight=smooth(330,450,time)*(1-smooth(1080,1200,time));
 const night=1-daylight,altitude=Math.max(0,Math.sin((time-360)/840*Math.PI));
 const dusk=Math.exp(-Math.pow((time-1110)/65,2)),dawn=Math.exp(-Math.pow((time-390)/60,2));
 return {day:daylight,night,dusk,dawn,altitude,hemisphere:inside?1.1:.42+daylight*1.25,moon:inside?0:night*.38,sun:inside?0:3.1*altitude*daylight,environment:inside?.32:.12+daylight*.27,lamp:inside?0:smooth(.06,.8,night)};
}
// Preserve assignments until a nearby lamp leaves the pool: no per-frame light teleporting.
export function selectLamps(lamps,position,current=[],count=8){
 const active=new Set(current),ranked=lamps.map(lamp=>({lamp,distance:Math.hypot(lamp.x-position.x,lamp.z-position.z),score:((lamp.x-position.x)**2+(lamp.z-position.z)**2)*(active.has(lamp)?.82:1)})).sort((a,b)=>a.score-b.score);
 // Accent fixtures must not push every road light out of the finite light pool.
 const road=ranked.filter(p=>p.lamp.kind==='street'&&p.distance<26).slice(0,Math.ceil(count/2)),chosen=new Set(road.map(p=>p.lamp));
 for(const p of ranked){if(chosen.size>=count)break;if(p.lamp.kind==='accent'&&p.distance>(p.lamp.distance??14))continue;chosen.add(p.lamp);}
 return [...chosen];
}

// Keep the main streets lit continuously, with extra coverage on the north/south road.
export const STREET_LAMPS=[];
for(let x=-99;x<=99;x+=18)for(const z of [-75,-55,-9,9,55,75])STREET_LAMPS.push({x,z});
for(const x of [-119,-101,-9,9,101,119])for(const z of [-99,-87,-39,-21,27,43,93,111])STREET_LAMPS.push({x,z});

for(const x of [-191,-173,-155,-137])for(const z of [-75,-55,-9,9,55,75])STREET_LAMPS.push({x,z});
for(const x of [-212,-192])for(const z of [-38,-20,20,38])STREET_LAMPS.push({x,z});
for(const z of [-30,30])STREET_LAMPS.push({x:-161,z});

// Suspend the light beside its pole so the shadow-casting pole cannot enclose it.
export function streetLampHead(x,z){
 const roadX=ROAD_X.reduce((a,b)=>Math.abs(b-x)<Math.abs(a-x)?b:a);
 const roadZ=[-65,0,65].reduce((a,b)=>Math.abs(b-z)<Math.abs(a-z)?b:a);
 const dx=Math.abs(roadX-x)<Math.abs(roadZ-z)?Math.sign(roadX-x)*.95:0;
 const dz=dx?0:Math.sign(roadZ-z)*.95;
 return {x:x+dx,z:z+dz,dx,dz};
}
