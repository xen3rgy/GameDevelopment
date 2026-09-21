// Metres, shared by the scene and pedestrian collision. Keep crowns clear of lamps,
// entrance approaches and facade canopies; the 1.92 m tree pits remain on the pavement.
export const STREET_TREES=[-94,-54,-20,18,39,89].flatMap(x=>[-10,10,-55,55].map(z=>({x,z,r:2.4})));
export const STREET_PLANTERS=[-91,48,96].map(x=>({x,z:10,w:1,d:.5}));
export const ENTRANCE_HALF_WIDTH=1.18;
// Trim window/awning intervals around an entrance instead of putting a door through them.
export function facadeSpans(center,width,doorX=null,clearance=ENTRANCE_HALF_WIDTH){
 const a=center-width/2,b=center+width/2;
 if(doorX==null||b<=doorX-clearance||a>=doorX+clearance)return [{x:center,w:width}];
 return [[a,Math.min(b,doorX-clearance)],[Math.max(a,doorX+clearance),b]].filter(([l,r])=>r-l>.12).map(([l,r])=>({x:(l+r)/2,w:r-l}));
}
