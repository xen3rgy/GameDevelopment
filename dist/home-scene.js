import * as THREE from './vendor/three.module.js';
import {createCafeMeal,reachCafeHand} from './cafe-food.js?v=0.8.0';
import {groundHeight} from './spatial.js?v=0.8.0';
const smooth=t=>{t=Math.max(0,Math.min(1,t));return t*t*(3-2*t)};
export class HomeScene{
 constructor(world,kit){
  this.world=world;this.kit=kit;this.root=new THREE.Group();world.homeShell.add(this.root);this.props=new THREE.Group();world.scene.add(this.props);this.lastAction=false;
  const {box,cylinder,sign}=kit,g=this.root;
  // Open cabinet: shelves, inner lining and a real hinged door, kept out of static batching.
  box(g,305.8,1.28,-5.08,1.16,2.4,.09,0x5e7377);for(const x of [305.24,306.36])box(g,x,1.28,-4.55,.07,2.4,1.1,0xc7d3ce);
  for(const y of [.12,.62,1.18,1.76,2.46])box(g,305.8,y,-4.55,1.12,.06,1.1,0xe1e6dd);
  this.door=new THREE.Group();this.door.position.set(305.2,.08,-3.98);g.add(this.door);box(this.door,.60,1.20,0,1.2,2.40,.09,0x849c98);box(this.door,1.02,1.20,.09,.05,.5,.05,0xd8ddd3);box(this.door,.6,1.73,.055,1.05,.022,.01,0x486369);
  sign(this.door,'KÜHLVORRAT',.6,2.10,.07,.85,.16,'#f1e6d0','#38565b');
  this.chilled=[];for(let i=0;i<8;i++){const v=new THREE.Group();v.position.set(305.48+(i%2)*.52,.65+Math.floor(i/2)*.55,-4.36);g.add(v);const bottle=new THREE.Group(),pack=new THREE.Group(),cup=new THREE.Group();v.add(bottle,pack,cup);
   cylinder(bottle,0,.15,0,.075,.28,0x668f81);cylinder(bottle,0,.31,0,.035,.05,0xd4dccb);
   const wrapper=box(pack,0,.1,0,.29,.18,.24,0xd1af62);wrapper.material=wrapper.material.clone();box(pack,0,.19,0,.27,.016,.22,0xeee3ca);
   cylinder(cup,0,.09,0,.08,.16,0x55746d);v.userData={bottle,pack,cup,wrapper};this.chilled.push(v);
  }
  box(g,294.1,.46,-4.8,.75,.78,.7,0x6b5340);cylinder(g,294.1,1.13,-4.8,.04,.55,0x6d786e);cylinder(g,294.1,1.43,-4.8,.24,.25,0xe7cca0);this.lamp=new THREE.PointLight(0xffc486,2.5,5);this.lamp.position.set(294.1,1.4,-4.8);g.add(this.lamp);
  this.blanket=box(g,296.5,1.34,-3.13,1.1,.04,1.35,0x6e9090);this.blanket.visible=false;
  this.privacy=new THREE.Group();g.add(this.privacy);for(const x of [305.08,306.64])box(this.privacy,x,1.8,2,.025,2.2,1.65,0xa1b5ad);box(this.privacy,305.86,1.8,2.82,1.58,2.2,.025,0xa1b5ad);box(this.privacy,305.86,1.8,1.18,1.58,2.2,.025,0xa1b5ad);this.privacy.visible=false;
  const points=new Float32Array(60*3);this.water=new THREE.Points(new THREE.BufferGeometry(),new THREE.PointsMaterial({color:0xc6ecec,size:.035,transparent:true,opacity:.6}));this.water.geometry.setAttribute('position',new THREE.BufferAttribute(points,3));g.add(this.water);this.water.visible=false;
  this.items=new Map();for(const id of ['water','coffee','sandwich','meal','medicine']){const p=new THREE.Group();this.props.add(p);p.visible=false;this.items.set(id,p);
   if(id==='coffee'){const cup=createCafeMeal('latte').userData.movable;p.add(cup);}
   else if(id==='water'){cylinder(p,0,.15,0,.065,.3,0x729a92);cylinder(p,0,.33,0,.027,.08,0x617d76);box(p,0,.16,.061,.09,.09,.008,0xe3e8d7);}
   else if(id==='sandwich'){for(const [y,c] of [[-.045,0xc8a777],[-.012,0xe0c365],[.002,0x739253],[.035,0xd4b788]])box(p,0,y,0,.22,.03,.16,c);}
   else if(id==='meal'){cylinder(p,0,0,0,.19,.085,0xdbdfd0);cylinder(p,0,.05,0,.17,.025,0xc39854);for(let i=0;i<7;i++)box(p,Math.sin(i*2)*.1,.07,Math.cos(i*2)*.1,.05,.02,.05,i%2?0x75925a:0xb96143);}
   else{box(p,0,0,0,.19,.13,.11,0xddd9c6);box(p,0,0,.06,.11,.026,.012,0xaf6755);box(p,0,0,.06,.026,.09,.012,0xaf6755);}
  }
  this.spoon=new THREE.Group();cylinder(this.spoon,0,.045,0,.008,.17,0xbac7c4);const end=new THREE.Mesh(new THREE.SphereGeometry(.024,10,6),kit.mat(0xbac7c4));end.scale.set(1,1.5,.25);end.position.y=.145;this.spoon.add(end);this.props.add(this.spoon);this.spoon.visible=false;
 }
 update(s,dt){
  const a=s.dailyLife?.action,p=this.world.player,d=p.userData,phase=a?a.elapsed/a.duration:0;
  // Reset only root tilt; position is restored by the world from the saved standing anchor.
  this.cameraAnchor=null;p.rotation.x=0;p.rotation.z=0;if(d.backpack)d.backpack.visible=true;
  for(const item of this.items.values())item.visible=false;this.spoon.visible=false;this.blanket.visible=false;this.privacy.visible=false;this.water.visible=false;
  const open=s.interior==='home'&&this.world.homeScreen==='fridge';this.door.rotation.y+=((open?-1.65:0)-this.door.rotation.y)*(1-Math.exp(-dt*9));
  const stock=(s.fridge||[]).flatMap(v=>Array(Math.min(8,v.count)).fill(v.id)).slice(0,8);this.chilled.forEach((v,i)=>{const id=stock[i],d=v.userData;v.visible=!!id;d.bottle.visible=id==='water';d.cup.visible=id==='coffee';d.pack.visible=!!id&&id!=='water'&&id!=='coffee';d.wrapper.material.color.set({vegetables:0x8ca96d,cheese:0xdbbd67,pasta:0xbc895e,bread:0xc7a573,sandwich:0x789b88,meal:0xb6c9c4}[id]||0xc4b07f);});
  if(!a){if(this.lastAction){d.arms.forEach(v=>v.rotation.set(0,0,0));d.elbows.forEach(v=>v.rotation.set(0,0,0));}this.lastAction=false;return;}
  this.lastAction=true;
  const settle=smooth(Math.min(phase/.28,(1-phase)/.22));p.rotation.y=a.yaw;
  if(a.kind==='sleep'){
   this.cameraAnchor=new THREE.Vector3(a.origin.x,.02,a.origin.z).lerp(new THREE.Vector3(296.5,.02,-3.0),settle);
   const target=new THREE.Vector3(296.5,1.14,-2.45);p.position.set(a.origin.x,.02,a.origin.z).lerp(target,settle);
   const start=new THREE.Quaternion().setFromEuler(new THREE.Euler(0,a.yaw,0)),lying=new THREE.Quaternion().setFromEuler(new THREE.Euler(-Math.PI/2,0,0));p.quaternion.copy(start.slerp(lying,settle));
   for(let i=0;i<2;i++){d.legs[i].rotation.x=-Math.sin(Math.PI*settle)*.7;d.knees[i].rotation.x=Math.sin(Math.PI*settle)*1.0;d.arms[i].rotation.set(-.12,0,i?-.12:.12);d.elbows[i].rotation.set(-.15,0,0);}
   d.upper.rotation.x=.12*settle;if(d.backpack)d.backpack.visible=settle<.1;this.blanket.visible=settle>.95;
  }else if(a.kind==='shower'){
   p.position.set(a.origin.x,.02,a.origin.z).lerp(new THREE.Vector3(305.85,.56,2),settle);this.privacy.visible=settle>.1;this.water.visible=settle>.8;
   d.arms[1].rotation.x=-2.5;d.elbows[1].rotation.x=-.7;d.upper.rotation.z=Math.sin(a.elapsed*3)*.025;
   if(d.backpack)d.backpack.visible=settle<.1;
   const pos=this.water.geometry.attributes.position;for(let i=0;i<pos.count;i++){const y=2.8-((a.elapsed*2+i*.073)%2.2);pos.setXYZ(i,305.9+Math.sin(i*7)*.4,y,2+Math.cos(i*11)*.4);}pos.needsUpdate=true;
  }else if(a.kind==='cook'){
   p.rotation.y=Math.PI;d.arms[1].rotation.x=-1.0;d.elbows[1].rotation.x=-.6;d.arms[1].rotation.z=Math.sin(a.elapsed*5)*.15;
   const meal=this.items.get('meal');meal.visible=true;meal.position.set(303,1.73,-4.3);meal.rotation.set(0,0,0);
  }else if(a.kind==='consume'){
   const item=this.items.get(a.item);if(!item)return;item.visible=true;p.updateMatrixWorld(true);
   const lift=Math.sin(Math.PI*smooth(phase)),food=a.item==='sandwich',drink=a.item==='water'||a.item==='coffee';
   const local=new THREE.Vector3(.15*(1-lift),1.10+lift*(food?.54:.40),.43-(food?.21:0)*lift);item.position.copy(p.localToWorld(local));item.quaternion.copy(p.quaternion);if(drink)item.rotateX(-lift*1.08);
   if(a.item==='meal'){
    item.position.copy(p.localToWorld(new THREE.Vector3(-.22,1.07,.39)));d.arms[0].rotation.x=-1.2;d.elbows[0].rotation.x=-.75;
    this.spoon.visible=true;this.spoon.position.copy(p.localToWorld(new THREE.Vector3(.12*(1-lift),1.18+lift*.40,.38-lift*.16)));this.spoon.quaternion.copy(p.quaternion);this.spoon.rotateX(-lift*1.2);reachCafeHand(p,this.spoon.position,settle);
   }else reachCafeHand(p,item.localToWorld(new THREE.Vector3(a.item==='coffee'?.12:0,drink?.10:0,0)),settle);
  }else if(a.kind==='wash'){d.arms[1].rotation.x=-2.4;d.elbows[1].rotation.x=-.5;}
  else if(a.kind==='rest'){
   p.position.set(a.origin.x,groundHeight(a.origin.x,a.origin.z)-.05,a.origin.z).lerp(new THREE.Vector3(65,-.16,76.94),settle);p.rotation.y=Math.PI;
   d.legs.forEach((leg,i)=>{leg.rotation.x=-1.35*settle;d.knees[i].rotation.x=1.55*settle;});
  }else if(a.kind==='shelterSleep'){p.visible=phase<.2||phase>.8;}
 }
}
