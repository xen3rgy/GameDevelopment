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
 update(night){this.windows.forEach((m,i)=>m.emissiveIntensity=(i===3?.65:i===4?.32:.025)*night)}
}

const capsuleGeo=new Map();
export function createCitizen(kit,color=0x344c55,skin=0xc39c7b,variant=0,style=null){
 const {box,mat}=kit,g=new THREE.Group(),upper=new THREE.Group(),backpack=new THREE.Group(),arms=[],legs=[],elbows=[],knees=[],feet=[];g.add(upper);
 const trousers=style?.trousers??0x354350,hairColor=style?.hairColor??[0x302c29,0x5b4030,0x212629][variant%3];let headRoot=null;
 const cap=(parent,x,y,z,r,l,col,sx=1,sz=1)=>{const key=r+','+l;if(!capsuleGeo.has(key))capsuleGeo.set(key,new THREE.CapsuleGeometry(r,l,4,10));const m=new THREE.Mesh(capsuleGeo.get(key),mat(col));m.position.set(x,y,z);m.scale.set(sx,1,sz);m.castShadow=true;parent.add(m);return m};
 cap(upper,0,1.15,0,.21,.30,color,1.12,.7);cap(upper,0,.91,0,.17,.05,0x303d48,1.16,.75);cap(upper,0,1.48,0,.067,.07,skin);
 const head=new THREE.Mesh(new THREE.SphereGeometry(.145,16,12),mat(skin));head.position.set(0,1.68,.015);head.scale.set(.9,1.22,.92);head.castShadow=true;upper.add(head);
 const hair=new THREE.Mesh(new THREE.SphereGeometry(.151,16,8,0,Math.PI*2,0,Math.PI*.56),mat(hairColor));hair.position.set(0,1.725,.007);hair.scale.z=.95;upper.add(hair);
 for(const x of [-.135,.135])cap(upper,x,1.68,.01,.029,.032,skin,1,.55);
 cap(upper,0,1.67,.14,.025,.035,skin,.65,1);for(const x of [-.05,.05]){box(upper,x,1.708,.143,.025,.015,.012,0x36322e);box(upper,x,1.733,.14,.042,.008,.012,0x514036)}
 if(style){
  if(style.hair==='long')cap(upper,0,1.59,-.085,.13,.14,hairColor,1.03,.55);
  if(style.hair==='bun')cap(upper,0,1.79,-.13,.07,.04,hairColor);
  if(style.hair==='cap'){cap(upper,0,1.82,.005,.142,.025,style.accent,1,.92);box(upper,0,1.83,.11,.27,.022,.19,style.accent);}
  headRoot=new THREE.Group();headRoot.position.y=1.65;for(const part of upper.children.slice(3)){part.position.y-=1.65;headRoot.add(part);}upper.add(headRoot);
 }
 for(const x of [-.26,.26]){const arm=new THREE.Group(),elbow=new THREE.Group();arm.position.set(x,1.35,0);cap(arm,0,-.13,0,.072,.17,color);elbow.position.y=-.29;cap(elbow,0,-.09,.015,.061,.15,color);cap(elbow,0,-.22,.026,.048,.035,skin);arm.add(elbow);upper.add(arm);arms.push(arm);elbows.push(elbow)}
 for(const x of [-.115,.115]){const leg=new THREE.Group(),knee=new THREE.Group(),foot=new THREE.Group();leg.position.set(x,.9,0);cap(leg,0,-.19,0,.087,.25,trousers);knee.position.y=-.38;cap(knee,0,-.18,0,.069,.25,trousers);foot.position.set(0,-.405,0);box(foot,0,0,.04,.17,.095,.30,style?.shoes??0x252c30);box(foot,0,-.05,.04,.18,.03,.32,0xb5b3a4);box(foot,0,.052,.10,.12,.018,.14,0x667277);knee.add(foot);leg.add(knee);g.add(leg);legs.push(leg);knees.push(knee);feet.push(foot)}
 box(upper,0,1.17,.158,.017,.49,.012,0xa5a58f);for(const x of [-.14,.14])box(upper,x,1.14,.15,.105,.12,.025,0x40585c);
 if(variant===0){upper.add(backpack);cap(backpack,0,1.17,-.21,.17,.18,0x827a59,1,.65);box(backpack,0,1.12,-.323,.22,.17,.055,0xa0926e);for(const x of [-.16,.16])box(backpack,x,1.24,.153,.039,.37,.024,0x8f8565)}
 else if(variant%3===0)box(upper,0,1.43,.03,.35,.08,.28,0xb49562);
 if(style?.coat){cap(upper,0,1.02,-.018,.21,.25,color,1.16,.73);box(upper,0,1.33,.165,.1,.18,.035,style.accent);}
 if(style?.bag){box(elbows[0],0,-.40,.026,.24,.31,.15,style.accent);for(const x of [-.065,.065])box(elbows[0],x,-.22,.026,.016,.17,.022,style.accent);}
 upper.position.y=.9;for(const child of upper.children)child.position.y-=.9;g.userData={arms,legs,upper,elbows,knees,feet,backpack,headRoot,style};mergeRigidParts(g);return g;
}

