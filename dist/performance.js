export class FrameMetrics{
 constructor(){this.frames=[];this.cpu=[];this.last=null;this.drawCalls=0;this.triangles=0;}
 record(now,cpu,info){if(this.last!==null){const ms=now-this.last;if(ms>0&&ms<1000){this.frames.push(ms);this.cpu.push(cpu);if(this.frames.length>1800){this.frames.shift();this.cpu.shift();}}}this.last=now;this.drawCalls=info?.calls||0;this.triangles=info?.triangles||0;}
 reset(){this.frames=[];this.cpu=[];this.last=null;}
 summary(){const quantile=(a,q)=>{const b=[...a].sort((a,b)=>a-b);return b[Math.min(b.length-1,Math.floor(b.length*q))]||0;};return {samples:this.frames.length,fps:1000/(quantile(this.frames,.5)||1000),p95:quantile(this.frames,.95),cpu:quantile(this.cpu,.5),drawCalls:this.drawCalls,triangles:this.triangles};}
}
