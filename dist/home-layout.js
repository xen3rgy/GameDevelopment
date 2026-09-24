// Home identities share gameplay anchors; furnishings stay inside the established fixture zones.
export const HOME_STYLES={
 stationRoom:{wall:0xbdb19a,floor:0x716654,wood:0x75604a,fabric:0x80745b,accent:0x7c8875,ceiling:0xc9bfa8,light:24,warm:0xffce92,bedWidth:1.3,kitchenWidth:2.35,kitchenX:302.4},
 room:{wall:0xd6d0bd,floor:0x9c896c,wood:0xad9471,fabric:0x607d82,accent:0xbd9166,ceiling:0xe2ddcb,light:34,warm:0xffdfb4,bedWidth:1.5,kitchenWidth:3,kitchenX:302.6},
 flat:{wall:0xd9ded5,floor:0xbaa58a,wood:0x9a7b5b,fabric:0x758d86,accent:0x3d625d,ceiling:0xe6e7dd,light:45,warm:0xffe3bc,bedWidth:2,kitchenWidth:3.8,kitchenX:303},
 penthouse:{wall:0xd9d2c4,floor:0xb7aca0,wood:0x57483d,fabric:0xe0d5bc,accent:0x9c8251,ceiling:0xe5dfd1,light:52,warm:0xffddb0,bedWidth:2.4,kitchenWidth:4,kitchenX:303}
};
export const homeStyle=home=>HOME_STYLES[home]||HOME_STYLES.room;
const common=[
 {id:'fridge',x:305.8,z:-4.55,w:.6,d:.6,h:2.5},
 {id:'storage',x:295,z:2.3,w:.9,d:.38,h:1.4},
 {id:'shower',x:305.8,z:2,w:.76,d:.76,h:2.85},
 {id:'nightstand',x:294.1,z:-4.8,w:.375,d:.35,h:1.6}
];
export const HOME_LAYOUTS=Object.fromEntries(Object.entries(HOME_STYLES).map(([id,s])=>[id,[
 ...common.map(f=>f.id==='storage'&&id!=='stationRoom'?{...f,h:2.8}:f),{id:'bed',x:id==='penthouse'?296.2:296.5,z:-3.25,w:s.bedWidth/2,d:1.55,h:1.05},
 {id:'kitchen',x:s.kitchenX,z:-4.5,w:s.kitchenWidth/2,d:.55,h:1.12},
 {id:'table',x:id==='stationRoom'?299.25:299,z:1,w:id==='stationRoom'?.65:1,d:.6,h:.82},
 {id:'sofa',x:298,z:3,w:id==='stationRoom'?.7:1.5,d:.5,h:1.05},
 ...(id==='flat'?[{id:'bedScreen',x:294.7,z:-3.2,w:.04,d:1.5,h:2.2}]:[]),
 ...(id==='room'?[{id:'bookshelf',x:295.3,z:-4.82,w:.55,d:.16,h:2.8}]:[]),
 ...(id==='stationRoom'?[{id:'kitchenShelf',x:302.1,z:-5.4,w:.75,d:.28,minY:1.9,h:2.3}]:[{id:'upperCabinets',x:303.15,z:-5.41,w:2.17,d:.325,minY:1.9,h:2.80}]),
 ...(['flat','penthouse'].includes(id)?[{id:'desk',x:302,z:1.8,w:1.3,d:.6,h:1.35},{id:'chair',x:301,z:3.2,w:.375,d:.375,h:1}]:[])
]]));
export const homeFixtures=home=>HOME_LAYOUTS[home]||HOME_LAYOUTS.room;
