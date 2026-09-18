export const WORKSHOP_ROOM={minX:330.25,maxX:347.75,minZ:72.25,maxZ:89.75,ceiling:4.15,spawn:{x:340,z:88},outside:{x:-174,z:43},angle:0};
export const WORKSHOP_POINTS={
 workshopExit:{x:340,z:88.8,name:'Werkstatt verlassen'},
 workshopDesk:{x:333.6,z:85.5,name:'Tessa · Aufträge & Weiterbildung',angle:0},
 workshopIntake:{x:333.5,z:78.3,name:'Wareneingang prüfen',angle:Math.PI},
 workshopStock:{x:344.9,z:77,name:'Ersatzteile & Werkzeug',angle:Math.PI/2},
 workshopBench:{x:339,z:75.6,name:'Montagebank',angle:Math.PI},
 workshopBike:{x:343.5,z:81.75,name:'Fahrrad am Montageständer',angle:Math.PI},
 workshopTest:{x:336,z:82.2,name:'Prüfplatz',angle:Math.PI}
};
export const WORKSHOP_FIXTURES=[
 {id:'desk',x:333.6,z:86.65,w:1.8,d:.6,h:1.08},
 {id:'intake',x:333.5,z:77.15,w:1.8,d:.6,h:1.1},
 {id:'stock',x:346,z:77,w:.55,d:3.25,h:2.7},
 {id:'bench',x:339,z:74.5,w:2.2,d:.65,h:1.45},
 {id:'bike',x:343.5,z:80.8,w:1.3,d:.42,h:1.7},
 {id:'test',x:336,z:81.1,w:.85,d:.55,h:1.1},
 {id:'parked',x:346,z:85.8,w:1.2,d:1.6,h:1.55},
 {id:'cabinet',x:331,z:81.7,w:.55,d:1.3,h:2.4}
];
export function workshopClear(x,z,r=.35){const b=WORKSHOP_ROOM;return x>b.minX+r&&x<b.maxX-r&&z>b.minZ+r&&z<b.maxZ-r&&!WORKSHOP_FIXTURES.some(f=>Math.abs(x-f.x)<f.w+r&&Math.abs(z-f.z)<f.d+r);}
export function atWorkshop(s,point){const p=WORKSHOP_POINTS[point];if(!p||s.interior!=='workshop'||!s.inside||s.riding||Math.hypot(s.position.x-p.x,s.position.z-p.z)>1.75)return false;for(let i=0;i<=12;i++){const t=i/12;if(!workshopClear(s.position.x+(p.x-s.position.x)*t,s.position.z+(p.z-s.position.z)*t))return false;}return true;}

export function workshopWorkPoint(a,current){const p=WORKSHOP_POINTS[current.point];if(current.point!=='workshopBike')return p;const rear=a.revision===2?a.variant%2===1:!!a.variant;return {...p,x:p.x+(rear?-.62:1.05)};}
// Prefer a direct clear approach; use the interaction point only when furniture blocks it.
export function workshopApproach(a,current,origin,seconds=Infinity){
 const end=workshopWorkPoint(a,current),samples=Math.max(1,Math.ceil(Math.hypot(end.x-origin.x,end.z-origin.z)/.1));let direct=true;for(let i=0;i<=samples;i++){const u=i/samples;if(!workshopClear(origin.x+(end.x-origin.x)*u,origin.z+(end.z-origin.z)*u)){direct=false;break;}}const pivot=current.point==='workshopBike'&&!direct?WORKSHOP_POINTS[current.point]:end;
 const first=Math.hypot(pivot.x-origin.x,pivot.z-origin.z),last=Math.hypot(end.x-pivot.x,end.z-pivot.z),length=first+last,duration=length/1.65,travel=Math.min(length,Math.max(0,seconds)*1.65);
 const from=travel<first?origin:pivot,to=travel<first?pivot:end,distance=travel<first?first:last,t=distance?Math.min(1,(travel<first?travel:travel-first)/distance):1;
 return {x:from.x+(to.x-from.x)*t,z:from.z+(to.z-from.z)*t,angle:travel>=length?end.angle:Math.atan2(to.x-from.x,to.z-from.z),duration,length};
}
