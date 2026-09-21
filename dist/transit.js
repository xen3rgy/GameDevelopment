export const TRANSIT_LOCATIONS=[
 {id:'transitCenter',name:'Haltestelle Stadtmitte',type:'transit',x:9,z:9,icon:'▰',color:'#8fc4d0',address:'Lindenplatz · Stadtmitte'},
 {id:'transitDepot',name:'Haltestelle Westhafen',type:'transit',x:-50,z:-73,icon:'▰',color:'#8fc4d0',address:'Hafenstraße · Westhafen'},
 {id:'transitPark',name:'Haltepunkt Lindenpark',type:'transit',x:90,z:76,icon:'▤',color:'#d5b97f',address:'Parkring · Lindenpark'}
];
export const TRANSIT_STOPS={
 station:{id:'station',name:'Bahnhof Lindenstadt West',x:-175,z:-31,address:'Bahnhofplatz 1'},
 transitCenter:TRANSIT_LOCATIONS[0],transitDepot:TRANSIT_LOCATIONS[1],transitPark:TRANSIT_LOCATIONS[2]
};
export const TRANSIT_LINES=[
 {id:'bus2',kind:'bus',name:'Bus 2',a:'station',b:'transitCenter',fare:240,ride:16,every:20,offset:0,start:300,end:1380},
 {id:'bus5',kind:'bus',name:'Bus 5',a:'station',b:'transitDepot',fare:280,ride:22,every:30,offset:10,start:330,end:1350},
 {id:'rail1',kind:'rail',name:'Regionalbahn R1',a:'station',b:'transitPark',fare:420,ride:12,every:60,offset:15,start:315,end:1335}
];
export const transitStop=id=>TRANSIT_STOPS[id]||null;
export const lineById=id=>TRANSIT_LINES.find(l=>l.id===id)||null;
const absolute=(day,minute)=>(day-1)*1440+minute;
export function nextDeparture(s,line,from){
 if(!line||![line.a,line.b].includes(from))return null;
 const current=absolute(s.day,s.minute),directionOffset=from===line.b?Math.floor(line.every/2):0;
 for(let dayOffset=0;dayOffset<3;dayOffset++){
  const dayBase=(s.day-1+dayOffset)*1440,start=dayBase+line.start+line.offset+directionOffset,end=dayBase+line.end+directionOffset;
  if(current>end)continue;
  const n=Math.max(0,Math.ceil((current-start)/line.every)),depart=start+n*line.every;
  if(depart<=end)return{depart,wait:Math.max(0,depart-current)};
 }
 return null;
}
export function transitOffer(s,lineId,from){
 const line=lineById(lineId);if(!line)return null;const next=nextDeparture(s,line,from);if(!next)return null;
 const to=from===line.a?line.b:line.a,stop=transitStop(to);return{...next,line,to,stop,fare:line.fare,ride:line.ride,total:next.wait+line.ride};
}
export const transitOptions=(s,stopId)=>TRANSIT_LINES.filter(l=>l.a===stopId||l.b===stopId).map(l=>transitOffer(s,l.id,stopId)).filter(Boolean);
export function validateTransitState(s){
 const r=s.mobility?.lastTransit;if(!r)return;
 if(!lineById(r.line)||!transitStop(r.from)||!transitStop(r.to)||!Number.isSafeInteger(r.day)||r.day<1||!Number.isFinite(r.minute)||r.minute<0||r.minute>=1440||!Number.isSafeInteger(r.cost)||r.cost<0)throw Error('Ungültige ÖPNV-Historie.');
}
