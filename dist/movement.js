// Pure motion rules shared by the world and regression tests. Distances are metres.
export const approach = (value, target, amount) => value < target ? Math.min(target, value + amount) : Math.max(target, value - amount);
export function moveWithCollision(position, dx, dz, canWalk, radius=.35, slide=true) {
  const next={...position}; let blocked=false;
  // Smaller substeps reduce corner tunnelling and make collision behaviour independent of frame rate.
  const steps=Math.max(1,Math.ceil(Math.hypot(dx,dz)/.16)),sx=dx/steps,sz=dz/steps;
  const attempt=(base,first)=>{
    const out={...base};
    const axes=first==='x'?[['x',sx],['z',sz]]:[['z',sz],['x',sx]];
    for(const [axis,delta] of axes){
      if(!delta)continue;
      const candidate={...out,[axis]:out[axis]+delta};
      if(canWalk(candidate.x,candidate.z,radius))Object.assign(out,candidate);
    }
    return out;
  };
  for(let i=0;i<steps;i++){
    const x=next.x+sx,z=next.z+sz;
    if(canWalk(x,z,radius)){next.x=x;next.z=z;continue}
    blocked=true;if(!slide)break;
    // Evaluate both slide orders. Fixed X-then-Z ordering could pin the player on some
    // convex corners even though a valid tangential step existed.
    const a=attempt(next,'x'),b=attempt(next,'z');
    const da=Math.hypot(a.x-next.x,a.z-next.z),db=Math.hypot(b.x-next.x,b.z-next.z);
    const best=db>da?b:a;
    next.x=best.x;next.z=best.z;
  }
  return {...next,blocked,distance:Math.hypot(next.x-position.x,next.z-position.z)};
}
export function stepVehicle(vehicle, input, dt, canWalk, maximumSpeed){
  const throttle=Number(!!input.forward)-Number(!!input.backward);
  const steer=Number(!!input.left)-Number(!!input.right);
  let speed=Number.isFinite(vehicle.speed)?vehicle.speed:0;
  const powered=vehicle.id==='bike'||vehicle.fuel>0;
  const top=maximumSpeed*(vehicle.condition<20?.5:1);
  if(input.brake)speed=approach(speed,0,dt*15);
  else if(!powered||!throttle)speed=approach(speed,0,dt*(vehicle.id==='bike'?2:1.2));
  else speed=approach(speed,throttle>0?top:-top*.3,dt*(throttle*Math.sign(speed)<0?13:5));
  const radius=vehicle.id==='bike'?.48:1.3,currentAngle=vehicle.angle??Math.PI;
  let angle=currentAngle+steer*Math.min(1,Math.abs(speed)/2.5)*Math.sign(speed)*dt*(vehicle.id==='bike'?2.1:1.5)/(1+Math.abs(speed)*.045);
  if(!canWalk(vehicle.x,vehicle.z,radius,angle))angle=currentAngle;
  const result=moveWithCollision(vehicle,Math.sin(angle)*speed*dt,Math.cos(angle)*speed*dt,(x,z,r)=>canWalk(x,z,r,angle),radius,false);
  return {...result,angle,speed:result.blocked?0:speed};
}
export function segmentClear(a,b,canWalk,radius=.35){
  const steps=Math.max(1,Math.ceil(Math.hypot(b.x-a.x,b.z-a.z)/.32));
  for(let i=1;i<=steps;i++){const t=i/steps;if(!canWalk(a.x+(b.x-a.x)*t,a.z+(b.z-a.z)*t,radius))return false}return true;
}
