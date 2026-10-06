import * as THREE from './vendor/three.module.js';
import {EXPANDED_BOUNDS,EXPANSION_BUILDINGS,EXPANSION_FIXTURES,EXPANSION_PATHS,EXPANSION_LANDMARKS,EXPANSION_BINS,EXPANSION_ROADS} from './expansion-layout.js?v=0.8.1';
import {expansionBuilding} from './district-architecture.js?v=0.8.1';
import {groundHeight} from './spatial.js?v=0.8.1';
import {discoverNearby} from './exploration.js?v=0.8.1';
import {surfaceMaterial} from './atmosphere.js?v=0.8.1';
const signalRoof=new THREE.CylinderGeometry(0,1,1,4);signalRoof.rotateY(Math.PI/4);

export class ExpansionScene{
 constructor(world,kit){
  this.world=world;this.age=0;this.blocks=new Map();this.root=new THREE.Group();this.root.name='Lindenstadt · outer districts';world.exterior.add(this.root);
  const {box,cylinder,sphere,sign}=kit,b=EXPANDED_BOUNDS;
  const horizon=new THREE.Group();horizon.name='Outer landscape';this.root.add(horizon);
  box(horizon,(b.minX+b.maxX)/2,-.365,0,b.maxX-b.minX+220,.6,b.maxZ-b.minZ+200,0x68775a).castShadow=false;
  for(const side of [-1,1])for(let x=b.minX-45;x<b.maxX+60;x+=19){const z=side*(b.maxZ+20+(Math.abs(x)%3)*9);world.tree(x,z,2.4,{grate:false,distant:true,seed:Math.round(x),ground:-.065});}
  world.batchStaticGroup(horizon);
  // Four disjoint ground aprons preserve every original surface elevation.
  for(const [x,X,z,Z] of [[b.minX,b.maxX,b.minZ,-125],[b.minX,b.maxX,125,b.maxZ],[125,b.maxX,-125,125],[b.minX,-225,-125,125],[-225,-125,-125,-80],[-225,-125,80,125]])box(this.root,(x+X)/2,-.35,(z+Z)/2,X-x,.6,Z-z,0x69745a).castShadow=false;
  const group=(x,z)=>{const key=Math.floor(x/90)+':'+Math.floor(z/90);if(!this.blocks.has(key)){const g=new THREE.Group();g.name='Exterior block '+key;g.userData.center={x:Math.floor(x/90)*90+45,z:Math.floor(z/90)*90+45};this.root.add(g);this.blocks.set(key,g);}return this.blocks.get(key);};
  world.colliders.push(...EXPANSION_FIXTURES);
  for(const side of [-1,1]){
   const g=group(-145,side*154.5);box(g,-145,6.2,side*154.5,12,.8,151,0x7b8176);world.overheadColliders.push({x:-145,z:side*154.5,w:6,d:75.5,minY:5.8,h:6.6});
   for(const x of [-147.1,-146,-144,-142.9])box(g,x,6.8,side*154.5,.12,.12,151,0x596664);
   for(let z=92;z<222;z+=23){if(Math.abs(z-175)<12)continue;for(const x of [-149,-141]){box(g,x,2.9,side*z,1.2,5.8,1.8,0x83887c);world.colliders.push({x,z:side*z,w:.6,d:.9,h:5.8});}}
  }
  for(const building of EXPANSION_BUILDINGS)expansionBuilding(group(building.x,building.z),kit,building,world.art.windows);
  for(const p of EXPANSION_PATHS){
   const g=group(p.x,p.z),industrial=['yard','rail','parking','construction'].includes(p.kind);
   const paving=typeof document!=='undefined'?surfaceMaterial(industrial?'asphalt':p.kind==='garden'?'cobble':'paving',p.w,p.d):undefined;
   box(g,p.x,-.02,p.z,p.w,.08,p.d,industrial?0x838477:p.kind==='plaza'?0xb8bbb3:p.kind==='campus'?0xb8bbaa:0xb5b09c,paving).castShadow=false;
   // Ground-level courses define designed places without adding path obstacles.
   if(!industrial)for(const side of [-1,1]){box(g,p.x+side*(p.w/2-1),.026,p.z,.22,.012,p.d-1,0x8c968b).castShadow=false;box(g,p.x,.026,p.z+side*(p.d/2-1),p.w-1,.012,.22,0x8c968b).castShadow=false;}
   if(p.kind==='plaza'||p.kind==='campus')for(let dx=-p.w/2+3;dx<p.w/2;dx+=4)box(g,p.x+dx,.025,p.z,.035,.012,p.d-2,0x90998f).castShadow=false;
   // Walkable lawn inlays leave a generous cross-shaped route and perimeter path.
   if(['garden','overlook','campus'].includes(p.kind)){
    const width=p.w*.33,depth=p.d*.26,grass=typeof document!=='undefined'?surfaceMaterial('grass',width,depth):undefined;
    for(const sx of [-1,1])for(const sz of [-1,1])box(g,p.x+sx*p.w*.27,.029,p.z+sz*p.d*.28,width,.008,depth,0x7f8f6c,grass).castShadow=false;
   }
  }
  for(const l of EXPANSION_LANDMARKS){const g=group(l.x,l.z),x=l.x,z=l.z;
   // Identification belongs on built objects, never on identical floating gates.
   if(l.kind==='court'){const label=new THREE.Group();label.position.set(x-17,0,z);label.rotation.y=Math.PI/2;g.add(label);box(label,0,1.45,0,4,.7,.08,0x425951);sign(label,'NORDRING · SPORTHOF',0,1.45,.046,3.7,.32,'#e5dfca','#425951');}
   else if(['yard','rail'].includes(l.kind))sign(g,l.name,x-2,2.43,z+9.54,6,.22,'#e5dfca','#48534f');
   else if(l.kind==='parking')sign(g,'P · OSTRING',x,1.55,z+9.54,6,.48,'#e5dfca','#38534f');
   else if(l.kind==='construction')sign(g,'BAUFELD OST',x,2.5,z+6.01,5,.35,'#e5dfca','#555d56');
   else {const face=l.kind==='overlook'||l.id==='uferGarden'?-1:1;sign(g,l.name,x-l.w*.28,.3,z-l.d*.26+face*1.51,4,.22,'#e5dfca','#58685b',face<0?Math.PI:0);}
   if(l.kind==='court'){
    for(const side of [-1,1])world.colliders.push({x:x+side*17,z,w:.06,d:10,h:2.4});
    box(g,x,.03,z,32,.02,20,0x56766a);for(const side of [-1,1]){box(g,x+side*15.5,.05,z,.09,.01,19,0xe0d7b9);box(g,x,.05,z+side*9.5,31,.01,.09,0xe0d7b9);box(g,x+side*14,2,z,.08,4,.08,0x4c5655);box(g,x+side*14,3.5,z, .12,1.1,1.7,0xe2dfcb);}box(g,x,.05,z,.08,.01,19,0xe0d7b9);
    for(const side of [-1,1]){for(let dz=-9;dz<10;dz+=3)box(g,x+side*17,1.2,z+dz,.06,2.4,.06,0x62766d);for(const y of [.35,1.2,2.3])box(g,x+side*17,y,z,.035,.035,20,0x62766d);const hoop=new THREE.Mesh(new THREE.TorusGeometry(.24,.025,5,12),kit.mat(0xa76d49));hoop.rotation.x=Math.PI/2;hoop.position.set(x+side*13.5,3.05,z);g.add(hoop);}
   }else if(l.kind==='yard'||l.kind==='rail'){
    world.colliders.push({x,z:z+l.d/2,w:l.w/2,d:.06,h:2.3});
    for(let i=0;i<3;i++){const px=x-13+i*11,stellwerk=l.kind==='rail'&&i===1;
     box(g,px,stellwerk?1.15:1.3,z+7,8,stellwerk?2.2:2.5,5,stellwerk?0xa58d72:[0x766451,0x586e70,0x917454][i]);
     if(stellwerk){const roof=new THREE.Mesh(signalRoof,kit.mat(0x485853));roof.position.set(px,2.46,z+7);roof.scale.set(8/Math.SQRT2,.42,5/Math.SQRT2);g.add(roof);}
     else box(g,px,2.61,z+7,8,.12,5,0x58635f);
     if(stellwerk){box(g,px,1.55,z+9.51,6.5,1.4,.025,0x354e50);for(let n=-3;n<=3;n++)box(g,px+n,1.55,z+9.55,.085,1.5,.045,0xc9baa0);box(g,px,2.37,z+9.57,7.9,.14,.15,0x56675f);}
     else for(const sx of [-1,1]){box(g,px+sx*1.85,1.3,z+9.53,3.6,2.3,.04,0x68766b);box(g,px+sx*1.85,1.3,z+9.57,.07,2.2,.045,0xbbb6a1);}
     world.colliders.push({x:px,z:z+7,w:4,d:2.5,h:2.7});}
    if(l.kind==='rail')for(const dz of [-4,-2.5]){box(g,x,.12,z+dz,l.w,.12,.1,0x555b57);for(let dx=-l.w/2;dx<l.w/2;dx+=1.5)box(g,x+dx,.05,z-3.25,.3,.08,2.7,0x6d6252);}
    for(let dx=-l.w/2;dx<l.w/2;dx+=4){box(g,x+dx,1.15,z+l.d/2,.08,2.3,.08,0x606d65);for(const y of [.45,1.25,2.15])box(g,x+dx+2,y,z+l.d/2,4,.04,.04,0x606d65);}
    for(let i=0;i<3;i++){
     const px=x-13+i*11,office=l.kind==='rail'&&i===1;
     box(g,px,office?1.55:1.35,z+4.48,office?6.6:7.4,office?1.1:2.25,.035,office?0x354e50:0x68766b);
     for(let dx=-3;dx<=3;dx+=office?1:.75)box(g,px+dx,office?1.55:1.3,z+4.445,.05,office?1.15:2.15,.025,office?0xc9baa0:0x9b9c86);
     if(!office)for(const dx of [-1.8,1.8])box(g,px+dx,1.35,z+4.42,.055,1.95,.035,0xc5bda4);
     if(i===1){sign(g,l.name,px,2.18,z+4.415,6,.22,'#e5dfca','#48534f',Math.PI);world.atmosphere.registerLamp(px,z+4.35,null,{height:2.4,power:45,poolSize:5,distance:12});}
    }
    for(let i=0;i<4;i++){box(g,x-17+i*10,.05,z-8,6,.02,.1,0xd0c39d);box(g,x-20+i*10,.05,z-4,.1,.02,8,0xd0c39d);}
    if(l.kind==='yard'){for(let dx=-10;dx<=10;dx+=2){const stripe=box(g,x+dx,.052,z+3.8,1,.015,.2,0xc9b075);stripe.rotation.y=-.6;}sign(g,'VERLADEHOF · TOR 02',x-13,2.07,z+9.59,6,.28,'#e6dab6','#48534f');}
   }else if(l.kind==='construction'){
    box(g,x,2,z,17,4,12,0x898c82);for(const dx of [-9,9])for(const dz of [-7,7])box(g,x+dx,4,z+dz,.35,8,.35,0xc4aa73);box(g,x,8,z,19,.3,15,0xa5a393);cylinder(g,x+16,9,z,.35,18,0xc49b4f);box(g,x+9,17,z,22,.3,.4,0xc49b4f);world.colliders.push({x,z,w:10,d:8,h:9});
   }else if(l.kind==='parking'){
    box(g,x,1.55,z+9.43,6.4,.7,.12,0x38534f);for(const dx of [-2.8,2.8])box(g,x+dx,2.55,z+9.43,.055,1.5,.055,0x647b77);
    for(let y=0;y<10;y+=3.2){box(g,x,y+.3,z,28,.6,19,0x87958e);for(const dx of [-12,0,12])for(const dz of [-8,8])box(g,x+dx,y+1.7,z+dz,.6,3,.6,0xaab0a3);if(y>0)box(g,x,y+.92,z+9.35,27.8,.66,.12,0x687b77);}
    for(let dx=-13;dx<14;dx+=1.15)box(g,x+dx,5,z-9.36,.12,9.7,.2,0x455e63);
    box(g,x+10,5,z+8,4,10,2.8,0x576d70);for(let y=1;y<10;y+=3.2)box(g,x+10,y,z+9.42,2.5,1.7,.06,0x92aaa6);
    world.colliders.push({x,z,w:14,d:9.5,h:10});
   }else{
    // Public art, planting and benches create recognisable, walkable squares.
    if(['plaza','campus','overlook'].includes(l.kind)){
     if(l.kind==='plaza'){
      box(g,x,.24,z,5.6,.48,5.6,0x828d81);
      for(const side of [-1,1])box(g,x+side*1.7,2.2,z,.6,3.8,1.1,0x9b9d8e);box(g,x,4.1,z,4,.65,1.1,0x9b9d8e);
      box(g,x,.7,z+1.7,4.7,.4,.65,0x667973);
     }else if(l.kind==='campus'){
      cylinder(g,x,.23,z,3.25,.42,0x929e90);
      for(const side of [-1,1]){const leaf=box(g,x+side*.9,2.3,z,1.3,3.9,.55,side<0?0x698c89:0xb5b79d);leaf.rotation.z=side*.24;}
      box(g,x,3.8,z,.4,.3,3,0x526e70);
     }else{
      cylinder(g,x,.17,z,3.3,.32,0xa6a995);cylinder(g,x,.42,z,2.6,.2,0xbabbab);cylinder(g,x,.65,z,1.9,.25,0x919c88);
      box(g,x,.95,z,2.9,.16,1.2,0x7b6d55);box(g,x,.83,z,1.8,.15,.7,0x556759);
     }
     world.colliders.push({x,z,w:3.3,d:3.3,h:5});
    }
    for(const side of [-1,1]){const tx=x+side*(l.w/2-5),tz=z+5;world.tree(tx,tz,2.6,{grate:true,seed:Math.round(tx)});world.colliders.push({x:tx,z:tz,w:.23,d:.23,h:4});box(g,tx,.48,tz-4,3,.16,.6,0x89714e);box(g,tx,.88,tz-3.7,3,.75,.1,0x89714e);world.colliders.push({x:tx,z:tz-4,w:1.5,d:.4,h:1.3});}
    for(const side of [-1,1]){const px=x+side*l.w*.28,pz=z-l.d*.26,quiet=l.kind==='overlook'||l.id==='uferGarden';
     for(const edge of [-1,1]){box(g,px+edge*2.42,.27,pz,.16,.5,3,quiet?0xb2b39f:0x9c9f8d);box(g,px,.27,pz+edge*1.42,4.68,.5,.16,quiet?0xb2b39f:0x9c9f8d);}
     box(g,px,.42,pz,4.68,.08,2.68,0x59654c);for(let i=0;i<7;i++){const shrub=sphere(g,px-1.9+i*.63,.69,pz+Math.sin(i*2)*.5,.4,quiet?0x6e825b:l.kind==='campus'?0x758b71:0x798c5b);shrub.scale.y*=.65;}
     const lamp=new THREE.MeshBasicMaterial({color:0x998d74});box(g,px,.35,pz+1.505,1.2,.035,.02,0,lamp);world.atmosphere.registerLamp(px,pz+1.53,lamp,{height:.4,power:quiet?9:15,poolSize:3,distance:8});
     if(l.id==='nordGarden'){box(g,px,.86,pz+1.1,4.6,.14,.5,0x8a7354);for(let i=-2;i<=2;i++)box(g,px+i,.7,pz+1.08,.05,.3,.45,0x65756b);}
     if(quiet)for(let i=-2;i<=2;i++)box(g,px+i,.63,pz-.65,.45,.13,.45,i%2?0x9f9478:0xc1b697);
     world.colliders.push({x:px,z:pz,w:2.5,d:1.5,h:1.1});}
    if(l.kind==='campus')for(const side of [-1,1]){box(g,x+side*7,2.7,z+l.d/2-2,.06,5.4,.06,0x7d8d82);box(g,x+side*7+.7,4.7,z+l.d/2-2,1.4,1.9,.035,side<0?0x577d81:0xabc09d);}
   }
  }
  for(const p of EXPANSION_BINS){const g=group(p.x,p.z);cylinder(g,p.x,.48,p.z-.95,.3,.96,0x405c52);world.colliders.push({x:p.x,z:p.z-.95,w:.3,d:.3,h:1});}
  for(const r of EXPANSION_ROADS){const horizontal=r.w>r.d,length=Math.max(r.w,r.d);for(let n=-length/2+18;n<length/2-9;n+=30){const x=r.x+(horizontal?n:10),z=r.z+(horizontal?10:n);
   if(EXPANSION_ROADS.some(q=>q!==r&&Math.abs(x-q.x)<q.w/2+2&&Math.abs(z-q.z)<q.d/2+2))continue;
   const g=group(x,z),y=groundHeight(x,z);cylinder(g,x,y+2.5,z,.055,5,0x455551);box(g,x,y+5,z,.7,.15,.35,0xc9c6a4);world.atmosphere.registerLamp(x,z,null,{height:5,power:65,poolSize:6,distance:17});world.colliders.push({x,z,w:.08,d:.08,h:5.5});
   if(n%60<1){const tx=x+3,tz=z+3;if(!EXPANSION_FIXTURES.some(b=>Math.abs(tx-b.x)<b.w+2&&Math.abs(tz-b.z)<b.d+2)){world.tree(tx,tz,2.1,{grate:groundHeight(tx,tz)>.1,seed:Math.round(tx+tz)});world.colliders.push({x:tx,z:tz,w:.18,d:.18,h:4});}}
  }}
  for(const g of this.blocks.values())world.batchStaticGroup(g);

 }
 update(s,dt,p){this.age+=dt;if(this.age<.25)return;this.age=0;for(const g of this.blocks.values()){const c=g.userData.center;g.visible=Math.hypot(c.x-p.x,c.z-p.z)<235;}discoverNearby(this.world.model);}
}
