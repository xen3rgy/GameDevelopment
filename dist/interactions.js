// One source for prompts and authoritative action checks, measured in world metres.
export const REACH={indoor:1.9,location:3,person:3,vehicle:2.6,bottle:1.9,drop:1.9,work:2.2,trunk:2.2,transit:2.6};
export const inReach=(a,b,kind='location')=>!!a&&!!b&&Number.isFinite(a.x)&&Number.isFinite(a.z)&&Math.hypot(a.x-b.x,a.z-b.z)<=REACH[kind]+1e-7;
export function interactionReach(type,inside=false){return inside?REACH.indoor:REACH[type]??(['trash','crate','shelf'].includes(type)?REACH.work:REACH.location);}
