import * as THREE from './vendor/three.module.js';
import {groundHeight} from './spatial.js?v=0.7.3-map1';

export const TREE_PIT_BOUNDS={
 minX:-0.950892984867096,maxX:0.947039008140564,
 minY:-0.0831800028681755,maxY:0.10050799697637558,
 minZ:-0.9506480097770691,maxZ:0.9471060037612915
};
export const TREE_PIT_TARGET_SIZE=1.92;
// The soil/leaves in the source asset sit around local Y=0. Lifting the origin 2.5 cm keeps
// that surface visibly above the pavement while the stone base remains naturally recessed.
export const TREE_PIT_SURFACE_LIFT=.025;
const PIT_SIZE=Math.max(TREE_PIT_BOUNDS.maxX-TREE_PIT_BOUNDS.minX,TREE_PIT_BOUNDS.maxZ-TREE_PIT_BOUNDS.minZ);
const PIT_SCALE=TREE_PIT_TARGET_SIZE/PIT_SIZE;
const componentInfo={
 5120:[Int8Array,1,'getInt8'],5121:[Uint8Array,1,'getUint8'],
 5122:[Int16Array,2,'getInt16'],5123:[Uint16Array,2,'getUint16'],
 5125:[Uint32Array,4,'getUint32'],5126:[Float32Array,4,'getFloat32']
};
const widths={SCALAR:1,VEC2:2,VEC3:3,VEC4:4};

function hash(x,z){
 let n=(Math.imul(Math.round(x*100)+313,73856093)^Math.imul(Math.round(z*100)-197,19349663))>>>0;
 n=Math.imul(n^(n>>>16),2246822519)>>>0;n=Math.imul(n^(n>>>13),3266489917)>>>0;
 return ((n^(n>>>16))>>>0)/4294967295;
}
export function treePitTransformSpec(x,z,{ground=groundHeight(x,z)}={}){
 return {x,y:ground+TREE_PIT_SURFACE_LIFT,z,scale:PIT_SCALE,yaw:Math.floor(hash(x,z)*4)*Math.PI/2};
}

function parseGLB(buffer){
 const view=new DataView(buffer);
 if(view.getUint32(0,true)!==0x46546c67||view.getUint32(4,true)!==2)throw Error('Ungültiges GLB-Baumscheibenasset.');
 const jsonLength=view.getUint32(12,true),jsonType=view.getUint32(16,true);
 if(jsonType!==0x4e4f534a)throw Error('Baumscheibe: GLB ohne JSON-Chunk.');
 const json=JSON.parse(new TextDecoder().decode(new Uint8Array(buffer,20,jsonLength)).replace(/\0+$/,'').trim());
 const binHeader=20+jsonLength,binLength=view.getUint32(binHeader,true),binType=view.getUint32(binHeader+4,true);
 if(binType!==0x004e4942)throw Error('Baumscheibe: GLB ohne BIN-Chunk.');
 const binOffset=binHeader+8,primitive=json.meshes?.[0]?.primitives?.[0];
 if(!primitive)throw Error('Baumscheibenasset enthält kein Mesh.');

 const readAccessor=index=>{
  const a=json.accessors[index],bv=json.bufferViews[a.bufferView],info=componentInfo[a.componentType];
  if(!a||!bv||!info)throw Error('Baumscheibe: nicht unterstützter Accessor.');
  const [Type,bytes,getter]=info,n=widths[a.type],packed=n*bytes,stride=bv.byteStride||packed;
  const start=binOffset+(bv.byteOffset||0)+(a.byteOffset||0),out=new Type(a.count*n),data=new DataView(buffer);
  if(stride===packed&&start%bytes===0)return new Type(buffer.slice(start,start+a.count*packed));
  for(let i=0;i<a.count;i++)for(let c=0;c<n;c++)out[i*n+c]=data[getter](start+i*stride+c*bytes,bytes>1);
  return out;
 };
 const geometry=new THREE.BufferGeometry();
 geometry.setAttribute('position',new THREE.BufferAttribute(readAccessor(primitive.attributes.POSITION),3));
 if(primitive.attributes.NORMAL!==undefined)geometry.setAttribute('normal',new THREE.BufferAttribute(readAccessor(primitive.attributes.NORMAL),3));
 if(primitive.attributes.TEXCOORD_0!==undefined)geometry.setAttribute('uv',new THREE.BufferAttribute(readAccessor(primitive.attributes.TEXCOORD_0),2));
 geometry.setIndex(new THREE.BufferAttribute(readAccessor(primitive.indices),1));
 if(!geometry.getAttribute('normal'))geometry.computeVertexNormals();
 geometry.computeBoundingBox();geometry.computeBoundingSphere();
 return {json,binOffset,geometry};
}

