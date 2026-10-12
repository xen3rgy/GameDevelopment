import {westFenceMesh,buildWestIndustry,westContainer,westBusShelter,buildWestEdges} from './west-details.js?v=0.8.1';
import {buildWestCourtyards} from './west-courtyards.js?v=0.8.1';
import {addWestTree,westShrub} from './west-vegetation.js?v=0.8.1';
import * as THREE from './vendor/three.module.js';
import {WEST_BOUNDS,WEST_ROADS,WEST_PATHS,WEST_BUILDINGS,WEST_BUILDING_FIXTURES,WEST_BINS,westPathHeight} from './west-edge-layout.js?v=0.8.1';
import {expansionBuilding} from './district-architecture.js?v=0.8.1';
import {districtMaterial,fitFacadeUV,districtWindowSet} from './district-materials.js?v=0.8.1';
import {surfaceMaterial} from './atmosphere.js?v=0.8.1';
import {groundHeight} from './spatial.js?v=0.8.1';
import {westBox,westGround,westMaterial,westWallArt,westFacadeMaterial,batchWestGeometry} from './west-materials.js?v=0.8.1';
import {dressWest} from './west-streetscape.js?v=0.8.1';
import {westParkedColliders} from './west-parked.js?v=0.8.1';
import {createCar} from './art.js?v=0.8.1';

const gable=new THREE.BufferGeometry();
gable.setAttribute('position',new THREE.Float32BufferAttribute([0,0,-.5,0,0,.5,0,1,0,0,0,.5,0,0,-.5,0,1,0],3));gable.computeVertexNormals();
let canopyMaterial;
function wornCanopyMaterial(){if(canopyMaterial)return canopyMaterial;let map=null;if(typeof document!=='undefined'){const c=document.createElement('canvas');c.width=c.height=256;const a=c.getContext('2d');a.fillStyle='#496149';a.fillRect(0,0,256,256);for(let x=0;x<256;x+=64){a.fillStyle='#b5ae8e';a.fillRect(x,0,21,256);}for(let n=0;n<90;n++){a.fillStyle='#343c2b19';a.fillRect(n*73%256,n*91%256,2,13);}map=new THREE.CanvasTexture(c);map.colorSpace=THREE.SRGBColorSpace;}canopyMaterial=new THREE.MeshStandardMaterial({map,color:map?0xffffff:0x68705a,roughness:1});return canopyMaterial;}
const wheel=new THREE.TorusGeometry(.32,.025,5,16),surfaceCache=new Map();
const surface=(kind,w,d)=>{const key=kind+':'+w.toFixed(2)+':'+d.toFixed(2);if(!surfaceCache.has(key))surfaceCache.set(key,typeof document==='undefined'?new THREE.MeshStandardMaterial({color:kind==='grass'?0x758263:0x9b9a89,roughness:.97}):surfaceMaterial(kind,w,d));return surfaceCache.get(key);};
const roadFootprints=WEST_ROADS.flatMap(r=>{const width=r.pavementWidth??7;return [r,...[-1,1].map(side=>r.w>r.d?{x:r.x,z:r.z+side*(r.d/2+width/2),w:r.w,d:width}:{x:r.x+side*(r.w/2+width/2),z:r.z,w:width,d:r.d})];});
// Subtract the street footprint so forecourts never fight coplanar road/shoulder meshes.
export function westPathPatches(){
 const done=[],out=[];
 for(const path of WEST_PATHS){let pieces=[{...path,support:path}];for(const cut of [...roadFootprints,...WEST_BUILDINGS.filter(b=>b.style!=='shelter'),...done]){const next=[];for(const p of pieces){const ax=p.x-p.w/2,bx=p.x+p.w/2,az=p.z-p.d/2,bz=p.z+p.d/2,x=Math.max(ax,cut.x-cut.w/2),X=Math.min(bx,cut.x+cut.w/2),z=Math.max(az,cut.z-cut.d/2),Z=Math.min(bz,cut.z+cut.d/2);if(x>=X||z>=Z){next.push(p);continue;}for(const [a,b,c,d] of [[ax,x,az,bz],[X,bx,az,bz],[x,X,az,z],[x,X,Z,bz]])if(b-a>.001&&d-c>.001)next.push({...p,x:(a+b)/2,z:(c+d)/2,w:b-a,d:d-c});}pieces=next;}out.push(...pieces);done.push(path);}
 return out;
}
export function westPathEdges(){
 const out=[],cuts=[...roadFootprints,...WEST_PATHS,...WEST_BUILDINGS];
 for(const p of westPathPatches()){
  if(p.kind==='dock')continue;
  for(const side of [-1,1])for(const axis of ['x','z']){const alongX=axis==='z',edge=(alongX?p.z:p.x)+side*(alongX?p.d:p.w)/2,min=(alongX?p.x:p.z)-(alongX?p.w:p.d)/2,max=(alongX?p.x:p.z)+(alongX?p.w:p.d)/2,stops=[min,max];
   for(const c of cuts){const perpendicular=alongX?c.z:c.x,size=alongX?c.d:c.w;if(Math.abs(edge-perpendicular)>size/2+.04)continue;for(const end of [(alongX?c.x:c.z)-(alongX?c.w:c.d)/2,(alongX?c.x:c.z)+(alongX?c.w:c.d)/2])if(end>min+.001&&end<max-.001)stops.push(end);}
   stops.sort((a,b)=>a-b);
   for(let i=1;i<stops.length;i++){const a=stops[i-1],b=stops[i];if(b-a<.001)continue;const point=t=>({x:alongX?t:edge,z:alongX?edge:t}),middle=point((a+b)/2),neighbor=groundHeight(middle.x+(alongX?0:side*.025),middle.z+(alongX?side*.025:0)),top=westPathHeight(p,middle.x,middle.z);if(top-neighbor<.04)continue;
    const A=point(a),B=point(b);out.push({kind:p.kind==='gravel'?'yard':'concrete',a:{...A,y:westPathHeight(p,A.x,A.z)},b:{...B,y:westPathHeight(p,B.x,B.z)},bottom:neighbor-.015,side,alongX});
   }
  }
 }return out;
}

