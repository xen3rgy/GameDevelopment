// Shared exterior footprint. Detached interiors are deliberately outside this area.
export const WORLD_BOUNDS={minX:-222,maxX:115,minZ:-119,maxZ:119};
export const ROAD_X=[-202,-110,0,110];
export const ROAD_Z=[-65,0,65];
export const ROADS=[...ROAD_Z.map(z=>({x:-50,z,w:350,d:13})),...ROAD_X.map(x=>({x,z:0,w:13,d:x===-202?160:250}))];
export function exteriorContains(x,z,r=0){
 return Number.isFinite(x)&&Number.isFinite(z)&&x>=-222+r&&x<=115-r&&z>=-119+r&&z<=119-r&&(x>=-115+r||Math.abs(z)<=79-r);
}
export const districtOf=p=>p.x<-120?'BAHNHOFSVIERTEL':p.z<-48?'WESTHAFEN':p.z>48?'KANALVIERTEL':p.x>15?'INNENSTADT':'LINDENHÖFE';
export const STATION_PLAZAS=[{x:-174,z:-32,w:42,d:50},{x:-174,z:32.5,w:42,d:51}];
export const VIADUCT={x:-145,z:0,w:12,d:158,bottom:5.8,top:6.6};
export const DISTRICT_FIXTURES=[
 ...[-188,-162].map(x=>({x,z:-32.1,w:.09,d:.09,h:4.4,kind:'canopyPost'})),
 ...[-149,-141].flatMap(x=>[-53,-32,-11,11,32,53].map(z=>({x,z,w:.65,d:1.1,h:5.8,kind:'pillar'}))),
 ...[-193,-155].flatMap(x=>[-27,27].map(z=>({x,z,w:.9,d:.9,h:1.1,kind:'planter'}))),
 ...[-191,-159].map(x=>({x,z:-29,w:1.5,d:.5,h:1.2,kind:'bench'})),
 {x:-156,z:43,w:1.5,d:.5,h:1.2,kind:'bench'},
 // Low masonry edges finish the extension without blocking the three connecting streets.
 {x:-170,z:-79,w:52,d:.3,h:1.1,kind:'boundary'},
 {x:-170,z:79,w:52,d:.3,h:1.1,kind:'boundary'},
 {x:-222,z:0,w:.3,d:79,h:1.1,kind:'boundary'}
];
export const STATION_BUILDINGS=[
 [-175,-42,36,18,10,'#9b7052','LINDENSTADT WEST',1],
 [-179,-19,9,8,4.2,'#ab885f','KIOSK AM GLEIS',1],
 [-179,29,28,20,17,'#ae8a70','GLEISHÖFE',-1],
 [-174,52,22,12,7,'#8d6954','WERKSTATT WEST',-1],
 [-216,-33,8,34,18,'#9c8273',null,1],
 [-216,34,8,34,16,'#8f8374',null,1],
 [-126,-34,12,27,21,'#9d8e7a',null,1],
 [-126,34,12,27,18,'#a28b74',null,1]
];
