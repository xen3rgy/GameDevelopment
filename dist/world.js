import {WorkshopScene} from './workshop-scene.js?v=0.7.2-stability1';
import {WORKSHOP_POINTS,WORKSHOP_FIXTURES} from './workshop-layout.js?v=0.7.2';
import {CityCharacter,buildHorizonLinks} from './city-character-scene.js?v=0.7.2';
import {buildNeighborhoodDetails} from './neighborhood-scene.js?v=0.7.2';
import {buildPromenade} from './promenade-scene.js?v=0.7.2';
import {PedestrianScene} from './pedestrian-scene.js?v=0.7.2';
import {STREET_SEATS,streetSpawn} from './pedestrian-layout.js?v=0.7.2';
import {cityBackdrop} from './district-architecture.js?v=0.7.2';
import {buildStreets} from './street-scene.js?v=0.7.2';
import {stepJump} from './jump-motion.js?v=0.7.2';
import {buildStationDistrict} from './station-district.js?v=0.7.2';
import {WORLD_BOUNDS,exteriorContains} from './city-layout.js?v=0.7.2';
import {HomeScene} from './home-scene.js?v=0.7.2';
import {walkingProfile} from './player-movement.js?v=0.7.2';
import {deliveryTarget} from './delivery-routes.js?v=0.7.2';
import {CourierScene} from './courier-scene.js?v=0.7.2';
import {addStreetSigns} from './city-addresses.js?v=0.7.2';
import * as THREE from './vendor/three.module.js';
import {LOCATIONS,PEOPLE,VEHICLES,HOME_POINTS,SHOP_POINTS,WORK,BUILDINGS} from './data.js?v=0.7.2';
import {Traffic,parkedVehicleBlocks} from './traffic.js?v=0.7.2';
import {animateCitizen,playGesture,VehicleTransition} from './animation.js?v=0.7.2';
import {STREET_LAMPS,streetLampHead} from './lighting.js?v=0.7.2';
import {VehicleLights} from './vehicle-lights.js?v=0.7.2';
import {Atmosphere,surfaceMaterial} from './atmosphere.js?v=0.7.2';
import {approach,moveWithCollision,stepVehicle,segmentClear} from './movement.js?v=0.7.2';
import {CityNavigation,routeLength} from './navigation.js?v=0.7.2';
import {buildMarket} from './shop-interior.js?v=0.7.2';
import {buildCafe,attachCafeTray,updateCafe} from './cafe-interior.js?v=0.7.2';
import {CAFE_POINTS,CAFE_FIXTURES,guestLabel,cafeGuestIntent} from './cafe.js?v=0.7.2';
import {CityArt,createCitizen,createCar} from './art.js?v=0.7.2';
import {groundHeight,ROOMS,SHOP_FIXTURES,HOME_FIXTURES,homeFixtures,canWalkRoom,CameraRig} from './spatial.js?v=0.7.2';
const mats=new Map();
function mat(color,roughness=.8,metalness=0){const key=color+'_'+roughness+'_'+metalness;if(!mats.has(key))mats.set(key,new THREE.MeshStandardMaterial({color,roughness,metalness}));return mats.get(key)}
const boxGeo=new THREE.BoxGeometry(1,1,1),sphereGeo=new THREE.IcosahedronGeometry(1,1),cylinderGeo=new THREE.CylinderGeometry(1,1,1,8);
function box(parent,x,y,z,w,h,d,color,material){let m=new THREE.Mesh(boxGeo,material??mat(color));m.position.set(x,y,z);m.scale.set(w,h,d);m.castShadow=true;m.receiveShadow=true;parent.add(m);return m}
function cylinder(parent,x,y,z,r,h,color){let m=new THREE.Mesh(cylinderGeo,mat(color));m.position.set(x,y,z);m.scale.set(r,h,r);m.castShadow=true;parent.add(m);return m}
function sphere(parent,x,y,z,r,color){let m=new THREE.Mesh(sphereGeo,mat(color));m.position.set(x,y,z);m.scale.setScalar(r);m.castShadow=true;parent.add(m);return m}
function textureLabel(text,color='#ece3cc',bg='#202e30',w=1024,h=160){let c=document.createElement('canvas');c.width=w;c.height=h;let a=c.getContext('2d');a.fillStyle=bg;a.fillRect(0,0,w,h);a.fillStyle=color;a.font='600 '+Math.floor(h*.48)+'px Arial';a.textAlign='center';a.textBaseline='middle';a.fillText(text,w/2,h/2,w*.92);let t=new THREE.CanvasTexture(c);t.colorSpace=THREE.SRGBColorSpace;return t}
function sign(parent,text,x,y,z,w,h,color='#e9d9b6',bg='#243232',rotate=0){let m=new THREE.Mesh(new THREE.PlaneGeometry(w,h),new THREE.MeshBasicMaterial({map:textureLabel(text,color,bg),side:THREE.DoubleSide}));m.position.set(x,y,z);m.rotation.y=rotate;parent.add(m);return m}
const artKit={box,cylinder,sphere,sign,mat};
function person(color,skin,variant=0){return createCitizen(artKit,color,skin,variant)}

