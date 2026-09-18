import * as THREE from './vendor/three.module.js';

// Four shared projectors keep traffic lighting affordable, including on low quality.
// The player's running car has priority; parked owned vehicles keep their lights off.
export class VehicleLights {
 constructor(scene){
  this.slots=Array.from({length:2},()=>({car:null,power:0,lights:[-.6,.6].map(x=>{
   const light=new THREE.SpotLight(0xffedcf,0,38,Math.PI*.18,.65,2);
   light.userData.offset=x;scene.add(light,light.target);return light;
  })}));
 }
 update(traffic,owned,position,night,dt,inside=false,quality='high',riding=false){
  const running=[...traffic];if(owned&&riding)running.push(owned);
  for(const car of new Set([...traffic,owned].filter(Boolean))){const lamps=car.userData.vehicleLights;if(!lamps)continue;
   const power=!inside&&running.includes(car)?night:0;
   lamps.headlights.emissiveIntensity=.15+power*4.5;
   lamps.taillights.emissiveIntensity=.1+power*1.8+(!inside&&car.userData.braking?2.5:0);
  }
  const current=new Set(this.slots.map(s=>s.car));
  const selected=inside?[]:running.filter(c=>c.visible&&c.userData.vehicleLights).map(car=>({car,distance:car.position.distanceToSquared(position)}))
   .filter(v=>v.distance<85*85).sort((a,b)=>(a.car===owned?-1:a.distance*(current.has(a.car)?.75:1))-(b.car===owned?-1:b.distance*(current.has(b.car)?.75:1)))
   .slice(0,quality==='low'?1:2).map(v=>v.car);
  const wanted=new Set(selected),occupied=new Set(this.slots.map(s=>s.car).filter(Boolean));
  const step=Math.max(0,Math.min(dt,.1))*4;
  for(const slot of this.slots){
   for(const light of slot.lights)light.visible=!inside;
   if(slot.car&&!wanted.has(slot.car)){slot.power=Math.max(0,slot.power-step);if(slot.power===0){occupied.delete(slot.car);slot.car=null}}
   if(!slot.car){slot.car=selected.find(car=>!occupied.has(car))||null;if(slot.car)occupied.add(slot.car)}
   if(slot.car&&wanted.has(slot.car))slot.power=Math.min(1,slot.power+step);
   if(slot.car){
    const car=slot.car,front=car.userData.vehicleLights.front;car.updateWorldMatrix(true,false);
    const distance=car.position.distanceTo(position),fade=Math.max(0,Math.min(1,(85-distance)/25));
    for(const light of slot.lights){
     light.position.set(light.userData.offset,.8,front).applyMatrix4(car.matrixWorld);
     // Local +Z is forward. The dipped beam meets the road about 12 metres ahead.
     light.target.position.set(light.userData.offset,.04,front+12).applyMatrix4(car.matrixWorld);
     light.intensity=inside?0:night*slot.power*fade*1250;
    }
   }else for(const light of slot.lights)light.intensity=0;
  }
 }
}
