import * as THREE from './vendor/three.module.js';
import {LOCATIONS} from './data.js?v=0.7.2';
import {ADDRESSES,addressOf,STREETS} from './orientation.js?v=0.7.2';
export function entranceForBuilding(x,z,depth,face=1){
 return LOCATIONS.find(l=>!['recycle','park'].includes(l.id)&&Math.abs(l.x-x)<4&&Math.abs(l.z-(z+face*depth/2))<4);
}
export const STREET_POSTS=STREETS.flatMap(s=>[-1,1].map(side=>({x:side*9,z:s.z+side*12,name:s.name})));
export function addStreetSigns(world,kit){
 const {box,cylinder,sign}=kit;
 for(const p of STREET_POSTS){
  const g=new THREE.Group();g.position.set(p.x,0,p.z);world.scene.add(g);world.staticGroups.push(g);
  cylinder(g,0,1.85,0,.055,3.1,0x3c5155);box(g,0,3.3,0,2.8,.42,.08,0x233d45);
  sign(g,p.name,0,3.3,.052,2.64,.32,'#f1e7d2','#233d45');sign(g,p.name,0,3.3,-.052,2.64,.32,'#f1e7d2','#233d45',Math.PI);
  const cross=new THREE.Group();cross.rotation.y=Math.PI/2;g.add(cross);box(cross,0,2.86,0,2.8,.35,.08,0x233d45);
  sign(cross,'Bahnhofstraße',0,2.86,.052,2.64,.27,'#f1e7d2','#233d45');sign(cross,'Bahnhofstraße',0,2.86,-.052,2.64,.27,'#f1e7d2','#233d45',Math.PI);
  world.colliders.push({x:p.x,z:p.z,w:.06,d:.06,h:3.6});
 }
}
export function decorateAddress(front,kit,location,localX){
 if(!location)return;
 const {box,sign,cylinder}=kit,id=location.id;
 // Plaques share their source with the phone, map and navigation. No invented numbers.
 box(front,localX+1.6,2.13,.32,.68,.6,.08,0x263f45);
 sign(front,String(ADDRESSES[id][1]),localX+1.6,2.13,.371,.57,.43,'#f4e6c5','#263f45');
 sign(front,addressOf(id),localX,3.82,.17,3.4,.28,'#e8e1ce','#344b50');
 if(id!=='jobs'&&location.type!=='delivery')return;
 const colors={jobs:0xae7041,deliveryA:0x31594c,deliveryB:0x96573d,deliveryC:0x354d60},accent=colors[id]??0x44635a;
 const g=new THREE.Group();g.position.x=localX;front.add(g);
 box(g,0,1.52,.32,1.75,2.45,.14,0x202f32);box(g,0,1.52,.413,1.50,2.25,.04,accent);
 box(g,0,1.89,.446,1.14,1.2,.022,0x6b8281);box(g,0,1.17,.455,1.32,.11,.035,0xc1ad7e);
 box(g,.53,1.42,.51,.065,.40,.075,0xd1c5a0);
 sign(g,id==='jobs'?'PAKETAUSGABE':'WARENANNAHME',0,1.95,.47,1.08,.17,'#fbebc8','#263f45');
 sign(g,id==='jobs'?'E · AUFTRÄGE ANSEHEN':'E · PERSÖNLICH ÜBERGEBEN',0,1.61,.47,1.12,.12,'#d7e2d8','#263f45');
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
