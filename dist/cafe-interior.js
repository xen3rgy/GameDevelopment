import * as THREE from './vendor/three.module.js';
import {CAFE_TABLES,TABLE_RADIUS,CHAIR_OFFSET,STAFF_HOME,STAFF_ROLES,guestSeat,guestPath,pathLength,samplePath,guestWalkDuration} from './cafe-layout.js?v=0.7.3-map1';
import {CAFE_MENU,guestPlace} from './cafe.js?v=0.7.3-map1';
import {createCafeMeal,resetCafeMeal,setMealConsumed,animateCafeMeal,reachCafeHand} from './cafe-food.js?v=0.7.3-map1';
import {createCitizen} from './art.js?v=0.7.3-map1';
import {createBread} from './food-models.js?v=0.7.3-map1';
import {animateCitizen} from './animation.js?v=0.7.3-map1';
import {guestServiceView,paintGuestBadge} from './cafe-service-view.js?v=0.7.3-map1';
import {applyCafeGuestSignal,applyCafeWorkerPose,staffGesture} from './cafe-gestures.js?v=0.7.3-map1';
const oak=0x94704c,walnut=0x513b30,cream=0xe5dcc8,green=0x23473e,brass=0xba975a,leather=0xa56545;
function dynamic(group){group.userData.dynamicCafe=true;return group}
function cup(parent,k,x,y,z){k.cylinder(parent,x,y+.011,z,.125,.022,0xece6d6);k.cylinder(parent,x,y+.081,z,.074,.12,0xece6d6);k.cylinder(parent,x,y+.144,z,.060,.006,0x513125);const handle=new THREE.Mesh(new THREE.TorusGeometry(.039,.012,6,12),k.mat(0xece6d6));handle.position.set(x+.082,y+.09,z);parent.add(handle)}
function tray(k){const g=dynamic(new THREE.Group());k.box(g,0,0,0,.62,.035,.46,walnut);for(const z of [-.23,.23])k.box(g,0,.022,z,.64,.03,.02,oak);g.userData.meals={};for(const recipe of Object.keys(CAFE_MENU)){const meal=createCafeMeal(recipe);meal.position.y=.02;meal.visible=false;g.add(meal);g.userData.meals[recipe]=meal}return g}
function showTray(g,recipe){for(const [key,meal] of Object.entries(g.userData.meals)){meal.visible=key===recipe;if(meal.visible)resetCafeMeal(meal)}}

