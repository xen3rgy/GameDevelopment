// Public transport uses separate interaction and arrival points.
// A stop sign may stand beside furniture/lighting, but a passenger must always exit on validated free pavement.
export const TRANSIT_LOCATIONS=[
 {id:'transitCenter',name:'Haltestelle Stadtmitte',type:'transit',x:12,z:9,arrival:{x:12,z:12,angle:Math.PI},icon:'▰',color:'#8fc4d0',address:'Lindenplatz · Stadtmitte'},
 {id:'transitDepot',name:'Haltestelle Westhafen',type:'transit',x:-50,z:-73,arrival:{x:-50,z:-74,angle:0},icon:'▰',color:'#8fc4d0',address:'Hafenstraße · Westhafen'},
 {id:'transitPark',name:'Haltepunkt Lindenpark',type:'transit',x:90,z:76,arrival:{x:90,z:74,angle:Math.PI},icon:'▤',color:'#d5b97f',address:'Parkring · Lindenpark'}
];
export const TRANSIT_STOPS={
 station:{id:'station',name:'Bahnhof Lindenstadt West',x:-175,z:-31,arrival:{x:-175,z:-29.4,angle:Math.PI},address:'Bahnhofplatz 1'},
 transitCenter:TRANSIT_LOCATIONS[0],transitDepot:TRANSIT_LOCATIONS[1],transitPark:TRANSIT_LOCATIONS[2]
};
export const TRANSIT_LINES=[
 {id:'bus2',kind:'bus',name:'Bus 2',a:'station',b:'transitCenter',fare:240,ride:16,every:20,offset:0,start:300,end:1380},
 {id:'bus5',kind:'bus',name:'Bus 5',a:'station',b:'transitDepot',fare:280,ride:22,every:30,offset:10,start:330,end:1350},
 {id:'rail1',kind:'rail',name:'Regionalbahn R1',a:'station',b:'transitPark',fare:420,ride:12,every:60,offset:15,start:315,end:1335}
];
export const transitStop=id=>TRANSIT_STOPS[id]||null;
export const transitArrival=id=>{const s=transitStop(id);return s?.arrival?{...s.arrival}:{x:s?.x??0,z:s?.z??0,angle:0}};
export const lineById=id=>TRANSIT_LINES.find(l=>l.id===id)||null;
const absolute=(day,minute)=>(day-1)*1440+minute;
export const displayMinutes=n=>Math.max(0,Math.ceil((Number(n)||0)-1e-8));
export const clockText=n=>{const m=Math.floor((((Number(n)||0)%1440)+1440)%1440+1e-7);return String(Math.floor(m/60)).padStart(2,'0')+':'+String(m%60).padStart(2,'0')};

export function nextDeparture(s,line,from){
 if(!line||![line.a,line.b].includes(from)||!Number.isFinite(s?.minute)||!Number.isSafeInteger(s?.day))return null;
 const current=absolute(s.day,s.minute),directionOffset=from===line.b?Math.floor(line.every/2):0;
 for(let dayOffset=0;dayOffset<3;dayOffset++){
  const dayBase=(s.day-1+dayOffset)*1440,start=dayBase+line.start+line.offset+directionOffset,end=dayBase+line.end+directionOffset;
  if(current>end+1e-8)continue;
  const n=Math.max(0,Math.ceil((current-start-1e-8)/line.every)),depart=start+n*line.every;
  if(depart<=end+1e-8)return{depart,wait:Math.max(0,depart-current)};
 }
 return null;
}
export function transitOffer(s,lineId,from){
 const line=lineById(lineId);if(!line||![line.a,line.b].includes(from))return null;const next=nextDeparture(s,line,from);if(!next)return null;
 const to=from===line.a?line.b:line.a,stop=transitStop(to),arrival=transitArrival(to);
 return{...next,line,to,stop,arrival,fare:line.fare,ride:line.ride,total:next.wait+line.ride};
}
export const transitOptions=(s,stopId)=>TRANSIT_LINES.filter(l=>l.a===stopId||l.b===stopId).map(l=>transitOffer(s,l.id,stopId)).filter(Boolean);
export function validateTransitState(s){
 const r=s.mobility?.lastTransit;if(!r)return;
 if(!lineById(r.line)||!transitStop(r.from)||!transitStop(r.to)||!Number.isSafeInteger(r.day)||r.day<1||!Number.isFinite(r.minute)||r.minute<0||r.minute>=1440||!Number.isSafeInteger(r.cost)||r.cost<0)throw Error('Ungültige ÖPNV-Historie.');
}
