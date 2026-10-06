import * as THREE from './vendor/three.module.js';
import {entranceForBuilding,decorateAddress} from './city-addresses.js?v=0.8.1';
import {districtMaterial,fitFacadeUV,districtWindowSet} from './district-materials.js?v=0.8.1';

// Scenery only: no addresses, interaction targets or collision additions.
export function decorativeStorefront(parent,kit,{kind='bakery',width=5,windows=[],seed=0}){
 const {box,cylinder,sphere,sign,mat}=kit,g=new THREE.Group();g.name='Display · '+kind;parent.add(g);
 const style={bakery:['KRUME · BACKSTUBE',0x6c5743],pharmacy:['LINDEN APOTHEKE',0x475d53],restaurant:['TISCH & ZEIT',0x724f42],boutique:['FADENWERK',0x525c63],office:['KONTOR · EINGANG',0x4c6067],kiosk:['PRESSE · REISEBEDARF',0x5c6250]}[kind]||['WERKHALLE',0x5c6250],color=style[1];
 box(g,0,1.68,.04,width,2.78,.08,color);
 for(const side of [-1,1]){
  box(g,side*width*.25,1.64,.105,width*.46,2.35,.04,0,windows[(seed+(side===1?1:3))%5]||mat(0x465c62));
  box(g,side*(width/2-.07),1.68,.18,.11,2.8,.16,0xb5b3a3);
 }
 box(g,0,1.62,.17,.075,2.4,.1,0xb5b3a3);box(g,0,.44,.2,width,.22,.25,color);
 box(g,0,2.98,.17,width,.44,.2,color);sign(g,style[0],0,2.98,.276,width-.25,.25,'#e8e0cb','#'+color.toString(16).padStart(6,'0'));
 if(kind==='bakery'||kind==='restaurant'){
  const awning=box(g,0,2.68,.23,width+.1,.09,.36,color);awning.rotation.x=.14;
  for(let n=0;n<6;n++)box(g,-width*.42+n*width*.168,2.73,.24,.18,.014,.34,0xc5b798);
  box(g,0,.94,.17,width-.35,.08,.16,0xb19970);
  if(kind==='bakery')for(let n=0;n<6;n++){const loaf=sphere(g,-width*.36+n*width*.145,1.07,.19,.13,0xb69562);loaf.scale.set(1.5,.65,.55);}
  else {box(g,-width*.24,1.61,.15,1.15,1.1,.018,0x283e3e);sign(g,'MITTAGSTISCH',-width*.24,1.84,.166,.95,.13,'#e3d3b2','#283e3e');for(let n=0;n<4;n++)box(g,-width*.24,1.65-n*.13,.17,.75,.022,.01,0xbcb59e);}
 }else if(kind==='pharmacy'){
  for(const [w,h] of [[.22,.8],[.8,.22]])box(g,-width*.25,1.9,.18,w,h,.035,0x899e83);
  for(let n=0;n<5;n++)box(g,width*.08+n*.14,.98,.17,.09,.32+(n%2)*.13,.08,0xc2c8b7);
 }else if(kind==='boutique'){
  for(const side of [-1,1]){cylinder(g,side*width*.25,1.35,.19,.018,1.5,0x929b94);const cloth=box(g,side*width*.25,1.67,.2,.56,.8,.08,side<0?0x88745f:0x53686a);cloth.rotation.z=side*.05;box(g,side*width*.25,.65,.2,.6,.06,.13,0x929b94);}
 }else if(kind==='office'){
  for(let n=0;n<4;n++)box(g,width*.27,1.84-n*.19,.18,.65,.06,.02,0xb5b3a3);
 }else for(let n=0;n<6;n++){box(g,-width*.37+n*width*.145,1.17,.17,.42,.65,.055,[0xbcb095,0x82928a,0xa08b73][n%3]);box(g,-width*.37+n*width*.145,1.35,.206,.32,.06,.016,0x485b56);}
 return g;
}

