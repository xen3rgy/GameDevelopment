import * as THREE from './vendor/three.module.js';
import {createCitizen,applyCitizenOutfit} from './citizen.js?v=0.8.1';
import {clothingById,defaultWardrobe,CLOTHING_FIXTURES} from './clothing.js?v=0.8.1';

// Merchandise and previews share the exact player's cached geometry/materials.
let sourceActor;
function source(){return sourceActor??=createCitizen({});}
export function clothingDisplay(id){
 const actor=source(),g=new THREE.Group();actor.updateMatrixWorld(true);
 for(const part of actor.userData.clothingModules.get(id)||[])part.traverse(o=>{if(!o.isMesh)return;const m=new THREE.Mesh(o.geometry,o.material);m.matrix.copy(o.matrixWorld);m.matrixAutoUpdate=false;m.castShadow=m.receiveShadow=true;m.userData.staticBatch=true;g.add(m);});
 const bounds=new THREE.Box3().setFromObject(g),center=bounds.getCenter(new THREE.Vector3());
 // Bake only transforms, retaining shared immutable geometry.
 for(const child of g.children){child.matrix.elements[12]-=center.x;child.matrix.elements[13]-=bounds.min.y;child.matrix.elements[14]-=center.z;}
 return g;
}

let preview;
const cards=new Map(),outfits=new Map();
function previewScene(){
 if(preview)return preview;
 const renderer=new THREE.WebGLRenderer({antialias:true,preserveDrawingBuffer:true,alpha:false,powerPreference:'low-power'});
 renderer.setPixelRatio(1);renderer.toneMapping=THREE.ACESFilmicToneMapping;renderer.toneMappingExposure=1.2;
 const scene=new THREE.Scene();scene.background=new THREE.Color(0xdedbd1);
 scene.add(new THREE.HemisphereLight(0xffffff,0x8d9188,2.5));
 const light=new THREE.DirectionalLight(0xfff3df,3);light.position.set(3,5,4);scene.add(light);
 const fill=new THREE.DirectionalLight(0xc7deeb,1.5);fill.position.set(-3,2,-3);scene.add(fill);
 const actor=source();scene.add(actor);
 preview={renderer,scene,actor,camera:new THREE.OrthographicCamera(-1,1,1,-1,.01,30)};return preview;
}
function capture(target,width,height,angle,fullBody=false){
 const p=previewScene(),bounds=new THREE.Box3().setFromObject(target),center=fullBody?new THREE.Vector3(0,.9,0):bounds.getCenter(new THREE.Vector3()),size=bounds.getSize(new THREE.Vector3());
 const span=fullBody?2.08:Math.max(size.y,Math.hypot(size.x,size.z)*height/width)*1.25;
 p.camera.left=-span*width/height/2;p.camera.right=-p.camera.left;p.camera.top=span/2;p.camera.bottom=-span/2;
 p.camera.position.copy(center).add(new THREE.Vector3(Math.sin(angle)*5,fullBody?.12:.65,Math.cos(angle)*5));p.camera.lookAt(center);p.camera.updateProjectionMatrix();
 p.renderer.setSize(width,height,false);p.renderer.render(p.scene,p.camera);return p.renderer.domElement.toDataURL('image/png');
}
export function clothingThumbnail(id){
 if(cards.has(id))return cards.get(id);
 const p=previewScene(),item=clothingById(id);if(!item)return '';
 p.actor.visible=false;const display=clothingDisplay(id);p.scene.add(display);
 try{const url=capture(display,280,260,item.slot==='backpack'?Math.PI+.45:.4);cards.set(id,url);return url;}
 finally{p.scene.remove(display);p.actor.visible=true;}
}
export function outfitThumbnail(outfit,back=false){
 const key=JSON.stringify([outfit,back]);if(outfits.has(key))return outfits.get(key);
 const p=previewScene();p.actor.visible=true;applyCitizenOutfit(p.actor,outfit);p.actor.userData.backpack.visible=true;
 const url=capture(p.actor,420,560,back?Math.PI+.35:.35,true);outfits.set(key,url);
 if(outfits.size>32)outfits.delete(outfits.keys().next().value);return url;
}

function merchandise(parent,id,x,y,z,scale=1,rotation=0){const g=clothingDisplay(id);g.position.set(x,y,z);g.rotation.y=rotation;g.scale.setScalar(scale);parent.add(g);return g;}
export function decorateClothingStore(world,k){
 const g=new THREE.Group();g.name='STADTSTOFF · window displays';g.userData.exterior=true;world.scene.add(g);world.staticGroups.push(g);
 const {box,sign}=k;
 // Shallow window cases stay within the building's existing facade envelope.
 for(const x of [89.2,98.8]){
  box(g,x,1.8,-14.82,4.6,2.6,.18,0x183b3b);box(g,x,.5,-14.63,4.5,.25,.45,0xc8b59a);
  for(const dx of [-2.3,2.3])box(g,x+dx,1.8,-14.52,.065,2.7,.065,0xb79e75);
  const garment=merchandise(g,x<94?'hoodie':'jacket',x-.7,.65,-14.4,2);
  const top=new THREE.Box3().setFromObject(garment).max.y;box(g,x-.7,(top+3.05)/2,-14.4,.025,3.05-top,.025,0xb79e75);
  merchandise(g,x<94?'boots':'city-pack',x+1,.65,-14.42,1.8,x<94?0:Math.PI);
 }
 sign(g,'STADTSTOFF',94,4.8,-14.60,13,.95,'#efd7ad','#183b3b');
 sign(g,'KLEIDUNG · DEIN ALLTAG. DEIN STIL.',94,3.85,-14.59,11,.30,'#eadcc6','#183b3b');
}

