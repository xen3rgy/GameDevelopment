import * as THREE from './vendor/three.module.js';
import {CITY_CHARACTER_FIXTURES,CITY_CHARACTER_OVERHEAD,LITTER_ZONES,HORIZON_ROADS} from './city-character-layout.js?v=0.7.2';
import {groundHeight} from './spatial.js?v=0.7.2-stationstep1';
import {createCitizen} from './art.js?v=0.7.2';
import {seatedStreetPose} from './pedestrian-scene.js?v=0.7.2';

// Shared fixtures are used by collision, old-save recovery and route planning.
export class CityCharacter {
 constructor(world,kit){
  this.world=world;this.k=kit;this.time=0;this.residents=[];
  const {box,cylinder,sphere,sign}=kit,g=new THREE.Group();this.root=g;g.name='Lindenstadt · district character';world.scene.add(g);world.staticGroups.push(g);
  world.colliders.push(...CITY_CHARACTER_FIXTURES.map(p=>({...p,h:p.h+groundHeight(p.x,p.z)})));
  world.overheadColliders.push(...CITY_CHARACTER_OVERHEAD.map(p=>({...p})));
  for(const f of CITY_CHARACTER_FIXTURES){
   const p=new THREE.Group();p.name=f.id;p.position.set(f.x,groundHeight(f.x,f.z),f.z);g.add(p);
   if(f.kind==='camp'){
    // Personal possessions stay under the bridge, out of the through route.
    box(p,-.5,.025,.5,2.8,.05,2.1,0x937f60);
    box(p,-.65,.14,.55,1.12,.23,1.85,0x596b61);
    const blanket=box(p,-.65,.30,.80,1.14,.10,1.32,0x606877);blanket.rotation.z=.035;
    for(let n=0;n<7;n++)box(p,-1.15+n*.17,.357,.8,.023,.012,1.3,0x7b8390);
    box(p,-.65,.30,-.1,.85,.22,.35,0xa29e8b);
    box(p,1.1,.23,-.7,.75,.46,.65,0x9a815c);
    for(let n=0;n<4;n++)box(p,.78+n*.20,.27,-.365,.045,.32,.02,0x6d5940);
    const bag=sphere(p,1.65,.42,.85,.3,0x344c49);bag.scale.y*=1.4;
    cylinder(p,1.65,.89,.85,.06,.12,0x626755);
    box(p,1.35,.035,.1,.7,.07,.85,0xa9916b);
    cylinder(p,.25,.13,-.7,.075,.24,0x718fa0);
    // A small wire trolley instead of a featureless heap.
    for(const x of [1.42,2.08])for(const z of [-1.7,-1.2]){cylinder(p,x,.14,z,.08,.18,0x303939);box(p,x,.63,z,.025,.83,.025,0x82918a);}
    for(let y=.38;y<1.05;y+=.14){box(p,1.75,y,-1.7,.7,.025,.025,0x82918a);box(p,1.75,y,-1.2,.7,.025,.025,0x82918a);}
    box(p,1.75,.47,-1.45,.55,.4,.4,0x89734f);box(p,1.75,1.18,-1.68,.72,.045,.045,0x465756);
   }else if(f.kind==='dumpster'){
    for(const x of [-.56,.56])for(const z of [-.31,.31])cylinder(p,x,.11,z,.08,.20,0x242f31);
    box(p,0,.59,0,1.36,.86,.84,0x425f56);box(p,0,1.04,0,1.48,.09,.95,0x2d4640);
    for(const x of [-.66,.66])box(p,x,.75,0,.025,.47,.60,0x64776a);
    box(p,0,1.13,.34,.37,.055,.07,0x9aa79b);
    sign(p,'RESTMÜLL',0,.71,.429,.64,.15,'#d0d3bf','#425f56');
   }else if(f.kind==='courtSeat'){
    p.rotation.y=f.angle;
    for(const x of [-1.1,1.1])box(p,x,.25,0,.09,.5,.55,0x4b6464);
    for(let i=0;i<5;i++)box(p,0,.52,-.31+i*.14,2.8,.075,.11,0x9c7e52);
    for(let i=0;i<4;i++)box(p,0,.69+i*.13,-.35,2.8,.095,.065,0x9c7e52);
   }else if(f.kind==='courtGarden'){
    box(p,0,.29,0,1.4,.58,2.65,0xb2b6aa);box(p,0,.585,0,1.24,.015,2.45,0x4d513d);
    for(let n=0;n<11;n++){
     const z=-1.05+n*.21,x=Math.sin(n*2.7)*.4;
     cylinder(p,x,.86,z,.018,.55,0x55704e);
     const leaf=sphere(p,x,1.02,z,.22,[0x62805a,0x81925e,0x536e54][n%3]);leaf.scale.multiply(new THREE.Vector3(.7,1.3,.6));
     if(n%3===0)sphere(p,x,1.26,z,.07,0xc0a2b4);
    }
   }else if(f.kind==='pergolaPost')box(p,0,1.45,0,.14,2.9,.14,0x506c65);
  }
  for(const x of [51.3,59])box(g,x,2.92,21.5,.16,.20,5.5,0x506c65);
  for(let z=18.8;z<24.3;z+=.45)box(g,55.15,3.06,z,8.25,.10,.08,0xa09274);
  box(g,55.15,2.68,18.8,2.6,.28,.08,0x375750);sign(g,'LICHTHOF',55.15,2.68,18.748,2.2,.18,'#ece3c5','#375750',Math.PI);
  for(const x of [51.3,59])for(let n=0;n<7;n++){const leaf=sphere(g,x,2.91,19+n*.6,.28,0x547154);leaf.scale.multiply(new THREE.Vector3(.8,.45,1.1));}
  let seed=69069;const rand=()=>((seed=(Math.imul(seed,1664525)+1013904223)>>>0)/4294967296);
  for(const area of LITTER_ZONES)for(let i=0;i<area.count;i++){
   const x=area.x+(rand()-.5)*area.w,z=area.z+(rand()-.5)*area.d,y=groundHeight(x,z);
   if(i%4===0){const can=cylinder(g,x,y+.055,z,.034,.10,[0x8c9590,0x788779][i%2]);can.rotation.z=.9;can.castShadow=false;}
   else {const paper=box(g,x,y+.015,z,.07+rand()*.15,.009,.09+rand()*.15,[0xb7ac8c,0x929581,0xd0c3a5][i%3]);paper.rotation.y=rand()*Math.PI;paper.rotation.x=(rand()-.5)*.035;paper.castShadow=false;}
  }
  // Smaller inlaid repairs, above the support surface but never a second floor.
  for(const [x,z,w,d] of [[-184,-25,1.1,.6],[-170,-21,1.8,.9],[-165,39,.9,1.2],[-152,49,1.7,.6]]){
   const patch=box(g,x,groundHeight(x,z)+.005,z,w,.008,d,0x7d8077);patch.castShadow=false;
   for(let n=0;n<4;n++)box(g,x-w/2+n*w/3,groundHeight(x,z)+.012,z,.014,.004,d,0x62675e);
  }
  // A timetable and directional plates belong to the station wall, not floating in space.
  box(g,-186,2.05,-32.76,1.5,1.75,.13,0x42534d);box(g,-186,2.05,-32.685,1.35,1.58,.015,0xc9c8b6);
  sign(g,'ABFAHRTEN',-186,2.66,-32.67,1.15,.18,'#243f3c','#c9c8b6');
  for(let n=0;n<6;n++){box(g,-186,2.39-n*.18,-32.67,1.12,.012,.008,0x839084);box(g,-186.43,2.32-n*.18,-32.66,.18,.045,.007,0x48605b);box(g,-185.94,2.32-n*.18,-32.66,.55,.035,.007,0x798578);}
  for(const x of [-187,-175,-163])this.fixtureLight(g,x,4.1,-31.6,1.3,120);
  for(const x of [52,58.3])this.fixtureLight(g,x,2.78,22,.65,65);
 }
 fixtureLight(g,x,y,z,width,power){
  const {box}=this.k;box(g,x,y+.08,z,width,.12,.20,0x3c514e);
  const bulb=new THREE.MeshBasicMaterial({color:0x454c43});box(g,x,y,z,width-.1,.045,.16,0,bulb).castShadow=false;
  this.world.atmosphere.registerLamp(x,z,bulb,{height:y,power,poolSize:7,distance:14});
 }
 initResidents(){
  const root=new THREE.Group();root.name='People sheltering beneath the railway';this.world.scene.add(root);this.residentRoot=root;
  CITY_CHARACTER_FIXTURES.filter(f=>f.kind==='camp').forEach((f,i)=>{
   const m=createCitizen(this.k,i?0x65694f:0x6a7480,i?0xab8468:0xc39a7c,900+i,{hair:'cap',hairColor:0x4b443d,trousers:0x3e484a,accent:0x998366,shoes:0x484842,coat:true});
   m.position.set(f.x+1.1,groundHeight(f.x,f.z)-.05,f.z-.7);m.rotation.y=0;seatedStreetPose(m,1,.46);root.add(m);this.residents.push(m);
  });
 }
 update(state,dt,position){
  this.time+=dt;if(!this.residentRoot)return;this.residentRoot.visible=!state.inside;
  for(let i=0;i<this.residents.length;i++){
   const m=this.residents[i];if(state.inside||Math.hypot(m.position.x-position.x,m.position.z-position.z)>65)continue;
   const d=m.userData;d.upper.rotation.y=Math.sin(this.time*.25+i)*.035;if(d.headRoot)d.headRoot.rotation.y=Math.sin(this.time*.33+i*2)*.13;
   d.arms[0].rotation.x=-.43+Math.sin(this.time*.5+i)*.025;
  }
 }
}

