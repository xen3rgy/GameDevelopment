import * as THREE from './vendor/three.module.js';
import {SERVICE_SITE,SERVICE_BAYS,SERVICE_FIXTURES} from './service-layout.js?v=0.8.1';
import {fuelPrice} from './vehicle-service.js?v=0.8.1';
import {ServiceGestures,serviceHose,serviceNozzle} from './service-gestures.js?v=0.8.1';
export class ServiceScene{
 constructor(world,kit){
  const {box,sign,cylinder}=kit;
  this.root=new THREE.Group();this.root.name='Hafenenergie & Hafenwerk';world.scene.add(this.root);
  const r=new THREE.Group(),p=SERVICE_SITE;this.root.add(r);
  const green=0x315f57,dark=0x293e41,cream=0xe7dec5,steel=0x879490,amber=0xe7bc62;
  box(r,p.x,.125,p.z,p.w,.35,p.d,0x626d6d);
  box(r,-33,.307,-104,19.5,.014,9.5,0x78817b);
  for(const c of SERVICE_FIXTURES){
   world.colliders.push({...c,h:c.h+.30});
   if(['storage','bollard'].includes(c.kind))continue;
   box(r,c.x,.30+c.h/2,c.z,c.w*2,c.h,c.d*2,c.kind==='pump'?cream:c.kind==='post'?green:c.kind==='island'?0xb3b4a6:c.kind==='pylon'?green:steel);
  }
  // Layered canopy, recessed soffit, fascia and slender cross beams.
  box(r,-33,5.35,-87,30,.40,13,green);box(r,-33,5.59,-87,30.25,.08,13.2,steel);
  box(r,-33,5.12,-87,29.5,.08,12.5,cream);
  for(const z of [-80.45,-93.55])box(r,-33,5.34,z,30,.12,.06,amber);
  for(const x of [-47,-33,-19])box(r,x,4.98,-87,.16,.22,12.5,dark);
  sign(r,'HAFENENERGIE',-33,5.36,-80.39,13,.32,'#fff1c5','#315f57');
  for(const x of [-47,-19]){box(r,x,.52,-93,.55,.44,.55,steel);box(r,x,1.1,-92.82,.23,.12,.025,amber);}
  const luminous=new THREE.MeshStandardMaterial({color:0xffedd0,emissive:0xffe1af,emissiveIntensity:1.5,roughness:.45});
  for(const x of [-40,-25])for(const z of [-90,-84])box(r,x,5.02,z,3.2,.055,.25,0,luminous);
  // Drainage, expansion joints and restrained approach markings.
  for(const x of [-47.8,-18.2])box(r,x,.314,-94.5,.12,.018,30,dark);
  for(const z of [-94.5,-79.3])box(r,-33,.314,z,29,.018,.045,0x485655);
  for(const x of [-37,-22]){
   box(r,x,.32,-81.3,.10,.018,1.8,cream);
   for(const side of [-1,1]){const m=box(r,x+side*.26,.32,-81.9,.08,.018,.75,cream);m.rotation.y=-side*.75;}
  }
  this.docked=new Map();
  for(const [i,b] of SERVICE_BAYS.entries()){
   const v=b.vehicle;
   for(const dx of [-2.15,2.15])box(r,v.x+dx,.322,v.z,.065,.02,6.8,cream);
   box(r,v.x,.322,v.z-3.4,4.3,.02,.065,cream);
   if(b.kind==='fuel'){
    const x=b.x-1.2;
    box(r,x,.72,b.z,1.13,.55,2.22,green);box(r,x,2.73,b.z,1.18,.09,2.26,green);
    for(const side of [-1,1]){
     const z=b.z+side*1.115,turn=side===1?0:Math.PI;
     sign(r,'SUPER E5 · 0'+(i+1),x,2.43,z+side*.008,1,.24,'#fff1c5','#315f57',turn);
     box(r,x,1.98,z,.92,.48,.035,dark);
     sign(r,'000.00 L',x,2.04,z+side*.025,.76,.16,'#b7e7c7','#162c2c',turn);
     sign(r,'KARTE  ·  PIN',x,1.64,z+side*.025,.70,.12,'#d5daca','#293e41',turn);
     for(let k=0;k<3;k++)box(r,x-.16+k*.16,1.46,z+side*.025,.07,.055,.03,steel);
    }
    const dock=new THREE.Group();this.root.add(dock);this.docked.set(b.id,dock);
    dock.add(serviceHose([new THREE.Vector3(x+.56,2.22,b.z),new THREE.Vector3(x+.92,.72,b.z+.32),new THREE.Vector3(x+.95,.65,b.z-.34),new THREE.Vector3(x+.65,1.86,b.z-.45)]));
    const nozzle=serviceNozzle(kit);nozzle.position.set(x+.65,1.86,b.z-.45);dock.add(nozzle);
    for(const z of [b.z-1.55,b.z+1.55]){cylinder(r,x,.73,z,.12,.85,amber);cylinder(r,x,.91,z,.125,.12,dark);}
   }else{
    for(const dx of [-1.5,1.5]){box(r,v.x+dx,.37,v.z,.35,.14,5.2,dark);box(r,v.x+dx,.45,v.z+2.4,.35,.025,.3,amber);}
    for(let x=-36;x<=-30;x+=.6){const m=box(r,x,.325,-98.7,.25,.025,.65,amber);m.rotation.y=-.5;}
    sign(r,'ANMELDUNG · E',b.x,1.5,b.z-.8,2,.35,'#ffffff','#253d40');
   }
  }
  // Roadside pylon stays outside both drive-through lanes.
  sign(r,'HAFEN',-47,5.25,-79.64,1.15,.24,'#fff1c5','#315f57');
  sign(r,'ENERGIE',-47,4.9,-79.64,1.15,.22,'#fff1c5','#315f57');
  sign(r,'SUPER E5',-47,4.25,-79.64,1.12,.21,'#d9ebdf','#293e41');
  sign(r,'24 h',-47,2.7,-79.64,.9,.40,'#fff1c5','#315f57');
  sign(r,'SERVICE →',-47,1.65,-79.64,1.12,.2,'#fff1c5','#315f57');
  // Open garage with a deep rear wall, framed opening and rolled-up shutter.
  box(r,-33,4.95,-104,21,.30,11,dark);box(r,-33,5.14,-104,21.25,.08,11.2,steel);
  box(r,-33,4.43,-98.65,20,.65,.3,green);
  sign(r,'HAFENWERK · SERVICE',-33,4.48,-98.48,15,.42,'#f2e1b9','#315f57');
  for(const x of [-42.7,-23.3]){box(r,x,2.23,-99,.18,3.85,.3,dark);box(r,x,1,-98.82,.19,.55,.03,amber);}
  for(let y=3.78;y<4.13;y+=.11)box(r,-33,y,-98.78,19,.065,.12,steel);
  for(let x=-42;x<=-24;x+=1.5)box(r,x,2.4,-108.75,.035,4,.035,0x727f7b);
  box(r,-33,1.04,-108.73,19,.8,.03,green);
  sign(r,'HAFENWERK',-33,3.4,-108.70,4,.45,'#eddfb9','#315f57');
  sign(r,'01 · FAHRZEUGSERVICE',-33,2.83,-108.70,4,.24,'#d4ded4','#293e41');
  for(const z of [-101,-106]){box(r,-33,4.69,z,7,.08,.22,0,luminous);box(r,-33,4.79,z,7.2,.12,.35,dark);}
  box(r,-39,1.44,-100,2.15,.12,1.02,0xb1976f);
  box(r,-39,2.1,-100.36,2,.9,.06,dark);
  for(let i=0;i<5;i++){box(r,-39.7+i*.32,2.16,-100.31,.035,.33,.035,steel);box(r,-39.7+i*.32,2.32,-100.29,.12,.055,.04,steel);}
  // Cabinets and tire shelving occupy collider-backed rear storage pockets.
  for(const x of [-40.5,-38.8]){
   box(r,x,.95,-108,1.45,1.3,1.15,green);box(r,x,1.65,-108,1.55,.10,1.25,0xb1976f);
   for(let y=.55;y<1.6;y+=.25){box(r,x,y,-107.41,1.32,.02,.035,dark);box(r,x,y+.1,-107.37,.43,.035,.04,steel);}
  }
  for(const x of [-25.6,-24.4])for(const z of [-107.3,-104.7])box(r,x,1.62,z,.065,2.6,.065,dark);
  for(const y of [.42,1.52,2.65])box(r,-25,y,-106,1.35,.08,2.8,steel);
  const tireGeo=new THREE.TorusGeometry(.32,.13,6,12),tireMat=new THREE.MeshStandardMaterial({color:0x292e2d,roughness:.95});
  for(const y of [.91,2.02])for(const z of [-106.8,-105.8]){const tire=new THREE.Mesh(tireGeo,tireMat);tire.rotation.y=Math.PI/2;tire.position.set(-25,y,z);r.add(tire);const hub=cylinder(r,-25,y,z,.19,.12,steel);hub.rotation.z=Math.PI/2;}
  box(r,-41,1.12,-100, .30,.65,.28,0x994e3d);
  sign(r,'FEUERLÖSCHER',-41,1.66,-99.83,.8,.12,'#f3ead4','#994e3d');
  this.lights=[];
  for(const [x,y,z,power] of [[-33,4.5,-87,85],[-33,4.2,-104,65]]){const light=new THREE.PointLight(0xffe1b4,power,18,2);light.position.set(x,y,z);this.root.add(light);this.lights.push(light);}
  world.overheadColliders.push({x:-33,z:-87,w:15.15,d:6.6,minY:4.87,h:5.63},{x:-33,z:-104,w:10.63,d:5.6,minY:4.65,h:5.18},{x:-33,z:-98.65,w:10,d:.3,minY:3.74,h:4.8});
  world.batchStaticGroup(r);
  this.gestures=new ServiceGestures(this.root,kit,this.docked);
  this.priceRoot=new THREE.Group();this.root.add(this.priceRoot);this.sign=sign;
 }
 resetPose(player){this.gestures.resetPose(player);}
 update(s,player,dt=0){
  this.root.visible=!s.inside;
  const near=Math.hypot(s.position.x-SERVICE_SITE.x,s.position.z-SERVICE_SITE.z)<55;
  for(const light of this.lights)light.visible=!s.inside&&near&&s.settings.quality!=='low';
  this.gestures.update(s,player,dt);
  const price=fuelPrice(s);if(this.price===price)return;this.price=price;
  for(const child of [...this.priceRoot.children]){child.geometry?.dispose();this.priceRoot.remove(child);}
  this.sign(this.priceRoot,(price/100).toFixed(2)+' €/L',-47,3.65,-79.64,1.16,.36,'#d6f3c8','#162c2c');
 }
}
