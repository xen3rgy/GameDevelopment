export const CLOTHING_SLOTS={top:'Oberteile',outer:'Jacken',pants:'Hosen',shoes:'Schuhe',backpack:'Rucksäcke'};
const piece=(id,name,slot,price,style,color)=>({id,name,slot,price,visual:{style,color}});
export const CLOTHING=[
 piece('tee','Basic T-Shirt','top',800,'tee',0x657b79),
 piece('hoodie','Hoodie','top',2400,'hoodie',0x303e4b),
 piece('sweatshirt','Sweatshirt','top',1800,'sweatshirt',0xb4b4a2),
 piece('overshirt','Overshirt','outer',2500,'overshirt',0x777b56),
 piece('jacket','Casual Jacket','outer',3800,'jacket',0x394957),
 piece('bomber','Bomber Jacket','outer',4200,'bomber',0x69534a),
 piece('jeans','Basic Jeans','pants',1500,'jeans',0x394b5b),
 piece('chinos','Chinos','pants',2200,'chinos',0xa39777),
 piece('workpants','Work Trousers','pants',2800,'work',0x55594f),
 piece('sneakers','Basic Sneakers','shoes',1200,'sneakers',0xb9b4a6),
 piece('clean-sneakers','Clean Sneakers','shoes',2700,'sneakers',0xd6d6c9),
 piece('boots','Boots','shoes',3900,'boots',0x654a36),
 piece('olive-pack','Starter Olive Backpack','backpack',2500,'starter',0x77745c),
 piece('city-pack','Black City Backpack','backpack',3500,'city',0x292f34),
 piece('grey-pack','Grey Backpack','backpack',3000,'travel',0x929795)
];
export const clothingById=id=>typeof id==='string'?CLOTHING.find(v=>v.id===id):undefined;
export function defaultWardrobe(){return {owned:['hoodie','jeans','sneakers','olive-pack'],equipped:{top:'hoodie',outer:null,pants:'jeans',shoes:'sneakers',backpack:'olive-pack'}};}
export function validateWardrobe(raw){
 const result=defaultWardrobe();
 if(Array.isArray(raw?.owned))result.owned=[...new Set([...result.owned,...raw.owned.filter(id=>clothingById(id))])];
 for(const slot of Object.keys(CLOTHING_SLOTS)){
  const id=raw?.equipped?.[slot],item=clothingById(id);
  if(item?.slot===slot&&result.owned.includes(id))result.equipped[slot]=id;
 }
 return result;
}

// Shared store anchors: the room is east of the expanded exterior (maxX 440).
export const CLOTHING_LOCATION={id:'clothingStore',name:'STADTSTOFF',type:'clothingStore',x:94,z:-12,icon:'◇',color:'#d7b783',description:'Zeitlose Kleidung. Anprobieren, vergleichen und deinen Stil finden.'};
export const CLOTHING_BUILDING=[94,-27,16,24,12,'#b7b4a6','STADTSTOFF',1];
export const CLOTHING_ROOM={minX:494.25,maxX:505.75,minZ:-5.75,maxZ:5.75,ceiling:3.55,spawn:{x:500,z:3.2},outside:{x:94,z:-12},angle:0};
export const CLOTHING_POINTS={
 clothingExit:{name:'STADTSTOFF verlassen',x:500,z:4.8},
 clothingTop:{name:'Oberteile ansehen',x:496,z:-2.6,category:'top'},
 clothingOuter:{name:'Jacken ansehen',x:496,z:.7,category:'outer'},
 clothingPants:{name:'Hosen ansehen',x:500,z:-3.8,category:'pants'},
 clothingShoes:{name:'Schuhe ansehen',x:504,z:-2.6,category:'shoes'},
 clothingBackpack:{name:'Rucksäcke ansehen',x:504,z:.7,category:'backpack'},
 clothingCounter:{name:'Gesamte Kollektion ansehen',x:502.4,z:3.1,category:'all'}
};
export const CLOTHING_FIXTURES=[
 {id:'tops',x:494.8,z:-2.6,w:.45,d:1.2,h:2.5},
 {id:'outer',x:494.8,z:.7,w:.45,d:1.2,h:2.5},
 {id:'pants',x:500,z:-5.2,w:1.6,d:.45,h:2.5},
 {id:'shoes',x:505.2,z:-2.6,w:.45,d:1.2,h:2.5},
 {id:'backpacks',x:505.2,z:.7,w:.45,d:1.2,h:2.5},
 {id:'counter',x:502.7,z:4.5,w:1.25,d:.55,h:1.2},
 {id:'fitting',x:495.2,z:4.5,w:.8,d:1,h:2.6}
];
export const atClothingDisplay=s=>s.inside&&s.interior==='clothing'&&Object.values(CLOTHING_POINTS).some(p=>p.category&&Math.hypot(s.position.x-p.x,s.position.z-p.z)<=1.9);
export function previewOutfit(equipped,id){const result={...equipped};if(id==='none')result.outer=null;else{const item=clothingById(id);if(item)result[item.slot]=item.id;}return result;}
