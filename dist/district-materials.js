import * as THREE from './vendor/three.module.js';
import {districtBlendAt,districtIdentityAt} from './district-identity.js?v=0.8.1';

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

export function districtLampStyle(x,z=0){
 const color=new THREE.Color(0);let power=0;
 for(const entry of districtBlendAt({x,z})){color.add(new THREE.Color(entry.profile.light.color).multiplyScalar(entry.weight));power+=entry.profile.light.power*entry.weight;}
 return {color,power};
}
const windowSets=new WeakMap();
const windowStyles={brick:[0xffcf99,.65],civic:[0xffe4bd,.65],residential:[0xffd9ad,.6],industrial:[0xf0debe,.36],office:[0xf0eadd,.42],campus:[0xe7eddf,.46],townhouse:[0xffdfb5,.48]};
export function districtWindowSet(windows,x,z){
 if(windows.length<5)return windows;
 const style=districtIdentityAt({x,z}).style,key=style==='townhouse'?'residential':style;
 let sets=windowSets.get(windows);if(!sets){sets=new Map();windowSets.set(windows,sets);}
 if(!sets.has(key)){const [color,strength]=windowStyles[key]||windowStyles.residential;sets.set(key,windows.slice(0,5).map((base,i)=>{const m=base.clone();m.name='District glazing · '+key+' '+i;m.emissive.set(color);m.userData.districtGlow=strength;m.emissiveIntensity=0;return m;}));}
 return sets.get(key);
}
export function updateDistrictWindows(windows,night){
 const sets=windowSets.get(windows);if(!sets)return;
 for(const materials of sets.values())for(let i=0;i<materials.length;i++)materials[i].emissiveIntensity=(i===3?1.25:i===4?.62:i===0?.38:.025)*night*materials[i].userData.districtGlow;
}
