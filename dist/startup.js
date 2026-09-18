// Catch module-graph failures too: app.js cannot handle a failed static import itself.
import('./app.js?v=0.7.2-start0800-fix1').catch(error=>{
 console.error('ZERO // RISE could not load its game modules:',error);
 const loading=document.getElementById('loading');
 if(!loading)return;
 loading.classList.remove('hidden');
 const title=document.createElement('span');title.className='brand';title.textContent='ZERO // RISE';
 const message=document.createElement('p');message.textContent='Die Spieldateien konnten nicht geladen werden.';
 const detail=document.createElement('p');detail.textContent='Bitte lade die Seite erneut. Dein gespeicherter Fortschritt bleibt erhalten.';detail.style.cssText='max-width:430px;padding:0 20px;text-align:center;line-height:1.6';
 const retry=document.createElement('button');retry.type='button';retry.className='primary';retry.textContent='Erneut laden';retry.addEventListener('click',()=>location.reload());
 loading.replaceChildren(title,message,detail,retry);
});
