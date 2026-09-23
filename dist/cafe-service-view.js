import {CAFE_MENU,guestPatience} from './cafe.js?v=0.8.1';

export function guestServiceView(g){
 const paying=g.stage==='paying',fraction=guestPatience(g),seconds=Math.max(0,Math.ceil(g.patience-g.wait));
 const urgent=!paying&&(fraction<=.2||seconds<=10);
 const color=paying?'#f2d399':urgent?'#ffab96':fraction<=.5?'#ffdc88':'#adf0cd';
 const view={visible:['order','accepted','paying'].includes(g.stage),paying,fraction,seconds,urgent,color,
  seat:'T'+(g.seat+1)+' · '+(g.side===1?'B':'A'),
  value:paying?(g.price/100).toFixed(2).replace('.',',')+' €':seconds+' min',
  label:paying?'Bitte kassieren':g.stage==='order'?'Möchte bestellen':(CAFE_MENU[g.recipe]?.name||'Bestellung')+' ausstehend',
  detail:paying?'RECHNUNG OFFEN':urgent?'GEHT BALD · BITTE BEDIENEN':'VERBLEIBENDE GEDULD'};
 view.key=[g.stage,seconds,Math.round(fraction*100),g.price,g.recipe,g.seat,g.side].join(':');return view;
}

// All parts share one texture: the panel can never blend over its own progress bar.
// The existing sign kit supplies the canvas; update it only when the shown values change.
export function paintGuestBadge(texture,view){
 const canvas=texture?.image,ctx=canvas?.getContext?.('2d');if(!ctx)return;
 if(canvas.width!==640||canvas.height!==224){canvas.width=640;canvas.height=224}
 ctx.clearRect(0,0,640,224);ctx.fillStyle='#10292f';ctx.fillRect(0,0,640,224);
 ctx.strokeStyle=view.color;ctx.lineWidth=6;ctx.strokeRect(3,3,634,218);
 ctx.textBaseline='alphabetic';ctx.textAlign='left';ctx.fillStyle='#f7f4e9';ctx.font='600 34px Segoe UI, Arial, sans-serif';ctx.fillText(view.seat,28,48);
 ctx.textAlign='right';ctx.font='700 42px Segoe UI, Arial, sans-serif';ctx.fillStyle=view.color;ctx.fillText(view.value,612,49);
 ctx.textAlign='left';ctx.font='500 31px Segoe UI, Arial, sans-serif';ctx.fillStyle='#f0f4ee';ctx.fillText(view.label,28,100,585);
 if(!view.paying){
  ctx.fillStyle='#4b656a';ctx.fillRect(28,126,584,34);
  ctx.fillStyle=view.color;ctx.fillRect(28,126,584*view.fraction,34);
  ctx.strokeStyle='#d7e7e8';ctx.lineWidth=2;ctx.strokeRect(28,126,584,34);
 }else{ctx.strokeStyle='#6d7f78';ctx.lineWidth=2;ctx.beginPath();ctx.moveTo(28,144);ctx.lineTo(612,144);ctx.stroke()}
 ctx.font='600 25px Segoe UI, Arial, sans-serif';ctx.fillStyle=view.color;ctx.fillText(view.detail,28,200,585);
 texture.needsUpdate=true;
}
