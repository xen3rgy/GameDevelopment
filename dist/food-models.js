import * as THREE from './vendor/three.module.js';
// Geometry is authored in metres. No unit-sphere scale is replaced after creation.
const materials=new Map();
const material=(color,roughness=.6)=>{const key=color+':'+roughness;if(!materials.has(key))materials.set(key,new THREE.MeshStandardMaterial({color,roughness}));return materials.get(key)};
const round=new THREE.SphereGeometry(1,20,14),stemGeo=new THREE.CylinderGeometry(.007,.009,.038,7),leafGeo=new THREE.SphereGeometry(1,8,6);
function mesh(parent,geo,mat,x=0,y=0,z=0){const m=new THREE.Mesh(geo,mat);m.position.set(x,y,z);m.castShadow=true;m.receiveShadow=true;m.userData.staticBatch=true;parent.add(m);return m}
const pepper=round.clone();const p=pepper.attributes.position;for(let i=0;i<p.count;i++){const x=p.getX(i),z=p.getZ(i),factor=1+.11*Math.cos(Math.atan2(z,x)*4);p.setXYZ(i,x*factor,p.getY(i),z*factor)}pepper.computeVertexNormals();
export function createProduce(type='tomato',variation=0){
 const g=new THREE.Group(),scale=1+(variation%4-1.5)*.035;
 if(type==='tomato'){
  mesh(g,round,material([0xb93b28,0xca4932,0xad3324][variation%3],.38),0,.087,0).scale.set(.10*scale,.078*scale,.097*scale);
  for(let n=0;n<5;n++){const angle=n*Math.PI*2/5,leaf=mesh(g,leafGeo,material(0x4c6d31),Math.sin(angle)*.026,.164,Math.cos(angle)*.026);leaf.scale.set(.012,.006,.035);leaf.rotation.y=angle}
  mesh(g,stemGeo,material(0x4a6533),0,.175,0).rotation.z=.18;
 }else if(type==='apple'){
  mesh(g,round,material([0x8caa42,0x789738,0xa3b952][variation%3],.43),0,.097,0).scale.set(.10*scale,.092*scale,.094*scale);
  mesh(g,stemGeo,material(0x6b4b2d),0,.194,0).rotation.z=-.22;
  const leaf=mesh(g,leafGeo,material(0x557a35),.023,.185,0);leaf.scale.set(.038,.006,.014);leaf.rotation.z=.24;
 }else{
  mesh(g,pepper,material([0xdeb345,0xd1a137,0xe3b844][variation%3],.42),0,.105,0).scale.set(.077*scale,.099*scale,.078*scale);
  mesh(g,stemGeo,material(0x4a7138),0,.219,0).rotation.z=.35;
 }
 return g;
}
const breadGeo=new THREE.SphereGeometry(1,24,16);breadGeo.scale(.125,.091,.23);breadGeo.translate(0,.099,0);
const colors=[],v=breadGeo.attributes.position;for(let i=0;i<v.count;i++){const y=v.getY(i),grain=Math.sin(i*9.31)*.04,col=new THREE.Color(0xb47b39).lerp(new THREE.Color(0xd5a460),Math.max(0,(y-.025)/.17)*.6+grain);colors.push(col.r,col.g,col.b)}breadGeo.setAttribute('color',new THREE.Float32BufferAttribute(colors,3));
const crust=new THREE.MeshStandardMaterial({vertexColors:true,roughness:.91}),cuts=[];
for(const z0 of [-.115,0,.115]){const points=[];for(let n=0;n<=10;n++){const x=-.074+n*.0148,z=z0+x*.30,y=.099+.091*Math.sqrt(Math.max(0,1-(x/.125)**2-(z/.23)**2))+.002;points.push(new THREE.Vector3(x,y,z))}cuts.push(new THREE.TubeGeometry(new THREE.CatmullRomCurve3(points),12,.004,4,false))}
export function createBread(variation=0){const g=new THREE.Group();mesh(g,breadGeo,crust);for(const cut of cuts)mesh(g,cut,material(0xead2a1,.96));g.rotation.y=(variation%3-1)*.045;return g}
