const fs=require('node:fs'),path=require('node:path');
const sharp=require(process.env.ZERO_RISE_SHARP||'sharp');
const out=path.resolve(__dirname,'../unity/ZeroRise/Assets/ZeroRise/Art/SurfacesV3');
async function run(){
  fs.mkdirSync(out,{recursive:true});
  const input=process.argv[2],foliage=process.argv[3],meta=await sharp(input).metadata();
  const names=['asphalt','paving','yard','grass','plaster','brick','concrete','sand','mud'];
  for(let i=0;i<9;i++){
    const left=Math.round(i%3*meta.width/3),top=Math.round(Math.floor(i/3)*meta.height/3);
    const width=Math.round((i%3+1)*meta.width/3)-left,height=Math.round((Math.floor(i/3)+1)*meta.height/3)-top;
    const size=Math.min(width-6,height-6);
    const rgb=await sharp(input).extract({left:left+3,top:top+3,width:size,height:size}).removeAlpha().resize(1024,1024).raw().toBuffer();
    // Restrict seam matching to tile borders, never blur the material's main detail.
    for(let y=0;y<1024;y++)for(let n=0;n<20;n++)for(let c=0;c<3;c++){
      let a=(y*1024+n)*3+c,b=(y*1024+1023-n)*3+c,t=(1-n/20)*.5,av=rgb[a],bv=rgb[b];rgb[a]=av*(1-t)+bv*t;rgb[b]=bv*(1-t)+av*t;
    }
    for(let x=0;x<1024;x++)for(let n=0;n<20;n++)for(let c=0;c<3;c++){
      let a=(n*1024+x)*3+c,b=((1023-n)*1024+x)*3+c,t=(1-n/20)*.5,av=rgb[a],bv=rgb[b];rgb[a]=av*(1-t)+bv*t;rgb[b]=bv*(1-t)+av*t;
    }
    await sharp(rgb,{raw:{width:1024,height:1024,channels:3}}).jpeg({quality:94}).toFile(path.join(out,names[i]+'.jpg'));
    await sharp(rgb,{raw:{width:1024,height:1024,channels:3}}).greyscale().blur(1).png().toFile(path.join(out,names[i]+'-height.png'));
    // HDRP mask R metal, G AO, B detail, A smoothness. Derived, not scanned PBR.
    const mask=Buffer.alloc(1024*1024*4);
    for(let p=0;p<1024*1024;p++){
      const l=(rgb[p*3]+rgb[p*3+1]+rgb[p*3+2])/765;
      mask[p*4]=0;mask[p*4+1]=Math.round(220+l*35);mask[p*4+2]=255;
      mask[p*4+3]=Math.round((names[i]==='mud'?.25:names[i]==='asphalt'?.18:.08)*255*(.75+l*.5));
    }
    await sharp(mask,{raw:{width:1024,height:1024,channels:4}}).png().toFile(path.join(out,names[i]+'-mask.png'));
  }
  fs.copyFileSync(input,path.join(out,'material-atlas-source.png'));
  if(foliage)fs.copyFileSync(foliage,path.join(out,'urban-foliage.png'));
  console.log('9 material tiles, derived heights/masks and foliage saved to '+out);
}
run().catch(e=>{console.error(e);process.exitCode=1;});
