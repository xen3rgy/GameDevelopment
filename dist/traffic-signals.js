import {ROAD_JUNCTIONS} from './street-layout.js?v=0.8.1';
export const SIGNAL_JUNCTIONS=ROAD_JUNCTIONS.filter(p=>[-110,0,110,220].includes(p.x)&&[-65,0,65].includes(p.z));
export const hasSignal=j=>SIGNAL_JUNCTIONS.some(p=>p.x===j.x&&p.z===j.z);
// Real seconds, shared by vehicle decisions and the visible lamps. Two clearance phases.
export function signalPhase(j,time,axis){
 const offset=(Math.abs(j.x)*.08+Math.abs(j.z)*.12)%48,t=((time+offset)%48+48)%48;
 if(axis==='x')return t<18?'green':t<21?'amber':'red';
 return t>=24&&t<42?'green':t>=42&&t<45?'amber':'red';
}
export function signalBlocks(car,pose,j,time){
 if(!hasSignal(j))return false;
 const dx=Math.sin(car.angle),dz=Math.cos(car.angle),axis=Math.abs(dx)>Math.abs(dz)?'x':'z';
 if(signalPhase(j,time,axis)==='green')return false;
 const line=12+car.length/2;
 // Committed vehicles clear the junction even if the phase changes behind them.
 const before=(j.x-car.x)*dx+(j.z-car.z)*dz;
 const after=(j.x-pose.x)*dx+(j.z-pose.z)*dz;
 return before>line&&after<=line;
}
