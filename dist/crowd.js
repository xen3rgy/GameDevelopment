// Small sidewalk crowd: reciprocal right-hand passing, distance-based animation.
const clamp=(v,a,b)=>Math.max(a,Math.min(b,v));
export function updateCrowd(people,dt,obstacles=[],canWalk=()=>true){
 for(const p of people){p.distance=0;p.vx=0;p.vz=0}
 let remaining=clamp(dt,0,.2);
 while(remaining>1e-8){
  const step=Math.min(remaining,1/60);remaining-=step;
  const snapshot=people.map(p=>({...p}));
  for(let i=0;i<people.length;i++){
   const p=people[i],old=snapshot[i];
   if(p.x>=(p.maxX??101))p.dir=-1;else if(p.x<=(p.minX??-101))p.dir=1;
   const neighbors=[...snapshot.filter((_,j)=>j!==i),...obstacles].filter(q=>Math.abs(q.z-p.z)<2&&Math.abs(q.x-p.x)<4);
   const ahead=neighbors.filter(q=>(q.x-p.x)*p.dir>-.2&&(q.x-p.x)*p.dir<3&&Math.abs(q.z-p.z)<1.1);
   // Everyone keeps right relative to their own heading; no random left/right flips.
   p.passing=Math.max(0,(p.passing||0)-step);
   if(ahead.length)p.passing=1.5;
   const targetZ=p.lane+(p.passing?-p.dir*.78:0);
   const dx=p.dir,dz=clamp((targetZ-p.z)*2,-1.1,1.1),target=Math.atan2(dx,dz);
   const turn=Math.atan2(Math.sin(target-p.yaw),Math.cos(target-p.yaw));
   p.yaw+=clamp(turn,-step*3.5,step*3.5);
   const speed=p.speed*Math.max(0,Math.cos(turn))**2;
   const travel=speed*step,x=p.x+Math.sin(p.yaw)*travel,z=p.z+Math.cos(p.yaw)*travel;
   // Earlier accepted positions plus later snapshot positions make updates non-overlapping.
   const occupied=[...people.filter((_,j)=>j!==i),...obstacles];
   const clear=occupied.every(q=>{const before=Math.hypot(p.x-q.x,p.z-q.z),after=Math.hypot(x-q.x,z-q.z);return after>=(q.radius||.36)+.36||after>before+1e-7});
   if(clear&&Math.abs(z-p.lane)<=1.05&&canWalk(x,z,.36)){
    p.x=x;p.z=z;p.distance+=travel;
   }
   p.vx=(p.x-old.x)/step;p.vz=(p.z-old.z)/step;
  }
 }
 return people;
}
