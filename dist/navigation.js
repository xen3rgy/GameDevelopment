import {segmentClear} from './movement.js?v=0.8.1';
// A* on a fixed grid. The graph is cached; each route is smoothed only through free space.
export class CityNavigation {
  constructor(canWalk,step=4,bound=116){
    this.canWalk=canWalk;this.step=step;this.bound=bound;const b=typeof bound==='number'?{minX:-bound,maxX:bound,minZ:-bound,maxZ:bound}:bound;this.size=Math.floor((b.maxX-b.minX)/step)+1;this.height=Math.floor((b.maxZ-b.minZ)/step)+1;
    this.nodes=[];
    for(let z=0;z<this.height;z++)for(let x=0;x<this.size;x++){
      const p={x:x*step+b.minX,z:z*step+b.minZ};this.nodes.push({...p,open:canWalk(p.x,p.z,.55)});
    }
  }
  nearest(p){let best=-1,dist=Infinity;for(let i=0;i<this.nodes.length;i++){let n=this.nodes[i];let d=Math.hypot(n.x-p.x,n.z-p.z);if(n.open&&d<dist&&segmentClear(p,n,this.canWalk,.45)){best=i;dist=d}}return best}
  find(start,end){
    if(segmentClear(start,end,this.canWalk,.45))return [{...start},{...end}];
    const source=this.nearest(start),target=this.nearest(end);if(source<0||target<0)return [];
    const size=this.nodes.length,g=new Float64Array(size).fill(Infinity),f=new Float64Array(size).fill(Infinity),parent=new Int32Array(size).fill(-1),closed=new Uint8Array(size),open=new Set([source]);
    const estimate=i=>Math.hypot(this.nodes[i].x-this.nodes[target].x,this.nodes[i].z-this.nodes[target].z);
    g[source]=0;f[source]=estimate(source);
    while(open.size){let cur=-1,score=Infinity;for(const i of open)if(f[i]<score){score=f[i];cur=i}if(cur===target){let path=[{...end}],i=cur;while(i>=0){path.push({x:this.nodes[i].x,z:this.nodes[i].z});i=parent[i]}path.push({...start});path.reverse();const smooth=[path[0]];let anchor=0;while(anchor<path.length-1){let far=path.length-1;while(far>anchor+1&&!segmentClear(path[anchor],path[far],this.canWalk,.45))far--;smooth.push(path[far]);anchor=far}return smooth}
      open.delete(cur);closed[cur]=1;const x=cur%this.size,z=Math.floor(cur/this.size);
      for(let dz=-1;dz<=1;dz++)for(let dx=-1;dx<=1;dx++){if(!dx&&!dz)continue;let xx=x+dx,zz=z+dz;if(xx<0||zz<0||xx>=this.size||zz>=this.height)continue;let id=zz*this.size+xx;if(closed[id]||!this.nodes[id].open||!segmentClear(this.nodes[cur],this.nodes[id],this.canWalk,.55))continue;let cost=g[cur]+this.step*Math.hypot(dx,dz);if(cost<g[id]){parent[id]=cur;g[id]=cost;f[id]=cost+estimate(id);open.add(id)}}
    }return [];
  }
}
export const routeLength=path=>path.slice(1).reduce((sum,p,i)=>sum+Math.hypot(p.x-path[i].x,p.z-path[i].z),0);
