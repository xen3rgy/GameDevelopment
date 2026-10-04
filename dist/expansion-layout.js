import {TUNING_SITE} from './tuning-layout.js?v=0.8.1';
// Exterior-only data. Existing addresses and detached interior coordinates are unchanged.
export const EXPANDED_BOUNDS={minX:-260,maxX:440,minZ:-255,maxZ:255};
const road=(name,x,z,w,d,kind='arterial')=>({name,x,z,w,d,kind});
export const EXPANSION_ROADS=[
 road('Nordring',0,-175,464,13),road('Werksstraße',0,175,464,13),
 road('Kontorallee',274,0,302,13),road('Hafenbogen',274,-65,302,13),road('Uferallee',274,65,302,13),
 road('Campusallee',316,-175,208,13),road('Höhenallee',316,175,208,13),
 road('Ostring',220,0,13,474),road('Höhenweg',400,0,13,414),
 road('Nordbogen',-202,-153,13,176),road('Werksbogen',-202,153,13,176),
 road('Westkai Nord',-110,-177,13,116),road('Westkai Süd',-110,177,13,116),
 road('Lindenstraße Nord',0,-177,13,116),road('Lindenstraße Süd',0,177,13,116),
 road('Parkring Nord',110,-177,13,116),road('Parkring Süd',110,177,13,116),
 road('Ahornstraße',0,-230,438,9,'local'),road('Speditionsweg',0,230,438,9,'local'),
 road('Forschungsgasse',305,-207.5,9,65,'local'),road('Gartenstieg',310,207.5,9,65,'local'),
 road('Kontorpassage',310,32.5,9,65,'local'),road('Werkhof',165,202.5,9,55,'local')
];
export const EXPANSION_DISTRICTS=[
 {id:'nordstadt',name:'NORDSTADT',x:-65,z:-194,minX:-260,maxX:120,minZ:-255,maxZ:-120,color:'#48594e',style:'residential'},
 {id:'suedwerk',name:'SÜDWERK',x:-60,z:209,minX:-260,maxX:220,minZ:120,maxZ:255,color:'#55514a',style:'industrial'},
 {id:'ostkontor',name:'OSTKONTOR',x:302,z:-25,minX:120,maxX:440,minZ:-115,maxZ:115,color:'#405563',style:'office'},
 {id:'techpark',name:'TECHPARK',x:312,z:-217,minX:120,maxX:440,minZ:-255,maxZ:-115,color:'#455d56',style:'campus'},
 {id:'hoehen',name:'HÖHENVIERTEL',x:334,z:221,minX:220,maxX:440,minZ:115,maxZ:255,color:'#52634d',style:'townhouse'}
];
export const expansionDistrict=p=>EXPANSION_DISTRICTS.find(d=>p.x>=d.minX&&p.x<=d.maxX&&p.z>=d.minZ&&p.z<=d.maxZ);
export const EXPANSION_LANDMARKS=[
 {id:'nordCourt',name:'Nordring · Sporthof',x:-55,z:-203,w:40,d:28,kind:'court'},
 {id:'nordGarden',name:'Ahornhöfe',x:55,z:-202,w:35,d:24,kind:'garden'},
 {id:'loadingYard',name:'Südwerk · Verladehof',x:-48,z:202,w:32,d:30,kind:'yard'},
 {id:'railYard',name:'Altes Stellwerk',x:-155,z:204,w:28,d:32,kind:'rail'},
 {id:'kontorSquare',name:'Kontorplatz',x:288,z:-25,w:25,d:22,kind:'plaza'},
 {id:'parkingTower',name:'Parkhaus am Ostring',x:264,z:101,w:36,d:27,kind:'parking'},
 {id:'campusGarden',name:'Forum der Ideen',x:355,z:-210,w:46,d:28,kind:'campus'},
 {id:'construction',name:'Baufeld Ost',x:354,z:-100,w:43,d:23,kind:'construction'},
 {id:'overlook',name:'Lindenblick',x:357,z:216,w:48,d:28,kind:'overlook'},
 {id:'uferGarden',name:'Ufergarten',x:353,z:105,w:49,d:28,kind:'garden'}
];
export const EXPANSION_PLACES=[
 ['deliveryNord','Nordring Buch & Papier',-83,-163,'Nordring',8],
 ['deliveryWerk','Südwerk Ersatzteillager',-83,187,'Werksstraße',12],
 ['deliveryOst','Kontorhaus Empfang',264,-53,'Hafenbogen',22],
 ['deliveryTech','Campus Poststelle',263,-163,'Campusallee',4],
 ['deliveryHoehen','Lindenblick Gärtnerei',356,163,'Höhenallee',9]
].map(([id,name,x,z,street,number])=>({id,name,x,z,street,number,type:'delivery',icon:'◫',color:'#c6bd95'}));
export const EXPANSION_STOPS=[
 ['busNord','Nordring',-25,-184,-Math.PI/2],['busWerk','Südwerk',-25,184,Math.PI/2],
 ['busOst','Kontorplatz',252,9,Math.PI/2],['busTech','Techpark',330,-166,Math.PI/2],
 ['busHoehen','Lindenblick',370,184,Math.PI/2]
].map(([id,name,x,z,angle],i)=>{const roadZ=[-175,175,0,-175,175][i],side=Math.sign(z-roadZ);return {id,name:name+' · Bus',mode:'bus',x,z,roadZ,arrival:{x:x+2,z:z+side*1.5},vehicle:{x,z:z-side*3.9,angle}};});
export const EXPANSION_PARKING=[[-72,-169.85],[-72,180.15],[280,5.15],[280,-169.85],[340,180.15]].map(([x,z],i)=>({id:'outer-'+i,name:['Nordring','Südwerk','Kontorplatz','Techpark','Lindenblick'][i],x,z,angle:Math.PI/2,exit:{x,z:z+3.2}}));
export const EXPANSION_BINS=EXPANSION_STOPS.map((p,i)=>({id:100+i,x:p.x+8,z:p.z}));
export const EXPANSION_PATHS=EXPANSION_LANDMARKS.map(l=>({x:l.x,z:l.z,w:l.w+6,d:l.d+6,kind:l.kind}));
// Buildings fill defined blocks, leaving named squares, setbacks and connected street aisles.
export const EXPANSION_BUILDINGS=[];
for(const district of EXPANSION_DISTRICTS){
 const industrial=district.style==='industrial',town=district.style==='townhouse';
 let n=0;
 for(let z=district.minZ+22;z<district.maxZ-12;z+=8)for(let x=district.minX+23;x<district.maxX-12;x+=8){
  const w=industrial?35:town?17:22+(n%3)*3,d=industrial?26:town?18:22,h=industrial?8+(n%3)*2:town?8+(n%3)*3:district.style==='office'?24+(n%4)*6:district.style==='campus'?13+(n%3)*4:15+(n%4)*3;n++;
  if(x-w/2<-253||x+w/2>432||z-d/2<-248||z+d/2>248)continue;
  if(EXPANSION_ROADS.some(r=>Math.abs(x-r.x)<(w+r.w)/2+6&&Math.abs(z-r.z)<(d+r.d)/2+6))continue;
  if(EXPANSION_LANDMARKS.some(l=>Math.abs(x-l.x)<(w+l.w)/2+5&&Math.abs(z-l.z)<(d+l.d)/2+5))continue;
  if(Math.abs(x-TUNING_SITE.x)<(w+TUNING_SITE.w)/2+2&&Math.abs(z-TUNING_SITE.z)<(d+TUNING_SITE.d)/2+2)continue;
  if(EXPANSION_BUILDINGS.some(b=>Math.abs(x-b.x)<(w+b.w)/2+7&&Math.abs(z-b.z)<(d+b.d)/2+7))continue;
  if(Math.abs(x+145)<w/2+9)continue;
  if(EXPANSION_PLACES.some((l,i)=>Math.abs(x-l.x)<(w+22)/2+5&&Math.abs(z-(l.z-(i===4?1:-1)*11))<(d+17)/2+5))continue;
  EXPANSION_BUILDINGS.push({x,z,w,d,h,style:district.style,seed:n,district:district.id});
 }
}
// Doorstep buildings are deliberate destinations rather than markers in empty plots.
for(const [i,p] of EXPANSION_PLACES.entries()){const face=i===4?1:-1,d=17,z=p.z-face*(d/2+2.5);EXPANSION_BUILDINGS.push({x:p.x,z,w:22,d,h:12+i*3,style:EXPANSION_DISTRICTS[i].style,seed:i,name:p.name,face,district:EXPANSION_DISTRICTS[i].id});}
export const EXPANSION_FIXTURES=EXPANSION_BUILDINGS.map(b=>({x:b.x,z:b.z,w:b.w/2+.35,d:b.d/2+.35,h:b.h+2}));
export const EXPANSION_HOMES=EXPANSION_PLACES.map((p,i)=>({id:'outerHome'+i,label:p.name,home:true,x:p.x-6,z:p.z,doorZ:p.z-1,angle:0,district:EXPANSION_DISTRICTS[i].id}));
export const EXPANSION_ACTIVITIES=EXPANSION_STOPS.flatMap((p,i)=>[-5,0,5].map((dx,j)=>({id:p.id+'-wait'+j,cluster:p.id,kind:j===1?'browse':'wait',x:p.x+dx,z:p.z+1,angle:Math.PI,open:360,close:1380,district:EXPANSION_DISTRICTS[i].id})));
