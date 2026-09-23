import {STATION_BUILDINGS} from './city-layout.js?v=0.8.1';
export const ITEMS={
 bottle:{name:'Pfandflasche',icon:'🍾',category:'Wertstoffe',weight:.1,stack:20,price:25,description:'Eine leere Mehrwegflasche. Am Pfandautomaten bekommst du 0,25 € zurück.'},
 water:{name:'Mineralwasser',icon:'💧',category:'Getränke',weight:.5,stack:6,price:100,effects:{thirst:45},description:'Stilles Wasser. Füllt deinen Durstvorrat um 45 Punkte auf. Die leere Flasche bleibt im Rucksack.'},
 sandwich:{name:'Käsesandwich',icon:'🥪',category:'Nahrung',weight:.25,stack:6,price:250,effects:{hunger:38},description:'Frisch belegt. Gibt dir 38 Punkte Sättigung.'},
 coffee:{name:'Kaffee',icon:'☕',category:'Getränke',weight:.2,stack:4,price:180,effects:{energy:22,thirst:8,stress:-5},description:'22 Energie, 8 Durstvorrat und etwas weniger Stress.'},
 meal:{name:'Warme Mahlzeit',icon:'🍲',category:'Nahrung',weight:.4,stack:4,price:600,effects:{hunger:70,health:8},description:'Eine richtige Mahlzeit: 70 Sättigung und 8 Gesundheit.'},
 medicine:{name:'Erste-Hilfe-Set',icon:'🩹',category:'Medizin',weight:.3,stack:3,price:1200,effects:{health:45},description:'Versorgt kleinere Verletzungen. Stellt 45 Gesundheit wieder her.'},
 parcel:{name:'Lieferpaket',icon:'📦',category:'Aufträge',weight:2,stack:1,price:0,description:'Eine versiegelte Sendung für deinen laufenden Lieferauftrag.'}
};
export const LOCATIONS=[
 {id:'recycle',name:'Pfandrückgabe',type:'recycle',x:-23,z:-12,icon:'♻',color:'#76d6a2',description:'Flaschen abgeben. Aus Kleingeld wird ein Anfang.'},
 {id:'market',name:'MARKT 24',type:'shop',x:-36,z:-12,icon:'▣',color:'#91bd91',description:'Lebensmittel, Getränke und das Nötigste. Rund um die Uhr.'},
 {id:'shelter',name:'Anlaufstelle Nord',type:'shelter',x:-68,z:-12,icon:'☾',color:'#93bde2',description:'Ein Bett für die Nacht, Wasser und eine zweite Chance.'},
 {id:'jobs',name:'Kiez & Kurier',type:'jobs',x:26,z:-12,icon:'↗',color:'#edc285',description:'Echte Wege. Ehrliche Arbeit. Dein erster Lohn.'},
 {id:'cafe',name:'Café Morgen',type:'business',business:'cafe',x:63,z:-12,icon:'☕',color:'#dba674',description:'Ein kleines Café mit großer Zukunft. Vielleicht deiner.'},
 {id:'home',name:'Lindenhöfe',type:'housing',x:-34,z:12,icon:'⌂',color:'#b9c7df',description:'Vom eigenen Zimmer zum Penthouse.'},
 {id:'school',name:'Campus West',type:'school',x:-72,z:12,icon:'◇',color:'#c1a9dd',description:'Investiere in das, was dir niemand nehmen kann.'},
 {id:'garage',name:'Mobilwerk',type:'garage',x:76,z:12,icon:'◉',color:'#b6c7d3',description:'Dein erster fahrbarer Untersatz. Kaufen, tanken und instand halten.'},
 {id:'bank',name:'Stadtbank',type:'bank',x:31,z:12,icon:'▤',color:'#dfc888',description:'Rücklagen, Kredite und Immobilien.'},
 {id:'depot',name:'Westhafen Logistik',type:'depot',x:-70,z:-76,icon:'▥',color:'#cbb895',description:'Pakete scannen, zum richtigen Regal tragen, Lohn verdienen.'},
 {id:'agency',name:'Studio Nord',type:'business',business:'agency',x:65,z:-76,icon:'◈',color:'#abbde6',description:'Deine eigene digitale Agentur.'},
 {id:'park',name:'Lindenpark',type:'park',x:65,z:75.7,icon:'♧',color:'#9bc8a3',description:'Durchatmen, Menschen treffen und den Kopf frei bekommen.'},
 {id:'deliveryA',name:'Buchhandlung Kapitel',type:'delivery',x:28,z:76,icon:'◫',color:'#d6bb93'},
 {id:'deliveryB',name:'Atelier am Kanal',type:'delivery',x:-68,z:76,icon:'◫',color:'#d6bb93'},
 {id:'station',name:'Bahnhof Lindenstadt West',type:'station',x:-175,z:-31,icon:'◷',color:'#e0b881',description:'Der Vorplatz am alten Viadukt. Kiosk, Werkstatt und ein neues Zuhause im Westen.'},
 {id:'stationHome',name:'Gleishöfe',type:'housing',x:-179,z:17,icon:'⌂',color:'#b9c7df',description:'Ein günstiges Zimmer im Bahnhofsviertel. Mehr Weg, weniger Miete.'},
 {id:'deliveryKiosk',name:'Kiosk am Gleis',type:'delivery',x:-179,z:-13,icon:'◫',color:'#dfb679'},
 {id:'deliveryWorkshop',name:'Werkstatt West',type:'delivery',x:-174,z:44,icon:'◫',color:'#c1aa87'},
 {id:'deliveryC',name:'Nordkontor',type:'delivery',x:29,z:-76,icon:'◫',color:'#d6bb93'},
];
export const HOMES=[{id:'stationRoom',location:'stationHome',name:'Zimmer in den Gleishöfen',price:8500,rent:750,energy:80,description:'Ein einfaches Zimmer am ruhigen Innenhof. Günstiger wohnen, weiter zur Arbeit gehen.'},{id:'room',name:'WG-Zimmer',price:12000,rent:1200,energy:80,description:'Ein Schlüssel. Ein Bett. Endlich dein eigener Rückzugsort.'},{id:'flat',name:'Stadtwohnung',price:65000,rent:3500,energy:100,description:'Mehr Raum, ein eigenes Bad und bessere Erholung.'},{id:'penthouse',name:'Penthouse',price:450000,rent:16000,energy:100,description:'Über den Dächern. Dein Aufstieg wird sichtbar.'}];
export const BUSINESSES={cafe:{name:'Café Morgen',cost:85000,baseSales:18000,baseCosts:7000,stockCost:2500,description:'Kaffee, Frühstück und Stammgäste.'},agency:{name:'Studio Nord',cost:240000,baseSales:45000,baseCosts:16000,stockCost:6000,description:'Digitale Projekte für lokale Unternehmen.'}};
export const PEOPLE=[{id:'mara',name:'Mara',role:'Kurierin',x:23,z:-9,color:0xb66b4e},{id:'emil',name:'Emil',role:'Nachbar',x:-38,z:9,color:0x778b68},{id:'leyla',name:'Leyla',role:'Gründerin',x:61,z:-9,color:0x6a7a9c}];
export const QUESTS=[
 {name:'Ein kleiner Anfang',text:'Sammle 5 Pfandflaschen.',key:'collected',goal:5,reward:500,target:'recycle'},
 {name:'Dein erstes Geld',text:'Gib 5 Flaschen am Pfandautomaten ab.',key:'returned',goal:5,reward:800,target:'recycle'},
 {name:'Erst mal durchatmen',text:'Kaufe etwas im Markt und iss oder trink es.',key:'consumed',goal:1,reward:1200,target:'market'},
 {name:'Auf eigenen Beinen',text:'Schließe deinen ersten Job ab.',key:'jobs',goal:1,reward:5000,target:'jobs'},
 {name:'Eine Nacht in Sicherheit',text:'Schlafe in der Anlaufstelle oder zu Hause.',key:'slept',goal:1,reward:3500,target:'shelter'},
 {name:'Ein Schlüssel für dich',text:'Miete dein erstes Zimmer in Lindenstadt.',key:'homes',goal:1,reward:10000,target:'home'},
 {name:'In dich investieren',text:'Schließe einen Kurs am Campus West ab.',key:'courses',goal:1,reward:18000,target:'school'},
 {name:'Dein Name an der Tür',text:'Übernimm das Café Morgen.',key:'businesses',goal:1,reward:25000,target:'cafe'},
 {name:'Gemeinsam wachsen',text:'Stelle deine erste Mitarbeiterin ein.',key:'hired',goal:1,reward:15000,target:'cafe'},
 {name:'Werte schaffen',text:'Kaufe deine erste Mietimmobilie bei der Stadtbank.',key:'properties',goal:1,reward:50000,target:'bank'},
 {name:'ZERO // RISE',text:'Erreiche 10.000 € Nettovermögen.',key:'wealth',goal:1000000,reward:0,target:'bank'}
];
export const euro=n=>(n/100).toLocaleString('de-AT',{style:'currency',currency:'EUR'});
export const clamp=(n,a=0,b=100)=>Math.max(a,Math.min(b,n));

