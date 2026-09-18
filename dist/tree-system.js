import * as THREE from './vendor/three.module.js';
import {groundHeight} from './spatial.js?v=0.7.2';

export const TREE_ASSET_BOUNDS={
 minX:-0.4471232295036316,maxX:0.46034368872642517,
 minY:-0.5006174445152283,maxY:0.49576935172080994,
 minZ:-0.49972811341285706,maxZ:0.4996509253978729
};
const TREE_HEIGHT=TREE_ASSET_BOUNDS.maxY-TREE_ASSET_BOUNDS.minY;
const components={5121:Uint8Array,5123:Uint16Array,5125:Uint32Array,5126:Float32Array};
const widths={SCALAR:1,VEC2:2,VEC3:3,VEC4:4};

function hash(x,z,seed=0){
 let n=(Math.imul(Math.round(x*100)+seed*101,73856093)^Math.imul(Math.round(z*100)-seed*53,19349663))>>>0;
 n=Math.imul(n^(n>>>16),2246822519)>>>0;n=Math.imul(n^(n>>>13),3266489917)>>>0;return ((n^(n>>>16))>>>0)/4294967295;
}
export function treeTransformSpec(x,z,r=2.4,{distant=false,seed=0,ground=groundHeight(x,z)}={}){
 const a=hash(x,z,seed),b=hash(x,z,seed+17);
 const base=distant?5.35:5.85+(Math.max(1.6,Math.min(3,r))-1.8)*.72;
 const height=base+(a-.5)*(distant?1.05:.52),scale=height/TREE_HEIGHT,sink=distant?0:.055;
 return {x,y:ground-sink-TREE_ASSET_BOUNDS.minY*scale,z,scale,yaw:b*Math.PI*2,height,sink};
}
function parseGLB(buffer){
 const view=new DataView(buffer);if(view.getUint32(0,true)!==0x46546c67||view.getUint32(4,true)!==2)throw Error('Ungültiges GLB-Baumasset.');
 const jsonLength=view.getUint32(12,true),jsonType=view.getUint32(16,true);if(jsonType!==0x4e4f534a)throw Error('GLB ohne JSON-Chunk.');
 const json=JSON.parse(new TextDecoder().decode(new Uint8Array(buffer,20,jsonLength)).replace(/\0+$/,'').trim());
 const binHeader=20+jsonLength,binLength=view.getUint32(binHeader,true),binType=view.getUint32(binHeader+4,true);if(binType!==0x004e4942)throw Error('GLB ohne BIN-Chunk.');
 const binOffset=binHeader+8;
 const primitive=json.meshes?.[0]?.primitives?.[0];if(!primitive)throw Error('Baumasset enthält kein Mesh.');
 const readAccessor=index=>{const a=json.accessors[index],bv=json.bufferViews[a.bufferView],Type=components[a.componentType],count=a.count*widths[a.type],offset=binOffset+(bv.byteOffset||0)+(a.byteOffset||0),bytes=count*Type.BYTES_PER_ELEMENT;return new Type(buffer.slice(offset,offset+bytes))};
 const geometry=new THREE.BufferGeometry(),positions=readAccessor(primitive.attributes.POSITION),uv=readAccessor(primitive.attributes.TEXCOORD_0),indices=readAccessor(primitive.indices);
 geometry.setAttribute('position',new THREE.BufferAttribute(positions,3));geometry.setAttribute('uv',new THREE.BufferAttribute(uv,2));geometry.setIndex(new THREE.BufferAttribute(indices,1));geometry.computeVertexNormals();geometry.computeBoundingBox();geometry.computeBoundingSphere();
 const image=json.images?.[0],bv=json.bufferViews[image?.bufferView];if(!image||!bv)throw Error('Baumasset enthält keine eingebettete Textur.');
 const bytes=new Uint8Array(buffer.slice(binOffset+(bv.byteOffset||0),binOffset+(bv.byteOffset||0)+bv.byteLength));
 return {geometry,imageBytes:bytes,mime:image.mimeType||'image/jpeg'};
}
export class TreeSystem{
 constructor(world){this.world=world;this.near=[];this.far=[];this.loaded=false;this.failed=false;this.meshes=[]}
 add(x,z,r=2.4,options={}){const list=options.distant?this.far:this.near;list.push({x,z,r,options});return list.length-1}
 async load(){
  if(this.loaded||this.failed)return;
  try{
   const response=await fetch('./assets/lindenstadt-tree.glb');if(!response.ok)throw Error('Baumasset HTTP '+response.status);
   const {geometry,imageBytes,mime}=parseGLB(await response.arrayBuffer()),bitmap=await createImageBitmap(new Blob([imageBytes],{type:mime}));
   const texture=new THREE.Texture(bitmap);texture.needsUpdate=true;texture.flipY=false;texture.colorSpace=THREE.SRGBColorSpace;texture.anisotropy=Math.min(4,this.world.renderer.capabilities.getMaxAnisotropy());
   const material=new THREE.MeshStandardMaterial({map:texture,roughness:.92,metalness:0});
   this.mount(this.near,geometry,material,true);this.mount(this.far,geometry,material,false);this.loaded=true;
  }catch(error){this.failed=true;console.error('Lindenstadt tree asset could not be loaded:',error)}
 }
 mount(items,geometry,material,shadows){
  if(!items.length)return;const mesh=new THREE.InstancedMesh(geometry,material,items.length),dummy=new THREE.Object3D();
  for(let i=0;i<items.length;i++){const {x,z,r,options}=items[i],t=treeTransformSpec(x,z,r,options);dummy.position.set(t.x,t.y,t.z);dummy.rotation.set(0,t.yaw,0);dummy.scale.setScalar(t.scale);dummy.updateMatrix();mesh.setMatrixAt(i,dummy.matrix)}
  mesh.instanceMatrix.needsUpdate=true;mesh.castShadow=shadows;mesh.receiveShadow=false;mesh.name=shadows?'Lindenstadt Trees':'Lindenstadt Distant Trees';mesh.computeBoundingSphere?.();
  (this.world.exterior||this.world.scene).add(mesh);this.meshes.push(mesh);
 }
}
