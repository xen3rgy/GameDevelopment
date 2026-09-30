import {WORLD_BOUNDS,ROADS,VIADUCT,districtOf,STATION_PLAZAS} from './city-layout.js?v=0.8.1';
import {PAVEMENTS} from './street-layout.js?v=0.8.1';
import {BUILDINGS,LOCATIONS} from './data.js?v=0.8.1';
import {EXPANSION_BUILDINGS,EXPANSION_DISTRICTS,EXPANSION_LANDMARKS,EXPANSION_PATHS} from './expansion-layout.js?v=0.8.1';
import {addressOf} from './orientation.js?v=0.8.1';

export function markerLabel(index){let label='';for(let n=index+1;n>0;n=Math.floor((n-1)/26))label=String.fromCharCode(65+(n-1)%26)+label;return label;}
export const mapPlaces=s=>[...LOCATIONS,...EXPANSION_LANDMARKS.filter(l=>s.discovered?.includes(l.id)).map(l=>({...l,z:l.z-l.d/2+3,type:'landmark',color:'#9ec294'}))];
export function mapTransform(w,h,position,large=false,view={}){
 const b=WORLD_BOUNDS,fit=Math.min((w-44)/(b.maxX-b.minX),(h-44)/(b.maxZ-b.minZ));
 const scale=large?fit*(view.zoom||1):w/135,cx=large?(view.x??(b.minX+b.maxX)/2):position.x,cz=large?(view.z??(b.minZ+b.maxZ)/2):position.z;
 return {scale,cx,cz,w,h,point:(x,z)=>[w/2+(x-cx)*scale,h/2+(z-cz)*scale]};
}
export function drawCityMapBase(ctx,t,s,large){
 const {w,h,scale,point:p}=t,b=WORLD_BOUNDS;
 ctx.fillStyle='#10212b';ctx.fillRect(0,0,w,h);
 const rect=(x,z,ww,dd,col)=>{ctx.fillStyle=col;ctx.fillRect(...p(x-ww/2,z-dd/2),ww*scale,dd*scale);};
 rect((b.minX+b.maxX)/2,(b.minZ+b.maxZ)/2,b.maxX-b.minX,b.maxZ-b.minZ,'#30453e');
 for(const d of EXPANSION_DISTRICTS)rect((d.minX+d.maxX)/2,(d.minZ+d.maxZ)/2,d.maxX-d.minX,d.maxZ-d.minZ,d.color);
 for(const q of [...STATION_PLAZAS,...EXPANSION_PATHS])rect(q.x,q.z,q.w,q.d,['garden','overlook','campus'].includes(q.kind)?'#648262':'#7b8070');
 for(const r of PAVEMENTS)rect(r.x,r.z,r.w,r.d,'#73817a');
 for(const r of ROADS)rect(r.x,r.z,r.w,r.d,r.kind==='local'?'#46595c':'#20313b');
 for(const q of [...BUILDINGS.map(([x,z,w,d])=>({x,z,w,d})),...EXPANSION_BUILDINGS]){rect(q.x+1,q.z+1,q.w,q.d,'#12262d');rect(q.x,q.z,q.w,q.d,q.style==='industrial'?'#a1967d':'#91a4a3');}
 ctx.save();ctx.strokeStyle='#c3b18c';ctx.lineWidth=3*scale;ctx.setLineDash([3,3]);ctx.beginPath();ctx.moveTo(...p(VIADUCT.x,-230));ctx.lineTo(...p(VIADUCT.x,230));ctx.stroke();ctx.restore();
 if(large){ctx.font='600 13px Arial';ctx.textAlign='center';ctx.fillStyle='#f2e8d1';for(const d of EXPANSION_DISTRICTS)ctx.fillText(d.name,...p(d.x,d.z));
  const core=new Map();for(const l of LOCATIONS.filter(l=>l.x<120&&Math.abs(l.z)<120)){const name=districtOf(l),g=core.get(name)||[];g.push(l);core.set(name,g);}for(const [name,ls] of core)ctx.fillText(name,...p(ls.reduce((n,l)=>n+l.x,0)/ls.length,ls.reduce((n,l)=>n+l.z,0)/ls.length+26));
 }
}
export function mapLocationList(s,position){
 const groups=new Map();for(const [i,l] of mapPlaces(s).entries()){const d=districtOf(l);if(!groups.has(d))groups.set(d,[]);groups.get(d).push({l,i});}
 return [...groups].map(([district,places])=>`<details open><summary>${district}</summary>${places.map(({l,i})=>`<button class="map-location" data-action="navigate" data-arg="${l.id}"><span class="map-key">${markerLabel(i)}</span><span>${l.name}<small>${addressOf(l.id)||'Entdeckter Ort'} · ${Math.round(Math.hypot(l.x-position.x,l.z-position.z))} m</small></span></button>`).join('')}</details>`).join('');
}
export function bindCityMapControls(canvas,draw,player){
 canvas.mapView={zoom:1};let drag=null;
 const t=()=>mapTransform(canvas.width,canvas.height,player,true,canvas.mapView),clamp=(v,a,b)=>Math.max(a,Math.min(b,v));
 canvas.onwheel=e=>{e.preventDefault();const r=canvas.getBoundingClientRect(),old=t(),x=(e.clientX-r.left)*canvas.width/r.width-canvas.width/2,z=(e.clientY-r.top)*canvas.height/r.height-canvas.height/2,zoom=clamp(canvas.mapView.zoom*Math.exp(-e.deltaY*.0015),1,5);canvas.mapView.zoom=zoom;const next=t();canvas.mapView.x=old.cx+x/old.scale-x/next.scale;canvas.mapView.z=old.cz+z/old.scale-z/next.scale;draw();};
 canvas.onpointerdown=e=>{drag={x:e.clientX,z:e.clientY};canvas.setPointerCapture(e.pointerId);};
 canvas.onpointermove=e=>{if(!drag)return;const r=canvas.getBoundingClientRect(),old=t();canvas.mapView.x=clamp(old.cx-(e.clientX-drag.x)*canvas.width/r.width/old.scale,WORLD_BOUNDS.minX,WORLD_BOUNDS.maxX);canvas.mapView.z=clamp(old.cz-(e.clientY-drag.z)*canvas.height/r.height/old.scale,WORLD_BOUNDS.minZ,WORLD_BOUNDS.maxZ);drag={x:e.clientX,z:e.clientY};draw();};
 canvas.onpointerup=canvas.onpointercancel=()=>{drag=null;};
 document.querySelector('#mapFit').onclick=()=>{canvas.mapView={zoom:1};draw();};
 document.querySelector('#mapCenter').onclick=()=>{canvas.mapView={zoom:3,x:player.x,z:player.z};draw();};
}
