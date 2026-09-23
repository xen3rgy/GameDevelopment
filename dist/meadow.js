import * as THREE from './vendor/three.module.js';
import {groundHeight} from './spatial.js?v=0.8.1';
import {BUILDINGS} from './data.js?v=0.8.1';
import {streetSurface} from './street-layout.js?v=0.8.1';
import {onGardenPath} from './pedestrian-layout.js?v=0.8.1';
import {frontageSurface} from './frontage-layout.js?v=0.8.1';

export function meadowPoint(x,z,colliders=[]){
 if(x<-124||x>124||Math.abs(z)>124||streetSurface(x,z)||onGardenPath(x,z)||frontageSurface(x,z))return false;
 if(BUILDINGS.some(([bx,bz,w,d])=>Math.abs(x-bx)<w/2+.65&&Math.abs(z-bz)<d/2+.65))return false;
 if(colliders.some(c=>Math.abs(x-c.x)<c.w+.5&&Math.abs(z-c.z)<c.d+.5))return false;
 return groundHeight(x,z)<0||groundHeight(x,z)===.18;
}
export function meadowPlacements(colliders=[]){
 let seed=731;const rand=()=>((seed=(Math.imul(seed,1664525)+1013904223)>>>0)/4294967296),groups=new Map();
 for(let x=-123;x<124;x+=1.05)for(let z=-123;z<124;z+=1.05){
  const px=x+(rand()-.5)*.7,pz=z+(rand()-.5)*.7,r=rand();if(r<.16||!meadowPoint(px,pz,colliders))continue;
  const key=Math.floor(px/18)+','+Math.floor(pz/18);if(!groups.has(key))groups.set(key,[]);
  groups.get(key).push({x:px,z:pz,size:.36+rand()*.32,angle:rand()*Math.PI,cell:Math.floor(rand()*12),tone:.82+rand()*.18});
 }
 return groups;
}
export class Meadow{
 constructor(world){
  this.world=world;this.root=new THREE.Group();this.root.name='Living meadow';world.scene.add(this.root);this.chunks=[];this.time={value:0};
  const loader=new THREE.TextureLoader(),ground=loader.load('./assets/meadow-ground.png');ground.colorSpace=THREE.SRGBColorSpace;ground.wrapS=ground.wrapT=THREE.RepeatWrapping;ground.repeat.set(250/3.5,250/3.5);ground.anisotropy=Math.min(8,world.renderer.capabilities.getMaxAnisotropy());
  const bed=new THREE.Mesh(new THREE.PlaneGeometry(250,250),new THREE.MeshStandardMaterial({map:ground,color:0xcbd0b7,roughness:1}));bed.rotation.x=-Math.PI/2;bed.position.y=-.044;bed.receiveShadow=true;this.root.add(bed);const park=bed.clone();park.geometry=new THREE.PlaneGeometry(39,36);park.material=bed.material.clone();park.material.map=ground.clone();park.material.map.repeat.set(39/3.5,36/3.5);park.position.set(76,.184,93);this.root.add(park);
  const atlas=loader.load('./assets/meadow-clumps.png');atlas.colorSpace=THREE.SRGBColorSpace;atlas.anisotropy=ground.anisotropy;
  const material=new THREE.MeshStandardMaterial({map:atlas,alphaTest:.38,side:THREE.DoubleSide,roughness:1});
  material.onBeforeCompile=shader=>{
   shader.uniforms.meadowTime=this.time;
   shader.vertexShader='attribute vec2 meadowCell; uniform float meadowTime;\n'+shader.vertexShader;
   shader.vertexShader=shader.vertexShader.replace('#include <uv_vertex>','#include <uv_vertex>\n vMapUv = (vec2(.025) + vMapUv * .95 + meadowCell) / vec2(3.,4.);');
   shader.vertexShader=shader.vertexShader.replace('#include <begin_vertex>','#include <begin_vertex>\n transformed.x += sin(meadowTime*1.5+instanceMatrix[3].x*.8+instanceMatrix[3].z)*.055*uv.y*uv.y;');
  };
  material.customProgramCacheKey=()=> 'meadow-atlas-wind-1';
  const dummy=new THREE.Object3D();
  for(const list of meadowPlacements(world.colliders).values()){
   // Two crossed, root-anchored cards share one draw call per spatial chunk.
   const p=[-.5,0,0,.5,0,0,.5,1,0,-.5,1,0,0,0,-.5,0,0,.5,0,1,.5,0,1,-.5],uv=[0,0,1,0,1,1,0,1,0,0,1,0,1,1,0,1];
   const geo=new THREE.BufferGeometry();geo.setAttribute('position',new THREE.Float32BufferAttribute(p,3));geo.setAttribute('uv',new THREE.Float32BufferAttribute(uv,2));geo.setIndex([0,1,2,0,2,3,4,5,6,4,6,7]);geo.computeVertexNormals();
   geo.setAttribute('meadowCell',new THREE.InstancedBufferAttribute(new Float32Array(list.flatMap(p=>[p.cell%3,3-Math.floor(p.cell/3)])),2));
   const mesh=new THREE.InstancedMesh(geo,material,list.length);let x=0,z=0;
   list.forEach((p,i)=>{dummy.position.set(p.x,groundHeight(p.x,p.z)-.005,p.z);dummy.rotation.y=p.angle;dummy.scale.set(p.size*1.5,p.size,p.size*1.5);dummy.updateMatrix();mesh.setMatrixAt(i,dummy.matrix);mesh.setColorAt(i,new THREE.Color(p.tone,p.tone,p.tone));x+=p.x;z+=p.z;});
   mesh.computeBoundingSphere();mesh.receiveShadow=true;mesh.userData.center={x:x/list.length,z:z/list.length};this.root.add(mesh);this.chunks.push(mesh);
  }
 }
 update(position,dt,inside,quality){this.time.value+=dt;this.root.visible=!inside&&this.enabled!==false;const range=quality==='low'?28:62;for(const mesh of this.chunks){mesh.visible=Math.hypot(position.x-mesh.userData.center.x,position.z-mesh.userData.center.z)<range;mesh.count=quality==='low'?Math.ceil(mesh.instanceMatrix.count*.45):mesh.instanceMatrix.count;}}
}
