import {streetSurface} from './street-layout.js?v=0.8.1';
import {STATION_PLAZAS} from './city-layout.js?v=0.8.1';
import {groundHeight} from './spatial.js?v=0.8.1';
import {onGardenPath} from './pedestrian-layout.js?v=0.8.1';
export function surfaceAt(x,z,interior){
 if(interior==='shop'||interior==='workshop')return 'tile';if(interior==='home'||interior==='cafe')return 'wood';
 if(Math.abs(x-76)<19.5&&Math.abs(z-93)<18)return Math.abs(x-76)<2.5||Math.abs(z-93)<2?'gravel':'grass';
 const surface=streetSurface(x,z);if(surface)return surface.kind==='road'?'asphalt':'paving';
 if(onGardenPath(x,z))return 'paving';
 if(STATION_PLAZAS.some(p=>Math.abs(x-p.x)<=p.w/2&&Math.abs(z-p.z)<=p.d/2))return 'paving';
 return groundHeight(x,z)>.2?'paving':groundHeight(x,z)>=.05?'asphalt':'grass';
}
export function soundMix(state,active=true){
 if(!active||!state.settings.sound)return {city:0,market:0,home:0,rain:0};
 const night=state.minute>=1200||state.minute<360;
 return {city:state.inside?0:night?.24:1,market:state.interior==='shop'?1:state.interior==='cafe'?.55:0,home:state.interior==='home'?1:state.interior==='cafe'?.3:0,rain:state.weather==='rain'?(state.inside?.08:.65):0};
}
export function stereoAt(source,listener,angle){
 const dx=source.x-listener.x,dz=source.z-listener.z,d=Math.hypot(dx,dz);
 return {gain:1/(1+(d/12)**2),pan:Math.max(-1,Math.min(1,(dx*Math.cos(angle)-dz*Math.sin(angle))/Math.max(3,d)))};
}
export class Soundscape {
 constructor(){this.context=null;this.stepDistance=0;this.nextBird=4;this.clock=0;this.enabled=true;this.running=false;this.lastInterior=null;this.voices=[];this.volumes={ambienceVolume:100,vehicleVolume:100,effectsVolume:100}}
 unlock(){
  try{if(!this.context){const C=globalThis.AudioContext||globalThis.webkitAudioContext;if(!C)return;this.init(new C())}if(this.context.state==='suspended')this.context.resume().catch(()=>{})}catch{/* Audio must never prevent playing. */}
 }
 init(ctx){
  this.context=ctx;this.master=ctx.createGain();this.master.gain.value=this.enabled?.65:0;const limiter=ctx.createDynamicsCompressor();limiter.threshold.value=-15;limiter.ratio.value=5;this.master.connect(limiter);limiter.connect(ctx.destination);
  this.ambient=ctx.createGain();this.ambient.gain.value=0;this.ambient.connect(this.master);for(const [key,bus] of [['ambienceVolume','environmentBus'],['vehicleVolume','vehicleBus'],['effectsVolume','effectsBus']]){this[bus]=ctx.createGain();this[bus].gain.value=this.volumes[key]/100;this[bus].connect(bus==='effectsBus'?this.master:this.ambient)}
  this.noise=ctx.createBuffer(1,ctx.sampleRate*2,ctx.sampleRate);const samples=this.noise.getChannelData(0);let low=0;for(let i=0;i<samples.length;i++){low=.97*low+.03*(Math.random()*2-1);samples[i]=(Math.random()*2-1)*.6+low*3}
  this.wind=this.loopNoise(480,.6);this.rain=this.loopNoise(2300,.4);this.room=this.loopNoise(180,.7);
  this.hum=ctx.createOscillator();this.hum.frequency.value=100;this.humGain=ctx.createGain();this.humGain.gain.value=0;this.hum.connect(this.humGain).connect(this.environmentBus);this.hum.start();
  for(let i=0;i<3;i++){const oscillator=ctx.createOscillator(),filter=ctx.createBiquadFilter(),gain=ctx.createGain(),pan=ctx.createStereoPanner();oscillator.type='sawtooth';oscillator.frequency.value=45;filter.type='lowpass';filter.frequency.value=180;gain.gain.value=0;oscillator.connect(filter).connect(gain).connect(pan).connect(this.vehicleBus);oscillator.start();this.voices.push({oscillator,filter,gain,pan,car:null})}
 }
 loopNoise(frequency,q){const ctx=this.context,source=ctx.createBufferSource(),filter=ctx.createBiquadFilter(),gain=ctx.createGain();source.buffer=this.noise;source.loop=true;filter.type='lowpass';filter.frequency.value=frequency;filter.Q.value=q;gain.gain.value=0;source.connect(filter).connect(gain).connect(this.environmentBus);source.start();return gain}
 setEnabled(enabled){if(this.enabled===enabled)return;this.enabled=enabled;if(this.master)this.master.gain.setTargetAtTime(enabled?.65:0,this.context.currentTime,.04)}
 setActive(active){this.running=active;if(this.ambient)this.ambient.gain.setTargetAtTime(active?1:0,this.context.currentTime,.08);if(!active)this.stepDistance=0}
 setVolumes(settings={}){for(const [key,bus] of [['ambienceVolume','environmentBus'],['vehicleVolume','vehicleBus'],['effectsVolume','effectsBus']]){const value=Number.isFinite(settings[key])?Math.max(0,Math.min(100,settings[key])):100;if(this.volumes[key]!==value){this.volumes[key]=value;if(this[bus])this[bus].gain.setTargetAtTime(value/100,this.context.currentTime,.04)}}}
 tone(freq=440,duration=.1,volume=.03,pan=0,bus='effectsBus'){
  if(!this.enabled||!this.context||this.context.state!=='running')return;const ctx=this.context,t=ctx.currentTime,o=ctx.createOscillator(),g=ctx.createGain(),p=ctx.createStereoPanner();o.frequency.setValueAtTime(freq,t);o.frequency.exponentialRampToValueAtTime(freq*.8,t+duration);g.gain.setValueAtTime(volume,t);g.gain.exponentialRampToValueAtTime(.0001,t+duration);p.pan.value=pan;o.connect(g).connect(p).connect(this[bus]);o.start(t);o.stop(t+duration);o.onended=()=>{o.disconnect();g.disconnect();p.disconnect()};
 }
 burst(frequency,duration,volume,pan=0,type='bandpass',bus='effectsBus'){
  if(!this.enabled||!this.context||this.context.state!=='running')return;const ctx=this.context,t=ctx.currentTime,source=ctx.createBufferSource(),filter=ctx.createBiquadFilter(),g=ctx.createGain(),p=ctx.createStereoPanner();source.buffer=this.noise;filter.type=type;filter.frequency.value=frequency;filter.Q.value=.7;p.pan.value=pan;g.gain.setValueAtTime(.0001,t);g.gain.exponentialRampToValueAtTime(Math.max(.0002,volume),t+.006);g.gain.exponentialRampToValueAtTime(.0001,t+duration);source.connect(filter).connect(g).connect(p).connect(this[bus]);source.start(t,Math.random());source.stop(t+duration);source.onended=()=>{source.disconnect();filter.disconnect();g.disconnect();p.disconnect()};
 }
 effect(kind){if(kind==='checkout'){this.tone(1250,.085,.035);this.burst(1800,.25,.025)}else if(kind==='door'){this.burst(220,.15,.12,0,'lowpass')}else if(kind==='shop'){this.burst(1900,.2,.04)}else if(kind==='pickup')this.burst(1250,.12,.025)}
 update(dt,state,world,movement,active){
  this.setVolumes(state.settings);this.setEnabled(state.settings.sound);if(this.running!==active)this.setActive(active);if(!this.context)return;
  const t=this.context.currentTime,mix=soundMix(state,active),set=(node,value)=>node.gain.setTargetAtTime(value,t,.35);
  this.mixAge=(this.mixAge||0)+dt;if(this.mixAge>=.08){this.mixAge=0;
  set(this.wind,mix.city*.026);set(this.rain,mix.rain*.08);set(this.room,mix.market*.018+mix.home*.005);set(this.humGain,mix.market*.014);
  const sources=state.inside?[]:world.cars.map(c=>({car:c.mesh,position:c.mesh.position,speed:c.speed}));if(state.riding&&state.vehicle?.id!=='bike'&&world.vehicleMesh)sources.push({car:world.vehicleMesh,position:world.vehicleMesh.position,speed:Math.abs(state.vehicle.speed)});
  sources.sort((a,b)=>a.position.distanceToSquared(world.player.position)-b.position.distanceToSquared(world.player.position));const nearest=sources.slice(0,3);
  for(let i=0;i<this.voices.length;i++){const voice=this.voices[i],s=nearest[i];if(!s||!active){set(voice.gain,0);continue}const spatial=stereoAt(s.position,state.position,world.angle);voice.pan.pan.setTargetAtTime(spatial.pan,t,.12);voice.oscillator.frequency.setTargetAtTime(36+s.speed*7,t,.15);voice.filter.frequency.setTargetAtTime(110+s.speed*32,t,.2);set(voice.gain,spatial.gain*(.006+s.speed*.0025)*(state.inside?0:1))}
  }
  if(!active)return;this.clock+=dt;
  if(movement.grounded&&movement.moving&&!state.riding){this.stepDistance+=movement.distance;const stride=movement.running?1.7:1.15;if(this.stepDistance>=stride){this.stepDistance%=stride;const kind=surfaceAt(state.position.x,state.position.z,state.interior),settings={tile:[1800,.09,.09],wood:[420,.13,.13],paving:[1050,.1,.10],asphalt:[650,.12,.09],grass:[2600,.18,.04],gravel:[2100,.15,.08]}[kind];this.burst(settings[0]*(.9+Math.random()*.2),settings[1],settings[2]*(movement.running?1.15:1));if(kind==='wood'||kind==='tile')this.tone(kind==='wood'?100:180,.045,.012)}}else this.stepDistance=0;
  if(this.clock>this.nextBird){this.nextBird=this.clock+5+Math.random()*9;if(mix.city>.5&&state.weather!=='rain'){const pan=Math.random()*1.6-.8;this.tone(1800+Math.random()*1100,.13,.008,pan,'environmentBus')}else if(mix.market){this.burst(1400,.35,.014,Math.random()-.5,'bandpass','environmentBus')}}
 }
}
