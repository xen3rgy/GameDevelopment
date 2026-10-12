import {addWestTree,westShrub} from './west-vegetation.js?v=0.8.1';
import {westStoreShed,westFenceMesh} from './west-details.js?v=0.8.1';
import * as THREE from './vendor/three.module.js';
import {WEST_BUILDINGS,WEST_BUILDING_FIXTURES,WEST_PATHS,WEST_BOTTLE_SPOTS} from './west-edge-layout.js?v=0.8.1';
import {groundHeight} from './spatial.js?v=0.8.1';
import {westBox,westGround,westWallArt,westMaterial,westWeathering} from './west-materials.js?v=0.8.1';
import {westParkedColliders} from './west-parked.js?v=0.8.1';
import {createCar} from './art.js?v=0.8.1';

// Static, spatially batched streetscape. Every substantial prop has a collider;
// all entrance, bottle, bus and NPC corridors are checked against the actual scene.
export function dressWest(world,kit,{group,solid,line,bench,pallet}){
 const {box,cylinder,sign,mat}=kit;
 // Recessed service covers and worn parking edges give the street human scale.
 for(const [x,z] of [[-518,-.7],[-459,1.1],[-402,-1.4],[-347,.6],[-283,-1]]){
  const g=group(x,z);cylinder(g,x,.143,z,.44,.004,0x555b56);cylinder(g,x,.147,z,.385,.004,0x3e4542);
  for(let n=-2;n<=2;n++)box(g,x,.151,z+n*.105,.56,.003,.022,0x687069);
 }
 const shrub=(x,z,y=groundHeight(x,z),scale=1)=>westShrub(group(x,z),x,y,z,scale,Math.abs(Math.round(x*11+z*7)));
 const wall=(x,z,w,d,h=1.05)=>{const y=groundHeight(x,z);westBox(group(x,z),'concrete',x,y+h/2,z,w,h,d);westBox(group(x,z),'concrete',x,y+h+.05,z,w+.10,.1,d+.10);solid(x,z,w+.1,d+.1,y+h+.1);};
 const planter=(x,z,w,d)=>{const g=group(x,z),y=groundHeight(x,z);westBox(g,'concrete',x,y+.21,z,w,.42,d);westGround(g,'soil',x,z,w-.22,d-.22,y+.43);solid(x,z,w,d,y+.45);for(let n=0;n<Math.max(2,Math.floor(w/1.5));n++)shrub(x-w/2+.65+n*1.4,z,y+.43,.9);};

 // The underpass has a load-bearing deck, weathered abutments, drainage,
 // expansion joints, safety paint and separate wall artwork instead of flat slabs.
 const bridge=group(-422,0);
 for(const x of [-425.7,-418.3]){
  westBox(bridge,'steel',x,5.64,0,.28,.60,18.2);
  for(const y of [5.33,5.93])westBox(bridge,'steel',x,y,0,.50,.09,18.4);
  for(let z=-8;z<=8;z+=2.1){box(bridge,x,5.62,z,.34,.46,.065,0x858b7f);for(const yy of [5.47,5.78])box(bridge,x-.18,yy,z,.018,.06,.06,0xb3aca0);}
  for(const z of [-9.08,9.08]){
   westBox(bridge,'concrete',x,2.61,z,.75,5.28,.23);
   box(bridge,x-.04,1.5,z-Math.sign(z)*.13,.44,1.8,.018,0xd2c3a1);
   for(let i=0;i<4;i++){const stripe=box(bridge,x-.04,.81+i*.42,z-Math.sign(z)*.145,.45,.16,.025,0x42494a);stripe.rotation.z=.18;}
  }
 }
 for(const z of [-9.085,9.085]){
  const side=Math.sign(z);westWallArt(bridge,side<0?'rail':'home',-422,2.15,z-side*.026,5.8,2.5,side<0?0:Math.PI);
  line(bridge,{x:-425.6,y:4.75,z:z-side*.19},{x:-418.4,y:4.75,z:z-side*.19},.08,0x4e5755);
  westBox(bridge,'concrete',-422,.34,z-side*.1,8,.38,.24);
  for(const x of [-425.2,-418.8]){cylinder(bridge,x,2.65,z-side*.24,.065,4.5,0x646b67);for(const y of [.7,2.5,4.3])box(bridge,x,y,z-side*.25,.20,.05,.10,0x8c9087);}
 }
 for(const x of [-424.7,-419.3]){westBox(bridge,'steel',x,5.19,0,.30,.16,17.9);box(bridge,x,5.08,0,.36,.06,1.2,0xddd0ad);world.atmosphere.registerLamp(x,0,null,{height:4.95,power:45,poolSize:5,distance:10});}
 sign(bridge,'ALTGLEIS · 1932',-426.02,5.62,0,4.8,.30,'#d0c6ad','#454d48',-Math.PI/2);
 // Raised ballast extends continuously over the sloping approaches. A textured
 // retaining wall anchors each embankment edge instead of a green vertical wedge.
 for(const side of [-1,1])for(const x of [-426.06,-417.94]){
  for(let n=0;n<8;n++){const z=side*(14+n*5.9),h=5.6-n*.69;westBox(bridge,'brick',x,h/2-.05,z,.13,h,5.92);westBox(bridge,'concrete',x,h,z,.22,.13,5.92);world.colliders.push({x,z,w:.11,d:2.96,h:h+.065,westRetainingWall:true});}
 }

 for(const b of WEST_BUILDINGS){
  const g=group(b.x,b.z),face=b.face,front=b.z+face*b.d/2,local=new THREE.Group();local.position.set(b.x,0,front);local.rotation.y=face<0?Math.PI:0;g.add(local);
  if(!['shelter','kiosk'].includes(b.style))for(const [xx,zz,width,rot] of [[b.x,b.z+b.d/2+.018,b.w,0],[b.x,b.z-b.d/2-.018,b.w,Math.PI],[b.x+b.w/2+.018,b.z,b.d,Math.PI/2],[b.x-b.w/2-.018,b.z,b.d,-Math.PI/2]])westWeathering(g,xx,zz,width,b.h,rot,b.seed);
  if(b.style==='residential'){
   // Match the actual facade window grid and floor levels. Open balustrades
   // replace the old oversized opaque boxes and duplicated shallow balconies.
   const cols=Math.max(3,Math.floor(b.w/(3.5+(b.seed%2)*.5))),pitch=(b.w-1)/cols,rows=Math.max(1,Math.floor((b.h-4)/3.3)),step=(b.h-4)/rows;
   for(let row=0;row<rows;row++)for(let col=0;col<cols;col++){
    if(col%2!==b.seed%2)continue;
    const dx=-b.w/2+.5+(col+.5)*pitch,y=3.80+row*step,w=2.35;
    westBox(local,'concrete',dx,y,.59,w,.15,1.25);
    for(const side of [-1,1]){box(local,dx+side*w/2,y+.55,.61,.045,1.04,1.22,0x434b46);box(local,dx+side*w/2,y+1.08,.61,.055,.045,1.25,0x697265);}
    for(let n=0;n<=12;n++)box(local,dx-w/2+n*w/12,y+.57,1.23,.026,1.02,.026,0x4f574c);
    box(local,dx,y+1.08,1.23,w+.07,.05,.055,0x697265);box(local,dx,y+.08,1.23,w,.045,.035,0x4f574c);
    world.overheadColliders.push({x:b.x+dx*b.face,z:front+b.face*.6,w:w/2,d:.64,minY:y-.08,h:y+.09,westBalcony:true});
    if((b.seed+row+col)%3===0){westBox(local,'wood',dx+.64,y+.25,1.0,.68,.31,.27);westShrub(local,dx+.64,y+.40,1,.23,b.seed+row);}
    if((b.seed+row+col)%5===1)box(local,dx-.35,y+.83,1.255,.62,.8,.018,0x9a8f79);
   }
   for(const dx of [-b.w/2+.35,b.w/2-.35]){cylinder(local,dx,b.h/2,.19,.055,b.h,0x6e746c);box(local,dx,.9,.19,.23,.35,.27,0x59645e);}
   westWallArt(local,'posters',b.w*.32,1.57,.21,1.25,1.15);
   westBox(local,'steel',0,2.95,.9,3.4,.13,1.8);for(const dx of [-1.4,1.4])line(local,{x:dx,y:2.9,z:.1},{x:dx,y:2.86,z:1.55},.055,0x576660);
   world.overheadColliders.push({x:b.x,z:front+face*.9,w:1.7,d:.9,minY:2.885,h:3.015,westCanopy:true});
   for(const dx of [-.9,-.6,-.3])box(local,dx,1.3,.27,.23,.32,.08,0xa09f8e);
  }else if(b.style==='townhouse'){
   for(const dx of [-b.w*.32,b.w*.32]){westBox(local,'wood',dx,1.02,.42,1.6,.24,.4);box(local,dx,1.21,.42,1.5,.25,.32,0x69794d);for(const side of [-1,1]){westBox(local,'steel',dx+side*.55,.875,.36,.055,.05,.64);westBox(local,'steel',dx+side*.55,.75,.045,.055,.3,.08);}}
   westWallArt(local,'posters',b.w*.37,1.55,.29,.6,.75);
  }else if(b.style==='industrial'){
   if(b.h>5.4)for(const dx of [-b.w*.3,0,b.w*.3]){box(local,dx,b.h-.85,.10,b.w*.25,.72,.13,0x6c7269);box(local,dx,b.h-.85,.18,b.w*.25-.12,.58,.025,0x384b4b);box(local,dx,b.h-.85,.20,.04,.6,.025,0x868b7c);}
   for(const dx of [-b.w*.31,b.w*.31]){westBox(local,'steel',dx,1.7,.23,Math.min(4,b.w*.28),2.85,.12);for(let y=.45;y<3;y+=.32)box(local,dx,y,.31,Math.min(3.8,b.w*.27),.025,.025,0x8d9285);}
   const title=b.id==='bruchRepair'?'RAD & REPARATUR':b.id==='railAnnex'?'GEBRAUCHTTEILE':b.id==='railWorkshop'?'GLEISWERK · METALL & HOLZ':b.id==='railOffice'?'LAGER 03':b.id==='railStore'?'ALTES GLEISDREIECK':b.id==='railSignal'?'STELLWERK 04':b.id==='railRearWarehouse'?'LAGERHOF · ANNAHME':b.id==='railSouthWorks'?'ALTGLEIS · BAUTEILE':'ANLIEFERUNG';westBox(local,'steel',0,3.72,.36,Math.min(b.w-1,10.4),.58,.1);sign(local,title,0,3.72,.417,Math.min(b.w-1.4,10),.34,'#e0d2ad','#384e4b');
   westWallArt(local,'rail',b.w*.32,1.5,.33,2.2,1.05);
   if(b.id==='railStore'||b.id==='railWorkshop'){box(local,0,3.22,.75,1.25,.08,.32,0xd7c9a1);world.atmosphere.registerLamp(b.x,front+face*1.0,null,{height:3.18,power:55,poolSize:5,distance:11});}
  }
 }

 // Uneven, occupied street edges: repair yards, fenced gaps and everyday debris.
 // Keep the centre of every pavement and all interaction approaches open.
 for(const [x,z,length] of [[-445.85,-11,5.3],[-440.15,-11,1.3],[-308,47,8]]){
  const g=group(x,z);for(let n=0;n<=Math.ceil(length/2);n++){const xx=x-length/2+n*length/Math.ceil(length/2);westBox(g,'steel',xx,.72,z,.065,1.44,.065);}
  for(const yy of [.25,.8,1.35])line(g,{x:x-length/2,y:yy,z},{x:x+length/2,y:yy,z},.016,0x747b71);
  for(let n=0;n<length*2;n++){const xx=x-length/2+n*.5;line(g,{x:xx,y:.25,z},{x:Math.min(x+length/2,xx+.65),y:1.35,z},.009,0x798074);}
  solid(x,z,length,.12,1.5);
 }
 const rubbleGeo=new THREE.DodecahedronGeometry(.13,0),rubbleMat=mat(0x85816e),paperMat=mat(0xa69d81);
 for(const [j,[cx,cz]] of [[-523,5.9],[-490,-5.8],[-457,5.8],[-433,-6],[-408,7],[-391,-7.8],[-359,7.2],[-321,-10],[-286,15.4]].entries()){
  for(let n=0;n<9;n++){const x=cx+Math.sin(n*13.2+j)*2.8,z=cz+Math.cos(n*8.1+j)*.48;if(WEST_BOTTLE_SPOTS.some(p=>Math.hypot(p.x-x,p.z-z)<1))continue;const y=groundHeight(x,z),g=group(x,z);
   if(n%3===0){const m=box(g,x,y+.012,z,.17+n*.018,.008,.24,0,paperMat);m.rotation.y=n*1.2;m.castShadow=false;}
   else{const m=new THREE.Mesh(rubbleGeo,rubbleMat);m.position.set(x,y+.055,z);m.scale.set(1+n*.08,.38,1);m.rotation.set(n,.3*n,j);m.userData.staticBatch=true;g.add(m);}
  }
 }
 for(const [x,z] of [[-503,15],[-400,17],[-361,16],[-308,44]]){const g=group(x,z),y=groundHeight(x,z);westBox(g,'steel',x,y+.55,z,1.5,1.1,.85);box(g,x,y+1.14,z,1.58,.12,.95,0x485147);solid(x,z,1.6,1, y+1.3);westWallArt(g,'posters',x,y+.65,z+.435,.65,.5);}
 for(const [x,z] of [[-502,-12],[-439,-12],[-387,14.5]]){const g=group(x,z);westWallArt(g,'home',x,.95,z,3.4,1.2);westBox(g,'concrete',x,.75,z-.03,3.6,1.5,.13);solid(x,z,3.6,.17,1.55);}

 // Bruchfeld: inhabited plots, garden boundaries and small storage yards.
 wall(-518,-14,8,.35,.75);wall(-501.85,-13,7.3,.35,.75);wall(-494.2,-13,3.4,.35,.75);wall(-486,-24,.35,18,.75);
 wall(-468,40,14,.4,.65);wall(-441.6,53,.35,15,.7);wall(-543,51,.35,16,.65);
 for(const [x,z] of [[-500,-15],[-492,-15],[-470,41],[-440.5,55],[-544,55]])shrub(x,z,groundHeight(x,z),1.25);
 for(const [x,z,w,d] of [[-498,-14.5,4,5],[-544.9,53,2,11],[-440,53,2,11]])westGround(group(x,z),'ballast',x,z,w,d,-.032);
 for(const [x,z] of [[-500,-37],[-466,-46],[-436.8,61.5]])westStoreShed(world,kit,group(x,z),x,z);
 // The garage is part of an occupied plot, with a broad driveway mouth
 // and a separate continuous pedestrian passage to the rear housing lane.
 const plotFence=(x,z,len,rotate=false)=>{const g=group(x,z),y=groundHeight(x,z);for(let n=0;n<=Math.ceil(len/2.5);n++){const t=-len/2+n*len/Math.ceil(len/2.5);westBox(g,'steel',x+(rotate?0:t),y+.53,z+(rotate?t:0),.055,1.06,.055);}westFenceMesh(g,x,y+.54,z,len,1.02,rotate?Math.PI/2:0);solid(x,z,rotate?.12:len,rotate?len:.12,y+1.08);};
 plotFence(-478.5,-19,17,true);plotFence(-453.5,-19,17,true);plotFence(-477.6,-10.3,1.8);plotFence(-473,-10.3,.5);plotFence(-460.3,-10.3,13.6);
 for(const [x,z] of [[-478,-23],[-453.9,-25],[-456,-11.1]])westShrub(group(x,z),x,groundHeight(x,z),z,1.1,Math.abs(x));
 const garageYard=group(-463,-25);pallet(-462,-26);pallet(-460.5,-26);westBox(garageYard,'steel',-457.1,.52,-25,1.4,1.08,.80);box(garageYard,-457.1,1.11,-25,1.49,.10,.88,0x767a66);solid(-457.1,-25,1.5,.9,1.18);
 sign(garageYard,'GARAGENHOF · EINFAHRT FREI',-460,1.07,-10.25,3.8,.26,'#d1c4a3','#55634f');
 // Back entrances sit on the house faces reached by the repaired rear paths.
 for(const [x,z] of [[-517,-32.5],[-520,-51.5],[-491,-52.5]]){const g=group(x,z);box(g,x,1.14,z-.07,1.02,2.22,.11,0x49584c);box(g,x+.34,1.09,z-.145,.045,.21,.04,0xa69f86);sign(g,'HOFZUGANG',x,2.36,z-.15,1.4,.19,'#ccc2a4','#56634e',Math.PI);}
 const cover=group(-511,29);for(let x=-514.2;x<-507.8;x+=.43)westBox(cover,'wood',x,1.45,31.62,.055,2.62,.04);
 sign(cover,'KEIN ORT IST DAS ENDE',-511,1.85,31.39,3.4,.29,'#c4bda3','#605e4d',Math.PI);

 // Altgleis: coherent working yards, rather than debris scattered on the road.
 for(const [x,z] of [[-405,13],[-393,13],[-365.5,34]]){pallet(x,z);pallet(x,z+1.3);const g=group(x,z),y=groundHeight(x,z)+1.08;westBox(g,'wood',x,y,z,1.6,1.8,1.3);for(const side of [-1,1]){for(const yy of [y-.8,y+.8])westBox(g,'wood',x,yy,z+side*.675,1.68,.10,.06);westBox(g,'wood',x+side*.74,y,z+.675,.09,1.65,.06);box(g,x+side*.38,y,z+.71,.035,1.82,.02,0x656b5e);}sign(g,'ERSATZTEILE',x,y+.35,z+.725,.99,.22,'#b7ad8c','#536052');solid(x,z,1.7,1.4,y+.9);}
 wall(-396,15,9,.4,.85);wall(-374,25,.35,18,.85);
 const work=group(-398,17);westGround(work,'ballast',-399,15,23,6,-.027);
 westBox(work,'steel',-402,1.05,16,2.6,2.1,1);solid(-402,16,2.6,1,2.2);
 westBox(work,'steel',-402,1.38,15.48,2.3,.48,.045);sign(work,'EINFAHRT FREI',-402,1.38,15.451,2.05,.23,'#dccdb0','#4c5a50',Math.PI);
 for(const [x,z,angle,color,type] of [[-398,11.5,Math.PI/2,0x8c958a,'van'],[-291,-29,Math.PI/2,0x7c6051,'car'],[-274,43,.07,0x576b70,'car']]){
  const car=createCar(kit,type,color);car.position.set(x,groundHeight(x,z)-.03,z);car.rotation.y=angle;group(x,z).add(car);world.colliders.push(...westParkedColliders(x,z,angle,type,car.position.y));
 }

 // Westbruch: replace the empty paved square with distinct usable edges and a
 // clear middle route. Trees are rooted in raised soil beds, not through paving.
 planter(-305,25,5,3);planter(-276,24,5,3);planter(-290,43,10,2.8);planter(-295,29,4.5,3);planter(-287,39,3.5,2.5);planter(-303,10,3,1.9);
 for(const [x,z] of [[-305,25],[-276,24],[-295,29],[-287,39],[-303,10]])addWestTree(world,x,z,2.5,{grate:false,fixed:true,ground:groundHeight(x,z)+.43,seed:1170+Math.abs(x)});
 for(const [x,z] of [[-305,30],[-272,35]])bench(x,z,Math.PI/2);
 for(const [x,z] of [[-300,26],[-288,36]]){const g=group(x,z),y=groundHeight(x,z);box(g,x,y+.56,z,.13,1.12,.13,0x43554e);box(g,x,y+1.1,z,.23,.17,.23,0xddc8a0);solid(x,z,.25,.25,1.45);world.atmosphere.registerLamp(x,z,null,{height:y+1.15,power:24,poolSize:4,distance:8});}
 const court=group(-290,32);westGround(court,'ballast',-300,35,5.5,3.2,.308);
 for(const [x,z] of [[-286,28],[-283,28]]){box(court,x,.82,z,1.8,.11,.9,0xa39a80);for(const dx of [-.65,.65])box(court,x+dx,.52,z,.08,.55,.65,0x55615b);solid(x,z,1.9,1,1.0);}
 wall(-310,42,.4,13,1.1);wall(-271,37,.4,11,.8);
 const kiosk=group(-296,-21);westWallArt(kiosk,'posters',-301,1.7,-15.82,1.1,1.25);
 for(const x of [-302.8,-290.2]){westBox(kiosk,'wood',x,.72,-14.7,1.1,.8,.7);for(let i=0;i<3;i++)box(kiosk,x-.35+i*.34,1.22,-14.7,.22,.25,.26,[0x8f704e,0x8b9860,0xb5a276][i]);solid(x,-14.7,1.2,.8,1.4);}
 sign(kiosk,'KAFFEE  ·  KLEINER EINKAUF',-296,2.46,-14.16,7.7,.24,'#f1e3c1','#486b55');
 const bins=group(-320,-49);for(let n=0;n<3;n++){const x=-322+n*1.2,y=groundHeight(x,-46.6);westBox(bins,'wood',x,y+1,-46,1.05,2,.09);box(bins,x,y+.6,-46.6,.8,1.2,.8,0x526556);solid(x,-46.6,1,.95,y+2);}

 // Trees frame the inhabited blocks. No random trunks on routes or bottle spots.
 for(const [i,[x,z]] of [[-540,-45],[-510,-47],[-482,-58],[-437,59],[-411,46],[-383,54],[-351,77],[-306,-83],[-264,-63],[-267,79]].entries()){
  if(WEST_BUILDING_FIXTURES.some(b=>Math.abs(x-b.x)<b.w+2&&Math.abs(z-b.z)<b.d+2))continue;
  addWestTree(world,x,z,2.4+i%3*.18,{grate:false,seed:1280+i});
 }
 // Clusters of planted margins soften plot edges without blanketing pavements.
 for(let i=0;i<65;i++){const x=-541+(i*23.73%271),z=(i%2?1:-1)*(34+i*7.53%58);if(WEST_BUILDING_FIXTURES.some(b=>Math.abs(x-b.x)<b.w+2&&Math.abs(z-b.z)<b.d+2)||WEST_PATHS.some(p=>Math.abs(x-p.x)<p.w/2+1&&Math.abs(z-p.z)<p.d/2+1))continue;shrub(x,z,groundHeight(x,z),.7+i%3*.2);}
}
