export const ADDRESSES={
 station:['Bahnhofsplatz',1],stationHome:['Gleishof',4],deliveryKiosk:['Bahnhofsplatz',3],deliveryWorkshop:['Gleishof',8],
 shelter:['Lindenallee',2],market:['Lindenallee',6],recycle:['Lindenallee',6],jobs:['Lindenallee',12],cafe:['Lindenallee',18],
 school:['Lindenallee',1],home:['Lindenallee',5],bank:['Lindenallee',11],garage:['Lindenallee',19],
 depot:['Hafenstraße',2],deliveryC:['Hafenstraße',12],agency:['Hafenstraße',18],
 deliveryB:['Am Kanal',3],deliveryA:['Am Kanal',11],park:['Am Kanal',17]
};
export const addressOf=id=>ADDRESSES[id]?.join(' ')||'';
export const STREETS=[{name:'Hafenstraße',z:-65},{name:'Lindenallee',z:0},{name:'Am Kanal',z:65}];
export function streetAt(p){
 if(p.x<-120){if(Math.abs(p.x+202)<15)return 'Westbogen';if(Math.abs(p.z)<14)return 'Lindenallee West';if(Math.abs(p.z+65)<14)return 'Hafenstraße West';if(Math.abs(p.z-65)<14)return 'Am Kanal West';return p.z<0?'Bahnhofsplatz':'Gleishof';}
 const street=STREETS.reduce((best,s)=>Math.abs(p.z-s.z)<Math.abs(p.z-best.z)?s:best);
 if(Math.abs(p.z-street.z)<15)return street.name;
 return p.x<-95?'Westkai':p.x>95?'Parkring':Math.abs(p.x)<15?'Bahnhofstraße':'Hinterhöfe';
}
// Directions describe the next walkable leg, never a straight line through a building.
export function routeGuidance(position,target,route,riding=false){
 if(!target)return null;
 const direct=Math.hypot(target.x-position.x,target.z-position.z);
 if(direct<3)return {distance:Math.ceil(direct),instruction:riding?'Angekommen · anhalten und aussteigen':'Angekommen · E zum Interagieren'};
 const end=route?.at(-1),usable=!riding&&route?.length>1&&Math.hypot(end.x-target.x,end.z-target.z)<.1;
 if(!usable)return {distance:Math.round(direct),instruction:riding?'Zielrichtung · Straßen folgen':'Zielrichtung · Route wird gesucht'};
 const next=route[1],dx=next.x-position.x,dz=next.z-position.z,leg=Math.hypot(dx,dz);
 const direction=Math.abs(dx)>Math.abs(dz)?dx>0?'Osten':'Westen':dz>0?'Süden':'Norden';
 const distance=leg+route.slice(2).reduce((sum,p,i)=>sum+Math.hypot(p.x-route[i+1].x,p.z-route[i+1].z),0);
 return {distance:Math.round(distance),instruction:`${Math.ceil(leg)} m Richtung ${direction}${route.length>2?' · dann dem Weg folgen':' · bis zum Eingang'}`};
}
