// Deliberate western plots. Coordinates are shared by geometry, navigation and survival.
export const WEST_BOUNDS={minX:-550,maxX:440,minZ:-255,maxZ:255};
export const WEST_DISTRICTS=[
 {id:'bruchfeld',name:'BRUCHFELD',x:-488,z:72,minX:-550,maxX:-425,minZ:-255,maxZ:255,color:'#666a4f',style:'townhouse'},
 {id:'altgleis',name:'ALTGLEIS',x:-378,z:73,minX:-425,maxX:-335,minZ:-255,maxZ:255,color:'#62635a',style:'industrial'},
 {id:'westbruch',name:'WESTBRUCH',x:-297,z:72,minX:-335,maxX:-260,minZ:-255,maxZ:255,color:'#796d5d',style:'residential'}
];
export const westDistrict=p=>WEST_DISTRICTS.find(d=>p.x>=d.minX&&p.x<d.maxX&&p.z>=d.minZ&&p.z<=d.maxZ);
export const WEST_SPAWN={x:-508,z:18};
export const WEST_SLEEP_POSE={x:-511.4,z:30.4,y:.55};
export const WEST_LOCATIONS=[
 {id:'roughSleep',name:'Provisorischer Schlafplatz',x:-511,z:24,type:'shelter',icon:'☾',color:'#b7a986',street:'Bruchfelder Weg',number:2},
 {id:'westKiosk',name:'PFAND & SPAR',x:-296,z:-12,type:'shop',icon:'▣',color:'#b7c197',street:'Westbruchstraße',number:5},
 {id:'westRecycle',name:'PFAND & SPAR · Rückgabe',x:-307,z:-12,type:'recycle',icon:'♻',color:'#b7c197',street:'Westbruchstraße',number:5}
];
export const WEST_BUS_STOP={id:'busWestbruch',name:'Westbruch · Bus',mode:'bus',x:-276,z:10,roadZ:0,arrival:{x:-274,z:11.5},vehicle:{x:-276,z:5.2,angle:Math.PI/2}};
// Disjoint widths prevent coplanar surfaces. The east mouth meets Bahnhof's old road.
export const WEST_ROADS=[
 {name:'Bruchfelder Weg',x:-487,z:0,w:124,d:7,kind:'rural',pavementWidth:3,pavementHeight:.14},
 {name:'Gleisweg',x:-380,z:0,w:90,d:9,kind:'service',pavementWidth:3,pavementHeight:.14},
 {name:'Westbruchstraße',x:-280,z:0,w:110,d:13,kind:'local'},
 {name:'Ladehofzufahrt',x:-381,z:-30,w:7,d:60,kind:'service',pavementWidth:2,pavementHeight:.14},
 {name:'Hofgasse',x:-318,z:29,w:7,d:58,kind:'local',pavementWidth:3},
 {name:'Gartenweg',x:-480,z:33,w:5,d:66,kind:'rural',pavementWidth:2.5,pavementHeight:.14}
];
export const WEST_PATHS=[
 {id:'oldLoadingDock',x:-399,z:-47.6,w:14,d:2.3,height:.57,kind:'dock'},
 {id:'oldLoadingRamp',x:-390.5,z:-47.6,w:3,d:2.3,height:.57,kind:'ramp'},
 {id:'warehouseEntryWalk',x:-381,z:-64,w:2.6,d:12,height:.14,kind:'paving'},
 {id:'warehouseDoorWalk',x:-393,z:-74.5,w:2.6,d:15,height:.14,kind:'paving'},
 {id:'warehouseApronWalk',x:-387,z:-68,w:14,d:2.6,height:.14,kind:'paving'},
 {id:'warehouseApron',x:-393,z:-75,w:30,d:12,height:.14,kind:'gravel'},
 // Rear routes end at an entrance, courtyard or lane, never in bare soil.
 {id:'northWestLink',x:-534,z:-25.75,w:3,d:38.5,height:.14,kind:'paving'},
 {id:'northEastLink',x:-475,z:-25.75,w:3,d:38.5,height:.14,kind:'paving'},
 {id:'northHouseDoor',x:-520,z:-48,w:3,d:12,height:.14,kind:'paving'},
 {id:'northWingDoor',x:-491,z:-48,w:3,d:12,height:.14,kind:'paving'},
 {id:'gartenTurn',x:-480,z:67,w:12,d:6,height:.14,kind:'paving'},
 {id:'gartenSouthDoor',x:-470,z:70,w:3,d:9,height:.14,kind:'paving'},
 {id:'gartenSouthLink',x:-474,z:67,w:12,d:3,height:.14,kind:'paving'},
 {id:'hofTurn',x:-318,z:61,w:16,d:7,height:.30,kind:'paving'},
 {id:'hofSouthDoor',x:-323,z:66,w:3,d:10,height:.30,kind:'paving'},
 {id:'loadingTurn',x:-381,z:-65,w:17,d:10,height:.14,kind:'gravel'},
 {id:'laundryDoor',x:-533,z:41,w:3,d:6,height:.14,kind:'paving'},
 {id:'courtyardPassage',x:-522,z:38,w:9,d:3,height:.14,kind:'paving'},
 {id:'playRearPassage',x:-514,z:65,w:3,d:14,height:.14,kind:'paving'},
 {id:'bruchPlayCourt',x:-507,z:48,w:24,d:21,height:.14,kind:'paving'},
 {id:'northBackLane',x:-498,z:-43,w:72,d:4,height:.14,kind:'paving'},
 {id:'northCourt',x:-515,z:-36,w:9,d:18,height:.14,kind:'paving'},
 {id:'westLaundryCourt',x:-534,z:36,w:20,d:9,height:.14,kind:'paving'},
 {id:'playExit',x:-490,z:49,w:12,d:4,height:.14,kind:'paving'},
 {id:'oldGarageApron',x:-468,z:-17,w:20,d:15,height:.14,kind:'gravel'},
 {id:'metalYard',x:-397,z:13,w:29,d:11,height:.14,kind:'gravel'},
 {id:'repairForecourt',x:-495,z:12,w:13,d:11,height:.14,kind:'gravel'},
 {id:'eastLoadingApron',x:-365,z:12,w:15,d:9,height:.14,kind:'gravel'},
 {id:'sleepPath',x:-509,z:17,w:6,d:27,height:.14,kind:'paving'},
 {id:'kioskCourt',x:-299,z:-14,w:31,d:12,height:.30,kind:'paving'},
 {id:'railCourt',x:-387,z:-34,w:44,d:37,height:.14,kind:'gravel'},
 {id:'busCourt',x:-277,z:11,w:17,d:8,height:.30,kind:'paving'},
 {id:'housingCourt',x:-290,z:32,w:40,d:27,height:.30,kind:'paving'},
 {id:'sleepYard',x:-510,z:32,w:16,d:12,height:.14,kind:'paving'},
 {id:'bruchDoor',x:-454,z:11.5,w:3,d:14,height:.14,kind:'gravel'},
 {id:'westDoor',x:-338,z:12.5,w:3,d:12,height:.30,kind:'paving'}
 ,...[-1,1].map(side=>({id:'westTransition'+side,x:-336,z:side*10,w:10,d:8,height:.14,kind:'gravel'}))
];
// Narrow, deliberate access routes for the outlying houses and working yards.
// These join the existing streets/courts; building bodies are cut out of meshes.
const access=(id,x,z,w,d,height=.14,kind='paving')=>({id,x,z,w,d,height,kind});
WEST_PATHS.push(
 access('tailHouseDoor',-543,-9.25,2,5.5),access('tailShopDoor',-509.5,-9.75,2,6.5),
 access('house1Door',-523,-12,2,11),access('cottageDoor',-497,-11.75,2,10.5),
 access('cornerDoor',-532,11,2,9),access('infillDoor',-442,-10.25,2,7.5),
 access('rearHouseDoor',-462,-47.5,2,9),
 access('outerCourtLane',-528,67,28,2.5),access('outerHouseDoor',-542,74,2,14),
 access('backEastDoor',-492,64.5,2,9),access('backEastLink',-486,66,12,2),
 access('railCornerLink',-454,67,32,2.5),access('railCornerDoor',-438,69.5,2,5),
 access('laneHouseLink',-465.5,43,29,2),access('laneHouseDoor',-451,44.25,2,2.5),
 access('gardenEastDoor',-433.5,46.75,2,7.5,.14,'gravel'),access('gardenEastLink',-442.25,43,17.5,2,.14,'gravel'),
 access('garageApronDoor',-465,-26.25,2.4,3.5,.14,'gravel'),
 access('railAnnexDoor',-363,16.75,2.4,2.5,.14,'gravel'),access('railOfficeDoor',-364,-13.25,2.4,11.5,.14,'gravel'),
 access('signalDoor',-364,-43,2.2,3, .14,'gravel'),access('railGarageDoor',-362,34.5,2.4,5,.14,'gravel'),
 access('railGarageLink',-369.5,32.5,15,2.4,.14,'gravel'),access('southWorksDoor',-391,63,2.4,6,.14,'gravel'),
 access('railGarageApron',-362,34,17,6,.14,'gravel'),
 access('southWorksLane',-380,59,24,2.4,.14,'gravel'),access('southWorksLink',-373,33.5,2.4,52,.14,'gravel'),
 access('westBlock1Door',-338,-12.25,2,9.5),
 access('westNorthEntry',-305,-20.75,2.2,14.5,.30),access('westNorthElbow',-302.5,-28,7.2,2.2,.30),
 access('westNorthPassage',-299.75,-63.75,2.2,71.5,.30),
 access('westBlock2Door',-285,-29.75,2.2,4.5,.30),access('westBlock2Link',-292.375,-28,14.75,2.2,.30),
 access('westRearDoor',-311,-47.25,2.2,2.5,.30),access('westRearLink',-305.375,-46,11.25,2.2,.30),
 access('westNorthLane',-298.5,-74,35,2.2,.30),access('westWingDoor',-316,-75.25,2.2,2.5,.30),
 access('westNorthBlockDoor',-281,-76.75,2.2,5.5,.30),
 access('westNorthRowLink',-311.375,-99.5,23.25,2.2,.30),access('westNorthRowDoor',-323,-100.5,2.2,2,.30),
 access('westKioskWingDoor',-266,-15.75,2.2,4.5,.30),
 access('westSouthPassage',-310,77.75,2.2,33.5,.30),access('westSouthEntry',-314,61,8,2.2,.30),
 access('westSouthWingLink',-316.5,67.5,13,2.2,.30),
 access('westSouthBlockLink',-298,78.5,24,2.2,.30),access('westSouthBlockDoor',-286,79.75,2.2,2.5,.30),
 access('westSouthRowLink',-312,94.5,4,2.2,.30),access('westSouthRowDoor',-314,96,2.2,3,.30),
 access('housingEntryWest',-304,16,2.2,7,.30),access('housingEntryEast',-272.5,17,2.2,7,.30),
 access('westBlock4Door',-280,47.25,2.2,3.5,.30),access('westGarageDoor',-302,47.75,2.4,4.5,.30)
);
// Gravel yards are flush with soil. Only their mouths rise gently to the street.
// The renderer and walking support use this same function.
const westRoadEdges=WEST_ROADS.map(r=>({...r,w:r.w+(r.w<r.d?2*(r.pavementWidth??7):0),d:r.d+(r.w>r.d?2*(r.pavementWidth??7):0)}));
export function westPathHeight(p,x,z){
 p=p.support??p;
 if(p.kind==='ramp')return -.03+(p.height+.03)*Math.max(0,Math.min(1,(p.x+p.w/2-x)/p.w));
 if(p.kind!=='gravel')return p.height;
 let y=-.03;
 for(const r of westRoadEdges){const gap=Math.hypot(Math.max(0,Math.abs(x-r.x)-r.w/2),Math.max(0,Math.abs(z-r.z)-r.d/2));if(gap<1.6)y=Math.max(y,-.03+((r.pavementHeight??.30)+.03)*(1-gap/1.6));}
 return y;
}
export const WEST_BUILDINGS=[
 {id:'bruchTailHouse',x:-543,z:-19,w:12,d:14,h:6.6,style:'townhouse',seed:42,face:1},
 {id:'bruchTailShop',x:-509.5,z:-19,w:6,d:12,h:6.5,style:'townhouse',seed:43,face:1},
 {id:'bruchOuterHouse',x:-542,z:87,w:18,d:13,h:8.8,style:'townhouse',seed:44,face:-1},
 {id:'bruchCourtSouth',x:-470,z:81,w:18,d:14,h:10.4,style:'residential',seed:45,face:-1},
 {id:'bruchRailCorner',x:-438,z:79,w:16,d:14,h:11.4,style:'residential',seed:46,face:-1},
 {id:'railRearWarehouse',x:-393,z:-90,w:30,d:18,h:6.8,style:'industrial',seed:47,face:1},
 {id:'railSouthWorks',x:-391,z:74,w:26,d:16,h:6.5,style:'industrial',seed:48,face:-1},
 {id:'westNorthRow',x:-323,z:-108,w:22,d:13,h:12.4,style:'residential',seed:49,face:1},
 {id:'westSouthRow',x:-314,z:104,w:22,d:13,h:12.4,style:'residential',seed:50,face:-1},
 {id:'bruchBackWest',x:-533,z:51,w:18,d:15,h:11.8,style:'residential',seed:33,face:-1},
 {id:'bruchBackEast',x:-492,z:77,w:17,d:16,h:10.2,style:'residential',seed:34,face:-1},
 {id:'bruchNorth',x:-520,z:-59,w:24,d:15,h:12.4,style:'residential',seed:36,face:1},
 {id:'bruchNorthWing',x:-491,z:-60,w:17,d:15,h:9.4,style:'residential',seed:37,face:1},
 {id:'westKioskWing',x:-266,z:-25,w:9,d:14,h:11.4,style:'residential',seed:39,face:1},
 {id:'westRearWing',x:-316,z:-84,w:23,d:15,h:10.4,style:'residential',seed:40,face:1},
 {id:'bruchRepair',x:-495,z:23,w:11,d:10,h:4.5,style:'industrial',seed:29,face:-1},
 {id:'bruchInfill',x:-442,z:-22,w:13,d:16,h:8.3,style:'townhouse',seed:31,face:1},
 {id:'railAnnex',x:-363,z:23,w:13,d:10,h:4.2,style:'industrial',seed:32,face:-1},
 {id:'bruchHouse1',x:-523,z:-25,w:17,d:15,h:10.2,style:'residential',seed:2,face:1},
 {id:'bruchHouse2',x:-454,z:27,w:18,d:17,h:10.8,style:'residential',seed:5,face:-1},
 {id:'bruchShed',x:-465,z:-33,w:12,d:10,h:3.2,style:'shed',seed:3,face:1},
 {id:'sleepCover',x:-511,z:29,w:7,d:5,h:3,style:'shelter',seed:1,face:-1},
 {id:'railStore',x:-399,z:-39,w:17,d:13,h:6.2,style:'industrial',seed:8,face:1},
 {id:'railSignal',x:-364,z:-48,w:7,d:8,h:5,style:'industrial',seed:4,face:1},
 {id:'westBlock1',x:-338,z:-30,w:23,d:26,h:12.4,style:'residential',seed:7,face:1},
 {id:'westBlock2',x:-285,z:-43,w:25,d:22,h:15.5,style:'residential',seed:10,face:1},
 {id:'westBlock3',x:-338,z:33,w:23,d:30,h:12.4,style:'residential',seed:12,face:-1},
 {id:'westBlock4',x:-280,z:58,w:24,d:18,h:12.4,style:'residential',seed:14,face:-1},
 {id:'westKioskBuilding',x:-296,z:-21,w:12,d:10,h:10.4,style:'kiosk',seed:0,face:1},
 {id:'westGarage',x:-302,z:54,w:11,d:8,h:3.4,style:'shed',seed:6,face:-1},
 {id:'bruchCottage',x:-497,z:-24,w:15,d:14,h:9.3,style:'residential',seed:16,face:1},
 {id:'bruchCorner',x:-532,z:22,w:17,d:13,h:10.2,style:'residential',seed:17,face:-1},
 {id:'bruchGardenShed',x:-514,z:77,w:19,d:13,h:10.4,style:'residential',seed:18,face:-1},
 {id:'bruchRearHouse',x:-462,z:-59,w:18,d:14,h:7.4,style:'townhouse',seed:19,face:1},
 {id:'bruchLaneHouse',x:-451,z:53,w:16,d:15,h:6.6,style:'townhouse',seed:20,face:-1},
 {id:'railWorkshop',x:-396,z:26,w:24,d:15,h:5.6,style:'industrial',seed:21,face:-1},
 {id:'railGarage',x:-362,z:43,w:17,d:12,h:3.8,style:'shed',seed:22,face:-1},
 {id:'railOffice',x:-364,z:-24,w:12,d:10,h:6.2,style:'industrial',seed:23,face:1},
 {id:'westRearBlock',x:-311,z:-58,w:18,d:19,h:12.4,style:'residential',seed:24,face:1},
 {id:'westCourtWing',x:-323,z:77,w:21,d:16,h:9.4,style:'residential',seed:25,face:-1},
 {id:'westSouthBlock',x:-286,z:89,w:29,d:16,h:12.4,style:'residential',seed:26,face:-1},
 {id:'westNorthBlock',x:-281,z:-88,w:28,d:17,h:12.4,style:'residential',seed:27,face:1}
];
// One open side on the sleeping shelter; no full-body collider may close it.
export const WEST_BUILDING_FIXTURES=WEST_BUILDINGS.filter(b=>b.style!=='shelter').map(b=>({x:b.x,z:b.z,w:b.w/2+.35,d:b.d/2+.35,h:b.h+({townhouse:2.85,shed:.18,industrial:1.4,residential:1.62,kiosk:.2}[b.style]??2)}));
export const WEST_BINS=[
 {id:200,x:-488,z:11,fixture:{x:-488,z:12.1},district:'bruchfeld'},
 {id:201,x:-438,z:-9,fixture:{x:-438,z:-10.1},district:'bruchfeld'},
 {id:202,x:-389,z:-17,fixture:{x:-390.1,z:-17},district:'altgleis'},
 {id:203,x:-357,z:10,fixture:{x:-357,z:11.1},district:'altgleis'},
 {id:204,x:-312,z:-16,fixture:{x:-313.1,z:-16},district:'westbruch'},
 {id:205,x:-270,z:11,fixture:{x:-269,z:11},district:'westbruch'}
];
export const WEST_BOTTLE_SPOTS=[
 {x:-488,z:10.4},{x:-462,z:9.2},{x:-439,z:-8.4},{x:-416,z:11},{x:-394,z:-15.7},{x:-369,z:10.4},{x:-350,z:-11},{x:-306,z:-11.3},{x:-284,z:11.8}
];
export const WEST_LANDMARKS=[{id:'oldRailTriangle',name:'Altes Gleisdreieck',x:-377,z:-54,w:72,d:14,kind:'rail'},{id:'bruchGardens',name:'Bruchfelder Gärten',x:-495,z:52,w:43,d:24,kind:'garden'}];
export const WEST_HOMES=[
 {id:'bruchCourtHome',label:'Bruchfelder Hof',home:true,x:-533,z:41,doorZ:42.7,angle:Math.PI,district:'bruchfeld'},
 {id:'bruchHome',label:'Bruchfelder Weg',home:true,x:-454,z:16,doorZ:17.8,angle:Math.PI,district:'bruchfeld'},
 {id:'railHome',label:'Gleisweg',home:true,x:-399,z:-30,doorZ:-31,angle:0,district:'altgleis'},
 {id:'westHome',label:'Westbruchstraße',home:true,x:-338,z:16,doorZ:17.8,angle:Math.PI,district:'westbruch'}
];
export const WEST_ACTIVITIES=[
 {id:'bruchPlayground',cluster:'bruchPlayground',kind:'wait',x:-507,z:40,angle:Math.PI,open:540,close:1200,district:'bruchfeld'},
 {id:'bruchCourtRest',cluster:'bruchCourtRest',kind:'wait',x:-507,z:55,angle:0,open:540,close:1200,district:'bruchfeld'},
 {id:'westGardens',cluster:'westGardens',kind:'browse',x:-476.7,z:49,angle:-Math.PI/2,open:480,close:1200,district:'bruchfeld'},
 {id:'westRail',cluster:'westRail',kind:'wait',x:-386,z:-23,angle:Math.PI,open:420,close:1200,district:'altgleis'},
 ...[-4,0,4].map((dx,i)=>({id:'busWestbruch-wait'+i,cluster:'busWestbruch',kind:'wait',x:-276+dx,z:11,angle:Math.PI,open:360,close:1380,district:'westbruch'})),
 {id:'westBrowse',cluster:'westShop',kind:'browse',x:-298,z:-12,angle:Math.PI,open:420,close:1320,district:'westbruch'}
];