export class WestEdgeScene{
 constructor(world,kit,{batch=true}={}){
  this.world=world;this.age=1;this.blocks=new Map();this.root=new THREE.Group();this.root.name='West · Bruchfeld / Altgleis / Westbruch';world.exterior.add(this.root);
  const {box,cylinder,sphere,sign,mat}=kit;
  const group=(x,z)=>{const key=Math.floor(x/70)+':'+Math.floor(z/70);if(!this.blocks.has(key)){const g=new THREE.Group();g.name='West block '+key;g.userData.center={x:Math.floor(x/70)*70+35,z:Math.floor(z/70)*70+35};this.blocks.set(key,g);this.root.add(g);}return this.blocks.get(key);};
  const solid=(x,z,w,d,h)=>world.colliders.push({x,z,w:w/2,d:d/2,h});
  const part=(g,geometry,material,x,y,z,sx=1,sy=1,sz=1)=>{const mesh=new THREE.Mesh(geometry,material);mesh.position.set(x,y,z);mesh.scale.set(sx,sy,sz);mesh.castShadow=true;mesh.receiveShadow=true;mesh.userData.staticBatch=true;g.add(mesh);return mesh;};
  const line=(g,a,b,width,color)=>{const dx=b.x-a.x,dy=b.y-a.y,dz=b.z-a.z,length=Math.hypot(dx,dy,dz);const m=box(g,(a.x+b.x)/2,(a.y+b.y)/2,(a.z+b.z)/2,width,length,width,color);m.quaternion.setFromUnitVectors(new THREE.Vector3(0,1,0),new THREE.Vector3(dx,dy,dz).normalize());return m;};
  // Separate western apron: its east edge meets the previous landscape exactly.
  box(this.root,(-550-260)/2,-.46,0,290,.6,510,0,surface('grass',290,510)).castShadow=false;
  for(const p of westPathPatches()){
   if(p.kind==='dock')continue; // The dock's concrete top is already its support face.
   const g=group(p.x,p.z);westGround(g,p.kind==='gravel'?'yard':'paving',p.x,p.z,p.w,p.d,p.kind==='paving'?p.height:0,p.kind!=='paving'?(x,z)=>westPathHeight(p,x,z):null);
  }
  // Split skirts at every adjoining surface. No full-length wall may run across
  // a connected path, and raised driveway sides are closed down to the soil.
  for(const e of westPathEdges()){const {a,b,bottom,alongX,side}=e,vertices=[a.x,a.y,a.z,b.x,b.y,b.z,b.x,bottom,b.z,a.x,bottom,a.z],geo=new THREE.BufferGeometry();geo.setAttribute('position',new THREE.Float32BufferAttribute(vertices,3));geo.setAttribute('uv',new THREE.Float32BufferAttribute([0,a.y/4,Math.hypot(b.x-a.x,b.z-a.z)/4,b.y/4,Math.hypot(b.x-a.x,b.z-a.z)/4,bottom/4,0,bottom/4],2));geo.setIndex((alongX?side>0:side<0)?[0,2,1,0,3,2]:[0,1,2,0,2,3]);geo.computeVertexNormals();part(group((a.x+b.x)/2,(a.z+b.z)/2),geo,westMaterial(e.kind),0,0,0);}
  world.colliders.push(...WEST_BUILDING_FIXTURES);
  const windows=districtWindowSet(world.art.windows,-300,0);
  const facade=(g,x,z,w,h,angle,seed,door=false)=>{const f=new THREE.Group();f.position.set(x,0,z);f.rotation.y=angle;g.add(f);const cols=Math.max(2,Math.floor(w/4)),rows=Math.max(1,Math.floor(h/3)),pitch=(w-1)/cols,ww=Math.min(1.55,pitch-1.25);
   for(let row=0;row<rows;row++)for(let col=0;col<cols;col++){const xx=(col-(cols-1)/2)*pitch,yy=1.9+row*2.8;if(door&&row===0&&Math.abs(xx)<.525+ww/2+.1)continue;
    box(f,xx,yy,.065,ww,1.72,.10,0xd6cfba);box(f,xx,yy,.13,ww-.21,1.5,.035,0,windows[(col+row+seed)%5]||mat(0x536665));box(f,xx,yy,.16,.055,1.51,.035,0xaaa99a);box(f,xx,yy-.25,.16,ww-.21,.055,.035,0xaaa99a);box(f,xx,yy-.93,.21,ww+.25,.13,.34,0xb8b5a4);
   }
   for(const side of [-1,1])cylinder(f,side*(w/2-.2),h/2,.15,.045,h,0x667066);return f;
  };
  const roof=(g,b,rise=1.6)=>{const len=Math.hypot(b.d/2+.3,rise),a=Math.atan2(rise,b.d/2+.3);for(const side of [-1,1]){const m=westBox(g,'roof',b.x,b.h+rise/2,b.z+side*(b.d/4+.15),b.w+.6,.18,len);m.rotation.x=side*a;}for(const side of [-1,1]){part(g,gable,mat(0xb5aa91),b.x+side*b.w/2,b.h,b.z,1,rise,b.d);box(g,b.x,b.h,b.z+side*(b.d/2+.28),b.w+.6,.14,.16,0x59625c);}box(g,b.x+b.w*.25,b.h+rise+.55,b.z, .8,1.4,.7,0x8c7961);};
  for(const b of WEST_BUILDINGS){const g=group(b.x,b.z),{x,z,w,d,h}=b;
   if(b.style==='residential'||b.style==='industrial'){
    const skinMaterial=b.style==='residential'?(b.seed%3===0?westFacadeMaterial(w,h,0xb9a18d,'brick'):westFacadeMaterial(w,h,[0xd2c6af,0xbec4b3,0xc5b7a6][b.seed%3])):westFacadeMaterial(w,h,0xbdb5a4,'brick');
    expansionBuilding(g,kit,{...b,skinMaterial,customFrontage:true},world.art.windows);
    // Scuffed lower courses and masonry repairs stay on the facade, below windows.
    for(const side of [-1,1]){box(g,x,1.08,z+side*(d/2+.015),w-.5,.5,.035,0x8e8978);for(let n=0;n<3;n++)box(g,x-w*.34+n*w*.31,.6+n%2*.15,z+side*(d/2+.037),1.3+n*.4,.24,.02,[0x9f927b,0x827b67,0xa69e87][n]);}
    if(b.style==='residential'){const face=b.face;for(const dx of [-w*.3,w*.3]){box(g,x+dx,3.08,z+face*(d/2+.25),2.9,.16,.45,0x737c6e);box(g,x+dx,2.88,z+face*(d/2+.24),2.8,.23,.06,0x526758);}sign(g,(x<-425?'BRUCHFELD':x<-335?'ALTGLEIS':'WESTBRUCH')+' · '+String(b.seed+3),x,2.65,z+face*(d/2+.24),3,.22,'#e4dbc5','#525e53',face<0?Math.PI:0);}
   }else if(b.style==='townhouse'){
    fitFacadeUV(box(g,x,h/2,z,w,h,d,0,westFacadeMaterial(w,h,b.seed%2?0xa59d84:0xa88d77)),w,d);box(g,x,.36,z,w+.2,.72,d+.2,0x777a68);
    facade(g,x,z+d/2,w,h,0,b.seed,b.face===1);facade(g,x,z-d/2,w,h,Math.PI,b.seed,b.face===-1);facade(g,x+w/2,z,d,h,Math.PI/2,b.seed);facade(g,x-w/2,z,d,h,-Math.PI/2,b.seed);roof(g,b);
    const f=new THREE.Group();f.position.set(x,.14,z+b.face*d/2);f.rotation.y=b.face<0?Math.PI:0;g.add(f);box(f,0,1.08,.22,1.05,2.16,.15,0x48534a);box(f,.28,1,.31,.045,.24,.05,0xc0b79f);box(f,0,2.32,.55,1.9,.14,1.15,0x606c5d);for(const side of [-1,1])box(f,side*.83,1.15,1,.07,2.3,.07,0x8e8d74);
    world.overheadColliders.push({x,z:z+b.face*(d/2+.55),w:.95,d:.575,minY:2.39,h:2.53,westCanopy:true});
   }else if(b.style==='shed'){
    fitFacadeUV(box(g,x,h/2,z,w,h,d,0,westFacadeMaterial(w,h,0x9b9179,'brick')),w,d);box(g,x,h+.08,z,w+.4,.16,d+.4,0x515e54);const face=b.face;for(const dx of [-w*.26,w*.26]){box(g,x+dx,1.4,z+face*(d/2+.04),w*.43,2.8,.08,0x657366);for(let yy=.4;yy<2.8;yy+=.35)box(g,x+dx,yy,z+face*(d/2+.09),w*.41,.025,.025,0x8b9380);}
   }else if(b.style==='kiosk'){
    fitFacadeUV(box(g,x,h/2,z,w,h,d,0,westFacadeMaterial(w,h,0xc6b69a,'brick')),w,d);box(g,x,h+.06,z,w+.5,.2,d+.5,0x48574a);const front=z+d/2;
    // Apartments over the shop join the kiosk to the residential street wall.
    for(const [xx,zz,width,rot] of [[x,front,w,0],[x,z-d/2,w,Math.PI],[x+w/2,z,d,Math.PI/2],[x-w/2,z,d,-Math.PI/2]]){const upper=facade(g,xx,zz,width,h-3.6,rot,b.seed);upper.position.y=3.6;}
    box(g,x,1.65,front+.06,w-1,2.5,.08,0x405d4c);box(g,x-.8,1.65,front+.115,w-3,2.22,.04,0,windows[1]||mat(0x6a806e));
    for(const dx of [-4.7,-2.3,.1,2.5])box(g,x+dx,1.65,front+.16,.075,2.45,.09,0x9eaa88);
    box(g,x+4.05,1.35,front+.17,1.5,2.7,.09,0x344b3e);box(g,x+4.05,1.7,front+.23,1.28,1.4,.035,0,windows[2]||mat(0x6a806e));box(g,x+4.57,1.05,front+.26,.035,.3,.04,0xc5c4a8);
    for(const yy of [.78,1.37]){box(g,x-1,yy,front+.18,7,.07,.12,0xb6a57f);for(let i=0;i<11;i++){const px=x-4.15+i*.62;if(i%3===0)box(g,px,yy+.15,front+.20,.23,.24,.08,0xb7a67b);else{cylinder(g,px,yy+.12,front+.2,.062,.24,i%2?0x596b45:0x937347);cylinder(g,px,yy+.265,front+.2,.025,.05,0x78806a);}}}
    box(g,x,3.27,front+.2,w+.25,.65,.2,0x33533e);sign(g,'PFAND & SPAR',x,3.31,front+.31,9.9,.46,'#ede3bd','#33533e');

    for(const dx of [-w*.32,0,w*.32]){const awning=box(g,x+dx,2.72,front+.9,w*.30,.07,1.6,0,wornCanopyMaterial());awning.rotation.x=.10;box(g,x+dx,2.59,front+1.69,w*.30,.17,.04,0,wornCanopyMaterial());for(const side of [-1,1])line(g,{x:x+dx+side*w*.13,y:2.65,z:front+.2},{x:x+dx+side*w*.13,y:2.6,z:front+1.55},.024,0x6f7866);world.overheadColliders.push({x:x+dx,z:front+.9,w:w*.15,d:.8,minY:2.50,h:2.835,westCanopy:true});}
    for(const dx of [-3.6,3.6]){box(g,x+dx,2.93,front+.5,.75,.06,.23,0xe8d6aa);world.atmosphere.registerLamp(x+dx,front+.7,null,{height:2.9,power:36,poolSize:4,distance:9});}
    sign(g,'25 CT / FLASCHE',x-8,1.3,front+.08,2.1,.25,'#e4debf','#3c5946');
    box(g,x-8,.63,front-.12,1.55,1.1,1.2,0x3a614b);box(g,x-8,1.23,front-.12,1.65,.15,1.3,0x6f8764);solid(x-8,front-.12,1.7,1.35,1.4);
   }else if(b.style==='shelter'){
    // An open lean-to, not a locked room: approaching from the south remains clear.
    box(g,x,.10,z,w,.12,d,0x8a806b);westBox(g,'wood',x,1.55,z+d/2,w,2.8,.16);solid(x,z+d/2,w,.16,2.95);
    westBox(g,'wood',x-w/2,1.55,z,.16,2.8,d);solid(x-w/2,z,.16,d,2.95);
    for(const dx of [-w/2,w/2]){box(g,x+dx,1.6,z-d/2,.13,3,.13,0x737764);solid(x+dx,z-d/2,.13,.13,3.1);}
    const top=westBox(g,'steel',x,3.04,z,w+.5,.15,d+.5);top.rotation.x=.07;world.overheadColliders.push({x,z,w:w/2+.25,d:d/2+.25,minY:2.78,h:3.3});
    for(const dx of [-1.25,.35])westBox(g,'wood',x+dx,.215,z+.6,.15,.11,2.2);
    for(let n=0;n<4;n++)box(g,x-1.5+n*.7,.31,z+.6,.6,.08,2.2,0x9b8664);box(g,x-.4,.44,z+.6,1.5,.18,2.2,0x727b74);box(g,x-.4,.56,z+.9,1.4,.16,1.4,0x3b4d4d);box(g,x-.4,.57,z-.1,1.1,.16,.4,0xb4ae94);
    solid(x-.4,z+.6,1.55,2.25,.7);box(g,x+2,.50,z+1.45,1,.68,.8,0x8f7957);box(g,x+2,.87,z+1.45,1.1,.07,.85,0xb1a17d);solid(x+2,z+1.45,1.1,.85,.91);
    sign(g,'BRUCHFELD',x+1.5,2.12,z+d/2-.095,2.1,.25,'#c8c4a8','#5b6554',Math.PI);world.atmosphere.registerLamp(x+.8,z+.8,null,{height:2.4,power:20,poolSize:3,distance:7});
   }
  }
  const fence=(x,z,length,alongX=true,wire=false)=>{const g=group(x,z);for(let n=-length/2;n<=length/2;n+=3){const px=x+(alongX?n:0),pz=z+(alongX?0:n);box(g,px,.7,pz,.10,1.5,.10,wire?0x6e7868:0x817b5d);}for(const yy of [.4,1.05])box(g,x,yy,z,alongX?length:.055,wire?.026:.12,alongX?.055:length,wire?0x7b8274:0x8b8263);solid(x,z,alongX?length:.12,alongX?.12:length,1.55);westFenceMesh(g,x,.76,z,length,1.26,alongX?0:Math.PI/2);};
  // Side gates face the lanes; the main east–west road has no fence across it.
  for(const [a,b] of [[-532.5,-524.2],[-521.8,-510.7],[-508.3,-507.5]])fence((a+b)/2,-12,b-a);
  fence(-535.7,-26,27,false);fence(-458,15,3);fence(-446,15,11);fence(-441,27,23,false);
  for(const [a,b] of [[-524.5,-515.7],[-512.3,-502]])fence((a+b)/2,69,b-a);
  fence(-400,-61,32,true,true);fence(-419,-44,27,false,true);fence(-354,-39,19,false,true);
  const rail=(ax,az,bx,bz)=>{const g=group((ax+bx)/2,(az+bz)/2),dx=bx-ax,dz=bz-az,len=Math.hypot(dx,dz),nx=-dz/len,nz=dx/len;for(const side of [-1,1])line(g,{x:ax+nx*.72*side,y:.045,z:az+nz*.72*side},{x:bx+nx*.72*side,y:.045,z:bz+nz*.72*side},.095,0x6c6a58);for(let s=0;s<len;s+=1.45){const m=box(g,ax+dx*s/len,-.025,az+dz*s/len,.22,.08,2.5,0x6b6150);m.rotation.y=-Math.atan2(dz,dx);}return g;};
  rail(-416,-55,-344,-55);rail(-401,-55,-350,-68);
  // A disused railway embankment gives the west entrance a real underpass.
  const bridge=group(-422,0);
  westBox(bridge,'concrete',-422,5.58,0,8,.65,22);world.overheadColliders.push({x:-422,z:0,w:4,d:11,minY:5.25,h:6.2});
  for(const side of [-1,1]){
   westBox(bridge,'concrete',-422,2.59,side*10,8,5.28,1.8);solid(-422,side*10,8,1.8,5.25);
   const vertices=[-426,-.05,side*11,-418,-.05,side*11,-418,5.9,side*11,-426,5.9,side*11,-426,-.05,side*59,-418,-.05,side*59];
   const geometry=new THREE.BufferGeometry();geometry.setAttribute('position',new THREE.Float32BufferAttribute(vertices,3));geometry.setIndex([3,2,5,3,5,4,0,3,4,1,5,2]);geometry.computeVertexNormals();geometry.setAttribute('uv',new THREE.Float32BufferAttribute(vertices.flatMap((v,i)=>i%3===0?[v/4,vertices[i+2]/4]:[]),2));
   const bank=new THREE.Mesh(geometry,westMaterial('ballast'));bank.receiveShadow=true;bridge.add(bank);
   for(let n=0;n<12;n++)world.colliders.push({x:-422,z:side*(13+n*4),w:4,d:2,h:5.9-5.95*n/12,westBank:true});
   for(const x of [-422.72,-421.28])line(bridge,{x,y:5.97,z:side*11},{x,y:.07,z:side*59},.09,0x6c6a58);
  }
  for(const x of [-422.72,-421.28])box(bridge,x,5.97,0,.09,.09,22,0x6c6a58);
  for(let z=-10;z<=10;z+=1.5)box(bridge,-422,5.94,z,2.3,.07,.22,0x6c6150);
  for(const x of [-425.8,-418.2]){box(bridge,x,6.4,0,.06,.06,22,0x657061);for(let z=-10;z<=10;z+=3)box(bridge,x,6.13,z,.05,.55,.05,0x657061);}
  world.atmosphere.registerLamp(-422,0,null,{height:4.9,power:50,poolSize:7,distance:13});
  for(const [x,z] of [[-416,-55],[-344,-55],[-350,-68]]){const g=group(x,z);box(g,x,.55,z,.3,.18,2.8,0x6e5848);for(const dz of [-.9,.9])box(g,x,.22,z+dz,.13,.5,.13,0x7e7158);}
  const railg=group(-377,-54);westBox(railg,'concrete',-399,.27,-47.6,14,.60,2.3);world.overheadColliders.push({x:-399,z:-47.6,w:7,d:1.15,minY:-.03,h:.57,westDock:true});
  const rampSides=new THREE.BufferGeometry(),rampVertices=[];for(const side of [-1,1]){const z=-47.6+side*1.15,points=[[-392,-.03,z],[-392,.57,z],[-389,-.03,z]];for(const n of side>0?[0,2,1]:[0,1,2])rampVertices.push(...points[n]);}rampSides.setAttribute('position',new THREE.Float32BufferAttribute(rampVertices,3));rampSides.setAttribute('uv',new THREE.Float32BufferAttribute(rampVertices.flatMap((n,i)=>i%3===0?[n/4,rampVertices[i+1]/4]:[]),2));rampSides.computeVertexNormals();part(railg,rampSides,westMaterial('concrete'),0,0,0);
  for(const x of [-404,-399,-394]){westBox(railg,'steel',x,.29,-46.4,.25,.5,.13);sign(railg,'LADERAMPE',x,.28,-46.32,1.8,.18,'#cfc6a8','#545d53');}
  for(const [x,z,color] of [[-373,-40,0x68786b],[-409,-25,0x886f56]])westContainer(world,kit,group(x,z),x,z,color);
  const bench=(x,z,angle=0)=>{const g=new THREE.Group();g.position.set(x,groundHeight(x,z),z);g.rotation.y=angle;group(x,z).add(g);for(let n=0;n<4;n++)box(g,0,.47,-.26+n*.16,2.2,.08,.13,0x8a8166);for(let n=0;n<3;n++)box(g,0,.68+n*.16,.3,2.2,.12,.08,0x8a8166);for(const dx of [-.85,.85]){box(g,dx,.22,0,.08,.44,.6,0x566250);box(g,dx,.75,.3,.07,.7,.07,0x566250);}solid(x,z,Math.abs(Math.cos(angle))*2.3+Math.abs(Math.sin(angle))*.85,Math.abs(Math.sin(angle))*2.3+Math.abs(Math.cos(angle))*.85,1.2);};
  bench(-439,-11.9,Math.PI);bench(-360,12.5);bench(-291,36);bench(-280,36);
  // A modest courtyard: planting, bench, laundry and a little swing, clear central aisle.
  const court=group(-291,32);for(const x of [-287,-282]){box(court,x,1.35,41,.1,2.7,.1,0x6e7866);solid(x,41,.1,.1,2.8);}line(court,{x:-287,y:2.55,z:41},{x:-282,y:2.5,z:41},.025,0x89927d);for(let n=0;n<4;n++)box(court,-286.4+n,2.02,41,.62,.93,.035,[0xa4ad94,0xb4afa0,0x647e76,0x9f947d][n]);
  for(const x of [-302,-298])for(const dz of [-1.2,1.2])line(court,{x,y:.3,z:35+dz},{x,y:3.1,z:35},.08,0x6d6352);box(court,-300,3.1,35,4.4,.12,.12,0x6d6352);for(const x of [-300.55,-299.45])line(court,{x,y:3,z:35},{x,y:.82,z:35},.025,0x969885);box(court,-300,.8,35,1.3,.11,.42,0x7d785e);solid(-300,35,4.5,2.6,3.3);
  // Searchable receptacles use exactly the gameplay IDs/fixture positions.
  for(const [i,p] of WEST_BINS.entries()){const {x,z}=p.fixture,g=group(x,z),y=groundHeight(x,z);box(g,x,y+.53,z,.72,1.06,.64,0x435c4a);box(g,x,y+1.1,z,.82,.13,.73,0x66785a);box(g,x,y+.75,z+.33,.36,.11,.035,0x203c31);for(const dx of [-.27,.27])cylinder(g,x+dx,y+.13,z+.26,.10,.13,0x323d32);solid(x,z,.86,.76,1.35);for(let n=0;n<3;n++){const litter=box(g,x+.7+n*.19,y+.008,z-.2+n*.17,.22,.008,.15,[0xc5bda4,0x9c9c82,0x8e826a][n]);litter.rotation.y=n*.9;litter.castShadow=false;}}
  const pallet=(x,z)=>{const g=group(x,z),y=groundHeight(x,z);for(let n=0;n<4;n++)box(g,x-.48+n*.32,y+.13,z,.27,.1,1.1,0x978467);for(const dz of [-.4,.4])box(g,x,y+.055,z+dz,1.3,.1,.11,0x7a7256);};
  pallet(-410,-30);pallet(-407,-30);pallet(-470,-25);pallet(-504,34);
  for(const [x,z] of [[-456.5,-27],[-410,-20],[-492.5,45.9]]){const g=group(x,z),y=groundHeight(x,z);cylinder(g,x,y+.5,z,.36,1,0x797451);for(const yy of [.16,.78])cylinder(g,x,y+yy,z,.375,.07,0x4b6050);solid(x,z,.75,.75,y+1);}
  for(const [x,z] of [[-522,-15],[-452,17]]){const g=group(x,z),y=groundHeight(x,z);for(const dx of [-.65,.65])part(g,wheel,mat(0x303f37),x+dx,y+.35,z);for(const [a,b] of [[[-.65,.35],[0,.85]],[[0,.85],[.65,.35]],[[-.65,.35],[.2,.35]],[[.2,.35],[0,.85]]])line(g,{x:x+a[0],y:y+a[1],z},{x:x+b[0],y:y+b[1],z},.035,0x819178);box(g,x,y+.9,z,.32,.05,.16,0x3f4d42);}
  // Utility spans sag naturally. Three wires per span, no hanging textures.
  const poleXs=[-528,-489,-450,-412,-374,-337];for(const [i,x] of poleXs.entries()){const z=8.3,g=group(x,z);cylinder(g,x,3.3,z,.09,6.7,0x706b52);box(g,x,6.3,z,1.7,.13,.13,0x6c7259);solid(x,z,.20,.20,6.8);if(i){const prev=poleXs[i-1];for(const offset of [-.55,0,.55]){let a={x:prev+offset,y:6.4,z};for(let n=1;n<=8;n++){const t=n/8,b={x:prev+(x-prev)*t+offset,y:6.4-Math.sin(t*Math.PI)*.65,z};line(g,a,b,.017,0x4c584b);a=b;}}}if(i%2===0){box(g,x,5.5,z-.5,.14,.12,1.1,0x53604e);box(g,x,5.45,z-1.1,.6,.14,.33,0xc2b993);world.atmosphere.registerLamp(x,z-1.1,null,{height:5.4,power:65,poolSize:6,distance:18});}}
  buildWestEdges(world,kit,{group,line});
  buildWestIndustry(world,kit,{group,solid,line,pallet});
  // Sparse edge weeds cluster by fences/drains, never in the central traffic lane.
  // Detailed atlas undergrowth is supplied by Meadow; no upright triangle weeds.
  for(const [i,p] of [[-538,40],[-509,62],[-506,-46],[-481,-45],[-458,53],[-438,-29],[-432,40],[-416,28],[-402,50],[-372,30],[-348,67],[-307,41],[-270,31]].entries()){addWestTree(world,p[0],p[1],2.3+i%3*.25,{grate:false,seed:820+i});}
  // Quiet fields extend to the north/south limits, with a sparse treeline beyond play.
  for(let i=0;i<18;i++){const x=-544+(i*29%274),z=(i%2?1:-1)*(110+i%5*25);addWestTree(world,x,z,2.7,{grate:false,distant:true,seed:940+i});}
  // A small bus shelter, set behind the clear boarding strip.
  westBusShelter(world,kit,group(-278,14.8),{line,bench});
  // Personal belongings under cover and a cart: small, intentional arrangements.
  const sleep=group(-511,29);for(const [x,z,w,d] of [[-513,28.7,1.1,.7],[-509,27.5,.6,.8]]){const paper=box(sleep,x,.171,z,w,.013,d,0x9f896b);paper.rotation.y=.2;}
  for(const [x,z,r] of [[-513,30,.32],[-508.5,30,.24]]){const bag=sphere(sleep,x,.16+r*1.2,z,r,0x4f5750);bag.scale.y*=1.2;}
  const cart=group(-505,32),cx=-505,cz=32,cy=groundHeight(cx,cz);
  for(const dx of [-.4,.4])for(const dz of [-.43,.43]){const tire=cylinder(cart,cx+dx,cy+.12,cz+dz,.12,.08,0x39463d);tire.rotation.z=Math.PI/2;}
  box(cart,cx,cy+.47,cz,.9,.04,1.05,0x7b8a7c);
  for(const yy of [.5,.7,.9])for(const side of [-1,1]){box(cart,cx+side*.46,cy+yy,cz,.026,.026,1.05,0x8b9585);box(cart,cx,cy+yy,cz+side*.52,.95,.026,.026,0x8b9585);}
  for(const dx of [-.45,0,.45])for(const dz of [-.52,.52])box(cart,cx+dx,cy+.7,cz+dz,.023,.5,.023,0x929c8e);
  box(cart,cx,cy+1.05,cz+.65,.85,.05,.05,0x57705c);box(cart,cx,cy+.65,cz,.7,.32,.6,0x918666);solid(cx,cz,1.05,1.4,cy+1.1);
  const oldCar=createCar(kit,'car',0x626d53);oldCar.position.set(-471,groundHeight(-471,-19)-.03,-19);oldCar.rotation.y=.18;group(-471,-19).add(oldCar);world.colliders.push(...westParkedColliders(-471,-19,.18,'car',oldCar.position.y));
  // Wet patches retain the shared weather response; asphalt repairs are in the material.
  for(const [x,z] of [[-519,2.3],[-469,-2.5],[-409,3.4],[-353,-3.1]])world.atmosphere.addPuddle(x,z,1.15,.42);
  for(const x of [-518,-466,-409,-356,-292]){const z=x<-425?3.3:x<-335?4.3:6.2,g=group(x,z);box(g,x,.149,z,.8,.008,.38,0x35483e);for(let n=0;n<5;n++)box(g,x-.3+n*.15,.155,z,.035,.006,.31,0x899080);}
  // Open, irregular foliage replaces the old rows of solid green balls.
  for(const [i,[x,z]] of [[-536,-16],[-521,-14],[-488,-39],[-444,40],[-413,35],[-500,62],[-516,62],[-530,62]].entries())westShrub(group(x,z),x,groundHeight(x,z),z,1.05+i%3*.15,i);
  dressWest(world,kit,{group,solid,line,bench,pallet});
  buildWestCourtyards(world,kit,{group,solid,line,bench});
  if(batch)for(const g of this.blocks.values())batchWestGeometry(g);
 }
 cameraShelter(position,riding=false){
  if(riding)return null;
  if(Math.abs(position.x+278)<2.45&&position.z>=13.35&&position.z<=15.62)return {ceiling:groundHeight(-278,14.8)+2.575,minBoom:2.6};
  if(Math.abs(position.x+511)<3.4&&position.z>=26.45&&position.z<=31.5)return {ceiling:2.78,minBoom:2.2};
  const canopy=this.world.overheadColliders.find(c=>c.westCanopy&&Math.abs(position.x-c.x)<c.w&&Math.abs(position.z-c.z)<c.d&&position.y+1.65<c.minY);
  return canopy?{ceiling:canopy.minY,minBoom:2.2}:null;
 }
 update(state,dt,p){this.age+=Math.max(0,dt);if(this.age<.25)return;this.age=0;for(const g of this.blocks.values()){const c=g.userData.center;g.visible=Math.hypot(c.x-p.x,c.z-p.z)<205;}}
}
