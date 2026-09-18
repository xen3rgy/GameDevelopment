// Short, explicit routes keep customers out of work bays and the player entrance.
export const CUSTOMER_ROUTE=[{x:340,z:89.1},{x:340,z:87.9},{x:337.1,z:87.9},{x:337.1,z:84.6},{x:336.4,z:84.6},{x:336.4,z:85.5}];
export const TESSA_ROUTE=[{x:333.6,z:87.65},{x:337.9,z:87.65},{x:338.5,z:83.1},{x:339,z:78.4},{x:342.5,z:78.4}];
export const CUSTOMER_NAMES=['Lena','Alex','Mika','Robin','Sam','Kim'];
const now=s=>s.day*1440+s.minute;
export const newWorkshopLife=()=>({serial:0,nextArrival:0,parked:[],people:[],tessa:{x:333.6,z:87.65,segment:0,offset:0,phase:0,pause:0}});
function walk(p,path,dt){
 let budget=Math.max(0,dt)*1.15;
 while(budget>0&&p.node<path.length){const t=path[p.node],d=Math.hypot(t.x-p.x,t.z-p.z);if(d<1e-7){p.node++;continue;}const length=Math.min(d,budget);p.angle=Math.atan2(t.x-p.x,t.z-p.z);p.x+=(t.x-p.x)*length/d;p.z+=(t.z-p.z)*length/d;budget-=length;if(length>=d-1e-7)p.node++;}
 return p.node>=path.length;
}
export function tickWorkshopLife(s,seconds){
 if(!Number.isFinite(seconds)||seconds<=0)return;
 const w=s.workshop,l=w.life??=newWorkshopLife(),time=now(s),open=s.minute>=480&&s.minute<1130;
 for(const p of l.people){
  if(p.phase==='entering'&&walk(p,CUSTOMER_ROUTE,seconds)){p.phase='waiting';p.wait=12;p.node=0;}
  if(p.phase==='waiting'){
   if(Math.hypot(l.tessa.x-TESSA_ROUTE[0].x,l.tessa.z-TESSA_ROUTE[0].z)<.4)p.wait-=seconds*s.settings.speed;
   if(p.wait<=0||!open){
    p.phase='leaving';p.node=0;
    if(p.kind==='dropoff'&&open&&l.parked.length<3){l.parked.push({id:p.id,bike:p.bike,ready:time+90+(p.id%3)*35,owner:p.owner});p.withBike=false;}
    if(p.kind==='pickup'){l.parked=l.parked.filter(v=>v.id!==p.job);p.withBike=true;}
   }
  }
  if(p.phase==='leaving'&&walk(p,[...CUSTOMER_ROUTE].reverse(),seconds))p.done=true;
 }
 l.people=l.people.filter(p=>!p.done);
 const ready=l.parked.find(p=>p.ready<=time&&!l.people.some(v=>v.job===p.id));
 const inflight=l.people.filter(p=>p.kind==='dropoff'&&p.phase!=='leaving').length;
 if(open&&l.people.length<1&&(ready||time>=l.nextArrival&&l.parked.length+inflight<3)){
  const id=++l.serial,kind=ready?'pickup':'dropoff';
  l.people.push({id,kind,job:ready?.id??null,owner:ready?.owner??id%6,bike:ready?.bike??id%3,withBike:kind==='dropoff',phase:'entering',node:1,x:CUSTOMER_ROUTE[0].x,z:CUSTOMER_ROUTE[0].z,angle:Math.PI,wait:12});
  l.nextArrival=time+(s.minute<660||s.minute>=960?65:105)+(id%3)*13;
 }
 // Tessa walks a continuous route to the parts bay and back. Approaching the desk calls her back.
 const t=l.tessa,nearDesk=s.interior==='workshop'&&Math.hypot(s.position.x-333.6,s.position.z-85.5)<3.2;
 const needsDesk=nearDesk||l.people.some(p=>p.phase==='waiting')||!open,workToDo=!!w.active?.action||l.parked.some(p=>p.ready>time);
 if(t.phase===2){if(needsDesk){t.pause=0;t.phase=1;}else {t.pause=Math.max(0,t.pause-seconds*s.settings.speed);if(t.pause>0)return;t.phase=1;}}
 if(needsDesk&&t.phase===0){if(t.offset)t.segment++;t.phase=1;}
 else if(!needsDesk&&t.segment===0&&t.offset===0&&workToDo)t.phase=0;
 if(!needsDesk&&!workToDo&&t.segment===0&&t.offset===0)return;
 let budget=seconds*1.05;
 while(budget>0){const i=t.segment,next=t.phase===0?i+1:i-1;
  if(next<0){t.offset=0;break;}if(next>=TESSA_ROUTE.length){t.phase=2;t.pause=18;break;}
  const goal=TESSA_ROUTE[next],d=Math.hypot(goal.x-t.x,goal.z-t.z),move=Math.min(d,budget);if(d>1e-7){t.x+=(goal.x-t.x)*move/d;t.z+=(goal.z-t.z)*move/d;}budget-=move;
  if(d<=move+1e-7){t.segment=next;t.offset=0;}else{t.offset=1;break;}
 }
}
export function queueWorkshopCollection(s,a){
 if(a.type==='stock')return;const l=s.workshop.life??=newWorkshopLife();
 // Bound the visual queue; actual wages and task completion never depend on ambient customers.
 if(l.parked.length<3){const id=++l.serial;l.parked.push({id,bike:a.variant%3,ready:now(s)+8,owner:id%6});}
}
export function validateWorkshopLife(w){
 w.life??=null;if(!w.life)return;const l=w.life,integer=(n,max=1e9)=>Number.isInteger(n)&&n>=0&&n<=max,point=p=>Number.isFinite(p.x)&&Number.isFinite(p.z)&&p.x>=330&&p.x<=348&&p.z>=72&&p.z<=90;
 if(!integer(l.serial)||!Number.isFinite(l.nextArrival)||!Array.isArray(l.people)||l.people.length>2||!Array.isArray(l.parked)||l.parked.length>3)throw Error('Ungültiger Werkstattbetrieb.');
 if(new Set(l.people.map(p=>p.id)).size!==l.people.length||new Set(l.parked.map(p=>p.id)).size!==l.parked.length)throw Error('Doppelte Werkstattzuordnung.');
 for(const p of l.people)if(p.id>l.serial||p.kind==='pickup'&&!integer(p.job)||!integer(p.id)||!integer(p.owner,5)||!integer(p.bike,2)||!['dropoff','pickup'].includes(p.kind)||!['entering','waiting','leaving'].includes(p.phase)||!integer(p.node,CUSTOMER_ROUTE.length)||!point(p)||!Number.isFinite(p.wait)||!Number.isFinite(p.angle)||typeof p.withBike!=='boolean')throw Error('Ungültiger Werkstattkunde.');
 for(const p of l.parked)if(p.id>l.serial||!integer(p.id)||!integer(p.owner,5)||!integer(p.bike,2)||!Number.isFinite(p.ready))throw Error('Ungültiger Reparaturbestand.');
 const t=l.tessa;if(!t||!point(t)||!integer(t.segment,TESSA_ROUTE.length-1)||!integer(t.phase,2)||!integer(t.offset,1)||t.pause!=null&&(!Number.isFinite(t.pause)||t.pause<0||t.pause>18))throw Error('Ungültiger Mitarbeiterweg.');
}