export function createCar(kit,id='car',color=0x506d78){
 const {box,cylinder,mat,sign}=kit,g=new THREE.Group(),length=id==='van'?4.8:id==='sport'?4.1:3.8,paint=new THREE.MeshStandardMaterial({color,roughness:.32,metalness:.45});
 // Rounded body panels and a sloped cabin give the cars a readable silhouette.
 const shape=new THREE.Shape();shape.moveTo(-.79,-.26);shape.lineTo(.79,-.26);shape.quadraticCurveTo(.9,-.26,.9,-.15);shape.lineTo(.9,.15);shape.quadraticCurveTo(.9,.26,.79,.26);shape.lineTo(-.79,.26);shape.quadraticCurveTo(-.9,.26,-.9,.15);shape.lineTo(-.9,-.15);shape.quadraticCurveTo(-.9,-.26,-.79,-.26);
 const geo=new THREE.ExtrudeGeometry(shape,{depth:length-.16,bevelEnabled:true,bevelThickness:.08,bevelSize:.04,bevelSegments:2,steps:1,curveSegments:4});geo.translate(0,.69,-length/2+.08);const body=new THREE.Mesh(geo,paint);body.castShadow=true;body.receiveShadow=true;g.add(body);
 const cabin=new THREE.BufferGeometry(),verts=[[-.78,.92,-1.15],[.78,.92,-1.15],[.78,.92,1.13],[-.78,.92,1.13],[-.65,1.53,-.85],[.65,1.53,-.85],[.65,1.53,.57],[-.65,1.53,.57]],tri=[];
 for(const [a,b,c,d] of [[0,1,5,4],[1,2,6,5],[2,3,7,6],[3,0,4,7]])for(const i of [a,b,c,a,c,d])tri.push(...verts[i]);cabin.setAttribute('position',new THREE.Float32BufferAttribute(tri,3));cabin.computeVertexNormals();const glass=new THREE.Mesh(cabin,new THREE.MeshStandardMaterial({color:0x273e4b,roughness:.13,metalness:.6,side:THREE.DoubleSide}));glass.castShadow=true;g.add(glass);
 box(g,0,1.55,-.14,1.37,.065,1.46,0,paint);box(g,0,.99,1.28,1.72,.06,.72,0,paint);box(g,0,.88,-length/2+.2,1.6,.09,.35,0,paint);
 if(id==='van'){box(g,0,1.15,-1,1.72,1.28,2.3,0,paint);box(g,0,1.39,-2.17,1.47,.79,.04,0x768b8f);box(g,0,1.38,-2.205,.05,1.13,.025,0x43575a)}
 for(const x of [-.8,.8]){box(g,x,1.23,-.16,.065,.52,.09,0,paint);box(g,x*1.09,.88,-.3,.025,.045,.20,0xb8c0b9);box(g,x*1.15,1.04,.64,.19,.1,.18,0,paint);box(g,x,.52,0,.055,.07,length-.4,0x354247)}
 g.userData.wheels=[];
 for(const x of [-.91,.91])for(const z of [-1.19,1.19]){const tire=new THREE.Mesh(new THREE.CylinderGeometry(.37,.37,.19,20),mat(0x22282a,.96));tire.rotation.z=Math.PI/2;tire.position.set(x,.4,z);tire.castShadow=true;g.add(tire);g.userData.wheels.push(tire);const rim=cylinder(g,x+Math.sign(x)*.11,.4,z,.22,.026,0xa6b1b0);rim.rotation.z=Math.PI/2;const hub=cylinder(g,x+Math.sign(x)*.129,.4,z,.078,.025,0x3e5158);hub.rotation.z=Math.PI/2;}
 const makeFrontDoor=side=>{const door=new THREE.Group();door.position.set(side*.94,0,.64);door.userData.side=side;box(door,0,.85,-.47,.045,.39,.94,0,paint);box(door,0,1.21,-.47,.035,.32,.87,0x314750);box(door,side*.035,.9,-.69,.04,.04,.18,0xb8c0b9);g.add(door);return door};
 const passengerDoor=makeFrontDoor(-1),driverDoor=makeFrontDoor(1);g.userData.passengerDoor=passengerDoor;g.userData.driverDoor=driverDoor;g.userData.door=driverDoor;
 const headlights=new THREE.MeshStandardMaterial({color:0xf7edda,emissive:0xffedcc,emissiveIntensity:.15}),taillights=new THREE.MeshStandardMaterial({color:0xa63325,emissive:0xff2812,emissiveIntensity:.1});
 g.userData.vehicleLights={headlights,taillights,front:length/2+.16};
 for(const x of [-.6,.6]){const head=box(g,x,.8,length/2+.055,.39,.13,.025,0,headlights),tail=box(g,x,.83,-length/2-.055,.29,.13,.025,0,taillights);head.castShadow=tail.castShadow=false;}
 box(g,0,.55,length/2+.05,1.2,.18,.05,0x2b3d42);for(let y of [.5,.55,.6])box(g,0,y,length/2+.085,.9,.022,.02,0x88999b);sign(g,'LS · 204',0,.69,length/2+.09,.42,.1,'#28353b','#deddd2');sign(g,'LS · 204',0,.65,-length/2-.09,.42,.1,'#28353b','#deddd2',Math.PI);
 return g;
}