function plant(g,k,x,z){k.cylinder(g,x,.32,z,.26,.5,0x9b6447);k.cylinder(g,x,.58,z,.24,.025,0x463d2a);for(let i=0;i<9;i++){const a=i*2.4,r=.12+i%3*.045;const leaf=new THREE.Mesh(new THREE.SphereGeometry(.18,10,8),k.mat([0x385d3c,0x55754b,0x6e8751][i%3]));leaf.position.set(x+Math.sin(a)*r,.82+(i%3)*.14,z+Math.cos(a)*r);leaf.scale.set(.55,1.8,.45);leaf.rotation.z=Math.sin(a)*.5;g.add(leaf)}}
export function buildCafe(world,k){
 const {box,cylinder,sign,mat}=k,g=new THREE.Group();world.scene.add(g);world.cafe=g;world.cafeKit=k;
 const subfloor=box(g,300,-.10,80,16,.3,16,0x584738);subfloor.name='cafe-subfloor';
 // Narrow oak boards, staggered joints and restrained colour variation.
 for(let col=0;col<32;col++)for(let row=0;row<8;row++){const x=292.25+col*.5,z=73+row*2;const board=box(g,x,.064,z,.493,.012,1.99,[0x96744f,0x9d7b54,0x8e6c49,0xa17e56][(col*7+row*3)%4]);board.name='cafe-floorboard'}
 box(g,300,1.9,72,16,4,.25,cream);box(g,300,3.88,80,16,.18,16,0xd8ccb6);
 for(const x of [292,308]){box(g,x,1.9,80,.25,4,16,cream);box(g,x+(x<300?.15:-.15),.62,80,.065,1.1,15.8,green);box(g,x+(x<300?.20:-.20),1.21,80,.13,.07,15.8,oak)}
 // Front wall encloses the room; the entrance remains unmistakable.
 for(const x of [295.45,304.55])box(g,x,1.9,88,6.9,4,.25,cream);
 box(g,300,3.38,88,2.2,1,.25,cream);world.cafeDoor=world.exitDoor(g,300,88,'AUSGANG',true);
 box(g,300,.079,86.8,2.1,.016,1.3,0x3c5148);
 // Glazing is on the wall, not a freestanding obstacle in the dining room.
 world.cafeWindows=[];
 const windowMat=new THREE.MeshStandardMaterial({color:0x6d96a0,roughness:.23,metalness:.25,emissive:0x375f76,emissiveIntensity:.2});
 for(const x of [292.17,307.83])for(const z of [78.8,83]){
  const inward=x<300?1:-1;box(g,x,2.26,z,.04,1.65,2.65,0,windowMat);
  for(const dz of [-1.36,0,1.36])box(g,x+inward*.06,2.26,z+dz,.1,1.8,.06,walnut);
  for(const y of [1.36,2.29,3.16])box(g,x+inward*.06,y,z,.1,.065,2.78,walnut);
  box(g,x+inward*.11,1.34,z,.3,.10,2.85,oak);
 }world.cafeWindows.push(windowMat);
 // Low service counter, oak slats, stone top and a real machine silhouette.
 box(g,300,.60,74.8,7,1.06,1.4,walnut);box(g,300,1.12,74.8,7.1,.08,1.44,0xd0c5ae);
 for(let x=296.58;x<303.5;x+=.16)box(g,x,.59,75.51,.085,.89,.035,oak);
 box(g,300,.17,75.55,7,.045,.055,brass);
 const steel=new THREE.MeshStandardMaterial({color:0xabb5b2,metalness:.72,roughness:.26});
 box(g,300,1.46,74.6,1.4,.55,.64,green);box(g,300,1.47,74.965,1.30,.25,.075,0,steel);
 box(g,300,1.2,75.03,1.35,.05,.40,0,steel);box(g,300,1.77,74.6,1.46,.045,.68,0,steel);
 for(const x of [299.62,300.34]){cylinder(g,x,1.42,75.02,.075,.12,0x383e3b);box(g,x,1.4,75.18,.055,.055,.28,walnut);cup(g,k,x,1.225,75.06);cylinder(g,x,1.85,74.6,.075,.12,cream)}
 for(let i=0;i<6;i++)box(g,299.5+i*.20,1.232,75.05,.04,.013,.27,0x3b4541);
 box(g,301.25,1.38,74.65,.37,.44,.4,0x364440);cylinder(g,301.25,1.72,74.65,.16,.27,0x726048);
 box(g,301.8,1.26,74.9,.18,.22,.14,0,steel);
 // Pastries behind framed glass; they rest on trays above the worktop.
 const glass=new THREE.MeshStandardMaterial({color:0xc9e0d8,transparent:true,opacity:.14,roughness:.13,depthWrite:false,side:THREE.DoubleSide});
 box(g,297.7,1.18,74.9,1.65,.04,.7,walnut);
 for(const x of [296.9,298.5]){box(g,x,1.45,74.9,.03,.55,.72,brass);box(g,x,1.45,74.9,.012,.50,.68,0,glass)}
 box(g,297.7,1.73,74.9,1.65,.025,.72,0,glass);box(g,297.7,1.46,75.26,1.65,.54,.015,0,glass);
 for(let i=0;i<4;i++){const bread=createBread(i);bread.position.set(297.12+i*.38,1.21,74.96);bread.scale.setScalar(.72);g.add(bread)}
 // Back bar and readable menu face the room.
 box(g,300,.59,72.45,6.8,1.02,.55,green);box(g,300,1.12,72.45,6.9,.06,.64,oak);
 box(g,300,2.68,72.17,6.5,1.63,.08,green);
 sign(g,'CAFÉ MORGEN',300,3.16,72.23,5.8,.46,'#f2e6cc','#23473e');
 world.cafeMenu=dynamic(new THREE.Group());g.add(world.cafeMenu);updateMenu(world,1);
 for(const x of [294,306]){box(g,x,2.27,72.4,2.2,.09,.55,oak);for(let n=0;n<5;n++){cylinder(g,x-.75+n*.36,2.46,72.4,.12,.29,[0xebe0c9,0x977954,0x466252][n%3]);box(g,x-.75+n*.36,2.46,72.55,.13,.1,.01,cream)}}
 sign(g,'BESTELLUNGEN · ABHOLUNG',300,.76,75.54,2.8,.23,'#eadcbe','#513b30');
 box(g,293,.60,85,1,1.06,1.7,green);box(g,293,1.15,85,1.1,.06,1.8,oak);
 const book=box(g,293,1.2,85,.55,.05,.38,0xe3d7b9);book.rotation.y=.2;
 sign(g,'BETRIEBSBUCH',293.56,1.04,85,.85,.20,'#eadcbe','#23473e',Math.PI/2);
 for(const [i,p] of CAFE_TABLES.entries()){
  cylinder(g,p.x,.77,p.z,TABLE_RADIUS,.08,walnut);cylinder(g,p.x,.42,p.z,.075,.63,0x343d37);cylinder(g,p.x,.10,p.z,.31,.05,0x343d37);
  for(const side of [-1,1]){
   const x=p.x+side*CHAIR_OFFSET;
   box(g,x,.42,p.z,.55,.08,.55,leather);box(g,x+side*.275,.75,p.z,.065,.58,.55,leather);
   for(const dx of [-.22,.22])for(const dz of [-.22,.22]){const leg=cylinder(g,x+dx,.24,p.z+dz,.025,.34,walnut);leg.rotation.z=dx*.12}
   box(g,x,.38,p.z,.57,.04,.56,walnut);
  }
  // Small vase and menu leave clear space for both place settings.
  cylinder(g,p.x,.89,p.z-.31,.045,.16,0xd6bc88);cylinder(g,p.x,.99,p.z-.31,.008,.14,0x587748);
  const flower=new THREE.Mesh(new THREE.SphereGeometry(.038,8,6),mat(0xdcc6a4));flower.position.set(p.x,1.05,p.z-.31);g.add(flower);
  sign(g,String(i+1),p.x,.88,p.z+.25,.14,.15,'#efe3c9','#23473e');
  for(const side of [-1,1]){box(g,p.x+side*.43,.82,p.z+.32,.14,.02,.09,green);sign(g,side===-1?'A':'B',p.x+side*.43,.88,p.z+.365,.12,.1,'#efe3c9','#23473e')}
 }
 for(const [x,z] of [[293,73.3],[307,73.3],[307,86.5]])plant(g,k,x,z);
 // Wall detail belongs at the perimeter, leaving the central aisle clear.
 for(let z=76.1;z<86;z+=.48)for(const x of [292.21,307.79])box(g,x,.65,z,.055,.91,.035,0x557062);
 for(const x of [295,300,305]){
  cylinder(g,x,3.53,80,.012,.54,0x403d32);
  const shade=new THREE.Mesh(new THREE.CylinderGeometry(.22,.43,.32,24,1,true),new THREE.MeshStandardMaterial({color:brass,side:THREE.DoubleSide,roughness:.4,metalness:.2}));shade.position.set(x,3.14,80);g.add(shade);
  const bulb=new THREE.Mesh(new THREE.CircleGeometry(.39,24),new THREE.MeshBasicMaterial({color:0xffe0a6,side:THREE.DoubleSide}));bulb.position.set(x,2.98,80);bulb.rotation.x=Math.PI/2;g.add(bulb);
  const light=new THREE.PointLight(0xffdfb2,16,10,2);light.position.set(x,2.92,80);g.add(light);
 }
 world.cafeGuests=new Map();world.cafeGuestPool=[];world.cafeStaff=[];world.cafeDishes=[];
 for(const [i,p] of CAFE_TABLES.entries())for(const side of [-1,1]){
  const dish=dynamic(new THREE.Group());dish.rotation.y=guestSeat(i,side).angle;dish.position.set(p.x+side*.44,.815,p.z+side*.10);dish.userData.meals={};
  for(const recipe of Object.keys(CAFE_MENU)){const meal=createCafeMeal(recipe);if(recipe!=='breakfast')meal.position.z=-.09;meal.visible=false;dish.add(meal);dish.userData.meals[recipe]=meal}
  dish.visible=false;g.add(dish);world.cafeDishes.push(dish);
 }
 world.cafeReady=tray(k);world.cafeReady.position.set(300,1.19,75.04);world.cafeReady.visible=false;g.add(world.cafeReady);
}
export function attachCafeTray(world){world.cafeTray=tray(world.cafeKit);world.cafeTray.position.set(0,1.17,.5);world.player.add(world.cafeTray);world.cafeTray.visible=false;attachCafeTools(world,world.player)}
export function poseCafeSeated(actor,weight=1,eating=false){
 const d=actor.userData;weight=Math.max(0,Math.min(1,weight));
 // Hip and knee lengths match createCitizen. Shoe soles stay above the floor.
 actor.position.y=Math.max(.02-.27*weight,-.36+.38*Math.cos(1.3*weight));
 for(let i=0;i<2;i++){d.legs[i].rotation.x=-1.3*weight;d.legs[i].position.y=.9;d.knees[i].rotation.x=1.3*weight;d.feet[i].rotation.x=0;d.arms[i].rotation.set(-.35*weight,0,0);d.elbows[i].rotation.set(-.7*weight,0,0)}
 d.upper.position.y=.9;d.upper.rotation.set(.04*weight,0,0);
 if(eating&&weight===1){const t=d.animation?.time||0;d.upper.rotation.y=Math.sin(t*.7)*.05;d.arms[1].rotation.x-=Math.max(0,Math.sin(t*.8))*.12}
}
function citizen(world,variant){const k=world.cafeKit,m=createCitizen(k,[0x526e79,0x9e7156,0x61735c][variant%3],[0xb98566,0xd4ad8e,0x79543e][variant%3],variant+1);dynamic(m);world.cafe.add(m);return m}
function updateWalking(m,target,dt,options={}){
 const last=m.userData.last,d=last?Math.hypot(target.x-last.x,target.z-last.z):0;
 m.position.set(target.x,.02,target.z);
 if(d>.00001)m.rotation.y+=Math.atan2(Math.sin(target.angle-m.rotation.y),Math.cos(target.angle-m.rotation.y))*(1-Math.exp(-dt*16));
 m.userData.upper.rotation.y=0;for(const arm of m.userData.arms)arm.rotation.y=0;for(const elbow of m.userData.elbows){elbow.rotation.y=0;elbow.rotation.z=0}
 animateCitizen(m,dt,dt>0&&d<dt*8?d:0,{npc:true,...options});m.userData.last={x:target.x,z:target.z};
}
export function updateCafe(world,state,dt){
 if(!world.cafe)return;const c=state.cafe,active=state.interior==='cafe';world.cafe.visible=active;
 if(world.cafeTray)world.cafeTray.visible=active&&!!c?.carrying;
 if(world.player?.userData.cafeTools)showCafeTools(world.player,active?world.player.userData.gesture?.kind:null);
 if(!active)return;
 if(world.cafeDoor){const crossing=c?.employees?.some(e=>['entering','leaving'].includes(e.status)&&e.z>85.5)||c?.guests?.some(g=>['arriving','leaving'].includes(g.stage));world.cafeDoor.rotation.y+=(Number(!!crossing)*1.4-world.cafeDoor.rotation.y)*(1-Math.exp(-dt*8));}
 const ids=new Set(c?.guests?.map(g=>g.id)||[]);
 for(const [id,m] of world.cafeGuests)if(!ids.has(id)){m.visible=false;if(m.userData.badge)m.userData.badge.visible=false;world.cafeGuestPool.push(m);world.cafeGuests.delete(id)}
 world.cafeDishes.forEach((dish,place)=>{
  const guest=c?.guests.find(g=>guestPlace(g)===place),recipe=c?.dishes?.[place];
  dish.visible=!!recipe&&(!!c.dirty[place]||!!(guest?.served||guest?.paid));
  for(const [key,meal] of Object.entries(dish.userData.meals)){meal.visible=key===recipe;if(meal.visible){resetCafeMeal(meal);if(!guest||guest.stage!=='eating')setMealConsumed(meal,1)}}
 });
 if(c)for(const guest of c.guests){
  let m=world.cafeGuests.get(guest.id);
  if(!m){m=world.cafeGuestPool.pop()||citizen(world,guest.id);m.userData.last=null;m.userData.animation=null;world.cafeGuests.set(guest.id,m)}
  const side=guest.side||-1,path=guestPath(guest.seat,side),length=pathLength(path),total=guestWalkDuration(guest.seat,side),seat=guestSeat(guest.seat,side);
  let target=seat,sit=1;m.visible=true;
  if(guest.stage==='arriving'){const elapsed=total-guest.time,travel=length/1.3;if(elapsed<0)m.visible=false;target=samplePath(path,Math.min(length,Math.max(0,elapsed)*1.3));sit=Math.max(0,Math.min(1,(elapsed-travel)/.8))}
  if(guest.stage==='leaving'){const elapsed=Math.max(0,total-guest.time);sit=Math.max(0,1-elapsed/.8);target=samplePath([...path].reverse(),Math.max(0,elapsed-.8)*1.3)}
  updateWalking(m,target,dt);
  if(sit>0){m.rotation.y=seat.angle;poseCafeSeated(m,sit,guest.stage==='eating');if(sit===1)applyCafeGuestSignal(m,guest)}
  const dish=world.cafeDishes[guestPlace(guest)],meal=dish?.userData.meals[guest.recipe];
  updateGuestBadge(world,m,guest);
  if(meal&&(guest.served||guest.paid)&&['eating','finished'].includes(guest.stage))animateCafeMeal(m,meal,guest);
 }
 const trayGuest=c?.guests.find(g=>g.id===c.tray);
 if(world.cafeTray&&trayGuest)showTray(world.cafeTray,trayGuest.recipe);
 if(trayGuest)showTray(world.cafeReady,trayGuest.recipe);
 updateMenu(world,c?.phase==='open'?c.config?.price||1:state.businesses?.cafe?.price||1);
 const count=c?.employees?.length||0;
 for(let i=world.cafeStaff.length;i<count;i++){
  const m=citizen(world,10+i);m.userData.role=i;
  world.cafeKit.box(m.userData.upper,0,.20,.165,.34,.49,.025,green);
  world.cafeKit.sign(m,STAFF_ROLES[i],0,2.02,0,.65,.15,'#eddfc3','#23473e');
  m.userData.tray=tray(world.cafeKit);m.userData.tray.position.set(0,1.17,.5);m.add(m.userData.tray);attachCafeTools(world,m);world.cafeStaff.push(m);
 }
 world.cafeStaff.forEach((m,i)=>{
  m.visible=i<count&&c.employees[i]?.status!=='gone';if(!m.visible){m.userData.last=null;return;}
  const e=c?.employees?.[i],p=e||STAFF_HOME[i],task=e?.task,last=m.userData.last;
  const angle=last&&Math.hypot(p.x-last.x,p.z-last.z)>.00001?Math.atan2(p.x-last.x,p.z-last.z):i===0?0:Math.PI;
  updateWalking(m,{...p,angle},dt,{carrying:i===1&&c?.staffCarry!=null});
  m.userData.tray.visible=i===1&&c?.staffCarry!=null;const carriedGuest=c?.guests.find(g=>g.id===c.staffCarry);if(carriedGuest)showTray(m.userData.tray,carriedGuest.recipe);
  if(i===0&&e?.status==='working'){m.rotation.y=0;if(c?.prep){m.userData.arms[1].rotation.x=-1.1+Math.sin((m.userData.animation?.time||0)*5)*.08;m.userData.elbows[1].rotation.x=-.5}}
  const signal=staffGesture(e);if(signal&&signal.kind!=='cafeServe')applyCafeWorkerPose(m,signal.kind,signal.weight,signal.phase);showCafeTools(m,signal?.kind,signal);
  if(task&&!e.route.length){const p=CAFE_TABLES[task.seat];if(p)m.rotation.y=Math.atan2(p.x-e.x,p.z-e.z);if(task.kind==='clean'){m.userData.upper.rotation.x=.22;m.userData.arms[1].rotation.x=-.95+Math.sin((m.userData.animation?.time||0)*7)*.14}}
 });
 world.cafeReady.visible=c?.tray!=null&&!c.carrying;
 const day=Math.max(0,Math.sin((state.minute-360)/720*Math.PI));world.cafeWindows.forEach(m=>{m.emissiveIntensity=.03+day*.26;m.color.setHex(day>.15?0x71939a:0x1c3443)});
}

