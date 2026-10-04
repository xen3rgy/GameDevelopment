import * as THREE from './vendor/three.module.js';
import {EXPANDED_BOUNDS,EXPANSION_BUILDINGS,EXPANSION_FIXTURES,EXPANSION_PATHS,EXPANSION_LANDMARKS,EXPANSION_BINS,EXPANSION_ROADS} from './expansion-layout.js?v=0.8.1';
import {expansionBuilding} from './district-architecture.js?v=0.8.1';
import {groundHeight} from './spatial.js?v=0.8.1';
import {discoverNearby} from './exploration.js?v=0.8.1';

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
  for(const p of EXPANSION_PATHS)box(group(p.x,p.z),p.x,-.02,p.z,p.w,.08,p.d,['yard','rail','parking','construction'].includes(p.kind)?0x838477:0xb5b09c).castShadow=false;
  for(const l of EXPANSION_LANDMARKS){const g=group(l.x,l.z),x=l.x,z=l.z;
   sign(g,l.name,x,2.5,z-l.d/2,Math.min(8,l.w*.6),.48,'#e5dfca','#38534f');
   for(const side of [-1,1])cylinder(g,x+side*2.3,1.2,z-l.d/2,.045,2.4,0x4b5b55);
   if(l.kind==='court'){
    for(const side of [-1,1])world.colliders.push({x:x+side*17,z,w:.06,d:10,h:2.4});
    box(g,x,.03,z,32,.02,20,0x56766a);for(const side of [-1,1]){box(g,x+side*15.5,.05,z,.09,.01,19,0xe0d7b9);box(g,x,.05,z+side*9.5,31,.01,.09,0xe0d7b9);box(g,x+side*14,2,z,.08,4,.08,0x4c5655);box(g,x+side*14,3.5,z, .12,1.1,1.7,0xe2dfcb);}box(g,x,.05,z,.08,.01,19,0xe0d7b9);
    for(const side of [-1,1]){for(let dz=-9;dz<10;dz+=3)box(g,x+side*17,1.2,z+dz,.06,2.4,.06,0x62766d);for(const y of [.35,1.2,2.3])box(g,x+side*17,y,z,.035,.035,20,0x62766d);const hoop=new THREE.Mesh(new THREE.TorusGeometry(.24,.025,5,12),kit.mat(0xa76d49));hoop.rotation.x=Math.PI/2;hoop.position.set(x+side*13.5,3.05,z);g.add(hoop);}
   }else if(l.kind==='yard'||l.kind==='rail'){
    world.colliders.push({x,z:z+l.d/2,w:l.w/2,d:.06,h:2.3});
    for(let i=0;i<3;i++){const px=x-13+i*11;box(g,px,1.3,z+7,8,2.5,5,[0x766451,0x586e70,0x917454][i]);box(g,px,2.61,z+7,8.1,.12,5.1,0x58635f);world.colliders.push({x:px,z:z+7,w:4,d:2.5,h:2.7});}
    if(l.kind==='rail')for(const dz of [-4,-2.5]){box(g,x,.12,z+dz,l.w,.12,.1,0x555b57);for(let dx=-l.w/2;dx<l.w/2;dx+=1.5)box(g,x+dx,.05,z-3.25,.3,.08,2.7,0x6d6252);}
    for(let dx=-l.w/2;dx<l.w/2;dx+=4){box(g,x+dx,1.15,z+l.d/2,.08,2.3,.08,0x606d65);for(const y of [.45,1.25,2.15])box(g,x+dx+2,y,z+l.d/2,4,.04,.04,0x606d65);}
    for(let i=0;i<3;i++)for(let dx=-3.5;dx<4;dx+=.65)box(g,x-13+i*11+dx,1.3,z+9.53,.045,2.4,.025,0x9b9c86);
    for(let i=0;i<4;i++){box(g,x-17+i*10,.05,z-8,6,.02,.1,0xd0c39d);box(g,x-20+i*10,.05,z-4,.1,.02,8,0xd0c39d);}
   }else if(l.kind==='construction'){
    box(g,x,2,z,17,4,12,0x898c82);for(const dx of [-9,9])for(const dz of [-7,7])box(g,x+dx,4,z+dz,.35,8,.35,0xc4aa73);box(g,x,8,z,19,.3,15,0xa5a393);cylinder(g,x+16,9,z,.35,18,0xc49b4f);box(g,x+9,17,z,22,.3,.4,0xc49b4f);world.colliders.push({x,z,w:10,d:8,h:9});
   }else if(l.kind==='parking'){
    for(let y=0;y<10;y+=3.2){box(g,x,y+.3,z,28,.6,19,0x87958e);for(const dx of [-12,0,12])for(const dz of [-8,8])box(g,x+dx,y+1.7,z+dz,.6,3,.6,0xaab0a3);}world.colliders.push({x,z,w:14,d:9.5,h:10});
   }else{
    // Public art, planting and benches create recognisable, walkable squares.
    if(['plaza','campus','overlook'].includes(l.kind)){cylinder(g,x, .35,z,3.3,.65,0x828d81);const art=box(g,x,2.6,z,.8,4.4,.8,l.kind==='campus'?0x4e7881:0xb7a98d);art.rotation.z=.23;world.colliders.push({x,z,w:3.3,d:3.3,h:5});}
    for(const side of [-1,1]){const tx=x+side*(l.w/2-5),tz=z+5;world.tree(tx,tz,2.6,{grate:true,seed:Math.round(tx)});world.colliders.push({x:tx,z:tz,w:.23,d:.23,h:4});box(g,tx,.48,tz-4,3,.16,.6,0x89714e);box(g,tx,.88,tz-3.7,3,.75,.1,0x89714e);world.colliders.push({x:tx,z:tz-4,w:1.5,d:.4,h:1.3});}
    for(const side of [-1,1]){const px=x+side*l.w*.28,pz=z-l.d*.26;box(g,px,.27,pz,5,.5,3,0x9c9f8d);box(g,px,.54,pz,4.6,.05,2.6,0x59654c);for(let i=0;i<5;i++){const shrub=sphere(g,px-1.7+i*.85,.81,pz,.58,0x798c5b);shrub.scale.y*=.6;}world.colliders.push({x:px,z:pz,w:2.5,d:1.5,h:1.1});}
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
