import test from 'node:test';
import assert from 'node:assert/strict';
import {readFileSync} from 'node:fs';
import {treeTransformSpec,TREE_ASSET_BOUNDS} from '../dist/tree-system.js';
import {treePitTransformSpec,TREE_PIT_BOUNDS,TREE_PIT_TARGET_SIZE} from '../dist/tree-pit-system.js';

test('optimized Lindenstadt tree asset is a valid committed GLB',()=>{
 const b=readFileSync(new URL('../dist/assets/lindenstadt-tree.glb',import.meta.url));assert.equal(b.readUInt32LE(0),0x46546c67);assert.equal(b.readUInt32LE(4),2);assert.ok(b.length>2_000_000&&b.length<2_600_000);
});
test('city tree transforms stay human-scale, deterministic and seated into the tree pit',()=>{
 for(const r of [1.8,2.4,2.8]){const a=treeTransformSpec(15,10,r,{ground:.18}),b=treeTransformSpec(15,10,r,{ground:.18});assert.deepEqual(a,b);assert.ok(a.height>5.4&&a.height<7.2);assert.equal(a.sink,.055);assert.ok(Math.abs((a.y+TREE_ASSET_BOUNDS.minY*a.scale)-(.18-a.sink))<1e-8);assert.ok(a.yaw>=0&&a.yaw<Math.PI*2)}
});
test('distant trees use the same asset with restrained height variation and no artificial tree-pit sink',()=>{
 const specs=Array.from({length:30},(_,i)=>treeTransformSpec(-244+i*16,180,i%4?2.05:2.1,{distant:true,seed:i,ground:-.05})),heights=specs.map(v=>v.height);assert.ok(Math.min(...heights)>4.8);assert.ok(Math.max(...heights)<6.0);assert.ok(new Set(heights.map(v=>v.toFixed(3))).size>10);assert.ok(specs.every(v=>v.sink===0));
});


test('optimized stone-bordered tree pit GLB is committed and browser-sized',()=>{
 const b=readFileSync(new URL('../dist/assets/lindenstadt-tree-pit.glb',import.meta.url));assert.equal(b.readUInt32LE(0),0x46546c67);assert.equal(b.readUInt32LE(4),2);assert.ok(b.length>1_500_000&&b.length<2_200_000);
 const jsonLength=b.readUInt32LE(12),json=JSON.parse(b.subarray(20,20+jsonLength).toString('utf8').replace(/\0+$/,'').trim()),primitive=json.meshes[0].primitives[0],indices=json.accessors[primitive.indices];
 assert.ok(indices.count/3<=31_000,'tree pit should stay close to the optimized ~30k triangle target');assert.equal(json.images.length,3);
});
test('new GLB tree pits match the old 1.92m footprint and sit directly in the pavement',()=>{
 const width=TREE_PIT_BOUNDS.maxX-TREE_PIT_BOUNDS.minX,depth=TREE_PIT_BOUNDS.maxZ-TREE_PIT_BOUNDS.minZ,t=treePitTransformSpec(15,10,{ground:.30});
 assert.ok(Math.abs(Math.max(width,depth)*t.scale-TREE_PIT_TARGET_SIZE)<1e-9);assert.equal(t.y,.30);assert.ok([0,.5,1,1.5].some(q=>Math.abs(t.yaw-q*Math.PI)<1e-9));
});
test('procedural metal-and-mulch tree pit was fully replaced by the GLB system',()=>{
 const source=readFileSync(new URL('../dist/art.js',import.meta.url),'utf8');assert.ok(!source.includes('treePit(x,z){'));assert.match(source,/treePitSystem\.add\(x,z\)/);
});
