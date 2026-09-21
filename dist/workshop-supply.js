import {deliveryLeg} from './delivery-routes.js?v=0.7.3-map1';
export const SUPPLY_BASE=800,SUPPLY_BONUS=200;
export function supplyPlan(s){
 const leg=deliveryLeg('deliveryWorkshop','depot');if(!leg)return null;
 const speed=[1,4,10].includes(s.settings.speed)?s.settings.speed:1;
 const out=Math.ceil(leg.distance/3.8+20),back=Math.ceil(leg.distance/2.55+25);
 return {distance:leg.distance,returnMinutes:back*speed,totalMinutes:(out+back)*speed,speed};
}
export const needsSupply=s=>{const a=s.workshop?.active;return !!a?.supply&&a.supply.phase!=='received';};
export const supplyTarget=s=>s.workshop?.active?.supply?.phase==='requested'?'depot':'deliveryWorkshop';
export function supplyReceipt(s){const a=s.workshop?.active,p=a?.supply;if(!p||p.phase!=='carrying')return null;const onTime=s.day*1440+s.minute<=p.deadline;return {order:a.id,day:s.day,base:SUPPLY_BASE,bonus:onTime?SUPPLY_BONUS:0,total:SUPPLY_BASE+(onTime?SUPPLY_BONUS:0),late:!onTime,duration:Math.round(s.day*1440+s.minute-p.started)};}
