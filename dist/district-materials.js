import * as THREE from './vendor/three.module.js';

const textures=new Map(),materials=new Map(),sources=new Map();
export const DISTRICT_TEXTURES={brick:'./assets/station-brick-069.png',limestone:'./assets/civic-limestone-069.png'};
// BoxGeometry repeats the front UVs on its side walls. Correct their horizontal
// density so a long side wall does not stretch the same bricks as a narrow front.
export function fitFacadeUV(mesh,width,depth){
 const geometry=mesh.geometry.clone(),uv=geometry.getAttribute('uv');
 for(let i=0;i<8;i++)uv.setX(i,uv.getX(i)*depth/width);
 uv.needsUpdate=true;mesh.geometry=geometry;return mesh;
}
export function districtMaterial(kind,width=1,height=1,tint=0xffffff){
 const key=[kind,width,height,tint].join(':');if(materials.has(key))return materials.get(key);
 let map;
 if(typeof document!=='undefined'&&typeof document.createElementNS==='function'){
  const textureKey=kind+':'+width+':'+height;
  if(!textures.has(textureKey)){
   if(!sources.has(kind)){const copies=[];const base=new THREE.TextureLoader().load(DISTRICT_TEXTURES[kind],()=>copies.forEach(t=>t.needsUpdate=true));sources.set(kind,{base,copies});}
   // Texture.clone() marks an upload immediately, even while the image is still
   // loading. Share the source but request upload only once pixels are available.
   const source=sources.get(kind),t=new THREE.Texture();t.source=source.base.source;source.copies.push(t);if(source.base.image)t.needsUpdate=true;t.colorSpace=THREE.SRGBColorSpace;
   t.wrapS=t.wrapT=THREE.RepeatWrapping;t.anisotropy=8;
   t.repeat.set(width/(kind==='brick'?1.4:3.2),height/(kind==='brick'?1.2:2.4));textures.set(textureKey,t);
  }
  map=textures.get(textureKey);
 }
 const m=new THREE.MeshStandardMaterial({name:'District '+kind,color:tint,map:map||null,bumpMap:map||null,bumpScale:kind==='brick'?.022:.009,roughness:kind==='brick'?.94:.74,metalness:0});
 materials.set(key,m);return m;
}

export function districtLampStyle(x){
 const t=Math.max(0,Math.min(1,(x+145)/175));
 return {color:new THREE.Color(0xffbe79).lerp(new THREE.Color(0xffe8ca),t),power:250+t*20};
}
