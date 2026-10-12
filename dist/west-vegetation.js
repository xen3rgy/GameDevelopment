import * as THREE from './vendor/three.module.js';
import {WEST_BUILDINGS,WEST_PATHS,WEST_ROADS,WEST_BOTTLE_SPOTS,WEST_LOCATIONS} from './west-edge-layout.js?v=0.8.1';
import {groundHeight} from './spatial.js?v=0.8.1';

// The imported tree is wider than its nominal r parameter. Reserve the complete
// rotated crown, plus balconies and gutters, rather than just testing the trunk.
export const WEST_CROWN_CLEARANCE=6.2;
export function westTreeClear(x,z,colliders=[]){return !WEST_BUILDINGS.some(b=>Math.abs(x-b.x)<b.w/2+WEST_CROWN_CLEARANCE&&Math.abs(z-b.z)<b.d/2+WEST_CROWN_CLEARANCE)&&!colliders.some(c=>(c.westStore||c.westContainer)&&Math.abs(x-c.x)<c.w+WEST_CROWN_CLEARANCE&&Math.abs(z-c.z)<c.d+WEST_CROWN_CLEARANCE);}
export function addWestTree(world,x,z,r=2.4,options={}){
 const valid=(px,pz)=>westTreeClear(px,pz,world.colliders)&&!WEST_ROADS.some(b=>Math.abs(px-b.x)<b.w/2+1&&Math.abs(pz-b.z)<b.d/2+1)&&
  (options.fixed||!WEST_PATHS.some(b=>Math.abs(px-b.x)<b.w/2+.6&&Math.abs(pz-b.z)<b.d/2+.6))&&
  ![...WEST_BOTTLE_SPOTS,...WEST_LOCATIONS].some(p=>Math.hypot(px-p.x,pz-p.z)<2)&&
  !world.colliders.some(c=>Math.abs(px-c.x)<c.w+.5&&Math.abs(pz-c.z)<c.d+.5);
 let p=null;
 if(options.fixed){if(westTreeClear(x,z,world.colliders))p={x,z};}
 else for(let ring=0;ring<=6&&!p;ring++)for(let n=0;n<(ring?12:1);n++){const a=n*Math.PI/6,px=x+Math.cos(a)*ring*2,pz=z+Math.sin(a)*ring*2;if(valid(px,pz)){p={x:px,z:pz};break;}}
 if(!p)return null;
 const ground=options.fixed?options.ground:groundHeight(p.x,p.z);
 world.tree(p.x,p.z,r,{...options,ground});
 if(!options.distant)world.colliders.push({x:p.x,z:p.z,w:.22,d:.22,h:ground+6,westTree:true});
 return p;
}

const variants=new Map(),leafMaterials=[0x4c5836,0x667044,0x798052].map(color=>new THREE.MeshStandardMaterial({color,roughness:1,side:THREE.DoubleSide})),twigMaterial=new THREE.MeshStandardMaterial({color:0x655642,roughness:1});
const grassGeometry=new Map();let grassMaterial;
export function westWildGrass(parent,x,y,z,size=.7,seed=0){
 if(!grassMaterial){let map=null;if(typeof document!=='undefined'&&document.createElementNS){map=new THREE.TextureLoader().load('./assets/meadow-clumps.png');map.colorSpace=THREE.SRGBColorSpace;map.anisotropy=4;}grassMaterial=new THREE.MeshStandardMaterial({map,color:0xabb69a,alphaTest:.38,side:THREE.DoubleSide,roughness:1});}
 const cell=[1,5,6,11][Math.abs(seed)%4];if(!grassGeometry.has(cell)){const p=[-.6,0,0,.6,0,0,.6,1,0,-.6,1,0,0,0,-.6,0,0,.6,0,1,.6,0,1,-.6],geo=new THREE.BufferGeometry(),u=cell%3,v=3-Math.floor(cell/3);
  geo.setAttribute('position',new THREE.Float32BufferAttribute(p,3));geo.setAttribute('uv',new THREE.Float32BufferAttribute([0,0,1,0,1,1,0,1,0,0,1,0,1,1,0,1].map((n,i)=>(.025+n*.95+(i%2?v:u))/(i%2?4:3)),2));geo.setIndex([0,1,2,0,2,3,4,5,6,4,6,7]);geo.computeVertexNormals();grassGeometry.set(cell,geo);}
 const mesh=new THREE.Mesh(grassGeometry.get(cell),grassMaterial);mesh.position.set(x,y,z);mesh.rotation.y=seed*2.399;mesh.scale.setScalar(size);mesh.receiveShadow=true;mesh.userData.staticBatch=true;parent.add(mesh);return mesh;
}
function shrubGeometry(seed){
 if(variants.has(seed))return variants.get(seed);
 let state=619+seed;const rand=()=>((state=(Math.imul(state,1664525)+1013904223)>>>0)/4294967296),leaves=[[],[],[]],twigs=[];
 for(let branch=0;branch<9;branch++){
  const a=rand()*Math.PI*2,reach=.3+rand()*.5,tip=new THREE.Vector3(Math.cos(a)*reach,.38+rand()*.68,Math.sin(a)*reach);
  const rod=new THREE.CylinderGeometry(.008,.02,tip.length(),4);rod.applyMatrix4(new THREE.Matrix4().makeRotationFromQuaternion(new THREE.Quaternion().setFromUnitVectors(new THREE.Vector3(0,1,0),tip.clone().normalize())));rod.translate(tip.x/2,tip.y/2,tip.z/2);const points=rod.toNonIndexed().attributes.position;twigs.push(...points.array);rod.dispose();
  for(let n=0;n<48;n++){
   const t=.32+rand()*.68,center=tip.clone().multiplyScalar(t).add(new THREE.Vector3((rand()-.5)*.5,(rand()-.5)*.26,(rand()-.5)*.5));
   const yaw=rand()*Math.PI*2,size=.035+rand()*.045,along=new THREE.Vector3(Math.cos(yaw)*size,rand()*.018,Math.sin(yaw)*size),across=new THREE.Vector3(-Math.sin(yaw)*size*.48,.012,Math.cos(yaw)*size*.48),out=leaves[n%3];
   // Smaller oval leaves, six curved perimeter points and a shallow midrib.
   const p=[center.clone().sub(along),center.clone().addScaledVector(along,-.45).add(across),center.clone().addScaledVector(along,.45).add(across),center.clone().add(along),center.clone().addScaledVector(along,.45).sub(across),center.clone().addScaledVector(along,-.45).sub(across),center.clone().add(new THREE.Vector3(0,.012,0))];
   for(let i=0;i<6;i++)for(const j of [i,(i+1)%6,6])out.push(p[j].x,p[j].y,p[j].z);
  }
 }
 const make=v=>{const g=new THREE.BufferGeometry();g.setAttribute('position',new THREE.Float32BufferAttribute(v,3));g.computeVertexNormals();return g;};
 const result=[...leaves.map(make),make(twigs)];variants.set(seed,result);return result;
}
export function westShrub(parent,x,y,z,scale=1,seed=0){
 const g=new THREE.Group();g.name='Wild shrub · branches and leaves';g.userData.westRoot=true;g.position.set(x,y,z);g.rotation.y=seed*2.399;g.scale.set(scale,scale*(.8+(seed%3)*.1),scale);parent.add(g);
 shrubGeometry(Math.abs(Math.floor(seed))%4).forEach((geo,i)=>{const m=new THREE.Mesh(geo,i===3?twigMaterial:leafMaterials[i]);m.castShadow=true;m.receiveShadow=true;m.userData.staticBatch=true;g.add(m);});return g;
}
