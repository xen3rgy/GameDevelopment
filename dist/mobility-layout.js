// Metres; interaction markers and safe pavement arrivals are deliberately separate.
import {SERVICE_BAYS} from './service-layout.js?v=0.8.1';
export const PARKING_SPACES=[
 ...[70,78,86].map((x,i)=>({id:'mobilwerk-'+i,name:'Mobilwerk '+(i+1),x,z:5.15,angle:Math.PI/2,exit:{x,z:8.2},garage:true})),
 ...[-188,-180].map((x,i)=>({id:'west-'+i,name:'Westbahnhof '+(i+1),x,z:5.15,angle:Math.PI/2,exit:{x,z:8.2}})),
 ...[-94,-86].map((x,i)=>({id:'hafen-'+i,name:'Westhafen '+(i+1),x,z:-70.15,angle:-Math.PI/2,exit:{x,z:-73.2}})),
 ...[60,68].map((x,i)=>({id:'kanal-'+i,name:'Am Kanal '+(i+1),x,z:70.15,angle:Math.PI/2,exit:{x,z:73.2}})),
 ...SERVICE_BAYS.map(b=>({id:'service-'+b.id,name:b.name,...b.vehicle,exit:{x:b.x,z:b.z},service:true}))
];
export const TRANSIT_STOPS=[
 {id:'busWest',name:'Westbahnhof · Bus',mode:'bus',x:-168,z:-9,arrival:{x:-166,z:-10.5},vehicle:{x:-168,z:-5.1,angle:-Math.PI/2}},
 {id:'busCity',name:'Lindenhöfe · Stadtmitte',mode:'bus',x:-34,z:9,arrival:{x:-32,z:11},vehicle:{x:-34,z:5.1,angle:Math.PI/2}},
 {id:'busHarbor',name:'Westhafen · Bus',mode:'bus',x:-39,z:-74,arrival:{x:-37,z:-76},vehicle:{x:-39,z:-70.1,angle:-Math.PI/2}},
 {id:'busPark',name:'Lindenpark · Bus',mode:'bus',x:84,z:74,arrival:{x:85.5,z:75.5},vehicle:{x:84,z:70.1,angle:Math.PI/2}},
 {id:'station',name:'Lindenstadt West · S1',mode:'train',x:-175,z:-31,arrival:{x:-176,z:-29},vehicle:{x:-145,z:-30,angle:0}},
 {id:'railSouth',name:'Gleishof Süd · S1',mode:'train',x:-155,z:58,arrival:{x:-157,z:59},vehicle:{x:-145,z:58,angle:0}}
];
export const stopById=id=>TRANSIT_STOPS.find(p=>p.id===id);
export const parkingById=id=>PARKING_SPACES.find(p=>p.id===id);
export function safeArrival(origin,canStand,radius=6){
 if(canStand(origin.x,origin.z))return {...origin};
 for(let r=.5;r<=radius;r+=.5)for(let i=0;i<24;i++){const a=i*Math.PI/12,p={x:origin.x+Math.sin(a)*r,z:origin.z+Math.cos(a)*r};if(canStand(p.x,p.z))return p;}
 return null;
}
