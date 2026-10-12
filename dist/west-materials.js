import * as THREE from './vendor/three.module.js';

// Bounded, deterministic texture set. Metre-based UVs keep concrete, boards and
// paving at the same scale on every wall, including narrow sides and cut plots.
const materials=new Map(),geometries=new Map(),textureCopies=new Map();
const photoBumpKinds=new Set(['asphalt','paving','concrete','yard']);
const photoKinds=new Set(['asphalt','paving','yard','plaster','brick','concrete','wood','steel','ballast']);
export const westTileMeters=kind=>({asphalt:2.5,paving:2,yard:1.2,plaster:3,brick:1.8,concrete:4,wood:2,steel:2,ballast:3,roof:2}[kind]||4);
export function cloneWestTexture(source){const copy=source.clone(),url=source.userData.westSource;if(url){if(!textureCopies.has(url))textureCopies.set(url,[]);textureCopies.get(url).push(copy);}return copy;}
function photoTexture(kind,height=false){const url='./assets/west-v2/'+kind+(height?'-height':'')+'.jpg',loader=new THREE.TextureLoader(),t=loader.load(url,()=>{for(const copy of textureCopies.get(url)||[])copy.needsUpdate=true;});t.userData.westSource=url;t.wrapS=t.wrapT=THREE.RepeatWrapping;t.anisotropy=8;if(!height)t.colorSpace=THREE.SRGBColorSpace;return t;}
const palette={brick:'#9b7963',yard:'#716c5d',concrete:'#92928b',plaster:'#b9b3a2',wood:'#76695b',steel:'#535c59',ballast:'#746d61',soil:'#625d46',roof:'#65534b',paving:'#777a71',asphalt:'#42484a'};
export function westMaterial(kind){
 if(materials.has(kind))return materials.get(kind);
 let map=null;
 if(typeof document!=='undefined'&&typeof document.createElementNS==='function'&&photoKinds.has(kind)){map=photoTexture(kind);}
 else if(typeof document!=='undefined'){
  const canvas=document.createElement('canvas');canvas.width=canvas.height=512;const c=canvas.getContext('2d');let seed=1847+kind.length*81;const random=()=>((seed=(Math.imul(seed,1664525)+1013904223)>>>0)/4294967296);
  c.fillStyle=palette[kind]||palette.concrete;c.fillRect(0,0,512,512);
  for(let i=0;i<24000;i++){const light=random()>.5;c.fillStyle=light?'#ddd4bf12':'#151f2410';const s=1+random()*2;c.fillRect(random()*512,random()*512,s,s);}
  if(kind==='yard'){
   for(let i=0;i<95;i++){const x=random()*512,y=random()*512,r=8+random()*40;c.fillStyle=i%3?'#494e3520':'#ac9e8120';c.beginPath();for(let n=0;n<14;n++){const a=n/13*Math.PI*2,rr=r*(.45+random()*.55);c.lineTo(x+Math.cos(a)*rr,y+Math.sin(a)*rr*.7);}c.fill();}
   for(let i=0;i<18000;i++){c.fillStyle=i%2?'#d3c8ac30':'#30372e30';const r=.4+random()*1.2;c.fillRect(random()*512,random()*512,r,r);}
  }
  if(kind==='concrete'||kind==='plaster'){
   for(let i=0;i<24;i++){const x=random()*512,y=random()*512,r=12+random()*65;const g=c.createRadialGradient(x,y,1,x,y,r);g.addColorStop(0,kind==='concrete'?'#303a3510':'#6155470d');g.addColorStop(1,'#34372c00');c.fillStyle=g;c.fillRect(x-r,y-r,r*2,r*2);}
   if(kind==='concrete')for(let y=0;y<512;y+=128){c.fillStyle='#343b3430';c.fillRect(0,y,512,2);c.fillStyle='#eee8d323';c.fillRect(0,y+2,512,1);for(let x=32;x<512;x+=128){c.fillStyle='#464b4370';c.beginPath();c.arc(x,y+28,2.5,0,Math.PI*2);c.fill();}}
   for(let i=0;i<14;i++){const x=random()*512,y=random()*512;c.fillStyle='#514a3c14';c.fillRect(x,y,2+random()*5,30+random()*100);}
  }
  if(kind==='wood'||kind==='steel')for(let x=0;x<512;x+=kind==='wood'?64:32){c.fillStyle='#131d2455';c.fillRect(x,0,2,512);c.fillStyle='#dfd5b72c';c.fillRect(x+2,0,2,512);for(let i=0;i<30;i++){c.fillStyle=kind==='wood'?'#32291d30':'#97624128';c.fillRect(x+4+random()*22,random()*512,1+random()*3,10+random()*80);}}
  if(['ballast','soil','asphalt'].includes(kind))for(let i=0;i<4200;i++){const v=80+Math.floor(random()*70);c.fillStyle=`rgba(${v+9},${v+6},${v},${kind==='asphalt'?.055:.28})`;c.beginPath();c.ellipse(random()*512,random()*512,kind==='asphalt'?.3+random():1+random()*4,kind==='asphalt'?.3+random():1+random()*2,random()*3,0,Math.PI*2);c.fill();}
  if(kind==='asphalt'){
   // Irregular resurfacing, fine aggregate and branching tar cracks. All in one
   // cached texture: no repeated raised discs or per-frame geometry.
   for(let i=0;i<8;i++){const x=random()*512,y=random()*512,r=12+random()*49;c.fillStyle=i%2?'#30373680':'#74757130';c.beginPath();for(let n=0;n<11;n++){const a=n/10*Math.PI*2,rr=r*(.7+random()*.3);c.lineTo(x+Math.cos(a)*rr,y+Math.sin(a)*rr*.65);}c.fill();}
   for(let i=0;i<18;i++){let x=random()*512,y=random()*512;c.strokeStyle='#18202099';c.lineWidth=.6+random();c.beginPath();c.moveTo(x,y);for(let n=0;n<5;n++){x+=random()*27-10;y+=random()*18-5;c.lineTo(x,y);}c.stroke();}
   for(let i=0;i<25000;i++){c.fillStyle=i%3?'#cad0c11d':'#141c2040';c.fillRect(random()*512,random()*512,.7,.7);}
  }
  if(kind==='paving'){for(let i=0;i<28;i++){c.fillStyle=i%3?'#39402b0a':'#6254470a';c.fillRect(Math.floor(random()*8)*64,Math.floor(random()*8)*64,64,64);}}
  if(kind==='paving'||kind==='roof')for(let y=0;y<512;y+=kind==='roof'?32:64)for(let x=-64;x<512;x+=64){const xx=x+(y%128?32:0);c.strokeStyle='#30393455';c.lineWidth=2;c.strokeRect(xx+1,y+1,62,kind==='roof'?30:62);c.fillStyle='#e5dcc017';c.fillRect(xx+3,y+3,59,3);if(random()<.13){c.strokeStyle='#40473c70';c.beginPath();c.moveTo(xx+5,y+3);c.lineTo(xx+27,y+22);c.lineTo(xx+59,y+28);c.stroke();}}
  map=new THREE.CanvasTexture(canvas);map.colorSpace=THREE.SRGBColorSpace;map.wrapS=map.wrapT=THREE.RepeatWrapping;map.anisotropy=8;
 }
 const m=new THREE.MeshStandardMaterial({name:'West '+kind,color:map?0xffffff:palette[kind],map,bumpMap:map?.userData.westSource?(photoBumpKinds.has(kind)?photoTexture(kind,true):null):map,bumpScale:kind==='asphalt'?.022:kind==='concrete'?.045:.028,roughness:kind==='steel'?.7:.94,metalness:kind==='steel'?.3:0,side:kind==='ballast'?THREE.DoubleSide:THREE.FrontSide});if(map?.userData.westSource&&kind==='yard')m.color.set(0xb2b3a6);materials.set(kind,m);return m;
}
export function westFacadeMaterial(w,h,tint,kind='plaster'){const key=['facade',kind,w,h,tint].join(':');if(!materials.has(key)){const m=westMaterial(kind).clone();m.color.set(tint).lerp(new THREE.Color(0xffffff),.65);if(m.map){m.map=cloneWestTexture(m.map);m.map.repeat.set(w/westTileMeters(kind),h/westTileMeters(kind));if(m.bumpMap){m.bumpMap=cloneWestTexture(m.bumpMap);m.bumpMap.repeat.copy(m.map.repeat);}}materials.set(key,m);}return materials.get(key);}
export function westBox(parent,kind,x,y,z,w,h,d){
 const key=[kind,w,h,d].join(':');let geometry=geometries.get(key);
 if(!geometry){geometry=new THREE.BoxGeometry(w,h,d);const p=geometry.attributes.position,n=geometry.attributes.normal,uv=geometry.attributes.uv,tile=westTileMeters(kind);
  for(let i=0;i<p.count;i++)uv.setXY(i,(Math.abs(n.getX(i))>.5?p.getZ(i):p.getX(i))/tile,(Math.abs(n.getY(i))>.5?p.getZ(i):p.getY(i))/tile);geometries.set(key,geometry);}
 const mesh=new THREE.Mesh(geometry,westMaterial(kind));mesh.position.set(x,y,z);mesh.castShadow=true;mesh.receiveShadow=true;mesh.userData.staticBatch=true;parent.add(mesh);return mesh;
}
export function westGround(parent,kind,x,z,w,d,y,heightAt=null){
 const texturedYard=kind==='yard'||kind==='ballast',step=heightAt?.35:texturedYard?3:Math.max(w,d),geometry=new THREE.PlaneGeometry(w,d,Math.ceil(w/step),Math.ceil(d/step));geometry.rotateX(-Math.PI/2);const p=geometry.attributes.position,uv=geometry.attributes.uv,colors=[];
 for(let i=0;i<p.count;i++){const px=x+p.getX(i),pz=z+p.getZ(i);uv.setXY(i,px/westTileMeters(kind),pz/westTileMeters(kind));if(heightAt)p.setY(i,heightAt(px,pz)-y);const shade=.86+.08*Math.sin(px*.17+pz*.11)+.06*Math.cos(px*.39-pz*.23);colors.push(shade,shade,shade*.96);}
 let material=westMaterial(kind);if(texturedYard){const key='terrain:'+kind;if(!materials.has(key)){const m=material.clone();m.vertexColors=true;materials.set(key,m);}material=materials.get(key);geometry.setAttribute('color',new THREE.Float32BufferAttribute(colors,3));}
 geometry.computeVertexNormals();const mesh=new THREE.Mesh(geometry,material);mesh.position.set(x,y,z);mesh.receiveShadow=true;mesh.userData.westGround=true;parent.add(mesh);return mesh;
}
const decals=new Map();
export function westWeathering(parent,x,z,w,h,angle,seed=0){
 const key='weather'+seed%4;
 if(!decals.has(key)){
  let map=null;
  if(typeof document!=='undefined'){
   const canvas=document.createElement('canvas');canvas.width=1024;canvas.height=512;const c=canvas.getContext('2d');let state=728+seed%4;
   const rnd=()=>((state=(Math.imul(state,1664525)+1013904223)>>>0)/4294967296);
   const damp=c.createLinearGradient(0,330,0,512);damp.addColorStop(0,'#292d2200');damp.addColorStop(1,'#292d22c0');c.fillStyle=damp;c.fillRect(0,330,1024,182);
   for(let i=0;i<24;i++){const px=rnd()*1024,py=rnd()*470,r=5+rnd()*30;c.fillStyle=['#655e5085','#8c827185','#998d7480'][i%3];c.beginPath();for(let n=0;n<18;n++){const a=n/17*Math.PI*2,rr=r*(.45+rnd()*.55);c.lineTo(px+Math.cos(a)*rr,py+Math.sin(a)*rr*.7);}c.fill();}
   for(let i=0;i<90;i++){const px=rnd()*1024,py=rnd()*400,len=25+rnd()*105;const streak=c.createLinearGradient(0,py,0,py+len);streak.addColorStop(0,'#39352660');streak.addColorStop(1,'#39352600');c.fillStyle=streak;c.fillRect(px,py,1+rnd()*6,len);}
   for(let i=0;i<12;i++){let px=rnd()*1024,py=rnd()*460;c.strokeStyle='#3938309a';c.lineWidth=.9;c.beginPath();c.moveTo(px,py);for(let n=0;n<6;n++){px+=rnd()*14-7;py+=rnd()*15;c.lineTo(px,py);}c.stroke();}
   map=new THREE.CanvasTexture(canvas);map.colorSpace=THREE.SRGBColorSpace;map.anisotropy=8;
  }
  decals.set(key,new THREE.MeshStandardMaterial({map,transparent:true,depthWrite:false,roughness:1,polygonOffset:true,polygonOffsetFactor:-1,polygonOffsetUnits:-1}));
 }
 const m=new THREE.Mesh(new THREE.PlaneGeometry(w,h),decals.get(key));m.position.set(x,h/2,z);m.rotation.y=angle;parent.add(m);return m;
}
export function westWallArt(parent,kind,x,y,z,w,h,angle=0){
 if(!decals.has(kind)){
  let map=null;if(typeof document!=='undefined'){
   const canvas=document.createElement('canvas');canvas.width=512;canvas.height=256;const c=canvas.getContext('2d');
   if(kind==='posters'){for(let i=0;i<4;i++){c.fillStyle=['#c2b89b','#848f80','#b2987a','#a1a597'][i];c.fillRect(8+i*124,12+i%2*17,110,210-i%2*23);c.fillStyle='#34403c';c.font='bold 18px sans-serif';c.fillText(['HOFMARKT','LIVE MUSIK','REPARATUR','TAUSCHEN'][i],14+i*124,50+i%2*17);for(let j=0;j<7;j++)c.fillRect(17+i*124,76+j*15,65+(j%3)*11,3);}}
   else {c.translate(18,173);c.rotate(-.045);c.font='italic 900 108px sans-serif';c.lineJoin='round';c.lineWidth=13;c.strokeStyle='#242e2ce0';c.strokeText(kind==='rail'?'ALTGLEIS':'BLEIB HIER',0,0,475);c.lineWidth=3;c.strokeStyle='#d7c7a5';c.fillStyle=kind==='rail'?'#6f9791':'#b28b65';c.fillText(kind==='rail'?'ALTGLEIS':'BLEIB HIER',0,0,475);c.strokeText(kind==='rail'?'ALTGLEIS':'BLEIB HIER',0,0,475);}
   map=new THREE.CanvasTexture(canvas);map.colorSpace=THREE.SRGBColorSpace;map.anisotropy=8;
  }
  decals.set(kind,new THREE.MeshStandardMaterial({map,transparent:true,depthWrite:false,roughness:1,polygonOffset:true,polygonOffsetFactor:-2,polygonOffsetUnits:-2}));
 }
 const mesh=new THREE.Mesh(new THREE.PlaneGeometry(w,h),decals.get(kind));mesh.position.set(x,y,z);mesh.rotation.y=angle;parent.add(mesh);return mesh;
}

