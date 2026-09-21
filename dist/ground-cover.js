import * as THREE from './vendor/three.module.js';
import {groundHeight} from './spatial.js?v=0.7.4-core1';
import {streetSurface} from './street-layout.js?v=0.7.3-map1';
import {frontageSurface} from './frontage-layout.js?v=0.7.3-map1';
import {onGardenPath} from './pedestrian-layout.js?v=0.7.3-map1';

function rng(seed){let n=seed>>>0;return()=>((n=(Math.imul(n,1664525)+1013904223)>>>0)/4294967296)}
function pushTri(a,p0,p1,p2){a.push(...p0,...p1,...p2)}
function tuftGeometry(){
 const pos=[],rand=rng(7519);
 for(let i=0;i<10;i++){
  const a=rand()*Math.PI*2,r=.03+rand()*.12,cx=Math.cos(a)*r,cz=Math.sin(a)*r,h=.22+rand()*.34,w=.035+rand()*.045;
  const dx=Math.cos(a)*w,dz=Math.sin(a)*w;
  pushTri(pos,[cx-dx,0,cz-dz],[cx+dx,0,cz+dz],[cx+(rand()-.5)*.025,h,cz+(rand()-.5)*.025]);
 }
 const g=new THREE.BufferGeometry();g.setAttribute('position',new THREE.Float32BufferAttribute(pos,3));g.computeVertexNormals();return g;
}
function rosetteGeometry(){
 const pos=[];for(let i=0;i<8;i++){const a=i/8*Math.PI*2,b=a+.34,r=.18+(i%3)*.035;pushTri(pos,[0,.008,0],[Math.cos(a)*r,.012,Math.sin(a)*r],[Math.cos(b)*r*.82,.012,Math.sin(b)*r*.82])}
 const g=new THREE.BufferGeometry();g.setAttribute('position',new THREE.Float32BufferAttribute(pos,3));g.computeVertexNormals();return g;
}
function petalGeometry(){
 const pos=[];for(let i=0;i<8;i++){const a=i/8*Math.PI*2,b=(i+.5)/8*Math.PI*2;pushTri(pos,[0,0,0],[Math.cos(a)*.11,0,Math.sin(a)*.11],[Math.cos(b)*.055,0,Math.sin(b)*.055])}
 const g=new THREE.BufferGeometry();g.setAttribute('position',new THREE.Float32BufferAttribute(pos,3));g.computeVertexNormals();return g;
}
const scratch=new THREE.Object3D();

export class GroundCover{
 constructor(world){
  this.world=world;this.root=new THREE.Group();this.root.name='Lindenstadt · living grass';world.scene.add(this.root);
  this.meshes=[];this.build();
 }
 clearPoint(x,z,margin=.55){
  if(streetSurface(x,z)||frontageSurface(x,z)||onGardenPath(x,z))return false;
  if(Math.abs(x)>121||Math.abs(z)>121)return false;
  for(const c of this.world.colliders){
   if(c.radius!=null){if(Math.hypot(x-c.x,z-c.z)<c.radius+margin)return false;continue}
   if(Math.abs(x-c.x)<(c.w??0)+margin&&Math.abs(z-c.z)<(c.d??0)+margin)return false;
  }
  return true;
 }
 makeInstanced(geometry,material,items,name){
  const mesh=new THREE.InstancedMesh(geometry,material,items.length);mesh.name=name;mesh.castShadow=false;mesh.receiveShadow=true;
  items.forEach((p,i)=>{scratch.position.set(p.x,p.y,p.z);scratch.rotation.set(0,p.r,0);scratch.scale.set(p.s,p.sy??p.s,p.s);scratch.updateMatrix();mesh.setMatrixAt(i,scratch.matrix)});
  mesh.instanceMatrix.needsUpdate=true;mesh.computeBoundingSphere?.();this.root.add(mesh);this.meshes.push(mesh);return mesh;
 }
 build(){
  const rand=rng(20260921),tuftsA=[],tuftsB=[],rosettes=[],daisies=[],dandelions=[];
  for(let x=-119;x<=119;x+=2.45)for(let z=-119;z<=119;z+=2.45){
   const px=x+(rand()-.5)*1.55,pz=z+(rand()-.5)*1.55;if(!this.clearPoint(px,pz))continue;
   const park=Math.abs(px-76)<19&&Math.abs(pz-93)<17,west=px<-72,density=park?.92:west?.58:.46;
   if(rand()<density){
    const y=groundHeight(px,pz)+.012,s=.58+rand()*.78,entry={x:px,y,z:pz,r:rand()*Math.PI,s,sy:.65+rand()*.7};
    (rand()>.42?tuftsA:tuftsB).push(entry);
   }
   if(rand()<(park?.24:west?.12:.08))rosettes.push({x:px+(rand()-.5)*.65,y:groundHeight(px,pz)+.017,z:pz+(rand()-.5)*.65,r:rand()*Math.PI,s:.55+rand()*.8});
   const flowerChance=park?.115:west?.055:.035;
   if(rand()<flowerChance){
    const y=groundHeight(px,pz)+.19+rand()*.08,target=rand()<.7?daisies:dandelions;
    target.push({x:px+(rand()-.5)*.7,y,z:pz+(rand()-.5)*.7,r:rand()*Math.PI,s:.45+rand()*.55,sy:1});
   }
  }
  const grassA=new THREE.MeshStandardMaterial({color:0x5f8247,roughness:.95,side:THREE.DoubleSide});
  const grassB=new THREE.MeshStandardMaterial({color:0x769356,roughness:.96,side:THREE.DoubleSide});
  const weed=new THREE.MeshStandardMaterial({color:0x6d8d51,roughness:.97,side:THREE.DoubleSide});
  const white=new THREE.MeshStandardMaterial({color:0xf3eed6,roughness:.9,side:THREE.DoubleSide});
  const yellow=new THREE.MeshStandardMaterial({color:0xe6b92d,roughness:.9,side:THREE.DoubleSide});
  const tuft=tuftGeometry(),rosette=rosetteGeometry(),petal=petalGeometry();
  this.makeInstanced(tuft,grassA,tuftsA,'Grass tufts · deep');
  this.makeInstanced(tuft,grassB,tuftsB,'Grass tufts · fresh');
  this.makeInstanced(rosette,weed,rosettes,'Low weeds & clover');
  const daisy=this.makeInstanced(petal,white,daisies,'Daisies');daisy.rotation.x=0;
  const dandelion=this.makeInstanced(petal,yellow,dandelions,'Dandelions');dandelion.rotation.x=0;
 }
 update(state){
  const outside=!state.inside;this.root.visible=outside;
  const low=state.settings?.quality==='low';
  for(const mesh of this.meshes)mesh.visible=outside&&(!low||mesh.name.includes('Grass tufts · deep'));
 }
}