// Merge rigid parts within each animated joint; limbs remain separately movable.
function mergeRigidParts(group){
 for(const child of [...group.children])if(child.isGroup)mergeRigidParts(child);
 const buckets=new Map();for(const m of group.children){if(!m.isMesh||Array.isArray(m.material))continue;if(!buckets.has(m.material))buckets.set(m.material,[]);buckets.get(m.material).push(m)}
 for(const [material,meshes] of buckets){if(meshes.length<2)continue;const positions=[],normals=[],uvs=[];
  for(const m of meshes){m.updateMatrix();const geom=m.geometry.index?m.geometry.toNonIndexed():m.geometry.clone();geom.applyMatrix4(m.matrix);positions.push(...geom.attributes.position.array);normals.push(...geom.attributes.normal.array);uvs.push(...geom.attributes.uv.array);geom.dispose();group.remove(m)}
  const geo=new THREE.BufferGeometry();geo.setAttribute('position',new THREE.Float32BufferAttribute(positions,3));geo.setAttribute('normal',new THREE.Float32BufferAttribute(normals,3));geo.setAttribute('uv',new THREE.Float32BufferAttribute(uvs,2));const merged=new THREE.Mesh(geo,material);merged.castShadow=true;merged.receiveShadow=true;group.add(merged);
 }
}
