import {ITEMS,VEHICLES,euro} from './data.js?v=0.7.3-map1';
import {inventoryWeight} from './inventory.js?v=0.7.3-map1';
import {itemIcon} from './icons.js?v=0.7.3-map1';
import {allVehicles,ownedVehicleCount,usedOffers,trunkLimits,insurancePremium,repairPrice,resaleValue,nearestParkingSpot,parkingSpotOccupied,MAX_OWNED_VEHICLES} from './mobility.js?v=0.7.5-fix1';
import {transitOptions,transitStop,displayMinutes,clockText} from './transit.js?v=0.7.5-fix1';

const km=v=>(v.mileage||0).toFixed(1).replace('.',',')+' km';
const atGarage=s=>!s.inside&&!s.riding&&Math.hypot(s.position.x-76,s.position.z-12)<=4.5;
const nearActive=s=>!!s.vehicle&&!s.inside&&!s.riding&&Math.hypot(s.position.x-s.vehicle.x,s.position.z-s.vehicle.z)<=4.2;
const vehicleLine=v=>Math.round(v.condition)+' % Zustand · '+km(v)+' · '+(v.id==='bike'?'ohne Kraftstoff':Math.round(v.fuel)+' % Tank')+' · '+(v.insured?'versichert':'nicht versichert');

