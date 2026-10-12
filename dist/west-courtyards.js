import {westFenceMesh} from './west-details.js?v=0.8.1';
import * as THREE from './vendor/three.module.js';
import {WEST_BUILDINGS} from './west-edge-layout.js?v=0.8.1';
import {groundHeight} from './spatial.js?v=0.8.1';
import {westBox,westGround,westWallArt} from './west-materials.js?v=0.8.1';
import {westShrub,addWestTree,westWildGrass} from './west-vegetation.js?v=0.8.1';

// Deliberately composed yards, with clear entrances and generous paths between
// play equipment. Everything is built once and joins the existing block batches.
export function buildWestCourtyards(world,kit,{group,solid,line,bench}){
 const {box,cylinder,sign}=kit,play=group(-507,48),y=.14;
 westGround(play,'yard',-509,49,17,16,y+.008);
 // Old tubular double swing, with separate hangers and seats.
 for(const x of [-515.5,-510.5])for(const z of [44.8,47.6])line(play,{x,y:y+.04,z},{x,y:y+3.0,z:46.2},.085,0x77564a);
 box(play,-513,y+3,46.2,5.4,.12,.12,0x795746);
 for(const x of [-514.1,-512]){
  for(const dx of [-.31,.31])line(play,{x:x+dx,y:y+2.96,z:46.2},{x:x+dx,y:y+.65,z:46.2},.024,0x8e9389);
  westBox(play,'wood',x,y+.62,46.2,.82,.09,.34);
 }
 solid(-513,46.2,5.6,3.1,y+3.15);
 // Slide: a raised platform, guarded ladder and continuous sloping metal chute.
 for(const x of [-504.7,-503.3])for(const z of [44.2,45.6])westBox(play,'wood',x,y+1.04,z,.13,2.08,.13);
 westBox(play,'wood',-504,y+1.75,44.9,1.7,.14,1.7);
 for(const x of [-504.77,-503.23]){line(play,{x,y:y+1.85,z:44.2},{x,y:y+2.55,z:44.2},.06,0x867854);box(play,x,y+2.54,44.9,.06,.06,1.5,0x867854);}
 for(let n=0;n<6;n++)box(play,-504,y+.3+n*.28,44.02,.95,.075,.10,0x8e9180);
 for(const dx of [-.54,.54])line(play,{x:-504+dx,y:y+.05,z:43.65},{x:-504+dx,y:y+1.82,z:44.2},.065,0x646a5e);
 const chuteAngle=Math.asin((1.82-.008)/3.7),chuteY=y+(1.82+.008)/2-.035*Math.cos(chuteAngle),chute=box(play,-504,chuteY,47.0,1.08,.07,3.7,0x92978d);chute.rotation.x=chuteAngle;chute.userData.westSlide=true;
 for(const dx of [-.56,.56]){const side=box(play,-504+dx,chuteY+.075,47.0,.075,.22,3.7,0x785b46);side.rotation.x=chuteAngle;}
 solid(-504,46.2,1.8,5.7,y+2.6);
 // Sandpit and a weathered spring rider: distinct low silhouettes in the rear.
 westGround(play,'soil',-512,54.5,4.2,3.4,y+.016);
 for(const side of [-1,1]){westBox(play,'wood',-512,y+.13,54.5+side*1.75,4.6,.26,.19);westBox(play,'wood',-512+side*2.2,y+.13,54.5,.19,.26,3.6);}
 solid(-512,54.5,4.7,3.8,y+.3);
 const rider=group(-501,54);cylinder(rider,-501,y+.27,54,.11,.48,0x535d59);box(rider,-501,y+.62,54,.36,.13,1.05,0x987448);box(rider,-501,y+.85,54.45,.28,.52,.26,0x987448);box(rider,-501,y+.94,54.48,.75,.045,.045,0x7f8980);solid(-501,54,1,1.3,y+1.25);
 // Low perimeter railing, open along the approach from the sleeping shelter.
 for(const [x,a,b] of [[-519,39,59],[-495,39,47],[-495,51,59]]){for(let z=a;z<=b;z+=2)box(play,x,y+.55,z,.06,1.1,.06,0x5b6255);for(const yy of [.25,.95])box(play,x,y+yy,(a+b)/2,.055,.045,b-a,0x5b6255);solid(x,(a+b)/2,.10,b-a,y+1.15);westFenceMesh(play,x,y+.57,(a+b)/2,b-a,.94,Math.PI/2);}
 for(let x=-519;x<=-495;x+=2)if(Math.abs(x+514)>2)box(play,x,y+.55,59,.06,1.1,.06,0x5b6255);
 for(const [x,w] of [[-517.5,3],[-503.5,17]]){box(play,x,y+1.05,59,w,.06,.06,0x5b6255);solid(x,59,w,.1,y+1.15);westFenceMesh(play,x,y+.57,59,w,.94);}
 bench(-518,53,Math.PI/2);bench(-497,42,-Math.PI/2);
 sign(play,'HOF BRUCHFELD · SPIELPLATZ',-498,1.7,38.1,3.4,.38,'#d8cfb4','#4e5e50');box(play,-498,.8,38.15,.09,1.7,.09,0x737666);solid(-498,38.15,.15,.15,1.95);
 // A narrow paved approach and lit noticeboard make the yard discoverable.
 world.atmosphere.registerLamp(-499,39,null,{height:3.5,power:45,poolSize:6,distance:10});
 cylinder(play,-499,1.8,39,.06,3.6,0x505a52);box(play,-499,3.6,39,.52,.13,.32,0xccc3a1);solid(-499,39,.16,.16,3.8);
 for(const [i,[x,z]] of [[-521,61],[-499,62],[-519,35],[-490,59]].entries())westShrub(group(x,z),x,groundHeight(x,z),z,.85+i*.12,70+i);
 addWestTree(world,-521,39,2.2,{grate:false,seed:1723});addWestTree(world,-488,60,2.2,{grate:false,seed:1727});

 // Laundry court between the two blocks: rails, fabric, utility cabinets, bins.
 const laundry=group(-534,36),binBase=groundHeight(-539,41.35);
 for(const x of [-539,-530]){cylinder(laundry,x,1.45,36,.045,2.6,0x6d7266);solid(x,36,.12,.12,2.9);}
 line(laundry,{x:-539,y:2.7,z:36},{x:-530,y:2.7,z:36},.017,0x8c9184);
 for(let i=0;i<5;i++){const cloth=box(laundry,-537.7+i*1.48,2.15,36,.85,1.02,.018,[0xa7a18c,0x687b78,0xafa995,0x8a8579,0x6b7278][i]);cloth.rotation.z=(i%2-.5)*.04;}
 // A small covered bin bay and stacked return crates sit against the block,
 // leaving the central entrance free for residents.
 westBox(laundry,'wood',-539,binBase+1.175,41.8,3.2,2.35,.10);solid(-539,41.8,3.2,.10,binBase+2.35);
 westBox(laundry,'steel',-539,binBase+2.43,41.2,3.5,.12,1.5);
 world.overheadColliders.push({x:-539,z:41.2,w:1.75,d:.75,minY:binBase+2.37,h:binBase+2.49});
 for(const x of [-540.5,-537.5]){westBox(laundry,'steel',x,binBase+1.2,40.6,.065,2.4,.065);solid(x,40.6,.12,.12,binBase+2.5);}
 for(const x of [-539.7,-538.35]){box(laundry,x,binBase+.6,41.35,1.05,1.2,.8,0x48594d);box(laundry,x,binBase+1.26,41.35,1.12,.12,.87,0x72796a);solid(x,41.35,1.15,.9,binBase+1.32);}
 for(const [x,z] of [[-504.5,-40],[-527,40.8],[-491,34]]){const g=group(x,z),base=groundHeight(x,z);for(let n=0;n<3;n++){westBox(g,'wood',x+(n===2?.6:0),base+.155+n*.31,z,.85,.31,.6);box(g,x+(n===2?.6:0),base+.155+n*.31,z+.305,.5,.075,.012,0x343d32);}solid(x+.25,z,1.55,.7,base+.93);}
 for(const [x,z] of [[-542,33],[-526,39],[-486,-47],[-304,-71]]){
  const g=group(x,z),base=groundHeight(x,z);westBox(g,'steel',x,base+.65,z,1.55,1.3,.9);box(g,x,base+1.33,z,1.65,.12,1,0x4e584b);solid(x,z,1.7,1.05,base+1.45);
  for(const dx of [-.58,.58])cylinder(g,x+dx,base+.14,z+.3,.12,.1,0x313936);
  westWallArt(g,'posters',x,base+.74,z+.465,.8,.65);
 }
 // Building entrances receive cellar vents, noticeboards, patched walls and
 // meter boxes. These fit inside the existing facade collision margin.
 for(const b of WEST_BUILDINGS.filter(b=>b.style==='residential')){
  const f=new THREE.Group();f.position.set(b.x,0,b.z+b.face*b.d/2);f.rotation.y=b.face<0?Math.PI:0;group(b.x,b.z).add(f);
  for(const dx of [-b.w*.32,b.w*.32]){box(f,dx,.41,.21,1.3,.42,.1,0x333d39);for(let n=-2;n<=2;n++)box(f,dx+n*.22,.41,.27,.025,.39,.025,0x797f6e);}
  westBox(f,'steel',2.1,1.24,.22,.52,.75,.12);box(f,2.1,1.24,.29,.37,.54,.025,0x797f70);
  if(b.seed%2===0)westWallArt(f,'home',-b.w*.31,1.45,.031,Math.min(3.7,b.w*.26),1.35);
  westWallArt(f,'posters',-1.7,1.6,.22,.72,.95);
  if([18,33,36].includes(b.seed)){box(f,0,2.92,.47,.52,.1,.30,0xd4c5a4);world.atmosphere.registerLamp(b.x,b.z+b.face*(b.d/2+.8),null,{height:2.85,power:28,poolSize:4,distance:7});}
  // A few boarded ground-floor panes, instead of every home looking renovated.
  if(b.seed%3===1)for(let n=0;n<3;n++){const plank=westBox(f,'wood',b.w*.31,1.2+n*.27,.29,1.42,.18,.05);plank.rotation.z=.025*(n-1);}
 }
 // Modest allotments occupy the gaps beside the housing; deliberate entrances
 // join existing lanes. No props spill into the pavement or driving corridor.
 for(const [i,p] of [{x:-469,z:35,w:8,d:11},{x:-502,z:-47,w:13,d:4},{x:-433.5,z:55,w:12,d:9}].entries()){
  const g=group(p.x,p.z),base=groundHeight(p.x,p.z);
  for(const side of [-1,1]){const x=p.x+side*p.w/2;westFenceMesh(g,x,base+.53,p.z,p.d,.95,Math.PI/2);for(let n=0;n<=Math.ceil(p.d/2.5);n++)westBox(g,'wood',x,base+.53,p.z-p.d/2+n*p.d/Math.ceil(p.d/2.5),.075,1.06,.075);solid(x,p.z,.12,p.d,base+1.1);}
  westFenceMesh(g,p.x,base+.53,p.z+p.d/2,p.w,.95);solid(p.x,p.z+p.d/2,p.w,.12,base+1.1);
  for(const side of [-1,1]){const span=(p.w-2)/2,x=p.x+side*(1+span/2);westFenceMesh(g,x,base+.53,p.z-p.d/2,span,.95);solid(x,p.z-p.d/2,span,.12,base+1.1);}
  const length=Math.max(1.2,p.d-2.4);for(const side of [-1,1]){const x=p.x+side*p.w*.26;westGround(g,'soil',x,p.z,Math.max(1,p.w*.31),length,base+.01);for(const dz of [-length/2,length/2])westBox(g,'wood',x,base+.075,p.z+dz,Math.max(1,p.w*.31),.13,.10);for(let n=0;n<Math.ceil(length*1.4);n++){const z=p.z-length/2+.4+n*.65;westWildGrass(g,x+Math.sin(n*2+i)*.37,base+.02,z,.58+n%3*.1,n+i*7);}}
  for(const [n,side] of [-1,1].entries())westShrub(g,p.x+side*(p.w/2-.7),base,p.z+p.d/2-.9,.9,n+i*3);
 }
 // Rooted, taller weed clusters at specific margins replace the dotted field.
 for(const [i,[x,z,dx,dz,count]] of [[-478.1,-24,0,1,11],[-453.9,-26,0,1,14],[-535.3,-38,0,1,23],[-412.8,-38,0,1,11],[-408.6,-24,1,0,12],[-354.8,-44,0,1,10],[-305.5,45,1,0,10],[-270.4,18,0,1,15],[-519.4,45,0,1,14]].entries()){
  for(let n=0;n<count;n++){const px=x+dx*n*.62+Math.sin(n*4+i)*.17,pz=z+dz*n*.62+Math.cos(n*3+i)*.19;if(WEST_BUILDINGS.some(b=>Math.abs(px-b.x)<b.w/2+.15&&Math.abs(pz-b.z)<b.d/2+.15)||world.colliders.some(c=>c.w>.3&&c.d>.3&&Math.abs(px-c.x)<c.w+.15&&Math.abs(pz-c.z)<c.d+.15))continue;westWildGrass(group(px,pz),px,groundHeight(px,pz)+.007,pz,.56+(n%4)*.13,n+i*13);}
 }
}
