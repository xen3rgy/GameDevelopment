import * as THREE from './vendor/three.module.js';
import {createCitizen} from './art.js?v=0.8.0';
import {animateCitizen} from './animation.js?v=0.8.0';
import {groundHeight} from './spatial.js?v=0.8.0';
import {DELIVERY_PEOPLE,DELIVERY_SECONDS,deliveryLocation,stationFacing} from './delivery-routes.js?v=0.8.0';
const smooth=t=>{t=Math.max(0,Math.min(1,t));return t*t*(3-2*t)};
export function parcelAsset(kit,index=0,label=''){
 const g=new THREE.Group(),{box,sign}=kit;
 box(g,0,0,0,.56,.38,.42,[0xb99766,0xbba98a,0xa78766][index%3]);
 box(g,0,.193,0,.075,.008,.426,0xd3bc8a);box(g,0,0,.214,.075,.39,.008,0xd3bc8a);
 box(g,.13,.199,-.02,.22,.008,.24,0xe9e5d5);
 for(let i=0;i<11;i++)box(g,.048+i*.015,.205,.025,(i%3===0?.012:.005),.003,.07,0x263e44);
 sign(g,label||'ZR · '+String(index+1).padStart(2,'0'),.12,.055,.221,label?.46:.22,.11,'#233e45','#e9e5d5');
 return g;
}
// Shared two-bone solve; roots and feet remain fixed while hands meet the parcel.
export function parcelHands(actor,parcel,weight=1){
 actor.updateMatrixWorld(true);parcel.updateMatrixWorld(true);
 for(const i of [0,1]){
  const {upper,arms,elbows}=actor.userData,arm=arms[i],elbow=elbows[i];
  const shoulder=arm.getWorldPosition(new THREE.Vector3()),hand=elbow.localToWorld(new THREE.Vector3(0,-.22,.026));
  const side=new THREE.Vector3(1,0,0).applyQuaternion(actor.getWorldQuaternion(new THREE.Quaternion())).dot(new THREE.Vector3(1,0,0).applyQuaternion(parcel.getWorldQuaternion(new THREE.Quaternion())))>=0?1:-1;
  const grip=parcel.localToWorld(new THREE.Vector3((i===0?-.265:.265)*side,-.03,-.17*side));
  const goal=hand.lerp(grip,weight),delta=goal.sub(shoulder),distance=Math.max(.025,Math.min(.507,delta.length())),direction=delta.normalize();
  const outward=new THREE.Vector3(i===0?-1:1,-.2,0).applyQuaternion(actor.getWorldQuaternion(new THREE.Quaternion()));
  const bend=outward.addScaledVector(direction,-outward.dot(direction)).normalize(),a=.29,b=Math.hypot(.22,.026),cos=Math.max(-1,Math.min(1,(a*a+distance*distance-b*b)/(2*a*distance)));
  const joint=shoulder.clone().addScaledVector(direction,a*cos).addScaledVector(bend,a*Math.sqrt(1-cos*cos));
  arm.quaternion.setFromUnitVectors(new THREE.Vector3(0,-1,0),joint.clone().sub(shoulder).normalize().applyQuaternion(upper.getWorldQuaternion(new THREE.Quaternion()).invert()));actor.updateMatrixWorld(true);
  const target=shoulder.clone().addScaledVector(direction,distance).sub(joint).normalize().applyQuaternion(arm.getWorldQuaternion(new THREE.Quaternion()).invert());
  elbow.quaternion.setFromUnitVectors(new THREE.Vector3(0,-.22,.026).normalize(),target);actor.updateMatrixWorld(true);
 }
}
export class CourierScene{
 constructor(world,kit){
  this.world=world;this.root=new THREE.Group();world.scene.add(this.root);this.stations=new Map();this.time=0;
  for(const [id,info] of Object.entries(DELIVERY_PEOPLE)){
   const l=deliveryLocation(id),face=stationFacing(id),actor=createCitizen(kit,info.color,[0xbf9275,0x8e6a50,0xd3ae8c,0xad825e][this.stations.size%4],this.stations.size+1);
   actor.position.set(l.x,groundHeight(l.x,l.z)-.05,l.z);actor.rotation.y=face===1?0:Math.PI;this.root.add(actor);
   const rack=new THREE.Group();rack.position.set(l.x,groundHeight(l.x,l.z),l.z);rack.rotation.y=actor.rotation.y;this.root.add(rack);
   kit.box(rack,-1.5,.8,-.18,1.1,.08,.54,0x755c40);for(const x of [-1.94,-1.06])kit.box(rack,x,.4,-.18,.045,.8,.42,0x324b52);
   for(let i=0;i<2;i++){const box=parcelAsset(kit,i);box.position.set(-1.73+i*.46,1.01,-.17);box.scale.setScalar(.72);rack.add(box);}
   kit.sign(rack,id==='jobs'?'ABHOLUNG':'EMPFANG',-1.5,1.62,-.17,1.13,.2,'#f0dec1','#253f48');
   kit.sign(rack,info.name+' · '+info.role,-1.5,1.36,-.17,1.34,.17,'#d3dfd9','#253f48');
   const mark=new THREE.Mesh(new THREE.RingGeometry(.64,.70,48),new THREE.MeshBasicMaterial({color:0xf2c878,transparent:true,opacity:.85,depthWrite:false,side:THREE.DoubleSide}));mark.rotation.x=-Math.PI/2;mark.position.set(l.x,groundHeight(l.x,l.z)+.025,l.z+face*1.2);this.root.add(mark);mark.visible=false;
   const received=parcelAsset(kit,0);actor.add(received);received.position.set(0,1.10,.40);received.rotation.y=Math.PI;received.visible=false;
   world.colliders?.push({x:l.x-1.5*face,z:l.z-.18*face,w:.55,d:.27,h:1.7},{x:l.x,z:l.z,w:.22,d:.22,h:1.8});
   this.stations.set(id,{actor,mark,received,ttl:0,face});
  }
  this.parcels=new Map(Object.keys(DELIVERY_PEOPLE).filter(id=>id!=='jobs').map((id,i)=>{const parcel=parcelAsset(kit,i,DELIVERY_PEOPLE[id].role.toUpperCase());this.root.add(parcel);parcel.visible=false;return [id,parcel];}));this.parcel=this.parcels.get('deliveryA');this.previous=null;
 }
 update(s,dt){
  this.time+=dt;this.root.visible=!s.inside;const j=s.job,a=j?.interaction,carrying=j?.type==='courier'&&!s.inside&&!s.riding&&!s.dailyLife?.action;
  const activeId=a?.target||(j?.type==='courier'?j.target:null),player=this.world.player;
  if(this.previous?.kind==='deliver'&&!a&&((!j&&s.courier.last?.status!=='cancelled'&&s.courier.last?.deliveries?.some(d=>d.target===this.previous.target&&d.index===this.previous.progress))||j?.progress>this.previous.progress)){
   const station=this.stations.get(this.previous.target);if(station)station.ttl=2.5;
  }
  this.previous=a?{kind:a.kind,target:a.target,progress:j.progress}:null;
  for(const [id,station] of this.stations){const {actor,mark,received}=station;station.ttl=Math.max(0,station.ttl-dt);received.visible=station.ttl>0;
   const distance=Math.hypot(s.position.x-actor.position.x,s.position.z-actor.position.z);mark.visible=id===activeId&&!s.inside;
   if(distance<7&&id===activeId)actor.rotation.y=Math.atan2(s.position.x-actor.position.x,s.position.z-actor.position.z);else actor.rotation.y=station.face===1?0:Math.PI;
   animateCitizen(actor,dt,0,{carrying:received.visible});
   if(received.visible)parcelHands(actor,received);
   else if(id===activeId&&distance>3&&distance<13){actor.userData.arms[1].rotation.x=-1.15;actor.userData.elbows[1].rotation.x=-.75+Math.sin(this.time*3)*.18;}
   mark.material.opacity=.66+Math.sin(this.time*2)*.12;
  }
  for(const [id,parcel] of this.parcels)parcel.visible=!!carrying&&id===j.target;if(!carrying)return;this.parcel=this.parcels.get(j.target);if(!this.parcel)return;
  const station=this.stations.get(activeId),phase=a?a.elapsed/DELIVERY_SECONDS[a.kind]:0;
  if(a&&station){player.rotation.y=Math.atan2(station.actor.position.x-player.position.x,station.actor.position.z-player.position.z);}
  player.updateMatrixWorld(true);const hold=player.localToWorld(new THREE.Vector3(0,1.14,.40));
  this.parcel.position.copy(hold);this.parcel.quaternion.copy(player.quaternion);
  if(a&&station){
   station.actor.updateMatrixWorld(true);const other=station.actor.localToWorld(new THREE.Vector3(0,1.14,.40));
   const transfer=smooth((phase-.20)/.55),toReceiver=a.kind==='deliver'?transfer:1-transfer;
   this.parcel.position.lerp(other,toReceiver);this.parcel.position.y+=Math.sin(Math.PI*transfer)*.035;
   parcelHands(player,this.parcel,a.kind==='pickup'?smooth(phase/.35):1-smooth((phase-.75)/.25));
   parcelHands(station.actor,this.parcel,a.kind==='deliver'?smooth(phase/.5):1-smooth((phase-.65)/.3));
   station.actor.userData.upper.rotation.x=Math.sin(Math.PI*phase)*.065;
  }else parcelHands(player,this.parcel);
 }
}