async function embeddedTexture(buffer,json,binOffset,textureInfo,renderer,srgb=false){
 if(!textureInfo)return null;
 const texDef=json.textures?.[textureInfo.index],image=json.images?.[texDef?.source],bv=json.bufferViews?.[image?.bufferView];
 if(!texDef||!image||!bv)return null;
 const start=binOffset+(bv.byteOffset||0),bytes=new Uint8Array(buffer.slice(start,start+bv.byteLength));
 const bitmap=await createImageBitmap(new Blob([bytes],{type:image.mimeType||'image/jpeg'})),texture=new THREE.Texture(bitmap);
 texture.needsUpdate=true;texture.flipY=false;texture.anisotropy=Math.min(4,renderer.capabilities.getMaxAnisotropy());
 if(srgb)texture.colorSpace=THREE.SRGBColorSpace;
 const sampler=json.samplers?.[texDef.sampler];
 if(sampler){
  const wrap=v=>v===10497?THREE.RepeatWrapping:v===33648?THREE.MirroredRepeatWrapping:THREE.ClampToEdgeWrapping;
  if(sampler.wrapS)texture.wrapS=wrap(sampler.wrapS);if(sampler.wrapT)texture.wrapT=wrap(sampler.wrapT);
 }
 return texture;
}

export class TreePitSystem{
 constructor(world){this.world=world;this.items=[];this.loaded=false;this.failed=false;this.mesh=null}
 add(x,z){this.items.push({x,z});return this.items.length-1}
 async load(){
  if(this.loaded||this.failed||!this.items.length)return;
  try{
   const response=await fetch('./assets/lindenstadt-tree-pit.glb');if(!response.ok)throw Error('Baumscheibenasset HTTP '+response.status);
   const buffer=await response.arrayBuffer(),{json,binOffset,geometry}=parseGLB(buffer),def=json.materials?.[json.meshes[0].primitives[0].material]||{},pbr=def.pbrMetallicRoughness||{};
   const [map,normalMap,mrMap]=await Promise.all([
    embeddedTexture(buffer,json,binOffset,pbr.baseColorTexture,this.world.renderer,true),
    embeddedTexture(buffer,json,binOffset,def.normalTexture,this.world.renderer,false),
    embeddedTexture(buffer,json,binOffset,pbr.metallicRoughnessTexture,this.world.renderer,false)
   ]);
   const base=pbr.baseColorFactor||[1,1,1,1],material=new THREE.MeshStandardMaterial({
    color:new THREE.Color(base[0],base[1],base[2]),opacity:base[3],transparent:def.alphaMode==='BLEND',
    map,normalMap,roughnessMap:mrMap,metalnessMap:mrMap,
    roughness:pbr.roughnessFactor??1,metalness:pbr.metallicFactor??1,
    side:def.doubleSided?THREE.DoubleSide:THREE.FrontSide
   });
   if(def.normalTexture?.scale!==undefined)material.normalScale.setScalar(def.normalTexture.scale);
   const mesh=new THREE.InstancedMesh(geometry,material,this.items.length),dummy=new THREE.Object3D();
   for(let i=0;i<this.items.length;i++){const {x,z}=this.items[i],t=treePitTransformSpec(x,z);dummy.position.set(t.x,t.y,t.z);dummy.rotation.set(0,t.yaw,0);dummy.scale.setScalar(t.scale);dummy.updateMatrix();mesh.setMatrixAt(i,dummy.matrix)}
   mesh.instanceMatrix.needsUpdate=true;mesh.name='Lindenstadt Tree Pits';mesh.castShadow=false;mesh.receiveShadow=true;mesh.computeBoundingSphere?.();
   (this.world.exterior||this.world.scene).add(mesh);this.mesh=mesh;this.loaded=true;
  }catch(error){this.failed=true;console.error('Lindenstadt tree pit asset could not be loaded:',error)}
 }
}
