import * as THREE from './vendor/three.module.js';
import {PROMENADE_FIXTURES,gardenPatches} from './pedestrian-layout.js?v=0.8.0';
import {groundHeight} from './spatial.js?v=0.8.0';
import {surfaceMaterial,surfaceTileMeters} from './atmosphere.js?v=0.8.0';
import {CAFE_OPEN,CAFE_CLOSE,clockLabel} from './game-time.js?v=0.8.0';

export function buildPromenade(world,kit){
 const {box,cylinder,sign}=kit,g=new THREE.Group();g.name='Cafe to station · public realm';world.scene.add(g);world.staticGroups.push(g);
 world.colliders.push(...PROMENADE_FIXTURES.map(p=>({...p})));
 const vertices=[],uv=[];
 const quad=(a,b,c,d)=>vertices.push(...a,...b,...c,...a,...c,...d);
 for(const p of gardenPatches()){
  const {minX:x,maxX:X,minZ:z,maxZ:Z}=p;
  quad([x,.02,z],[x,.02,Z],[X,.02,Z],[X,.02,z]);
  for(const [a,b] of [[[x,.02,z],[x,.02,Z]],[[x,.02,Z],[X,.02,Z]],[[X,.02,Z],[X,.02,z]],[[X,.02,z],[x,.02,z]]])quad(a,[a[0],-.05,a[2]],[b[0],-.05,b[2]],b);
 }
 for(let i=0;i<vertices.length;i+=3)uv.push(vertices[i]/surfaceTileMeters('cobble'),vertices[i+2]/surfaceTileMeters('cobble'));
 const geo=new THREE.BufferGeometry();geo.setAttribute('position',new THREE.Float32BufferAttribute(vertices,3));geo.setAttribute('uv',new THREE.Float32BufferAttribute(uv,2));geo.computeVertexNormals();
 const paving=surfaceMaterial('cobble',6,6);paving.map.repeat.set(1,1);paving.color.set(0xb4b5a4);
 const paths=new THREE.Mesh(geo,paving);paths.name='Connected garden paths';paths.receiveShadow=true;g.add(paths);
 // Doors belong to the facade builder; resident portals only define arrival behavior.
 // Window displays stay inside the facade collision envelope, not in the walking aisle.
 for(const [x,width] of [[59,2.2],[74,2.8]]){
  const root=new THREE.Group();root.position.set(x,0,-13.48);g.add(root);
  box(root,0,1.45,.025,width,1.4,.065,0x263f43);box(root,0,.82,.16,width,.09,.30,0x9b825f);
  for(let i=0;i<4;i++){
   const px=-width*.34+i*width*.22;
   if(i%2){cylinder(root,px,.97,.18,.09,.21,0xd8d1bb);cylinder(root,px,1.083,.18,.073,.012,0x61452c);}
   else{box(root,px,1.04,.16,.25,.36,.18,0x967952);box(root,px,1.075,.258,.16,.13,.012,0xe0d6b7);}
  }
  sign(root,'KAFFEE · FRISCH GERÖSTET',0,1.99,.08,width-.15,.18,'#e9d7b3','#263f43');
 }
 sign(g,clockLabel(CAFE_OPEN)+'–'+clockLabel(CAFE_CLOSE),64.6,2.02,-13.28,.75,.19,'#f0dec0','#354c47');
 // Historic frontage: narrow maintenance details and flower boxes, not another billboard.
 for(const [x,z] of [[-70,-13.70],[-175,-32.78],[-126,-20.28]]){
  for(const dx of [-3.7,3.7]){cylinder(g,x+dx,2.35,z,.038,4.7,0x58675f);box(g,x+dx,4.58,z,.17,.24,.14,0x788479);}
  for(const dx of [-5.2,5.2]){box(g,x+dx,.76,z+.16,1.8,.28,.30,0x866547);for(let n=0;n<8;n++){const xx=x+dx-.72+n*.205;kit.sphere(g,xx,.96,z+.16,.14,0x53704d);if(n%2)kit.sphere(g,xx,1.1,z+.20,.058,0xb79b7b);}}
 }
 return g;
}
