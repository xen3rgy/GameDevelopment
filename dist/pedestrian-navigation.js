import {CityNavigation} from './navigation.js?v=0.8.0';
export const PEDESTRIAN_RADIUS=.4;
// Dense swept-body probes with a safety margin: the old 45 cm samples skipped
// thin blocked sections at kerbs and furniture corners, despite valid endpoints.
export function pedestrianSegmentClear(a,b,canWalk,radius=PEDESTRIAN_RADIUS){
 const length=Math.hypot(b.x-a.x,b.z-a.z),steps=Math.max(1,Math.ceil(length/.12));
 for(let i=1;i<=steps;i++){
  const t=i/steps,margin=Math.min(.07,length*t*.2,length*(1-t)*.2);
  if(!canWalk(a.x+(b.x-a.x)*t,a.z+(b.z-a.z)*t,radius+margin))return false;
 }
 return true;
}
const segmentClear=pedestrianSegmentClear;

// Immutable pavement graph. Cache edges/components once, not inside each citizen's A*.
export class PedestrianNavigation extends CityNavigation{
 constructor(canWalk){
  const bounds={minX:-196,maxX:100,minZ:-60.5,maxZ:60.5};super(canWalk,1,bounds);this.bounds=bounds;
  this.edges=this.nodes.map(()=>[]);this.component=new Int32Array(this.nodes.length).fill(-1);
  for(let i=0;i<this.nodes.length;i++){
   const p=this.nodes[i];if(!p.open)continue;const x=i%this.size,z=Math.floor(i/this.size);
   for(const [dx,dz] of [[1,0],[0,1],[1,1],[-1,1]]){const xx=x+dx,zz=z+dz,j=zz*this.size+xx;
    if(xx<0||xx>=this.size||zz>=this.height||!this.nodes[j].open||!segmentClear(p,this.nodes[j],canWalk,.4))continue;
    const cost=Math.hypot(dx,dz)*this.step;this.edges[i].push([j,cost]);this.edges[j].push([i,cost]);
   }
  }
  let component=0;for(let i=0;i<this.nodes.length;i++){if(!this.nodes[i].open||this.component[i]>=0)continue;const queue=[i];this.component[i]=component;for(let at=0;at<queue.length;at++)for(const [j] of this.edges[queue[at]])if(this.component[j]<0){this.component[j]=component;queue.push(j);}component++;}
 }
 nearest(p,canWalk=this.canWalk){
  const cx=Math.round((p.x-this.bounds.minX)/this.step),cz=Math.round((p.z-this.bounds.minZ)/this.step),candidates=[];
  for(let dz=-6;dz<=6;dz++)for(let dx=-6;dx<=6;dx++){const x=cx+dx,z=cz+dz;if(x<0||z<0||x>=this.size||z>=this.height)continue;const i=z*this.size+x,n=this.nodes[i];if(n.open)candidates.push({i,d:(n.x-p.x)**2+(n.z-p.z)**2});}
  candidates.sort((a,b)=>a.d-b.d);return candidates.find(c=>segmentClear(p,this.nodes[c.i],canWalk,PEDESTRIAN_RADIUS))?.i??-1;
 }
 find(start,end,obstacles=[]){
  const canWalk=obstacles.length?(x,z,r)=>this.canWalk(x,z,r)&&obstacles.every(q=>{
   const before=Math.hypot(start.x-q.x,start.z-q.z),after=Math.hypot(x-q.x,z-q.z),space=(q.radius??.34)+r+.04;
   return after>=space||(before<space&&after>before+1e-6);
  }):this.canWalk;
  if(!canWalk(end.x,end.z,PEDESTRIAN_RADIUS))return [];
  if(segmentClear(start,end,canWalk,PEDESTRIAN_RADIUS))return [{x:start.x,z:start.z},{x:end.x,z:end.z}];
  const source=this.nearest(start,canWalk),target=this.nearest(end,canWalk);if(source<0||target<0||this.component[source]!==this.component[target])return [];
  const g=new Float64Array(this.nodes.length).fill(Infinity),parent=new Int32Array(this.nodes.length).fill(-1),closed=new Uint8Array(this.nodes.length),heap=[];
  const estimate=i=>Math.hypot(this.nodes[i].x-this.nodes[target].x,this.nodes[i].z-this.nodes[target].z);
  const push=(i,score)=>{let at=heap.length;heap.push({i,score});while(at){const up=(at-1)>>1;if(heap[up].score<=score)break;[heap[up],heap[at]]=[heap[at],heap[up]];at=up;}};
  const pop=()=>{const value=heap[0],last=heap.pop();if(heap.length){heap[0]=last;let at=0;for(;;){let child=at*2+1;if(child>=heap.length)break;if(child+1<heap.length&&heap[child+1].score<heap[child].score)child++;if(heap[at].score<=heap[child].score)break;[heap[at],heap[child]]=[heap[child],heap[at]];at=child;}}return value.i;};
  g[source]=0;push(source,estimate(source));
  while(heap.length){const i=pop();if(closed[i])continue;closed[i]=1;
   if(i===target){const path=[{x:end.x,z:end.z}];for(let j=i;j>=0;j=parent[j])path.push({x:this.nodes[j].x,z:this.nodes[j].z});path.push({x:start.x,z:start.z});path.reverse();
    const smooth=[path[0]];let a=0;while(a<path.length-1){let b=Math.min(a+12,path.length-1);while(b>a+1&&!segmentClear(path[a],path[b],canWalk,PEDESTRIAN_RADIUS))b--;smooth.push(path[b]);a=b;}return smooth;
   }
   for(const [j,cost] of this.edges[i]){if(closed[j]||g[i]+cost>=g[j]||(obstacles.length&&!segmentClear(this.nodes[i],this.nodes[j],canWalk,PEDESTRIAN_RADIUS)))continue;g[j]=g[i]+cost;parent[j]=i;push(j,g[j]+estimate(j));}
  }return [];
 }
}
