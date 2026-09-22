import {TRANSIT_STOPS,stopById,safeArrival} from './mobility-layout.js?v=0.8.0';
import {inReach} from './interactions.js?v=0.8.0';
export const TRANSIT_DURATION=8.5;
export function tripQuote(s,fromId,toId){
 const from=stopById(fromId),to=stopById(toId);if(!from||!to||from===to||from.mode!==to.mode)return null;
 const train=from.mode==='train',interval=train?30:20,start=360,end=1380,now=s.day*1440+s.minute,offset=train?5:TRANSIT_STOPS.filter(p=>p.mode==='bus').indexOf(from)*4;
 let departure=s.day*1440+start+offset+Math.max(0,Math.ceil((s.minute-start-offset)/interval))*interval;
 if(departure>s.day*1440+end)departure=(s.day+1)*1440+start+offset;
 const wait=Math.max(0,departure-now),ride=train?5:Math.max(3,Math.ceil(Math.hypot(from.x-to.x,from.z-to.z)/40));
 return {from:from.id,to:to.id,mode:from.mode,line:train?'S1':'B1',fare:train?240:180,departure,wait,ride,total:wait+ride,waitLabel:Math.ceil(wait),totalLabel:Math.ceil(wait+ride),interval};
}
export function canTravel(s,from){return !s.inside&&!s.riding&&!s.transit&&!s.dailyLife?.action&&!s.workshop?.active?.action&&!s.job?.interaction&&!s.job?.fieldAction&&!s.job?.carrying&&s.workshop?.active?.supply?.phase!=='carrying'&&inReach(s.position,from,'transit');}
export function bookTrip(model,fromId,toId){
 const s=model.s,q=tripQuote(s,fromId,toId);if(!q||!canTravel(s,stopById(fromId)))return false;
 if(q.wait>90){model.emit('Heute fährt hier in nächster Zeit nichts mehr. Nächste Abfahrt steht im Fahrplan.');return false;}
 if(!model.spend(q.fare)){model.emit('Nicht genug Bargeld für das Ticket.');return false;}
 s.transit={...q,elapsed:0,origin:{...s.position},bookedDay:s.day,bookedMinute:s.minute};model.emit(q.line+' · Ticket gekauft. Bitte einsteigen.');return true;
}
export function tickTransit(model,seconds){
 const s=model.s,t=s.transit;if(!t||!Number.isFinite(seconds)||seconds<=0)return false;t.elapsed=Math.min(TRANSIT_DURATION,t.elapsed+seconds);if(t.elapsed<TRANSIT_DURATION)return false;
 const to=stopById(t.to),arrival=model.findTransitArrival?model.findTransitArrival(to.arrival):{...to.arrival};
 if(!arrival){s.money+=t.fare;s.transit=null;model.emit('Ankunftsbereich blockiert. Fahrt abgebrochen, Ticket erstattet.','warning');return true;}
 s.transit=null;const original={...s.position};model.advance(t.total);
 // A collapse while advancing a late service must keep the rescue position.
 if(s.position.x===original.x&&s.position.z===original.z){s.position=arrival;s.angle=to.vehicle.angle-Math.PI;s.riding=false;s.inside=false;s.interior=null;model.emit('Angekommen: '+to.name+' · '+Math.ceil(t.total)+' Spielminuten.','success');}
 model.travelRevision=(model.travelRevision||0)+1;return true;
}
export function validateTransit(s){
 if(s.transit==null){s.transit=null;return;}
 const t=s.transit,from=stopById(t.from),to=stopById(t.to);
 if(!from||!to||from===to||from.mode!==to.mode||s.inside||s.riding||s.dailyLife?.action||s.job?.carrying||s.job?.interaction||s.workshop?.active?.action||!['elapsed','bookedDay','bookedMinute','fare','wait','ride','total','departure'].every(k=>Number.isFinite(t[k]))||t.elapsed<0||t.elapsed>TRANSIT_DURATION||!inReach(t.origin,from,'transit')||Math.hypot(s.position.x-t.origin.x,s.position.z-t.origin.z)>.01||t.bookedDay!==s.day||t.bookedMinute!==s.minute)throw Error('Ungültige ÖPNV-Fahrt.');
 const q=tripQuote(s,t.from,t.to);for(const k of ['fare','wait','ride','total','departure','mode','line'])if(t[k]!==q[k])throw Error('Ungültiges Ticket.');
}
