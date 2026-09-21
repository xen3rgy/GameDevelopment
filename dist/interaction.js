// Shared interaction rules. Keep world picking and HUD language consistent.
export const INTERACTION_RANGES=Object.freeze({
  default:3,
  inside:1.9,
  bottle:2,
  drop:2.2,
  vehicle:3,
  person:2.5,
  location:3.1,
  trash:2.2,
  crate:2.2,
  shelf:2.2,
  cafeGuest:1.9
});
const PRIORITY=Object.freeze({
  trash:-.18,crate:-.18,shelf:-.18,cafeGuest:-.16,
  bottle:-.08,drop:-.06,vehicle:-.04,person:.04,location:.24
});
export function interactionRange(target,inside=false){
  if(Number.isFinite(target?.range)&&target.range>0)return target.range;
  if(inside)return INTERACTION_RANGES[target?.type]??INTERACTION_RANGES.inside;
  return INTERACTION_RANGES[target?.type]??INTERACTION_RANGES.default;
}
export function withInteractionDistance(target,origin,inside=false){
  const dist=Math.hypot((target?.x??0)-(origin?.x??0),(target?.z??0)-(origin?.z??0));
  return {...target,dist,range:interactionRange(target,inside)};
}
export function interactionScore(target,origin,inside=false){
  const v=Number.isFinite(target?.dist)?target:withInteractionDistance(target,origin,inside);
  return v.dist/Math.max(.01,v.range)+(PRIORITY[v.type]??0);
}
export function interactionVerb(target){
  if(!target)return 'INTERAGIEREN';
  if(['trash','crate','shelf'].includes(target.type))return 'ARBEITEN';
  if(['bottle','drop'].includes(target.type))return 'AUFHEBEN';
  if(target.type==='person'||target.type==='cafeGuest')return 'SPRECHEN';
  if(target.type==='vehicle')return 'FAHRZEUG';
  if(['exit','shopExit','cafeExit','workshopExit'].includes(target.type))return 'VERLASSEN';
  if(target.type==='checkout')return 'BEZAHLEN';
  return 'INTERAGIEREN';
}
