import {createServer} from 'node:http';
import {createReadStream,statSync} from 'node:fs';
import {resolve,extname,sep} from 'node:path';
import {fileURLToPath} from 'node:url';
const toolRoot=fileURLToPath(new URL('.',import.meta.url)),root=resolve(toolRoot,'../dist'),videos=process.argv.slice(3),port=Number(process.argv[2]||8765);
const types={'.html':'text/html; charset=utf-8','.js':'text/javascript; charset=utf-8','.css':'text/css; charset=utf-8','.png':'image/png','.glb':'model/gltf-binary','.mp4':'video/mp4'};
createServer((req,res)=>{
 try{
  const url=new URL(req.url,'http://localhost'),name=decodeURIComponent(url.pathname),video=/^\/__video\/(\d+)$/.exec(name);
  const special=name==='/__qa'||name==='/__media';
  const file=video?videos[Number(video[1])]:name==='/__qa'?resolve(toolRoot,'visual-qa.html'):name==='/__media'?resolve(toolRoot,'media-review.html'):resolve(root,'.'+(name==='/'?'/index.html':name));
  if(!file||(!video&&!special&&!file.startsWith(root+sep))){res.writeHead(403);res.end();return;}
  const size=statSync(file).size,range=/bytes=(\d+)-(\d*)/.exec(req.headers.range||''),start=range?Number(range[1]):0,end=range?Math.min(size-1,range[2]?Number(range[2]):size-1):size-1;
  if(start> end){res.writeHead(416);res.end();return;}
  res.writeHead(range?206:200,{'Content-Type':types[extname(file)]||'application/octet-stream','Content-Length':end-start+1,'Cache-Control':'no-store','Accept-Ranges':'bytes',...(range?{'Content-Range':`bytes ${start}-${end}/${size}`}:{})});
  createReadStream(file,{start,end}).pipe(res);
 }catch{res.writeHead(404);res.end('Not found');}
}).on('error',error=>{console.error(error.code==='EADDRINUSE'?`Port ${port} is already in use. Open http://127.0.0.1:${port}/ or choose another port.`:error);process.exitCode=1;}).listen(port,'127.0.0.1',()=>console.log(`Local preview http://127.0.0.1:${port}/ · visual checks /__qa · Ctrl+C to stop`));
