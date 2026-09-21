import {BUILDINGS} from './data.js?v=0.7.3-map1';
import {ROADS} from './city-layout.js?v=0.7.3-map1';
import {PAVEMENTS,streetSurface} from './street-layout.js?v=0.7.3-map1';
import {GARDEN_PATHS,onGardenPath} from './pedestrian-layout.js?v=0.7.3-map1';
export const FRONTAGE_APRONS=BUILDINGS.filter(([x,,,,,,name])=>x>=-120&&name).map(([x,z,w,d])=>({x,z,w:w+3,d:d+3}));
const inside=(x,z,p)=>Math.abs(x-p.x)<p.w/2&&Math.abs(z-p.z)<p.d/2;
export function frontageSurface(x,z){
 if(!FRONTAGE_APRONS.some(p=>inside(x,z,p))||BUILDINGS.some(([bx,bz,w,d])=>inside(x,z,{x:bx,z:bz,w,d}))||streetSurface(x,z)||onGardenPath(x,z))return null;
 return {height:.30,kind:'paving'};
}
export function frontagePatches(){
 const xs=new Set(),zs=new Set();
 for(const p of [...FRONTAGE_APRONS,...BUILDINGS.map(([x,z,w,d])=>({x,z,w,d})),...ROADS,...PAVEMENTS,...GARDEN_PATHS]){xs.add(p.x-p.w/2);xs.add(p.x+p.w/2);zs.add(p.z-p.d/2);zs.add(p.z+p.d/2);}
 const xx=[...xs].sort((a,b)=>a-b),zz=[...zs].sort((a,b)=>a-b),out=[];
 for(let i=1;i<xx.length;i++)for(let j=1;j<zz.length;j++){const minX=xx[i-1],maxX=xx[i],minZ=zz[j-1],maxZ=zz[j];if(frontageSurface((minX+maxX)/2,(minZ+maxZ)/2))out.push({minX,maxX,minZ,maxZ});}
 return out;
}
