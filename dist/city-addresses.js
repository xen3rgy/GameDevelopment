import * as THREE from './vendor/three.module.js';
import {LOCATIONS} from './data.js?v=0.8.0';
import {ADDRESSES,addressOf,STREETS} from './orientation.js?v=0.8.0';
export function entranceForBuilding(x,z,depth,face=1){
 return LOCATIONS.find(l=>!['recycle','park'].includes(l.id)&&Math.abs(l.x-x)<4&&Math.abs(l.z-(z+face*depth/2))<4);
}
export const STREET_POSTS=STREETS.flatMap(s=>[-1,1].map(side=>({x:side*9,z:s.z+side*12,name:s.name,cross:'Bahnhofstraße'}))).concat(
 STREETS.flatMap(s=>[[-202,'Westbogen'],[-110,'Westkai'],[110,'Parkring']].map(([x,cross])=>({x:x-9,z:s.z-12,name:s.name+(x<-120?' West':''),cross})))
);
export function addStreetSigns(world,kit){
 const {box,cylinder,sign}=kit;
 for(const p of STREET_POSTS){
  const g=new THREE.Group();g.position.set(p.x,0,p.z);world.scene.add(g);world.staticGroups.push(g);
  cylinder(g,0,1.91,0,.045,3.22,0x677a7b);cylinder(g,0,.45,0,.075,.30,0x455658);
  const panel=(parent,text,y)=>{box(parent,0,y,0,3.12,.52,.09,0xd1d5cb);box(parent,0,y,.002,3.02,.42,.102,0x173f4c);
   for(const side of [-1,1]){sign(parent,text,0,y,side*.057,2.87,.34,'#f5f2e7','#173f4c',side<0?Math.PI:0);for(const x of [-1.49,1.49]){const bolt=cylinder(parent,x,y,side*.06,.022,.015,0xc7d1d0);bolt.rotation.x=Math.PI/2;}}
  };
  panel(g,p.name,3.32);
  const cross=new THREE.Group();cross.rotation.y=Math.PI/2;g.add(cross);panel(cross,p.cross,2.72);
  world.colliders.push({x:p.x,z:p.z,w:.06,d:.06,h:3.6});
 }
}
export function decorateAddress(front,kit,location,localX){
 if(!location)return;
 const {box,sign,cylinder}=kit,id=location.id;
 // Plaques share their source with the phone, map and navigation. No invented numbers.
 box(front,localX+1.48,2.06,.32,.54,.54,.08,0xe2dfd0);
 sign(front,String(ADDRESSES[id][1]),localX+1.48,2.06,.369,.46,.40,'#eff1e9','#23424a');
 // One entrance per location, contained below the shop fascia and outside its awnings.
 const floor=location.x<-120?.04:.30,door=new THREE.Group();door.name='Entrance · '+id;door.position.set(localX,floor,0);front.add(door);
 box(door,0,1.24,.255,2.10,2.48,.10,0x1b2d32);
 for(const side of [-1,1])box(door,side*1.02,1.30,.35,.12,2.60,.18,0x88958d);
 box(door,0,2.56,.35,2.16,.12,.18,0xabb2a5);
 box(door,0,.035,.39,2.04,.07,.42,0x969c91);
 box(door,0,1.22,.323,1.87,2.32,.035,0x456568);
 box(door,0,.39,.354,1.82,.45,.025,0x2c454b);
 box(door,0,1.77,.357,1.65,1.04,.025,0x779596);
 box(door,.69,1.07,.402,.045,.38,.08,0xd7d5bf);
 box(door,0,1.29,.38,1.81,.055,.045,0xa6b4af);
 const caption=id==='market'?'EINGANG · 24 H':id==='shelter'?'WILLKOMMEN':location.type==='housing'?'WOHNEN':id==='jobs'?'PAKETAUSGABE':location.type==='delivery'?'WARENANNAHME':'EINGANG';
 sign(door,caption,0,2.30,.382,1.70,.22,'#f1ecdc','#2c454b');
 sign(front,addressOf(id),localX,3.82,.22,3.4,.31,'#f2eddf','#344b50');
 if(id!=='jobs'&&location.type!=='delivery')return;
 const g=new THREE.Group();g.position.x=localX;front.add(g);
 // Shallow fittings remain inside the existing facade collision envelope.
 box(g,-1.45,1.28,.36,.54,.65,.15,0x485e61);box(g,-1.45,1.41,.45,.38,.025,.025,0x18292d);
 sign(g,id==='deliveryB'?'ATELIER':id==='deliveryA'?'KAPITEL':id==='jobs'?'KURIER':id==='deliveryKiosk'?'KIOSK':id==='deliveryWorkshop'?'WERKSTATT':'KONTOR',-1.45,1.12,.449,.43,.11,'#eee2c7','#485e61');
 cylinder(g,1.15,1.3,.40,.05,.02,0xd5b67a).rotation.x=Math.PI/2;
 if(id==='deliveryA'){
  box(g,3.5,1.35,.29,3,1.55,.08,0x263f37);
  for(const y of [.72,1.35]){box(g,3.5,y,.43,2.7,.065,.35,0xa68a5d);for(let i=0;i<11;i++){const h=.32+(i%3)*.07;box(g,2.32+i*.225,y+h/2+.04,.42,.16,h,.23,[0x9c5b43,0x49756c,0xd0b680,0x617b91][i%4]);}}
  sign(g,'NEUE GESCHICHTEN',3.5,2.24,.36,2.7,.19,'#f1debb','#31594c');
 }else if(id==='deliveryB'){
  box(g,3.4,1.55,.30,2.8,1.5,.12,0xab885b);box(g,3.4,1.55,.38,2.55,1.25,.025,0xd8c8a7);
  for(let i=0;i<5;i++)box(g,2.44+i*.47,1.55+Math.sin(i)*.2,.405,.3,.45+i*.10,.02,[0x9a593c,0x426c68,0xc9a054,0x334955,0x807a62][i]);
  sign(g,'WERKSTATT · AM KANAL',3.4,.67,.36,2.8,.20,'#e6d8c0','#714832');
 }else if(id==='deliveryC'){
  box(g,3.6,1.52,.30,3.3,2.35,.08,0x30424b);
  for(let i=0;i<13;i++)box(g,3.6,.45+i*.167,.37,3.05,.12,.035,i%2?0x718080:0x82918c);
  sign(g,'ANLIEFERUNG · NORDKONTOR',3.6,2.5,.414,2.85,.20,'#f2e3c5','#354d60');
  for(const x of [1.9,5.3])box(g,x,.7,.43,.12,.75,.16,0xbf9b50);
 }else if(id==='jobs'){
  box(g,3.55,1.56,.30,3.15,1.92,.12,0x23424a);
  for(const [text,y] of [['KIEZ & KURIER',2.2],['PAKETE · PERSÖNLICH',1.83],['TOUREN DURCH LINDENSTADT',1.43],['DEIN WEG BEGINNT HIER',1.04]])sign(g,text,3.55,y,.376,2.85,.19,'#ead9b3','#23424a');
 }
}
