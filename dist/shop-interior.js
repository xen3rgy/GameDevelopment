import * as THREE from './vendor/three.module.js';
import {createBread,createProduce} from './food-models.js?v=0.8.1';
import {ITEMS,euro} from './data.js?v=0.8.1';
import {SHOP_FIXTURES} from './spatial.js?v=0.8.1';
import {createCitizen,seeded} from './art.js?v=0.8.1';

// Every sales fixture shares its footprint with walking and camera collision.
export function buildMarket(world,k){
 const {box,cylinder,sphere,sign,mat}=k,g=new THREE.Group();world.scene.add(g);world.shop=g;
 const cream=0xe0ded2,green=0x294c41,oak=0x9f7950,steel=0x758489;
 const metal=mat(0xb5bec0,.32,.65),glass=new THREE.MeshStandardMaterial({color:0xafd6df,transparent:true,opacity:.13,roughness:.12,metalness:.2,depthWrite:false});
 const led=new THREE.MeshBasicMaterial({color:0xfff3db}),labels=new Map();
 const part=(parent,x,y,z,w,h,d,color,material)=>box(parent,x,y,z,w,h,d,color,material);
 function groupAt(x,z,rotation=0){const n=new THREE.Group();n.position.set(x,0,z);n.rotation.y=rotation;g.add(n);return n}
 function labelMaterial(id){
  if(labels.has(id))return labels.get(id);const c=document.createElement('canvas');c.width=256;c.height=256;const a=c.getContext('2d');
  const colors={water:'#e3eef0',coffee:'#c2a174',pasta:'#ebc061',vegetables:'#bbce93',bread:'#dbb477',cheese:'#e8d599',meal:'#c9d3b5',sandwich:'#dccd9e',medicine:'#e7e9df'};
  a.fillStyle=colors[id];a.fillRect(0,0,256,256);a.fillStyle='#284b40';a.fillRect(0,0,256,49);a.fillStyle='#f1ead6';a.font='bold 24px Arial';a.textAlign='center';a.fillText('MARKT 24',128,33);a.fillStyle='#29443b';a.font='bold 28px Arial';a.fillText(ITEMS[id].name,128,104,236);a.font='19px Arial';a.fillText(id==='water'?'STILL · 500 ml':id==='pasta'?'HARTWEIZEN':id==='cheese'?'MILD & CREMIG':'GUT FÜR DEINEN TAG',128,145,232);
  a.fillStyle='#f7f2e7';a.fillRect(28,188,200,43);a.fillStyle='#263c38';for(let n=0;n<45;n++)a.fillRect(39+n*4,194,n%3===0?2:1,28);
  const texture=new THREE.CanvasTexture(c);texture.colorSpace=THREE.SRGBColorSpace;const m=new THREE.MeshStandardMaterial({map:texture,roughness:.75});labels.set(id,m);return m;
 }
 function product(parent,id,x,y,z,rotation=0){
  const n=new THREE.Group();n.position.set(x,y,z);n.rotation.y=rotation;parent.add(n);
  if(id==='water'){
   cylinder(n,0,.19,0,.075,.35,0x8fbcc5);cylinder(n,0,.398,0,.035,.08,0xb7d5d8);cylinder(n,0,.447,0,.037,.026,0x385e76);part(n,0,.23,.073,.12,.15,.007,0,labelMaterial(id));
  }else if(id==='coffee'){
   cylinder(n,0,.15,0,.085,.29,0xb49166);cylinder(n,0,.302,0,.094,.034,0xefe3c9);part(n,0,.16,.083,.13,.17,.007,0,labelMaterial(id));
  }else if(id==='sandwich'||id==='meal'){
   part(n,0,.04,0,.32,.075,.24,0x304a3f);part(n,0,.09,0,.30,.025,.22,id==='sandwich'?0xd8b477:0xb1bb79);part(n,0,.122,0,.32,.022,.24,0,labelMaterial(id));
  }else{
   const dimensions=id==='pasta'?[.19,.38,.13]:id==='medicine'?[.14,.22,.1]:id==='cheese'?[.23,.19,.15]:[.25,.3,.17];const [w,h,d]=dimensions;
   part(n,0,h/2,0,w,h,d,id==='pasta'?0xd3a34c:id==='cheese'?0xd0bc7b:0xc9b999);part(n,0,h/2,d/2+.004,w*.92,h*.92,.006,0,labelMaterial(id));
  }
  return n;
 }
 function price(parent,id,x,y,z,w=.64,rotation=0){return sign(parent,ITEMS[id].name.toUpperCase()+'   '+euro(ITEMS[id].price),x,y,z,w,.125,'#263b33','#f6f2df',rotation)}
 function header(text,x,z,width=2.8,rotate=0){const n=groupAt(x,z,rotate);for(const dx of [-width*.4,width*.4])part(n,dx,3.25,0,.018,.75,.018,steel);part(n,0,2.89,0,width,.42,.055,green);sign(n,text,0,2.9,.032,width-.15,.31,'#f0ead7','#294c41');sign(n,text,0,2.9,-.032,width-.15,.31,'#f0ead7','#294c41',Math.PI)}
 // Small, subtly mottled porcelain tiles with narrow joints.
 const tile=document.createElement('canvas');tile.width=tile.height=512;const ctx=tile.getContext('2d'),random=seeded(291);
 ctx.fillStyle='#d7d7ce';ctx.fillRect(0,0,512,512);for(let i=0;i<12500;i++){ctx.fillStyle=i%2?'#6e797713':'#ffffff30';ctx.fillRect(random()*512,random()*512,1+random()*2,1+random()*2)}ctx.strokeStyle='#9da6a14d';ctx.lineWidth=2;ctx.strokeRect(1,1,510,510);
 const tex=new THREE.CanvasTexture(tile);tex.wrapS=tex.wrapT=THREE.RepeatWrapping;tex.repeat.set(14,18);tex.colorSpace=THREE.SRGBColorSpace;tex.anisotropy=4;
 part(g,340,-.08,40,14,.3,18,0,new THREE.MeshStandardMaterial({map:tex,color:0xf0efdf,roughness:.48,bumpMap:tex,bumpScale:.012}));
 part(g,340,1.92,31,14,3.85,.24,cream);for(const x of [333,347]){part(g,x,1.92,40,.24,3.85,18,cream);part(g,x+(x===333?.13:-.13),.55,40,.025,.9,17.7,green);part(g,x+(x===333?.145:-.145),.13,40,.035,.14,17.7,0x65756d)}
 for(const x of [336,344])part(g,x,1.92,49,6,3.85,.24,cream);
 part(g,340,3.38,49,2,1,.24,cream);
 // Closed ceiling: the shop is lit by its own fixtures, including after dark.
 part(g,340,3.88,40,14,.12,18,0xd9ddd6);for(let x=334;x<347;x+=2)part(g,x,3.805,40,.023,.018,17.7,0xa1ada7);for(let z=32;z<49;z+=2)part(g,340,3.805,z,13.7,.018,.023,0xa1ada7);
 for(const x of [336,343])for(const z of [34,40,46]){part(g,x,3.78,z,1.15,.065,2.2,0xb0b8b2);part(g,x,3.73,z,.98,.025,2,0,led);const light=new THREE.PointLight(0xfff2df,28,12,2);light.position.set(x,3.38,z);g.add(light)}
 // Glazed exit doors, vestibule mat, basket stack and visible exit signage.
 for(const x of [339.49,340.51]){part(g,x,1.43,48.85,.98,2.68,.075,0x283f3e);part(g,x,1.58,48.79,.80,2.1,.025,0x617f85);part(g,x,1.2,48.76,.80,.11,.025,0xb4c5bb);part(g,x+(x<340?.34:-.34),1.2,48.69,.032,.47,.08,0,metal)}
 sign(g,'AUSGANG',340,3.15,48.72,1.75,.27,'#e7f3dc','#286148',Math.PI);sign(g,'E · VERLASSEN',340,2.7,48.73,1.75,.22,'#f1e4c5','#294c41',Math.PI);part(g,340,.079,47.55,2.15,.016,2,0x3d4c48);
 for(let n=0;n<5;n++){part(g,341.6,.18+n*.10,47.45,.58,.07,.42,0x8a4c36);for(const dx of [-.28,.28])part(g,341.6+dx,.26+n*.10,47.45,.025,.16,.43,0xb26642)}sign(g,'EINKAUFSKÖRBE',341.6,.87,47.21,1.0,.15,'#355343','#e4dec9',Math.PI);
 sign(g,'MARKT 24',339,3.08,31.16,4.6,.58,'#e8dbc0','#294c41');sign(g,'FRISCH. NAH. DEIN MARKT.',339,2.62,31.17,4.2,.23,'#365a48','#e0ded2');
 // Drinks fridge: open front, recessed stocked shelves, glass dividers and LED trim.
 const fridge=groupAt(334.3,38.5,Math.PI/2);
 part(fridge,0,1.3,-.59,7,2.46,.12,0x334b4b);for(const x of [-3.46,3.46])part(fridge,x,1.3,0,.08,2.46,1.3,0x334b4b);part(fridge,0,1.29,.34,6.77,2.22,.045,0xc6d5d1);part(fridge,0,.2,.1,7.03,.24,1.34,0x4d6261);
 for(const y of [.4,.94,1.48]){part(fridge,0,y,.32,6.72,.045,.54,0,metal);part(fridge,0,y-.025,.61,6.75,.1,.065,0xdde1d5);for(let col=0;col<22;col++)product(fridge,col<13?'water':'coffee',-3.13+col*.295,y+.025,.4);price(fridge,'water',-1.72,y,.657,1.25);price(fridge,'coffee',1.85,y,.657,1.25)}
 for(let n=0;n<5;n++){const x=-3.35+n*1.67;part(fridge,x,1.34,.655,.055,2.16,.055,0,metal);part(fridge,x+.07,1.4,.622,.015,1.94,.01,0,led)}
 for(let n=0;n<4;n++){part(fridge,-2.52+n*1.67,1.39,.685,1.57,2.04,.012,0,glass);part(fridge,-1.87+n*1.67,1.31,.727,.036,.45,.055,0x405453)}
 part(fridge,0,2.55,.3,7.05,.19,.77,green);sign(fridge,'GEKÜHLTE GETRÄNKE',0,2.56,.704,4.6,.14,'#e6eadb','#294c41');header('01  GETRÄNKE',334.9,42.55);
 // Fresh food island: open chilled trays instead of another tall solid shelf.
 const fresh=groupAt(338.3,38.5,Math.PI/2);
 part(fresh,0,.53,0,7, .91,1.3,oak);for(let x=-3.35;x<3.5;x+=.22)part(fresh,x,.53,.658,.025,.87,.025,0x765a3c);
 part(fresh,0,1.02,0,7.05,.12,1.34,0,metal);part(fresh,0,1.10,0,6.8,.09,1.10,0x384b47);
 for(let col=0;col<17;col++)for(const z of [-.31,.2])product(fresh,col<8?'sandwich':'meal',-3.14+col*.39,1.155,z);
 for(const x of [-3.5,3.5])part(fresh,x,1.37,0,.035,.62,1.3,0,glass);for(const z of [-.65,.65])part(fresh,0,1.31,z,7,.35,.018,0,glass);
 price(fresh,'sandwich',-1.85,1.01,.692,1.35);price(fresh,'meal',1.75,1.01,.692,1.35);header('02  FRISCHE & SNACKS',338.7,42.55,3.1);
 // Grocery gondola: slim metal spine, adjustable shelves and actual labeled packs.
 const grocery=groupAt(342.3,38.5,Math.PI/2);
 part(grocery,0,1.01,0,7,1.85,.10,0xc6cdc1);for(const x of [-3.44,0,3.44])part(grocery,x,1.06,0,.065,1.98,.15,0x536b60);
 for(const side of [-1,1])for(const y of [.24,.78,1.32]){part(grocery,0,y,side*.35,7,.055,.62,0xdedfcf);part(grocery,0,y,side*.669,7,.10,.032,green);for(let col=0;col<19;col++){const id=['pasta','vegetables','bread','cheese'][Math.floor(col/5)%4];product(grocery,id,-3.2+col*.354,y+.03,side*.42,side<0?Math.PI:0)}for(const [n,id] of ['pasta','vegetables','bread','cheese'].entries())price(grocery,id,-2.6+n*1.73,y,side*.69,1.22,side<0?Math.PI:0)}header('03  VORRAT & ZUTATEN',342.6,42.55,3.25);
 // Back-wall health cabinet.
 const health=groupAt(344,32);
 part(health,0,1.10,-.49,4,2.08,.12,0xd4dace);for(const x of [-1.94,1.94])part(health,x,1.1,0,.12,2.08,1.1,0xd4dace);for(const y of [.32,.87,1.42]){part(health,0,y,.16,3.78,.06,.72,0x728779);for(let col=0;col<17;col++)product(health,'medicine',-1.7+col*.21,y+.04,.35);price(health,'medicine',0,y,.56,1.2)}sign(health,'04  HAUSAPOTHEKE',0,2.32,.55,3.6,.3,'#365442','#e0ded2');
 // Produce table in the entrance area, with individual tilted slatted crates.
 const produce=groupAt(334.8,44.6);
 part(produce,0,.53,0,2.6,.9,1.6,0x6d573d);for(let col=0;col<3;col++){const x=-.84+col*.84;part(produce,x,1.03,0,.79,.12,1.48,oak);for(const side of [-1,1])part(produce,x+side*.38,1.19,0,.035,.24,1.5,0xbd9760);for(const z of [-.73,.73])for(const y of [1.13,1.24])part(produce,x,y,z,.80,.065,.04,0xbd9760);for(let n=0;n<18;n++){const px=x-.25+(n%3)*.25,pz=-.55+Math.floor(n/3)*.22,fruit=createProduce(['tomato','apple','pepper'][col],n);fruit.position.set(px,1.10,pz);fruit.rotation.y=n*.71;produce.add(fruit)}}price(produce,'vegetables',0,.87,.815,1.7);header('OBST & GEMÜSE',334.8,44.6,2.8);sign(produce,'FRISCH AUS DER REGION',0,.56,.816,2.3,.21,'#ede0c2','#6d573d');
 // Bakery at the rear, reachable from the cross aisle.
 const bakery=groupAt(337,32);
 part(bakery,0,1.05,-.47,3.2,1.92,.12,0x99794f);for(const x of [-1.55,1.55])part(bakery,x,1.05,0,.1,1.92,1.05,0x99794f);for(const y of [.48,1.04,1.6]){part(bakery,0,y,.13,2.98,.09,.78,0xcaa571);for(let col=0;col<9;col++){const loaf=createBread(col);loaf.position.set(-1.29+col*.32,y+.048,.20);bakery.add(loaf)}price(bakery,'bread',0,y,.557,1.25)}sign(bakery,'BACKWAREN',0,2.3,.55,3,.3,'#60432b','#e0ded2');
 // Checkout with belt, scanner, POS, card reader and packing shelf.
 const till=groupAt(344.6,45.5);
 part(till,0,.57,0,2.8,1.0,1.2,green);part(till,0,1.10,0,2.95,.1,1.35,0,metal);part(till,-.63,1.164,0,1.44,.025,1.04,0x283431);for(let n=0;n<12;n++)part(till,-1.28+n*.12,1.18,0,.008,.004,1.03,0x48554e);
 part(till,.28,1.175,0,.35,.035,.57,0x242e30);part(till,.28,1.197,0,.22,.005,.41,0x904945);part(till,.90,1.5,.21,.48,.36,.09,0x203330);part(till,.90,1.48,.155,.4,.26,.016,0x91b3a2);sign(till,'MARKT 24',.90,1.49,.142,.36,.07,'#d8ebd9','#2b5145',Math.PI);
 part(till,.9,1.31,.21,.045,.3,.06,0x53645b);part(till,.68,1.24,-.44,.20,.09,.25,0x2a3b3a);part(till,.68,1.293,-.45,.14,.012,.12,0x9cbdad);part(till,1.12,1.175,0,.35,.04,1.03,0xadb5a9);
 sign(till,'HIER BEZAHLEN',0,.8,-.612,2.3,.23,'#ede7d1','#294c41',Math.PI);header('KASSE  01',344.6,45.5,2.7,Math.PI);
 const cashier=createCitizen(k,0x446d54,0xcba582,2);cashier.position.set(344.5,.02,46.75);cashier.rotation.y=Math.PI;g.add(cashier);
 // Keep the visual fixture dimensions inspectable without relying on renderer state.
 g.userData.fixtureFootprints=SHOP_FIXTURES.map(c=>({...c}));
 return g;
}
