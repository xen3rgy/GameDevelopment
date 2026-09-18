// A bounded receipt journal, separate from cash transactions. Recording never pays money.
export const WORK_LABELS={stock:'Wareneingang',check:'Fahrradcheck',tube:'Schlauchwechsel',parts:'Teilefahrt',courier:'Kurierdienst',cleaning:'Straßenreinigung',warehouse:'Lagerschicht'};
export const newWorkLog=()=>({serial:0,entries:[],days:[]});
export function recordWork(s,{ref,type,status='paid',base=0,bonus=0,deduction=0,total=0,minutes=0,late=false}){
 const log=s.workLog??=newWorkLog();if(log.entries.some(e=>e.ref===ref))return false;
 const e={id:++log.serial,ref,type,status,base,bonus,deduction,total,minutes:Math.max(0,Math.round(minutes)),late,day:s.day,minute:Math.floor(s.minute??0)};
 log.entries.unshift(e);log.entries.length=Math.min(40,log.entries.length);
 let day=log.days.find(d=>d.day===s.day);if(!day){day={day:s.day,paid:0,bonus:0,deduction:0,settled:0,unpaid:0};log.days.unshift(day);log.days.length=Math.min(7,log.days.length);}
 day.paid+=total;day.bonus+=bonus;day.deduction+=deduction;if(status==='paid')day.settled++;else day.unpaid++;
 return true;
}
export function validateWorkLog(s){
 const l=s.workLog??=newWorkLog(),int=(v,max=1e12)=>Number.isSafeInteger(v)&&v>=0&&v<=max;
 if(!int(l.serial)||!Array.isArray(l.entries)||l.entries.length>40||!Array.isArray(l.days)||l.days.length>7)throw Error('Ungültiges Arbeitsbuch.');
 const ids=new Set(),refs=new Set(),days=new Set();
 for(const e of l.entries){if(!int(e.id)||e.id>l.serial||ids.has(e.id)||typeof e.ref!=='string'||!/^[a-z0-9:-]{1,80}$/.test(e.ref)||refs.has(e.ref)||!Object.hasOwn(WORK_LABELS,e.type)||!['paid','cancelled','expired'].includes(e.status)||!['day','minute','base','bonus','deduction','total','minutes'].every(k=>int(e[k]))||e.day>s.day||e.minute>=1440||e.total!==e.base+e.bonus-e.deduction||e.status!=='paid'&&e.total!==0||typeof e.late!=='boolean')throw Error('Ungültiger Arbeitsbeleg.');ids.add(e.id);refs.add(e.ref);}
 for(const d of l.days){if(!['day','paid','bonus','deduction','settled','unpaid'].every(k=>int(d[k]))||d.day>s.day||days.has(d.day))throw Error('Ungültige Arbeitsbuch-Tagesübersicht.');days.add(d.day);const e=l.entries.filter(e=>e.day===d.day);if(d.paid<e.reduce((v,e)=>v+e.total,0)||d.settled<e.filter(e=>e.status==='paid').length||d.unpaid<e.filter(e=>e.status!=='paid').length)throw Error('Unvollständige Arbeitsabrechnung.');}
}
