import * as THREE from './vendor/three.module.js';
import {westBox,westMaterial,westWallArt} from './west-materials.js?v=0.8.1';
import {WEST_ROADS,WEST_PATHS} from './west-edge-layout.js?v=0.8.1';
import {groundHeight} from './spatial.js?v=0.8.1';
export function westKerbPlacements(){
 const out=[];
 for(const road of WEST_ROADS){const alongX=road.w>road.d,span=alongX?road.w:road.d,half=(alongX?road.d:road.w)/2+(road.pavementWidth??7);
  for(const side of [-1,1])for(let n=.5;n<span;n+=1.02){const x=road.x+(alongX?n-span/2:side*half),z=road.z+(alongX?side*half:n-span/2),w=alongX?.97:.18,d=alongX?.18:.97;
   if(WEST_PATHS.some(p=>Math.abs(x-p.x)<(p.w+w)/2+.15&&Math.abs(z-p.z)<(p.d+d)/2+.15)||WEST_ROADS.some(r=>Math.min(x+w/2,r.x+r.w/2)-Math.max(x-w/2,r.x-r.w/2)>.001&&Math.min(z+d/2,r.z+r.d/2)-Math.max(z-d/2,r.z-r.d/2)>.001))continue;
   out.push({x,z,w,d,y:groundHeight(x-side*(alongX?0:.04),z-side*(alongX?.04:0))});
  }
 }return out;
}
const fenceGeometries=new Map();let fenceMaterial;
const containerPaint=new Map();
export function westContainer(world,kit,parent,x,z,color=0x68786b){
 const {box,cylinder,sign}=kit,y=groundHeight(x,z),g=new THREE.Group();g.position.set(x,y,z);g.name='Corrugated container · double locking doors';parent.add(g);
 if(!containerPaint.has(color)){const m=westMaterial('steel').clone();m.color.set(color).lerp(new THREE.Color(0xffffff),.82);m.metalness=.12;containerPaint.set(color,m);}const m=containerPaint.get(color);
 box(g,0,1.30,0,5.8,2.6,2.75,0,m);
 for(const side of [-1,1]){
  for(let n=0;n<23;n++)box(g,-2.75+n*.25,1.33,side*1.405,.10,2.32,.07,0,m);
  for(const yy of [.13,2.50])westBox(g,'steel',0,yy,side*1.43,5.9,.12,.12);
  for(const dx of [-2.84,2.84])westBox(g,'steel',dx,1.3,side*1.43,.13,2.60,.13);
 }
 // Door end faces the yard rather than reading as a coloured cube.
 for(const dz of [-.68,.68]){
  westBox(g,'steel',2.93,1.30,dz,.08,2.40,1.28);
  for(const offset of [-.38,.38]){const bar=cylinder(g,3.005,1.30,dz+offset,.025,2.20,0x96968a);for(const yy of [.38,1.05,2.19])box(g,3.035,yy,dz+offset,.035,.10,.11,0x5d635a);box(g,3.05,1.04,dz+offset+.10,.035,.05,.25,0xa29d89);}
  for(const yy of [.42,1.30,2.17])box(g,2.99,yy,dz+Math.sign(dz)*.52,.08,.11,.19,0x59675e);
 }
 for(const side of [-1,1])for(let n=0;n<5;n++)westBox(g,'steel',-2.92,.30+n*.47,side*.68,.09,.05,1.22);
 westBox(g,'steel',0,2.65,0,5.93,.10,2.90);
 for(const dx of [-2.7,2.7])for(const dz of [-1.28,1.28])westBox(g,'concrete',dx,.02,dz,.33,.14,.30);
 sign(g,'ALTGLEIS · LAGER 02',-.8,1.80,1.453,2.45,.30,'#d2c7a7','#48584b');westWallArt(g,'rail',1.55,.76,1.455,1.55,.62);
 world.colliders.push({x:x+.05,z,w:3.06,d:1.49,h:y+2.72,westContainer:true});
}
export function westStoreShed(world,kit,parent,x,z){
 const {box,sign}=kit,y=groundHeight(x,z),g=new THREE.Group();g.position.set(x,y,z);g.name='Garden store · door, eaves and window';parent.add(g);
 westBox(g,'wood',0,1.02,0,3,2.04,2.2);westBox(g,'concrete',0,.06,0,3.12,.12,2.32);
 const roof=westBox(g,'steel',0,2.15,0,3.35,.12,2.55);roof.rotation.x=.10;
 for(const dx of [-1.43,1.43])westBox(g,'wood',dx,1.04,1.13,.10,2.08,.10);
 westBox(g,'wood',-.28,1.00,1.145,1.02,1.92,.08);box(g,-.28,1.99,1.19,1.15,.075,.08,0x908571);box(g,.08,1.00,1.20,.035,.20,.06,0xa59e89);
 box(g,.95,1.50,1.15,.67,.57,.06,0x939786);box(g,.95,1.50,1.188,.55,.45,.025,0x384e4d);box(g,.95,1.5,1.21,.025,.45,.025,0x9b9c8a);
 sign(g,'GERÄTE',-.28,1.62,1.20,.63,.16,'#c8b995','#5b5b49');
 world.colliders.push({x,z,w:1.57,d:1.22,h:y+2.35,westStore:true});
}
export function westBusShelter(world,kit,parent,{line,bench}){
 const {box,cylinder,sign}=kit,x=-278,z=14.8,y=groundHeight(x,z),g=new THREE.Group();g.position.set(x,y,z);g.name='Aged bus shelter · open boarding side';parent.add(g);
 for(const dx of [-2.5,2.5])for(const dz of [-.80,.88]){westBox(g,'steel',dx,1.29,dz,.065,2.58,.065);world.colliders.push({x:x+dx,z:z+dz,w:.045,d:.045,h:y+2.62});}
 const glass=kit.mat(0x718582).clone();glass.transparent=true;glass.opacity=.32;glass.depthWrite=false;
 box(g,0,1.35,.94,4.91,2.30,.035,0,glass);for(const dx of [-2.49,2.49])box(g,dx,1.35,0,.035,2.30,1.78,0,glass);
 for(const yy of [.22,2.50]){westBox(g,'steel',0,yy,.96,5,.06,.065);for(const dx of [-2.50,2.50])westBox(g,'steel',dx,yy,0,.065,.06,1.9);}
 world.colliders.push({x,z:z+.96,w:2.5,d:.055,h:y+2.57});for(const dx of [-2.5,2.5])world.colliders.push({x:x+dx,z,w:.055,d:.90,h:y+2.57});
 const roof=westBox(g,'steel',0,2.68,0,5.55,.12,2.4);roof.rotation.x=.035;
 world.overheadColliders.push({x,z,w:2.775,d:1.2,minY:y+2.575,h:y+2.785,westBusRoof:true});
 for(const dx of [-2.50,2.50])line(g,{x:dx,y:2.53,z:.90},{x:dx,y:2.58,z:-.98},.05,0x7c8070);
 bench(x,z+.40);sign(g,'WESTBRUCH · LINIE B1',0,2.24,.91,3.50,.24,'#d6cdac','#4e6555',Math.PI);
 westBox(g,'steel',1.89,1.4,.885,.57,.94,.045);sign(g,'B1 · FAHRPLAN',1.89,1.48,.85,.44,.51,'#d1c8ab','#697467',Math.PI);
}
export function buildWestEdges(world,kit,{group,line}){
 const {box,cylinder,sign}=kit;
 // Individual old kerbstones and gutters follow streets; driveway mouths stay open.
 for(const {x,z,w,d,y} of westKerbPlacements())westBox(group(x,z),'concrete',x,y-.05,z,w,.18,d);
 // A little resurfacing with irregular boundaries breaks up the road's tiling.
 const repair=westMaterial('asphalt').clone();repair.color.set(0x89938b);
 for(const [j,[x,z,w,d]] of [[-530,1.4,3.1,1.1],[-485,-1.2,4.4,1.3],[-447,.7,2.1,1.3],[-405,-1.2,5.1,1.4],[-351,1.2,2.5,1.4],[-282,-2.7,3.1,1.5]].entries()){
  const vertices=[],uv=[],y=groundHeight(x,z)+.008,points=[];for(let n=0;n<10;n++){const a=n*Math.PI/5,r=.87+.09*Math.sin(n*17+j);points.push([x+Math.cos(a)*w*.5*r,y,z+Math.sin(a)*d*.5*r]);}
  for(let n=0;n<10;n++)for(const p of [[x,y,z],points[(n+1)%10],points[n]]){vertices.push(...p);uv.push(p[0]/2.5,p[2]/2.5);}
  const geo=new THREE.BufferGeometry();geo.setAttribute('position',new THREE.Float32BufferAttribute(vertices,3));geo.setAttribute('uv',new THREE.Float32BufferAttribute(uv,2));geo.computeVertexNormals();const mesh=new THREE.Mesh(geo,repair);mesh.receiveShadow=true;mesh.userData.staticBatch=true;group(x,z).add(mesh);
 }
 // Old lamp heads: an arched arm, metal hood and recessed warm diffuser.
 for(const x of [-324,-295,-268]){const z=9,g=group(x,z),y=groundHeight(x,z);cylinder(g,x,y+.2,z,.13,.40,0x6c7364);cylinder(g,x,y+2.7,z,.065,5.4,0x535e53);
  let a={x,y:y+4.60,z};for(let i=1;i<=5;i++){const t=i/5,b={x,y:y+4.60+.75*Math.sin(t*Math.PI/2),z:z-t*1.5};line(g,a,b,.065,0x626e60);a=b;}
  westBox(g,'steel',x,y+5.31,z-1.5,.69,.15,.41);box(g,x,y+5.23,z-1.5,.54,.025,.29,0xc8b78d);
  world.atmosphere.registerLamp(x,z-1.5,null,{height:y+5.2,power:70,poolSize:7,distance:18});world.colliders.push({x,z,w:.14,d:.14,h:y+5.5});
 }
 for(const x of [-528,-489,-450,-412,-374,-337]){const g=group(x,8.3);for(const dx of [-.55,0,.55]){cylinder(g,x+dx,6.43,8.3,.055,.24,0xaba690);for(const yy of [6.35,6.41,6.47])cylinder(g,x+dx,yy,8.3,.08,.025,0xc0baa6);}box(g,x,3.5,8.3,.30,.47,.20,0x566456);}
 // Open scrap skip with recognisable rim, lifting eyes and irregular contents.
 const x=-412,z=-34,g=group(x,z),y=groundHeight(x,z);
 westBox(g,'steel',x,y+.18,z,3.1,.25,1.65);for(const side of [-1,1]){const wall=westBox(g,'steel',x,y+.70,z+side*.83,3.15,1.1,.09);wall.rotation.x=side*.12;westBox(g,'steel',x,y+1.27,z+side*.90,3.30,.09,.15);westBox(g,'steel',x+side*1.53,y+.68,z,.09,1.10,1.73);}
 const scrap=kit.mat(0x645c50);for(let n=0;n<8;n++){const p=box(g,x-1.15+n*.32,y+.38,z+Math.sin(n*3)*.45,.60,.07,.46,0,scrap);p.rotation.set(n*.37,n*.9,.4);p.position.y+=y+.31-new THREE.Box3().setFromObject(p).min.y;}
 sign(g,'METALL · KEIN RESTMÜLL',x,y+.85,z+.91,2.3,.25,'#c3b58d','#536653');world.colliders.push({x,z,w:1.7,d:1.03,h:y+1.35});
}
export function westFenceMesh(parent,x,y,z,length,height,angle=0){
 if(!fenceMaterial){
  let map=null;if(typeof document!=='undefined'){
   const c=document.createElement('canvas');c.width=c.height=128;const a=c.getContext('2d');a.lineWidth=2.2;a.strokeStyle='#8e9685';
   for(let n=-128;n<=256;n+=32){a.beginPath();a.moveTo(n,0);a.lineTo(n+128,128);a.moveTo(n,0);a.lineTo(n-128,128);a.stroke();}
   map=new THREE.CanvasTexture(c);map.colorSpace=THREE.SRGBColorSpace;map.wrapS=map.wrapT=THREE.RepeatWrapping;map.anisotropy=4;
  }
  fenceMaterial=new THREE.MeshStandardMaterial({map,color:0x687263,alphaTest:.42,side:THREE.DoubleSide,roughness:.85,metalness:.25});
 }
 const key=length+':'+height;if(!fenceGeometries.has(key)){const geo=new THREE.PlaneGeometry(length,height),uv=geo.attributes.uv;for(let i=0;i<uv.count;i++)uv.setXY(i,uv.getX(i)*length/.52,uv.getY(i)*height/.52);fenceGeometries.set(key,geo);}
 const mesh=new THREE.Mesh(fenceGeometries.get(key),fenceMaterial);mesh.position.set(x,y,z);mesh.rotation.y=angle;mesh.receiveShadow=true;parent.add(mesh);return mesh;
}

