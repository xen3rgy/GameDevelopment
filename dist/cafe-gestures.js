const clamp=v=>Math.max(0,Math.min(1,v));
const smooth=v=>{v=clamp(v);return v*v*(3-2*v)};
export function gestureEnvelope(time,duration){return smooth(time/.3)*smooth((duration-time)/.4)}
export function cafeGuestSignal(g){
 if(g.stage==='finished'&&g.time>0)return {kind:'thanks',weight:gestureEnvelope(.9-g.time,.9)};
 if(!['order','paying'].includes(g.stage))return null;
 const paying=g.stage==='paying',time=paying?g.paymentWait:g.wait,period=paying?8:11;
 const phase=(time+(g.id%4)*1.4)%period;
 return {kind:paying?'bill':'order',weight:gestureEnvelope(phase,1.9),phase};
}
export function applyCafeGuestSignal(actor,g){
 const signal=cafeGuestSignal(g);if(!signal)return;
 const d=actor.userData,w=signal.weight;if(w<=0)return;
 if(signal.kind==='thanks'){d.upper.rotation.x+=w*.09;return}
 // Raise a hand beside the face; the elbow folds instead of the whole arm circling.
 d.arms[1].rotation.set(-.35-1.15*w,0,.27*w);
 d.elbows[1].rotation.set(-.7-.65*w,0,Math.sin(signal.phase*8)*.09*w);
 d.upper.rotation.y+=.035*w;
}
export const CAFE_GESTURE_DURATION={cafeOrder:1.25,cafePay:1.05,cafeServe:.8,cafeClean:.9};
export function applyCafeWorkerPose(actor,kind,weight,phase=0){
 const d=actor.userData,w=clamp(weight);
 if(kind==='cafeOrder'){
  d.arms[0].rotation.set(-.85*w,0,.8*w);d.elbows[0].rotation.set(-1.0*w,0,0);
  d.arms[1].rotation.set((-1.08+Math.sin(phase*18)*.035)*w,-.18*w,.1*w);d.elbows[1].rotation.set(-.65*w,0,Math.sin(phase*18)*.08*w);
 }else if(kind==='cafePay'){
  d.arms[0].rotation.set(-.9*w,0,-.08*w);d.elbows[0].rotation.set(-.75*w,0,0);
  d.arms[1].rotation.set(-1.0*w,0,.12*w);d.elbows[1].rotation.set(-.45*w,0,0);
 }else if(kind==='cafeServe'){
  for(let i=0;i<2;i++){d.arms[i].rotation.x=-1.15*w;d.elbows[i].rotation.x=-.45*w}
 }else if(kind==='cafeClean'){
  d.arms[1].rotation.set(-1.0*w,Math.sin(phase*10)*.12*w,0);d.elbows[1].rotation.x=-.4*w;
 }
 d.upper.rotation.x+=.055*w;
}
export function staffGesture(e){
 if(!e?.task||e.route.length)return null;
 const duration={order:1.4,pay:1.1,serve:.65}[e.task.kind];if(!duration)return null;
 const kind={order:'cafeOrder',pay:'cafePay',serve:'cafeServe'}[e.task.kind],phase=clamp(1-e.wait/duration);
 return {kind,phase,weight:Math.sin(Math.PI*phase)};
}
