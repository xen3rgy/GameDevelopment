// One source of truth for furniture, collision, service positions and walking paths.
export const CAFE_TABLES=[{x:295,z:80},{x:305,z:80},{x:305,z:84}];
export const TABLE_RADIUS=.68,CHAIR_OFFSET=1.12;
export const CAFE_POINTS={cafeExit:{x:300,z:86.8,name:'Café verlassen'},cafeOffice:{x:294.5,z:85.7,name:'Café Morgen · Betriebsbuch'},cafePrep:{x:300,z:76.5,name:'Kaffeemaschine · Bestellungen & Abholung'},...Object.fromEntries(CAFE_TABLES.map((p,i)=>['cafeTable'+i,{x:p.x,z:p.z+1.25,name:'Tisch '+(i+1),seat:i}]))};
export const STAFF_HOME=[{x:300,z:73.55},{x:300,z:76.5},{x:298.5,z:76.5}];
export const STAFF_ROLES=['Barista','Service','Abräumen'];
export const CAFE_FIXTURES=[{x:300,z:74.8,w:3.55,d:.72,h:1.14},{x:293,z:85,w:.55,d:.9,h:1.15},...CAFE_TABLES.flatMap(p=>[{x:p.x,z:p.z,w:TABLE_RADIUS,d:TABLE_RADIUS,radius:TABLE_RADIUS,h:.82},...[-1,1].map(side=>({x:p.x+side*CHAIR_OFFSET,z:p.z,w:.31,d:.31,h:1.04}))]),...[[293,73.3],[307,73.3],[307,86.5]].map(([x,z])=>({x,z,w:.3,d:.3,radius:.3,h:1.3}))];
export function guestSeat(seat,side=-1){const p=CAFE_TABLES[seat];return {x:p.x+side*CHAIR_OFFSET,z:p.z,angle:-side*Math.PI/2}}
export function guestPath(seat,side=-1){const p=CAFE_TABLES[seat],end=guestSeat(seat,side);return [{x:300+side*.35,z:87},{x:300+side*.35,z:p.z+1.5},{x:end.x,z:p.z+1.5},{x:end.x,z:end.z}]}
export function pathLength(path){return path.slice(1).reduce((n,p,i)=>n+Math.hypot(p.x-path[i].x,p.z-path[i].z),0)}
export function samplePath(path,distance){for(let i=1;i<path.length;i++){const a=path[i-1],b=path[i],length=Math.hypot(b.x-a.x,b.z-a.z);if(length<1e-8)continue;if(distance<=length){const t=Math.max(0,distance)/length;return {x:a.x+(b.x-a.x)*t,z:a.z+(b.z-a.z)*t,angle:Math.atan2(b.x-a.x,b.z-a.z)}}distance-=length}const p=path.at(-1),a=path.at(-2);return {...p,angle:Math.atan2(p.x-a.x,p.z-a.z)}}
export function guestWalkDuration(seat,side){return (side?pathLength(guestPath(seat,side)):Math.max(...[-1,1].map(side=>pathLength(guestPath(seat,side)))))/1.3+.8}
export function staffWalkable(x,z,r=.35){return x>292.25+r&&x<307.75-r&&z>72.25+r&&(z<87.75-r||Math.abs(x-300)<.9&&z<=87.75)&&!CAFE_FIXTURES.some(f=>f.radius!=null?Math.hypot(x-f.x,z-f.z)<f.radius+r:Math.abs(x-f.x)<f.w+r&&Math.abs(z-f.z)<f.d+r)}
export function staffPath(from,to){
 const start={x:from.x,z:from.z},end={x:to.x,z:to.z},clear=staffSegmentClear;
 if(clear(start,end))return [start,end];
 const corners=CAFE_FIXTURES.flatMap(f=>[-1,1].flatMap(x=>[-1,1].map(z=>({x:f.x+x*(f.w+.44),z:f.z+z*(f.d+.44)})))).filter(p=>staffWalkable(p.x,p.z));
 const nodes=[start,end,...corners],dist=nodes.map(()=>Infinity),prev=nodes.map(()=>-1),done=new Set();dist[0]=0;
 while(done.size<nodes.length){let cur=-1;for(let i=0;i<nodes.length;i++)if(!done.has(i)&&(cur<0||dist[i]<dist[cur]))cur=i;if(cur<0||!Number.isFinite(dist[cur]))break;if(cur===1){const route=[];for(let n=1;n>=0;n=prev[n])route.unshift(nodes[n]);return route}done.add(cur);
 for(let i=0;i<nodes.length;i++)if(!done.has(i)&&clear(nodes[cur],nodes[i])){const d=dist[cur]+Math.hypot(nodes[cur].x-nodes[i].x,nodes[cur].z-nodes[i].z);if(d<dist[i]){dist[i]=d;prev[i]=cur}}}
 return [start];
}

function staffSegmentClear(a,b){
 const r=.36,dx=b.x-a.x,dz=b.z-a.z,len2=dx*dx+dz*dz;
 if(!staffWalkable(a.x,a.z,.35)||!staffWalkable(b.x,b.z,.35))return false;
 return !CAFE_FIXTURES.some(f=>{
  if(f.radius!=null){const t=len2?Math.max(0,Math.min(1,((f.x-a.x)*dx+(f.z-a.z)*dz)/len2)):0;return Math.hypot(a.x+t*dx-f.x,a.z+t*dz-f.z)<f.radius+r;}
  let lo=0,hi=1;
  for(const [value,delta,center,half] of [[a.x,dx,f.x,f.w+r],[a.z,dz,f.z,f.d+r]]){if(Math.abs(delta)<1e-10){if(Math.abs(value-center)>=half)return false;}else{let p=(center-half-value)/delta,q=(center+half-value)/delta;if(p>q)[p,q]=[q,p];lo=Math.max(lo,p);hi=Math.min(hi,q);if(lo>=hi)return false;}}
  return true;
 });
}