// The working port uses the same footprint/door and camera blocker as before.
export function portBuilding(art,x,z,w,d,h,color,name,face=1){
 const glazing=districtWindowSet(art.windows,x,z);
 const {box,cylinder,sign,mat}=art.k,g=new THREE.Group();g.name='Port warehouse · '+name;art.w.scene.add(g);art.w.staticGroups.push(g);art.index++;
 art.w.colliders.push({x,z,w:w/2+.35,d:d/2+.35,h:h+2.8});
 fitFacadeUV(box(g,x,h/2,z,w,h,d,0,districtMaterial('brick',w,h,0xa4947d)),w,d);
 box(g,x,3.6,z,w+.1,.26,d+.1,0x8b958b);
 for(const [xx,zz,width,angle] of [[x,z+d/2,w,0],[x,z-d/2,w,Math.PI],[x+w/2,z,d,Math.PI/2],[x-w/2,z,d,-Math.PI/2]]){
  const f=new THREE.Group();f.position.set(xx,0,zz);f.rotation.y=angle;g.add(f);
  for(let u=-width/2+.7;u<width/2;u+=3.2){box(f,u,h*.5,.11,.14,h-.3,.18,0x737f7c);box(f,u+1.1,h-2,.1,1.8,1.2,.08,0,glazing[1]);box(f,u+1.1,h-2,.17,.07,1.2,.045,0xa4aaa0);}
  box(f,0,.63,.11,width,1.1,.14,0x5d6a66);
 }
 const f=new THREE.Group();f.position.set(x,0,z+face*d/2);f.rotation.y=face===1?0:Math.PI;g.add(f);
 const entry=entranceForBuilding(x,z,d,face),doorX=entry?(entry.x-x)*face:0;
 for(const [i,u] of [-w*.29,-w*.035].entries()){
  if(Math.abs(u-doorX)<3.5)continue;
  box(f,u,2.25,.17,6,4.3,.18,0x283c40);box(f,u,2.27,.27,5.55,3.92,.05,0x697876);
  for(let y=.55;y<4.1;y+=.38)box(f,u,y,.304,5.45,.025,.017,0x3e5355);
  for(const side of [-1,1]){box(f,u+side*2.9,2.22,.27,.16,4.4,.16,0xb8ad8f);for(let y=.6;y<1.7;y+=.32)box(f,u+side*2.9,y,.359,.16,.13,.014,0x4a5048);}
  sign(f,'0'+(i+1)+' · VERLADUNG',u,4.62,.19,3.7,.24,'#dfd8bf','#435651');
 }
 if(entry)decorateAddress(f,art.k,entry,doorX);
 box(f,0,5.6,.15,w-.5,.75,.18,0x3c5153);sign(f,name,0,5.61,.25,w*.82,.43,'#ddd9c6','#3c5153');
 // Three northlight roof ridges replace the residential roof and chimney silhouette.
 for(let i=0;i<3;i++){
  const zz=z-d*.32+i*d*.32,span=d*.3,angle=Math.atan2(1.25,span);
  const roof=box(g,x,h+.65,zz,w+.2,.14,Math.hypot(span,1.25),0x4b6063);roof.rotation.x=-angle;
  box(g,x,h+.68,zz+span/2,w,1.3,.09,0,glazing[1]);box(g,x,h+1.36,zz+span/2,w+.2,.1,.14,0x818e89);
  // Closed end cheeks remain entirely inside the existing warehouse blocker.
  for(const side of [-1,1])box(g,x+side*(w/2-.06),h+.5,zz,.12,1,span,0x6c7b75);
 }
 for(const side of [-1,1]){cylinder(g,x+side*w*.43,h+.95,z-d*.3,.32,1.9,0x697b7c);box(g,x+side*w*.43,h+1.92,z-d*.3,.86,.12,.86,0x4b6063);}
 return g;
}

export function buildCoreFrontages(world,kit){
 const {box,cylinder,sign}=kit,g=new THREE.Group();g.name='District facade details';g.userData.exterior=true;world.scene.add(g);world.staticGroups.push(g);
 // Corner display windows on existing side walls, never over a real entrance.
 for(const [x,z,angle,kind,width] of [[14,-23,-Math.PI/2,'pharmacy',5],[79.5,-23,Math.PI/2,'restaurant',5],[-48,-20,-Math.PI/2,'bakery',4],[-193,27,-Math.PI/2,'kiosk',4],[48.5,23,Math.PI/2,'boutique',5],[44,-93,Math.PI/2,'office',5]]){
  const f=new THREE.Group();f.position.set(x,0,z);f.rotation.y=angle;g.add(f);decorativeStorefront(f,kit,{kind,width,windows:districtWindowSet(world.art.windows,x,z)});
 }
 // Domestic details remain on the existing balconies and walls.
 for(const [x,z,w,face] of [[-35,28,29,-1],[-74,28,28,-1]]){
  const f=new THREE.Group();f.position.set(x,0,z+face*14);f.rotation.y=Math.PI;g.add(f);
  for(const u of [-w*.34,w*.34]){
   box(f,u,7.25,.21,2.1,.25,.25,0x918371);for(let n=0;n<5;n++)kit.sphere(f,u-.8+n*.4,7.48,.23,.19,0x637a56);
   box(f,u,9.8,.19,2.15,.05,.09,0x9c9a89);for(let n=0;n<3;n++)box(f,u-.65+n*.65,9.5,.2,.45,.56,.035,[0xc0b7a1,0x829799,0xb8bdb0][n]);
  }
  for(const side of [-1,1]){box(f,side*2,2.1,.19,.36,.6,.22,0x52635e);box(f,side*2,2.1,.307,.24,.37,.014,0xd7c39c);}
 }
 // Wall-mounted service hardware belongs to the station back lanes, not the footway.
 for(const [x,z,angle] of [[-212,-29,Math.PI/2],[-212,32,Math.PI/2],[-120,-32,Math.PI/2]]){
  const f=new THREE.Group();f.position.set(x,0,z);f.rotation.y=angle;g.add(f);
  box(f,0,1.35,.09,1.2,2.1,.15,0x59645b);box(f,.35,1.33,.18,.07,.23,.06,0xa7a58f);
  box(f,1.6,1.32,.12,.75,.9,.2,0x8a8a78);for(let n=0;n<4;n++)box(f,1.6,1.12+n*.12,.228,.54,.025,.02,0x4c5b51);
  cylinder(f,-1.15,2.2,.18,.045,4.4,0x677269);
  box(f,-2.25,1.65,.1,.74,1.1,.045,0xb9ad91);sign(f,'AM GLEIS',-2.25,1.88,.127,.65,.13,'#344947','#b9ad91');
 }
 return g;
}
