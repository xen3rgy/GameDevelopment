import * as THREE from './vendor/three.module.js';
import {DISTRICT_FIXTURES,STATION_PLAZAS,STATION_YARD,ROAD_Z,VIADUCT} from './city-layout.js?v=0.7.2-stationedge1';
import {surfaceMaterial} from './atmosphere.js?v=0.7.2';
import {groundHeight} from './spatial.js?v=0.7.2-stationedge2';

export function buildStationDistrict(world,kit){
 const {box,cylinder,sign,sphere}=kit,s=world.scene,g=new THREE.Group();s.add(g);world.staticGroups.push(g);
 // The western quarter used to expose a single flat grey box here. Give the entire
 // station yard a continuous, weathered cobblestone surface so gaps between plazas,
 // buildings and the viaduct still read as finished public ground.
 const yardW=STATION_YARD.maxX-STATION_YARD.minX,yardD=STATION_YARD.maxZ-STATION_YARD.minZ,yardX=(STATION_YARD.minX+STATION_YARD.maxX)/2,yardZ=(STATION_YARD.minZ+STATION_YARD.maxZ)/2,yardThickness=.30;
 const yardMaterial=surfaceMaterial('cobble',yardW,yardD);yardMaterial.color.set(0xa89a82);yardMaterial.roughness=.98;yardMaterial.bumpScale=.034;
 const yard=box(g,yardX,STATION_YARD.height-yardThickness/2,yardZ,yardW,yardThickness,yardD,0,yardMaterial);yard.name='Bahnhofsviertel · Pflastergrund';yard.castShadow=false;yard.receiveShadow=true;
 // A narrow granite/drainage band makes the transition to the eastern carriageway intentional
 // instead of leaving the green base plane visible. It stops at each crossing/road opening.
 const edgeMaterial=surfaceMaterial('paving',.38,yardD);edgeMaterial.color.set(0x77756d);edgeMaterial.roughness=.99;edgeMaterial.bumpScale=.018;
 const roadClear=7.25,edgeX=STATION_YARD.maxX-7.19;let edgeFrom=STATION_YARD.minZ;
 const edgeSegment=(a,b)=>{if(b-a<.25)return;const m=box(g,edgeX,STATION_YARD.height+.010,(a+b)/2,.38,.020,b-a,0,edgeMaterial);m.name='Bahnhofsviertel · Granitrand';m.castShadow=false;m.receiveShadow=true;};
 for(const roadZ of ROAD_Z){edgeSegment(edgeFrom,roadZ-roadClear);edgeFrom=roadZ+roadClear;}edgeSegment(edgeFrom,STATION_YARD.maxZ);
 for(const p of STATION_PLAZAS){
  const material=surfaceMaterial('cobble',p.w,p.d);material.color.set(0xc1b198);material.polygonOffset=true;material.polygonOffsetFactor=-1;material.polygonOffsetUnits=-1;
  const plaza=new THREE.Mesh(new THREE.PlaneGeometry(p.w,p.d),material);plaza.name='Bahnhofsviertel · Platzbelag';plaza.rotation.x=-Math.PI/2;plaza.position.set(p.x,STATION_YARD.height+.003,p.z);plaza.receiveShadow=true;g.add(plaza);
 }
 // The bridge is above the streets: pillars stop feet, the deck only stops the camera.
 const v=VIADUCT;
 box(g,v.x,(v.bottom+v.top)/2,v.z,v.w,v.top-v.bottom,v.d,0x65594c);
 box(g,v.x,v.top+.12,0,v.w-.6,.24,v.d,0x494942);
 for(const x of [-149,-141]){
  box(g,x,5.7,0,.35,.3,158,0x34474a);
  box(g,x,7.1,0,.07,.055,158,0x3b4b4b);
  for(let z=-78;z<=78;z+=3)box(g,x,6.87,z,.055,.48,.055,0x3b4b4b);
 }
 for(let z=-78;z<=78;z+=1.6)box(g,-145,6.94,z,6.4,.18,.22,0x645343);
 for(const x of [-147.1,-146,-144,-142.9])box(g,x,7.07,0,.09,.15,158,0xa4a79e);
 for(const p of DISTRICT_FIXTURES){
  world.colliders.push({...p});
  if(p.kind==='pillar'){
   box(g,p.x,2.9,p.z,p.w*2,5.8,p.d*2,0x8b725a);
   box(g,p.x,5.42,p.z,1.7,.6,2.4,0xac9780);
   box(g,p.x,.36,p.z,1.25,.72,2.15,0x776759);
   for(let y=1;y<5;y+=.5)box(g,p.x,y,p.z+p.d+.012,1.06,.025,.02,0x68594b);
  }else if(p.kind==='planter'){
   const y=groundHeight(p.x,p.z);box(g,p.x,y+.36,p.z,1.8,.72,1.8,0x84745c);box(g,p.x,y+.735,p.z,1.62,.03,1.62,0x514d3b);
   for(let i=0;i<9;i++)sphere(g,p.x+Math.sin(i*2.4)*.55,y+.91,p.z+Math.cos(i*2.4)*.55,.3,[0x587347,0x6b8250,0x8a9359][i%3]);
  }else if(p.kind==='bench'){
   const y=groundHeight(p.x,p.z);for(const x of [p.x-1.1,p.x+1.1])box(g,x,y+.22,p.z,.09,.44,.7,0x344c4b);
   for(let i=0;i<4;i++)box(g,p.x,y+.48,p.z-.3+i*.19,3,.075,.14,0xa88a5c);
   for(let i=0;i<3;i++)box(g,p.x,y+.7+i*.14,p.z-.43,3,.10,.06,0xa88a5c);
  }else if(p.kind==='canopyPost')box(g,p.x,2.29,p.z,.18,4.22,.18,0x344947);
  else if(p.kind==='boundary'){
   // Masonry ends at the carriageway. A thin closed service gate marks the playable edge.
   const horizontal=p.w>p.d,start=horizontal?p.x-p.w:p.z-p.d,end=horizontal?p.x+p.w:p.z+p.d,cuts=horizontal?[-202]:[-65,0,65];let from=start;
   const wall=(a,b)=>{if(b<=a)return;box(g,horizontal?(a+b)/2:p.x,.48,horizontal?p.z:(a+b)/2,horizontal?b-a:.6,1.06,horizontal?.6:b-a,0x796d5b);};
   for(const c of cuts){wall(from,c-7.2);from=c+7.2;
    for(const side of [-1,1])box(g,horizontal?c+side*7.1:p.x,.65,horizontal?p.z:c+side*7.1,.16,1.35,.16,0x52665e);
    box(g,horizontal?c:p.x,1.03,horizontal?p.z:c,horizontal?14.2:.09,.10,horizontal?.09:14.2,0xafb6a5);
    for(let t=-6.6;t<7;t+=.65)box(g,horizontal?c+t:p.x,.67,horizontal?p.z:c+t,.045,.65,.045,0x52665e);
   }wall(from,end);
  }else box(g,p.x,.48,p.z,p.w*2,1.06,p.d*2,0x796d5b);
 }
 world.overheadColliders=[{x:v.x,z:v.z,w:v.w/2,d:v.d/2,minY:5.5,h:v.top+.25},{x:-175,z:-31.8,w:15,d:1.05,minY:4.3,h:4.5}];
 // A covered arrival area and a working clock make the station a recognizable landmark.
 box(g,-175,4.4,-31.8,30,.18,2.1,0x354c4b);
 for(const x of [-188,-162]){box(g,x,4.2,-32.1,.5,.2,.5,0xbda781);}
 sign(g,'BAHNHOFSPLATZ',-175,4.65,-30.72,12,.44,'#efe3c5','#344947');
 sign(g,'LINDENSTADT WEST · SEIT 1898',-175,7.1,-32.8,17,.46,'#e7d3ae','#6d5540');
 const clock=new THREE.Group();clock.position.set(-175,8.65,-32.75);s.add(clock);
 const face=new THREE.Mesh(new THREE.CircleGeometry(1.03,48),new THREE.MeshStandardMaterial({color:0xf0ddad,emissive:0xffcb7c,emissiveIntensity:.32}));clock.add(face);
 const rim=new THREE.Mesh(new THREE.TorusGeometry(1.05,.07,8,48),new THREE.MeshStandardMaterial({color:0x344946,metalness:.4,roughness:.4}));clock.add(rim);
 for(let i=0;i<12;i++){const a=i/12*Math.PI*2,tick=box(clock,Math.sin(a)*.86,Math.cos(a)*.86,.035,.055,.14,.025,0x344946);tick.rotation.z=-a;}
 const hand=length=>{const root=new THREE.Group();root.position.z=.07;box(root,0,length/2,0,.055,length,.028,0x283d3b);clock.add(root);return root;};
 world.stationClock={root:clock,hour:hand(.48),minute:hand(.72)};
 // Housing entrance and a sheltered shared court. Furnishings stay outside the walking aisle.
 const door=new THREE.Group();door.position.set(-179,0,19);door.rotation.y=Math.PI;g.add(door);
 box(door,0,1.45,.29,1.8,2.5,.12,0x354e4b);box(door,0,1.85,.36,1.42,1.3,.025,0xa49c76);
 box(door,.62,1.3,.41,.07,.3,.06,0xe0bd79);sign(door,'GLEISHÖFE · ZIMMER',0,2.93,.42,3.6,.24,'#eddcba','#354e4b');
 for(let i=0;i<4;i++){box(door,2+i*.36,1.2,.3,.29,.38,.16,0x526762);box(door,2+i*.36,1.3,.4,.19,.025,.01,0x243a39);}
 // Kiosk shelves and the workshop's roller door are visible in the street-facing windows.
 for(let row=0;row<3;row++)for(let i=0;i<9;i++)box(g,-181.9+i*.25,.65+row*.46,-14.65,.16,.28,.06,[0xb75e40,0xcbb78b,0x597774,0xd6cabc][(row+i)%4]);
 sign(g,'ZEITUNGEN · KAFFEE · KIEZ',-179,3.68,-14.59,7.5,.27,'#efd9ac','#485d50');
 for(let i=0;i<13;i++)box(g,-168,.45+i*.18,45.65,5.2,.145,.07,i%2?0x586762:0x738079);
 sign(g,'REPARIEREN STATT WEGWERFEN',-174,4.2,45.6,16,.37,'#e7d6b1','#354d50',Math.PI);
 // Legible wayfinding at every connection, with no false promise of a working train.
 for(const z of [-65,0,65]){
  for(const x of [-139.02,-150.98]){box(g,x,5.25,z,.12,.62,10.3,0x304949);for(const dz of [-4.6,4.6])box(g,x,5.64,z+dz,.10,.5,.10,0x34474a);}
  sign(g,'BAHNHOFSVIERTEL',-138.9,5.25,z,10,.48,'#eddbb7','#304949',Math.PI/2);
  sign(g,'INNENSTADT →',-151.1,5.25,z,9,.38,'#d1dace','#304949',-Math.PI/2);
 }
 const plaque=new THREE.Group();plaque.position.set(-179,2.2,39.18);g.add(plaque);box(plaque,0,0,0,3.8,.75,.10,0x354f4b);sign(plaque,'GLEISHOF',0,.13,.058,3.35,.27,'#ead9ba','#354f4b');sign(plaque,'WOHNEN & HANDWERK',0,-.18,.058,3.35,.18,'#c7d3c3','#354f4b');

 for(const [x,z] of [[-188,-56.3],[-159,-56.3],[-193,49]])world.tree(x,z,1.8);
 for(const z of [-47,45])world.atmosphere.addPuddle(-206,z,2,.7);
}