function vehicleCard(v,s,button,active=false){
 const def=VEHICLES[v.id],garage=atGarage(s),repair=repairPrice(v),premium=insurancePremium(v);
 let html='<article class="card"><span class="tag '+(active?'gold':'')+'">'+(active?'AKTIV':'GARAGE')+' · '+(v.used?'GEBRAUCHT':'GEKAUFT')+'</span><h3>'+def.name+'</h3><p>'+vehicleLine(v)+'</p><div class="price">'+euro(resaleValue(v))+'<small>aktueller Verkaufswert · Kaufpreis '+euro(v.purchasePrice)+'</small></div><div class="button-row">';
 html+=active?button('Fahrzeug markieren','trackVehicle','','secondary'):button('Als aktives Fahrzeug holen','activateVehicle',v.uid,'secondary',!garage);
 html+=button(v.id==='bike'?'Gepäckträger':'Stauraum','trunk','','secondary',!active||!nearActive(s))+'</div>';
 if(garage){
  html+='<div class="button-row">'+button('Volltanken · '+euro(Math.ceil((100-v.fuel)*30)),'vehicleService','fuel|'+v.uid,'secondary',v.id==='bike'||v.fuel>=100)+button('Reparieren · '+euro(repair),'vehicleService','repair|'+v.uid,'secondary',v.condition>=100)+'</div><div class="button-row">';
  html+=v.id==='bike'?'<span class="tag">KEINE VERSICHERUNG NÖTIG</span>':button(v.insured?'Versicherung beenden':'Versichern · '+euro(premium)+'/Tag','vehicleService','insurance|'+v.uid,'secondary');
  html+=button('Verkaufen · '+euro(resaleValue(v)),'confirmSellVehicle',v.uid,'secondary')+'</div>';
 }
 return html+'</article>';
}
export function garagePanel(s,button){
 const owned=allVehicles(s),garage=atGarage(s),offers=usedOffers(s),full=ownedVehicleCount(s)>=MAX_OWNED_VEHICLES;
 let ownedHtml=owned.length?'<div class="cards">'+owned.map(v=>vehicleCard(v,s,button,v===s.vehicle)).join('')+'</div>':'<div class="info-box">Noch kein eigenes Fahrzeug. Das Stadtrad ist der günstigste Einstieg: kein Sprit, keine Versicherungsprämie und ein kleiner Gepäckträger.</div>';
 let newCars='<h3 class="section-title">Neufahrzeuge</h3><div class="cards">'+Object.entries(VEHICLES).map(([id,v])=>'<article class="card"><span class="card-icon">'+(id==='bike'?'◉':'▰')+'</span><h3>'+v.name+'</h3><p>'+v.description+'</p><div class="price">'+euro(v.cost)+'</div>'+button(full?'Garage voll':'Neu kaufen','buyVehicle',id,'primary',full||s.money<v.cost||!garage)+'</article>').join('')+'</div>';
 let used='<h3 class="section-title">Gebrauchtmarkt · heute</h3><p class="footnote">Die Angebote wechseln jeden Spieltag. Weniger Kaufpreis bedeutet meist mehr Kilometer und schlechteren Zustand. Reparaturen und Versicherung kommen extra.</p><div class="cards">'+(offers.length?offers.map(o=>'<article class="card"><span class="tag">GEBRAUCHT · '+o.condition+' %</span><h3>'+o.name+'</h3><p>'+o.mileage.toFixed(1).replace('.',',')+' km · Tank '+o.fuel+' % · sofort verfügbar</p><div class="price">'+euro(o.price)+'<small>statt neu '+euro(VEHICLES[o.id].cost)+'</small></div>'+button('Gebraucht kaufen','buyUsedVehicle',o.offerId,'primary',full||s.money<o.price||!garage)+'</article>').join(''):'<p class="lead">Die heutigen Angebote wurden bereits gekauft.</p>')+'</div>';
 return '<p class="lead">Bis zu fünf eigene Fahrzeuge. Das aktive Fahrzeug steht in der Stadt; weitere Fahrzeuge lagern auf den markierten Mobilwerk-Stellplätzen. Wechsel, Service, Versicherung und Fahrzeugkauf sind nur direkt beim Mobilwerk möglich.</p><div class="stats-grid"><div class="stat"><span>Eigene Fahrzeuge</span><strong>'+owned.length+' / '+MAX_OWNED_VEHICLES+'</strong></div><div class="stat"><span>Versicherung / Tag</span><strong>'+euro(owned.reduce((n,v)=>n+(v.insured?insurancePremium(v):0),0))+'</strong></div><div class="stat"><span>Mobilwerk</span><strong>'+(garage?'VOR ORT':'NICHT VOR ORT')+'</strong></div></div><h3 class="section-title">Deine Garage</h3>'+ownedHtml+newCars+used+'<div class="funds">Dein Bargeld <b>'+euro(s.money)+'</b></div>';
}
export function vehiclesPanel(s,button){
 const owned=allVehicles(s),v=s.vehicle,near=nearActive(s),spot=v?nearestParkingSpot(v,7):null;
 if(!owned.length)return '<p class="lead">Du besitzt noch kein Fahrzeug. Beim Mobilwerk beginnt Mobilität mit dem Stadtrad. Es braucht keinen Kraftstoff, keine Versicherung und bleibt für kurze Wege sowie Kurierfahrten günstig.</p>'+button('Mobilwerk markieren','navigate','garage');
 let html='<div class="stats-grid"><div class="stat"><span>Eigene Fahrzeuge</span><strong>'+owned.length+'</strong></div><div class="stat"><span>Aktiv</span><strong>'+(v?VEHICLES[v.id].name:'—')+'</strong></div><div class="stat"><span>ÖPNV-Fahrten</span><strong>'+s.mobility.transitTrips+'</strong></div></div>';
 if(v){html+='<div class="info-box"><b>'+VEHICLES[v.id].name+'</b><br>'+vehicleLine(v)+'<br>'+(v.parkingSpot?'Geparkt: '+(nearestParkingSpot(v,.5)?.name||v.parkingSpot):'Frei abgestellt in Lindenstadt')+'.</div><div class="button-row">'+button('Fahrzeug finden','trackVehicle')+button(trunkLimits(v).name+' öffnen','trunk','','secondary',!near)+(spot?button(parkingSpotOccupied(s,spot.id,v.uid)?'Stellplatz belegt':'Hier sauber einparken','parkVehicle',spot.id,'secondary',!near||parkingSpotOccupied(s,spot.id,v.uid)):'')+'</div>';}
 html+='<h3 class="section-title">Alle Fahrzeuge</h3><div class="rows">'+owned.map(v=>'<div class="row"><div><strong>'+VEHICLES[v.id].name+'</strong><small>'+(v===s.vehicle?'Aktiv in der Stadt':'Im Mobilwerk')+' · '+vehicleLine(v)+'</small></div><span class="tag">'+(v.used?'GEBRAUCHT':'EIGEN')+'</span></div>').join('')+'</div>'+button('Garage & Gebrauchtmarkt','navigate','garage','secondary');
 return html;
}
export function trunkPanel(s,button){
 const v=s.vehicle;if(!v)return '<p class="lead">Kein aktives Fahrzeug.</p>';
 const near=nearActive(s),lim=trunkLimits(v);
 const list=(slots,direction)=>slots.length?slots.map((item,i)=>'<div class="row"><div><strong>'+itemIcon(item.id)+' '+ITEMS[item.id].name+' ×'+item.count+'</strong><small>'+(ITEMS[item.id].weight*item.count).toFixed(1)+' kg</small></div><div>'+button('1','trunkTransfer',direction+'|'+i+'|1','secondary',item.id==='parcel')+button('Alle','trunkTransfer',direction+'|'+i+'|'+item.count,'secondary',item.id==='parcel')+'</div></div>').join(''):'<p class="lead">Leer.</p>';
 return '<p class="lead">'+lim.name+' des '+VEHICLES[v.id].name+'. Auftrags-Pakete bleiben im bestehenden Kurier-System und können hier nicht manuell verschoben werden.</p>'+(!near?'<div class="info-box">Gehe zum abgestellten Fahrzeug und öffne den Stauraum erneut.</div>':'')+'<div class="storage-layout"><section><h3 class="section-title">Rucksack <small>'+inventoryWeight(s.inventory).toFixed(1)+' / 20 kg</small></h3><div class="rows">'+list(s.inventory,'deposit')+'</div></section><section><h3 class="section-title">'+lim.name+' <small>'+inventoryWeight(v.trunk).toFixed(1)+' / '+lim.weight+' kg · '+v.trunk.length+' / '+lim.slots+' Plätze</small></h3><div class="rows">'+list(v.trunk,'withdraw')+'</div></section></div>';
}
const time=clockText;
export function transitPanel(s,stopId,button){
 const stop=transitStop(stopId),options=transitOptions(s,stopId);if(!stop)return '<p class="lead">Keine Haltestelle.</p>';
 let cards=options.map(o=>'<article class="card"><span class="tag">'+(o.line.kind==='rail'?'BAHN':'BUS')+'</span><h3>'+o.line.name+' → '+o.stop.name+'</h3><p>Abfahrt '+time(o.depart)+' · noch '+displayMinutes(o.wait)+' Spielminuten warten · '+o.ride+' Spielminuten Fahrt.</p><div class="price">'+euro(o.fare)+'<small>Gesamtzeit '+displayMinutes(o.total)+' Spielminuten</small></div>'+button('Ticket kaufen & fahren','travelTransit',o.line.id+'|'+stopId,'primary',s.money<o.fare||s.riding||s.inside)+'</article>').join('');
 let last=s.mobility.lastTransit?'<div class="info-box">Letzte Fahrt: '+s.mobility.lastTransit.line.toUpperCase()+' · Tag '+s.mobility.lastTransit.day+' · '+time(s.mobility.lastTransit.minute)+' · '+euro(s.mobility.lastTransit.cost)+'</div>':'';
 return '<p class="lead">Fahrplan in echter Spielzeit. Warten und Fahrt lassen Hunger, Durst, Miettage, Cafébetrieb und Expressfristen weiterlaufen. Menüs selbst pausieren weiterhin.</p><div class="cards">'+cards+'</div>'+last+'<div class="funds">Dein Bargeld <b>'+euro(s.money)+'</b></div>';
}
