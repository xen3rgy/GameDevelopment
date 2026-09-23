import {euro,VEHICLES} from './data.js?v=0.8.1';
import {serviceBay} from './service-layout.js?v=0.8.1';
import {serviceQuote,vehicleAtBay,serviceBusy,fuelPrice} from './vehicle-service.js?v=0.8.1';
import {inReach} from './interactions.js?v=0.8.1';
export function servicePanel(s,id,button){
 const bay=serviceBay(id);if(!bay)return '';
 const v=vehicleAtBay(s,bay),q=serviceQuote(s,v,bay.kind);
 return `<p class="lead">${bay.kind==='fuel'?'Super E5 · '+euro(fuelPrice(s))+'/Liter · Tagespreis':'Wartung & Unfallschäden · Preis nach Fahrzeugklasse und Zustand'}</p><div class="info-box">Fahrzeug mittig und längs in der markierten Bucht abstellen, aussteigen und am Terminal bedienen. Laufende Arbeitsaufträge zuerst abschließen.</div>${v?`<article class="card"><h3>${VEHICLES[v.id].name}</h3><p>Tank ${Math.round(v.fuel)} % · Zustand ${Math.round(v.condition)} %</p>${q?`<p>${q.litres!=null?q.litres.toFixed(1)+' Liter · ':''}${q.duration} Sekunden<br>Brutto ${euro(q.gross)} · Versicherungsgutschrift ${euro(q.credit)}</p><div class="price">${euro(q.cost)}</div>${button(bay.kind==='fuel'?'Volltanken':'Reparieren','beginService',id,'primary',serviceBusy(s)||!inReach(s.position,bay)||s.money<q.cost)}`:'<p>Kein Service nötig. Fahrräder benötigen keinen Kraftstoff.</p>'}</article>`:'<p>Kein eindeutig zugeordnetes, stillstehendes Fahrzeug erkannt.</p>'}<p class="footnote">Preis wird beim Start festgeschrieben und reserviert. Beim Tankabbruch zahlst du nur die abgegebene Menge. Reparatur wird erst bei Abschluss berechnet; Abbruch erstattet den gesamten Betrag. Speichern erhält den Fortschritt.</p>`;
}
