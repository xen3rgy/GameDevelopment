// Shared exterior coordinates; collider widths/depths are half extents.
export const TUNING_SITE={x:187,z:205,w:38,d:28};
export const TUNING_BUILDING={x:195,z:205,w:18,d:16};
export const TUNING_BAY={x:190,z:205,angle:Math.PI/2,w:4.8,d:7.2};
export const TUNING_LOCATION={id:'werkform',name:'WERKFORM',type:'tuning',x:186,z:210,icon:'◉',color:'#d9bb81',description:'Fahrzeugdesign & Tuning · Werkhof 7 · Südwerk'};
export const TUNING_FIXTURES=[
 {x:204,z:205,w:.2,d:8,h:4.5},{x:195,z:197,w:9,d:.2,h:4.5},{x:195,z:213,w:9,d:.2,h:4.5},
 {x:201,z:210.9,w:2,d:.65,h:1.1},{x:201,z:198.2,w:2,d:.65,h:2},
 {x:192,z:211.9,w:.7,d:.7,h:1.2},{x:186.7,z:210,w:.45,d:.45,h:1.2},
 ...[198,212].map(z=>({x:184,z,w:.13,d:.13,h:.9}))
];
// 1 m longitudinal / .85 m lateral tolerance; either direction can enter safely.
export function tuningAligned(v){return !!v&&!v.stored&&['car','van','sport'].includes(v.id)&&Number.isFinite(v.angle)&&Math.abs(v.x-TUNING_BAY.x)<=1&&Math.abs(v.z-TUNING_BAY.z)<=.85&&Math.abs(Math.cos(v.angle))<=.3&&Math.abs(v.speed)<=.1;}
