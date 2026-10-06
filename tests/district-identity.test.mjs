import test from 'node:test';
import assert from 'node:assert/strict';
import {districtIdentityAt,districtBlendAt,districtAppearance,districtCitizenAwake,districtSoundMix,DISTRICT_POPULATION} from '../dist/district-identity.js';
import {Soundscape} from '../dist/soundscape.js';
import {districtLampStyle,districtWindowSet,updateDistrictWindows} from '../dist/district-materials.js';
import * as THREE from '../dist/vendor/three.module.js';
import {citizenAppearance} from '../dist/citizen.js';
import {CITIZEN_PORTALS} from '../dist/pedestrian-layout.js';
import {PedestrianLife} from '../dist/pedestrian-life.js';
import {BUILDINGS} from '../dist/data.js';
const positions={station:{x:-179,z:17},centre:{x:65,z:-15},linden:{x:-34,z:13},harbor:{x:-64,z:-76},nordstadt:{x:-83,z:-163},suedwerk:{x:-83,z:187},ostkontor:{x:264,z:-53},techpark:{x:263,z:-163},hoehen:{x:356,z:163}};
test('district glazing reuses bounded palettes, fades in daylight and retains dark windows',()=>{
 const base=Array.from({length:5},()=>new THREE.MeshStandardMaterial({emissive:0xffca7c})),sets=Object.values(positions).map(p=>districtWindowSet(base,p.x,p.z));
 assert.ok(new Set(sets.flat()).size<=30);assert.equal(sets[0],districtWindowSet(base,-179,17));
 updateDistrictWindows(base,1);assert.ok(sets[0][3].emissiveIntensity>sets[3][3].emissiveIntensity);assert.notEqual(sets[0][3].emissive.getHex(),sets[6][3].emissive.getHex());assert.ok(sets.every(s=>s[1].emissiveIntensity<s[3].emissiveIntensity*.1));
 updateDistrictWindows(base,0);assert.ok(sets.flat().every(m=>m.emissiveIntensity===0));
});

