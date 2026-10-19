// Reuse our own browser hero/default outfit. No downloaded character/model.
import fs from 'node:fs';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
import * as THREE from '../dist/vendor/three.module.js';
import {createCitizen} from '../dist/citizen.js';
const project=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');
const hero=createCitizen(null);hero.updateMatrixWorld(true);
const u=hero.userData,shift=new THREE.Vector3(0,-.05,0);
const point=g=>g.getWorldPosition(new THREE.Vector3()).add(shift).toArray();
const bones=[
 {name:'Rig',parent:-1,p:[0,0,0]},
 {name:'Hips',parent:0,p:point(u.upper)},
 {name:'Chest',parent:1,p:[0,1.23,0]},
 {name:'Neck',parent:2,p:[0,1.43,0]},
 {name:'Head',parent:3,p:point(u.headRoot)},
 {name:'Backpack',parent:2,p:[0,1.15,-.24]}
];
const joints=new Map([[hero,0],[u.upper,1],[u.headRoot,4],[u.backpack,5]]);
for(let side=0;side<2;side++){
 const suffix=side===0?'L':'R';
 const add=(name,parent,p,g)=>{const id=bones.length;bones.push({name,parent,p});if(g)joints.set(g,id);return id;};
 const arm=add('UpperArm_'+suffix,2,point(u.arms[side]),u.arms[side]);
 const elbow=add('Forearm_'+suffix,arm,point(u.elbows[side]),u.elbows[side]);
 add('Hand_'+suffix,elbow,[bones[elbow].p[0],bones[elbow].p[1]-.229,.026]);
 const leg=add('Thigh_'+suffix,1,point(u.legs[side]),u.legs[side]);
 const knee=add('Calf_'+suffix,leg,point(u.knees[side]),u.knees[side]);
 add('Foot_'+suffix,knee,point(u.feet[side]),u.feet[side]);
}
const materials=[],parts=[];
const matIndex=new Map();
const round=x=>Math.round(x*1000000)/1000000;
hero.traverseVisible(m=>{
 if(!m.isMesh)return;
 const source=m.geometry.clone();source.applyMatrix4(m.matrixWorld);source.translate(0,-.05,0);
 const pos=source.attributes.position,norm=source.attributes.normal,uv=source.attributes.uv;
 let ancestor=m.parent;while(ancestor&&!joints.has(ancestor))ancestor=ancestor.parent;
 let bone=joints.get(ancestor)??1;
 source.computeBoundingBox();const center=source.boundingBox.getCenter(new THREE.Vector3());
 const color=m.material.color.getHex();
 if(bone===1&&m.material.roughness===.72&&center.y>1.38)bone=3;
 if(bone===5&&center.z>.02)bone=2; // Front straps remain attached to the chest.
 if(bones[bone].name.startsWith('Forearm_')&&m.material.roughness===.72&&center.y<bones[bone].p[1]-.15)bone++;
 const key=color+':'+m.material.roughness;
 if(!matIndex.has(key)){
  matIndex.set(key,materials.length);
  const kind=m.material.roughness===.72?'skin':m.material.roughness===.96?'hair':color===0x394b5b?'denim':color===0x77745c?'pack':m.material.roughness===.82?'shoe':'cloth';
  materials.push({name:kind+'_'+color.toString(16),kind,color:[m.material.color.r,m.material.color.g,m.material.color.b].map(round),roughness:m.material.roughness});
 }
 parts.push({bone,material:matIndex.get(key),positions:Array.from(pos.array,round),normals:Array.from(norm.array,round),uv:Array.from(uv.array,round),indices:source.index?Array.from(source.index.array):Array.from({length:pos.count},(_,i)=>i)});
 source.dispose();
});
const output=path.join(project,'unity/ZeroRise/Assets/ZeroRise/Art/Player');fs.mkdirSync(output,{recursive:true});
fs.writeFileSync(path.join(output,'BrowserHero.json'),JSON.stringify({bones,materials,parts}));
fs.writeFileSync(path.join(output,'SOURCE.md'),'Base geometry and default streetwear exported from our dist/citizen.js and dist/clothing.js using the bundled Three.js. Unity adds smooth surface refinement, facial/clothing detail, HDRP fabric materials, a generic skinning rig and authored locomotion/jump/landing clips. No external character, motion-capture pack or downloaded model is used. Re-export with node tools/export-unity-player.mjs after intentionally updating the browser base.\n');
console.log(`Browser hero exported: ${bones.length} bones, ${parts.length} visible parts, ${materials.length} materials, ${parts.reduce((n,p)=>n+p.indices.length/3,0)} source triangles.`);
