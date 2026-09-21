import {execFileSync} from 'node:child_process';
import {existsSync,readdirSync,readFileSync,statSync} from 'node:fs';
import {dirname,extname,join,resolve} from 'node:path';
import {fileURLToPath} from 'node:url';

const root=resolve(dirname(fileURLToPath(import.meta.url)),'..');
const dist=join(root,'dist');
const walk=dir=>readdirSync(dir).flatMap(name=>{const p=join(dir,name);return statSync(p).isDirectory()?walk(p):[p]});
const js=walk(dist).filter(p=>extname(p)==='.js');

for(const file of js)execFileSync(process.execPath,['--check',file],{stdio:'inherit'});

const missing=[];
for(const file of js){
 const source=readFileSync(file,'utf8');
 const re=/(?:from\s*|import\s*\()\s*['"]([^'"]+)['"]/g;
 for(const match of source.matchAll(re)){
  const spec=match[1].split('?')[0];
  if(!spec.startsWith('.'))continue;
  const target=resolve(dirname(file),spec);
  if(!existsSync(target))missing.push(file.slice(root.length+1)+' -> '+spec);
 }
}
const html=readFileSync(join(dist,'index.html'),'utf8');
for(const match of html.matchAll(/(?:src|href)=["']([^"'#]+)["']/g)){
 const spec=match[1].split('?')[0];
 if(spec.startsWith('.')&&!existsSync(resolve(dist,spec)))missing.push('dist/index.html -> '+spec);
}
for(const rel of [
 'dist/vendor/three.module.js',
 'dist/assets/civic-limestone-069.png',
 'dist/assets/lindenstadt-mural.png',
 'dist/assets/station-brick-069.png',
 'dist/assets/lindenstadt-tree.glb',
 'dist/assets/lindenstadt-tree-pit.glb'
])if(!existsSync(join(root,rel)))missing.push(rel);
const treeAsset=join(root,'dist/assets/lindenstadt-tree.glb');
if(existsSync(treeAsset)){const b=readFileSync(treeAsset);if(b.length<2_000_000||b.length>2_600_000||b.readUInt32LE(0)!==0x46546c67||b.readUInt32LE(4)!==2)missing.push('dist/assets/lindenstadt-tree.glb is not the expected optimized GLB asset')}
const treePitAsset=join(root,'dist/assets/lindenstadt-tree-pit.glb');
if(existsSync(treePitAsset)){const b=readFileSync(treePitAsset);if(b.length<1_500_000||b.length>2_200_000||b.readUInt32LE(0)!==0x46546c67||b.readUInt32LE(4)!==2)missing.push('dist/assets/lindenstadt-tree-pit.glb is not the expected optimized GLB asset')}
if(existsSync(join(root,'.zero-rise-parts'))&&walk(join(root,'.zero-rise-parts')).length)missing.push('.zero-rise-parts must not contain legacy runtime fragments after assets are committed directly');

if(missing.length){console.error('Static validation failed:\n'+missing.map(v=>' - '+v).join('\n'));process.exit(1)}
console.log('Static validation passed for '+js.length+' JavaScript modules and critical runtime assets.');
