import * as THREE from './vendor/three.module.js';
import {frontagePatches,frontageSurface} from './frontage-layout.js?v=0.8.0';
import {streetSurface} from './street-layout.js?v=0.8.0';
import {surfaceMaterial,surfaceTileMeters} from './atmosphere.js?v=0.8.0';
export function buildFrontages(world){
 const vertices=[],sides=[],uv=[],tile=surfaceTileMeters('paving');
 const quad=(arr,a,b,c,d)=>arr.push(...a,...b,...c,...a,...c,...d);
 for(const p of frontagePatches()){
  const {minX:x,maxX:X,minZ:z,maxZ:Z}=p,a=[x,.3,z],b=[x,.3,Z],c=[X,.3,Z],d=[X,.3,z];quad(vertices,a,b,c,d);
  for(const [u,v,px,pz] of [[a,b,x-.001,(z+Z)/2],[b,c,(x+X)/2,Z+.001],[c,d,X+.001,(z+Z)/2],[d,a,(x+X)/2,z-.001]]){
   if(frontageSurface(px,pz)||streetSurface(px,pz))continue;
   quad(sides,u,[u[0],-.055,u[2]],[v[0],-.055,v[2]],v);
  }
 }
 for(let i=0;i<vertices.length;i+=3)uv.push(vertices[i]/tile,vertices[i+2]/tile);
 const root=new THREE.Group();root.name='Continuous building aprons';
 const material=surfaceMaterial('paving',tile,tile);material.map.repeat.set(1,1);
 for(const [name,positions,mat] of [['Frontage paving',vertices,material],['Frontage edge',sides,new THREE.MeshStandardMaterial({color:0x969e98,roughness:.94,side:THREE.DoubleSide})]]){
  const geo=new THREE.BufferGeometry();geo.setAttribute('position',new THREE.Float32BufferAttribute(positions,3));if(name==='Frontage paving')geo.setAttribute('uv',new THREE.Float32BufferAttribute(uv,2));geo.computeVertexNormals();geo.computeBoundingSphere();const mesh=new THREE.Mesh(geo,mat);mesh.name=name;mesh.receiveShadow=true;root.add(mesh);
 }
 world.scene.add(root);world.staticGroups.push(root);return root;
}
