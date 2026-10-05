import * as THREE from './vendor/three.module.js';
import {createCar} from './art.js?v=0.8.1';
import {TRAFFIC_TYPES} from './traffic.js?v=0.8.1';
import {SIGNAL_JUNCTIONS,signalPhase} from './traffic-signals.js?v=0.8.1';
import {groundHeight} from './spatial.js?v=0.8.1';
const glass=new THREE.MeshStandardMaterial({color:0x253e48,roughness:.2,metalness:.3});
const tyre=new THREE.MeshStandardMaterial({color:0x202425,roughness:.95}),rim=new THREE.MeshStandardMaterial({color:0x9ca6a5,roughness:.35,metalness:.7});
const wheelGeo=new THREE.CylinderGeometry(.44,.44,.23,16),hubGeo=new THREE.CylinderGeometry(.26,.26,.24,12);
export function createTrafficVehicle(kit,type,index=0){
 const {box,sign}=kit,colors=[0x4c6a79,0xb5afa0,0x934c3d,0x34534f,0xc5c4b6,0x414d67,0x977147];
 if(type!=='bus'){
  const g=createCar(kit,TRAFFIC_TYPES[type]?.model||'car',type==='taxi'?0xd9cda7:type==='works'?0xcb792d:colors[index%colors.length]);
  if(type==='taxi'){
   box(g,0,1.71,-.1,.62,.22,.28,0xf3d798);sign(g,'TAXI',0,1.72,.047,.55,.15,'#262c2f','#f3d798');
   for(const side of [-1,1])box(g,side*.945,.66,-.2,.02,.09,1.8,0x454e4d);
  }
  if(type==='van'||type==='works')for(const side of [-1,1])sign(g,type==='van'?'LINDEN · LOGISTIK':'STADTBAU',side*.96,1.42,-1.38,1.28,.33,'#f0e7d3',type==='van'?'#315257':'#613e28',side*Math.PI/2);
  if(type==='works'){
   for(const z of [-1.8,-.1])box(g,0,2.05,z,1.72,.07,.11,0x404c50);
   for(const x of [-.45,.45])box(g,x,2.13,-.9,.055,.06,2.25,0xc3c8c4);
   for(let z=-1.9;z<.2;z+=.35)box(g,0,2.14,z,.9,.05,.05,0xc3c8c4);
   box(g,0,2.13,.26,.55,.14,.24,0xe6a431);
   for(const side of [-1,1])box(g,side*.55,.59,-2.42,.45,.13,.025,0xf1e9d3);
  }
  g.userData.trafficType=type;return g;
 }
 const g=new THREE.Group(),b=(x,y,z,w,h,d,c,m)=>box(g,x,y,z,w,h,d,c,m);
 b(0,1.43,0,2.12,2.08,6.5,0x315e62);b(0,.73,0,2.17,.32,6.55,0xd8d0b9);b(0,2.53,0,2.06,.14,6.36,0xc9cec3);
 b(0,1.98,3.26,1.94,.98,.03,0,glass);b(0,1.96,-3.26,1.92,.8,.03,0,glass);
 for(const side of [-1,1])for(let z=-2.6;z<=2.6;z+=1.04)b(side*1.067,2.04,z,.025,.82,.9,0,glass);
 // Practical passenger doors on the kerb side, ventilation and low bumpers.
 for(const z of [2.1,-.8]){b(-1.085,1.34,z,.03,1.83,.95,0x243438);b(-1.106,1.76,z,.015,.85,.79,0,glass);b(-1.116,1.32,z,.012,1.73,.035,0xaebbb9);}
 b(0,.51,3.28,2.12,.18,.04,0x38484a);b(0,.51,-3.28,2.12,.18,.04,0x38484a);for(let y=.91;y<1.46;y+=.12)b(.45,y,-3.274,1,.045,.018,0x20383a);
 b(0,2.69,-.8,1.5,.2,1.7,0xaab7b6);sign(g,'B2 · STADTRING',0,2.39,3.29,1.85,.25,'#ffe3a0','#172828');
 const headlights=new THREE.MeshStandardMaterial({color:0xf7ead4,emissive:0xffedcc,emissiveIntensity:.15}),taillights=new THREE.MeshStandardMaterial({color:0x983a2e,emissive:0xff2812,emissiveIntensity:.1});
 for(const side of [-1,1]){b(side*.81,.88,3.285,.34,.15,.025,0,headlights);b(side*.91,.96,-3.285,.15,.43,.025,0,taillights);b(side*1.11,1.94,2.9,.1,.28,.12,0x3b484c);}
 g.userData.wheels=[];
 for(const x of [-.97,.97])for(const z of [-2.05,2.05]){const wheel=new THREE.Group();wheel.position.set(x,.44,z);for(const [geometry,material] of [[wheelGeo,tyre],[hubGeo,rim]]){const part=new THREE.Mesh(geometry,material);part.rotation.z=Math.PI/2;wheel.add(part);}g.add(wheel);g.userData.wheels.push(wheel);}
 g.userData.vehicleLights={headlights,taillights,front:3.3};g.userData.trafficType=type;return g;
}
const lensGeometry=new THREE.SphereGeometry(.09,8,6);
const off=new THREE.MeshBasicMaterial({color:0x26302b});
const lenses=Object.fromEntries(Object.entries({red:0xff3528,amber:0xffba32,green:0x49ef94}).map(([key,color])=>[key,new THREE.MeshBasicMaterial({color,toneMapped:false})]));
export class TrafficSignals{
 constructor(world,kit){
  this.root=new THREE.Group();this.root.userData.exterior=true;world.scene.add(this.root);this.heads=[];this.age=1;
  for(const junction of SIGNAL_JUNCTIONS)for(let n=0;n<4;n++){
   const angle=n*Math.PI/2,g=new THREE.Group();g.position.set(junction.x,groundHeight(junction.x,junction.z),junction.z);g.rotation.y=angle;this.root.add(g);
   kit.box(g,-7.1,1.75,-12,.1,3.5,.1,0x526360);kit.box(g,-7.1,3.24,-12,.32,.88,.27,0x253330);
   const bulbs=['red','amber','green'].map((color,i)=>{const mesh=new THREE.Mesh(lensGeometry,off);mesh.position.set(-7.1,3.51-i*.27,-12.15);g.add(mesh);return {color,mesh};});
   this.heads.push({junction,axis:n%2?'x':'z',bulbs});
   kit.box(g,-3.2,.019,-12,5.8,.018,.24,0xd8d6c3);
   // A simple straight arrow keeps the shared through/turn lane legible.
   kit.box(g,-2.8,.022,-18,.1,.018,1.35,0xc3c3b6);
   for(const side of [-1,1]){const arrow=kit.box(g,-2.8+side*.22,.022,-17.48,.08,.018,.62,0xc3c3b6);arrow.rotation.y=-side*Math.PI/4;}
   const x=junction.x+Math.cos(angle)*-7.1+Math.sin(angle)*-12,z=junction.z-Math.sin(angle)*-7.1+Math.cos(angle)*-12;
   world.colliders.push({x,z,w:.08,d:.08,h:3.7});
  }
 }
 update(time,inside,dt){this.root.visible=!inside;this.age+=dt;if(this.age<.1)return;this.age=0;for(const head of this.heads){const phase=signalPhase(head.junction,time,head.axis);for(const bulb of head.bulbs)bulb.mesh.material=bulb.color===phase?lenses[phase]:off;}}
}
