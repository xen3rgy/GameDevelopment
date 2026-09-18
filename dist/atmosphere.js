import {districtLampStyle} from './district-materials.js?v=0.7.2';
import * as THREE from './vendor/three.module.js';
import {lightingAt,selectLamps} from './lighting.js?v=0.7.2';
import {groundHeight} from './spatial.js?v=0.7.2-stationedge2';
function random(seed){let n=seed;return()=>{n=(n*1664525+1013904223)>>>0;return n/4294967296}}
export function surfaceTexture(kind){
  const c=document.createElement('canvas');c.width=c.height=512;const a=c.getContext('2d'),rand=random(kind==='asphalt'?18:72);
  a.fillStyle=kind==='asphalt'?'#72777a':'#c1beb3';a.fillRect(0,0,512,512);
  if(kind==='paving')for(let y=0;y<512;y+=32)for(let x=-64;x<512;x+=64){const xx=x+(y%64?32:0);let shade=Math.floor(166+rand()*22);a.fillStyle=`rgb(${shade+8},${shade+7},${shade})`;a.fillRect(xx+2,y+2,62,30);a.fillStyle='#d5d2bf55';a.fillRect(xx+3,y+3,60,1)}
  if(kind==='cobble')for(let y=0;y<512;y+=24)for(let x=-32;x<512;x+=32){const xx=x+(y/24%2?16:0),v=156+Math.floor(rand()*38);a.fillStyle=`rgb(${v+8},${v+4},${v-6})`;a.fillRect(xx+2,y+2,28,20);a.fillStyle='#e0d4bd35';a.fillRect(xx+3,y+3,26,2);}
  for(let i=0;i<26000;i++){const n=rand();a.fillStyle=n>.5?'rgba(255,255,255,.065)':'rgba(0,0,0,.075)';a.fillRect(rand()*512,rand()*512,1+rand()*2,1+rand()*2)}
  const t=new THREE.CanvasTexture(c);t.colorSpace=THREE.SRGBColorSpace;t.wrapS=t.wrapT=THREE.RepeatWrapping;t.anisotropy=4;return t;
}
export function surfaceMaterial(kind,w,d){
  const t=surfaceTexture(kind);t.repeat.set(w/(kind==='asphalt'?6:8),d/(kind==='asphalt'?6:8));
  return new THREE.MeshStandardMaterial({map:t,color:kind==='asphalt'?0x899096:kind==='cobble'?0xb0a189:0xd0d6d5,roughness:kind==='asphalt'?.86:.95,metalness:kind==='asphalt'?.04:0,bumpMap:t,bumpScale:kind==='asphalt'?.014:.025});
}
export class Atmosphere {
  constructor(scene,renderer){
    this.scene=scene;this.time=0;this.selectionAge=1;this.selected=[];this.lampSlots=[];this.puddles=[];this.lightPositions=[];this.materials=[];this.lights=[];this.quality='high';
    const uniforms={zenith:{value:new THREE.Color('#577788')},horizon:{value:new THREE.Color('#d4b292')},sunColor:{value:new THREE.Color('#f9d3a0')},sunDirection:{value:new THREE.Vector3(-.7,.13,-.5).normalize()},night:{value:0},time:{value:0}};
    this.sky=new THREE.Mesh(new THREE.SphereGeometry(450,32,16),new THREE.ShaderMaterial({uniforms,side:THREE.BackSide,depthWrite:false,toneMapped:false,vertexShader:'varying vec3 vPosition; void main(){vPosition=position;gl_Position=projectionMatrix*modelViewMatrix*vec4(position,1.0);}',fragmentShader:`varying vec3 vPosition;uniform vec3 zenith;uniform vec3 horizon;uniform vec3 sunColor;uniform vec3 sunDirection;uniform float night;uniform float time;
float hash(vec2 p){return fract(sin(dot(p,vec2(127.1,311.7)))*43758.5453);}
float noise(vec2 p){vec2 i=floor(p),f=fract(p);f=f*f*(3.-2.*f);return mix(mix(hash(i),hash(i+vec2(1.,0.)),f.x),mix(hash(i+vec2(0.,1.)),hash(i+vec2(1.)),f.x),f.y);}
void main(){vec3 d=normalize(vPosition);float h=max(d.y,0.);vec3 color=mix(horizon,zenith,pow(h,.45));vec2 uv=d.xz/(max(d.y,0.)+.25)*2.2+vec2(time*.006,0.);float clouds=noise(uv)*.57+noise(uv*2.1)*.28+noise(uv*4.3)*.15;clouds=smoothstep(.48,.77,clouds)*smoothstep(0.,.18,d.y);color=mix(color,mix(horizon,vec3(.97,.95,.9),.55),clouds*.52*(1.-night));float sun=max(dot(d,sunDirection),0.);color+=sunColor*(pow(sun,420.)*.5+pow(sun,22.)*.08)*(1.-night);float moon=dot(d,normalize(vec3(.45,.6,.2)));color+=vec3(.5,.6,.75)*smoothstep(.99955,.99985,moon)*night;float stars=step(.9993,fract(sin(dot(floor(d.xz*900.),vec2(12.9898,78.233)))*43758.5453))*smoothstep(.1,.6,d.y)*night;gl_FragColor=vec4(color+vec3(stars*.6),1.);
#include <colorspace_fragment>
}`}));scene.add(this.sky);this.uniforms=uniforms;
    // A neutral urban environment provides specular response without network assets.
    const c=document.createElement('canvas');c.width=512;c.height=256;let a=c.getContext('2d'),gradient=a.createLinearGradient(0,0,0,256);gradient.addColorStop(0,'#62829a');gradient.addColorStop(.48,'#d8c8a3');gradient.addColorStop(.53,'#5a6669');gradient.addColorStop(1,'#202e34');a.fillStyle=gradient;a.fillRect(0,0,512,256);for(let i=0;i<24;i++){a.fillStyle=i%3?'#87969b':'#e5c890';a.fillRect(i*23,124,6,20)}
    let texture=new THREE.CanvasTexture(c);texture.mapping=THREE.EquirectangularReflectionMapping;texture.colorSpace=THREE.SRGBColorSpace;let pmrem=new THREE.PMREMGenerator(renderer);this.environment=pmrem.fromEquirectangular(texture);scene.environment=this.environment.texture;scene.environmentIntensity=.32;texture.dispose();pmrem.dispose();
    for(let i=0;i<8;i++){const light=new THREE.SpotLight(0xffc87f,0,30,Math.PI*.39,.65,2);light.castShadow=i<2;light.shadow.mapSize.set(512,512);light.shadow.bias=-.0007;light.shadow.normalBias=.035;scene.add(light);scene.add(light.target);this.lights.push(light);this.lampSlots.push({light,lamp:null,power:0})}
    const halo=document.createElement('canvas');halo.width=halo.height=128;const h=halo.getContext('2d'),glow=h.createRadialGradient(64,64,0,64,64,64);glow.addColorStop(0,'rgba(255,245,220,1)');glow.addColorStop(.08,'rgba(255,221,163,.85)');glow.addColorStop(.3,'rgba(255,184,97,.18)');glow.addColorStop(1,'rgba(255,170,74,0)');h.fillStyle=glow;h.fillRect(0,0,128,128);this.haloTexture=new THREE.CanvasTexture(halo);this.haloTexture.colorSpace=THREE.SRGBColorSpace;
  }
  registerLamp(x,z,bulb,options={}){
    const style=districtLampStyle(x),height=options.height??5,color=style.color,power=options.power??style.power,poolSize=options.poolSize??15;
    const halo=new THREE.Sprite(new THREE.SpriteMaterial({map:this.haloTexture,color,transparent:true,opacity:0,blending:THREE.AdditiveBlending,depthWrite:false}));halo.position.set(x,height,z);halo.scale.set(1.8,1.8,1);this.scene.add(halo);
    const pool=new THREE.Mesh(new THREE.PlaneGeometry(poolSize,poolSize),new THREE.MeshBasicMaterial({map:this.haloTexture,color,transparent:true,opacity:0,depthWrite:false,blending:THREE.AdditiveBlending}));pool.rotation.x=-Math.PI/2;pool.position.set(x,groundHeight(x,z)+.019,z);this.scene.add(pool);
    this.lightPositions.push({x,z,bulb,halo,pool,height,color,power,kind:options.height?'accent':'street',distance:options.distance??30});
  }
  addPuddle(x,z,w,d){const mesh=new THREE.Mesh(new THREE.CircleGeometry(1,24),new THREE.MeshStandardMaterial({color:0x697c89,roughness:.09,metalness:.72,transparent:true,opacity:.48,depthWrite:false}));mesh.rotation.x=-Math.PI/2;mesh.scale.set(w,d,1);mesh.position.set(x,groundHeight(x,z)+.009,z);mesh.receiveShadow=true;this.scene.add(mesh);this.puddles.push(mesh)}
  update(state,position,dt=0){
    this.time+=dt;this.uniforms.time.value=this.time;
    const light=lightingAt(state.minute,state.inside),{day,night,dusk,dawn}=light,rain=state.weather==='rain';
    const top=new THREE.Color(rain?'#647d8a':'#75a3bc').lerp(new THREE.Color('#030810'),night),horizon=new THREE.Color(rain?'#a8b8bb':'#cedde0').lerp(new THREE.Color('#d99468'),Math.max(dawn,dusk)*.48*(1-night*.7)).lerp(new THREE.Color('#0b1625'),night);
    this.scene.environmentIntensity=light.environment;
    this.uniforms.zenith.value.copy(top);this.uniforms.horizon.value.copy(horizon);this.uniforms.night.value=night;
    this.uniforms.sunDirection.value.set(-.8,Math.max(.035,light.altitude),-.5).normalize();this.sky.position.set(position.x,0,position.z);
    this.scene.fog.color.copy(horizon);this.scene.fog.density=rain?.007:.0033;
    for(let m of this.materials)m.roughness=rain?.28:.86;
    this.selectionAge+=dt;if(this.selectionAge>.15||!this.selected.length){this.selectionAge=0;this.selected=selectLamps(this.lightPositions,position,this.lampSlots.map(s=>s.lamp).filter(Boolean),this.quality==='low'?4:8)}const selected=this.selected,wanted=new Set(selected);
    const occupied=new Set(this.lampSlots.map(s=>s.lamp).filter(Boolean));
    for(const slot of this.lampSlots){
      slot.light.visible=!state.inside;
      if(slot.lamp&&!wanted.has(slot.lamp)){slot.power=Math.max(0,slot.power-dt*4);if(slot.power===0){occupied.delete(slot.lamp);slot.lamp=null}}
      if(!slot.lamp){const next=selected.find(p=>!occupied.has(p));if(next){slot.lamp=next;occupied.add(next);slot.light.position.set(next.x,next.height-.15,next.z);slot.light.target.position.set(next.x,groundHeight(next.x,next.z),next.z);slot.light.color.copy(next.color);slot.light.distance=next.distance}}
      if(slot.lamp&&wanted.has(slot.lamp))slot.power=Math.min(1,slot.power+dt*3);
      slot.light.intensity=slot.power*light.lamp*(slot.lamp?.power??260);slot.light.castShadow=this.quality!=='low'&&this.lampSlots.indexOf(slot)<2;
    }
    for(const p of this.lightPositions){p.halo.visible=p.pool.visible=!state.inside&&light.lamp>.001;p.halo.material.opacity=light.lamp*.65;p.pool.material.opacity=light.lamp*(rain?.09:.055);if(p.bulb)p.bulb.color.copy(p.color).multiplyScalar(light.lamp*2).addScalar(.065)}
    for(let p of this.puddles){p.visible=!state.inside;p.material.opacity=rain?.52:.13}
    return light;
  }
}
