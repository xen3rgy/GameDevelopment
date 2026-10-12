const fs=require('node:fs'),path=require('node:path');
const sharp=require(process.env.ZERO_RISE_SHARP||'sharp');
const input=process.argv[2],out=path.resolve(__dirname,'../dist/assets/west-v2');
const names=['asphalt','paving','yard','plaster','brick','concrete','wood','steel','ballast'];
async function run(){
 fs.mkdirSync(out,{recursive:true});const meta=await sharp(input).metadata();
 for(let i=0;i<9;i++){
  const x=Math.round(i%3*meta.width/3),y=Math.round(Math.floor(i/3)*meta.height/3),X=Math.round((i%3+1)*meta.width/3),Y=Math.round((Math.floor(i/3)+1)*meta.height/3);
  const image=await sharp(input).extract({left:x+2,top:y+2,width:X-x-4,height:Y-y-4}).resize(512,512).removeAlpha().raw().toBuffer();
  // Edge equality prevents obvious repeating seams. Restrict blending to the
  // outer 16 pixels; the photographed/generated detail inside stays intact.
  const blend=(a,b,t)=>Math.round(a*(1-t)+b*t),edge=16;
  for(let row=0;row<512;row++)for(let n=0;n<edge;n++)for(let c=0;c<3;c++){const a=(row*512+n)*3+c,b=(row*512+511-n)*3+c,t=(1-n/edge)*.5;const av=image[a],bv=image[b];image[a]=blend(av,bv,t);image[b]=blend(bv,av,t);}
  for(let col=0;col<512;col++)for(let n=0;n<edge;n++)for(let c=0;c<3;c++){const a=(n*512+col)*3+c,b=((511-n)*512+col)*3+c,t=(1-n/edge)*.5,av=image[a],bv=image[b];image[a]=blend(av,bv,t);image[b]=blend(bv,av,t);}
  await sharp(image,{raw:{width:512,height:512,channels:3}}).jpeg({quality:88}).toFile(path.join(out,names[i]+'.jpg'));
  await sharp(image,{raw:{width:512,height:512,channels:3}}).greyscale().blur(.5).jpeg({quality:82}).toFile(path.join(out,names[i]+'-height.jpg'));
 }
 console.log('9 diffuse + 9 height textures written to '+out);
}
run().catch(e=>{console.error(e);process.exitCode=1;});
