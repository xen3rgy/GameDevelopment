import {workshopCarry} from './workshop.js?v=0.7.2';
// Real metres per second. The clock multiplier never changes player movement.
export const PARCEL_WALK_SPEED=2.8;
export function heldLoad(s){
 if(s.riding)return null;
 if(workshopCarry(s)==='supply')return 'parcel';
 if(s.job?.carrying||['crate','wheel','tool'].includes(workshopCarry(s)))return 'crate';
 if(s.interior==='cafe'&&s.cafe?.carrying)return 'tray';
 if(!s.inside&&s.job?.type==='courier')return 'parcel';
 return null;
}
export function walkingProfile(s,sprint=false){
 const load=heldLoad(s),tired=s.needs.energy<10;
 const tiredness=fatigue(s),running=!load&&sprint&&s.needs.energy>10&&tiredness<.8;
 const speed=load?Math.min(tired?2.25:Infinity,{parcel:PARCEL_WALK_SPEED,crate:2.45,tray:2.65}[load]):tired?2.7:running?7.8:4.25;
 return {load,speed,running,canJump:!load,sprintCost:1+tiredness*.65,acceleration:load?10:18};
}

export const fatigue=s=>Math.max(Math.max(0,Math.min(1,((s.dailyLife?.awakeMinutes??600)-960)/600)),Math.max(0,Math.min(1,(30-s.needs.energy)/30)));
