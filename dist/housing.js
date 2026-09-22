import {HOMES,LOCATIONS,euro} from './data.js?v=0.8.0';
import {ROOMS} from './spatial.js?v=0.8.0';
import {deliveryLeg} from './delivery-routes.js?v=0.8.0';

export const homeLocation=s=>LOCATIONS.find(l=>l.id===(HOMES.find(h=>h.id===s.home)?.location||'home'));
export const outsidePosition=s=>s.interior==='home'?{x:homeLocation(s).x,z:homeLocation(s).z-2}:ROOMS[s.interior]?.outside||s.position;
export function housingPanel(s,locationId,button){
 const location=LOCATIONS.find(l=>l.id===locationId)||LOCATIONS.find(l=>l.id==='home'),owned=HOMES.find(h=>h.id===s.home),local=HOMES.filter(h=>(h.location||'home')===location.id);
 const other=location.id==='home'?'stationHome':'home';
 return `<p class="lead">${location.description} Einzugskosten enthalten die Kaution und drei Tage Miete. Du mietest immer eine Unterkunft. Bei einem Umzug wird die neue Einzugssumme fällig; die bisherige Kaution wird nicht erstattet. Vorräte ziehen mit.</p>${owned?`<div class="info-box">Dein Zuhause: <b>${owned.name}</b> · Nächste Miete: Tag ${s.rentDue}. ${homeLocation(s).id===location.id?button('Wohnung betreten ↗','enterHome','','secondary'):button('Eigenes Zuhause markieren ↗','navigateHome','','secondary')}</div>`:''}<div class="cards">${local.map(h=>{
  const cafe=deliveryLeg(location.id,'cafe')?.distance,market=deliveryLeg(location.id,'market')?.distance;
  return `<article class="card"><span class="card-icon">⌂</span><span class="tag ${s.home===h.id?'':'gold'}">${s.home===h.id?'DEIN ZUHAUSE':h.energy<100?'EINFACH WOHNEN':'MEHR KOMFORT'}</span><h3>${h.name}</h3><p>${h.description}</p><div class="price">${euro(h.price)}<small>Einzug · danach ${euro(h.rent)} / 3 Tage</small></div><p class="footnote">Fußweg zum Café: ${cafe??'—'} m · zum Markt: ${market??'—'} m.<br>${h.energy<100?'Einfaches Bett':'Bequemes Bett · schnellere Erholung'} · Küche, Dusche und Lager.</p>${button(s.home===h.id?'Bereits gemietet':owned?'Umzug ansehen ↗':'Einzug ansehen ↗','reviewRent',h.id,'primary',s.home===h.id||s.money<h.price)}</article>`;
 }).join('')}</div><div class="info-box">${location.id==='home'?'Gleishöfe: 85 € Einzug und 7,50 € Miete alle drei Tage, dafür längere Wege.':'Lindenhöfe: ab 120 € Einzug und 12 € Miete alle drei Tage, näher an Markt und Café.'} ${button('Anderen Standort markieren ↗','navigate',other,'secondary')}</div>`;
}
export function movePanel(s,id,button){
 const h=HOMES.find(h=>h.id===id);if(!h)return '';
 const old=HOMES.find(h=>h.id===s.home);
 return `<p class="lead">${old?`${old.name} verlassen und in ${h.name} ziehen?`:`In ${h.name} einziehen?`}</p><div class="stats-grid"><div class="stat"><span>Heute fällig</span><strong>${euro(h.price)}</strong></div><div class="stat"><span>Danach alle drei Tage</span><strong>${euro(h.rent)}</strong></div></div><p class="lead">Nächste Mietzahlung: Tag ${s.day+3}. ${old?'Dein bisheriger Mietvertrag endet. Keine Rückerstattung der alten Einzugskosten. Rucksack, Wohnungslager und Kühlschrank bleiben erhalten.':''}</p><div class="button-row">${button(old?'Kostenpflichtig umziehen':'Kostenpflichtig einziehen','rent',id,'primary',s.money<h.price)}${button('Abbrechen','close','','secondary')}</div>`;
}
