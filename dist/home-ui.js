import {HOMES,ITEMS,HOME_POINTS,euro} from './data.js?v=0.8.1';
import {SLEEP_HOURS,timePreview,actionOffer,fatigue,FRIDGE_SLOTS,FRIDGE_WEIGHT,fridgeItem} from './daily-life.js?v=0.8.1';
import {inventoryWeight} from './inventory.js?v=0.8.1';
import {heldLoad} from './player-movement.js?v=0.8.1';
import {cafeHours} from './game-time.js?v=0.8.1';
export function timeCard(s,minutes){const p=timePreview(s,minutes);return `<div class="time-preview"><div><span>JETZT · TAG ${p.start.day}</span><strong>${p.start.time}</strong></div><span class="time-arrow" aria-hidden="true">→</span><div><span>DANACH · TAG ${p.end.day}</span><strong>${p.end.time}</strong></div></div><p class="footnote">${minutes} Spielminuten als sichtbarer Zeitsprung. Die ganze Stadt läuft mit: Bedürfnisse, Lieferfristen und laufender Cafébetrieb. Menüs pausieren.</p>${p.warnings.map(t=>`<p class="routine-warning">${t}</p>`).join('')}`;}
export function sleepPanel(s,hours,button){
 const home=HOMES.find(h=>h.id===s.home),minutes=hours*60;
 return `<p class="lead">${home?.name||'Dein Zuhause'} · Plane deine Nachtruhe. Ein kurzer Schlaf hilft, ersetzt aber keine volle Nacht.</p><div class="sleep-options" role="group" aria-label="Schlafdauer">${SLEEP_HOURS.map(h=>button(h+' Stunden','sleepHours',h,h===hours?'secondary active':'secondary')).join('')}</div>${timeCard(s,minutes)}<div class="rest-facts"><div><span>Energie jetzt</span><strong>${Math.round(s.needs.energy)} / 100</strong></div><div><span>Erholung</span><strong>${fatigue(s)>.5?'Übermüdet':fatigue(s)>0?'Müde':'Ausgeglichen'}</strong></div><div><span>Schlafplatz</span><strong>${home?.energy<100?'Einfaches Bett':'Bequemes Bett'}</strong></div></div><p class="lead">Schlaf baut Müdigkeit ab und füllt Energie allmählich auf. Mit wenig Essen oder Flüssigkeit erholst du dich schlechter. Ein besseres Bett hilft; Werte bleiben auf 100 begrenzt.</p><div class="button-row">${button(hours+' Stunden schlafen','beginDaily','sleep|'+hours,'primary',!actionOffer(s,'sleep',hours))}${button('Zurück','close','','secondary')}</div>`;
}
export function dailyActionPanel(s,kind,arg,button){
 const offer=actionOffer(s,kind,arg);if(!offer)return '<p class="lead">Diese Aktion ist gerade nicht möglich. Geh zum passenden Ort und lege getragene Gegenstände ab. Prüfe bei kostenpflichtigen Aktionen dein Bargeld.</p>'+button('Zurück ins Spiel','close');
 return `<p class="lead">${{shower:'Eine kurze Dusche macht dich frisch und senkt Stress.',wash:'Der Waschraum der Anlaufstelle ist für dich bereit.',shelterSleep:'Ein sicheres Bett für acht Stunden. Essen und Trinken vorher nicht vergessen.',rest:'Zwei Stunden Ruhe helfen gegen Erschöpfung. Eine Parkpause ersetzt keine Nachtruhe.',cook:'Zutaten aus Rucksack, Kühlschrank und Wohnungslager werden gemeinsam verwendet.'}[kind]||''}</p>${timeCard(s,offer.minutes)}${offer.cost?`<div class="funds">Kosten bei Beginn <b>${euro(offer.cost)}</b></div>`:''}<div class="button-row">${button(offer.label+' beginnen','beginDaily',kind+'|'+(arg||''))}${button('Zurück','close','','secondary')}</div>`;
}
export function fridgePanel(s,button,icon){
 const list=(slots,direction)=>slots.map((v,i)=>({v,i})).filter(({v})=>fridgeItem(v.id)).map(({v,i})=>`<div class="fridge-row"><span class="fridge-icon">${icon(v.id)}</span><div><strong>${ITEMS[v.id].name}</strong><small>${v.count} Stück · ${(v.count*ITEMS[v.id].weight).toFixed(1)} kg</small></div>${button(direction==='deposit'?'Einlagern':'Mitnehmen','fridgeTransfer',direction+'|'+i,'secondary')}</div>`).join('')||'<p class="footnote">Keine Lebensmittel vorhanden.</p>';
 return `<p class="lead">Hier bleiben Essen, Getränke und Zutaten für später. Deine Küche greift automatisch darauf zu. Der Kühlschrank hat ${FRIDGE_SLOTS} Plätze und fasst ${FRIDGE_WEIGHT} kg.</p><div class="fridge-summary"><strong>${s.fridge.length} / ${FRIDGE_SLOTS} Plätze</strong><span>${inventoryWeight(s.fridge).toFixed(1)} / ${FRIDGE_WEIGHT} kg</span></div><div class="fridge-columns"><section><h3>Im Kühlschrank</h3>${list(s.fridge,'withdraw')}</section><section><h3>Im Rucksack</h3>${list(s.inventory,'deposit')}</section></div>`;
}
export function dailyHints(s){
 const hints=[],a=s.dailyLife?.action;
 if(a)return [];
 const load=heldLoad(s);if(load)hints.push({tone:'neutral',text:({parcel:'Paket in den Händen',crate:'Kiste in den Händen',tray:'Tablett in den Händen'}[load])+' · Gehen, kein Sprinten'});
 const f=fatigue(s);if(f>=.8)hints.push({tone:'warning',text:'Übermüdet · Sprint gesperrt. Schlaf hilft; Kaffee ersetzt ihn nicht.'});else if(f>.2)hints.push({tone:'warning',text:'Müde · Sprinten kostet mehr Energie. Plane deine Erholung.'});
 if(s.needs.thirst<20)hints.push({tone:'warning',text:'Durstig · Trinken vor längeren Aktionen'});else if(s.needs.hunger<20)hints.push({tone:'warning',text:'Hungrig · Essen vor der nächsten Schicht'});
 const nearCafe=!s.inside&&Math.hypot(s.position.x-63,s.position.z+12)<18;
 if(s.businesses.cafe&&nearCafe){const h=cafeHours(s);hints.push({tone:'neutral',text:h.state+' · '+h.detail});}
 if(!s.inside&&Math.hypot(s.position.x+36,s.position.z+12)<15)hints.push({tone:'neutral',text:'MARKT 24 · Rund um die Uhr geöffnet'});
 return hints.slice(0,3);
}
export function activityHud(s){const a=s.dailyLife?.action;if(!a)return '';const remaining=Math.max(0,a.minutes-a.applied),p=timePreview(s,remaining),sleep=a.kind==='sleep'||a.kind==='shelterSleep';
 return `<div class="routine-running"><div><span class="eyebrow">${a.minutes?'ZEITSPRUNG':'ALLTAG'}</span><strong>${a.interrupted?'Aktion beenden …':sleep?'Zur Ruhe kommen':a.label}</strong></div><progress max="1" value="${a.elapsed/a.duration}" aria-label="Fortschritt der Alltagsaktion"></progress><span class="routine-time">${a.minutes&&!a.interrupted?'Bis '+p.end.time+' · Tag '+p.end.day:'Einen Moment …'}</span>${!a.interrupted?'<button class="secondary" data-action="cancelDaily">'+(sleep?'Früher aufstehen':'Abbrechen')+'</button>':''}</div>`;
}
