// Half extents shared by scene, navigation and collision. Keep existing main walking aisles clear.
export const CITY_CHARACTER_FIXTURES=[
 {id:'viaduct-north',kind:'camp',x:-144.8,z:-38.2,w:2.45,d:2.1,h:1.8},
 {id:'viaduct-south',kind:'camp',x:-144.8,z:37.6,w:2.45,d:2.1,h:1.8},
 {id:'kiosk-bin',kind:'dumpster',x:-185.9,z:-18.8,w:.77,d:.51,h:1.25},
 {id:'workshop-bin',kind:'dumpster',x:-160,z:49.4,w:.77,d:.51,h:1.25},
 {id:'courtyard-seat-a',kind:'courtSeat',x:51.5,z:33.8,w:.44,d:1.42,h:1.15,angle:Math.PI/2},
 {id:'courtyard-seat-b',kind:'courtSeat',x:59,z:33.8,w:.44,d:1.42,h:1.15,angle:-Math.PI/2},
 {id:'courtyard-planter-a',kind:'courtGarden',x:51.5,z:38.5,w:.72,d:1.35,h:1.4},
 {id:'courtyard-planter-b',kind:'courtGarden',x:59,z:38.5,w:.72,d:1.35,h:1.4},
 ...[51.3,59].flatMap(x=>[19,24].map(z=>({id:`pergola-${x}-${z}`,kind:'pergolaPost',x,z,w:.08,d:.08,h:2.9})))
];
export const CITY_CHARACTER_OVERHEAD=[{x:55.15,z:21.5,w:4.2,d:2.8,minY:2.75,h:3.08}];
export const CITY_COURTYARD_PATH={x:55.2,z:28,w:10.8,d:29};
// Decorative litter is small and non-blocking, mostly beside walls and service yards.
export const LITTER_ZONES=[
 {x:-187,z:-17,w:2.2,d:1.3,count:10},{x:-160,z:50.8,w:2.2,d:.8,count:9},
 {x:-149.7,z:-36.5,w:.55,d:2,count:7},{x:-149.7,z:39.7,w:.55,d:1.3,count:7},
 {x:-190,z:37.4,w:1.7,d:.55,count:5},{x:-154.5,z:-47.5,w:.5,d:2.4,count:5}
];
export const HORIZON_ROADS=[
 ...[-65,0,65].flatMap(z=>[{x:160,z,w:70,d:13},{x:-252.5,z,w:55,d:13}]),
 ...[-110,0,110].flatMap(x=>[-1,1].map(side=>({x,z:side*167.5,w:13,d:85}))),
 ...[-1,1].map(side=>({x:-202,z:side*145,w:13,d:130}))
];

// Local landmarks use the existing HUD rather than floating world labels.
export function cityLandmarkAt({x,z}){
 if(Math.abs(x-55.2)<5.4&&z>17&&z<42.5)return 'LICHTHOF';
 if(x>-193&&x<-156&&z>-32.5&&z<-20)return 'BAHNHOFSPLATZ';
 if(x>-152&&x<-138&&Math.abs(z)>28&&Math.abs(z)<47)return 'UNTER DEN GLEISEN';
 return null;
}
