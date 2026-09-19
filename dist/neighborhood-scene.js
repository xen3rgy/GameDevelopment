import * as THREE from './vendor/three.module.js';
import {NEIGHBORHOOD_FIXTURES} from './neighborhood-layout.js?v=0.7.2';
import {groundHeight} from './spatial.js?v=0.7.2-stationstep1';

export function buildNeighborhoodDetails(world,kit){
 const {box,cylinder,sphere,sign}=kit,g=new THREE.Group();g.name='Courtyards and street life';world.scene.add(g);world.staticGroups.push(g);
 // Shared geometries/materials join the existing static instance batches.
 const metal=0x354f50,wood=0x9e8058,stone=0x9baba2;
 const rod=(p,a,b,r,color)=>{const from=new THREE.Vector3(...a),to=new THREE.Vector3(...b),delta=to.clone().sub(from),mid=from.clone().add(to).multiplyScalar(.5);const m=cylinder(p,mid.x,mid.y,mid.z,r,delta.length(),color);m.quaternion.setFromUnitVectors(new THREE.Vector3(0,1,0),delta.normalize());return m;};
 const tireGeometry=new THREE.TorusGeometry(.32,.035,5,16),tireMaterial=kit.mat(0x303b3c),rimGeometry=new THREE.TorusGeometry(.28,.012,4,16),rimMaterial=kit.mat(0x9ba9a4);
 for(const [i,p] of NEIGHBORHOOD_FIXTURES.entries()){
  const root=new THREE.Group();root.position.set(p.x,groundHeight(p.x,p.z),p.z);root.name='Street detail · '+p.kind;root.userData.fixture=p;g.add(root);world.colliders.push({...p,h:p.h+root.position.y});
  const old=p.x<0;
  if(p.kind==='garden'||p.kind==='treeBed'){
   const height=p.kind==='treeBed'?.38:.48;
   // Thin retaining walls, exposed earth and mixed planting instead of solid green cubes.
   for(const side of [-1,1]){box(root,side*(p.w-.07),height/2,0,.14,height,p.d*2,old?0x826e55:stone);box(root,0,height/2,side*(p.d-.07),p.w*2,height,.14,old?0x826e55:stone);}
   box(root,0,height-.07,0,p.w*2-.28,.12,p.d*2-.28,0x575140);
   for(let n=0;n<(p.kind==='treeBed'?10:14);n++){
    const x=Math.sin(n*2.399)*p.w*.77,z=Math.cos(n*2.399)*p.d*.69;
    const shrub=sphere(root,x,height+.14,z,.27,[0x587154,0x6f8458,0x83915b][n%3]);shrub.scale.y*=.8;
    if(n%3===0){rod(root,[x,height,z],[x,height+.44,z],.013,0x57715a);sphere(root,x,height+.44,z,.08,old?0xc8b993:[0xd2b18a,0xb49b98,0xcbbf83][n%3]);}
   }
   if(p.kind==='treeBed'){
    cylinder(root,0,2,0,.15,4,0x6a5743);for(let n=0;n<4;n++)sphere(root,Math.sin(n*2.4)*.5,3.8+n*.23,Math.cos(n*2.4)*.55,1.05,[0x526e4c,0x657c52][n%2]);
    for(const x of [-.55,.55]){cylinder(root,x,1,0,.045,2,wood);box(root,x/2,1.7,0,.65,.04,.035,metal);}
   }
  }else if(p.kind==='seat'){
   for(const x of [-1.08,1.08]){box(root,x,.29,0,.10,.58,.74,metal);box(root,x,.75,-.36,.065,.65,.065,metal);}
   for(let n=0;n<5;n++)box(root,0,.52,-.31+n*.15,2.9,.07,.11,wood);
   for(let n=0;n<3;n++)box(root,0,.71+n*.13,-.4,2.9,.09,.065,wood);
   for(const x of [-1.25,1.25]){box(root,x,.67,.1,.06,.28,.06,metal);box(root,x,.81,0,.08,.06,.52,metal);}
  }else if(p.kind==='bin'){
   box(root,0,.49,0,.60,.98,.60,metal);box(root,0,1,0,.67,.06,.67,0x71857d);
   box(root,0,.78,.307,.38,.15,.015,0x1a2d30);box(root,0,.48,.316,.14,.20,.012,0xb6c6b6);
   for(let n=0;n<5;n++)box(root,-.25+n*.125,.39,.317,.016,.64,.018,0x75887d);
  }else if(p.kind==='cycles'){
   // Two locked city bicycles; open frames and spokes make them legible at street scale.
   for(const z of [-.48,.48]){
    for(const x of [-.62,.62])cylinder(root,x,.43,z,.032,.86,metal);box(root,0,.85,z,1.24,.064,.064,metal);
    const bike=new THREE.Group();bike.position.z=z+.12;root.add(bike);
    for(const x of [-.74,.74]){
     for(const [geo,mat] of [[tireGeometry,tireMaterial],[rimGeometry,rimMaterial]]){const m=new THREE.Mesh(geo,mat);m.position.set(x,.34,0);bike.add(m);}
     for(let n=0;n<6;n++){const a=n*Math.PI/3;rod(bike,[x,.34,0],[x+Math.cos(a)*.28,.34+Math.sin(a)*.28,0],.005,0xb0bab0);}
    }
    const color=old?0x887356:0x5c8580,points={rear:[-.74,.34,0],pedal:[-.07,.33,0],seat:[-.28,.83,0],head:[.47,.82,0],front:[.74,.34,0]};
    for(const [a,b] of [['rear','seat'],['rear','pedal'],['pedal','seat'],['seat','head'],['pedal','head'],['head','front']])rod(bike,points[a],points[b],.026,color);
    rod(bike,points.seat,[-.3,.99,0],.024,metal);box(bike,-.33,1.01,0,.26,.065,.15,0x39403a);
    rod(bike,points.head,[.45,1.12,0],.024,metal);rod(bike,[.45,1.12,-.18],[.45,1.12,.18],.023,metal);
    rod(bike,[-.09,.33,-.14],[-.09,.33,.14],.025,metal);box(bike,-.07,.33,.18,.16,.04,.09,0x2c393a);
   }
  }else if(p.kind==='notice'){
   for(const x of [-.95,.95])box(root,x,1.18,0,.08,2.36,.08,metal);
   box(root,0,1.66,0,2.1,1.5,.12,wood);box(root,0,1.66,.068,1.94,1.34,.016,0x445c55);
   for(let n=0;n<4;n++){box(root,-.67+n*.44,1.57+(n%2)*.09,.085,.36,.67,.012,[0xd4c6a3,0xb0c4b3,0xc5b29a][n%3]);for(let l=0;l<4;l++)box(root,-.67+n*.44,1.73+(n%2)*.09-l*.09,.095,.25,.014,.008,0x738076);}
   sign(root,'NEUES AUS DEM KIEZ',0,2.24,.088,1.9,.18,'#ead9b8','#445c55');
  }
 }
 // Small service details belong against walls, inside existing building footprints.
 for(const [x,z,face] of [[-74,14,-1],[-35,14,-1],[-70,-14,1],[-126,47.5,1]]){
  const root=new THREE.Group();root.position.set(x,0,z);root.rotation.y=face===1?0:Math.PI;g.add(root);
  for(let n=0;n<3;n++){box(root,3+n*.35,1.3,.13,.28,.4,.18,metal);box(root,3+n*.35,1.42,.226,.2,.018,.013,0x1c3031);}
  box(root,0,.7,.05,1.15,.58,.06,0x50625e);for(let n=0;n<6;n++)box(root,0,.47+n*.085,.09,1,.025,.035,0x2d4241);
 }
 return g;
}