export function buildWestIndustry(world,kit,{group,solid,line,pallet}){
 const {box,cylinder,sign,mat}=kit;
 // A parked wagon gives the disused rail triangle a recognisable silhouette.
 const g=new THREE.Group(),y=groundHeight(-400,-55);g.name='Wagon chassis and body';g.userData.westAssembly=true;group(-400,-55).add(g);
 westBox(g,'steel',-400,y+.89,-55,12,.32,2.8);
 westBox(g,'steel',-400,y+2.0,-55,11.6,1.9,2.66);
 for(const z of [-56.38,-53.62]){
  for(let x=-405.5;x<=-394.5;x+=1.15)westBox(g,'steel',x,y+2.04,z,.085,1.9,.10);
  for(const yy of [1.1,2.97])westBox(g,'steel',-400,y+yy,z,11.8,.08,.13);
 }
 for(const x of [-404.3,-395.7])for(const z of [-55.72,-54.28]){
  const wheel=cylinder(g,x,.5125,z,.42,.16,0x353d38);wheel.rotation.x=Math.PI/2;wheel.userData.westRailWheel=true;
  const hub=cylinder(g,x,.5125,z+Math.sign(z+55)*.1,.17,.04,0x6b6e60);hub.rotation.x=Math.PI/2;
 }
 for(const x of [-406.4,-393.6]){box(g,x,y+.93,-55,.8,.09,.11,0x4c5149);for(const z of [-55.95,-54.05])cylinder(g,x,y+.94,z,.11,.2,0x484d45);}
 sign(g,'ALTGLEIS · 041',-400,y+2.22,-53.548,2.8,.35,'#c8b99c','#4d5a50');westWallArt(g,'rail',-396,y+1.9,-53.54,2.5,.9);
 solid(-400,-55,12.4,3,3.2);
 // Stacked culvert pipes and tires, with open centres rather than solid boxes.
 const tube=new THREE.CylinderGeometry(.64,.64,1.5,16,1,true),rim=new THREE.TorusGeometry(.61,.055,5,20),rubber=mat(0x303731),tire=new THREE.TorusGeometry(.39,.105,6,16);
 const yard=group(-351,-64),pipes=new THREE.Group(),pipeMat=westMaterial('concrete').clone();pipeMat.side=THREE.DoubleSide;pipes.name='Supported pipe stack';pipes.userData.westAssembly=true;yard.add(pipes);
 const pipeBase=groundHeight(-353,-64),bottom=pipeBase+.665,top=bottom+Math.sqrt(1.33**2-.70**2);
 for(const [x,z,yy] of [[-353,-64,bottom],[-351.6,-64,bottom],[-352.3,-64,top]]){
  const m=new THREE.Mesh(tube,pipeMat);m.rotation.x=Math.PI/2;m.position.set(x,yy,z);m.userData.westPipe=true;pipes.add(m);m.castShadow=true;m.receiveShadow=true;
  for(const side of [-1,1]){const lip=new THREE.Mesh(rim,westMaterial('concrete'));lip.position.set(x,yy,z+side*.75);pipes.add(lip);lip.castShadow=true;}
 }
 solid(-352.3,-64,3.1,1.6,top+.64);
 tire.computeBoundingBox();const tireHalfHeight=tire.boundingBox.max.z;
 for(let n=0;n<4;n++){const m=new THREE.Mesh(tire,rubber);m.rotation.x=Math.PI/2;m.position.set(-357,groundHeight(-357,-60)+tireHalfHeight*(1+2*n),-60);m.userData.westStackTire=true;yard.add(m);m.castShadow=true;}solid(-357,-60,1,1,groundHeight(-357,-60)+tireHalfHeight*8);
 pallet(-360,-62);pallet(-361.5,-62);
 // Tarp shelter sits beyond the through pavement, beneath the actual deck.
 const camp=group(-423,7.95),ground=groundHeight(-423,7.95),shape=new THREE.BufferGeometry();
 shape.setAttribute('position',new THREE.Float32BufferAttribute([-1.2,0,-.65,0,1.3,-.65,0,1.3,.65,-1.2,0,-.65,0,1.3,.65,-1.2,0,.65,0,1.3,-.65,1.2,0,-.65,1.2,0,.65,0,1.3,-.65,1.2,0,.65,0,1.3,.65],3));shape.computeVertexNormals();
 const tarp=new THREE.Mesh(shape,mat(0x4d5a4d,.97).clone());tarp.position.set(-423,ground,7.95);tarp.material.side=THREE.DoubleSide;tarp.castShadow=true;camp.add(tarp);
 line(camp,{x:-423,y:ground,z:7.25},{x:-423,y:ground+1.3,z:7.25},.04,0x6d6d57);
 westBox(camp,'wood',-423.6,ground+.08,7.3,1,.16,.8);solid(-423,7.95,2.45,1.4,ground+1.35);
 for(const [x,z] of [[-425,8.1],[-420.8,8.2]]){cylinder(camp,x,ground+.3,z,.28,.6,0x63584a);solid(x,z,.6,.6,ground+.65);}
 pallet(-419.1,8.2);
}
