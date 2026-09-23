import * as THREE from './vendor/three.module.js';
import {SERVICE_SITE,SERVICE_BAYS,SERVICE_FIXTURES} from './service-layout.js?v=0.8.1';
import {fuelPrice} from './vehicle-service.js?v=0.8.1';
export class ServiceScene{
 constructor(world,{box,sign,cylinder}){
  this.root=new THREE.Group();this.root.name='Hafenenergie & Hafenwerk';world.scene.add(this.root);
  const r=this.root,p=SERVICE_SITE;
  box(r,p.x,.125,p.z,p.w,.35,p.d,0x626d6d);
  for(const c of SERVICE_FIXTURES){box(r,c.x,.30+c.h/2,c.z,c.w*2,c.h,c.d*2,c.kind==='pump'?0xe7e1c9:c.kind==='post'?0x37685f:0x8c9997);world.colliders.push({...c,h:c.h+.30});}
  box(r,-33,5.35,-87,30,.40,13,0x315f57);box(r,-33,5.12,-80.55,30,.18,.08,0xe7bc62);
  sign(r,'HAFEN ENERGIE',-33,5.36,-80.28,14,.48,'#fff1c5','#244f47');
  box(r,-33,4.95,-104,21,.30,11,0x3f5558);
  sign(r,'HAFENWERK · SERVICE',-33,4.56,-98.43,15,.48,'#f2e1b9','#253d40');
  world.overheadColliders.push({x:-33,z:-87,w:15,d:6.5,minY:5.15,h:5.55},{x:-33,z:-104,w:10.5,d:5.5,minY:4.8,h:5.1});
  for(const [i,b] of SERVICE_BAYS.entries()){
   const v=b.vehicle;
   for(const dx of [-2.15,2.15])box(r,v.x+dx,.312,v.z,.065,.02,6.8,0xe6d5aa);
   box(r,v.x,.312,v.z-3.4,4.3,.02,.065,0xe6d5aa);
   if(b.kind==='fuel'){
    sign(r,'SUPER E5 · '+(i+1),b.x-1.2,2.36,b.z+1.12,1,.28,'#ffffff','#183a36');
    sign(r,'SUPER E5 · '+(i+1),b.x-1.2,2.36,b.z-1.12,1,.28,'#ffffff','#183a36',Math.PI);
    box(r,b.x-1.2,1.84,b.z+1.115,.85,.42,.025,0x162c2c);
    cylinder(r,b.x-.58,1.15,b.z,.045,1.4,0x17292b);box(r,b.x-.56,1.83,b.z,.13,.28,.16,0x252e2c);
   }else{
    for(const dx of [-1.5,1.5])box(r,v.x+dx,.37,v.z,.35,.14,5.2,0x394c51);
    sign(r,'SERVICE · E',b.x,1.5,b.z-.8,2,.35,'#ffffff','#253d40');
   }
  }
  this.priceRoot=new THREE.Group();r.add(this.priceRoot);this.sign=sign;
 }
 update(s){this.root.visible=!s.inside;const price=fuelPrice(s);if(this.price===price)return;this.price=price;
  for(const child of [...this.priceRoot.children]){child.geometry?.dispose();this.priceRoot.remove(child);}
  this.sign(this.priceRoot,'SUPER E5 · '+(price/100).toFixed(2)+' €/L',-33,4.75,-80.27,8,.42,'#fff1c5','#183a36');
 }
}
