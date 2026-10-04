import * as THREE from './vendor/three.module.js';
// Normalized face outlines are shared by alloy meshes, UI cards and the wall display.
export const ALLOY_STYLES=['classic','multi','sport','concave','split','turbine'];
export function rimProfiles(style){
 const profiles=[],add=(count,polygons)=>{for(let n=0;n<count;n++){const a=n*Math.PI*2/count;for(const poly of polygons)profiles.push(poly.map(([r,t])=>[Math.cos(a)*r-Math.sin(a)*t,Math.sin(a)*r+Math.cos(a)*t]));}};
 if(style==='split')add(5,[[[.14,-.065],[.48,-.07],[.89,-.24],[.94,-.16],[.54,.015],[.16,.055]],[[.44,-.015],[.91,.21],[.94,.13],[.56,-.08]]]);
 else if(style==='turbine')add(7,[[[.15,-.06],[.51,-.035],[.92,.15],[.94,.28],[.58,.12],[.18,.065]]]);
 else if(style==='concave')add(5,[[[.14,-.075],[.43,-.045],[.8,-.105],[.94,-.105],[.94,.085],[.65,.075],[.37,.035],[.14,.075]]]);
 else if(style==='multi')add(12,[[[.17,-.027],[.5,-.017],[.93,-.036],[.95,.03],[.49,.02],[.17,.027]]]);
 else if(style==='sport')add(5,[[[.16,-.095],[.57,-.07],[.94,-.15],[.94,.035],[.52,.06],[.16,.095]]]);
 else add(6,[[[.15,-.10],[.72,-.10],[.94,-.16],[.94,.16],[.72,.10],[.15,.10]]]);
 return profiles;
}
const cache=new Map();
const cached=(key,make)=>{if(!cache.has(key))cache.set(key,make());return cache.get(key)};
export function alloyGeometry(style,radius){return cached(style+':'+radius,()=>{const positions=[];for(const poly of rimProfiles(style)){const vec=poly.map(p=>new THREE.Vector2(...p)),depth=p=>{const r=Math.hypot(...p);return radius*(-(style==='concave'?.23:style==='split'?.18:.12)*(1-Math.min(1,r)));},point=(p,back=false)=>[p[0]*radius,p[1]*radius,depth(p)-(back?radius*.07:0)];for(const face of THREE.ShapeUtils.triangulateShape(vec,[])){for(const i of face)positions.push(...point(poly[i]));for(const i of [...face].reverse())positions.push(...point(poly[i],true));}for(let i=0;i<poly.length;i++){const a=poly[i],b=poly[(i+1)%poly.length];for(const p of [point(a),point(a,true),point(b,true),point(a),point(b,true),point(b)])positions.push(...p);}}const geo=new THREE.BufferGeometry();geo.setAttribute('position',new THREE.Float32BufferAttribute(positions,3));geo.computeVertexNormals();return geo;});}
export function createAlloyRim(style,radius,material,dark){const g=new THREE.Group();g.name='Alloy '+style;const add=(geo,mat,z=0)=>{const m=new THREE.Mesh(geo,mat);m.position.z=z;g.add(m);return m;};
 add(cached('lip:'+radius,()=>new THREE.TorusGeometry(radius*.94,radius*.035,6,32)),material);
 const barrel=add(cached('barrel:'+radius,()=>new THREE.CylinderGeometry(radius*.95,radius*.95,radius*.3,24,1,true)),dark,-radius*.12);barrel.rotation.x=Math.PI/2;
 add(cached('recess:'+radius,()=>new THREE.CircleGeometry(radius*.89,24)),dark,-radius*.29);
 add(alloyGeometry(style,radius),material);
 const hub=add(cached('cap:'+radius,()=>new THREE.CylinderGeometry(radius*.17,radius*.19,radius*.05,12)),material,-radius*(style==='concave'?.19:.095));hub.rotation.x=Math.PI/2;
 return g;
}
