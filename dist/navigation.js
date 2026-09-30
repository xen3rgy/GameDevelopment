import {segmentClear} from './movement.js?v=0.8.1';
export class CollisionIndex{
 constructor(blocks,cell=24){this.blocks=blocks;this.cell=cell;this.buckets=new Map();for(const b of blocks)for(let x=Math.floor((b.x-b.w-4)/cell);x<=Math.floor((b.x+b.w+4)/cell);x++)for(let z=Math.floor((b.z-b.d-4)/cell);z<=Math.floor((b.z+b.d+4)/cell);z++){const key=x+':'+z;if(!this.buckets.has(key))this.buckets.set(key,[]);this.buckets.get(key).push(b);}}
 candidates(x,z,r=0){return r>4?this.blocks:this.buckets.get(Math.floor(x/this.cell)+':'+Math.floor(z/this.cell))||[];}
 blocked(x,z,r=0){return this.candidates(x,z,r).some(b=>Math.abs(x-b.x)<b.w+r&&Math.abs(z-b.z)<b.d+r);}
}
class MinHeap{
 constructor(){this.items=[];}
 push(item){const a=this.items;a.push(item);let i=a.length-1;while(i){const p=(i-1)>>1;if(a[p].f<=item.f)break;a[i]=a[p];i=p;}a[i]=item;}
 pop(){const a=this.items,top=a[0],last=a.pop();if(a.length){let i=0;while(i*2+1<a.length){let c=i*2+1;if(c+1<a.length&&a[c+1].f<a[c].f)c++;if(a[c].f>=last.f)break;a[i]=a[c];i=c;}a[i]=last;}return top;}
}
// A* on a fixed grid. The graph is cached; each route is smoothed only through free space.
export class CityNavigation {
  constructor(canWalk,step=4,bound=116){
    this.canWalk=canWalk;this.step=step;this.bound=bound;const b=typeof bound==='number'?{minX:-bound,maxX:bound,minZ:-bound,maxZ:bound}:bound;this.size=Math.floor((b.maxX-b.minX)/step)+1;this.height=Math.floor((b.maxZ-b.minZ)/step)+1;
    this.bounds=b;this.nodes=[];
    for(let z=0;z<this.height;z++)for(let x=0;x<this.size;x++){
      const p={x:x*step+b.minX,z:z*step+b.minZ};this.nodes.push({...p,open:canWalk(p.x,p.z,.55)});
    }
  }
  nearest(p){
    const x=Math.round((p.x-this.bounds.minX)/this.step),z=Math.round((p.z-this.bounds.minZ)/this.step),candidates=[];
    for(let dz=-4;dz<=4;dz++)for(let dx=-4;dx<=4;dx++){const xx=x+dx,zz=z+dz;if(xx<0||zz<0||xx>=this.size||zz>=this.height)continue;const i=zz*this.size+xx,n=this.nodes[i];if(n.open)candidates.push({i,d:(n.x-p.x)**2+(n.z-p.z)**2});}
    candidates.sort((a,b)=>a.d-b.d);return candidates.find(c=>segmentClear(p,this.nodes[c.i],this.canWalk,.45))?.i??-1;
  }
  find(start,end){
    if(segmentClear(start,end,this.canWalk,.45))return [{...start},{...end}];
    const source=this.nearest(start),target=this.nearest(end);if(source<0||target<0)return [];
    const size=this.nodes.length,g=new Float64Array(size).fill(Infinity),f=new Float64Array(size).fill(Infinity),parent=new Int32Array(size).fill(-1),closed=new Uint8Array(size),open=new MinHeap();
    const estimate=i=>Math.hypot(this.nodes[i].x-this.nodes[target].x,this.nodes[i].z-this.nodes[target].z);
    g[source]=0;f[source]=estimate(source);open.push({id:source,f:f[source]});
    while(open.items.length){const entry=open.pop(),cur=entry.id;if(closed[cur]||entry.f!==f[cur])continue;if(cur===target){let path=[{...end}],i=cur;while(i>=0){path.push({x:this.nodes[i].x,z:this.nodes[i].z});i=parent[i]}path.push({...start});path.reverse();const smooth=[path[0]];let anchor=0;while(anchor<path.length-1){let far=path.length-1;while(far>anchor+1&&!segmentClear(path[anchor],path[far],this.canWalk,.45))far--;smooth.push(path[far]);anchor=far}return smooth}
      closed[cur]=1;const x=cur%this.size,z=Math.floor(cur/this.size);
      for(let dz=-1;dz<=1;dz++)for(let dx=-1;dx<=1;dx++){if(!dx&&!dz)continue;let xx=x+dx,zz=z+dz;if(xx<0||zz<0||xx>=this.size||zz>=this.height)continue;let id=zz*this.size+xx;if(closed[id]||!this.nodes[id].open||!segmentClear(this.nodes[cur],this.nodes[id],this.canWalk,.55))continue;let cost=g[cur]+this.step*Math.hypot(dx,dz);if(cost<g[id]){parent[id]=cur;g[id]=cost;f[id]=cost+estimate(id);open.push({id,f:f[id]})}}
    }return [];
  }
}
export const routeLength=path=>path.slice(1).reduce((sum,p,i)=>sum+Math.hypot(p.x-path[i].x,p.z-path[i].z),0);
