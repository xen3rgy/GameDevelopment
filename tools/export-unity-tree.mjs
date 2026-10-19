// Lossless geometry/UV transfer of our existing GLB. Unity imports OBJ natively.
import fs from 'node:fs';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const source = fs.readFileSync(path.join(root, 'dist/assets/lindenstadt-tree.glb'));
const jsonLength = source.readUInt32LE(12);
const gltf = JSON.parse(source.subarray(20, 20 + jsonLength).toString());
const binaryStart = 20 + jsonLength + 8;
const binary = source.subarray(binaryStart);
const folder = path.join(root, 'unity/ZeroRise/Assets/ZeroRise/Art/Trees');
fs.mkdirSync(folder, {recursive:true});
function accessor(id) {
  const a = gltf.accessors[id], view = gltf.bufferViews[a.bufferView];
  const size = {5121:1,5123:2,5125:4,5126:4}[a.componentType];
  const dimensions = {SCALAR:1,VEC2:2,VEC3:3}[a.type];
  const read = {5121:'readUInt8',5123:'readUInt16LE',5125:'readUInt32LE',5126:'readFloatLE'}[a.componentType];
  if (!size || !dimensions || a.sparse) throw Error('Unsupported tree accessor');
  return Array.from({length:a.count}, (_, i) => Array.from({length:dimensions}, (_, c) =>
    binary[read]((view.byteOffset || 0) + (a.byteOffset || 0) + i * (view.byteStride || size * dimensions) + c * size)));
}
const primitive = gltf.meshes[0].primitives[0];
if (gltf.meshes.length !== 1 || primitive.mode && primitive.mode !== 4) throw Error('Unexpected tree geometry');
const positions = accessor(primitive.attributes.POSITION), uv = accessor(primitive.attributes.TEXCOORD_0), indices = accessor(primitive.indices).flat();
const lines = ['# ZERO RISE existing tree; glTF right-handed to Unity left-handed', 'mtllib Linden.mtl', 'o Linden', 'usemtl Linden'];
positions.forEach(p => lines.push(`v ${p[0]} ${p[1]} ${-p[2]}`));
uv.forEach(p => lines.push(`vt ${p[0]} ${1-p[1]}`));
for (let i=0;i<indices.length;i+=3) lines.push('f '+[indices[i],indices[i+2],indices[i+1]].map(v=>`${v+1}/${v+1}`).join(' '));
fs.writeFileSync(path.join(folder, 'Linden.obj'), lines.join('\n')+'\n');
fs.writeFileSync(path.join(folder, 'Linden.mtl'), 'newmtl Linden\nKd 1 1 1\nmap_Kd Linden.jpg\n');
const imageView = gltf.bufferViews[gltf.images[0].bufferView];
fs.writeFileSync(path.join(folder,'Linden.jpg'),binary.subarray(imageView.byteOffset, imageView.byteOffset+imageView.byteLength));
fs.writeFileSync(path.join(folder,'SOURCE.md'),'Geometry and diffuse image copied from dist/assets/lindenstadt-tree.glb. Original provenance remains that of the browser asset; verify its original commercial license before shipping on Steam. No third-party assets downloaded for this port.\n');
console.log(`Unity tree exported: ${positions.length} vertices, ${indices.length/3} triangles.`);
