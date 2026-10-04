import * as THREE from './vendor/three.module.js';

export const damageStage=value=>{const c=Number.isFinite(value)?Math.max(0,Math.min(100,value)):100;return c>=80?0:c>=60?1:c>=35?2:c>=16?3:c>=6?4:5;};
const hash=uid=>{let n=2166136261;for(const c of String(uid))n=Math.imul(n^c.charCodeAt(0),16777619);return n>>>0;};
const scratchGeo=new THREE.BufferGeometry();
const lines=[];for(let i=0;i<5;i++){const y=(i-2)*.034;lines.push(-.42,y,0,.33-i*.04,y+.045,0,.19,y+.057,0);}
scratchGeo.setAttribute('position',new THREE.Float32BufferAttribute(lines,3));scratchGeo.computeVertexNormals();
const dentGeo=new THREE.BufferGeometry(),dentVertices=[];
for(let i=0;i<9;i++){const a=i*Math.PI*2/9,b=(i+1)*Math.PI*2/9;dentVertices.push(0,0,0,Math.cos(a),Math.sin(a),.023,Math.cos(b),Math.sin(b),.023);}
dentGeo.setAttribute('position',new THREE.Float32BufferAttribute(dentVertices,3));dentGeo.computeVertexNormals();
const frustum=new THREE.Frustum(),viewProjection=new THREE.Matrix4(),bounds=new THREE.Sphere(new THREE.Vector3(),3.5);
const scuffGeo=new THREE.CircleGeometry(1,9),trimGeo=new THREE.BoxGeometry(1,1,1);
const dark=new THREE.MeshStandardMaterial({color:0x252a29,roughness:1,side:THREE.DoubleSide,polygonOffset:true,polygonOffsetFactor:-2});
const metal=new THREE.MeshStandardMaterial({color:0xc1c1b3,roughness:.77,side:THREE.DoubleSide,polygonOffset:true,polygonOffsetFactor:-3});
let smokeMap,smokeBase;
function smokeMaterial(){
 if(smokeBase)return smokeBase;
 if(typeof document!=='undefined'){
  const c=document.createElement('canvas');c.width=c.height=64;const ctx=c.getContext('2d'),gradient=ctx.createRadialGradient(32,32,2,32,32,31);
  gradient.addColorStop(0,'rgba(255,255,255,.65)');gradient.addColorStop(.45,'rgba(255,255,255,.4)');gradient.addColorStop(1,'rgba(255,255,255,0)');ctx.fillStyle=gradient;ctx.fillRect(0,0,64,64);smokeMap=new THREE.CanvasTexture(c);
 }
 smokeBase=new THREE.SpriteMaterial({map:smokeMap||null,transparent:true,depthWrite:false,color:0x565a56});return smokeBase;
}
function attach(mesh,v){
 const seed=hash(v.uid),side=seed%2?1:-1,L=v.id==='van'?2.4:v.id==='sport'?2.05:1.9;
 const root=new THREE.Group();root.name='Owned vehicle damage';mesh.add(root);
 const state={root,parts:[],particles:[],stage:-1,seed,time:0,puff:0,cooldown:0,L,side};
 const add=(parent,geo,material,x,y,z,sx,sy,sz,stage,ry=0)=>{const m=new THREE.Mesh(geo,material);m.position.set(x,y,z);m.scale.set(sx,sy,sz);m.rotation.y=ry;m.name='Damage overlay';parent.add(m);state.parts.push({mesh:m,stage});return m;};
 for(const s of [-1,1]){
  const door=s===1?mesh.userData.driverDoor:mesh.userData.passengerDoor;
  const offset=((seed>>>4)%9)*.018;
  add(door,scratchGeo,dark,s*.009,.78+offset,-.5,.78,1,1,s===side?1:2,s*Math.PI/2);
  add(door,scratchGeo,metal,s*.012,.79+offset,-.5,.64,.48,1,s===side?1:2,s*Math.PI/2);
  const dent=add(door,dentGeo,mesh.userData.paintMaterial,s*.014,.72,-.75,.16,.10,1,2,s*Math.PI/2);dent.rotation.z=.3*s;
  add(root,scratchGeo,metal,s*(v.id==='van'?.843:.952),v.id==='van'?1.33:.91,v.id==='van'?-1.38:-.83,.6,1,1,2,s*Math.PI/2);
  add(root,scuffGeo,dark,s*.952,.55,.1,.28,.045,1,4,s*Math.PI/2);
 }
 // Hood follows each model's existing sloping bonnet; marks never alter its geometry.
 const z=L-.5,y=(v.id==='sport'?.94:1.01)+((z-.64)/(L-.68))*((v.id==='sport'?.88:.94)-(v.id==='sport'?.94:1.01));
 const hood=add(root,scratchGeo,metal,side*.32,y+.012,z,.85,1,1,2);hood.rotation.x=-Math.PI/2+Math.atan2(.07,L-.68);
 for(const front of [-1,1]){
  const scuff=add(root,scuffGeo,dark,side*.52,.58,front*(L+.101),.3,.075,1,front===1?1:2,front===1?0:Math.PI);scuff.rotation.z=.13*side;
  const loose=add(root,trimGeo,dark,side*.59,.52,front*(L+.115),.52,.095,.065,2);loose.rotation.z=side*front*.10;
  const exposed=add(root,scratchGeo,metal,-side*.45,.65,front*(L+.107),.65,.7,1,4,front===1?0:Math.PI);exposed.rotation.z=.15*front;
 }
 // Housing-only masks leave the original lamp materials and dynamic lights intact.
 add(root,scuffGeo,dark,side*.6,.82,L+.072,.14,v.id==='sport'?.031:.054,1,3);
 add(root,scratchGeo,metal,side*.6,.82,L+.074,.28,.3,1,3);
 add(root,scuffGeo,dark,v.id==='van'?-side*.76:-side*.6,v.id==='van'?1.03:.85,-L-.069,v.id==='van'?.049:.11,v.id==='van'?.18:.05,1,4,Math.PI);
 add(root,trimGeo,dark,0,.5,L+.118,.67,.13,.035,5).rotation.z=.07*side;
 for(let i=0;i<10;i++){const material=smokeMaterial().clone(),sprite=new THREE.Sprite(material);sprite.visible=false;sprite.name='Pooled engine smoke';root.add(sprite);state.particles.push(sprite);}
 mesh.userData.vehicleDamage=state;return state;
}
export function impactVehicleDamage(mesh,speed,v){if(!mesh||v?.id==='bike'||speed<4)return;const d=mesh.userData.vehicleDamage||(v?attach(mesh,v):null);if(d&&d.cooldown<=0){d.puff=.42;d.cooldown=.8;}}
export function updateVehicleDamage(mesh,v,dt=0,nearby=true,camera=null){
 if(!mesh||v.id==='bike')return;
 const stage=damageStage(v.condition);let d=mesh.userData.vehicleDamage;
 if(!d&&(!stage||!nearby))return;
 if(!d)d=attach(mesh,v);
 if(stage!==d.stage){for(const p of d.parts)p.mesh.visible=stage>=p.stage;d.stage=stage;}
 // Repair clears all effects immediately, even while parked or outside update range.
 if(v.condition>d.lastCondition)d.puff=0;d.lastCondition=v.condition;
 if(!stage&&d.puff<=0){for(const p of d.particles)p.visible=false;return;}
 if(nearby&&camera){camera.updateMatrixWorld();frustum.setFromProjectionMatrix(viewProjection.multiplyMatrices(camera.projectionMatrix,camera.matrixWorldInverse));bounds.center.copy(mesh.position);nearby=frustum.intersectsSphere(bounds);}
 if(!nearby){for(const p of d.particles)p.visible=false;d.puff=0;return;}
 const step=Math.min(.1,Math.max(0,dt));d.time+=step;d.cooldown=Math.max(0,d.cooldown-step);d.puff=Math.max(0,d.puff-step);
 const c=v.condition,heavy=c<=5,critical=c<=15,steam=c<=35;
 for(let i=0;i<d.particles.length;i++){
  const p=d.particles[i],t=(d.time*.43+i/10+(d.seed%97)/97)%1;
  const burst=d.puff>0&&i<3;
  p.visible=burst||((critical||(steam&&i<2&&Math.sin(d.time*1.7+d.seed)>0))&&t>.02&&t<.96);
  if(!p.visible)continue;
  const size=(heavy?.3:critical?.22:.13)+t*(heavy?.78:.55);
  p.position.set(d.side*.29+Math.sin(i*2.4+d.time*.6)*t*.18,(v.id==='sport'?.96:1.05)+t*(heavy?2.1:1.6),d.L-.5-t*.3);
  p.scale.setScalar(burst?.28+(1-d.puff/.42)*.55:size);
  p.material.color.setHex(burst?0x999183:heavy?0x333833:critical?0x515651:0xb2b6ad);
  p.material.opacity=Math.sin(t*Math.PI)*(burst?.42:heavy?.7:critical?.53:.23*Math.max(0,Math.sin(d.time*1.7+d.seed)));
 }
}
