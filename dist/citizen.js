import * as THREE from './vendor/three.module.js';

// All dimensions are in the existing rig's metres. Never move the IK joints.
const geometries=new Map(),materials=new Map();
const roles=['commuter','student','worker','employee','older','sporty','stylish','casual'];
const hash=(id,salt)=>{let n=Math.imul((Number(id)||0)^salt,0x45d9f3b);n=Math.imul(n^(n>>>16),0x45d9f3b);return (n^(n>>>16))>>>0;};
export function citizenAppearance(id){
 const pick=(salt,values)=>values[hash(id,salt)%values.length],archetype=roles[((Number(id)||0)%8+8)%8];
 return {archetype,color:pick(31,[0x354753,0x65705a,0x756457,0x8c7560,0x454954,0x694449,0x566974]),
  skin:pick(117,[0xc69b7d,0x98694f,0xd8b297,0x684637,0xb88061,0xe2bfa6]),
  trousers:pick(263,[0x35424e,0x535653,0x4c433a,0x293239]),hairColor:archetype==='older'?pick(81,[0x928d81,0x626361,0xb1aca0]):pick(81,[0x302923,0x634632,0x998260,0x242524,0x79503a]),
  hair:pick(501,['textured','messy','part','buzz','bun','ponytail','long','cap','beanie']),
  accent:pick(77,[0xa99a7d,0x67716a,0x875d4d,0x798792]),shoes:pick(99,[0x353b3d,0xa69f90,0x574b3f]),
  build:pick(178,[.91,.97,1.03,1.1]),shoulders:pick(342,[.92,1,1.06]),jaw:pick(29,[.87,.96,1.06]),
  torso:pick(684,[.96,1,1.04]),feminine:!!(hash(id,903)%2),bag:archetype==='commuter',backpack:archetype==='student'};
}
function material(color,kind='cloth'){
 const roughness={skin:.72,denim:.94,cloth:.91,jacket:.78,leather:.65,shoe:.82,hair:.96,trim:.74}[kind],key=color+':'+kind;
 if(!materials.has(key))materials.set(key,new THREE.MeshStandardMaterial({color,roughness,metalness:0}));return materials.get(key);
}
function geometry(key,make){if(!geometries.has(key))geometries.set(key,make());return geometries.get(key);}
// Elliptical cross sections give a chest, waist, jaw and tapered limbs instead of capsules.
function profile(key,rings,segments=16){return geometry(key,()=>{
 const p=[],uv=[],indices=[];
 for(let j=0;j<rings.length;j++){const [y,rx,rz,z=0]=rings[j];for(let i=0;i<=segments;i++){const a=i/segments*Math.PI*2;p.push(Math.sin(a)*rx,y,Math.cos(a)*rz+z);uv.push(i/segments,j/(rings.length-1));}}
 for(let j=0;j<rings.length-1;j++)for(let i=0;i<segments;i++){const a=j*(segments+1)+i,b=a+segments+1;if(rings[0][0]<rings.at(-1)[0])indices.push(a,a+1,b,b,a+1,b+1);else indices.push(a,b,a+1,b,b+1,a+1);}
 for(const j of [0,rings.length-1]){const center=p.length/3,[y,,,z=0]=rings[j];p.push(0,y,z);uv.push(.5,.5);const start=j*(segments+1),up=(j===0)===(rings[0][0]>rings.at(-1)[0]);for(let i=0;i<segments;i++)if(up)indices.push(center,start+i,start+i+1);else indices.push(center,start+i+1,start+i);}
 const g=new THREE.BufferGeometry();g.setAttribute('position',new THREE.Float32BufferAttribute(p,3));g.setAttribute('uv',new THREE.Float32BufferAttribute(uv,2));g.setIndex(indices);g.computeVertexNormals();return g;
});}
function mesh(parent,geo,mat,x,y,z,sx=1,sy=1,sz=1){const m=new THREE.Mesh(geo,mat);m.position.set(x,y,z);m.scale.set(sx,sy,sz);m.castShadow=m.receiveShadow=true;parent.add(m);return m;}
const ball=(p,m,x,y,z,sx,sy,sz)=>mesh(p,geometry('ellipsoid',()=>new THREE.SphereGeometry(1,10,8)),m,x,y,z,sx,sy,sz);
const joint=(p,m,y,rx,ry,rz)=>mesh(p,geometry('joint',()=>new THREE.SphereGeometry(1,8,6)),m,0,y,0,rx,ry,rz);
const box=(p,m,x,y,z,w,h,d)=>mesh(p,geometry('box',()=>new THREE.BoxGeometry(1,1,1)),m,x,y,z,w,h,d);
function softBox(p,m,x,y,z,w,h,d){const geo=geometry('soft-box',()=>{
 const s=new THREE.Shape();s.moveTo(-.35,-.5);s.lineTo(.35,-.5);s.quadraticCurveTo(.5,-.5,.5,-.35);s.lineTo(.5,.35);s.quadraticCurveTo(.5,.5,.35,.5);s.lineTo(-.35,.5);s.quadraticCurveTo(-.5,.5,-.5,.35);s.lineTo(-.5,-.35);s.quadraticCurveTo(-.5,-.5,-.35,-.5);
 const g=new THREE.ExtrudeGeometry(s,{depth:.6,bevelEnabled:true,bevelThickness:.1,bevelSize:.065,bevelSegments:2,curveSegments:3,steps:1});g.center();g.computeBoundingBox();const size=g.boundingBox.getSize(new THREE.Vector3());g.scale(1/size.x,1/size.y,1/size.z);return g;
});return mesh(p,geo,m,x,y,z,w,h,d);}
function strap(p,m,a,b,width,depth){const v=new THREE.Vector3(...b).sub(new THREE.Vector3(...a)),o=box(p,m,(a[0]+b[0])/2,(a[1]+b[1])/2,(a[2]+b[2])/2,width,v.length(),depth);o.quaternion.setFromUnitVectors(new THREE.Vector3(0,1,0),v.normalize());return o;}

