// Absolute foot height: crossing a kerb in flight must never add the new ground height.
export function stepJump(state,ground,dt,pressed,allowed=true){
 const next={y:state?.y??ground,velocity:state?.velocity??0,airborne:state?.airborne??false,held:!!pressed};
 if(!next.airborne){next.y=ground;next.velocity=0;if(pressed&&!state?.held&&allowed){next.velocity=5;next.airborne=true;}}
 if(next.airborne){
  const elapsed=Math.max(0,Math.min(dt,.2));next.y+=next.velocity*elapsed-7*elapsed*elapsed;next.velocity-=14*elapsed;
  if(next.velocity<=0&&next.y<=ground){next.y=ground;next.velocity=0;next.airborne=false;}
 }
 return next;
}
