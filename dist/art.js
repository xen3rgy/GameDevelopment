import {ALLOY_STYLES,createAlloyRim} from './tuning-rims.js?v=0.8.1';
import {buildingStyle,modernBuilding} from './district-architecture.js?v=0.8.1';
import {entranceForBuilding,decorateAddress} from './city-addresses.js?v=0.8.1';
import * as THREE from './vendor/three.module.js';
import {groundHeight} from './spatial.js?v=0.8.1';
import {districtMaterial,fitFacadeUV} from './district-materials.js?v=0.8.1';
import {facadeSpans} from './public-realm-layout.js?v=0.8.1';

const palette=[
 {wall:0xa86248,trim:0xdfcbb0,shop:0x254e45,type:'brick'},
 {wall:0xc5b791,trim:0xe4d8bf,shop:0x35556c,type:'plaster'},
 {wall:0xd5c9ad,trim:0xe9dec6,shop:0xa35236,type:'plaster'},
 {wall:0xb8c1b4,trim:0xe3dec9,shop:0x254d48,type:'plaster'},
 {wall:0xb68266,trim:0xe6d4b7,shop:0x3e535b,type:'brick'},
 {wall:0xc0c4bd,trim:0xddd9c8,shop:0x496077,type:'plaster'},
 {wall:0x52656b,trim:0x89989a,shop:0x273b45,type:'stone'},
 {wall:0xb69d7a,trim:0xe5d2af,shop:0x9c6639,type:'brick'}
];
export function seeded(seed=1){let n=seed>>>0;return()=>((n=(n*1664525+1013904223)>>>0)/4294967296)}
const textures=new Map();
function wallTexture(kind){
 if(textures.has(kind))return textures.get(kind);
 const c=document.createElement('canvas');c.width=c.height=512;const a=c.getContext('2d'),r=seeded(84);
 a.fillStyle='#aaa69c';a.fillRect(0,0,512,512);
 if(kind==='wood')for(let x=0;x<512;x+=64){a.fillStyle=x%128?'#b9ad92':'#a99b7e';a.fillRect(x+1,0,62,512);for(let i=0;i<120;i++){a.fillStyle=r()>.5?'#ffffff0d':'#32201113';a.fillRect(x+3+r()*56,r()*512,1,10+r()*100)}}
 if(kind==='brick'||kind==='stone')for(let y=0;y<512;y+=kind==='brick'?16:64)for(let x=-80;x<512;x+=kind==='brick'?64:128){const ww=kind==='brick'?64:128,hh=kind==='brick'?16:64,xx=x+(y/hh%2?ww/2:0),v=175+Math.floor(r()*35);a.fillStyle=`rgb(${v},${v-3},${v-8})`;a.fillRect(xx+1,y+1,ww-2,hh-2);a.fillStyle='#ffffff20';a.fillRect(xx+2,y+1,ww-4,1)}
 for(let i=0;i<36000;i++){a.fillStyle=r()>.5?'#ffffff0b':'#0000000b';a.fillRect(r()*512,r()*512,1,1)}
 const t=new THREE.CanvasTexture(c);t.colorSpace=THREE.SRGBColorSpace;t.wrapS=t.wrapT=THREE.RepeatWrapping;t.anisotropy=4;textures.set(kind,t);return t;
}
export class CityArt{
 constructor(world,kit){this.w=world;this.k=kit;this.index=0;this.windows=[];this.warm=[];this.foliage=[];this.texture=new THREE.TextureLoader().load('./assets/lindenstadt-mural.png');this.texture.colorSpace=THREE.SRGBColorSpace;this.texture.anisotropy=4;
  for(let i=0;i<5;i++)this.windows.push(new THREE.MeshStandardMaterial({color:[0x283c48,0x394f58,0x687777,0x454a41,0x84775d][i],roughness:.24,metalness:.32,emissive:0xffca7c,emissiveIntensity:0}));
 }
 building(x,z,w,d,h,color,name,face=1){
  if(buildingStyle(x,z,name)==='modern'){this.index++;return modernBuilding(this,x,z,w,d,h,color,name,face);}
  const {box,sign,mat,cylinder}=this.k,scene=this.w.scene,g=new THREE.Group();scene.add(g);this.w.staticGroups.push(g);
  const index=this.index++,p=x<-120&&Math.abs(z)<125?{wall:color,trim:0xc2b095,shop:0x3d5953,type:'brick'}:palette[index%palette.length];
  let texture;if(x>=-120||Math.abs(z)>=125){texture=wallTexture(p.type).clone();texture.needsUpdate=true;texture.repeat.set(w/6,h/6);}
  const wall=x<-120&&Math.abs(z)<125?districtMaterial('brick',w,h,index%2?0xd8c7ad:0xdfd1bc):new THREE.MeshStandardMaterial({color:name?p.wall:color,map:texture,bumpMap:texture,bumpScale:p.type==='plaster'?.024:.065,roughness:.94});
  fitFacadeUV(box(g,x,h/2,z,w,h,d,0,wall),w,d);box(g,x,.58,z,w+.16,1.15,d+.16,p.trim);box(g,x,3.65,z,w+.28,.22,d+.28,p.trim);
  box(g,x,h+.12,z,w+.6,.25,d+.6,p.trim);box(g,x,h+.42,z,w+.32,.35,d+.32,0x424e51);box(g,x,h+.64,z,w-.4,.1,d-.4,0x5e6461);
  this.w.colliders.push({x,z,w:w/2+.35,d:d/2+.35,h:h+2.8});
  if(!name&&Math.abs(z)>125){for(let y=5;y<h;y+=4)box(g,x,y,z+d/2+.025,w-.8,1.5,.035,0x465b64);return g}
  // Separate faces keep windows at a human scale instead of stretching a painted grid.
  for(const [xx,zz,width,rot] of [[x,z+d/2,w,0],[x,z-d/2,w,Math.PI],[x+w/2,z,d,Math.PI/2],[x-w/2,z,d,-Math.PI/2]]){
   const f=new THREE.Group();f.position.set(xx,0,zz);f.rotation.y=rot;g.add(f);
   const cols=Math.max(3,Math.floor(width/3.5)),spacing=width/cols,floors=h<6?0:Math.max(1,Math.floor((h-3.9)/3.3));
   for(let row=0;row<floors;row++){const y=5.35+row*(h-4.3)/floors;
    if(index%3!==0)box(f,0,y-1.22,.08,width,.085,.16,p.trim);
    for(let col=0;col<cols;col++){const wx=-width/2+spacing*(col+.5);if(index===0&&rot===-Math.PI/2&&Math.abs(wx-1)<6&&y<12.5)continue;const lit=(col*7+row*13+index)%5;
     box(f,wx,y,.04,1.72,2.18,.09,0x273337);box(f,wx,y,.10,1.43,1.92,.055,0,this.windows[lit]);
     for(const dx of [-.81,.81])box(f,wx+dx,y,.13,.10,2.18,.13,p.trim);
     box(f,wx,y+1.05,.15,1.83,.13,.18,p.trim);box(f,wx,y-1.1,.22,1.92,.16,.4,p.trim);
     box(f,wx,y,.145,.055,1.95,.045,0x94988a);box(f,wx,y+.26,.145,1.46,.045,.045,0x94988a);
     if(lit===2)box(f,wx+.38,y,.135,.46,1.8,.017,0xa79e88);
     if(row<2&&col%3===1&&index%3!==0){box(f,wx,y-.87,.69,2,.1,1.2,0x6b7472);box(f,wx,y-.23,1.22,2,.055,.06,0x344c4b);for(let rail=-.9;rail<=.91;rail+=.3)box(f,wx+rail,y-.55,1.22,.035,.63,.035,0x344c4b);if(lit%2){box(f,wx,y-.63,1.24,1.1,.26,.25,0x805c40);for(let n=0;n<6;n++)this.k.sphere(f,wx-.46+n*.18,y-.42,1.23,.16,0x516849)}}
    }
   }
   for(const dx of [-width/2+.2,width/2-.2])box(f,dx,(h+3.7)/2,.04,.27,h-3.7,.18,p.trim);
   if(x<-120){
    // Water marks and repaired render stay on the wall; no opaque overlay over the windows.
    for(const side of [-1,1]){
     const edge=side*(width/2-.48);
     cylinder(f,edge,Math.min(h-1,5)/2,.22,.045,Math.min(h-1,5),0x626b61);
     for(let n=0;n<4;n++)box(f,edge-side*(.18+n*.055),.44+n*.07,.088,.08,.30+n*.08,.01,[0x8d8b76,0x99947d][n%2]);
     const patch=new THREE.Shape();patch.moveTo(-.5,0);patch.lineTo(.47,.03);patch.lineTo(.53,.25);patch.lineTo(.19,.32);patch.lineTo(.02,.27);patch.lineTo(-.41,.38);patch.lineTo(-.5,0);
     const repair=new THREE.Mesh(new THREE.ShapeGeometry(patch),mat(0xada18a));repair.position.set(edge-side*.8,.62,.087);f.add(repair);
    }
   }

  }
  if(!name){for(const [xx,zz,width,rot] of [[x,z+d/2,w,0],[x,z-d/2,w,Math.PI],[x+w/2,z,d,Math.PI/2],[x-w/2,z,d,-Math.PI/2]]){const f=new THREE.Group();f.position.set(xx,0,zz);f.rotation.y=rot;g.add(f);for(let col=-width/2+1.8;col<width/2-1;col+=3.2){box(f,col,1.85,.13,1.3,1.75,.10,0x304842);box(f,col,1.87,.195,1.08,1.54,.018,0,this.windows[3]);box(f,col,1.87,.22,.065,1.55,.035,0xc2b095);}box(f,0,.7,.12,width,.12,.2,0x857563);}return g;}
  const front=new THREE.Group();front.position.set(x,0,z+face*d/2);front.rotation.y=face===1?0:Math.PI;g.add(front);
  box(front,0,1.78,.07,w-.45,3.25,.16,p.shop);
  const location=entranceForBuilding(x,z,d,face),doorX=location?(location.x-x)*face:null;
  const bays=Math.floor(w/3),bayW=(w-1)/bays;
  for(let i=0;i<bays;i++){const xx=-w/2+.5+bayW*(i+.5);for(const part of facadeSpans(xx,bayW-.16,doorX)){const {x:cx,w:ww}=part;box(front,cx,1.55,.18,ww-.04,2.5,.05,0,this.windows[(i+index)%5]);for(const side of [-1,1])box(front,cx+side*ww/2,1.58,.24,.075,2.6,.12,p.trim);box(front,cx,.34,.24,ww,.16,.14,p.trim);box(front,cx,2.71,.24,ww,.12,.14,p.trim);box(front,cx,2.04,.25,ww,.05,.06,p.trim);}}
  box(front,0,3.08,.22,w-.35,.58,.28,p.shop);sign(front,name,0,3.1,.38,Math.min(w-1,name.length*.43),.42,'#f3e9cd','#'+p.shop.toString(16).padStart(6,'0'));
  // Three separate canvas awnings break the repeated solid strip silhouette.
  for(const xx of [-w*.31,0,w*.31])for(const part of facadeSpans(xx,w*.27,doorX,1.35)){if(part.w<.7)continue;const awning=new THREE.Group();awning.name='Canvas awning';awning.position.set(part.x,2.87,.88);awning.rotation.x=.11;front.add(awning);box(awning,0,0,0,part.w,.07,1.5,p.shop);box(awning,0,-.085,.73,part.w,.16,.04,p.shop);for(let stripe=-part.w/2+.2;stripe<part.w/2-.1;stripe+=.46)box(awning,stripe,.039,0,.12,.006,1.46,p.trim);}
  if(location)decorateAddress(front,this.k,location,doorX);
  for(let xx of [-w*.35,w*.35]){cylinder(front,xx,3.85,.5,.055,.8,0x323b3b);box(front,xx,4.2,.57,.32,.12,.38,0x394544)}
  if(index%3===0){const roof=new THREE.Mesh(new THREE.ConeGeometry(1,1,4),mat(0x4a5358));roof.position.set(x,h+1.1,z);roof.scale.set(w/Math.SQRT2,2.4,d/Math.SQRT2);roof.rotation.y=Math.PI/4;roof.castShadow=true;g.add(roof)}
  for(let n=0;n<3;n++){box(g,x-w*.32+n*w*.22,h+1.1,z-d*.2,1,.9,.8,p.wall);box(g,x-w*.32+n*w*.22,h+1.6,z-d*.2,1.15,.14,.95,0x505b5d)}
  if(index===0){const mural=new THREE.Mesh(new THREE.PlaneGeometry(10.8,7.2),new THREE.MeshStandardMaterial({map:this.texture,roughness:1}));mural.position.set(x-w/2-.23,8.4,z+1);mural.rotation.y=-Math.PI/2;g.add(mural)}
  return g;
 }
 tree(x,z,r,options={}){if(options.grate!==false)this.w.treePitSystem.add(x,z);return this.w.treeSystem.add(x,z,r,options)}
  homeFinish(){
  const {box,cylinder,sign}=this.k,g=this.w.homeDecor,texture=wallTexture('wood').clone();texture.needsUpdate=true;texture.repeat.set(3,2);
  const floor=new THREE.MeshStandardMaterial({map:texture,color:0xbda57a,roughness:.68,bumpMap:texture,bumpScale:.018});box(g,300,.069,0,13.7,.002,11.7,0,floor);
  for(const x of [293.17,306.83])box(g,x,.2,0,.055,.27,11.8,0xe1d9c4);for(const z of [-5.83,5.83])box(g,300,.2,z,13.7,.27,.055,0xe1d9c4);
  box(g,299,.078,1.4,3.7,.012,2.7,0x596f73);for(const x of [297.2,300.8])box(g,x,.086,1.4,.035,.003,2.6,0xd0b28a);
  box(g,293.19,2,-2,.05,2.1,2.6,0x394b55);box(g,293.24,2,-2,.045,1.9,2.4,0x93b4bd);box(g,293.27,2,-2,.035,1.95,.065,0xd9d8c9);box(g,293.27,2,-2,.035,.065,2.4,0xd9d8c9);
  for(const z of [-3.3,-.7])for(let n=0;n<5;n++)cylinder(g,293.4+n*.025,1.85,z+n*.05,.05,2.15,0xd4c7af);
  cylinder(g,305,2.1,-1.6,.035,2.2,0x6c705e);const shade=new THREE.Mesh(new THREE.ConeGeometry(.42,.38,16),new THREE.MeshStandardMaterial({color:0xe7d2a7,emissive:0xffc47b,emissiveIntensity:.25}));shade.position.set(305,2.9,-1.6);g.add(shade);
  for(const [n,col] of [0xa66549,0x536f7a,0xa8ab79].entries())box(g,295.65+n*.13,1.36,2.3,.1,.38,.24,col);
  for(const x of [301.1,302.4,303.7,304.8]){box(g,x,1.0,-3.98,.06,.26,.05,0xc9c5b0)}
 }
 streetLife(){
  const {box,cylinder,sphere,sign,mat}=this.k,g=new THREE.Group();this.w.scene.add(g);this.w.staticGroups.push(g);
  for(const x of [57,69]){cylinder(g,x,1.65,-11,.038,2.7,0x463e32);const shade=new THREE.Mesh(new THREE.ConeGeometry(1.9,.45,8),mat(0xd5bf94));shade.position.set(x,3,-11);shade.castShadow=true;g.add(shade);cylinder(g,x,3.25,-11,.045,.18,0x584633);for(const dx of [-.45,.35]){cylinder(g,x+dx,1.07,-11,.07,.13,0xe1d9c6);box(g,x+dx,1,-11,.22,.02,.2,0xe1d9c6)}}
  for(const x of [-44,-28]){box(g,x,.7,-12.1,1.4,.75,.85,0x66553c);box(g,x,1.1,-12.1,1.55,.08,.96,0x987a4a);for(let n=0;n<12;n++){const px=x-.55+(n%4)*.35,pz=-12.4+Math.floor(n/4)*.27;sphere(g,px,1.21,pz,.125,x===-44?0xa75c39:0x80994f)}sign(g,x===-44?'FRISCH AUS DER REGION':'OBST & GEMÜSE',x,.78,-11.65,1.2,.2,'#eadfbd','#456044')}
  // Shop-specific street signs stay close to facades, leaving the main footway free.
  for(const [x,z,title,sub] of [[61,-12.3,'CAFÉ MORGEN','KAFFEE · KUCHEN'],[-39,-12.3,'MARKT 24','FRISCH IM KIEZ'],[29,-12.3,'KIEZ & KURIER','DEIN NÄCHSTER JOB']]){box(g,x,.9,z,.8,1.15,.10,0x715f46);box(g,x,.93,z+.065,.66,.92,.018,0x29413e);sign(g,title,x,1.18,z+.08,.59,.15,'#e8dfc4','#29413e');sign(g,sub,x,.92,z+.08,.61,.12,'#c4d4b8','#29413e');for(const dx of [-.34,.34])box(g,x+dx,.4,z+.05,.06,.7,.08,0x715f46)}
 }
 update(night){this.windows.forEach((m,i)=>m.emissiveIntensity=(i===3?1.25:i===4?.62:i===0?.38:.025)*night)}
}