export const VEHICLES={bike:{name:'Stadtrad',cost:12500,speed:8.5,description:'Keine Tankkosten. Schnell und unkompliziert durch den Kiez.'},car:{name:'Alter Kleinwagen',cost:90000,speed:17,description:'Dein erstes Auto. Achte auf Tank und Zustand.'},van:{name:'Lieferwagen',cost:180000,speed:15,description:'Mehr Platz für deine Arbeit. +25 % Kurierlohn, wenn du das Paket mit dem Lieferwagen transportierst.'},sport:{name:'Sportcoupé',cost:650000,speed:24,description:'Für die Zeit, in der der Weg selbst zum Ziel wird.'}};

export const HOME_POINTS={
 exit:{name:'Wohnung verlassen',x:300,z:4.6},
 bed:{name:'Schlaf planen',x:298.15,z:-2.5},
 kitchen:{name:'Kochen',x:302,z:-3},
 shower:{name:'Duschen',x:305.85,z:3.25},
 storage:{name:'Wohnungslager öffnen',x:295,z:3.3},
 fridge:{name:'Kühlschrank öffnen',x:305.8,z:-3}
};
export const WORK={
 cleaning:[[-47,-9],[-17,9],[17,9],[48,-9],[86,9],[92,-9]],
 crate:{x:-85,z:-73},
 shelves:[{x:-81,z:-73},{x:-74,z:-73},{x:-67,z:-73},{x:-60,z:-73}]
};
Object.assign(ITEMS,{
 pasta:{name:'Pasta',icon:'🍝',category:'Zutaten',weight:.5,stack:6,price:150,description:'Eine Packung Nudeln. Zusammen mit Gemüse kochst du zu Hause zwei Portionen.'},
 vegetables:{name:'Saisongemüse',icon:'🥕',category:'Zutaten',weight:.5,stack:6,price:180,description:'Frische Zutaten für deine Küche. Kann im Wohnungslager aufbewahrt werden.'},
 bread:{name:'Brot',icon:'🍞',category:'Zutaten',weight:.35,stack:6,price:120,description:'Mit Käse werden daraus zwei belegte Sandwiches.'},
 cheese:{name:'Käse',icon:'🧀',category:'Zutaten',weight:.2,stack:6,price:180,description:'Für einen kleinen Vorrat an selbstgemachten Sandwiches.'}
});
export const RECIPES={
 pasta:{name:'Gemüsepasta',icon:'🍝',ingredients:{pasta:1,vegetables:1},result:'meal',count:2,minutes:30,description:'Zwei warme Mahlzeiten. Spart Geld und macht satt.'},
 sandwich:{name:'Sandwiches vorbereiten',icon:'🥪',ingredients:{bread:1,cheese:1},result:'sandwich',count:2,minutes:10,description:'Zwei Sandwiches für den nächsten Arbeitstag.'}
};