// Only the western scenery is immutable. Merge opaque details of different
// shapes by material once, preserving UVs/normals and spatial block culling.
// Hidden customization variants and transparent glass/decals stay untouched.
export function batchWestGeometry(root){
 root.updateMatrixWorld(true);const inverse=root.matrixWorld.clone().invert(),buckets=new Map();
 root.traverse(m=>{if(!m.isMesh||m.isInstancedMesh||m.isSkinnedMesh||Array.isArray(m.material)||m.material.transparent||m.material.vertexColors)return;for(let p=m;p&&p!==root;p=p.parent)if(!p.visible)return;
  const key=m.material;if(!buckets.has(key))buckets.set(key,[]);buckets.get(key).push(m);
 });
 for(const [material,meshes] of buckets){if(meshes.length<3)continue;const positions=[],normals=[],uvs=[];
  for(const m of meshes){const source=m.geometry.index?m.geometry.toNonIndexed():m.geometry.clone();source.applyMatrix4(new THREE.Matrix4().multiplyMatrices(inverse,m.matrixWorld));if(!source.attributes.normal)source.computeVertexNormals();const p=source.attributes.position,n=source.attributes.normal,uv=source.attributes.uv;
   for(let i=0;i<p.count;i++){positions.push(p.getX(i),p.getY(i),p.getZ(i));normals.push(n.getX(i),n.getY(i),n.getZ(i));uvs.push(uv?.getX(i)||0,uv?.getY(i)||0);}source.dispose();m.removeFromParent();
  }
  const geometry=new THREE.BufferGeometry();geometry.setAttribute('position',new THREE.Float32BufferAttribute(positions,3));geometry.setAttribute('normal',new THREE.Float32BufferAttribute(normals,3));geometry.setAttribute('uv',new THREE.Float32BufferAttribute(uvs,2));geometry.computeBoundingSphere();const mesh=new THREE.Mesh(geometry,material);mesh.name='West batch · '+material.name;mesh.castShadow=meshes.some(m=>m.castShadow);mesh.receiveShadow=true;root.add(mesh);
 }
}
