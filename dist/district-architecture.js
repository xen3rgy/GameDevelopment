import {HORIZON_ROADS} from './city-character-layout.js?v=0.8.1';
import * as THREE from './vendor/three.module.js';
import {entranceForBuilding,decorateAddress} from './city-addresses.js?v=0.8.1';
import {districtMaterial,fitFacadeUV,districtWindowSet} from './district-materials.js?v=0.8.1';
import {facadeSpans} from './public-realm-layout.js?v=0.8.1';

export const buildingStyle=(x,z,name)=>x>0&&Math.abs(z)<125?'modern':x<-120&&Math.abs(z)<125?'heritage':name==='WESTHAFEN LOGISTIK'?'industrial':'residential';

// Outer districts share the core city's masonry, glazing and human-scale details.
// Keep every footprint fixed; repeated details are instanced by ExpansionScene.
const outerGable=new THREE.BufferGeometry();
outerGable.setAttribute('position',new THREE.Float32BufferAttribute([0,0,-.5,0,0,.5,0,1,0,0,0,.5,0,0,-.5,0,1,0],3));outerGable.computeVertexNormals();
const outerShed=new THREE.BufferGeometry();
outerShed.setAttribute('position',new THREE.Float32BufferAttribute([0,0,-.5,0,0,.5,0,1,-.5,0,0,.5,0,0,-.5,0,1,-.5],3));outerShed.computeVertexNormals();
export function expansionBuilding(root,kit,b,windows=[]){
 windows=districtWindowSet(windows,b.x,b.z);
 const {box,sign,cylinder,mat}=kit,{x,z,w,d,h,style,seed=0}=b,office=style==='office',industry=style==='industrial',campus=style==='campus',town=style==='townhouse',modern=office||campus;
 const tones=modern?[0xbac3be,0xd0cec0,0xaab9b7]:industry?[0xc5b39b,0xb4a28f,0xd1c1aa]:[0xdfd1bc,0xd8c7ad,0xc8baa4];
 const edgeMix=modern&&(x<180||(campus&&z>-153)),wall=edgeMix?[0xc9c7b8,0xc6c4b6,0xb8c1b7][seed%3]:tones[seed%3],trim=modern?0xc7cec6:0xdfd1b8,frame=modern?0x314c57:0x384b48,roofColor=0x48575b;
 const skin=districtMaterial(modern||(town&&seed%2===0)?'limestone':'brick',w,h,wall);
 const variant=seed%4,terraced=modern&&variant!==0,shoulder=terraced?h-3.4:h;
 // Stepped office/campus upper floors, domestic bay divisions and shed roofs
 // alter the massing while retaining the exact existing solid footprint.
 fitFacadeUV(box(root,x,shoulder/2,z,w,shoulder,d,0,skin),w,d);
 if(terraced){
  fitFacadeUV(box(root,x+w*.08,shoulder+1.7,z-d*.06,w*.76,3.4,d*.76,0,skin),w*.76,d*.76);
  box(root,x,shoulder+.04,z,w,.1,d,roofColor);
 }
 box(root,x,.36,z,w+.22,.72,d+.22,0x747c73);
 box(root,x,3.45,z,w+.22,.18,d+.22,trim);
 if(!terraced){box(root,x,h-.22,z,w+.3,.24,d+.3,trim);box(root,x,h+.1,z,w+.5,.2,d+.5,roofColor);}
 else box(root,x+w*.08,h+.08,z-d*.06,w*.76+.25,.16,d*.76+.25,roofColor);
 const face=b.face??1;
 for(const [xx,zz,width,rot,front] of [[x,z+d/2,w,0,face===1],[x,z-d/2,w,Math.PI,face===-1],[x+w/2,z,d,Math.PI/2,false],[x-w/2,z,d,-Math.PI/2,false]]){
  const f=new THREE.Group();f.position.set(xx,0,zz);f.rotation.y=rot;root.add(f);
  const cols=Math.max(3,Math.floor(width/(industry?6:town?4.7:office?3.1:campus?4.5:3.5+(variant%2)*.5))),pitch=(width-1)/cols;
  const rows=industry?1:Math.max(1,Math.floor((shoulder-4)/3.3)),step=(shoulder-4)/rows;
  for(let row=0;row<rows;row++){
   const y=industry?shoulder-1.5:4.95+row*step;
   if(modern)box(f,0,y+1.24,.13,width-.5,.17,.26,trim);
   for(let col=0;col<cols;col++){
    const cx=-width/2+.5+(col+.5)*pitch,ww=modern?pitch-.38:industry?pitch-1.2:town?1.45:1.72,hh=industry?1.05:2.05;
    box(f,cx,y,.06,ww+.16,hh+.18,.12,frame);
    box(f,cx,y,.135,ww,hh,.035,0,windows[(col*3+row*7+seed)%5]||mat(0x52666b));
    for(const side of [-1,1])box(f,cx+side*(ww/2+.1),y,.15,.09,hh+.28,.16,trim);
    box(f,cx,y-hh/2-.12,.18,ww+.4,.12,.32,trim);
    box(f,cx,y+hh/2+.12,.15,ww+.35,.11,.22,trim);
    box(f,cx,y,.17,.055,hh,.05,0x89998f);
    if(!modern)box(f,cx,y+.27,.17,ww,.045,.05,0x89998f);
    if(!industry&&!modern&&(col+variant)%3!==0&&(town?row===0:true)){
     // Shallow loggias stay inside the established collision margin.
     box(f,cx,y-1,.16,2.25,.12,.3,0x7b8278);
     box(f,cx,y-.36,.31,2.25,.065,.035,frame);
     box(f,cx,y-.68,.30,2.15,.55,.025,town?0xaaa99a:variant%2?0x8faaa3:0xb0b6a9);
     for(const side of [-1,1])box(f,cx+side*1.1,y-.65,.24,.045,.7,.18,frame);
     if((row+col+seed)%4===0)box(f,cx+.65,y-.28,.22,.65,.2,.18,0x728261);
    }
    if(office&&col%2===0)box(f,cx-ww/2-.1,y,.24,.12,2.5,.2,trim);
    if(campus)box(f,cx,y+1.15,.25,ww+.3,.09,.18,0x7c9286);
    if(edgeMix&&row===0&&col%2===variant%2){box(f,cx,y-.7,.23,ww,.48,.1,0x9ba997);box(f,cx,y-.42,.28,ww+.15,.055,.07,frame);}
   }
  }
  for(const side of [-1,1]){
   box(f,side*(width/2-.2),shoulder/2,.1,.23,shoulder-.5,.22,trim);
   if(!office)cylinder(f,side*(width/2-.55),(shoulder-.6)/2,.23,.045,shoulder-.6,0x65726d);
  }
  if(industry){
   if(front){
   for(const cx of [-width*.28,width*.28]){
    box(f,cx,1.64,.12,4.8,3.28,.18,frame);
    for(let yy=.38;yy<3.1;yy+=.42)box(f,cx,yy,.23,4.5,.035,.025,0x8d9994);
    box(f,cx,3.27,.25,5,.16,.2,0x7b8985);
    for(const side of [-1,1])box(f,cx+side*2.5,1.5,.27,.14,2.8,.1,0xc8af6b);
   }
   sign(f,'ANLIEFERUNG · '+String(seed%9+1).padStart(2,'0'),0,4,.2,Math.min(7,width*.38),.3,'#e2d9bc','#48534f');
   }else{
    for(let col=0;col<=cols;col++)box(f,-width/2+.35+col*(width-.7)/cols,(shoulder-1)/2,.12,.16,shoulder-1,.16,0x9eaa9e);
    box(f,width*.28,1.35,.13,1.2,2.7,.13,0x52615b);
    for(let n=0;n<4;n++)box(f,-width*.22,2.05+n*.15,.17,2.5,.07,.12,0x64736a);
   }
  }else{
   for(let col=0;col<cols;col++){
    const cx=-width/2+.5+(col+.5)*pitch;
    if(front&&Math.abs(cx)<2.2)continue;
    box(f,cx,1.8,.12,modern?pitch-.3:1.65,2.25,.1,frame);
    box(f,cx,1.8,.18,modern?pitch-.5:1.43,2.02,.025,0,windows[(col+seed+2)%5]||mat(0x52666b));
    box(f,cx,1.8,.21,.055,2.04,.04,trim);
    box(f,cx,.64,.22,modern?pitch-.2:1.9,.13,.25,trim);
   }
  }
  if(!industry&&!modern){
   for(let col=0;col<cols;col+=town?2:3){const cx=-width/2+.5+(col+.5)*pitch;box(f,cx,shoulder/2,.04,.18,shoulder-.3,.12,town?0xb3a491:0xa5ad9e);}
   if(front&&!b.name&&!town&&seed%3===0){
    const cx=-width*.3,label=['AHORN · BÄCKEREI','NORDRING · KIOSK','RAD & ALLTAG'][Math.floor(seed/3)%3];
    box(f,cx,2.95,.18,Math.min(6,width*.35),.42,.12,0x596d60);sign(f,label,cx,2.95,.248,Math.min(5.7,width*.33),.26,'#eee4cd','#596d60');
   }
  }
  if(town&&front)for(const side of [-1,1]){
   box(f,side*width*.3,.48,.19,2.7,.45,.22,0xb0ab97);box(f,side*width*.3,.8,.19,2.5,.26,.20,0x6a805c);
   for(let n=-1;n<=1;n++){box(f,side*width*.3+n*.9,1.3,.08,.035,1.25,.07,0x8b947d);box(f,side*width*.3+n*.9,1.16+Math.abs(n)*.16,.18,.48,.48,.18,0x6a805c);}
  }
 }
 const entry=new THREE.Group();entry.position.set(x,0,z+face*d/2);entry.rotation.y=face===1?0:Math.PI;root.add(entry);
 box(entry,0,1.36,.16,1.75,2.72,.25,frame);box(entry,0,1.55,.30,1.42,2.02,.025,0,windows[1]||mat(0x3c535b));
 box(entry,.53,1.16,.32,.05,.35,.04,0xb7b8a3);box(entry,0,2.87,.23,modern?4.2:2.7,.12,.23,trim);
 box(entry,1.2,1.8,.19,.18,.3,.05,0xb5b09b);
 if(b.name)sign(entry,b.name,0,3.8,.2,Math.min(w-2,9),.5,'#eee2cc','#354f50');
 if(!modern&&!industry&&(town||variant<2)){
  // Closed pitched roof, contained within the existing height allowance.
  const rise=town?1.65:1.15,angle=Math.atan2(rise,d/2),length=Math.hypot(d/2,rise);
  for(const side of [-1,1]){const roof=box(root,x,h+rise/2+.15,z+side*d/4,w+.65,.16,length+.25,roofColor);roof.rotation.x=side*angle;}
  for(const side of [-1,1]){const gable=new THREE.Mesh(outerGable,mat(wall));gable.scale.set(1,rise,d);gable.position.set(x+side*w/2,h+.15,z);root.add(gable);}
  box(root,x-w*.25,h+.75,z-d*.18,.7,1.5,.9,wall);
  box(root,x-w*.25,h+1.55,z-d*.18,.84,.12,1.04,trim);
 }else if(industry){
  const bays=variant%2?3:2,span=d/bays;
  for(let i=0;i<bays;i++){
   const zz=z-d/2+(i+.5)*span,roof=box(root,x,h+.55,zz,w,.12,Math.hypot(span,1.05),roofColor);roof.rotation.x=Math.atan2(1.05,span);
   box(root,x,h+.56,zz-span/2,w,1.06,.06,0x6b8c91);
   for(const side of [-1,1]){const end=new THREE.Mesh(outerShed,mat(roofColor));end.scale.set(1,1.05,span);end.position.set(x+side*(w/2-.05),h+.03,zz);root.add(end);}
  }
  for(let i=0;i<2;i++){cylinder(root,x-w*.3+i*w*.55,h+.65,z+d*.26,.42,1.2,0x879089);box(root,x-w*.3+i*w*.55,h+1.3,z+d*.26,1.2,.15,1.2,0x626d68);}
 }else{
  box(root,x+w*.12,h+.55,z-d*.1,w*(campus?.28:.37),.9,d*.3,0x788a89);
  box(root,x+w*.12,h+1.06,z-d*.1,w*(campus?.3:.39),.12,d*.32,trim);
  if(campus){box(root,x-w*.15,h+.2,z+d*.1,w*.35,.18,d*.3,0x73845f);for(let i=0;i<3;i++)box(root,x-w*.22+i*1.4,h+.34,z-d*.17,1,.12,1.5,0x3f5964);}
  else for(let i=0;i<3;i++)box(root,x-w*.23+i*1.3,h+.3,z+d*.17,1,.35,1.6,0x3f5964);
 }
 if(terraced){
  for(const [dx,dz,width,angle] of [[0,d*.38,w*.76,0],[0,-d*.38,w*.76,Math.PI],[w*.38,0,d*.76,Math.PI/2],[-w*.38,0,d*.76,-Math.PI/2]]){
   const upper=new THREE.Group();upper.position.set(x+w*.08+dx,0,z-d*.06+dz);upper.rotation.y=angle;root.add(upper);
   box(upper,0,h-1.65,.05,width-.8,2.15,.08,0,windows[(seed+2)%5]||mat(frame));
   for(let n=-2;n<=2;n++)box(upper,n*(width-.8)/5,h-1.65,.12,.1,2.3,.08,trim);
  }
 }
}

export function modernBuilding(art,x,z,w,d,h,color,name,face=1){
 const glazing=districtWindowSet(art.windows,x,z);
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
    if(lit===0||lit===3)box(f,cx,yy,.106,step-.40,2.10,.012,0,glazing[lit]);
    box(f,cx,yy,.13,.042,2.35,.045,0x7d9398);
    if(civic||office)box(f,cx-step/2+.10,yy,.24,.15,2.6,.55,stone);
    else box(f,cx,yy+1.24,.28,step-.10,.09,.72,0x829593);
   }
   if(!civic)box(f,0,yy+1.42,.06,width-.5,.22,.20,stone);
  }
  // Street-level glazing has visible structural frames and a continuous plinth.
  const frontFace=Math.abs(rot-(face===1?0:Math.PI))<.001,opening=frontFace?doorX:null;
  for(let c=0;c<cols;c++){const cx=-width/2+.4+step*(c+.5);for(const p of facadeSpans(cx,step-.12,opening)){box(f,p.x,1.63,.12,p.w,2.65,.07,0,glazing[(c+3)%5]);for(const side of [-1,1])box(f,p.x+side*p.w/2,1.65,.19,.07,2.9,.12,frame);}}
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
