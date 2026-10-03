import test from 'node:test';
import assert from 'node:assert/strict';
import {CLOTHING,CLOTHING_LOCATION,CLOTHING_BUILDING,CLOTHING_POINTS,CLOTHING_FIXTURES,previewOutfit,defaultWardrobe,validateWardrobe,clothingById} from '../dist/clothing.js';
import {GameModel,newGame,validateSave} from '../dist/model.js';
import {HOME_POINTS,SHOP_POINTS,LOCATIONS,BUILDINGS} from '../dist/data.js';
import {HOME_STYLES,homeFixtures} from '../dist/home-layout.js';
import {ROOMS,SHOP_FIXTURES,canWalkRoom} from '../dist/spatial.js';
import {createCitizen,applyCitizenOutfit} from '../dist/citizen.js';
import * as THREE from '../dist/vendor/three.module.js';
const shop=()=>{const m=new GameModel(newGame(true));Object.assign(m.s,{inside:true,interior:'clothing',position:{...CLOTHING_POINTS.clothingCounter}});return m;};
const home=m=>Object.assign(m.s,{home:'room',inside:true,interior:'home',position:{...HOME_POINTS.wardrobe}});
test('default wardrobe is independent, valid and included in both modes',()=>{
 assert.equal(CLOTHING.length,15);assert.equal(new Set(CLOTHING.map(v=>v.id)).size,15);
 for(const mode of [false,true])assert.deepEqual(newGame(mode).wardrobe,defaultWardrobe());
 const w=defaultWardrobe();w.owned.length=0;assert.equal(defaultWardrobe().owned.length,4);
 for(const [slot,id] of Object.entries(defaultWardrobe().equipped))if(id)assert.equal(clothingById(id).slot,slot);
});
test('old saves and current-version constructors receive wardrobe defaults',()=>{
 for(const version of [1,2,3]){const s=newGame();s.version=version;delete s.wardrobe;assert.deepEqual(validateSave(s).wardrobe,defaultWardrobe());assert.deepEqual(new GameModel(s).s.wardrobe,defaultWardrobe());}
});
test('direct purchase deducts exact price without touching inventory, storage or basket',()=>{
 const m=shop();m.s.inventory=[{id:'water',count:1}];m.s.basket=[{id:'coffee',count:1}];
 const before=structuredClone({inventory:m.s.inventory,storage:m.s.storage,basket:m.s.basket}),money=m.s.money,weight=m.weight;
 assert.equal(m.buyClothing('bomber'),true);assert.equal(m.s.money,money-clothingById('bomber').price);assert.ok(m.s.wardrobe.owned.includes('bomber'));
 assert.deepEqual({inventory:m.s.inventory,storage:m.s.storage,basket:m.s.basket},before);assert.equal(m.weight,weight);
 assert.equal(m.buyClothing('bomber'),false);assert.equal(m.s.money,money-clothingById('bomber').price);
});
test('invalid, unaffordable and remote purchases are rejected',()=>{
 const m=shop();for(const id of ['unknown','__proto__',null,{}])assert.equal(m.buyClothing(id),false);
 m.s.money=799;assert.equal(m.buyClothing('tee'),false);assert.equal(m.s.money,799);
 m.s.money=10000;m.s.position={...ROOMS.clothing.spawn};assert.equal(m.buyClothing('tee'),false);
});
test('only owned clothing equips to its declared slot, freely, at home; outer can be removed',()=>{
 const m=shop();m.buyClothing('bomber');m.buyClothing('boots');m.s.position={...ROOMS.clothing.spawn};assert.equal(m.equipClothing('bomber'),false);home(m);
 const money=m.s.money;assert.equal(m.equipClothing('chinos'),false);assert.equal(m.equipClothing('__proto__'),false);
 assert.equal(m.equipClothing('bomber'),true);assert.equal(m.s.wardrobe.equipped.outer,'bomber');assert.equal(m.equipClothing('boots'),true);assert.equal(m.s.wardrobe.equipped.shoes,'boots');
 assert.equal(m.equipClothing(null),true);assert.equal(m.s.wardrobe.equipped.outer,null);assert.equal(m.s.money,money);
});
test('ownership and exact outfit survive JSON save/reload and validation in both modes',()=>{
 for(const mode of ['story','sandbox']){const m=shop();m.s.mode=mode;for(const item of CLOTHING)m.buyClothing(item.id);home(m);for(const id of ['tee','jacket','workpants','boots','grey-pack'])assert.equal(m.equipClothing(id),true);
  const loaded=new GameModel(validateSave(JSON.parse(JSON.stringify(m.s))));assert.deepEqual(loaded.s.wardrobe,m.s.wardrobe);
  m.equipClothing(null);assert.deepEqual(validateSave(JSON.parse(JSON.stringify(m.s))).wardrobe,m.s.wardrobe);
 }
});
test('malformed wardrobe, unknown IDs, wrong slots and unowned selections fall back safely',()=>{
 for(const raw of [null,42,[],{owned:'bad',equipped:{top:'unknown'}},{owned:['bad','__proto__'],equipped:{top:'boots',outer:'bomber',shoes:'boots'}}])assert.deepEqual(validateWardrobe(raw),defaultWardrobe());
 const w=validateWardrobe({owned:['tee','tee',{},null],equipped:{top:'tee'}});assert.equal(w.owned.filter(v=>v==='tee').length,1);assert.equal(w.equipped.top,'tee');
});
function reachable(interior,homeId,point){
 const spawn=ROOMS[interior].spawn,step=.2,queue=[[0,0]],seen=new Set(['0,0']);
 for(let n=0;n<queue.length;n++){const [ix,iz]=queue[n],x=spawn.x+ix*step,z=spawn.z+iz*step;if(Math.hypot(x-point.x,z-point.z)<.22)return true;
  for(const [dx,dz] of [[1,0],[-1,0],[0,1],[0,-1]]){const a=ix+dx,b=iz+dz,key=a+','+b;if(!seen.has(key)&&canWalkRoom(interior,spawn.x+a*step,spawn.z+b*step,.35,homeId)){seen.add(key);queue.push([a,b]);}}
 }return false;
}
test('rack and all four wardrobes have collision, clear footprints and reachable approaches',()=>{
 assert.equal(SHOP_POINTS.clothing,undefined);assert.ok(!SHOP_FIXTURES.some(f=>f.id==='clothing'));
 for(const point of Object.values(CLOTHING_POINTS))assert.ok(reachable('clothing',null,point),point.name);
 for(const rack of CLOTHING_FIXTURES){assert.equal(canWalkRoom('clothing',rack.x,rack.z),false);for(const f of CLOTHING_FIXTURES.filter(f=>f!==rack))assert.ok(Math.abs(f.x-rack.x)>=f.w+rack.w||Math.abs(f.z-rack.z)>=f.d+rack.d,f.id);}
 for(const id of Object.keys(HOME_STYLES)){
  const fixtures=homeFixtures(id),wardrobe=fixtures.find(f=>f.id==='wardrobe');assert.ok(reachable('home',id,HOME_POINTS.wardrobe),id);assert.equal(canWalkRoom('home',wardrobe.x,wardrobe.z,.35,id),false);
  for(const f of fixtures.filter(f=>f!==wardrobe))assert.ok(Math.abs(f.x-wardrobe.x)>=f.w+wardrobe.w||Math.abs(f.z-wardrobe.z)>=f.d+wardrobe.d,id+': '+f.id);
  for(const point of Object.values(HOME_POINTS))assert.ok(reachable('home',id,point),id+': '+point.name);
 }
});
test('outfit modules preserve rig, attachments, geometry and hidden backpack root',()=>{
 const actor=createCitizen({}),u=actor.userData,refs=Object.fromEntries(['arms','legs','elbows','knees','feet','upper','backpack','headRoot'].map(k=>[k,u[k]]));
 const parcel=new THREE.Group(),basket=new THREE.Group();actor.add(parcel);u.arms[0].add(basket);
 const meshes=[];actor.traverse(o=>{if(o.isMesh)meshes.push([o,o.geometry,o.material]);});
 const wardrobe=defaultWardrobe();u.backpack.visible=false;
 for(const item of CLOTHING){wardrobe.equipped[item.slot]=item.id;applyCitizenOutfit(actor,wardrobe.equipped);for(const part of u.clothingModules.get(item.id))assert.equal(part.visible,!(item.slot==='top'&&wardrobe.equipped.outer));}
 for(const [key,ref] of Object.entries(refs))assert.equal(u[key],ref);
 assert.equal(parcel.parent,actor);assert.equal(basket.parent,u.arms[0]);assert.equal(u.backpack.visible,false);
 for(const [m,g,mat] of meshes){assert.equal(m.geometry,g);assert.equal(m.material,mat);}
 wardrobe.equipped.outer=null;applyCitizenOutfit(actor,wardrobe.equipped);for(const id of ['overshirt','jacket','bomber'])for(const part of u.clothingModules.get(id))assert.equal(part.visible,false);
 u.arms[0].rotation.x=.7;u.knees[0].rotation.x=1;actor.updateMatrixWorld(true);assert.ok(u.clothingModules.get('boots')[0].matrixWorld.elements.every(Number.isFinite));
});

