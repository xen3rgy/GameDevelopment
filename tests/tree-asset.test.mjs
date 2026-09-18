import test from 'node:test';
import assert from 'node:assert/strict';
import {readFileSync} from 'node:fs';
import {treeTransformSpec,TREE_ASSET_BOUNDS} from '../dist/tree-system.js';

test('optimized Lindenstadt tree asset is a valid committed GLB',()=>{
 const b=readFileSync(new URL('../dist/assets/lindenstadt-tree.glb',import.meta.url));assert.equal(b.readUInt32LE(0),0x46546c67);assert.equal(b.readUInt32LE(4),2);assert.ok(b.length>2_000_000&&b.length<2_600_000);
});
test('city tree transforms stay human-scale, grounded and deterministic',()=>{
 for(const r of [1.8,2.4,2.8]){const a=treeTransformSpec(15,10,r,{ground:.18}),b=treeTransformSpec(15,10,r,{ground:.18});assert.deepEqual(a,b);assert.ok(a.height>5.4&&a.height<7.2);assert.ok(Math.abs((a.y+TREE_ASSET_BOUNDS.minY*a.scale)-.18)<1e-8);assert.ok(a.yaw>=0&&a.yaw<Math.PI*2)}
});
test('distant trees use the same asset with restrained height variation',()=>{
 const heights=Array.from({length:30},(_,i)=>treeTransformSpec(-244+i*16,180,i%4?2.05:2.1,{distant:true,seed:i,ground:-.05}).height);assert.ok(Math.min(...heights)>4.8);assert.ok(Math.max(...heights)<6.0);assert.ok(new Set(heights.map(v=>v.toFixed(3))).size>10);
});
