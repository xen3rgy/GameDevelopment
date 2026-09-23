import * as THREE from './vendor/three.module.js';
import {createBread} from './food-models.js?v=0.8.1';
import {CAFE_MENU} from './cafe.js?v=0.8.1';
const ceramic=new THREE.MeshStandardMaterial({color:0xf0e6d2,roughness:.26});
const latteCeramic=new THREE.MeshStandardMaterial({color:0x567967,roughness:.3});
const steel=new THREE.MeshStandardMaterial({color:0xbcc5bf,metalness:.75,roughness:.25});
const coffee=new THREE.MeshStandardMaterial({color:0x482516,roughness:.28});
const foam=new THREE.MeshStandardMaterial({color:0xe8c993,roughness:.85});
const round=new THREE.SphereGeometry(1,16,12);
const materials=new Map();
function mat(c){if(!materials.has(c))materials.set(c,new THREE.MeshStandardMaterial({color:c,roughness:.7}));return materials.get(c)}
function mesh(g,geo,material,x=0,y=0,z=0){const m=new THREE.Mesh(geo,material);m.position.set(x,y,z);m.castShadow=true;m.receiveShadow=true;g.add(m);return m}
function ellipsoid(g,x,y,z,sx,sy,sz,color){const m=mesh(g,round,mat(color),x,y,z);m.scale.set(sx,sy,sz);return m}
const cups=new Map();
function cupGeometry(radius,height){const key=radius+':'+height;if(!cups.has(key)){const p=[[0,0],[radius*.65,0],[radius*.84,.012],[radius,height*.85],[radius,height],[radius-.009,height],[radius-.012,.02],[0,.02]];cups.set(key,new THREE.LatheGeometry(p.map(([x,y])=>new THREE.Vector2(x,y)),24))}return cups.get(key)}
const plateGeo=new THREE.LatheGeometry([[0,0],[.16,0],[.20,.012],[.21,.024],[.20,.028],[.16,.011],[0,.011]].map(([x,y])=>new THREE.Vector2(x,y)),32);
export function createCafeMeal(recipe){
 const g=new THREE.Group();g.userData.dynamicCafe=true;g.userData.recipe=recipe;g.userData.portions=[];
 if(recipe==='breakfast'){
  mesh(g,plateGeo,ceramic);
  const food=new THREE.Group();g.add(food);g.userData.portions.push(food);
  const bread=createBread();bread.scale.set(.60,.65,.60);bread.position.set(-.09,.03,-.02);food.add(bread);
  const egg=new THREE.Group();g.add(egg);g.userData.portions.push(egg);ellipsoid(egg,.07,.037,.035,.088,.014,.065,0xf2ebd5);ellipsoid(egg,.074,.059,.027,.031,.020,.028,0xe9ae31);
  for(let i=0;i<3;i++){const p=new THREE.Group();g.add(p);g.userData.portions.push(p);ellipsoid(p,.045+i*.038,.035,-.108+i*.014,.030,.008,.032,0xbb4c35);ellipsoid(p,.045+i*.038,.045,-.108+i*.014,.019,.002,.020,0xdb7860)}
  const cheese=new THREE.Group();g.add(cheese);g.userData.portions.push(cheese);const slice=mesh(cheese,new THREE.BoxGeometry(.065,.012,.06),mat(0xe7c377),-.03,.032,.12);slice.rotation.y=.3;
  const fork=new THREE.Group();g.add(fork);mesh(fork,new THREE.CylinderGeometry(.006,.009,.12,8),steel,0,0,0);mesh(fork,new THREE.BoxGeometry(.025,.032,.009),steel,0,.075,0);for(const x of [-.009,0,.009])mesh(fork,new THREE.BoxGeometry(.004,.035,.006),steel,x,.105,0);
  fork.position.set(.23,.025,0);fork.rotation.x=Math.PI/2;
  const bite=ellipsoid(fork,0,.12,0,.022,.017,.02,0xe1b960);bite.visible=false;
  g.userData.movable=fork;g.userData.bite=bite;g.userData.grip=new THREE.Vector3(0,-.035,0);g.userData.height=0;
 }else{
  const latte=recipe==='latte',r=latte?.091:.064,h=latte?.16:.10;
  const saucer=mesh(g,plateGeo,ceramic);saucer.scale.setScalar(latte?.67:.52);
  const cup=new THREE.Group();cup.position.y=.016;g.add(cup);
  mesh(cup,cupGeometry(r,h),latte?latteCeramic:ceramic);
  const handle=mesh(cup,new THREE.TorusGeometry(latte?.042:.028,.009,8,18),latte?latteCeramic:ceramic,r+.024,h*.52,0);
  handle.scale.y=1.2;
  const liquid=mesh(cup,new THREE.CircleGeometry(r-.013,28),latte?foam:coffee,0,h*.89,0);liquid.rotation.x=-Math.PI/2;
  if(latte){for(let i=0;i<5;i++){const leaf=mesh(liquid,new THREE.CircleGeometry(.019-i*.002,16),ceramic,0,-.022+i*.010,.001);leaf.scale.set(1,.42,1)}}
  else{const crema=mesh(liquid,new THREE.RingGeometry(r*.52,r*.75,24),foam,0,0,.001);crema.rotation.z=.4}
  g.userData.movable=cup;g.userData.liquid=liquid;g.userData.grip=new THREE.Vector3(r+.044,h*.52,0);g.userData.height=h;
 }
 g.userData.restPosition=g.userData.movable.position.clone();g.userData.restQuaternion=g.userData.movable.quaternion.clone();return g;
}
export function setMealConsumed(meal,progress=0){
 progress=Math.max(0,Math.min(1,progress));const d=meal.userData;
 d.portions.forEach((m,i)=>m.visible=progress<((i+1)/d.portions.length));
 if(d.liquid){d.liquid.visible=progress<.98;d.liquid.position.y=d.height*(.89-progress*.65)}
}
export function resetCafeMeal(meal){const d=meal.userData;d.movable.position.copy(d.restPosition);d.movable.quaternion.copy(d.restQuaternion);if(d.bite)d.bite.visible=false;setMealConsumed(meal,0)}
const smooth=t=>{t=Math.max(0,Math.min(1,t));return t*t*(3-2*t)};
export function diningMotion(elapsed){
 const t=Math.max(0,elapsed)%6.5;
 const lift=t<1.2?0:t<2.3?smooth((t-1.2)/1.1):t<3.4?1:t<4.5?1-smooth((t-3.4)/1.1):0;
 const reach=t<1.2?smooth(t/1.2):t<4.5?1:t<5.5?1-smooth(t-4.5):0;
 return {lift,reach,bite:t>1.7&&t<3.4,tilt:t>2.3&&t<3.4?Math.sin((t-2.3)/1.1*Math.PI)*.30:0};
}
// Two-bone IK puts the right hand on the actual cup handle / fork grip.
export function reachCafeHand(actor,target,weight=1){
 const {upper,arms,elbows}=actor.userData,arm=arms[1],elbow=elbows[1];
 actor.updateMatrixWorld(true);const shoulder=arm.getWorldPosition(new THREE.Vector3()),hand=elbow.localToWorld(new THREE.Vector3(0,-.22,.026));
 const goal=hand.lerp(target,weight),delta=goal.clone().sub(shoulder),distance=Math.max(.025,Math.min(.507,delta.length())),direction=delta.normalize();
 const outward=new THREE.Vector3(1,-.15,0).applyQuaternion(actor.getWorldQuaternion(new THREE.Quaternion()));
 const bend=outward.addScaledVector(direction,-outward.dot(direction)).normalize(),a=.29,b=Math.hypot(.22,.026),cos=Math.max(-1,Math.min(1,(a*a+distance*distance-b*b)/(2*a*distance)));
 const joint=shoulder.clone().addScaledVector(direction,a*cos).addScaledVector(bend,a*Math.sqrt(1-cos*cos));
 const upperInverse=upper.getWorldQuaternion(new THREE.Quaternion()).invert(),armDirection=joint.clone().sub(shoulder).normalize().applyQuaternion(upperInverse);
 arm.quaternion.setFromUnitVectors(new THREE.Vector3(0,-1,0),armDirection);actor.updateMatrixWorld(true);
 const targetDirection=shoulder.clone().addScaledVector(direction,distance).sub(joint).normalize().applyQuaternion(arm.getWorldQuaternion(new THREE.Quaternion()).invert());
 elbow.quaternion.setFromUnitVectors(new THREE.Vector3(0,-.22,.026).normalize(),targetDirection);actor.updateMatrixWorld(true);
}
export function animateCafeMeal(actor,meal,guest){
 resetCafeMeal(meal);const spec=CAFE_MENU[guest.recipe],progress=guest.stage==='finished'||guest.stage==='leaving'?1:Math.max(0,1-guest.time/spec.dining);
 setMealConsumed(meal,progress);if(guest.stage!=='eating')return;
 const elapsed=Math.max(0,spec.dining-guest.time-(guest.id%3)*.35),m=diningMotion(elapsed),d=meal.userData,object=d.movable;
 // Lean towards the table while reaching, keeping the root and seated legs fixed.
 actor.userData.upper.rotation.x=.04+.40*m.reach*(1-m.lift);
 actor.updateMatrixWorld(true);meal.updateMatrixWorld(true);
 const rest=meal.localToWorld(d.restPosition.clone()),mouth=actor.localToWorld(new THREE.Vector3(d.recipe==='breakfast'?.03:-.03,d.recipe==='breakfast'?1.54:1.635-d.height,.245));
 object.position.copy(meal.worldToLocal(rest.lerp(mouth,m.lift)));
 if(d.recipe==='breakfast'){object.rotation.x=Math.PI/2-m.lift*.8;if(d.bite)d.bite.visible=m.bite}
 else object.rotation.x=-m.tilt;
 meal.updateMatrixWorld(true);const grip=object.localToWorld(d.grip.clone());reachCafeHand(actor,grip,m.reach);
}
