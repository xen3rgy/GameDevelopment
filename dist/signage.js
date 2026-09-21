import * as THREE from './vendor/three.module.js';
const materials=new Map();
export function labelSize(width,height){
 const aspect=Math.max(.1,width/height),h=Math.min(256,Math.max(8,Math.floor(2048/aspect)));
 return {width:Math.min(2048,Math.max(8,Math.round(h*aspect))),height:h};
}
export function sign(parent,text,x,y,z,w,h,color='#f0eadb',bg='#263d42',rotate=0){
 const key=[text,w,h,color,bg].join('|');
 if(!materials.has(key)){
  const size=labelSize(w,h),canvas=document.createElement('canvas');canvas.width=size.width;canvas.height=size.height;
  const a=canvas.getContext('2d');a.fillStyle=bg;a.fillRect(0,0,canvas.width,canvas.height);
  let font=Math.floor(canvas.height*.68);a.font=`600 ${font}px Arial, sans-serif`;
  const measured=a.measureText(text).width;if(measured>canvas.width*.92)font=Math.floor(font*canvas.width*.92/measured);
  a.font=`600 ${font}px Arial, sans-serif`;a.fillStyle=color;a.textAlign='center';a.textBaseline='middle';a.fillText(text,canvas.width/2,canvas.height*.52);
  const map=new THREE.CanvasTexture(canvas);map.colorSpace=THREE.SRGBColorSpace;map.anisotropy=8;
  materials.set(key,new THREE.MeshBasicMaterial({map,side:THREE.FrontSide,toneMapped:false}));
 }
 const mesh=new THREE.Mesh(new THREE.PlaneGeometry(w,h),materials.get(key));mesh.name='Sign · '+text;mesh.position.set(x,y,z);mesh.rotation.y=rotate;parent.add(mesh);return mesh;
}