export function buildClothingStore(world,k){
 const {box,cylinder,sign,mat}=k,g=new THREE.Group();g.name='STADTSTOFF · interior';world.scene.add(g);world.clothingStore=g;
 const b=(x,y,z,w,h,d,c,m)=>box(g,x,y,z,w,h,d,c,m),cream=0xd8d4c8,oak=0xa58a63,green=0x294943,steel=0x52615b;
 b(500,-.08,0,12,.30,12,0xb5ac98);b(500,3.67,0,12,.24,12,cream);
 for(const x of [494,506])b(x,1.8,0,.22,3.6,12,cream);
 b(500,1.8,-6,12,3.6,.22,cream);for(const x of [496.5,503.5])b(x,1.8,6,5,3.6,.22,cream);
 b(500,3.2,6,2,.8,.22,cream);world.exitDoor(g,500,5.85,'STADTSTOFF');
 for(const x of [494.18,505.82])b(x,.18,0,.06,.22,11.8,oak);
 const glow=new THREE.MeshBasicMaterial({color:0xffedd1});
 for(const z of [-3,2]){b(500,3.48,z,7,.045,.12,0,glow);const light=new THREE.PointLight(0xffedcf,42,15,2);light.position.set(500,3.25,z);g.add(light);}
 const fixture=id=>CLOTHING_FIXTURES.find(f=>f.id===id);
 function wallRack(id,items,label,rotation){
  const f=fixture(id),rack=new THREE.Group();rack.position.set(f.x,0,f.z);rack.rotation.y=rotation;g.add(rack);
  box(rack,0,.19,0,2.3,.24,.8,oak);box(rack,0,1.37,-.34,2.3,2.3,.08,green);
  for(const x of [-1.05,1.05])cylinder(rack,x,1.24,.13,.025,2.12,steel);
  box(rack,0,2.26,.13,2.16,.04,.04,steel);
  for(const [i,item] of items.entries()){const x=(i-(items.length-1)/2)*.70;const garment=merchandise(rack,item,x,0,.13,1);garment.position.y=1.97-new THREE.Box3().setFromObject(garment).getSize(new THREE.Vector3()).y;box(rack,x,2.11,.13,.018,.27,.018,steel);const hanger=box(rack,x,1.94,.13,.44,.025,.025,steel);hanger.rotation.z=.10;}
  sign(rack,label,0,2.62,.16,2.25,.25,'#294943','#e3dccb');
 }
 wallRack('tops',['tee','hoodie','sweatshirt'],'OBERTEILE',Math.PI/2);
 wallRack('outer',['overshirt','jacket','bomber'],'JACKEN',Math.PI/2);
 // Back-wall trouser display and folded stacks below it.
 wallRack('pants',['jeans','chinos','workpants'],'HOSEN',0);
 for(let i=0;i<3;i++)for(let n=0;n<3;n++)b(499.2+i*.8,.40+n*.085,-5.1,.60,.07,.38,[0x394b5b,0xa39777,0x55594f][i]);
 function shelves(id,label,items){const f=fixture(id),rack=new THREE.Group();rack.position.set(f.x,0,f.z);rack.rotation.y=-Math.PI/2;g.add(rack);
  box(rack,0,1.30,-.34,2.3,2.4,.08,green);
  for(const [row,y] of [.5,1.3].entries()){box(rack,0,y,.02,2.25,.06,.75,oak);for(let i=0;i<items.length;i++)merchandise(rack,items[i],(i-(items.length-1)/2)*.72,y+.04,.03,id==='backpacks'?.9:1.25,id==='backpacks'?Math.PI:0);}
  sign(rack,label,0,2.62,.14,2.25,.25,'#294943','#e3dccb');
 }
 shelves('shoes','SCHUHE',['sneakers','clean-sneakers','boots']);shelves('backpacks','RUCKSÄCKE',['olive-pack','city-pack','grey-pack']);
 const c=fixture('counter');b(c.x,.61,c.z,2.5,1.08,1.08,green);b(c.x,1.19,c.z,2.5,.06,1.08,oak);b(c.x+.65,1.4,c.z,.32,.36,.06,steel);b(c.x+.65,1.43,c.z-.04,.26,.22,.02,0x8fa99a);
 sign(g,'STADTSTOFF',c.x,.8,c.z-.56,2,.28,'#efdfbe','#294943');
 // Mirror is a cheap tinted pane, not a second live reflection render.
 b(497.2,1.43,-5.83,1.4,2.5,.07,oak);b(497.2,1.43,-5.78,1.23,2.33,.025,0,mat(0xa2bdbe,.15,.55));
 const f=fixture('fitting');b(f.x,1.37,f.z,1.6,2.6,1.9,0xb8aaa0);for(let i=0;i<10;i++)b(f.x-.72+i*.16,1.37,f.z-1,.13,2.6,.08,i%2?0x8b9a90:0x9dab9e);
 sign(g,'ANPROBE',f.x,2.85,f.z-1.03,1.5,.22,'#294943','#e3dccb',Math.PI);
 g.userData.fixtureFootprints=CLOTHING_FIXTURES;g.visible=false;
 // Restore the reusable preview actor after geometry extraction.
 applyCitizenOutfit(source(),defaultWardrobe().equipped);
 return g;
}
