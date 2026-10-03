import * as THREE from './vendor/three.module.js';
import {HOME_STYLES,homeFixtures,HOME_ALIGNMENT} from './home-layout.js?v=0.8.1';
// Static room variants. All interactive anchors and animated props remain in HomeScene.
export function buildHomeInteriors(world,kit){
 const {box,cylinder,sphere,mat}=kit,variants=new Map();
 const glow=new THREE.MeshBasicMaterial({color:0xffdfaa});
 const cityWindows=[0xb5c6c6,0xe5cc98].map(color=>new THREE.MeshBasicMaterial({color}));
 const glass=new THREE.MeshStandardMaterial({color:0xb7d5dc,roughness:.15,metalness:.25,transparent:true,opacity:.13,depthWrite:false});
 for(const [id,s] of Object.entries(HOME_STYLES)){
  let g=new THREE.Group();g.name='Home · '+id;world.homeShell.add(g);variants.set(id,g);
  const premium=id==='penthouse',modern=premium||id==='flat',basic=id==='stationRoom';
  const fixtures=homeFixtures(id),f=name=>fixtures.find(v=>v.id===name),shell=g;
  const section=(name)=>{g=new THREE.Group();g.name=name;shell.add(g);};
  const place=(x,z,dx=0,dz=0,angle=0)=>{const group=g;group.position.set(x+dx,0,z+dz);group.rotation.y=angle;for(const child of group.children){child.position.x-=x;child.position.z-=z;}g=shell;return group;};
  const b=(x,y,z,w,h,d,c,m)=>box(g,x,y,z,w,h,d,c,m);
  const leg=(x,z,height=.70,color=s.wood)=>b(x,.07+height/2,z,.07,height,.07,color);
  const plant=(x,y,z,size=.4)=>{cylinder(g,x,y+size*.35,z,size*.32,size*.7,premium?0x8a795e:0x977958);for(let i=0;i<5;i++){const m=sphere(g,x+Math.sin(i*2)*size*.3,y+size*(.9+i*.07),z+Math.cos(i*2)*size*.3,size*.32,0x526d51);m.scale.y*=1.8;}};
  const art=(x,y,z,w,h)=>{b(x,y,z,w+.09,h+.09,.07,s.wood);b(x,y,z+.045,w,h,.025,modern?0xd4c7ae:0xe0d9c3);for(let i=0;i<3;i++){const m=b(x-w*.3+i*w*.3,y+(i%2?-.1:.1)*h,z+.064,w*.19,h*(.35+i*.12),.015,[s.accent,s.fabric,0xb78965][i]);m.rotation.z=(i-1)*.12;}};
  // Solid ceiling underside exactly matches ROOMS.home.ceiling; no camera cutaway.
  b(300,3.45,0,14,.20,12,s.ceiling,new THREE.MeshStandardMaterial({color:s.ceiling,emissive:s.ceiling,emissiveIntensity:modern?.14:.07,roughness:1}));b(300,-.09,0,14,.30,12,s.floor);
  if(premium){for(let x=294;x<307;x+=1.8)for(let z=-5.1;z<6;z+=1.8)b(x,.069,z,1.77,.009,1.77,(Math.round(x+z)%2)?0xbeb5a8:0xb1a79a);}
  else for(let i=0;i<26;i++){const x=293.25+i*.52;b(x,.068,0,.014,.009,11.8,basic?0x534d43:0x7d705d);if(i%3===0)b(x+.24,.07,(i%4-2)*2,.48,.01,.02,0x80715e);}
  b(300,1.7,-6,14,3.5,.25,s.wall);b(307,1.7,0,.25,3.5,12,s.wall);
  for(const x of [296,304])b(x,1.7,6,6,3.5,.25,s.wall);b(300,3.05,6,2,.8,.25,s.wall);
  // West window is an opening with a sill, reveals, glass and actual depth beyond it.
  const win= premium?{lo:-5.1,hi:5.1,bottom:.42,top:3.12}:id==='flat'?{lo:-1.8,hi:2,bottom:.9,top:2.85}:basic?{lo:-.2,hi:1.25,bottom:1.15,top:2.5}:{lo:-1.2,hi:1.2,bottom:1,top:2.75};
  const span=win.hi-win.lo,mid=(win.lo+win.hi)/2;
  for(const [lo,hi] of [[-6,win.lo],[win.hi,6]])b(293,1.7,(lo+hi)/2,.25,3.5,hi-lo,s.wall);
  b(293,win.bottom/2,mid,.25,win.bottom,span,s.wall);b(293,(win.top+3.5)/2,mid,.25,3.5-win.top,span,s.wall);
  for(const z of [win.lo,win.hi])b(293.10,(win.bottom+win.top)/2,z,.24,win.top-win.bottom,.10,premium?0x343c3d:0xe0dccb);
  for(const y of [win.bottom,win.top])b(293.1,y,mid,.33,.10,span,premium?0x343c3d:0xe0dccb);
  b(293.13,win.bottom-.04,mid,.46,.10,span+.15,modern?0xc8c6ba:s.wood);
  for(let z=win.lo+span/(premium?5:2);z<win.hi-.1;z+=span/(premium?5:2))b(293.06,(win.bottom+win.top)/2,z,.12,win.top-win.bottom,.055,0x667776);
  const pane=b(292.98,(win.bottom+win.top)/2,mid,.025,win.top-win.bottom,span,0,glass);pane.castShadow=false;
  b(285,3,0,.1,18,32,0,new THREE.MeshBasicMaterial({color:premium?0x9badb7:0xa9b4b0}));
  for(let i=0;i<9;i++){
   const z=-8+i*2,h=premium?2.2+(i*7%5)*.65:3.5+(i%3)*.6,x=premium?288-(i%3):290-(i%2);
   b(x,-2+h/2,z,1.1,h,1.65,[0x697a7e,0x829090,0x8b897c][i%3]);
   for(let y=-.4;y<h-2;y+=.7)b(x+.57,y,z,.02,.28,.65,0,cityWindows[i%3?0:1]);
  }
  if(basic){for(let i=0;i<4;i++)b(294.1+i*.65,.6+(i%2)*.18,-5.86,.35+i*.06,.12,.01,0xaaa08a);for(const z of [.1,.3,.5,.7,.9])b(293.22,.62,z,.18,.55,.09,0xb2aa96);for(const z of [-.4,1.45])b(293.35,1.9,z,.12,1.7,.3,0x887c67);}
  if(!basic&&!premium)for(const z of [win.lo-.12,win.hi+.12]){b(293.35,1.72,z,.13,2.3,.30,id==='flat'?0xc5cbbd:0x9baba1);}
  // Baseboards and an inset entrance with a real frame, handle and doormat.
  for(const x of [293.16,306.84])b(x,.17,0,.07,.20,11.75,modern?0xddd9cc:s.wood);
  b(300,.17,-5.84,13.7,.20,.07,modern?0xddd9cc:s.wood);for(const x of [296,304])b(x,.17,5.84,5.8,.20,.07,s.wood);
  b(300,1.32,5.94,1.7,2.5,.10,s.wood);for(const x of [299.08,300.92])b(x,1.4,5.81,.12,2.75,.17,modern?0xd9d6c9:0x6f6252);b(300,2.75,5.81,1.96,.13,.17,s.wood);b(300.64,1.17,5.83,.22,.055,.10,0xaaa68f);b(300,.075,4.98,1.75,.018,.75,0x555a51);
  // Beds: metal single, pine daybed, upholstered double, floating platform suite.
  section('Bed');
  const bed=f('bed'),bx=bed.x,bw=s.bedWidth;
  if(basic){b(bx,.51,-3.25,bw,.10,2.9,0x64665c);for(const x of [bx-bw/2+.07,bx+bw/2-.07])for(const z of [-4.65,-1.85]){cylinder(g,x,.47,z,.035,.80,0x64665c);}for(const z of [-4.65,-1.85])b(bx,.65,z,bw,.07,.055,0x64665c);}
  else{b(bx,.36,-3.25,bw,.48,3.0,s.wood);b(bx,premium?.95:.88,-4.74,bw+.06,premium?1.6:1.35,.12,modern?s.fabric:s.wood);}
  b(bx,.66,-3.25,bw-.08,.22,2.85,0xe2dfcf);b(bx,.80,-2.93,bw-.10,.08,2.15,s.fabric);
  for(const x of bw>1.7?[bx-.48,bx+.48]:[bx])b(x,.84,-4.24,bw>1.7?.75:bw-.2,.15,.48,0xeee7d6);
  if(premium){b(bx,.16,-3.25,bw+.15,.10,3.12,0x373d3b);b(bx,.24,-1.73,bw-.2,.025,.025,0,glow);for(let x=bx-bw/2+.08;x<bx+bw/2;x+=.19)b(x,2.2,-4.87,.08,2.1,.09,s.wood);}
  if(id==='flat'){b(bx,1.12,-4.74,bw+.06,2.1,.08,s.accent);b(bx,.079,-3.25,3.3,.025,3.7,0xbdb7a5);}
  place(bx,-3.25,0,HOME_ALIGNMENT.bedShiftZ);
  section('Bedside');
  b(294.1,.42,-4.8,.75,.70,.7,s.wood);b(294.1,.62,-4.43,.54,.03,.03,0xbebbaa);cylinder(g,294.1,1.02,-4.8,.035,.49,0x737c72);cylinder(g,294.1,1.32,-4.8,.23,.25,basic?0xcfb585:0xe5d6b8);
  place(294.1,-4.8,f('nightstand').x-294.1,f('nightstand').z+4.8);
  // Kitchens range from an exposed compact unit to a full integrated kitchen.
  const k=f('kitchen'),kw=k.w*2,back=-5.80,front=-4.64;
  section('Kitchen');
  b(k.x,.54,k.z,kw,.9,1.10,modern?(premium?0x545b57:0xbfc9bf):s.wood);b(k.x,1.02,k.z,kw,.08,1.16,premium?0xd6d0c1:0xb6b5a5);
  const doors=Math.max(3,Math.round(kw/.7)),doorWidth=kw/doors;
  for(let i=0;i<doors;i++){const x=k.x-kw/2+doorWidth*(i+.5);b(x,.55,front-.015,doorWidth-.04,.79,.035,modern?0xcbd0c3:s.accent);b(x,.84,front+.022,.25,.035,.04,premium?0xab9466:0x777b70);}
  const hob=k.x-kw/2+.62,sink=k.x+kw/2-.55;
  b(hob,1.068,k.z+.12,.82,.018,.60,0x2f3c3d);for(const x of [hob-.21,hob+.21])for(const z of [k.z-.03,k.z+.27])cylinder(g,x,1.083,z,.12,.012,0x777f79);
  b(sink,1.069,k.z,.54,.016,.62,0x7c918e);b(sink,1.08,k.z,.40,.018,.46,0x374e50);cylinder(g,sink,1.23,k.z-.32,.025,.33,0xb3bdb4);b(sink,1.39,k.z-.22,.045,.04,.20,0xb3bdb4);
  b(k.x,1.39,back+.015,kw,.58,.035,modern?0xb7c3b9:0xc6be9f);
  if(basic){b(k.x,1.95,-5.55,1.5,.08,.55,s.wood);for(const x of [k.x-.55,k.x+.55])b(x,1.8,-5.72,.055,.35,.2,0x505c53);b(k.x-.45,2.12,-5.55,.25,.27,.24,0xa8936c);}
  else{for(let i=0;i<doors;i++){const x=k.x-kw/2+doorWidth*(i+.5);b(x,2.36,-5.52,doorWidth-.04,.87,.65,premium?s.wood:0xd2d6c9);b(x,1.89,-5.21,doorWidth-.12,.025,.06,0,glow);}}
  if(premium){b(HOME_ALIGNMENT.fridgeX,2.65,HOME_ALIGNMENT.fridgeZ,1.23,.24,1.2,s.wood);b(k.x,1.185,k.z,.36,.25,.30,0x333f3d);}
  g=shell;
  // Storage remains at its original accessible interaction point.
  section('Storage');
  b(295,.72,2.3,1.8,1.3,.75,s.wood);for(const x of [294.58,295.42]){b(x,.72,2.69,.78,1.15,.025,modern?s.fabric:s.accent);b(x+.23,.9,2.72,.035,.18,.04,0xc3b99e);}
  if(!basic){b(295,1.87,1.95,1.78,1.8,.06,s.wood);plant(294.5,1.4,2.3,.32);art(295,2.18,2.01,1.45,.85);}
  place(295,2.3,0,HOME_ALIGNMENT.storageZ-2.3,Math.PI);
  section('Wardrobe');
  const wardrobe=f('wardrobe'),wx=wardrobe.x,wz=wardrobe.z;
  b(wx,1.21,wz,1.7,2.28,.72,s.wood);
  for(const dx of [-.42,.42]){b(wx+dx,1.23,wz-.375,.80,2.17,.035,s.accent);b(wx+dx*.25,1.16,wz-.41,.035,.26,.04,0xc3b99e);}
  g=shell;
  // Distinct living arrangements within the established table/lounge footprints.
  section('Living table');
  const table=f('table'),tableY=modern?.48:.79;b(table.x,tableY,1,table.w*2,.09,1.18,premium?0xc5bdad:s.wood);for(const x of [table.x-table.w+.12,table.x+table.w-.12])for(const z of [.53,1.47])leg(x,z,tableY-.11,premium?0x625d50:s.wood);if(modern){b(table.x-.3,tableY+.07,.9,.48,.04,.34,s.accent);cylinder(g,table.x+.4,tableY+.13,1.1,.09,.16,0xd7d4c4);}
  place(table.x,1,0,table.z-1);
  if(basic){b(298,.49,3,1.35,.14,.91,0x647778);b(298,.88,3.43,1.35,.72,.09,0x647778);for(const x of [297.42,298.58])for(const z of [2.62,3.35])leg(x,z,.38,0x535b4e);b(298,.865,1.5,.45,.06,.30,0x9f7658);}
  else{
   const sx=298;b(sx,.30,3,2.9,.44,.92,s.fabric);b(sx,.79,3.43,2.9,.67,.10,s.fabric);
   for(const x of [296.58,299.42])b(x,.56,3,.13,.53,.95,premium?0xc9bba2:s.fabric);
   for(const x of [297.1,298,298.9]){b(x,.54,2.92,.84,.13,.73,premium?0xe6dac2:s.fabric);b(x,.79,3.26,.67,.35,.12,id==='room'?0xb79570:s.accent);}
   b(298,.079,2,3.6,.018,3.2,id==='room'?0xa49278:premium?0x8a9388:0xb5baa8);
   if(id==='room'){for(let i=0;i<4;i++){b(295.3,1.98+i*.23,-5.72,1.1,.06,.3,s.wood);for(let j=0;j<3;j++)b(294.98+j*.16,2.11+i*.23,-5.72,.1,.20,.19,[0x617b79,0xba9a6f,0x99786e][j]);}art(300,2.25,-5.8,1.25,.95);}
  }
  if(modern){
   section('Work / dining');
   const desk=f('desk');b(302,.82,1.8,2.6,.11,1.18,premium?0xc7beaa:s.wood);for(const x of [300.84,303.16])for(const z of [1.32,2.28])leg(x,z,.7,0x525f5b);
   b(302,.50,2.95,.68,.14,.65,s.fabric);b(302,.81,3.25,.68,.60,.06,s.fabric);for(const x of [301.72,302.28])for(const z of [2.68,3.22])leg(x,z,.37,0x52615a);
   if(premium){for(const x of [301.3,302.7]){cylinder(g,x,.9,1.8,.21,.025,0xe7e0cf);cylinder(g,x+.28,1.02,1.8,.045,.20,0xb5c4b7);}plant(302,.88,1.8,.35);art(300,2.14,-5.8,1.65,1.4);}
   else{b(302,1.20,1.42,1.05,.57,.065,0x30433f);b(302,.915,1.42,.07,.08,.06,0x52615a);b(302,.89,1.42,.35,.03,.2,0x52615a);b(302,.91,1.75,.94,.035,.4,0x768983);plant(303,.88,1.9,.25);}
   if(premium)g=shell;else place(302,1.8,4.2,-2.45,-Math.PI/2);
  }
  // Tiled shower recess, tray, framed screen, drain, mixer and towel rail.
  section('Shower');
  b(305.84,.12,2,1.5,.10,1.5,0xd8dcd1);b(305.84,.18,2,1.34,.025,1.34,0xbccbc6);b(305.84,.2,2,.14,.015,.14,0x647572);
  for(const x of [305.09,306.59]){for(const z of [1.25,2.75])b(x,1.55,z,.055,2.8,.055,modern?0x758d8a:0xc5c3b4);for(const y of [.3,2.9])b(x,y,2,.055,.045,1.50,0x758d8a);b(x,1.6,2,.025,2.4,1.40,0,glass);}
  b(305.84,1.5,1.24,1.50,2.7,.065,modern?0xaebbb3:0xc4c1ad);
  for(let y=.4;y<2.8;y+=.4)b(305.84,y,1.28,1.43,.018,.018,0x8b9c95);
  for(const x of [305.4,305.9,306.4])b(x,1.55,1.28,.012,2.6,.018,0x8b9c95);
  cylinder(g,306.2,1.98,1.4,.025,1.4,0xaab8b3);b(306.2,2.67,1.62,.045,.045,.48,0xaab8b3);cylinder(g,306.2,2.62,1.85,.16,.06,0xaab8b3);b(306.2,1.30,1.38,.24,.09,.15,0xaab8b3);
  b(306.48,1.25,2.5,.06,.045,.46,0xc8cec2);b(306.43,1.03,2.5,.08,.45,.35,s.fabric);
  place(305.84,2,HOME_ALIGNMENT.showerShiftX,0);
  // Practical fixtures: shared two-light budget, emissive shades/coves add layering.
  if(basic){cylinder(g,300,3.29,0,.30,.12,0xbdb8a3);cylinder(g,300,3.21,0,.25,.06,0xf4d7a4);for(let i=0;i<5;i++)b(304.5+i*.17,.13,-5.83,.09,.015,.018,0x928873);}
  else if(premium){for(const z of [-5.55,5.55])b(300,3.335,z,13.2,.03,.06,0,glow);for(const x of [293.45,306.55])b(x,3.335,0,.06,.03,11.1,0,glow);for(const x of [298,302]){cylinder(g,x,3.09,x===298?1.1:1.8,.02,.52,0x9c8251);cylinder(g,x,2.83,x===298?1.1:1.8,.28,.09,0x9c8251);b(x,2.77,x===298?1.1:1.8,.35,.018,.2,0,glow);}}
  else{for(const z of [-2,2]){b(300,3.315,z,1.35,.07,.45,0x8c968a);b(300,3.265,z,1.2,.025,.32,0,glow);}}
  world.batchStaticGroup(g);g.visible=false;
 }
 return variants;
}