function updateMenu(world,price){
 if(world.cafeMenuPrice===price)return;world.cafeMenuPrice=price;
 for(const child of [...world.cafeMenu.children]){world.cafeMenu.remove(child);child.geometry?.dispose();child.material?.map?.dispose();child.material?.dispose()}
 const euro=n=>(Math.round(n*price)/100).toFixed(2).replace('.',',')+' €';
 world.cafeKit.sign(world.cafeMenu,'ESPRESSO  '+euro(CAFE_MENU.espresso.price)+'  ·  MILCHKAFFEE  '+euro(CAFE_MENU.latte.price),300,2.65,72.23,5.8,.28,'#e8dcc2','#23473e');
 world.cafeKit.sign(world.cafeMenu,'FRÜHSTÜCK  '+euro(CAFE_MENU.breakfast.price)+' / PERSON',300,2.24,72.23,5.6,.28,'#e8dcc2','#23473e');
}

function createGuestBadge(world){
 const g=dynamic(new THREE.Group());g.name='guest-patience';world.cafe.add(g);
 const panel=world.cafeKit.sign(g,'',0,0,0,1.48,.518,'#f7f4e9','#10292f');
 // One opaque surface retains depth testing against people and walls.
 if(panel.material){panel.material.transparent=false;panel.material.depthWrite=true;panel.material.toneMapped=false;panel.material.fog=false;panel.material.side=THREE.FrontSide}
 if(panel.material?.map){panel.material.map.generateMipmaps=false;panel.material.map.minFilter=THREE.LinearFilter;panel.material.map.magFilter=THREE.LinearFilter}
 g.userData={dynamicCafe:true,panel,key:null,view:null};return g;
}
function updateGuestBadge(world,actor,guest){
 const badge=actor.userData.badge??=createGuestBadge(world),view=guestServiceView(guest);
 badge.visible=actor.visible&&view.visible;
 if(!badge.visible)return;
 badge.position.set(actor.position.x,actor.position.y+2.22,actor.position.z);
 if(world.camera){badge.quaternion.copy(world.camera.quaternion);badge.scale.setScalar(Math.max(1,Math.min(1.35,badge.position.distanceTo(world.camera.position)/7)))}
 const d=badge.userData;d.view=view;
 if(d.key!==view.key){paintGuestBadge(d.panel.material?.map,view);d.key=view.key}
}

