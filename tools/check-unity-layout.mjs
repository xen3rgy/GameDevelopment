import fs from 'node:fs';
import assert from 'node:assert/strict';
const data=JSON.parse(fs.readFileSync(new URL('../unity/ZeroRise/Assets/ZeroRise/Data/Westbruch.json',import.meta.url)));
const contains=(r,x,z,m=0)=>Math.abs(x-r.x)<r.w/2+m&&Math.abs(z-r.z)<r.d/2+m;
const overlap=(a,b,m=0)=>Math.abs(a.x-b.x)<(a.w+b.w)/2+m&&Math.abs(a.z-b.z)<(a.d+b.d)/2+m;
const paths=[...data.paths];
for(const b of data.buildings){
  const sign=b.yaw===180?1:-1,door=b.z+sign*(b.d/2+.05);
  const candidates=[...data.paths,...data.roads].flatMap(p=>{
    const m=data.roads.includes(p)?3:0;
    if(Math.abs(b.x-p.x)>p.w/2+m)return[];
    if(contains(p,b.x,door,m))return[{edge:door,ahead:0}];
    const edge=p.z-sign*(p.d/2+m),ahead=(edge-door)*sign;
    return ahead>=0?[{edge,ahead}]:[];
  }).sort((a,b)=>a.ahead-b.ahead);
  assert(candidates.length,'Missing entrance connection: '+b.id);
  const edge=candidates[0].edge;
  paths.push({id:b.id+' approach',x:b.x,z:(door+edge)/2,w:2.2,d:Math.abs(door-edge)+.2});
  for(const other of data.buildings) if(other!==b)
    assert(!overlap(paths.at(-1),other),'Entrance crosses building: '+b.id+' / '+other.id);
}
for(let i=0;i<data.buildings.length;i++)for(let j=i+1;j<data.buildings.length;j++){
  assert(!overlap(data.buildings[i],data.buildings[j],2),'Cramped/overlapping buildings');
}
for(const b of data.buildings)for(const road of data.roads) assert(!overlap(b,road,3),'Building crosses road/sidewalk: '+b.id);
for(const tree of data.trees){
  for(const b of data.buildings)assert(!contains(b,tree.x,tree.z,3.7),'Canopy crosses building: '+b.id);
  for(const road of data.roads)assert(!contains(road,tree.x,tree.z,.4),'Trunk crosses road');
}
for(const b of data.buildings)assert(!contains(b,data.spawn.x,data.spawn.z,.4),'Spawn in building');
const surfaces=[...data.roads.map(r=>({...r,w:r.w+6,d:r.d+6})),...paths];
for(const b of data.buildings){const sign=b.yaw===180?1:-1;const p={id:b.id+' facade walk',x:b.x,z:b.z+sign*(b.d/2+1),w:b.w-1.2,d:2.3};paths.push(p);surfaces.push(p);}
const seen=new Set([0]),pending=[0];
while(pending.length){let i=pending.pop();surfaces.forEach((s,j)=>{if(!seen.has(j)&&overlap(s,surfaces[i],.25)){seen.add(j);pending.push(j);}});}
assert.equal(seen.size,surfaces.length,'Disconnected public path network');
for(const car of data.cars){
  const a={x:car.x,z:car.z,w:car.yaw===90?5.15:2.65,d:car.yaw===90?2.65:5.15};
  for(const b of data.buildings)assert(!overlap(a,b,.2),'Parked car overlaps '+b.id);
  for(const road of data.roads)assert(!overlap(a,road),'Parked car intrudes into road');
}
console.log(`Unity layout passed: ${data.buildings.length} buildings, ${data.roads.length} roads, ${paths.length} connected paths; spawn, tree clearances and parking checked.`);