export const BUILDINGS=[[-36,-28,24,28,15,'#7d837e','MARKT 24',1],[-70,-28,26,28,20,'#8e8171','ANLAUFSTELLE NORD',1],[27,-28,26,28,17,'#918a7b','KIEZ & KURIER',1],[65,-28,29,28,22,'#737e80','CAFÉ MORGEN',1],[-35,28,29,28,24,'#8d8a7e','LINDENHÖFE',-1],[-74,28,28,28,17,'#7e8b8e','CAMPUS WEST',-1],[33,28,31,28,27,'#999486','STADTBANK',-1],[76,29,27,29,19,'#8c7e70','MOBILWERK',-1],[-71,-91,32,26,11,'#7b8281','WESTHAFEN LOGISTIK',1],[30,-93,28,30,30,'#7d8488','NORDKONTOR',1],[68,-91,27,26,24,'#9a8e7b','STUDIO NORD',1],[-68,94,30,32,17,'#828b86','ATELIER AM KANAL',-1],[29,94,28,32,19,'#7e8686','BUCHHANDLUNG KAPITEL',-1]];

BUILDINGS.push(...STATION_BUILDINGS);

export const SHOP_POINTS={
 shopExit:{name:'Markt verlassen',x:340,z:47.7},
 produce:{name:'Frisches Gemüse auswählen',x:334.8,z:45.85,items:['vegetables']},
 bakery:{name:'Backwaren auswählen',x:337,z:33.5,items:['bread']},
 drinks:{name:'Getränke auswählen',x:335.65,z:38.5,items:['water','coffee']},
 food:{name:'Essen auswählen',x:339.65,z:38.5,items:['sandwich','meal']},
 ingredients:{name:'Zutaten auswählen',x:343.65,z:38.5,items:['pasta','vegetables','bread','cheese']},
 medicine:{name:'Hausapotheke ansehen',x:344,z:33.5,items:['medicine']},
 checkout:{name:'An der Kasse bezahlen',x:344.6,z:44.15}
};