test('all neighborhoods and the Lichthof retain a distinct geographic presentation profile',()=>{
 assert.equal(Object.values(DISTRICT_POPULATION).reduce((a,b)=>a+b),20);
 assert.ok(DISTRICT_POPULATION.hoehen<DISTRICT_POPULATION.nordstadt);
 for(const [id,p] of Object.entries(positions))assert.equal(districtIdentityAt(p).id,id);
 assert.equal(districtIdentityAt({x:55,z:28}).id,'linden');
 for(const x of [-120,15,120,220])for(const z of [-120,-48,14,48,120]){
  const weights=districtBlendAt({x,z});assert.ok(Math.abs(weights.reduce((n,e)=>n+e.weight,0)-1)<1e-10);
  const a=districtLampStyle(x-.001,z),b=districtLampStyle(x+.001,z);
  assert.ok(Math.abs(a.power-b.power)<.02);assert.ok(Math.abs(a.color.b-b.color.b)<.001);
 }
 assert.ok(districtLampStyle(356,175).power<districtLampStyle(270,0).power);
});
test('procedural district beds share the ambience bus and fade out when paused or indoors',()=>{
 const param=()=>({value:0,setTargetAtTime(v){this.value=v},setValueAtTime(v){this.value=v},exponentialRampToValueAtTime(v){this.value=v}});
 const node=()=>({gain:param(),frequency:param(),Q:param(),pan:param(),threshold:param(),ratio:param(),connections:[],connect(n){this.connections.push(n);return n;},start(){},stop(){},disconnect(){}});
 const ctx={state:'running',currentTime:0,sampleRate:100,destination:node(),createGain:node,createDynamicsCompressor:node,createOscillator:node,createBiquadFilter:node,createStereoPanner:node,createBufferSource:node,createBuffer:()=>({getChannelData:()=>new Float32Array(200)})};
 const sound=new Soundscape();sound.init(ctx);
 const s={position:positions.harbor,minute:720,inside:false,weather:'clear',settings:{sound:true}},world={cars:[],angle:0,player:{position:{}}};
 for(const bed of [sound.railBed,sound.workBed])assert.ok(bed.connections.includes(sound.environmentBus));
 sound.update(.1,s,world,{grounded:false},true);assert.ok(sound.workBed.gain.value>.015);
 sound.update(.1,s,world,{grounded:false},false);assert.equal(sound.workBed.gain.value,0);assert.equal(sound.railBed.gain.value,0);
 s.inside=true;s.interior='home';sound.update(.1,s,world,{grounded:false},true);assert.equal(sound.workBed.gain.value,0);
});
test('clothing distributions change while seeded skin, face, hair and body remain identical',()=>{
 for(const [id,p] of Object.entries(positions)){
  const looks=Array.from({length:96},(_,i)=>{
   const base=citizenAppearance(i),look=districtAppearance(base,p,i);
   for(const k of ['skin','jaw','feminine','hair','hairColor','build','shoulders','torso'])assert.equal(look[k],base[k]);
   assert.deepEqual(look,districtAppearance(base,p,i));return look;
  });
  if(['harbor','suedwerk'].includes(id))assert.ok(looks.filter(l=>l.archetype==='worker').length>50);
  if(id==='techpark')assert.ok(looks.filter(l=>l.backpack).length>35);
  if(id==='centre')assert.ok(new Set(looks.map(l=>l.archetype)).size>=7);
  if(id==='ostkontor')assert.ok(looks.every(l=>l.archetype!=='employee'&&l.archetype!=='worker'));
 }
 const homes=CITIZEN_PORTALS.filter(p=>p.home),station=homes.findIndex(p=>p.id==='west');
 const looks=Array.from({length:20},(_,i)=>districtAppearance(citizenAppearance(i*homes.length+station),homes[station],i*homes.length+station));
 assert.ok(new Set(looks.map(l=>l.archetype)).size>=3,'home allocation must not lock clothing to one role');
});
test('work districts wind down at night and station commuting has morning/evening peaks',()=>{
 const count=(id,m)=>Array.from({length:96},(_,i)=>districtCitizenAwake(i,m,positions[id])).filter(Boolean).length;
 assert.equal(count('ostkontor',60),0);assert.ok(count('ostkontor',660)>80);
 assert.ok(count('suedwerk',660)>count('suedwerk',1380)*4);
 assert.ok(count('station',480)>count('station',780));assert.ok(count('station',1080)>count('station',780));
 assert.ok(count('station',1380)>0);assert.ok(count('hoehen',1170)<count('centre',1170));
});
test('sound transitions stay continuous, residential areas are quiet and indoors are silent',()=>{
 const state={position:positions.harbor,minute:720,weather:'clear',inside:false,settings:{sound:true}};
 assert.ok(districtSoundMix(state).industry>.8);
 state.position=positions.hoehen;const garden=districtSoundMix(state);assert.ok(garden.garden<.6&&garden.birds>.7);
 state.position={x:120-.001,z:-30};const a=districtSoundMix(state);state.position.x+=.002;const b=districtSoundMix(state);
 for(const k of Object.keys(a))assert.ok(Math.abs(a[k]-b[k])<.001);
 state.inside=true;assert.ok(Object.values(districtSoundMix(state)).every(v=>v===0));
 state.inside=false;state.settings.sound=false;assert.ok(Object.values(districtSoundMix(state)).every(v=>v===0));
});
test('new district residents use the existing pedestrian graph and existing building entrances',()=>{
 const free=(x,z,r=.35)=>!BUILDINGS.some(([bx,bz,w,d])=>Math.abs(x-bx)<w/2+.35+r&&Math.abs(z-bz)<d/2+.35+r);
 const life=new PedestrianLife(free,{count:0});
 for(const id of ['harborCrew','cityResidents']){
  const home=CITIZEN_PORTALS.find(p=>p.id===id);assert.ok(life.canWalk(home.x,home.z));
  assert.ok(BUILDINGS.some(([x,z,w,d])=>Math.abs(home.x-x)<w/2&&Math.abs(home.doorZ-z)<d/2),'portal ends in existing building');
  assert.ok(life.destinations.some(d=>districtIdentityAt(d).id===districtIdentityAt(home).id&&life.nav.find(home,d).length>1));
 }
});
