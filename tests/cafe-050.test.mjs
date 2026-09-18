import test from 'node:test';
import assert from 'node:assert/strict';
import {GameModel,newGame,validateSave} from '../dist/model.js';
import {CAFE_POINTS,CAFE_FIXTURES,CAFE_MENU,newCafe,tickCafe,guestWalkDuration,guestPlace} from '../dist/cafe.js';
import {ROOMS,canWalkRoom} from '../dist/spatial.js';
import * as THREE from '../dist/vendor/three.module.js';
import {buildCafe,attachCafeTray,updateCafe} from '../dist/cafe-interior.js';
import {createCitizen} from '../dist/art.js';
import {surfaceAt,soundMix} from '../dist/soundscape.js';

function ownedCafe(){const s=newGame(true);s.businesses.cafe={stock:3,staff:0,price:1,quality:1,marketing:false,open:true,profit:0,sales:0,costs:0};s.stats.businesses=1;s.position={x:63,z:-12};const m=new GameModel(s);assert.equal(m.enterInterior('cafe'),true);return m}
function move(m,p){m.s.position={x:p.x,z:p.z}}
function tick(m,seconds){if(m.s.cafe.elapsed===0)m.s.cafe.rng=1000;for(let t=0;t<seconds;t+=.05)tickCafe(m,Math.min(.05,seconds-t))}

test('cafe interior is reachable and all operating points have a collision-free approach',()=>{
 const room=ROOMS.cafe,step=.3,key=(x,z)=>`${Math.round(x/step)},${Math.round(z/step)}`,queue=[room.spawn],seen=new Set([key(room.spawn.x,room.spawn.z)]);
 while(queue.length){const p=queue.shift();for(const [dx,dz] of [[step,0],[-step,0],[0,step],[0,-step]]){const n={x:p.x+dx,z:p.z+dz},k=key(n.x,n.z);if(!seen.has(k)&&canWalkRoom('cafe',n.x,n.z,.35)){seen.add(k);queue.push(n)}}}
 for(const p of Object.values(CAFE_POINTS)){let reachable=false;for(let a=0;a<Math.PI*2;a+=Math.PI/12){const x=p.x+Math.cos(a)*1.25,z=p.z+Math.sin(a)*1.25;if(seen.has(key(x,z))){reachable=true;break}}assert.equal(reachable,true,p.name)}
 assert.equal(canWalkRoom('cafe',300,74.8),false);assert.ok(CAFE_FIXTURES.length>=5);
});

test('cafe scene contains furnishings, lights, animated guests and a carried tray',()=>{
 const materials=new Map(),mat=color=>{if(!materials.has(color))materials.set(color,new THREE.MeshStandardMaterial({color:color||0xffffff}));return materials.get(color)};
 const kit={mat,box(parent,x,y,z,w,h,d,color,material){const m=new THREE.Mesh(new THREE.BoxGeometry(w,h,d),material||mat(color));m.position.set(x,y,z);parent.add(m);return m},cylinder(parent,x,y,z,r,h,color){const m=new THREE.Mesh(new THREE.CylinderGeometry(r,r,h,8),mat(color));m.position.set(x,y,z);parent.add(m);return m},sign(){return new THREE.Group()}};
 const world={scene:new THREE.Scene(),exitDoor(){}};buildCafe(world,kit);world.player=createCitizen(kit);attachCafeTray(world);world.scene.add(world.player);
 let meshes=0,lights=0;world.cafe.traverse(o=>{if(o.isMesh)meshes++;if(o.isLight)lights++});assert.ok(meshes>45);assert.equal(lights,3);
 const state=newGame();state.inside=true;state.interior='cafe';state.cafe={...newCafe(),phase:'open',day:1,guests:[{id:1,seat:0,recipe:'espresso',stage:'order',time:0,wait:1,patience:75,price:680}],serial:1,carrying:true};updateCafe(world,state,1/60);
 assert.equal(world.cafe.visible,true);assert.equal(world.cafeGuests.size,1);assert.equal(world.cafeTray.visible,true);const bounds=new THREE.Box3().setFromObject(world.cafe);assert.ok(bounds.min.x>=291.8&&bounds.max.x<=308.2);assert.ok(bounds.min.z>=71.8&&bounds.max.z<=88.2);
 state.cafe.guests=[];state.cafe.carrying=false;updateCafe(world,state,1/60);assert.equal(world.cafeGuests.size,0);assert.equal(world.cafeTray.visible,false);
});

