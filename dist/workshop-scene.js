import {reachCafeHand} from './cafe-food.js?v=0.8.1';
import {orderBike,BIKE_STYLES} from './workshop-orders.js?v=0.8.1';
import {applyWorkshopPose} from './workshop-gestures.js?v=0.8.1';
import {CUSTOMER_NAMES} from './workshop-life.js?v=0.8.1';
import {parcelHands} from './courier-scene.js?v=0.8.1';
import {surfaceMaterial} from './atmosphere.js?v=0.8.1';
import * as THREE from './vendor/three.module.js';
import {createCitizen} from './art.js?v=0.8.1';
import {animateCitizen} from './animation.js?v=0.8.1';
import {districtMaterial} from './district-materials.js?v=0.8.1';
import {WORKSHOP_POINTS,workshopWorkPoint,workshopApproach} from './workshop-layout.js?v=0.8.1';
import {workshopStep,workshopCarry} from './workshop.js?v=0.8.1';

// The room has its own visibility root: neither lights nor props leak into the city.
export class WorkshopScene{
 constructor(world,kit){
  this.world=world;this.kit=kit;const {box,cylinder,sign,mat}=kit;
  this.root=new THREE.Group();this.root.name='Werkstatt West';world.scene.add(this.root);
  const g=new THREE.Group();g.name='Workshop static fixtures';this.root.add(g);
  const steel=0x344b50,wood=0x927453,cream=0xc1b8a2;
  const floorMaterial=typeof document==='undefined'?mat(0x777b73):surfaceMaterial('concrete',18,18);floorMaterial.color.set(0x777b73);
  box(g,339,-.04,81,18,.22,18,0x777b73,floorMaterial);
  box(g,339,4.27,81,18,.24,18,0x525f5e);
  box(g,330,2.1,81,.3,4.2,18,cream);box(g,348,2.1,81,.3,4.2,18,cream);
  box(g,339,2.1,72,18,4.2,.3,0xffffff,districtMaterial('brick',18,4.2,0xb5a296));
  for(const [x,w] of [[334.4,8.8],[344.6,6.8]])box(g,x,2.1,90,w,4.2,.3,cream);
  box(g,340,3.6,90,2.4,1.2,.3,cream);
  for(const x of [330.17,347.83]){box(g,x,.62,81,.04,1.1,17.7,steel);box(g,x,1.22,81,.065,.07,17.7,0x9aa79b);}
  box(g,339,.62,72.18,17.7,1.1,.04,steel);
  // Real entrance opening, door surround, push bar and illuminated exit sign.
  this.door=new THREE.Group();this.root.add(this.door);box(this.door,340,1.5,90.04,2.25,2.85,.12,0x203a3e);box(this.door,340,1.92,89.96,1.85,1.55,.025,0x71938d);
  for(const x of [338.8,341.2])box(g,x,1.55,89.86,.09,3,.15,0xa6aca1);
  box(this.door,340,1.05,89.79,1.7,.06,.1,0xd0ccc0);sign(g,'AUSGANG',340,3.2,89.8,2.2,.35,'#e6f0db','#295949',Math.PI);
  // Windows are framed above the collision-free entrance aisle.
  for(const x of [332.4,336.1,344,346.2]){
   box(g,x,2.65,89.79,1.8,1.65,.08,steel);box(g,x,2.65,89.73,1.62,1.48,.04,0x9dafa4);
   box(g,x,2.65,89.68,.045,1.48,.04,steel);box(g,x,2.65,89.68,1.62,.045,.04,steel);
  }
  sign(g,'WEST / RAD & REPARATUR',339,3.26,72.22,8,.55,'#eadbc0','#304b4e');
  sign(g,'08–19 UHR · AUFTRÄGE BIS 17 UHR',339,2.64,72.22,6,.28,'#c7cdc0','#304b4e');
  const bench=(x,z,w,d)=>{for(const dx of [-w/2+.12,w/2-.12])for(const dz of [-d/2+.1,d/2-.1])box(g,x+dx,.53,z+dz,.07,.92,.07,steel);box(g,x,1.04,z,w,.1,d,wood);box(g,x,.31,z,w-.12,.05,d-.12,steel);};
  bench(339,74.5,4.4,1.3);bench(333.5,77.15,3.6,1.2);bench(336,81.1,1.7,1.1);
  box(g,333.6,.57,86.65,3.6,1,.95,steel);box(g,333.6,1.11,86.65,3.7,.09,1.2,wood);
  box(g,332.7,1.16,86.5,.5,.03,.38,0xdcd9c7);box(g,334.5,1.35,86.7,.52,.4,.08,0x222f33);
  box(g,334.5,1.35,86.64,.46,.32,.012,0x839c88);sign(g,'TESSA / AUFTRÄGE',333.6,.8,85.99,2.3,.28,'#e8dcc2','#304b4e',Math.PI);
  sign(g,'WARENEINGANG',333.5,1.6,77.1,2.65,.26);sign(g,'MONTAGE',339,1.65,74.65,2.1,.25);
  sign(g,'BESTAND & PRÜFUNG',336,1.47,81.05,1.6,.22);
  // Pegboard with individually mounted tools, safe rounded silhouettes and a vice.
  box(g,339,2,73.84,4.4,1.4,.08,0x697369);
  for(let i=0;i<14;i++)for(let j=0;j<4;j++)box(g,337+i*.3,1.53+j*.28,73.89,.018,.018,.008,0x273c3c);
  for(let i=0;i<7;i++){const x=337.2+i*.58;box(g,x,2.06,73.98,.055,.46,.06,0xb8b9a9);box(g,x,2.32,73.98,.17,.07,.06,0xb8b9a9);box(g,x,1.81,73.98,.085,.12,.07,i%2?0xa96d4d:0x315954);}
  box(g,340.5,1.18,74.83,.43,.2,.33,steel);box(g,340.5,1.33,74.83,.27,.13,.35,0x87918a);box(g,340.82,1.23,74.83,.4,.035,.035,0xb5b5a8);
  // Warehouse shelf, visible bins and compact product labels.
  for(const z of [73.8,76,78,80.2])box(g,346,.07+1.35,z,.98,2.7,.06,steel);
  for(const y of [.3,1.1,1.9,2.6])box(g,346,y,77,1.1,.065,6.5,steel);
  for(let row=0;row<3;row++)for(let i=0;i<6;i++){
   const z=74.35+i*1.06,y=.51+row*.8;box(g,345.96,y,z,.78,.36,.84,row===0?0xab8d5e:row===1?0x658077:0x647981);
   sign(g,row===0?'26 ZOLL':row===1?'28 ZOLL':i<2?'BELÄGE':i===2?'INBUS 5':i===3?'PUMPEN':'WERKZEUG',345.55,y,z,.76,.18,'#f2e8cd','#2c4246',-Math.PI/2);
  }
  sign(g,'ERSATZTEILE / WERKZEUG',345.4,2.88,77,4.2,.28,'#eeddbb','#304b4e',-Math.PI/2);
  for(let i=0;i<2;i++){const x=332.55+i*1.45;box(g,x,1.3,77.18,.62,.4,.68,0xb89765);box(g,x,1.505,77.18,.08,.013,.68,0xd4c099);box(g,x,1.3,77.531,.35,.2,.008,0xe0d9bb);}
  box(g,335.7,1.113,81.12,.48,.025,.65,0xe3decb);box(g,336.3,1.24,81.04,.4,.23,.43,steel);
  // Tall storage stays exactly within the corresponding collision fixture.
  box(g,331,1.25,81.7,1.1,2.35,2.6,0x576d6a);for(const z of [81.05,82.35]){box(g,331.57,1.25,z,.025,2.2,1.2,0x7f8980);box(g,331.62,1.2,z+.35,.035,.27,.035,0xd7ccad);}
  // Ceiling fixtures cast local, shadow-free light; capped at two point lights.
  const glow=new THREE.MeshBasicMaterial({color:0xffedc5});
  for(const z of [76,83,87])for(const x of [334.5,342.8]){
   box(g,x,4.08,z,2.4,.11,.3,steel);box(g,x,4.013,z,2.25,.025,.2,0xffffff,glow);
  }
  for(const [x,z] of [[335,78],[342,85]]){const light=new THREE.PointLight(0xffe1b3,35,14,2);light.position.set(x,3.65,z);this.root.add(light);}
  // Central repair stand and customer bicycle. The wheels stay dynamic for inspection.
  this.bike=new THREE.Group();this.bike.position.set(343.5,.07,80.8);this.root.add(this.bike);
  box(this.bike,0,.045,0,1.3,.09,.7,steel);cylinder(this.bike,0,.69,-.18,.04,1.3,0x8e9c9b);box(this.bike,0,1.3,-.04,.07,.08,.34,steel);
  const tube=(parent,a,b,r,color)=>{const A=new THREE.Vector3(...a),B=new THREE.Vector3(...b),delta=B.clone().sub(A);const m=cylinder(parent,...A.clone().add(B).multiplyScalar(.5).toArray(),r,delta.length(),color);m.quaternion.setFromUnitVectors(new THREE.Vector3(0,1,0),delta.normalize());return m;};
  const wheel=()=>{const w=new THREE.Group();const tire=new THREE.Mesh(new THREE.TorusGeometry(.37,.038,8,36),mat(0x253031));w.add(tire);const rim=new THREE.Mesh(new THREE.TorusGeometry(.332,.012,6,32),mat(0xbac2b7,.35,.7));w.add(rim);for(let i=0;i<12;i++){const t=i*Math.PI/6;tube(w,[0,0,0],[Math.cos(t)*.33,Math.sin(t)*.33,0],.004,0xa4afaa);}return w;};
  this.wheels=[wheel(),wheel()];this.wheels.forEach((w,i)=>{w.position.set(i===0?-.85:.85,.66,0);this.bike.add(w);});
  for(const [a,b] of [[[0,.75,0],[-.85,.66,0]],[[-.85,.66,0],[-.36,1.3,0]],[[-.36,1.3,0],[0,.75,0]],[[-.36,1.3,0],[.49,1.3,0]],[[.49,1.3,0],[0,.75,0]],[[.49,1.3,0],[.85,.66,0]]])tube(this.bike,a,b,.031,0xa4684b);
  tube(this.bike,[-.36,1.3,0],[-.4,1.49,0],.022,0xb9c0b5);box(this.bike,-.44,1.51,0,.3,.07,.21,0x29393b);
  tube(this.bike,[.49,1.3,0],[.43,1.61,0],.022,0xb9c0b5);tube(this.bike,[.43,1.61,-.28],[.43,1.61,.28],.02,0xaeb9b3);
  for(const z of [-.29,.29])box(this.bike,.43,1.61,z,.12,.05,.14,0x29393b);
  tube(this.bike,[0,.75,-.16],[0,.75,.16],.021,0x9da9a2);box(this.bike,.12,.7,.2,.24,.06,.16,steel);
  sign(g,'RAD / SICHERHEIT',343.5,2.23,79.98,2.5,.3);
  this.frameParts=this.bike.children.filter(v=>v.isMesh&&v.material.color?.getHex()===0xa4684b);for(const v of this.frameParts)v.material=v.material.clone();
  const bikeTemplate=this.bike.children.slice(3),bicycle=()=>{const root=new THREE.Group();for(const m of bikeTemplate){const copy=m.clone(true);if(this.frameParts.includes(m)){copy.material=m.material.clone();copy.userData.frame=true;}root.add(copy);}return root;};
  this.customers=new Map();this.customerActors=new Map();this.customerFactory=bicycle;
  this.parked=Array.from({length:3},(_,i)=>{const b=bicycle();b.position.set(346,-.182,84.5+i*1.3);this.root.add(b);return b;});
  this.basket=new THREE.Group();this.bike.add(this.basket);for(const z of [-.19,.19])box(this.basket,.78,1.31,z,.4,.24,.024,0x798d87);for(const x of [.58,.98])box(this.basket,x,1.31,0,.024,.24,.4,0x798d87);box(this.basket,.78,1.20,0,.42,.025,.42,0x798d87);
  this.carrier=new THREE.Group();this.bike.add(this.carrier);box(this.carrier,-.79,1.14,0,.47,.035,.24,steel);
  // Hinged cardboard flaps are dynamic; no coplanar lid is left beneath them.
  this.carton=new THREE.Group();this.carton.position.set(333.5,1.09,77.35);this.root.add(this.carton);box(this.carton,0,.025,0,.62,.05,.62,0xad895a);for(const x of [-.3,.3])box(this.carton,x,.22,0,.03,.4,.62,0xb89765);for(const z of [-.3,.3])box(this.carton,0,.22,z,.62,.4,.03,0xb89765);
  this.flaps=[-.3,.3].map(x=>{const f=new THREE.Group();f.position.set(x,.43,0);this.carton.add(f);box(f,-Math.sign(x)*.15,0,0,.30,.015,.62,0xc2a371);return f;});
  this.cartonContents=Array.from({length:12},(_,i)=>box(this.carton,-.18+(i%3)*.18,.13,-.21+Math.floor(i/3)*.14,.15,.15,.11,0x566e62));
  this.looseWheel=wheel();this.looseWheel.position.set(338.2,1.14,74.5);this.looseWheel.rotation.x=-Math.PI/2;this.root.add(this.looseWheel);this.looseWheel.visible=false;
  this.carryRoot=new THREE.Group();this.carryRoot.name='Workshop carried parts';world.scene.add(this.carryRoot);this.carryWheel=wheel();this.carryRoot.add(this.carryWheel);this.carryBox=new THREE.Group();this.carryRoot.add(this.carryBox);box(this.carryBox,0,0,0,.56,.38,.42,0xb89765);box(this.carryBox,0,.194,0,.07,.012,.42,0xd4c099);box(this.carryBox,0,0,.215,.3,.18,.012,0xe0d9bb);this.carryRoot.visible=false;
  this.nora=createCitizen(kit,0x365c59,0xc09173,2);this.nora.position.set(333.6,.02,87.65);this.nora.rotation.y=Math.PI;this.root.add(this.nora);
  this.staffBoard=new THREE.Group();this.nora.userData.elbows[1].add(this.staffBoard);this.staffBoard.position.set(0,-.22,.026);box(this.staffBoard,0,0,.05,.22,.028,.3,0x796547);box(this.staffBoard,0,.019,.05,.19,.008,.25,0xe1ddc9);
  // Work apron, fitted to the torso instead of a floating full-size accessory.
  box(this.nora.userData.upper,0,.30,.21,.42,.48,.04,0xb29668);box(this.nora.userData.upper,0,.24,.235,.23,.16,.018,0x8e7552);
  this.marker=new THREE.Mesh(new THREE.RingGeometry(.34,.39,40),new THREE.MeshBasicMaterial({color:0xe6bd78,transparent:true,opacity:.82,depthWrite:false,side:THREE.DoubleSide}));this.marker.rotation.x=-Math.PI/2;this.root.add(this.marker);
  this.tools=new THREE.Group();this.tools.name='Workshop hand tools';this.tools.visible=false;
  this.wrench=new THREE.Group();this.tools.add(this.wrench);box(this.wrench,0,0,.07,.035,.04,.26,0xb5c1b9);box(this.wrench,0,0,.2,.11,.04,.055,0xb5c1b9);for(const x of [-.044,.044])box(this.wrench,x,0,.235,.024,.04,.06,0xb5c1b9);
  this.package=new THREE.Group();this.tools.add(this.package);box(this.package,0,-.04,.07,.18,.12,.26,0xa18553);box(this.package,0,-.04,.205,.13,.085,.008,0xe0d6b7);
  this.hexKey=new THREE.Group();this.tools.add(this.hexKey);box(this.hexKey,0,0,.06,.016,.016,.17,0xb5c1b9);box(this.hexKey,.03,0,-.025,.07,.016,.016,0xb5c1b9);
  this.clipboard=new THREE.Group();this.tools.add(this.clipboard);box(this.clipboard,0,0,.05,.22,.028,.3,0x796547);box(this.clipboard,0,.019,.05,.19,.008,.25,0xe1ddc9);box(this.clipboard,0,.027,-.07,.09,.013,.028,0x9ca9a3);
  this.gauge=new THREE.Group();this.tools.add(this.gauge);cylinder(this.gauge,0,0,.1,.065,.035,0x3d585a);cylinder(this.gauge,0,.022,.1,.052,.008,0xe1ddc9);box(this.gauge,.018,.031,.1,.045,.005,.006,0x344e51);
  this.pump=new THREE.Group();this.root.add(this.pump);box(this.pump,0,.05,0,.36,.06,.23,steel);cylinder(this.pump,0,.38,0,.04,.65,0xa7754e);this.pumpHandle=new THREE.Group();this.pump.add(this.pumpHandle);cylinder(this.pumpHandle,0,.66,0,.018,.55,0xbac2b7);box(this.pumpHandle,0,.94,0,.38,.05,.07,steel);
  const hose=new THREE.CatmullRomCurve3([new THREE.Vector3(0,.18,0),new THREE.Vector3(.18,.08,-.24),new THREE.Vector3(.15,.31,-.42),new THREE.Vector3(.05,.47,-.45)]);this.pump.add(new THREE.Mesh(new THREE.TubeGeometry(hose,12,.012,5,false),mat(0x253031)));
  world.batchStaticGroup(g);
 }
 resetPose(player){if(!this.hadCarry&&!this.hadWork)return;this.hadWork=false;for(const v of [...player.userData.arms,...player.userData.elbows])v.rotation.set(0,0,0);}
 update(s,dt){
  this.root.visible=s.interior==='workshop';this.tools.visible=false;this.tools.quaternion.identity();this.hexKey.visible=false;this.clipboard.visible=false;this.gauge.visible=false;this.carryRoot.visible=false;this.pump.visible=false;
  const parcel=workshopCarry(s)==='supply';
  if(!this.root.visible){if(parcel){this.showCarried(s,'crate');}else this.hadCarry=false;return;}
  this.updatePeople(s,dt);
  const player=this.world.player,d=player.userData,a=s.workshop?.active,task=a?.action,current=workshopStep(s),target=current?workshopWorkPoint(a,current):WORKSHOP_POINTS.workshopDesk;
  if(!this.tools.parent){d.elbows[1].add(this.tools);this.tools.position.set(0,-.22,.026);}

  const approaching=!!task&&task.approachDuration!=null&&task.age<task.approachDuration;
  const point=approaching?target:task?null:a?.supply?.phase==='carrying'?WORKSHOP_POINTS.workshopIntake:a?.supply?.phase==='requested'&&a.step===1?WORKSHOP_POINTS.workshopExit:target;this.marker.visible=!!point;
  if(point){this.marker.position.set(point.x,.081,point.z);this.marker.scale.setScalar(1.25+.05*Math.sin((s.day*1440+s.minute)*.1));}
  const style=a?.revision===2?orderBike(a):BIKE_STYLES[0];for(const m of this.frameParts)m.material.color.set(style.color);this.basket.visible=style===BIKE_STYLES[0];this.carrier.visible=style===BIKE_STYLES[1];
  this.carton.visible=a?.type!=='stock'||a.step!==1;this.cartonContents.forEach((v,i)=>v.visible=a?.type==='stock'?a.step===0&&i<6+(a.variant%4)*2-a.variant%2:i<6);
  const opening=a?.type==='stock'&&(a.step>0||task?.correct)?a.step>0?1:Math.min(1,task.elapsed/task.duration*3):0;this.flaps[0].rotation.z=-opening*2;this.flaps[1].rotation.z=opening*2;
  const removed=a?.type==='tube'&&a.step>=1&&a.step<=3,chosen=a?.revision===2?a.variant%2===1?0:1:a?.variant?0:1,phase=task?task.elapsed/task.duration:0;
  this.wheels.forEach((w,i)=>{w.position.set(i===0?-.85:.85,.66,0);w.rotation.y=0;});
  this.wheels.forEach((w,i)=>{w.visible=!(removed&&i===chosen);if(task&&!approaching&&current?.point==='workshopBike'&&current.gesture==='test')w.rotation.z+=dt*1.7;});
  this.looseWheel.visible=removed&&!!task&&!approaching&&current?.point==='workshopBench';
  if(approaching){const here=workshopApproach(a,current,task.origin,task.age),previous=workshopApproach(a,current,task.origin,Math.max(0,task.age-dt));animateCitizen(player,dt,Math.hypot(here.x-previous.x,here.z-previous.z),{npc:true});player.rotation.y=here.angle;}
  const carry=workshopCarry(s);
  if(carry==='crate'||carry==='wheel'||carry==='supply'){this.carryRoot.visible=true;this.carryWheel.visible=carry==='wheel';this.carryBox.visible=carry==='crate'||carry==='supply';player.updateMatrixWorld(true);this.carryRoot.position.copy(player.localToWorld(new THREE.Vector3(0,1.12,.43)));this.carryRoot.quaternion.copy(player.quaternion);parcelHands(player,this.carryRoot);this.hadCarry=true;}
  else if(this.hadCarry){d.arms.forEach(v=>v.rotation.set(0,0,0));d.elbows.forEach(v=>v.rotation.set(0,0,0));this.hadCarry=false;}
  if(carry==='tool'){this.tools.visible=true;this.wrench.visible=false;this.hexKey.visible=a.revision!==2||a.variant%4<2;this.package.visible=a.revision===2&&a.variant%4===2;this.gauge.visible=a.revision===2&&a.variant%4===3;d.arms[1].rotation.x=-.42;d.elbows[1].rotation.x=-.45;this.hadCarry=true;if(a.revision===2&&a.variant%4===3){this.tools.visible=false;this.pump.visible=true;player.updateMatrixWorld(true);this.pump.position.copy(player.localToWorld(new THREE.Vector3(.3,.45,.35)));this.pump.quaternion.copy(player.quaternion);this.pumpHandle.position.y=-.15;this.pump.updateMatrixWorld(true);reachCafeHand(player,this.pumpHandle.localToWorld(new THREE.Vector3(0,.94,0)));}}
  if(!task||approaching)return;
  if(a.type==='tube'&&task.correct&&(a.step===0||a.step===3)){const wheel=this.wheels[chosen],move=a.step===0?Math.max(0,Math.min(1,(phase-.65)/.3)):1-Math.max(0,Math.min(1,(phase-.35)/.5));wheel.visible=true;wheel.position.x+=(target.x-this.bike.position.x-wheel.position.x)*move;wheel.position.z=move*.52;wheel.position.y=.66+move*.41;wheel.rotation.y=move*Math.PI;this.looseWheel.visible=false;}else this.wheels.forEach(w=>w.rotation.y=0);
  if(task.approachDuration==null&&task.age<.35){const f=t=>{t=Math.max(0,Math.min(1,t/.35));return t*t*(3-2*t);},distance=Math.hypot(task.origin.x-target.x,task.origin.z-target.z)*(f(task.age)-f(task.age-dt));animateCitizen(player,dt,distance,{npc:true});}
  player.rotation.y=target.angle;
  const motion=applyWorkshopPose(player,{...task,age:Math.max(0,task.age-(task.approachDuration??0))},current,target),settle=motion.weight;
  this.hadWork=true;this.tools.visible=settle>.1;this.wrench.visible=current.gesture==='wrench'||current.gesture==='collect'&&a.type==='check';this.hexKey.visible=this.wrench.visible&&a.type==='check';if(current.gesture==='collect'&&a.type==='check'&&a.revision===2&&a.variant%4>=2){this.wrench.visible=false;this.hexKey.visible=false;}if(this.hexKey.visible)this.wrench.visible=false;this.package.visible=['sort','collect'].includes(current.gesture)&&!this.wrench.visible&&!this.hexKey.visible;this.clipboard.visible=current.gesture==='inspect'&&current.point!=='workshopBike';this.gauge.visible=['test','pump'].includes(current.gesture)||current.gesture==='inspect'&&current.point==='workshopBike';
  if(a.type==='tube'&&task.correct&&(a.step===0||a.step===3)){const wheel=this.wheels[chosen],grip=Math.min(1,wheel.position.z/.15);if(grip>0){this.tools.visible=false;parcelHands(player,wheel,grip);}}
  if(current.gesture==='pump'){this.tools.visible=false;this.pump.visible=true;this.pump.quaternion.identity();this.pump.position.set(target.x-.2,.07,target.z-.51);this.pumpHandle.position.y=-.19+.14*motion.turn;this.pump.updateMatrixWorld(true);reachCafeHand(player,this.pumpHandle.localToWorld(new THREE.Vector3(0,.94,0)),settle);}
  if(current.point==='workshopBike'){player.updateMatrixWorld(true);const desired=new THREE.Quaternion().setFromEuler(new THREE.Euler(0,Math.PI,0));this.tools.quaternion.copy(d.elbows[1].getWorldQuaternion(new THREE.Quaternion()).invert().multiply(desired));}
 }
 showCarried(s,kind){const p=this.world.player;this.carryRoot.visible=true;this.carryWheel.visible=kind==='wheel';this.carryBox.visible=kind!=='wheel';p.updateMatrixWorld(true);this.carryRoot.position.copy(p.localToWorld(new THREE.Vector3(0,1.12,.43)));this.carryRoot.quaternion.copy(p.quaternion);parcelHands(p,this.carryRoot);this.hadCarry=true;}
 updatePeople(s,dt){
  const l=s.workshop?.life;
  this.parked.forEach((b,i)=>{const job=l?.parked[i];b.visible=!!job;if(job)for(const m of b.children)if(m.userData.frame)m.material.color.set(BIKE_STYLES[job.bike].color);});
  const doorOpen=(l?.people||[]).some(p=>p.z>87.6)||s.interior==='workshop'&&Math.hypot(s.position.x-340,s.position.z-89.1)<1.6;this.door.position.x+=( (doorOpen?2.3:0)-this.door.position.x)*(1-Math.exp(-dt*7));
  const staff=l?.tessa||{x:333.6,z:87.65};const distance=Math.hypot(this.nora.position.x-staff.x,this.nora.position.z-staff.z);if(distance>.0001)this.nora.rotation.y=Math.atan2(staff.x-this.nora.position.x,staff.z-this.nora.position.z);else if(staff.segment===0)this.nora.rotation.y=Math.PI;this.nora.position.set(staff.x,.02,staff.z);animateCitizen(this.nora,dt,dt?Math.min(distance,dt*1.1):0,{npc:true});this.staffBoard.visible=staff.phase===2;if(this.staffBoard.visible){this.nora.rotation.y=Math.PI/2;this.nora.userData.arms[1].rotation.x=-.95;this.nora.userData.elbows[1].rotation.x=-.7;}
  const ids=new Set((l?.people||[]).map(p=>p.id));for(const [id,v] of this.customers)if(!ids.has(id)){this.root.remove(v.actor,v.bike,v.label);v.label.geometry.dispose();v.label.material.map?.dispose();v.label.material.dispose();this.customers.delete(id);}
  for(const p of l?.people||[]){let v=this.customers.get(p.id);if(!v){let cached=this.customerActors.get(p.owner);if(!cached){cached={actor:createCitizen(this.kit,[0x766657,0x4e7375,0x7c695d][p.bike],0xc09173,p.owner+1),bike:this.customerFactory()};this.customerActors.set(p.owner,cached);}const {actor,bike}=cached,label=this.kit.sign(this.root,CUSTOMER_NAMES[p.owner]+(p.kind==='pickup'?' · ABHOLUNG':' · REPARATUR'),0,0,0,1.5,.18);this.root.add(actor,bike);for(const m of bike.children)if(m.userData.frame)m.material.color.set(BIKE_STYLES[p.bike].color);v={actor,bike,label,previous:{x:p.x,z:p.z}};this.customers.set(p.id,v);}
   const distance=Math.hypot(p.x-v.previous.x,p.z-v.previous.z);v.previous={x:p.x,z:p.z};v.actor.position.set(p.x,.02,p.z);v.actor.rotation.y+=Math.atan2(Math.sin(p.angle-v.actor.rotation.y),Math.cos(p.angle-v.actor.rotation.y))*(1-Math.exp(-dt*10));for(const joint of [...v.actor.userData.arms,...v.actor.userData.elbows])joint.rotation.set(0,0,0);animateCitizen(v.actor,dt,dt?Math.min(distance,dt*1.3):0,{npc:true});
   const facing=v.actor.rotation.y;v.bike.visible=p.withBike;v.bike.position.set(p.x+Math.cos(facing)*.48,-.182,p.z-Math.sin(facing)*.48);v.bike.rotation.y=facing-Math.PI/2;if(p.withBike){v.bike.updateMatrixWorld(true);reachCafeHand(v.actor,v.bike.localToWorld(new THREE.Vector3(.43,1.61,.29)));for(const w of v.bike.children.filter(o=>o.isGroup))w.rotation.z-=distance/.37;}
   v.label.position.set(p.x,2.08,p.z);v.label.quaternion.copy(this.world.camera?.quaternion||new THREE.Quaternion());
  }
 }
}