test('try-on copies only the selected slot without modifying any saved state',()=>{
 const m=shop(),before=JSON.stringify(m.s),p=previewOutfit(m.s.wardrobe.equipped,'bomber');assert.equal(p.outer,'bomber');assert.equal(p.top,m.s.wardrobe.equipped.top);assert.equal(JSON.stringify(m.s),before);
 p.top='tee';assert.notEqual(m.s.wardrobe.equipped.top,'tee');assert.equal(previewOutfit(p,'none').outer,null);
 assert.equal(m.buyClothing('bomber'),true);assert.equal(m.s.wardrobe.equipped.outer,null);assert.equal(m.equipClothing('bomber'),true);
});
test('store entry, exit and in-store saves work without losing ownership',()=>{
 const m=new GameModel(newGame(true));m.s.position={x:CLOTHING_LOCATION.x,z:CLOTHING_LOCATION.z};assert.equal(m.enterInterior('clothing'),true);
 m.s.position={...CLOTHING_POINTS.clothingTop};assert.equal(m.buyClothing('tee'),true);
 const restored=new GameModel(validateSave(JSON.parse(JSON.stringify(m.s))));assert.ok(restored.s.wardrobe.owned.includes('tee'));assert.equal(restored.s.interior,'clothing');assert.equal(restored.leaveInterior(),true);assert.deepEqual(restored.s.position,ROOMS.clothing.outside);
});
test('grocery shopping remains separate and functional',()=>{
 const m=new GameModel(newGame(true));Object.assign(m.s,{inside:true,interior:'shop',position:{...SHOP_POINTS.drinks}});assert.equal(m.basketAdd('water'),true);assert.equal(m.buyClothing('tee'),false);m.s.position={...SHOP_POINTS.checkout};assert.equal(m.checkout(),true);assert.equal(m.count('water'),1);
});
test('store is on the scalable map, addressed, outside other building footprints',async()=>{
 const {mapPlaces,mapLocationList}=await import('../dist/city-map.js');const {addressOf}=await import('../dist/orientation.js');const {districtOf,WORLD_BOUNDS}=await import('../dist/city-layout.js');
 assert.ok(mapPlaces(newGame()).some(l=>l.id==='clothingStore'));assert.match(mapLocationList(newGame(),CLOTHING_LOCATION),/STADTSTOFF/);assert.equal(addressOf('clothingStore'),'Lindenallee 22');assert.equal(districtOf(CLOTHING_LOCATION),'INNENSTADT');assert.ok(ROOMS.clothing.minX>WORLD_BOUNDS.maxX);
 const [x,z,w,d]=CLOTHING_BUILDING;for(const b of BUILDINGS.filter(b=>b[6]!=='STADTSTOFF'))assert.ok(Math.abs(x-b[0])>=(w+b[2])/2||Math.abs(z-b[1])>=(d+b[3])/2,b[6]);
 for(const [id,r] of Object.entries(ROOMS))if(id!=='clothing')assert.ok(ROOMS.clothing.minX>r.maxX||ROOMS.clothing.maxX<r.minX||ROOMS.clothing.minZ>r.maxZ||ROOMS.clothing.maxZ<r.minZ);
});
test('every catalog item has recognizable mesh merchandise using shared geometry',async()=>{
 const {clothingDisplay}=await import('../dist/clothing-store.js');for(const item of CLOTHING){const a=clothingDisplay(item.id),b=clothingDisplay(item.id),bounds=new THREE.Box3().setFromObject(a);assert.ok(a.children.length>1);assert.ok(!bounds.isEmpty());assert.equal(a.children[0].geometry,b.children[0].geometry);assert.ok(bounds.max.y>0);}
});

test('equipped modules follow the existing walk, run, carry, basket and bicycle animation rig',async()=>{
 const {animateCitizen}=await import('../dist/animation.js');const actor=createCitizen({}),bag=actor.userData.backpack;
 for(const options of [{},{running:true},{carrying:true},{basket:true},{riding:true}]){
  const outfit={top:'hoodie',outer:'bomber',pants:'workpants',shoes:'boots',backpack:'grey-pack'};applyCitizenOutfit(actor,outfit);
  for(let i=0;i<12;i++)animateCitizen(actor,1/60,.06,{grounded:true,...options});actor.updateMatrixWorld(true);
  for(const part of ['arms','legs','elbows','knees','feet'])for(const joint of actor.userData[part])assert.ok(joint.matrixWorld.elements.every(Number.isFinite));assert.equal(actor.userData.backpack,bag);
 }
});