export {createCitizen,applyCitizenOutfit} from './citizen.js?v=0.8.1';

// Shared static geometry; each vehicle keeps its own paint, rims and lamp materials.
const vehicleGeometry=new Map();
const vehicleGlass=new THREE.MeshStandardMaterial({color:0x304953,roughness:.19,metalness:.45,side:THREE.DoubleSide});
function vehicleGeo(key,build){if(!vehicleGeometry.has(key))vehicleGeometry.set(key,build());return vehicleGeometry.get(key)}
function vehiclePanel(points){return vehicleGeo('panel:'+JSON.stringify(points),()=>{const geo=new THREE.BufferGeometry(),v=[];for(let i=1;i<points.length-1;i++)v.push(...points[0],...points[i],...points[i+1]);geo.setAttribute('position',new THREE.Float32BufferAttribute(v,3));geo.computeVertexNormals();return geo})}
export function createCar(kit,id='car',color=0x506d78){
 const {box,mat,sign}=kit,g=new THREE.Group(),van=id==='van',sport=id==='sport',length=van?4.8:sport?4.1:3.8,L=length/2;
 const paint=new THREE.MeshStandardMaterial({color,roughness:sport?.27:.36,metalness:.45,side:THREE.DoubleSide});
 const rimMaterial=new THREE.MeshStandardMaterial({color:sport?0xc3c8c7:0x9da7a5,roughness:.31,metalness:.75});
 const trim=mat(0x293238,.83).clone(),glass=vehicleGlass.clone(),rimDark=mat(0x20282c,.8),rubber=mat(0x202628,.96),chrome=mat(0x939e9e,.36);
 const paintParts=[],rimParts=[],trimParts=[];
 const mesh=(parent,geo,material)=>{const m=new THREE.Mesh(geo,material);m.castShadow=true;m.receiveShadow=true;parent.add(m);if(material===paint)paintParts.push(m);if(material===rimMaterial)rimParts.push(m);if(material===trim)trimParts.push(m);return m};
 const block=(parent,x,y,z,w,h,d,material)=>{const m=box(parent,x,y,z,w,h,d,0,material);if(material===paint)paintParts.push(m);if(material===trim)trimParts.push(m);return m};
 const panel=(parent,points,material)=>mesh(parent,vehiclePanel(points),material);
 const loft=(parent,z0,z1,y0,y1,w0,w1,bottom,material)=>{const a=[-w0,bottom,z0],b=[w0,bottom,z0],c=[w1,bottom,z1],d=[-w1,bottom,z1],e=[-w0,y0,z0],f=[w0,y0,z0],h=[-w1,y1,z1],i=[w1,y1,z1];const faces=[[a,b,f,e],[b,c,i,f],[c,d,h,i],[d,a,e,h],[e,f,i,h]];mesh(parent,vehicleGeo('loft:'+JSON.stringify(faces),()=>{const geo=new THREE.BufferGeometry(),v=[];for(const face of faces)for(const n of [0,1,2,0,2,3])v.push(...face[n]);geo.setAttribute('position',new THREE.Float32BufferAttribute(v,3));geo.computeVertexNormals();return geo}),material)};
 // Original length/width envelope and axle centers are unchanged. Arches remove the
 // slab across the wheels; a narrow underbody joins the two sculpted side skins.
 block(g,0,.59,0,1.42,.27,length-.12,trim);
 const shoulder=sport?.94:1.01;
 const sideGeo=vehicleGeo('body:'+id,()=>{const s=new THREE.Shape();s.moveTo(-L+.04,.48);for(const z of [-1.19,1.19]){s.lineTo(z-.46,.48);s.quadraticCurveTo(z-.44,.87,z,.89);s.quadraticCurveTo(z+.44,.87,z+.46,.48)}s.lineTo(L-.04,.48);s.lineTo(L-.04,sport?.88:.94);s.lineTo(L-.3,sport?.91:.97);s.lineTo(.55,shoulder);s.lineTo(-.95,shoulder);s.lineTo(-L+.17,.88);s.lineTo(-L+.04,.77);s.closePath();const geo=new THREE.ExtrudeGeometry(s,{depth:.15,bevelEnabled:false,curveSegments:6});geo.rotateY(-Math.PI/2);return geo});
 for(const side of [-1,1]){const skin=mesh(g,sideGeo,paint);skin.position.x=side===1?.94:-.79;}
 // Hood and rear deck are tapered planes, rather than stacked rectangular blocks.
 loft(g,.64,L-.04,shoulder,sport?.88:.94,.85,.79,.73,paint);
 loft(g,-L+.04,-.98,.88,shoulder,.79,.85,.73,paint);
 const roofY=van?1.91:sport?1.34:1.55,roofFront=van?.43:sport?.02:.28,roofRear=van?-2.15:sport?-.83:-.83,roofW=van?.78:sport?.65:.68,baseY=sport?.95:1.02,baseW=.81;
 if(van){loft(g,-L+.08,-.4,1.88,1.91,.83,.83,.91,paint);block(g,0,1.94,-.87,1.6,.06,2.62,paint)}
 else block(g,0,roofY+.035,(roofFront+roofRear)/2,roofW*2+.05,.065,roofFront-roofRear+.1,paint);
 // Recessed glass with painted pillars; all front-door glass moves with its hinge.
 panel(g,[[-baseW,baseY,.85],[baseW,baseY,.85],[roofW,roofY,roofFront],[-roofW,roofY,roofFront]],glass);
 const pillar=(a,b,width,material,parent=g)=>{const delta=new THREE.Vector3(...b).sub(new THREE.Vector3(...a));const m=block(parent,(a[0]+b[0])/2,(a[1]+b[1])/2,(a[2]+b[2])/2,width,delta.length(),width,material);m.quaternion.setFromUnitVectors(new THREE.Vector3(0,1,0),delta.normalize());return m};
 for(const side of [-1,1])pillar([side*.83,baseY,.88],[side*(roofW+.015),roofY+.01,roofFront],.065,paint);
 if(!van){panel(g,[[-roofW,roofY,roofRear],[roofW,roofY,roofRear],[baseW,baseY,-1.16],[-baseW,baseY,-1.16]],glass);for(const side of [-1,1]){pillar([side*.83,baseY,-1.18],[side*roofW,roofY,roofRear],sport?.13:.1,paint);panel(g,[[side*.819,baseY,-1.06],[side*.819,baseY,-.42],[side*roofW,roofY-.04,-.42],[side*roofW,roofY-.04,roofRear+.06]],glass);pillar([side*.825,baseY,-.39],[side*roofW,roofY,-.39],.075,trim);block(g,side*.943,.86,-.82,.012,.025,.18,chrome);}}
 const makeFrontDoor=side=>{const door=new THREE.Group();door.position.set(side*.94,0,.64);door.userData.side=side;g.add(door);
  panel(door,[[0,.57,-.99],[0,.57,0],[0,baseY,0],[0,baseY,-.99]],paint);
  const lower=side*(baseW-.94),upper=side*(roofW-.94),end=van?-.99:-1.02;
  panel(door,[[lower,baseY+.015,end],[lower,baseY+.015,0],[upper,roofY-.035,roofFront-.68],[upper,roofY-.035,end]],glass);
  pillar([0,.56,-1.01],[0,baseY,-1.01],.019,trim,door);pillar([lower,baseY,end],[upper,roofY,end],.055,paint,door);
  block(door,side*.011,.63,-.49,.024,.055,.94,trim);block(door,side*.026,.9,-.82,.027,.035,.16,chrome);
  block(door,side*.025,baseY+.025,-.07,.075,.045,.12,trim);block(door,side*.045,baseY+.075,-.065,.12,van?.16:.105,.21,paint);block(door,side*.045,baseY+.075,-.176,.105,van?.12:.073,.012,vehicleGlass);
  return door;
 };
 g.userData.passengerDoor=makeFrontDoor(-1);g.userData.driverDoor=makeFrontDoor(1);g.userData.door=g.userData.driverDoor;
 for(const side of [-1,1]){
  block(g,side*.91,.48,0,.07,sport?.11:.07,1.36,sport?paint:trim);
  // Thin arch lips trace the cutout and remain outside the rolling tire.
  const arch=mesh(g,vehicleGeo('arch',()=>new THREE.TorusGeometry(.435,.025,4,16,Math.PI)),van?trim:paint);arch.rotation.y=Math.PI/2;arch.position.set(side*.944,.43,-1.19);
  const frontArch=arch.clone();frontArch.position.z=1.19;g.add(frontArch);(van?trimParts:paintParts).push(frontArch);
  if(van){block(g,side*.836,1.36,-1.33,.014,.019,1.52,trim);block(g,side*.838,1.39,-.53,.014,.9,.016,trim);block(g,side*.838,1.39,-2.13,.014,.9,.016,trim);block(g,side*.85,1.21,-.72,.027,.045,.22,trim);block(g,side*.854,.67,-1.27,.035,.12,1.61,trim);}
 }
 g.userData.wheels=[];g.userData.rimStyleGroups=Object.fromEntries(['default',...ALLOY_STYLES].map(id=>[id,[]]));
 for(const x of [-.91,.91])for(const z of [-1.19,1.19]){
  const tire=mesh(g,vehicleGeo('tire:'+id,()=>{const outer=van?.385:.37,inner=(sport?.265:van?.215:.235)*.95,half=(sport?.23:.19)/2;return new THREE.LatheGeometry([[inner,-half],[outer-.035,-half],[outer,-half+.025],[outer,half-.025],[outer-.035,half],[inner,half],[inner,-half]].map(p=>new THREE.Vector2(...p)),20)}),rubber);tire.rotation.z=Math.PI/2;tire.position.set(x,.4,z);g.userData.wheels.push(tire);
  // Rim children inherit wheel rotation, so later rim swaps retain the rolling rig.
  const side=Math.sign(x),halfWidth=(sport?.23:.19)/2,out=side*(halfWidth-.036),radius=sport?.265:van?.215:.235;
  const rim=mesh(tire,vehicleGeo('rim:'+id,()=>new THREE.CylinderGeometry(radius,radius,.025,20)),rimMaterial);rim.position.y=-out;
  const inset=mesh(tire,vehicleGeo('rim-inset:'+id,()=>new THREE.CylinderGeometry(radius*.78,radius*.78,.008,20)),rimDark);inset.position.y=-out-side*.016;
  const spokes=vehicleGeo('spokes:'+id,()=>{const positions=[];const count=sport?5:van?6:8;for(let n=0;n<count;n++){const angle=n*Math.PI*2/count;const r0=.045,r1=radius*.87,w=sport?.044:.025;const v=(r,t)=>[Math.cos(angle)*r-Math.sin(angle)*t,0,Math.sin(angle)*r+Math.cos(angle)*t];for(const p of [v(r0,-w),v(r1,-w),v(r1,w),v(r0,-w),v(r1,w),v(r0,w)])positions.push(...p)}const geo=new THREE.BufferGeometry();geo.setAttribute('position',new THREE.Float32BufferAttribute(positions,3));geo.computeVertexNormals();return geo});
  const spoke=mesh(tire,spokes,rimMaterial);spoke.position.y=-out-side*.022;if(side===-1)spoke.rotation.z=Math.PI;
  g.userData.rimStyleGroups.default.push(spoke);
  // Complete alloy variants include their own lip, barrel and recessed face.
  for(const style of ALLOY_STYLES){const variant=createAlloyRim(style,radius,rimMaterial,rimDark);variant.position.y=-side*(halfWidth-radius*.035-.003);variant.rotation.x=side*Math.PI/2;variant.visible=false;tire.add(variant);g.userData.rimStyleGroups[style].push(variant);variant.traverse(part=>{if(part.material===rimMaterial)rimParts.push(part);});}

  const hub=mesh(tire,vehicleGeo('hub',()=>new THREE.CylinderGeometry(.065,.065,.032,12)),rimMaterial);hub.position.y=-out-side*.018;g.userData.rimStyleGroups.default.push(rim,inset,hub);
 }
 const headlights=new THREE.MeshStandardMaterial({color:0xf7edda,emissive:0xffedcc,emissiveIntensity:.15}),taillights=new THREE.MeshStandardMaterial({color:0xa63325,emissive:0xff2812,emissiveIntensity:.1});
 g.userData.vehicleLights={headlights,taillights,front:L+.16};
 for(const front of [-1,1]){const z=front*(L+.045);block(g,0,.56,z,1.72,sport?.2:.17,.1,sport?paint:trim);block(g,0,.51,z+front*.055,sport?1.35:1.12,.1,.015,trim);block(g,0,.68,z+front*.016,.49,.145,.025,trim);sign(g,'LS · 204',0,.68,z+front*.033,.42,.1,'#28353b','#deddd2',front===1?0:Math.PI);}
 block(g,0,.82,L+.012,sport?.56:.66,sport?.115:.14,.065,trim);
 for(const y of [.79,.84])block(g,0,y,L+.05,sport?.49:.59,.015,.018,chrome);
 for(const x of [-.6,.6]){block(g,x,.82,L+.019,.45,sport?.115:.195,.067,trim);const head=block(g,x,.82,L+.055,sport?.4:.37,sport?.07:.13,.026,headlights);head.castShadow=false;
  block(g,van?Math.sign(x)*.76:x,van?1.03:.85,-L+.015,van?.16:sport?.44:.34,van?.5:sport?.12:.18,.13,trim);const tail=block(g,van?Math.sign(x)*.76:x,van?1.03:.85,-L-.055,van?.12:sport?.4:.3,van?.46:sport?.08:.145,.025,taillights);tail.castShadow=false;
  if(sport)block(g,x,.57,L+.102,.26,.105,.025,trim);
 }
 if(van){block(g,0,1.36,-L-.012,.022,1.05,.023,trim);for(const x of [-.39,.39]){block(g,x,1.52,-L-.024,.65,.48,.025,glass);block(g,x*.22,1.08,-L-.036,.075,.04,.03,chrome);}}
 else block(g,0,.97,-L+.1,1.5,.035,.11,paint);
 const spoilerGroups={none:new THREE.Group(),lip:new THREE.Group(),touring:new THREE.Group()};
 const rearY=van?1.97:.9875,rearZ=-L+(van?.23:.1),rearWidth=van?1.58:1.5;
 block(spoilerGroups.lip,0,rearY+.035,rearZ,rearWidth,.07,.16,paint);
 for(const x of [-.52,.52])block(spoilerGroups.touring,x,rearY+.075,rearZ,.09,.15,.15,trim);
 const blade=block(spoilerGroups.touring,0,rearY+.17,rearZ-.015,rearWidth+.04,.055,.27,paint);blade.rotation.x=-.13;
 for(const [style,group] of Object.entries(spoilerGroups)){group.name='Rear accessory '+style;group.visible=style==='none';g.add(group);}
 const exhaustGroups={stock:new THREE.Group(),dual:new THREE.Group(),sport:new THREE.Group()};
 for(const [style,group] of Object.entries(exhaustGroups)){
  const positions=style==='dual'?[-.55,-.40]:[-.5];
  for(const x of positions){const radius=style==='stock'?.046:.059,tip=mesh(group,vehicleGeo('exhaust:'+style,()=>new THREE.CylinderGeometry(radius,radius,.20,12,true)),chrome);tip.rotation.x=Math.PI/2;tip.position.set(x,.43,-L-.04);if(style==='sport')tip.scale.x=1.55;
   const opening=mesh(group,vehicleGeo('exhaust-opening:'+style,()=>new THREE.CircleGeometry(radius*.8,12)),rubber);opening.rotation.y=Math.PI;opening.position.set(x,.43,-L-.142);if(style==='sport')opening.scale.x=1.55;
  }group.visible=style==='stock';group.name='Exhaust '+style;g.add(group);
 }
 Object.assign(g.userData,{paintMaterial:paint,paintParts,rimMaterial,rimParts,trimMaterial:trim,trimParts,glassMaterial:glass,spoilerGroups,exhaustGroups});
 return g;
}
