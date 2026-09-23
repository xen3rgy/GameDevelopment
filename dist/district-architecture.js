import {HORIZON_ROADS} from './city-character-layout.js?v=0.8.1';
import * as THREE from './vendor/three.module.js';
import {entranceForBuilding,decorateAddress} from './city-addresses.js?v=0.8.1';
import {districtMaterial,fitFacadeUV} from './district-materials.js?v=0.8.1';
import {facadeSpans} from './public-realm-layout.js?v=0.8.1';

export const buildingStyle=(x,z,name)=>x>0&&Math.abs(z)<125?'modern':x<-120&&Math.abs(z)<125?'heritage':name==='WESTHAFEN LOGISTIK'?'industrial':'residential';

export function modernBuilding(art,x,z,w,d,h,color,name,face=1){
 const world=art.w,{box,sign,cylinder,sphere}=art.k,g=new THREE.Group();g.name='Modern · '+(name||'Skyline');world.scene.add(g);world.staticGroups.push(g);
 const civic=name==='STADTBANK',cafe=name==='CAFÉ MORGEN',office=h>23;
 const entry=entranceForBuilding(x,z,d,face),doorX=entry?(entry.x-x)*face:null;
 const stone=civic?0xc6c6bb:cafe?0xd6d2c5:office?0xafbbb9:0xbac3be,frame=0x2f444e,base=cafe?0x815f43:0x435760;
 const skin=districtMaterial('limestone',w,h-4,stone);
 const panel=new THREE.MeshStandardMaterial({color:office?0x38596b:0x537780,roughness:.22,metalness:.52});
 box(g,x,1.95,z,w,3.9,d,base);fitFacadeUV(box(g,x,(h+4)/2,z,w-.5,h-4,d-.5,0,skin),w-.5,d-.5);
 box(g,x,4.05,z,w+.28,.18,d+.28,0xbfc8c4);
 world.colliders.push({x,z,w:w/2+.35,d:d/2+.35,h:h+2.8});
 for(const [xx,zz,width,rot] of [[x,z+d/2,w,0],[x,z-d/2,w,Math.PI],[x+w/2,z,d,Math.PI/2],[x-w/2,z,d,-Math.PI/2]]){
  const f=new THREE.Group();f.position.set(xx,0,zz);f.rotation.y=rot;g.add(f);
  const cols=Math.max(2,Math.floor(width/(civic?4.2:3.4))),step=(width-.8)/cols,floors=Math.max(1,Math.floor((h-4.8)/3.4)),pitch=(h-4.8)/floors;
  for(let floor=0;floor<floors;floor++){
   const yy=5.5+floor*pitch;
   for(let c=0;c<cols;c++){
    const cx=-width/2+.4+step*(c+.5),lit=(c*3+floor*7+Math.round(x))%5;
    box(f,cx,yy,0,step-.18,2.4,.10,frame);box(f,cx,yy,.07,step-.3,2.25,.055,0,panel);
    if(lit===0||lit===3)box(f,cx,yy,.106,step-.40,2.10,.012,0,art.windows[lit]);
    box(f,cx,yy,.13,.042,2.35,.045,0x7d9398);
    if(civic||office)box(f,cx-step/2+.10,yy,.24,.15,2.6,.55,stone);
    else box(f,cx,yy+1.24,.28,step-.10,.09,.72,0x829593);
   }
   if(!civic)box(f,0,yy+1.42,.06,width-.5,.22,.20,stone);
  }
  // Street-level glazing has visible structural frames and a continuous plinth.
  const frontFace=Math.abs(rot-(face===1?0:Math.PI))<.001,opening=frontFace?doorX:null;
  for(let c=0;c<cols;c++){const cx=-width/2+.4+step*(c+.5);for(const p of facadeSpans(cx,step-.12,opening)){box(f,p.x,1.63,.12,p.w,2.65,.07,0,art.windows[(c+3)%5]);for(const side of [-1,1])box(f,p.x+side*p.w/2,1.65,.19,.07,2.9,.12,frame);}}
  for(const p of facadeSpans(0,width,opening))box(f,p.x,.36,.16,p.w,.25,.18,0x626f73);
 }
 const front=new THREE.Group();front.position.set(x,0,z+face*d/2);front.rotation.y=face===1?0:Math.PI;g.add(front);
 if(name){
  const location=entranceForBuilding(x,z,d,face),localX=location?(location.x-x)*face:0;
  // Every sign sits on a solid fascia or wall, within that facade's width.
  box(front,0,3.24,.28,w-.45,.59,.14,base);sign(front,name,0,3.25,.359,Math.min(w-1.2,name.length*.32),.38,'#f0e9d5',cafe?'#815f43':'#435760');
  if(location)decorateAddress(front,art.k,location,localX);
  if(cafe){for(let i=0;i<Math.floor(w/.25);i++){const px=-w/2+.15+i*.25;if(Math.abs(px-localX)<1.4)continue;box(front,px,.75,.31,.09,.65,.07,0xaa8359);}}
  const canopy=box(front,0,3.56,.72,w*.85,.08,1.4,0x758d92);canopy.material=panel;
  // Layered display bays sit within the facade envelope, clear of the entrance.
  for(const px of [-w*.32,w*.32]){
   if(Math.abs(px-localX)<2.5)continue;
   box(front,px,1.62,.21,2.5,1.95,.045,0x213b42);
   box(front,px,.78,.27,2.5,.10,.14,0xb8aa88);
   box(front,px,2.6,.28,2.56,.055,.12,0xbeb795);
   for(const side of [-1,1])box(front,px+side*1.27,1.68,.28,.035,1.87,.10,0x98a9a6);
   if(cafe){
    for(let n=0;n<4;n++){const xx=px-.8+n*.52;box(front,xx,1.03,.27,.3,.4,.12,[0xb0996a,0x746e53][n%2]);box(front,xx,1.06,.337,.17,.13,.008,0xdfd4b8);}
    sign(front,'RÖSTUNG DES MONATS',px,2.25,.243,2.1,.15,'#d9c9a5','#213b42');
   }else{
    box(front,px-.45,1.60,.251,.75,1.1,.015,0xbeb698);
    box(front,px-.45,1.60,.267,.60,.96,.015,0x728d89);
    for(let n=0;n<3;n++)box(front,px-.66+n*.21,1.5+n*.14,.28,.09,.3,.012,0xd2c4a3);
    cylinder(front,px+.69,.98,.26,.105,.32,0xb49d7e);
    for(let n=0;n<4;n++){const leaf=sphere(front,px+.69+Math.sin(n*2)*.10,1.2+n*.12,.27,.16,0x688668);leaf.scale.multiply(new THREE.Vector3(.6,1.5,.25));}
   }
  }
  for(const px of [-w*.3,w*.3]){
   const bulb=new THREE.MeshBasicMaterial({color:0x4a504c});
   box(front,px,3.74,.8,1.15,.05,.17,0,bulb).castShadow=false;
   world.atmosphere?.registerLamp(x+px*face,z+face*(d/2+.8),bulb,{height:3.74,power:75,poolSize:6,distance:12});
  }

 }
 // A recessed rooftop pavilion changes the skyline without filling the street with towers.
 box(g,x,h+.1,z,w+.12,.20,d+.12,0x52666c);
 const roofH=civic?2.1:1.45;box(g,x+w*.13,h+roofH/2+.2,z-d*.12,w*.57,roofH,d*.57,0x6c838a);
 box(g,x+w*.13,h+roofH+.23,z-d*.12,w*.59,.12,d*.59,0xbdc8c5);
 for(let i=0;i<4;i++)box(g,x-w*.30+i*.85,h+.3,z+d*.24,.63,.12,1.6,0x2e4655);
 return g;
}

