import test from 'node:test';
import assert from 'node:assert/strict';
import {GRASS_LUSH_URL} from '../dist/grass-texture.js';

test('0.7.5 Hi3D grass texture is a valid embedded WebP payload',()=>{
 assert.match(GRASS_LUSH_URL,/^data:image\/webp;base64,/);
 const data=Buffer.from(GRASS_LUSH_URL.split(',')[1],'base64');
 assert.ok(data.length>3000,'embedded texture should contain real image data');
 assert.equal(data.subarray(0,4).toString('ascii'),'RIFF');
 assert.equal(data.subarray(8,12).toString('ascii'),'WEBP');
});