export const horizonCorridor=(x,z,margin=0)=>HORIZON_ROADS.some(r=>Math.abs(x-r.x)<r.w/2+margin&&Math.abs(z-r.z)<r.d/2+margin);
export function buildHorizonLinks(world,kit){
 const {box,cylinder}=kit,g=new THREE.Group();g.name='Road and rail continuity';world.scene.add(g);world.staticGroups.push(g);
 const material=new THREE.MeshStandardMaterial({color:0x535d60,roughness:.9});
 for(const r of HORIZON_ROADS){
  box(g,r.x,.13,r.z,r.w,.02,r.d,0,material).castShadow=false;
  const horizontal=r.w>r.d,length=horizontal?r.w:r.d;
  for(const side of [-1,1]){
   const p=box(g,r.x+(horizontal?0:side*8.1),.225,r.z+(horizontal?side*8.1:0),horizontal?r.w:3.2,.15,horizontal?3.2:r.d,0x969d94);p.castShadow=false;
  }
  // Dashed continuation stays inside each road segment.
  for(let t=-length/2+4;t<length/2-2;t+=7)box(g,r.x+(horizontal?t:0),.146,r.z+(horizontal?0:t),horizontal?2.6:.12,.004,horizontal?.12:2.6,0xc5c3ac).castShadow=false;
 }
 for(const side of [-1,1]){
  box(g,-145,6.2,side*135,12,.8,112,0x727970);
  for(const x of [-150.8,-139.2])box(g,x,6.85,side*135,.16,.5,112,0x4a5956);
  for(const z0 of [90,114,138,162,186])for(const x of [-149,-141])box(g,x,2.86,side*z0,1.3,5.92,2.2,0x767d71);
  for(let z=80;z<191;z+=1.6)box(g,-145,6.94,side*z,6.3,.18,.32,0x594e3c);
  for(const x of [-147.1,-146,-144,-142.9])box(g,x,7.07,side*135,.13,.15,112,0x727e7f);
 }
 return g;
}