function attachCafeTools(world,actor){
 if(actor.userData.cafeTools)return;
 const k=world.cafeKit,left=actor.userData.elbows[0],right=actor.userData.elbows[1],book=dynamic(new THREE.Group()),bill=dynamic(new THREE.Group()),pen=dynamic(new THREE.Group());
 // Compact props stay attached to the palms throughout the gesture.
 book.position.set(0,-.23,.06);book.rotation.x=-.3;left.add(book);
 k.box(book,0,0,0,.17,.025,.22,green);k.box(book,0,.016,0,.145,.009,.194,0xf3e8cb);
 for(let z=-.06;z<=.06;z+=.03)k.box(book,0,.022,z,.11,.002,.002,0x988d71);
 bill.position.set(0,-.23,.06);bill.rotation.x=-.3;left.add(bill);
 k.box(bill,0,0,0,.18,.025,.24,walnut);k.box(bill,0,.017,0,.11,.007,.19,0xf4eddb);
 for(let z=-.055;z<.06;z+=.025)k.box(bill,0,.022,z,.075,.002,.002,0x8a8371);
 pen.position.set(0,-.22,.026);right.add(pen);k.box(pen,0,0,.025,.014,.014,.1,brass);
 actor.userData.cafeTools={book,bill,pen};showCafeTools(actor,null);
}
function showCafeTools(actor,kind,signal=null){
 const tools=actor.userData.cafeTools;if(!tools)return;
 tools.book.visible=kind==='cafeOrder';tools.pen.visible=kind==='cafeOrder';tools.bill.visible=kind==='cafePay';
 if(kind==='cafeOrder'){
  const gesture=actor.userData.gesture,phase=signal?.phase??(gesture?gesture.age/gesture.duration:0),weight=signal?.weight??Math.sin(Math.PI*phase);
  actor.updateMatrixWorld(true);const point=tools.book.localToWorld(new THREE.Vector3(Math.sin(phase*18)*.035,.025,.02));
  reachCafeHand(actor,point.clone().add(new THREE.Vector3(0,.075,0)),Math.max(0,Math.min(1,(weight-.3)/.4)));
  tools.pen.lookAt(point);
 }
}
