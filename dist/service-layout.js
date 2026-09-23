// Shared physical service geometry; widths/depths of colliders are half extents.
export const SERVICE_SITE={x:-33,z:-94.5,w:32,d:32,height:.30};
export const SERVICE_BAYS=[
 {id:'pump1',kind:'fuel',name:'HAFEN ENERGIE · Säule 1',x:-40,z:-87,vehicle:{x:-37,z:-87,angle:0}},
 {id:'pump2',kind:'fuel',name:'HAFEN ENERGIE · Säule 2',x:-25,z:-87,vehicle:{x:-22,z:-87,angle:0}},
 {id:'autoWorkshop',kind:'repair',name:'Hafenwerk · Fahrzeugservice',x:-39,z:-98,vehicle:{x:-33,z:-103,angle:0}}
];
export const serviceBay=id=>SERVICE_BAYS.find(p=>p.id===id);
export const onServiceApron=(x,z)=>Math.abs(x-SERVICE_SITE.x)<=16&&Math.abs(z-SERVICE_SITE.z)<=16;
export const SERVICE_FIXTURES=[
 ...[-41.2,-26.2].map(x=>({x,z:-87,w:.55,d:1.1,h:2.4,kind:'pump'})),
 ...[-47,-19].map(x=>({x,z:-93,w:.16,d:.16,h:5,kind:'post'})),
 {x:-33,z:-109,w:10,d:.22,h:4.5,kind:'wall'},
 ...[-43,-23].map(x=>({x,z:-104,w:.22,d:5,h:4.5,kind:'wall'})),
 {x:-39,z:-100,w:1,d:.45,h:1.1,kind:'bench'},
 ...[-41.2,-26.2].map(x=>({x,z:-87,w:.78,d:1.85,h:.14,kind:'island'})),
 ...[-41.2,-26.2].flatMap(x=>[-88.55,-85.45].map(z=>({x,z,w:.12,d:.12,h:.85,kind:'bollard'}))),
 {x:-47,z:-80,w:.65,d:.35,h:5.5,kind:'pylon'},
 {x:-39,z:-108,w:2.5,d:.65,h:2.6,kind:'storage'},
 {x:-25,z:-106,w:.7,d:1.4,h:2.6,kind:'storage'}
];