export class World{
 constructor(canvas,model){this.model=model;this.cameraRig=new CameraRig();this.lastInterior=model.s.interior;this.scene=new THREE.Scene();this.scene.background=new THREE.Color(0x97a4af);this.scene.fog=new THREE.FogExp2(0x97a4af,.006);this.camera=new THREE.PerspectiveCamera(55,innerWidth/innerHeight,.1,500);this.renderer=new THREE.WebGLRenderer({canvas,antialias:true,powerPreference:'high-performance'});this.renderer.setPixelRatio(Math.min(devicePixelRatio,1.5));this.renderer.setSize(innerWidth,innerHeight);this.renderer.shadowMap.enabled=true;this.renderer.shadowMap.type=THREE.PCFSoftShadowMap;this.renderer.toneMapping=THREE.ACESFilmicToneMapping;this.renderer.toneMappingExposure=1.04;this.staticGroups=[];this.colliders=[];this.angle=model.s.angle||0;this.pitch=.35;this.distance=8;this.jump=0;this.velocityY=0;this.elapsed=0;this.npcs=[];this.traffic=new Traffic();this.cars=[];this.transition=null;this.shadowAge=0;this.focusAge=0;this.bottles=[];this.trash=[];this.drops=[];this.lastDrops=null;this.vehicleMeshes=new Map();this.started=false;this.footstep=0;this.walkSpeed=0;this.jumpHeld=false;this.route=[];this.routeAge=0;this.routeKey="";this.routeOrigin=null;
 this.hemi=new THREE.HemisphereLight(0xaac8df,0x5c5949,2.4);this.scene.add(this.hemi);this.sun=new THREE.DirectionalLight(0xffc791,3.2);this.sun.position.set(-65,80,30);this.sun.castShadow=true;this.sun.shadow.mapSize.set(2048,2048);Object.assign(this.sun.shadow.camera,{left:-80,right:80,top:80,bottom:-80,near:1,far:250});this.sun.shadow.bias=-.0006;this.scene.add(this.sun);this.scene.add(this.sun.target);
 this.moon=new THREE.DirectionalLight(0xa9c9ef,0);this.scene.add(this.moon);this.scene.add(this.moon.target);this.vehicleLights=new VehicleLights(this.scene);this.atmosphere=new Atmosphere(this.scene,this.renderer);this.art=new CityArt(this,artKit);this.buildCity();this.batchStaticGroup(this.shop);this.batchStaticGroup(this.cafe);this.batchStatic();this.isolateWorld();this.homeScene=new HomeScene(this,artKit);this.workshopScene=new WorkshopScene(this,artKit);this.carMeshes=this.cars.map(c=>c.mesh);this.courierScene=new CourierScene(this,artKit);this.pedestrians=new PedestrianScene(this,artKit);this.npcs=this.pedestrians.life.people;this.cityCharacter.initResidents();this.cameraObstacles=[...this.colliders,...this.overheadColliders];this.navigator=new CityNavigation((x,z,r)=>this.canWalkExterior(x,z,r),4,WORLD_BOUNDS);this.routeLine=new THREE.Line(new THREE.BufferGeometry(),new THREE.LineBasicMaterial({color:0xdfc089,transparent:true,opacity:.48,depthWrite:false}));this.scene.add(this.routeLine);this.player=person();this.player.rotation.y=Math.PI;this.scene.add(this.player);this.carried=box(this.player,0,1.15,.49,.62,.52,.48,0xb99968);this.carried.visible=false;this.shoppingBasket=new THREE.Group();const basket=this.shoppingBasket;box(basket,0,0,0,.32,.22,.26,0x37584b);for(const x of [-.14,.14])box(basket,x,.2,0,.024,.25,.025,0xc4baa1);box(basket,0,.32,0,.30,.025,.025,0xc4baa1);basket.position.set(0,-.82,.04);this.player.userData.arms[0].add(basket);basket.visible=false;attachCafeTray(this);this.focusRing=new THREE.Mesh(new THREE.RingGeometry(.32,.36,24),new THREE.MeshBasicMaterial({color:0xf0cf8c,transparent:true,opacity:.8,side:THREE.DoubleSide}));this.focusRing.rotation.x=-Math.PI/2;this.scene.add(this.focusRing);if(!model.s.inside&&!model.s.riding)model.s.position=streetSpawn(model.s.position,(x,z)=>this.canWalkExterior(x,z));this.player.position.set(model.s.position.x,0,model.s.position.z);this.buildRain();this.resize=()=>{this.camera.aspect=innerWidth/innerHeight;this.camera.updateProjectionMatrix();this.renderer.setSize(innerWidth,innerHeight)};addEventListener('resize',this.resize);this.update(0,{},false);this.camera.position.copy(this.desiredCamera);this.camera.lookAt(this.player.position.clone().add(new THREE.Vector3(0,1.4,0)));this.renderer.render(this.scene,this.camera)}
 building(...args){return this.art.building(...args)}
 buildCity(){let s=this.scene;box(s,0,-.3,0,250,.5,250,0x65735e);buildStreets(this,artKit);
 const buildings=BUILDINGS;
 for(let b of buildings)this.building(...b);box(s,-36,1.55,-13.7,1.75,2.6,.18,0x182f32);box(s,-36,1.8,-13.58,1.4,1.8,.04,0x819b91);box(s,-35.45,1.25,-13.51,.06,.42,.06,0xdccdad);sign(s,'EINGANG',-36,2.85,-13.48,1.65,.3);box(s,-36,.31,-12.65,2,.025,1.4,0x354f49);
 cityBackdrop(this,artKit);
 // Trees and furnished pavements form distinct walkable blocks.
 for(let x of [-94,-53,-15,15,47,90])for(let z of [-10,10,-55,55])this.tree(x,z,2.4);
 for(let x of [54,66,82,95])for(let z of [83,99,111])this.tree(x,z,2.8);
 box(s,76,.08,93,39,.2,36,0x708061);box(s,76,.2,93,5,.15,36,0xb9ab93);box(s,76,.2,93,39,.15,4,0xb9ab93);
 buildStationDistrict(this,artKit);for(const {x,z} of STREET_LAMPS)this.lamp(x,z);for(const x of STREET_SEATS)this.bench(x,10);this.bench(65,77);this.bench(87,88);this.streetDetails();this.art.streetLife();addStreetSigns(this,artKit);buildNeighborhoodDetails(this,artKit);buildPromenade(this,artKit);this.cityCharacter=new CityCharacter(this,artKit);buildHorizonLinks(this,artKit);
 for(let x=-98;x<=98;x+=8){cylinder(s,x,.6,-7,.09,.85,0x434c4e);cylinder(s,x,.6,7,.09,.85,0x434c4e)}
 // Return machine is a physical interaction target.
 box(s,-23,1.2,-12,1.15,2.2,.65,0xc7cbc3);box(s,-23,1.25,-11.65,.85,1.65,.04,0x263d39);let opening=new THREE.Mesh(new THREE.TorusGeometry(.24,.055,8,20),new THREE.MeshStandardMaterial({color:0x82e2a8,emissive:0x42bf88,emissiveIntensity:1}));opening.position.set(-23,1.65,-11.59);s.add(opening);sign(s,'PFAND',-23,2.15,-11.58,.9,.25);box(s,-23,.9,-11.58,.6,.15,.07,0x86a797);
 for(const l of LOCATIONS){let ring=new THREE.Mesh(new THREE.RingGeometry(.65,.82,32),new THREE.MeshBasicMaterial({color:l.color,transparent:true,opacity:.55,side:THREE.DoubleSide}));ring.rotation.x=-Math.PI/2;ring.position.set(l.x,groundHeight(l.x,l.z)+.03,l.z);s.add(ring);l.ring=ring}
 for(let i=0;i<64;i++){let x,z;if(i<10){x=-31+(i%5)*3;z=-7+Math.floor(i/5)*4}else{x=-95+(i*31%190);z=[-9,9,-55,55,-74,74][i%6]+Math.sin(i*3)*1.5}let g=this.bottle(x,z);this.bottles.push({id:i,x,z,mesh:g})}
 this.cleanPositions=WORK.cleaning;
 for(let [x,z]of this.cleanPositions){let bag=sphere(s,x,.6,z,.42,0x253331);bag.scale.y*=1.2;this.trash.push(bag)}
 for(let i=0;i<4;i++){let x=-81+i*7;box(s,x,1.5,-75,2.5,2.6,1.5,0x696354);sign(s,['A','B','C','D'][i],x,2.9,-74.2,1,.7);for(let j=0;j<2;j++)box(s,x,1+j*.9,-74.5,2,.1,1,0xa5a393)}
 this.cratePos=WORK.crate;box(s,-85,.65,-73,1,1,1,0xab8756);sign(s,'SCAN',-85,1.5,-72.8,1.6,.4);
 this.shelves=WORK.shelves;
 for(const n of PEOPLE){let p=person(n.color,0xc09173,2);p.position.set(n.x,groundHeight(n.x,n.z)-.05,n.z);p.rotation.y=n.z<0?0:Math.PI;s.add(p);n.mesh=p}
 for(const car of this.traffic.cars){const i=car.id,g=createCar(artKit,i===4?'van':'car',[0x4c6a79,0xb5afa0,0x934c3d,0x34534f,0xc5c4b6,0x414d67,0x977147][i%7]);g.rotation.y=car.angle;g.position.set(car.x,groundHeight(car.x,car.z)-.03,car.z);s.add(g);car.mesh=g;this.cars.push(car)}
 // A furnished, physically walkable home interior, outside the exterior bounds.
 box(s,300,-.09,0,14,.3,12,0x82725b);box(s,300,1.7,-6,14,3.6,.25,0xc2b8a5);box(s,293,1.7,0,.25,3.6,12,0xb4aa98);box(s,307,1.7,0,.25,3.6,12,0xb4aa98);box(s,296,1.7,6,6,3.6,.25,0xb4aa98);box(s,304,1.7,6,6,3.6,.25,0xb4aa98);box(s,300,3.05,6,2,1,.25,0xb4aa98);this.exitDoor(s,300,6,'AUSGANG');box(s,296,.4,-3,3,.65,4,0x55493d);box(s,296,.85,-3,3,.25,4,0xbbb7a7);box(s,296,1,-4.4,2.4,.2,.8,0xe0d9c7);box(s,303,.8,-4.5,4,1.5,1,0x76786f);box(s,303,1.6,-4.5,4,.1,1.1,0xccbfa3);box(s,299,.55,1,2,.12,1.2,0xa18c67);box(s,298,.45,3,3,.7,1,0x647677);sign(s,'DEIN ZUHAUSE',300,2.4,-5.8,4,.55);this.interiorLight=new THREE.PointLight(0xffd9a5,28,18);this.interiorLight.position.set(300,3,0);s.add(this.interiorLight);this.homeDetails();this.art.homeFinish();this.buildShop();buildCafe(this,artKit);
 this.beacon=new THREE.Group();let beam=new THREE.Mesh(new THREE.CylinderGeometry(.15,.15,16,8),new THREE.MeshBasicMaterial({color:0xecc887,transparent:true,opacity:.4,depthWrite:false}));beam.position.y=8;this.beacon.add(beam);let diamond=new THREE.Mesh(new THREE.OctahedronGeometry(.5),new THREE.MeshBasicMaterial({color:0xf4d093}));diamond.position.y=3;this.beacon.add(diamond);s.add(this.beacon)
 }
 detailFacade(g,x,z,w,d,h,face,name){
  if(!name)return;const front=z+face*(d/2+.16);
  for(let y=6;y<h-1;y+=4){box(g,x,y,front,w+.2,.13,.28,0xb3ac99);for(let xx=x-w/2+3;xx<x+w/2-1;xx+=5){if(Math.round(xx+y)%3===0){box(g,xx,y-1.5,front+face*.4,2.3,.12,.9,0x88867c);box(g,xx,y-.95,front+face*.8,2.3,.05,.05,0x4a5859);for(let dx of [-1,-.5,0,.5,1])box(g,xx+dx,y-1.2,front+face*.8,.045,.55,.045,0x4a5859)}}}
  for(const dx of [-w/2+.2,w/2-.2])cylinder(g,x+dx,h/2,front,.045,h,0x626b68);
  box(g,x+w*.25,h+.65,z,.9,1.1,1.4,0x6f7775);box(g,x-w*.3,h+.8,z-2,1.2,1.4,1.4,0x8e8174);
 }
 streetDetails(){const s=this.scene;
  for(let i=0;i<22;i++){let x=-98+i*9;this.atmosphere.addPuddle(x,i%2?3.9:-4,1+(i%4)*.45,.4+(i%3)*.22)}

  // Outdoor seating, planting, a shelter and street-name signs give the block a human scale.
  for(let x of [57,69]){cylinder(s,x,.93,-11,.7,.1,0x9b8761);cylinder(s,x,.58,-11,.07,.7,0x4b5b57);for(let dx of [-1.1,1.1]){box(s,x+dx,.63,-11,.5,.12,.55,0x64705a);box(s,x+dx,.95,-11.25,.5,.56,.08,0x64705a)}}
  for(let x of [-91,48,96]){box(s,x,.62,10,2,.7,1,0x777b70);for(let dx of [-.55,0,.55])sphere(s,x+dx,1.1,10,.5,0x657d51)}
  box(s,-59,2.9,10,5.5,.16,2,0x3c5157);for(let x of [-61.5,-56.5])box(s,x,1.65,10,.08,2.6,.08,0x5c706e);box(s,-59,1.55,10.7,5.3,2.35,.07,0x516f76);sign(s,'LINDENPLATZ · TREFFPUNKT',-59,2.45,10.8,4.8,.32);this.bench(-59,9.9);
  for(let x of [-12,12]){cylinder(s,x,1.8,9,.05,3,0x596866);sign(s,'BAHNHOFSTRASSE',x,3.05,9,2.6,.3,'#e1e7df','#28454d')}
 }
 exitDoor(parent,x,z,label,animated=false){
  const leaf=box(parent,x,1.42,z-.06,1.92,2.7,.14,0x263f3c);
  for(const dx of [-1.04,1.04])box(parent,x+dx,1.45,z-.15,.12,2.85,.18,0xd0bba0);
  box(parent,x,2.88,z-.15,2.2,.12,.18,0xd0bba0);
  const glazing=box(parent,x,1.7,z-.15,1.45,1.5,.025,0x526c67);
  const handle=box(parent,x-.65,1.18,z-.24,.09,.34,.12,0xe3d4ac);
  sign(parent,label+'  →',x,3.13,z-.22,2.1,.32,'#dcecd9','#244b3d',Math.PI);
  const caption=sign(parent,'E · VERLASSEN',x,1.95,z-.19,1.45,.23,'#efe4cb','#526c67',Math.PI);
  box(parent,x,.085,z-.9,2,.025,1.2,0x344d46);
  if(animated){const hinge=new THREE.Group();hinge.userData.dynamicCafe=true;hinge.position.set(x-.96,0,z-.06);parent.add(hinge);for(const m of [leaf,glazing,handle,caption]){m.position.sub(hinge.position);hinge.add(m)}return hinge;}
 }
 buildShop(){buildMarket(this,artKit)}
 homeDetails(){const s=this.scene;this.homeDecor=new THREE.Group();s.add(this.homeDecor);
  // Separate interactions have separate furnishings, avoiding overlapping hot spots.
  box(this.homeDecor,295,.7,2.3,1.8,1.1,.75,0x766653);sign(this.homeDecor,'VORRÄTE',295,1.35,2.72,1.5,.22);
  box(this.homeDecor,303,1.7,-4.3,1,.04,.65,0x343e40);for(let x of [302.8,303.2])for(let z of [-4.1,-4.5])cylinder(this.homeDecor,x,1.74,z,.1,.025,0x9baba4);
  box(this.homeDecor,305.8,.45,2,1.5,.15,1.5,0xc5cfcb);box(this.homeDecor,306.5,1.55,2.6,.04,2.25,1.4,0x87a8a8);cylinder(this.homeDecor,306.4,2.6,2.4,.17,.07,0xabb5b0);
  this.homeExtras=new THREE.Group();s.add(this.homeExtras);box(this.homeExtras,302,.7,1.8,2.6,.13,1.2,0x655a4e);box(this.homeExtras,302,1.05,1.8,.8,.5,.08,0x22323c);box(this.homeExtras,302,.83,2,.8,.04,.6,0x75827e);box(this.homeExtras,301,.5,3.2,.75,.8,.75,0x637d78);
  this.homeLuxury=new THREE.Group();s.add(this.homeLuxury);box(this.homeLuxury,300,1.9,-5.8,4,2,.04,0x738e99);sign(this.homeLuxury,'LINDENSTADT',300,2.05,-5.74,3.5,.45,'#e2c799','#304d59');box(this.homeLuxury,298,.31,0,4,.04,3,0x647b77);
 }
 updateRoute(dt){
  this.routeAge+=dt;const s=this.model.s,t=this.target;const key=t?`${t.x},${t.z},${s.inside}`:'';
  if(!t||s.inside||s.riding){this.route=[];this.routeLine.visible=false;return}
  if(!this.route.length||key!==this.routeKey||this.routeAge>.6&&(!this.routeOrigin||Math.hypot(s.position.x-this.routeOrigin.x,s.position.z-this.routeOrigin.z)>2.5)){
    this.route=this.navigator.find(s.position,t);this.routeKey=key;this.routeOrigin={...s.position};this.routeAge=0;this.routeDistance=routeLength(this.route);
    this.routeLine.geometry.dispose();this.routeLine.geometry=new THREE.BufferGeometry().setFromPoints(this.route.map(p=>new THREE.Vector3(p.x,.37,p.z)));this.routeLine.visible=this.route.length>1;
  }
 }
 batchStaticGroup(root){
  const buckets=new Map();root.updateMatrixWorld(true);const inverse=root.matrixWorld.clone().invert();
  root.traverse(m=>{for(let p=m;p&&p!==root;p=p.parent)if(p.userData.dynamicCafe)return;if(!m.isMesh||!m.userData.staticBatch&&![boxGeo,sphereGeo,cylinderGeo].includes(m.geometry))return;const key=m.geometry.uuid+m.material.uuid;if(!buckets.has(key))buckets.set(key,[]);buckets.get(key).push(m)});
  for(const meshes of buckets.values()){if(meshes.length<3)continue;const inst=new THREE.InstancedMesh(meshes[0].geometry,meshes[0].material,meshes.length);meshes.forEach((m,i)=>{inst.setMatrixAt(i,new THREE.Matrix4().multiplyMatrices(inverse,m.matrixWorld));m.parent.remove(m)});inst.castShadow=true;inst.receiveShadow=true;root.add(inst)}
 }
 batchStatic(){const buckets=new Map(),exclude=new Set(this.trash);this.scene.updateMatrixWorld(true);const gather=m=>{if(!m.isMesh||exclude.has(m)||![boxGeo,sphereGeo,cylinderGeo].includes(m.geometry))return;const k=m.geometry.uuid+m.material.uuid+(m.matrixWorld.elements[12]>290?'home':'city')+m.castShadow;if(!buckets.has(k))buckets.set(k,[]);buckets.get(k).push(m)};for(const obj of [...this.scene.children])gather(obj);for(const group of this.staticGroups)group.traverse(gather);for(const meshes of buckets.values()){if(meshes.length<3)continue;let inst=new THREE.InstancedMesh(meshes[0].geometry,meshes[0].material,meshes.length);meshes.forEach((m,i)=>{inst.setMatrixAt(i,m.matrixWorld);m.parent.remove(m)});inst.castShadow=meshes[0].castShadow;inst.receiveShadow=true;inst.instanceMatrix.needsUpdate=true;this.scene.add(inst)}}
 isolateWorld(){this.exterior=new THREE.Group();this.homeShell=new THREE.Group();const bounds=new THREE.Box3();for(const object of [...this.scene.children]){if(object===this.atmosphere.sky||object===this.shop||object===this.cafe||object.isLight||(!object.isMesh&&!object.isGroup&&!object.isSprite))continue;bounds.setFromObject(object);if(bounds.isEmpty())continue;if(bounds.max.x<200)this.exterior.add(object);else if(bounds.min.x>290&&bounds.max.x<320)this.homeShell.add(object)}this.homeShell.add(this.interiorLight);this.scene.add(this.exterior,this.homeShell)}
 gesture(kind){playGesture(this.player,kind)}
 beginVehicleTransition(from,to,entering){this.transition=new VehicleTransition(from,to,entering,this.model.s.vehicle?.angle||0);this.jump=0;this.velocityY=0;this.walkSpeed=0}
 makeOwnedVehicle(id){if(this.vehicleMesh)this.vehicleMesh.visible=false;let g=this.vehicleMeshes.get(id);if(g){this.vehicleMesh=g;this.vehicleId=id;g.visible=true;return}g=new THREE.Group();if(id==='bike'){for(let z of [-.7,.7]){let wheel=new THREE.Mesh(new THREE.TorusGeometry(.4,.045,8,24),mat(0x252d30));wheel.rotation.y=Math.PI/2;wheel.position.set(0,.46,z);g.add(wheel);cylinder(g,0,.45,z,.035,.8,0xb6bdb8)}box(g,0,.73,0,.1,.16,1.25,0x5d9292);box(g,0,1.05,0,.08,.55,.1,0x5d9292);box(g,0,1.3,-.1,.35,.12,.42,0x293435);box(g,0,1.4,.68,.65,.08,.1,0xb3b6a9);box(g,0,1.04,.65,.08,.68,.08,0x566a6a)}else{g=createCar(artKit,id,id==='sport'?0x984f3b:id==='van'?0xb6bdba:0x447c84)}this.vehicleMeshes.set(id,g);this.vehicleMesh=g;this.vehicleId=id;this.scene.add(g)}
 tree(x,z,r){return this.art.tree(x,z,r)}
 lamp(x,z){const head=streetLampHead(x,z);cylinder(this.scene,x,2.7,z,.07,5,0x354444);box(this.scene,x+head.dx/2,5.16,z+head.dz/2,Math.abs(head.dx)+.10,.10,Math.abs(head.dz)+.10,0x354444);box(this.scene,head.x,5.2,head.z,.65,.2,.65,0x38494a);const bulb=new THREE.MeshBasicMaterial({color:0x293338});box(this.scene,head.x,5,head.z,.45,.25,.45,0,bulb).castShadow=false;this.atmosphere.registerLamp(head.x,head.z,bulb);if(x<-120){for(const dx of [-.25,.25])for(const dz of [-.25,.25])box(this.scene,head.x+dx,5.03,head.z+dz,.035,.43,.035,0x30423c);box(this.scene,head.x,4.8,head.z,.59,.075,.59,0x30423c);}else if(x>0){box(this.scene,head.x,5.22,head.z,1.05,.07,.45,0x71878a);}}
 bench(x,z){const y=groundHeight(x,z);box(this.scene,x,y+.47,z,2.2,.10,.6,0x7d6b4c);box(this.scene,x,y+.82,z+.3,2.2,.55,.1,0x7d6b4c);for(const dx of [-.8,.8])box(this.scene,x+dx,y+.22,z,.12,.44,.55,0x3d4746)}
 bottle(x,z){let g=new THREE.Group();cylinder(g,0,.19,0,.09,.35,0x4d8165);cylinder(g,0,.43,0,.036,.15,0x638f70);cylinder(g,0,.52,0,.04,.04,0xb1b397);g.rotation.z=.95;g.position.set(x,groundHeight(x,z)+.065,z);this.scene.add(g);return g}
 buildRain(){let pos=new Float32Array(1200*3);for(let i=0;i<pos.length;i+=3){pos[i]=Math.random()*100-50;pos[i+1]=Math.random()*40;pos[i+2]=Math.random()*100-50}let geo=new THREE.BufferGeometry();geo.setAttribute('position',new THREE.BufferAttribute(pos,3));this.rain=new THREE.Points(geo,new THREE.PointsMaterial({color:0xccdeea,size:.065,transparent:true,opacity:.6}));this.scene.add(this.rain)}
 canWalkExterior(x,z,radius=.35){if(!exteriorContains(x,z,radius))return false;return !this.colliders.some(c=>Math.abs(x-c.x)<c.w+radius&&Math.abs(z-c.z)<c.d+radius)}
 canMove(x,z,radius=.35){if(!this.model.s.inside&&!this.model.s.riding&&this.flight?.airborne&&groundHeight(x,z)>this.flight.y+.025)return false;if(this.model.s.inside)return canWalkRoom(this.model.s.interior,x,z,radius,this.model.s.home);const v=this.model.s.vehicle;return this.canWalkExterior(x,z,radius)&&!this.traffic.blocks(x,z,radius)&&!(v&&!this.model.s.riding&&parkedVehicleBlocks(v,x,z,radius,this.model.s.position))}
 update(dt,keys,active){let s=this.model.s;if(this.stationClock){this.stationClock.hour.rotation.z=-(s.minute%720)/720*Math.PI*2;this.stationClock.minute.rotation.z=-(s.minute%60)/60*Math.PI*2;}if(active)this.elapsed+=dt;let p=this.player;const delta=s.position;if(this.lastInterior!==s.interior||Math.hypot(p.position.x-delta.x,p.position.z-delta.z)>15){this.cameraRig.reset();this.flight=null;this.jump=0;this.velocityY=0;this.lastInterior=s.interior}if(Math.hypot(p.position.x-delta.x,p.position.z-delta.z)>.0001){p.position.x=delta.x;p.position.z=delta.z}
 let moving=false,running=false;const vehicle=s.vehicle;this.player.visible=!s.riding||vehicle?.id==='bike';if(vehicle){if(this.vehicleId!==vehicle.id)this.makeOwnedVehicle(vehicle.id);this.vehicleMesh.visible=true;this.vehicleMesh.position.set(vehicle.x,groundHeight(vehicle.x,vehicle.z)-(vehicle.id==='bike'?.015:.03),vehicle.z);this.vehicleMesh.rotation.y=vehicle.angle??Math.PI}else if(this.vehicleMesh)this.vehicleMesh.visible=false;
 let movedDistance=0;
 if(active&&!this.transition&&!s.job?.interaction&&!s.dailyLife?.action&&!s.workshop?.active?.action){
  const input={forward:keys.w||keys.ArrowUp,backward:keys.s||keys.ArrowDown,left:keys.a||keys.ArrowLeft,right:keys.d||keys.ArrowRight,brake:keys[' ']};
  if(s.riding&&vehicle){
   const result=stepVehicle(vehicle,input,dt,(x,z,r)=>this.canMove(x,z,r),VEHICLES[vehicle.id].speed);vehicle.angle=result.angle;vehicle.speed=result.speed;for(const wheel of this.vehicleMesh.userData.wheels||[])wheel.rotation.x+=result.distance*Math.sign(result.speed)/.37;this.vehicleMesh.userData.braking=!!input.brake;p.position.set(result.x,p.position.y,result.z);p.rotation.y=result.angle;movedDistance=result.distance;moving=movedDistance>.0001;
   if(moving){this.model.recordCourierTravel(movedDistance,vehicle);if(vehicle.id==='van'&&s.job?.type==='courier')s.job.vehicleUsed=true;vehicle.condition=Math.max(0,vehicle.condition-movedDistance*.0016);if(vehicle.id!=='bike')vehicle.fuel=Math.max(0,vehicle.fuel-movedDistance*.012);else s.needs.energy=Math.max(0,s.needs.energy-movedDistance*.008)}
  }else{
   let x=Number(!!input.right)-Number(!!input.left),z=Number(!!input.backward)-Number(!!input.forward),len=Math.hypot(x,z);const gait=walkingProfile(s,!!keys.Shift);running=gait.running;
   if(!this.flight?.airborne&&keys[' ']&&!this.flight?.held&&gait.canJump)this.flight=stepJump(this.flight,groundHeight(p.position.x,p.position.z,s.interior),0,true,true);
   const targetSpeed=len?gait.speed:0;this.walkSpeed=approach(this.walkSpeed,targetSpeed,dt*(len?gait.acceleration:25));if(gait.load)this.walkSpeed=Math.min(gait.speed,this.walkSpeed);
   if(len){x/=len;z/=len;const wx=x*Math.cos(this.angle)+z*Math.sin(this.angle),wz=-x*Math.sin(this.angle)+z*Math.cos(this.angle),result=moveWithCollision(p.position,wx*this.walkSpeed*dt,wz*this.walkSpeed*dt,(x,z,r)=>this.canMove(x,z,r));p.position.x=result.x;p.position.z=result.z;movedDistance=result.distance;moving=movedDistance>.0001;
    const target=Math.atan2(wx,wz);p.rotation.y+=Math.atan2(Math.sin(target-p.rotation.y),Math.cos(target-p.rotation.y))*Math.min(1,dt*12);if(moving){if(running)s.needs.energy=Math.max(0,s.needs.energy-movedDistance*.05*gait.sprintCost);s.skills.fitness+=movedDistance*.009}
   }
   this.flight=stepJump(this.flight,groundHeight(p.position.x,p.position.z,s.interior),dt,!!keys[' '],gait.canJump);this.jumpHeld=!!keys[' '];
  }
  this.velocityY=this.flight?.velocity||0;this.jump=this.flight?.airborne?Math.max(.001,this.flight.y-groundHeight(p.position.x,p.position.z,s.interior)):0;
  s.position={x:p.position.x,z:p.position.z};s.angle=this.angle;
 }else{this.walkSpeed=0;this.jumpHeld=false;if(vehicle)vehicle.speed=0}
 if(s.riding&&vehicle){vehicle.x=p.position.x;vehicle.z=p.position.z;this.vehicleMesh.position.set(vehicle.x,groundHeight(vehicle.x,vehicle.z)-(vehicle.id==='bike'?.015:.03),vehicle.z);this.vehicleMesh.rotation.y=vehicle.angle??p.rotation.y}if(s.riding||this.transition||s.job?.interaction||s.dailyLife?.action||s.workshop?.active?.action){this.flight=null;this.jump=0;}p.position.y=(this.flight?.airborne?this.flight.y:groundHeight(p.position.x,p.position.z,s.interior))-.05+(s.riding&&vehicle?.id==='bike'?.55:0);
 const transition=this.transition?.update(active?dt:0);if(transition){p.position.x=transition.x;p.position.z=transition.z;p.position.y=groundHeight(p.position.x,p.position.z)-.05;p.rotation.y=transition.angle;this.player.visible=transition.visible;if(this.vehicleMesh?.userData.door)this.vehicleMesh.userData.door.rotation.y=transition.door;if(transition.done){this.transition=null;p.position.set(s.position.x,groundHeight(s.position.x,s.position.z)-.05,s.position.z);this.player.visible=!s.riding||vehicle?.id==='bike'}}
 this.workshopScene.resetPose(p);animateCitizen(p,active?dt:0,movedDistance,{running,carrying:!!s.job?.carrying||!!s.cafe?.carrying||s.job?.type==='courier'&&!s.riding&&!s.inside,basket:s.interior==='shop'&&s.basket.length>0,riding:s.riding&&vehicle?.id==='bike',entering:!!transition,grounded:this.jump===0});this.shoppingBasket.visible=s.interior==='shop'&&s.basket.length>0;

 this.homeScene.update(s,dt);this.workshopScene.update(s,active?dt:0);
 const view=this.cameraRig.update(this.homeScene.cameraAnchor||p.position,this.angle,this.pitch,this.distance,dt,s.interior==='shop'?SHOP_FIXTURES:s.interior==='cafe'?CAFE_FIXTURES:s.interior==='home'?homeFixtures(s.home):s.interior==='workshop'?WORKSHOP_FIXTURES:this.cameraObstacles,ROOMS[s.interior]);
 this.desiredCamera=new THREE.Vector3(view.position.x,view.position.y,view.position.z);this.camera.position.copy(this.desiredCamera);this.camera.lookAt(view.anchor.x,view.anchor.y,view.anchor.z);
 this.player.visible=this.player.visible&&view.distance>.7;
 this.pedestrians.update(s,active?dt:0,p.position);
 if(active){
  const previous=this.previousPedestrian||s.position;const pedestrians=s.inside||s.riding?[]:[{...s.position,vx:(s.position.x-previous.x)/Math.max(dt,.001),vz:(s.position.z-previous.z)/Math.max(dt,.001)}];this.previousPedestrian={...s.position};pedestrians.push(...this.pedestrians.life.trafficPeople());
  this.traffic.update(dt,pedestrians,vehicle?{x:vehicle.x,z:vehicle.z,angle:vehicle.angle,length:vehicle.id==='van'?4.8:vehicle.id==='bike'?1.8:3.8,width:vehicle.id==='bike'?.65:1.85}:null);
  for(const c of this.cars){c.mesh.position.set(c.x,groundHeight(c.x,c.z)-.03,c.z);c.mesh.rotation.y=c.angle;c.mesh.userData.braking=c.braking;for(const wheel of c.mesh.userData.wheels||[])wheel.rotation.x+=c.speed*dt/.37}
 }
 this.exterior.visible=!s.inside;this.homeShell.visible=s.interior==='home';

 this.homeExtras.visible=s.interior==='home'&&['flat','penthouse'].includes(s.home);this.homeLuxury.visible=s.interior==='home'&&s.home==='penthouse';this.homeDecor.visible=s.interior==='home';this.shop.visible=s.interior==='shop';updateCafe(this,s,active?dt*s.settings.speed:0);
 this.bottles.forEach(b=>b.mesh.visible=!s.collected.includes(b.id)&&!s.inside);
 this.trash.forEach((mesh,i)=>{mesh.visible=s.job?.type==='cleaning'&&i>=s.job.progress;mesh.material=mat(i===s.job?.progress?0x5e7553:0x253331)});
 if(s.drops!==this.lastDrops){for(let d of this.drops)this.scene.remove(d.mesh);this.drops=s.drops.map((d,i)=>{let mesh=d.id==='bottle'?this.bottle(d.x,d.z):box(this.scene,d.x,groundHeight(d.x,d.z)+.25,d.z,.45,.5,.45,0xb99b64);return{...d,index:i,mesh}});this.lastDrops=s.drops}
 const light=this.atmosphere.update(s,p.position,active?dt:0);this.moon.intensity=light.moon;this.moon.position.set(p.position.x+45,65,p.position.z+20);this.moon.target.position.set(p.position.x,0,p.position.z);this.vehicleLights.update(this.carMeshes,s.vehicle&&this.vehicleMesh?.visible?this.vehicleMesh:null,p.position,light.lamp,dt,s.inside,s.settings.quality,s.riding);this.hemi.intensity=light.hemisphere;this.hemi.color.set(s.inside?0xd8e4e6:0x9dbce3);this.sun.intensity=light.sun;this.sun.castShadow=s.settings.quality!=='low'&&light.sun>.03;this.sun.position.set(p.position.x-60,15+light.altitude*85,p.position.z-40);this.sun.target.position.set(p.position.x,0,p.position.z);this.scene.background.copy(this.scene.fog.color);this.rain.visible=s.weather==='rain'&&!s.inside;if(this.rain.visible){this.rain.position.set(p.position.x,0,p.position.z);let a=this.rain.geometry.attributes.position;for(let i=0;i<a.count;i++){a.array[i*3+1]-=dt*22;if(a.array[i*3+1]<0)a.array[i*3+1]=40}a.needsUpdate=true}
 if(this.target&&!s.inside){this.beacon.visible=true;this.beacon.position.set(this.target.x,0,this.target.z);this.beacon.children[1].rotation.y=this.elapsed;this.beacon.children[1].position.y=3+Math.sin(this.elapsed*2)*.2}else this.beacon.visible=false;
 this.courierScene.update(s,active?dt:0);this.carried.visible=!!s.job?.carrying&&!s.riding;this.focusAge+=dt;if(this.focusAge>.12){this.focusAge=0;this.focusTarget=this.nearest()}const focused=this.focusTarget;this.focusRing.visible=!!focused&&!s.riding&&!s.dailyLife?.action&&!s.workshop?.active?.action;if(focused)this.focusRing.position.set(focused.x,groundHeight(focused.x,focused.z,s.interior)+.025,focused.z);this.art.update(light.night);this.cityCharacter.update(s,active?dt:0,p.position);this.sun.color.set(0xfff0d5).lerp(new THREE.Color(0xffb778),light.dusk*.65);this.updateRoute(dt);this.shadowAge+=dt;this.renderer.shadowMap.autoUpdate=false;this.renderer.shadowMap.needsUpdate=this.shadowAge>1/30;if(this.renderer.shadowMap.needsUpdate)this.shadowAge=0;this.renderer.render(this.scene,this.camera);return {moving,running,distance:movedDistance,grounded:this.jump===0}
 }
 nearest(){const s=this.model.s,p=s.position;let candidates=[];if(s.riding)return{type:'vehicle',name:'Aussteigen · '+VEHICLES[s.vehicle.id].name,x:p.x,z:p.z,dist:0};if(s.vehicle&&!s.inside)candidates.push({type:'vehicle',name:'Einsteigen · '+VEHICLES[s.vehicle.id].name,x:s.vehicle.x,z:s.vehicle.z});
 if(s.inside){
 if(s.interior==='cafe'&&s.cafe?.phase==='open'&&this.camera&&this.cafeGuests){
  const focused=[];
  for(const g of s.cafe.guests){
   const action=cafeGuestIntent(s.cafe,g),actor=this.cafeGuests.get(g.id),point=CAFE_POINTS['cafeTable'+g.seat];
   if(!action||!actor?.visible||Math.hypot(p.x-point.x,p.z-point.z)>=1.9)continue;
   const aim=new THREE.Vector3(actor.position.x,1.18,actor.position.z).project(this.camera);
   if(aim.z<=-1||aim.z>=1||Math.abs(aim.x)>.32||Math.abs(aim.y)>.5)continue;
   focused.push({type:'cafeGuest',id:g.id,seat:g.seat,x:actor.position.x,z:actor.position.z,name:action+' · '+guestLabel(g),score:aim.x*aim.x+aim.y*aim.y*.4});
  }
  if(focused.length)return focused.sort((a,b)=>a.score-b.score)[0];
 }
 candidates=Object.entries(s.interior==='shop'?SHOP_POINTS:s.interior==='cafe'?CAFE_POINTS:s.interior==='workshop'?WORKSHOP_POINTS:HOME_POINTS).map(([type,v])=>({type,...v}));for(let d of this.drops)if(d.x>290)candidates.push({...d,type:'drop',name:'Abgelegten Gegenstand aufnehmen'});return candidates.map(v=>({...v,dist:Math.hypot(v.x-p.x,v.z-p.z)})).filter(v=>v.dist<1.9).sort((a,b)=>a.dist-b.dist)[0]}

 for(let b of this.bottles)if(!s.collected.includes(b.id))candidates.push({...b,type:'bottle',name:'Pfandflasche aufnehmen · 0,25 €'});
 for(let d of this.drops)candidates.push({...d,type:'drop',name:'Abgelegten Gegenstand aufnehmen'});
 for(let l of LOCATIONS)candidates.push({...deliveryTarget(l.id),type:'location',name:l.name});
 for(let n of PEOPLE)candidates.push({...n,type:'person',name:n.name+' · '+n.role});
 if(s.job?.type==='cleaning'){let [x,z]=this.cleanPositions[s.job.progress]??[999,999];candidates.push({x,z,type:'trash',name:'Abfall einsammeln'})}
 if(s.job?.type==='warehouse'){if(!s.job.carrying)candidates.push({...this.cratePos,type:'crate',name:'Kiste scannen & aufnehmen'});else candidates.push({...this.shelves[s.job.progress%4],type:'shelf',name:'Kiste im richtigen Regal ablegen'})}
 return candidates.map(v=>({...v,dist:Math.hypot(v.x-p.x,v.z-p.z)})).filter(v=>v.dist<(v.type==='bottle'?2:3)&&segmentClear(p,v,(x,z,r)=>this.canWalkExterior(x,z,r),.08)).sort((a,b)=>a.dist-b.dist)[0]
 }
 applyViewSettings(){this.camera.fov=this.model.s.settings.fov||55;this.camera.updateProjectionMatrix();this.renderer.toneMappingExposure=1.04*(this.model.s.settings.brightness||1)}
 setQuality(q){this.atmosphere.quality=q;this.renderer.setPixelRatio(q==='low'?1:Math.min(devicePixelRatio,1.5));this.renderer.shadowMap.enabled=q!=='low'}
}
