import * as THREE from './vendor/three.module.js';
import {TUNING_SITE,TUNING_FIXTURES,tuningAligned} from './tuning-layout.js?v=0.8.1';
import {ALLOY_STYLES,createAlloyRim} from './tuning-rims.js?v=0.8.1';
export class TuningScene {
 constructor(world,kit){const {box,cylinder,sign,mat}=kit,g=this.root=new THREE.Group();g.userData.exterior=true;world.scene.add(g);world.colliders.push(...TUNING_FIXTURES);const wall=0x3b4b50,trim=0x243238,gold=0xc3aa76,metal=0x82908e;
 box(g,TUNING_SITE.x,.028,205,38,.035,28,0x888d84);box(g,195,.06,205,18,.08,16,0x929f9c);
 // Existing collision footprints stay unchanged; details sit on their surfaces.
 for(const f of TUNING_FIXTURES.slice(0,3))box(g,f.x,f.h/2,f.z,f.w*2,f.h,f.d*2,wall);
 for(const z of [197.23,212.77]){box(g,195,1.6,z,17.8,3.05,.035,0x9aa6a2);box(g,195,.36,z,17.8,.5,.055,trim);for(let x=187;x<204;x+=1.2)box(g,x,3.75,z,.025,1.35,.04,metal);}
 box(g,203.77,1.65,205,.035,3.15,15.5,0x909d99);box(g,203.73,.36,205,.04,.5,15.5,trim);
 box(g,195,4.6,205,18.5,.24,16.5,trim);box(g,195,4.77,205,18.55,.09,16.55,metal);
 // A roof is a thin overhead slab, never a solid obstacle extending to ground.
 world.overheadColliders.push({x:195,z:205,w:9.25,d:8.25,minY:4.45,h:4.8},{x:185.9,z:205,w:.15,d:7.85,minY:3.36,h:4.53});
 for(const z of [197.2,212.8]){box(g,186,2.25,z,.3,4.5,.3,trim);box(g,186.23,1.7,z,.08,3.3,.11,metal);box(g,195,4.34,z,17.8,.15,.12,trim);}
 for(const x of [186.8,195,203])box(g,x,4.32,205,.15,.2,15.7,trim);
 box(g,185.9,3.94,205,.22,1.15,15.7,trim);box(g,185.7,3.4,205,.14,.045,15.8,gold);
 const signWest=(text,x,y,z,w,h,fg='#ead8ad',bg='#243238')=>sign(g,text,x,y,z,w,h,fg,bg,-Math.PI/2);
 signWest('W E R K F O R M',185.73,4.12,204.2,10,.6);signWest('FAHRZEUGDESIGN & TUNING',185.72,3.66,204.2,8.5,.24);signWest('07',185.71,4,211.6,.75,.58);
 // Glazed reception frontage and upper workshop windows follow the existing walls.
 const glazing=new THREE.MeshStandardMaterial({color:0x365563,metalness:.5,roughness:.18});
 for(const z of [196.77,213.23]){box(g,190,2.25,z,6.8,2.8,.035,trim);box(g,190,2.25,z+(z<205?-.024:.024),6.5,2.56,.025,0,glazing);for(const x of [186.75,188.7,191.4,193.25])box(g,x,2.25,z+(z<205?-.048:.048),.055,2.7,.055,metal);box(g,190,2.75,z+(z<205?-.05:.05),6.6,.055,.055,metal);}
 for(const x of [197,200,203])for(const z of [196.77,213.23]){box(g,x,3.5,z,2.4,1.1,.04,trim);box(g,x,3.5,z+(z<205?-.03:.03),2.2,.9,.025,0,glazing);}
 // Glazing inside the reception corner reads as a small office behind the bay.
 box(g,194.7,2.35,212.73,3.6,1.7,.03,trim);box(g,194.7,2.35,212.7,3.4,1.5,.02,0,glazing);box(g,194.7,2.35,212.67,.055,1.5,.025,metal);
 for(const x of [170,175,180])box(g,x,.055,205,.025,.01,27.5,0x737e79);
 for(const z of [201,209]){box(g,185.15,.11,z,1.05,.015,1.4,trim);for(let x=184.72;x<185.65;x+=.16)box(g,x,.121,z,.045,.009,1.34,metal);}
 const guide=this.guide=new THREE.MeshStandardMaterial({color:gold,roughness:.65,emissive:gold,emissiveIntensity:.1});
 box(g,190,.105,205,7.3,.012,4.9,0x667e79);
 for(const z of [202.6,207.4])box(g,190,.119,z,7.2,.016,.07,0,guide);for(const x of [186.4,193.6])box(g,x,.119,205,.07,.016,4.8,0,guide);
 for(const x of [186.65,193.35])for(const z of [202.85,207.15]){box(g,x,.13,z,.5,.01,.08,0,guide);box(g,x,.13,z,.08,.01,.5,0,guide);}
 box(g,190,.12,205,.8,.016,.035,0,guide);box(g,190,.12,205,.035,.016,.8,0,guide);box(g,181,.075,205,3,.02,.09,gold);
 for(const z of [-1,1]){const arrow=box(g,182,.075,205+z*.38,1.05,.02,.09,gold);arrow.rotation.y=z*Math.PI/4;}
 for(const f of TUNING_FIXTURES.slice(3,6)){box(g,f.x,f.h/2,f.z,f.w*2,f.h,f.d*2,trim);box(g,f.x,f.h+.03,f.z,f.w*2+.1,.07,f.d*2+.1,metal);}
 // Drawer faces, worktop and pegboard remain within the existing cabinet footprint.
 for(const x of [199.7,201,202.3])for(const y of [.28,.57,.86]){box(g,x,y,210.22,1.15,.23,.03,0x465c60);box(g,x,y+.04,210.18,.68,.022,.04,metal);}
 box(g,201,2.05,212.7,4,1.3,.055,trim);for(const x of [199.5,200,200.5,201,201.5,202]){box(g,x,2.1,212.63,.055,.43,.045,metal);box(g,x,2.32,212.61,.17,.055,.07,metal);}
 for(const x of [200,202]){box(g,x,1.35,198.2,.9,.18,1.1,metal);box(g,x,2.03,198.2,.9,.16,1.1,metal);}
 box(g,186.7,.6,210,.9,1.2,.9,trim);box(g,186.7,1.22,210,1,.06,1,metal);box(g,186.2,1.04,210,.025,.32,.6,0,mat(0x80a8a0));signWest('EMPFANG',186.17,1.06,210,.54,.1,'#172b32','#b9cbb9');
 const tireGeo=new THREE.TorusGeometry(.36,.12,6,20),tireMat=mat(0x242a2b),rimMat=new THREE.MeshStandardMaterial({color:0xb8c1bf,metalness:.7,roughness:.27});
 for(let i=0;i<3;i++){const tire=new THREE.Mesh(tireGeo,tireMat);tire.rotation.x=Math.PI/2;tire.position.set(192,.23+i*.25,211.9);g.add(tire);}
 for(const x of [200,202])for(const y of [.72,1.65]){const tire=new THREE.Mesh(tireGeo,tireMat);tire.position.set(x,y,198.2);g.add(tire);}
 box(g,203.68,2.02,205,.07,3.1,8,trim);signWest('ALLOY COLLECTION',203.61,3.25,205,5.4,.22);
 this.displayRims=[];for(const [i,style] of ALLOY_STYLES.entries()){const rim=createAlloyRim(style,.48,rimMat,tireMat);rim.rotation.y=-Math.PI/2;rim.position.set(203.57,2.65-Math.floor(i/3)*1.25,202.6+(i%3)*2.4);g.add(rim);this.displayRims.push(rim);}
 for(const z of [198,212]){cylinder(g,184,.45,z,.13,.9,gold);cylinder(g,184,.59,z,.135,.12,trim);}
 const lamp=new THREE.MeshStandardMaterial({color:0xe4e0cd,emissive:0xffedcb,emissiveIntensity:.85});for(const z of [200,210]){box(g,187,3.32,z,.25,.09,2.3,0,lamp);box(g,195,4.16,z,6.5,.045,.14,0,lamp);}for(const z of [198.5,211.5]){box(g,185.62,3.1,z,.26,.16,.6,trim);box(g,185.46,3.07,z,.035,.08,.46,0,lamp);}
 world.batchStaticGroup(g);
 }
 cameraShelter(position,riding=false){return position.x>177&&position.x<205.5&&position.z>195.5&&position.z<214.5?{ceiling:4.18,minBoom:riding?3.4:1.7}:null;}
 frameCamera(world,dt,normalView){const c=world.camera,w=innerWidth,h=innerHeight,mobile=w<=700,s=world.tuningPreview,ease=1-Math.exp(-dt*8),move=(point,target)=>point.lerp(target,Math.min(ease,dt*18/Math.max(.001,point.distanceTo(target))));
  if(!s){if(!this.previewSession)return;const destination=new THREE.Vector3(normalView.position.x,normalView.position.y,normalView.position.z),anchor=new THREE.Vector3(normalView.anchor.x,normalView.anchor.y,normalView.anchor.z);
   // The normal CameraRig keeps running behind the showroom view. Hand control
   // back to it as soon as entering or driving starts, instead of making the
   // preview camera chase a moving vehicle for several seconds.
   const movingAway=this.previewNormalAnchor&&this.previewNormalAnchor.distanceTo(anchor)>.35;
   if(world.transition||world.model?.s?.riding||movingAway||this.previewPosition.distanceTo(destination)>25){this.previewSession=null;this.previewNormalAnchor=null;return;}
   move(this.previewPosition,destination);this.previewAnchor.lerp(anchor,ease);c.position.copy(this.previewPosition);c.lookAt(this.previewAnchor);if(this.previewPosition.distanceTo(destination)<.02){this.previewSession=null;this.previewNormalAnchor=null;}return;}
  const v=s.vehicle;this.previewRig??=new world.cameraRig.constructor();if(this.previewSession!==s){this.previewSession=s;this.previewRig.reset();this.previewPosition=c.position.clone();this.previewAnchor=new THREE.Vector3(normalView.anchor.x,normalView.anchor.y,normalView.anchor.z);}this.previewNormalAnchor=new THREE.Vector3(normalView.anchor.x,normalView.anchor.y,normalView.anchor.z);
  const angle=s.frontView?Math.PI/3:-Math.PI/3,position={x:v.x,y:-.35,z:v.z},view=this.previewRig.update(position,angle,.25,mobile?10:8.6,dt,world.cameraObstacles,null,{ceiling:4.18,minBoom:3.4});move(this.previewPosition,new THREE.Vector3(view.position.x,view.position.y,view.position.z));this.previewAnchor.lerp(new THREE.Vector3(view.anchor.x,view.anchor.y,view.anchor.z),ease);c.position.copy(this.previewPosition);c.lookAt(this.previewAnchor);c.setViewOffset(w,h,mobile?0:Math.min(430,w*.4)/2,mobile?h*.22:0,w,h);
 }

 update(s){this.root.visible=!s.inside;const valid=(s.fleet||[]).some(tuningAligned);this.guide.color.setHex(valid?0x83bf99:0xc3aa76);this.guide.emissive.copy(this.guide.color);}
}
