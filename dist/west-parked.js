// Static scenery cars use the same body envelope as createCar, split into short
// rotated slices. Low bonnet/boot sections do not behave like a tall camera wall.
export function westParkedColliders(x,z,angle,type='car',ground=0){
 const length=type==='van'?4.8:type==='sport'?4.1:3.8,parts=[],count=12;
 for(let i=0;i<count;i++){
  const localZ=-length/2+(i+.5)*length/count,half=length/count/2;
  const cabin=type==='van'?localZ<.6:localZ>-.95&&localZ<.65;
  const width=cabin?.86:1.02,height=cabin?(type==='van'?2:type==='sport'?1.42:1.62):1.02;
  parts.push({x:x+Math.sin(angle)*localZ,z:z+Math.cos(angle)*localZ,w:Math.abs(Math.cos(angle))*width+Math.abs(Math.sin(angle))*half,d:Math.abs(Math.sin(angle))*width+Math.abs(Math.cos(angle))*half,h:ground+height,parkedScenery:true});
 }
 return parts;
}
