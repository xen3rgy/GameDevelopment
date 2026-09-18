import * as THREE from './vendor/three.module.js';

const textures=new Map(),materials=new Map(),sources=new Map();
export const DISTRICT_TEXTURES={brick:'./assets/station-brick-069.png',limestone:'./assets/civic-limestone-069.png'};
export function districtMaterial(kind,width=1,height=1,tint=0xffffff){
 const key=[kind,width,height,tint].join(':');if(materials.has(key))return materials.get(key);
 let map;
 if(typeof document!=='undefined'&&typeof document.createElementNS==='function'){
  const textureKey=kind+':'+width+':'+height;
  if(!textures.has(textureKey)){
   if(!sources.has(kind)){const copies=[];const base=new THREE.TextureLoader().load(DISTRICT_TEXTURES[kind],()=>copies.forEach(t=>t.needsUpdate=true));sources.set(kind,{base,copies});}
   const source=sources.get(kind),t=source.base.clone();source.copies.push(t);t.colorSpace=THREE.SRGBColorSpace;
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