// Scenery remains separate from playable bounds and detached interiors at x > 290.
export const BACKDROP_GROUND={minX:-280,maxX:195,minZ:-210,maxZ:210,top:-.05};
// Disjoint apron around the original square and western station base: no near-coplanar overlay.
export const BACKDROP_TILES=[
 [-280,195,-210,-125],[-280,195,125,210],[125,195,-125,125],
 [-280,-225,-125,125],[-225,-125,-125,-80],[-225,-125,80,125]
];
export const BACKDROP_BUILDINGS=[
 ...Array.from({length:6},(_,i)=>({x:149+(i%2)*27,z:[-97,-95,-27,27,97,97][i],w:18+(i%3)*2,d:22,h:24+(i%3)*8,style:'modern'})),
 ...[[-249,-104,24,18,10],[-174,-107,24,20,12],[-249,107,22,22,9],[-174,106,24,18,11],[-248,-34,22,25,12],[-248,30,22,26,10]].map(([x,z,w,d,h])=>({x,z,w,d,h,style:'heritage'})),
 ...[-84,-54,-26,26,54,84].flatMap((x,i)=>[-1,1].map(side=>({x,z:side*(145+(i%2)*8),w:22,d:22,h:x<0?12+(i%3)*3:23+(i%3)*5,style:x<0?'heritage':'modern'})))
];
export function cityBackdrop(world,kit){
 const {box,cylinder,sphere}=kit,g=new THREE.Group();g.name='Grounded city horizon';world.scene.add(g);world.staticGroups.push(g);
 const a=BACKDROP_GROUND;
 const ground=new THREE.Group();ground.name='Continuous scenery ground';g.add(ground);
 for(const [x,X,z,Z] of BACKDROP_TILES){const tile=box(ground,(x+X)/2,a.top-.3,(z+Z)/2,X-x,.6,Z-z,0x606b55);tile.castShadow=false;}
 for(const [index,b] of BACKDROP_BUILDINGS.entries()){
  const {x,z,w,d,h,style}=b,modern=style==='modern',wall=modern?[0x98aaa8,0xb4bbb0,0x879c9e][index%3]:[0x94775d,0xa48b6d,0x8b7d68][index%3],trim=modern?0xc3ccc3:0xc0ac8a,roof=modern?0x405862:0x4b504a;
  const root=new THREE.Group();root.name='Scenery · '+style;root.position.set(x,0,z);g.add(root);
  // Foundations overlap the soil, so no daylight gap can appear under any facade.
  box(root,0,.26,0,w+.35,.84,d+.35,0x65716a);
  box(root,0,h/2,0,w,h,d,wall);
  for(const [xx,zz,width,angle] of [[0,d/2,w,0],[0,-d/2,w,Math.PI],[w/2,0,d,Math.PI/2],[-w/2,0,d,-Math.PI/2]]){
   const face=new THREE.Group();face.position.set(xx,0,zz);face.rotation.y=angle;root.add(face);
   const cols=Math.max(3,Math.floor(width/4.3)),pitch=(width-2)/cols;
   for(let row=0,y=2.4;y<h-1.2;y+=3.5,row++)for(let c=0;c<cols;c++){
    const cx=-width/2+1+(c+.5)*pitch,lit=(index+c*3+row*7)%5;
    box(face,cx,y,.05,modern?pitch-.4:1.65,modern?2.2:1.95,.1,0x3d5356);
    box(face,cx,y,.112,modern?pitch-.55:1.42,modern?2.03:1.72,.035,0x4f6564,world.art?.windows?.[lit]);
    box(face,cx,y,.15,.055,modern?2.03:1.72,.045,trim);
    if(!modern)box(face,cx,y-1,.19,1.95,.1,.4,trim);
   }
   box(face,0,.55,.16,width,.22,.22,0x65716a);
   box(face,0,h-.2,.12,width+.25,.22,.28,trim);
   if(modern){for(let c=0;c<=cols;c++)box(face,-width/2+1+c*pitch,h/2,.2,.12,h-.7,.4,trim);}
   else for(const sx of [-width/2+.22,width/2-.22])box(face,sx,h/2,.12,.25,h,.28,trim);
  }
  if(modern){
   box(root,0,h+.13,0,w+.4,.26,d+.4,roof);box(root,w*.14,h+1.15,-d*.1,w*.52,2,d*.5,0x5d7378);box(root,w*.14,h+2.23,-d*.1,w*.54,.16,d*.52,trim);
   for(let n=0;n<3;n++)box(root,-w*.29+n*1.4,h+.52,d*.22,1,.7,1.8,0x88978e);
  }else{
   const rise=2.4,angle=Math.atan2(rise,d/2),length=Math.hypot(d/2,rise);
   for(const side of [-1,1]){const roofPanel=box(root,0,h+rise/2,side*d/4,w+.8,.22,length+.45,roof);roofPanel.rotation.x=side*angle;}
   // Fill both gables, rather than leaving the pitched roof hollow at the ends.
   const geo=new THREE.BufferGeometry();geo.setAttribute('position',new THREE.Float32BufferAttribute([0,h,-d/2,0,h,d/2,0,h+rise,0],3));geo.computeVertexNormals();
   const m=new THREE.MeshStandardMaterial({color:wall,roughness:.96,side:THREE.DoubleSide});
   for(const side of [-1,1]){const end=new THREE.Mesh(geo,m);end.position.x=side*w/2;root.add(end);}
   box(root,-w*.25,h+1.9,-d*.18,.9,3,1.2,wall);box(root,-w*.25,h+3.43,-d*.18,1.05,.15,1.35,trim);
  }
 }
 // A layered tree belt connects the edge to a landscape instead of a line of boxes.
 const onCorridor=(x,z,margin)=>HORIZON_ROADS.some(r=>Math.abs(x-r.x)<r.w/2+margin&&Math.abs(z-r.z)<r.d/2+margin);
 const tree=(x,z,i)=>{if(onCorridor(x,z,3)||typeof world.tree!=='function')return;world.tree(x,z,2.05,{grate:false,distant:true,seed:i})};
 for(const side of [-1,1])for(let i=0;i<27;i++)tree(-244+i*16,side*(180+(i%3)*5),i);
 for(let i=0;i<20;i++)tree(-266, -161+i*17,i);
 // Low boundary planting masks the seam at eye level, while preserving skyline views.
 for(const side of [-1,1])for(let i=0;i<18;i++){
  const x=-221+i*23,z=side*(x<-120?85:129);
  if(onCorridor(x,z,8))continue;
  const hedge=box(g,x,.58,z,14,1.4,2.5,0x53684d);hedge.castShadow=false;
  for(let n=0;n<4;n++)sphere(g,x-5+n*3.3,1.25,z,.75,[0x5e7251,0x70815b][n%2]);
 }
 return g;
}
