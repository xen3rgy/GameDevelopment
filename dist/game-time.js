// One simulation minute per real second: 24 real minutes per day at 1x.
export const CAFE_OPEN=8*60,CAFE_CLOSE=20*60,CAFE_SHIFT=480;
export const simulationMinutes=(seconds,speed=1)=>Math.max(0,Number.isFinite(seconds)?seconds:0)*([1,4,10].includes(speed)?speed:1);
export const clockLabel=minute=>`${String(Math.floor(minute/60)%24).padStart(2,'0')}:${String(Math.floor(minute%60)).padStart(2,'0')}`;
export function cafeHours(s){
 const c=s.cafe,b=s.businesses.cafe;
 if(c?.phase==='open')return {canOpen:false,state:c.employees.some(e=>e.status==='entering')?'Team kommt an':c.elapsed>=c.duration-60?'Letzte Bestellungen beendet':'Café geöffnet',detail:'Schichtende '+clockLabel(c.closeMinute??Math.min(CAFE_CLOSE,s.minute+CAFE_SHIFT-c.elapsed))+' · Ladenschluss 20:00',tone:'open'};
 if(c?.employees?.some(e=>e.status==='leaving'))return {canOpen:false,state:'Feierabend',detail:'Das Team verlässt das Café.',tone:'closed'};
 if(s.minute<CAFE_OPEN||s.minute>=CAFE_CLOSE)return {canOpen:false,state:'Geschlossen',detail:'Öffnungszeiten 08:00–20:00 · '+(s.minute<CAFE_OPEN?'Heute':'Morgen')+' ab 08:00 öffnen',tone:'closed'};
 if(c?.day===s.day)return {canOpen:false,state:'Eigene Schicht beendet',detail:'Nächster Betriebstag morgen ab 08:00',tone:'closed'};
 if(s.minute>CAFE_CLOSE-120)return {canOpen:false,state:'Heute keine neue Schicht',detail:'Mindestens 2 Stunden Betrieb · morgen ab 08:00 öffnen',tone:'closed'};
 if(!b?.open||b.stock<1)return {canOpen:false,state:'Noch nicht bereit',detail:!b?.open?'Betrieb in der Verwaltung freigeben.':'Mindestens einen Vorratstag bestellen.',tone:'closed'};
 return {canOpen:true,state:'Bereit zum Öffnen',detail:'08:00–20:00 · Start bis 18:00 · bis zu 8 Stunden',tone:'ready'};
}