test('a complete shift requires order, preparation, collection, service and clearing',()=>{
 const m=ownedCafe();move(m,CAFE_POINTS.cafeOffice);assert.equal(m.cafeAction('start'),true);assert.equal(m.s.businesses.cafe.stock,2);
 tick(m,18);const guest=m.s.cafe.guests[0];assert.ok(guest);assert.equal(guest.stage,'order');
 move(m,CAFE_POINTS['cafeTable'+guest.seat]);assert.equal(m.cafeAction('table',guest.seat),true);assert.equal(guest.stage,'accepted');
 move(m,CAFE_POINTS.cafePrep);assert.equal(m.cafeAction('prepare',guest.id),true);tick(m,CAFE_MENU[guest.recipe].seconds+1);assert.equal(m.s.cafe.tray,guest.id);assert.equal(m.s.cafe.carrying,false);
 assert.equal(m.cafeAction('collect'),true);assert.equal(m.s.cafe.carrying,true);move(m,CAFE_POINTS['cafeTable'+guest.seat]);assert.equal(m.cafeAction('table',guest.seat),true);assert.equal(m.s.cafe.served,1);assert.equal(m.s.cafe.revenue,0);assert.equal(guest.served,true);
 tick(m,CAFE_MENU[guest.recipe].dining+.1);assert.equal(guest.stage,'paying');assert.ok(m.cafeAction('guest',guest.id));assert.ok(m.s.cafe.revenue>0);
 tick(m,guestWalkDuration(guest.seat,guest.side)+1);assert.ok(m.s.cafe.dirty[guestPlace(guest)]>0);assert.equal(m.cafeAction('table',guest.seat),true);assert.equal(m.s.cafe.dirty[guestPlace(guest)],0);
 move(m,CAFE_POINTS.cafeOffice);assert.equal(m.cafeAction('finish'),true);assert.equal(m.s.cafe.phase,'closed');assert.equal(m.s.cafe.lastReport.served,1);assert.equal(m.cafeAction('start'),false);
});

test('walking out closes a shift and midnight books its result exactly once',()=>{
 const m=ownedCafe();move(m,CAFE_POINTS.cafeOffice);m.cafeAction('start');m.s.cafe.revenue=4200;m.s.cafe.tips=300;m.s.cafe.ingredients=500;const before=m.s.money;
 move(m,CAFE_POINTS.cafeExit);assert.equal(m.leaveInterior(),true);assert.equal(m.s.cafe.phase,'closed');assert.equal(m.s.cafe.lastReport.reason,'Café verlassen');assert.equal(m.s.money,before);
 m.advance(1440-m.s.minute);const profit=4200+300-2450-833-500;assert.equal(m.s.businesses.cafe.profit,profit);assert.equal(m.s.money,before+profit+833);const booked=m.s.money;m.s.day++;m.daily();assert.notEqual(m.s.money,booked+profit);
});

test('impatient individual guests leave and a shift auto-closes',()=>{
 const m=ownedCafe();move(m,CAFE_POINTS.cafeOffice);m.cafeAction('start');tick(m,18);const g=m.s.cafe.guests[0];
 g.wait=g.patience-.1;tick(m,.2);assert.equal(m.s.cafe.lost,1);assert.equal(g.stage,'leaving');
 tick(m,220);assert.equal(m.s.cafe.phase,'closed');assert.equal(m.s.cafe.lastReport.reason,'Schicht abgeschlossen');
});

test('0.4.6 saves receive cafe defaults and malformed cafe state is rejected',()=>{
 const old=newGame();delete old.cafe;const migrated=validateSave(old);assert.deepEqual(migrated.cafe,newCafe());
 const bad=newGame();bad.businesses.cafe={stock:3,staff:0,price:1,quality:1,marketing:false,open:true,profit:0,sales:0,costs:0};bad.cafe={...newCafe(),phase:'open',day:bad.day};assert.throws(()=>validateSave(bad),/Café/);
});

test('cafe uses wood footsteps and a restrained indoor ambience',()=>{const s=newGame();s.inside=true;s.interior='cafe';assert.equal(surfaceAt(300,80,'cafe'),'wood');const mix=soundMix(s);assert.equal(mix.city,0);assert.ok(mix.market>0&&mix.market<1);assert.ok(mix.home>0&&mix.home<1)});
