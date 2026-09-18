export function newCafeService(partial=false,since=0){return {partial,since,served:0,totalWait:0,maxWait:0,satisfied:0,playerServed:0,staffServed:0,timedOut:0,closedUnserved:0,preparedByStaff:0,ordersByStaff:0,clearedByStaff:0,payments:{player:0,staff:0,table:0,closing:0}}}
export function ensureCafeService(c){return c.service??=newCafeService(c.elapsed>0||c.served>0||c.lost>0,c.elapsed)}
export function recordCafeService(c,g,byPlayer){const m=ensureCafeService(c);m.served++;m.totalWait+=g.wait;m.maxWait=Math.max(m.maxWait,g.wait);if(g.wait<=g.patience*.5)m.satisfied++;m[byPlayer?'playerServed':'staffServed']++}
export function cafeServiceSummary(c){
 const m=ensureCafeService(c),completed=m.served+m.timedOut+m.closedUnserved;
 return {...structuredClone(m),averageWait:m.served?m.totalWait/m.served:null,satisfaction:m.served?Math.round(m.satisfied/m.served*100):null,completion:completed?Math.round(m.served/completed*100):null};
}
export function validateCafeService(m){
 const number=v=>Number.isFinite(v)&&v>=0&&v<=1e9;
 if(!m||typeof m.partial!=='boolean'||!number(m.since)||m.since>481||!number(m.totalWait)||!number(m.maxWait)||!['served','satisfied','playerServed','staffServed','timedOut','closedUnserved','preparedByStaff','ordersByStaff','clearedByStaff'].every(k=>Number.isInteger(m[k])&&number(m[k]))||m.satisfied>m.served||m.playerServed+m.staffServed!==m.served||!m.payments||!['player','staff','table','closing'].every(k=>Number.isInteger(m.payments[k])&&number(m.payments[k])))throw Error('Ungültige Café-Servicemessung.');
 if(m.served===0&&(m.totalWait!==0||m.maxWait!==0)||m.maxWait>m.totalWait+.001)throw Error('Ungültige Café-Wartezeiten.');
 return m;
}