export function createCitizen(kit,color=0x303e4b,skin=0xc39c7b,variant=0,style=null){
 const hero=variant===0&&!style,look={...citizenAppearance(variant),...style};
 if(hero)Object.assign(look,{archetype:'student',hair:'messy',hairColor:0x3e3027,trousers:0x394b5b,accent:0x77745c,shoes:0xb9b4a6,build:1,shoulders:1,jaw:.98,torso:1,feminine:false,bag:false,backpack:true});
 const role=look.archetype,coat=look.coat||['commuter','older','stylish'].includes(role),hood=['student','casual'].includes(role),worker=role==='worker',sport=role==='sporty',apron=look.apron||role==='employee';
 const cloth=material(color,'jacket'),pants=material(look.trousers,'denim'),skinMat=material(skin,'skin'),hair=material(look.hairColor,'hair'),accent=material(look.accent,'cloth'),lining=material(hero?0x46586a:look.accent,'cloth'),dark=material(0x283039,'trim'),sole=material(worker?0x343735:0xb9b6aa,'shoe'),shoe=material(look.shoes,'shoe'),bagMat=material(look.accent,'leather');
 const g=new THREE.Group(),upper=new THREE.Group(),backpack=new THREE.Group(),headRoot=new THREE.Group(),details=new THREE.Group(),arms=[],elbows=[],legs=[],knees=[],feet=[];
 g.name=hero?'Player · layered streetwear':`Citizen · ${role}`;g.add(upper);upper.add(backpack,headRoot);headRoot.add(details);headRoot.position.set(0,1.65,0);
 const width=(look.build||1)*(look.shoulders||1),depth=look.feminine?.92:1;
 mesh(upper,profile(look.feminine?'torso-fitted':'torso',[[.89,.13,.09],[.94,.177,.12],[1.10,look.feminine?.148:.169,.117],[1.28,.21,.137],[1.37,.224,.114],[1.43,.12,.075],[1.44,.075,.06]]),cloth,0,1.44*(1-look.torso),0,width,look.torso,depth);
 mesh(upper,profile('hips',[[.83,.15,.087],[.87,.182,.11],[.97,.168,.105]]),pants,0,0,0,look.build,1,1);
 ball(upper,skinMat,0,1.47,0,.065,.103,.062);
 // Visible shirt, open jacket edges and folded lapels follow the chest surface.
 mesh(upper,profile('shirt',[[.966,.063,.026],[1.1,.065,.03],[1.28,.068,.034],[1.38,.061,.015]]),lining,0,0,.113);
 box(upper,material(0xb5b5a9),0,1.394,.094,.12,.035,.018);
 for(const side of [-1,1]){
  strap(upper,cloth,[side*.10,1.39,.126],[side*.081,1.01,.151],.041,.028);
  const lapel=box(upper,hood?lining:cloth,side*.098,1.36,.144,.067,.16,.026);lapel.rotation.z=side*.28;
  strap(upper,dark,[side*.11,1.10,.143],[side*.17,1.16,.13],.009,.009);
 }
 box(upper,dark,0,.94,.12,.30,.025,.013);
 if(hood){ball(upper,lining,0,1.407,-.091,.163,.085,.118);ball(upper,dark,0,1.46,-.066,.107,.02,.065);for(const s of [-1,1])strap(upper,lining,[s*.055,1.4,.13],[s*.061,1.24,.159],.009,.009);}
 if(worker){for(const s of [-1,1]){box(upper,cloth,s*.119,1.29,.135,.092,.096,.018);box(upper,accent,s*.119,1.331,.149,.092,.012,.009);}box(upper,accent,0,1.0,.151,.014,.4,.01);}
 if(sport)for(const s of [-1,1])strap(upper,accent,[s*.175,1.31,.096],[s*.143,1.02,.10],.018,.014);
 if(role==='older'){ball(upper,accent,0,1.435,.008,.105,.046,.094);box(upper,accent,.066,1.30,.16,.071,.26,.024);}
 if(apron){box(upper,dark,0,1.14,.164,.255,.36,.018);for(const s of [-1,1])strap(upper,dark,[s*.093,1.32,.174],[s*.064,1.43,.088],.024,.015);box(upper,accent,0,1.03,.179,.17,.08,.012);}
 // Sculpted skull/jaw: narrow chin, cheekbones and a flattened face plane.
 mesh(headRoot,profile('head',[[ -.151,.036,.043,.035],[-.133,.071,.067,.021],[-.084,.101,.085,.012],[-.015,.116,.1,.007],[.055,.119,.099],[.117,.107,.084,-.006],[.15,.073,.06,-.009],[.164,.001,.001,-.01]],16),skinMat,0,0,0,look.jaw,1,1);
 for(const s of [-1,1]){
  ball(headRoot,skinMat,s*.117*look.jaw,-.017,0,.023,.038,.025);
  ball(details,skinMat,s*.046,.017,.089,.031,.012,.017);
  box(details,material(0x494139,'hair'),s*.046,.018,.107,.023,.006,.005);
  const brow=box(details,hair,s*.047,.042,.098,.039,.008,.008);brow.rotation.z=s*.10;
 }
 ball(headRoot,skinMat,0,-.021,.105,.018,.034,.022);ball(headRoot,skinMat,0,-.042,.114,.022,.012,.019);
 box(details,material(0x825c50,'skin'),0,-.084,.091,.039,.004,.005);
 // Hair is anchored entirely under headRoot, including long/tied styles.
 const hairStyle=look.hair==='short'?'textured':look.hair;
 mesh(headRoot,profile('hair-shell',[[.058,.125,.108,-.007],[.10,.126,.108,-.007],[.15,.107,.088,-.01],[.178,.054,.045,-.01],[.182,.001,.001,-.01]],16),hair,0,0,0,look.jaw,1,1);
 if(['textured','messy','part'].includes(hairStyle))for(let i=0;i<(hero?7:4);i++){const x=(i%4-1.5)*.045,y=.145+(i%3)*.009,z=i<4?.059:-.033;const t=ball(headRoot,hair,x,y,z,.043,hairStyle==='messy'?.028:.018,.066);t.rotation.z=-.4+i*.1;t.rotation.y=.4;t.rotation.x=.24;}
 if(['long','bun','ponytail'].includes(hairStyle)){ball(headRoot,hair,0,.013,-.076,.118,.13,.057);if(hairStyle==='long')for(const s of [-1,1])ball(headRoot,hair,s*.098,-.106,-.035,.043,.135,.062);else ball(headRoot,hair,0,hairStyle==='bun'?.09:-.068,-.144,.061,hairStyle==='bun'?.063:.135,.059);}
 if(['cap','beanie'].includes(hairStyle)){ball(headRoot,accent,0,.12,-.009,.128,.081,.111);if(hairStyle==='cap')ball(headRoot,accent,0,.107,.112,.12,.013,.093);else mesh(headRoot,profile('beanie-rim',[[.075,.126,.109],[.11,.129,.112]]),accent,0,0,-.009);}
 for(const side of [-1,1]){
  const arm=new THREE.Group(),elbow=new THREE.Group();arm.position.set(side*.26,1.35,0);elbow.position.y=-.29;
  joint(arm,cloth,-.022,.076,.067,.077);joint(elbow,cloth,0,.058,.058,.058);
  mesh(arm,profile('sleeve',[[.024,.049,.053],[-.022,.078,.081],[-.13,.074,.069],[-.26,.061,.061],[-.295,.056,.056]]),cloth,0,0,0,worker?1.07: sport?.9:1,1,1);
  mesh(elbow,profile('forearm',[[.024,.057,.057],[-.045,.062,.059],[-.14,.052,.05],[-.184,.044,.044]]),cloth,0,0,.01);
  mesh(elbow,profile('cuff',[[-.162,.045,.046],[-.197,.045,.046]]),lining,0,0,.01);
  ball(elbow,skinMat,0,-.232,.026,.038,.057,.023);ball(elbow,skinMat,-side*.029,-.221,.04,.015,.03,.017);
  arm.add(elbow);upper.add(arm);arms.push(arm);elbows.push(elbow);
  const leg=new THREE.Group(),knee=new THREE.Group(),foot=new THREE.Group();leg.position.set(side*.115,.9,0);knee.position.y=-.38;foot.position.set(0,-.405,0);
  joint(leg,pants,-.006,.081,.083,.09);joint(knee,pants,0,.065,.065,.068);
  mesh(leg,profile('thigh',[[.031,.079,.088],[-.075,.093,.10],[-.23,.078,.079],[-.385,.064,.066]]),pants,0,0,0,look.feminine?.95:look.build,1,1);
  mesh(knee,profile('calf',[[.022,.066,.068],[-.1,.068,.065],[-.24,.053,.052],[-.37,.049,.05]]),pants,0,0,0,sport?.88:1,1,1);
  if(worker)box(leg,cloth,side*.083,-.175,0,.022,.12,.10);
  // Split coat/apron panels follow the thighs when sitting, cycling or kneeling.
  if(coat){mesh(leg,profile('coat-tail',[[.11,.118,.145],[-.08,.12,.153],[-.24,.121,.15]]),cloth,-side*.014,0,-.008);box(upper,cloth,side*.154,1.12,.131,.09,.12,.022);}
  if(apron)box(leg,dark,-side*.022,-.075,.116,.18,.31,.017);
  ball(foot,sole,0,-.045,.048,.085,.02,.161);ball(foot,shoe,0,-.006,.038,.078,.045,.149);ball(foot,shoe,0,.018,-.015,.062,worker?.083:.052,.073);
  box(foot,lining,0,.044,.063,.056,.011,.096);box(foot,sole,0,.054,.076,.06,.007,.045);
  knee.add(foot);leg.add(knee);g.add(leg);legs.push(leg);knees.push(knee);feet.push(foot);
 }
 if(look.backpack){
  softBox(backpack,bagMat,0,1.185,-.228,.326,.452,.19);softBox(backpack,bagMat,0,1.107,-.329,.245,.205,.065);
  for(const s of [-1,1]){strap(backpack,bagMat,[s*.145,1.405,-.075],[s*.155,1.29,.132],.037,.022);strap(backpack,bagMat,[s*.155,1.29,.132],[s*.13,.997,.129],.033,.017);box(backpack,dark,s*.073,1.154,-.356,.021,.038,.012);strap(backpack,lining,[s*.072,1.39,-.296],[s*.073,1.11,-.356],.019,.01);}
  strap(backpack,bagMat,[-.059,1.402,-.234],[-.045,1.46,-.234],.018,.02);strap(backpack,bagMat,[.059,1.402,-.234],[.045,1.46,-.234],.018,.02);box(backpack,bagMat,0,1.46,-.234,.09,.018,.02);
  box(backpack,dark,0,1.219,-.33,.204,.009,.012);
 }
 if(look.bag){softBox(elbows[0],bagMat,-.044,-.435,.026,.23,.265,.09);for(const x of [-.10,.01])strap(elbows[0],bagMat,[x,-.245,.026],[x,-.35,.026],.015,.019);}
 if(role==='stylish'||sport){strap(upper,bagMat,[-.17,1.39,.14],[.17,.985,.155],.024,.02);ball(upper,bagMat,.181,.99,.108,.068,.10,.054);}
 upper.position.y=.9;for(const child of upper.children)child.position.y-=.9;
 g.userData={arms,legs,upper,elbows,knees,feet,backpack,headRoot,style:look,details};mergeRigidParts(g);return g;
}
// Merge once per rigid joint/material. Articulation and detail visibility stay independent.
function mergeRigidParts(group){
 for(const child of [...group.children])if(child.isGroup)mergeRigidParts(child);
 const buckets=new Map();for(const m of group.children){if(!m.isMesh)continue;if(!buckets.has(m.material))buckets.set(m.material,[]);buckets.get(m.material).push(m);}
 for(const [mat,meshes] of buckets){if(meshes.length<2)continue;const p=[],n=[],uv=[];
  for(const m of meshes){m.updateMatrix();const geo=m.geometry.index?m.geometry.toNonIndexed():m.geometry.clone();geo.applyMatrix4(m.matrix);p.push(...geo.attributes.position.array);n.push(...geo.attributes.normal.array);uv.push(...geo.attributes.uv.array);geo.dispose();group.remove(m);}
  const geo=new THREE.BufferGeometry();geo.setAttribute('position',new THREE.Float32BufferAttribute(p,3));geo.setAttribute('normal',new THREE.Float32BufferAttribute(n,3));geo.setAttribute('uv',new THREE.Float32BufferAttribute(uv,2));mesh(group,geo,mat,0,0,0);
 }
}
