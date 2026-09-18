import * as THREE from './vendor/three.module.js';
const smooth=t=>{t=Math.max(0,Math.min(1,t));return t*t*(3-2*t);};
// Real-time gestures stay readable at 1x, 4x and 10x. End before the resident leaves.
export function streetIdlePose(person,speed=1){
 const seated=person.phase==='seated',waiting=person.phase==='wait'&&person.goal?.kind==='wait';
 if(!seated&&!waiting)return {kind:null,weight:0,lift:0,tilt:0};
 const kind=seated?(person.id%3===0?'coffee':person.id%3===1?'phone':null):(person.id%2===0?'phone':null);
 const age=person.age||0,weight=smooth((age-.3)/1.1)*smooth(Math.max(0,person.wait)/Math.max(1,speed)/1.3);
 const t=(age+person.id*.71)%13,lift=smooth((t-3)/1.1)*(1-smooth((t-5.4)/1.1));
 return {kind,weight:kind?weight:0,lift,tilt:lift*.22};
}
export function attachStreetProps(actor,kit){
 const {box,cylinder}=kit,root=new THREE.Group(),cup=new THREE.Group(),phone=new THREE.Group();root.name='Everyday objects';root.visible=false;root.add(cup,phone);actor.userData.upper.add(root);
 cylinder(cup,0,0,0,.061,.13,0xd2bd95);cylinder(cup,0,.07,0,.065,.017,0xe1ded0);cylinder(cup,0,.035,0,.062,.042,0x866c47);
 box(cup,0,.079,-.027,.018,.006,.011,0x454b43);
 box(phone,0,0,0,.079,.145,.014,0x253b42);
 const screen=new THREE.MeshBasicMaterial({color:0x799795,toneMapped:true});box(phone,0,.003,-.008,.062,.115,.002,0,screen).castShadow=false;
 box(phone,0,-.065,-.009,.022,.004,.003,0x8caaa6);
 actor.userData.streetProps={root,cup,phone};
}
export function resetStreetArms(actor){
 // IK may rotate all axes. The walking animator only controls X/Z, so clear the others.
 const d=actor.userData;for(const arm of d.arms)arm.rotation.y=0;
 for(const elbow of d.elbows){elbow.rotation.y=0;elbow.rotation.z=0;}
 if(d.headRoot)d.headRoot.rotation.x=0;
}
export function animateStreetIdle(actor,person,speed=1){
 const d=actor.userData,props=d.streetProps;if(!props)return;
 const pose=streetIdlePose(person,speed);props.root.visible=pose.weight>.015;
 if(!props.root.visible)return;
 const {kind,weight,lift,tilt}=pose;props.cup.visible=kind==='coffee';props.phone.visible=kind==='phone';
 const object=kind==='coffee'?props.cup:props.phone;
 // Coordinates relative to the upper body (hip origin). The cup rim reaches the lips.
 const x=kind==='coffee'?.23-.21*lift:.19,y=kind==='coffee'?.11+.55*lift:.31,z=kind==='coffee'?.35-.15*lift:.36;
 object.position.set(.27+(x-.27)*weight,-.03+(y+.03)*weight,.15+(z-.15)*weight);
 object.rotation.set(kind==='coffee'?-tilt:-.3,0,0);
 actor.updateMatrixWorld(true);
 const grip=object.localToWorld(new THREE.Vector3(kind==='coffee'?.064:.042,-.01,0));
 reachStreetHand(actor,grip,weight);
 if(d.headRoot){d.headRoot.rotation.x=(kind==='phone'?.18:-.035*lift)*weight;d.headRoot.rotation.y*=1-weight*.85;}
}

// Solve in upper-body coordinates so short/tall and differently built residents keep contact.
export function reachStreetHand(actor,target,weight=1){
 const {upper,arms,elbows}=actor.userData,arm=arms[1],elbow=elbows[1];actor.updateMatrixWorld(true);
 const hand=upper.worldToLocal(elbow.localToWorld(new THREE.Vector3(0,-.22,.026))),goal=hand.lerp(upper.worldToLocal(target.clone()),weight),shoulder=arm.position;
 const a=.29,b=Math.hypot(.22,.026),delta=goal.sub(shoulder),distance=Math.max(.025,Math.min(a+b-.001,delta.length())),direction=delta.normalize();
 const outward=new THREE.Vector3(1,-.15,0),bend=outward.addScaledVector(direction,-outward.dot(direction)).normalize();
 const cos=Math.max(-1,Math.min(1,(a*a+distance*distance-b*b)/(2*a*distance))),joint=shoulder.clone().addScaledVector(direction,a*cos).addScaledVector(bend,a*Math.sqrt(1-cos*cos));
 arm.quaternion.setFromUnitVectors(new THREE.Vector3(0,-1,0),joint.clone().sub(shoulder).normalize());
 const lower=shoulder.clone().addScaledVector(direction,distance).sub(joint).normalize().applyQuaternion(arm.quaternion.clone().invert());
 elbow.quaternion.setFromUnitVectors(new THREE.Vector3(0,-.22,.026).normalize(),lower);actor.updateMatrixWorld(true);
}
