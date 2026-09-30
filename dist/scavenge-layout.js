import {EXPANSION_BINS,EXPANSION_STOPS} from './expansion-layout.js?v=0.8.1';
import {NEIGHBORHOOD_FIXTURES} from './neighborhood-layout.js?v=0.8.1';
export const PUBLIC_BINS=NEIGHBORHOOD_FIXTURES.filter(f=>f.kind==='bin').map((f,id)=>({id,x:f.x,z:f.z+.95}));
PUBLIC_BINS.push(...EXPANSION_BINS);
// Hand-picked pavement/path positions; no arbitrary random coordinates.
const areas={
 outer:{weight:2,points:EXPANSION_STOPS.flatMap(p=>[-6,3,11].map(dx=>[p.x+dx,p.z+1]))},
 center:{weight:1,points:[[-45,-10],[-16,10],[35,-10]]},
 station:{weight:4,points:[[-162,-16],[-167,-18],[-172,-20],[-160,-27],[-166,-29],[-174,12],[-164,19],[-161,32],[-162,45]]},
 side:{weight:3,points:[[-87,-50],[-77,-50],[-58,-50],[-38,-50],[25,-50],[49,-50],[74,-50],[-85,51],[-57,51],[27,51],[53,51],[81,51]]},
 park:{weight:3,points:[[73,79],[79,86],[73,97],[79,105],[65,91],[87,95]]},
 residential:{weight:2,points:[[-86,11],[-43,11],[24,11],[56,11],[90,11]]},
 bins:{weight:4,points:PUBLIC_BINS.map(b=>[b.x+(b.id===2||b.id===3?0:1.2),b.z])}
};
export const BOTTLE_CANDIDATES=Object.entries(areas).flatMap(([area,{weight,points}])=>points.map(([x,z])=>({x,z,area,weight})));
export const NIGHT_CLUSTERS=[[[73,90],[73,91.5],[73,93]], [[-161,18],[-162.5,18],[-164,18]], [[59,-10],[58.5,-10],[60,-10]]];
