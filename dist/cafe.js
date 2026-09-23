import {CAFE_OPEN,CAFE_CLOSE,CAFE_SHIFT} from './game-time.js?v=0.8.1';
import {newCafeService,ensureCafeService,recordCafeService,cafeServiceSummary,validateCafeService} from './cafe-report.js?v=0.8.1';
import {CAFE_POINTS,STAFF_HOME,staffPath,guestWalkDuration} from './cafe-layout.js?v=0.8.1';
export {CAFE_TABLES,CAFE_POINTS,CAFE_FIXTURES,guestWalkDuration} from './cafe-layout.js?v=0.8.1';
export const CAFE_MENU={espresso:{name:'Espresso',price:340,cost:60,seconds:4,dining:16},latte:{name:'Milchkaffee',price:450,cost:110,seconds:6,dining:24},breakfast:{name:'Frühstück',price:890,cost:260,seconds:8,dining:32}};
export const guestPlace=g=>g.seat*2+(g.side===1?1:0);
export function cafeGuestIntent(c,g){
 if(c.carrying)return c.tray===g.id&&g.stage==='accepted'?'Servieren':null;
 return g.stage==='order'?'Bestellung aufnehmen':g.stage==='paying'?'Kassieren':null;
}
export const waitingCafeOrders=c=>c.guests.filter(g=>g.stage==='accepted'&&g.id!==c.staffCarry).sort((a,b)=>b.wait-a.wait||a.id-b.id);
export const guestLabel=g=>'Tisch '+(g.seat+1)+' · Platz '+(g.side===1?'B':'A');
export function newCafe(){return {schema:4,economyModel:2,service:newCafeService(),day:0,phase:'idle',elapsed:0,clockRate:1,legacyMinutes:0,duration:CAFE_SHIFT,closeMinute:null,nextGuest:3,serial:0,rng:1,guests:[],dirty:[0,0,0,0,0,0],dishes:[null,null,null,null,null,null],prep:null,tray:null,carrying:false,staffCarry:null,employees:[],served:0,lost:0,revenue:0,tips:0,ingredients:0,receipts:[],openingRevenue:0,openingTips:0,config:null,lastReport:null}}
export function nearCafe(s,id){const p=CAFE_POINTS[id];return s.inside&&s.interior==='cafe'&&p&&Math.hypot(s.position.x-p.x,s.position.z-p.z)<2}
export const STAFF_TRAINING_COST=[0,15000,30000];
export const staffLevel=(c,role)=>c.config?.staffLevels?.[role]||1;
export const staffWorkRate=(c,role)=>[1,1.5,2.1][staffLevel(c,role)-1];
export const staffWalkSpeed=(c,role)=>[1.15,1.65,2.15][staffLevel(c,role)-1];
export const cafeDayShare=c=>Math.max(0,Math.min(1,(c.elapsed*(c.clockRate??.5)+(c.legacyMinutes||0))/720));
export const guestPatience=g=>Math.max(0,Math.min(1,1-g.wait/g.patience));
export const validStaffLevels=v=>Array.isArray(v)&&v.length===3&&v.every(n=>Number.isInteger(n)&&n>=1&&n<=3);
function legacyCafeCosts(c){return Math.round((2450+(c.config?.staff||0)*4500+(c.config?.marketing?1500:0))*cafeDayShare(c))+c.ingredients}
function legacyCafeDayResult(c,b,d,skill){
 const fraction=cafeDayShare(c),remaining=b.open?1-fraction:0;
 const demand=(c.config.economy==='Aufschwung'?1.22:c.config.economy==='Rezession'?.75:1)*(1+(b.quality-1)*.18)*(b.marketing?1.2:1)*(1+b.staff*.32)*(1+(skill/100)*.08)*Math.max(.25,1-(b.price-1)*1.8);
 const automaticSales=Math.round(d.baseSales*demand*b.price*(b.staff===0?.65:1)*remaining);
 const manualSales=c.revenue+c.tips;
 // The shift reserves this day's stock. Fixed overhead and daily wages are charged once.
 const fixedCosts=Math.round(d.baseCosts*.35)+Math.max(b.staff,c.config.staff)*4500+(b.marketing||c.config.marketing?1500:0);
 const automaticCosts=Math.round(d.baseCosts*.65*remaining);
 const costs=fixedCosts+automaticCosts+c.ingredients,sales=manualSales+automaticSales;
 return {sales,costs,profit:sales-costs,manualSales,automaticSales,fixedCosts,automaticCosts,ingredients:c.ingredients,manualMinutes:Math.round(c.elapsed*(c.clockRate??.5)+(c.legacyMinutes||0))};
}
// Game-economy wages: €3.75 per game hour, with a two-hour call-out minimum.
// Supplies are prepaid when buying stock; ingredients are billed by actual preparation.
export const CAFE_HOURLY_WAGE=375,CAFE_FIXED_COST=2450,CAFE_SUPPLIES=833;
export function cafeBreakdown(c){
 const hours=Math.max(2,Math.min(8,(c.elapsed*(c.clockRate??1)+(c.legacyMinutes||0))/60));
 return {ingredients:c.ingredients,wages:Math.round((c.config?.staff||0)*CAFE_HOURLY_WAGE*hours),advertising:c.config?.marketing?1500:0,overhead:CAFE_FIXED_COST,supplies:CAFE_SUPPLIES};
}
export function cafeCosts(c){return c.economyModel===2?Object.values(cafeBreakdown(c)).reduce((a,b)=>a+b,0):legacyCafeCosts(c)}
export function cafeDayResult(c,b,d,skill){
 if(c&&c.economyModel!==2)return legacyCafeDayResult(c,b,d,skill);
 const detail=c?cafeBreakdown(c):{ingredients:0,wages:0,advertising:b.marketing?1500:0,overhead:CAFE_FIXED_COST,supplies:0};
 const sales=c?c.revenue+c.tips:0,costs=Object.values(detail).reduce((a,b)=>a+b,0);
 return {economyModel:2,sales,costs,profit:sales-costs,manualSales:sales,automaticSales:0,fixedCosts:detail.wages+detail.advertising+detail.overhead,automaticCosts:0,ingredients:detail.ingredients,manualMinutes:c?Math.round(c.elapsed):0,...detail,cashAdjustment:detail.supplies,cashMovement:sales-costs+detail.supplies};
}
export function cafeDemand(config){
 return (config.economy==='Aufschwung'?1.22:config.economy==='Rezession'?.75:1)*(1+(config.quality-1)*.18)*(config.marketing?1.2:1)*(1+Math.min(5,(config.skill||0)/100)*.08)*Math.max(.25,1-(config.price-1)*1.8);
}
function settlePayment(c,g,source='closing'){
 if(!g.served||g.paid||c.receipts.some(r=>r.id===g.id))return false;
 ensureCafeService(c).payments[source]++;g.time=.9;
 const tip=g.tipPending||0;g.paid=true;c.revenue+=g.price;c.tips+=tip;
 c.receipts.push({id:g.id,seat:g.seat,side:g.side,recipe:g.recipe,amount:g.price,tip});return true;
}
export function finishCafe(s,reason='Feierabend'){
 const c=s.cafe;if(c?.phase!=='open')return false;
 for(const g of c.guests)if(g.served&&!g.paid)settlePayment(c,g);
 const unserved=c.guests.filter(g=>['arriving','order','accepted'].includes(g.stage)).length;ensureCafeService(c).closedUnserved+=unserved;c.lost+=unserved;
 c.phase='closed';c.staffCarry=null;for(const e of c.employees){e.status='leaving';e.task=null;e.wait=0;e.route=staffPath(e,{x:300+(e.role-1)*.28,z:87.65}).slice(1)}c.nextGuest=Math.max(0,c.nextGuest);c.prep=null;c.tray=null;c.carrying=false;c.guests=[];c.dirty.fill(0);c.dishes.fill(null);
 const wages=Math.round((c.config.staff||0)*4500*cafeDayShare(c)),advertising=Math.round((c.config.marketing?1500:0)*cafeDayShare(c));
 const breakdown=c.economyModel===2?cafeBreakdown(c):{ingredients:c.ingredients,wages,advertising,overhead:cafeCosts(c)-c.ingredients-wages-advertising};
 c.lastReport={service:cafeServiceSummary(c),duration:c.elapsed,team:{staff:c.config.staff,levels:[...c.config.staffLevels]},breakdown,economyModel:c.economyModel,allocation:'shift',day:c.day,served:c.served,lost:c.lost,sales:c.revenue,tips:c.tips,costs:cafeCosts(c),profit:c.revenue+c.tips-cafeCosts(c),reason};return true;
}
function clearPlace(c,place){c.dirty[place]=0;c.dishes[place]=null}
function vacant(c,place){return !c.guests.some(g=>guestPlace(g)===place)}
function actOnGuest(model,g){
 const c=model.s.cafe;if(!g||!nearCafe(model.s,'cafeTable'+g.seat))return false;
 if(g.stage==='paying'){if(!settlePayment(c,g,'player'))return false;g.stage='finished';model.emit(guestLabel(g)+': Rechnung bezahlt.','success');return true}
 if(g.stage==='order'){g.stage='accepted';model.emit(guestLabel(g)+': '+CAFE_MENU[g.recipe].name+' · '+(g.price/100).toFixed(2)+' €.');return true}
 if(g.stage==='accepted'&&c.tray===g.id&&c.carrying)return serveCafe(model,g,true);
 model.emit(g.stage==='arriving'?'Der Gast kommt gerade an.':g.stage==='accepted'?'Dieser Gast wartet auf '+CAFE_MENU[g.recipe].name+'.':'Dieser Gast wurde bereits bedient.');return false;
}
export function cafeAction(model,action,arg){
 const s=model.s,c=s.cafe??=newCafe(),b=s.businesses.cafe,fail=text=>{model.emit(text);return false};
 if(!b)return fail('Übernimm zuerst das Café Morgen im Betriebsbuch.');
 if(action==='start'){
  if(!nearCafe(s,'cafeOffice'))return false;
  if(c.phase==='open'||c.day===s.day)return fail('Eine eigene Schicht pro Spieltag. Morgen kannst du wieder öffnen.');
  if(!b.open)return fail('Öffne den Betrieb zuerst in der Verwaltung.');
  if(b.stock<1)return fail('Bestelle zuerst mindestens einen Vorratstag.');
  if(s.minute<CAFE_OPEN||s.minute>=CAFE_CLOSE)return fail('Das Café öffnet von 08:00 bis 20:00.');
  if(s.minute>CAFE_CLOSE-120)return fail('Für einen neuen Betriebstag brauchst du mindestens zwei Stunden bis zum Ladenschluss. Öffne spätestens um 18:00.');
  if(s.job?.carrying)return fail('Lege zuerst deine Auftragskiste ab.');
  s.cafe={...newCafe(),rng:(Math.floor(Math.random()*4294967295)+1)>>>0||1,lastReport:c.lastReport,day:s.day,phase:'open',duration:Math.min(CAFE_SHIFT,CAFE_CLOSE-s.minute),closeMinute:Math.min(CAFE_CLOSE,s.minute+CAFE_SHIFT),config:{price:b.price,quality:b.quality,staff:b.staff,staffLevels:[...(b.staffLevels||[1,1,1])],marketing:b.marketing,economy:s.economy,skill:s.skills.business}};ensureCafeStaff(s.cafe);for(const e of s.cafe.employees){e.status='entering';e.x=300+(e.role-1)*.28;e.z=87.65;e.wait=e.role*.8;e.route=staffPath(e,STAFF_HOME[e.role]).slice(1)}b.stock--;
  model.emit('Schicht geöffnet. Sieh einen Gast an und drücke E. Der Balken zeigt seine verbleibende Geduld.','success');return true;
 }
 if(action==='finish'){if(!nearCafe(s,'cafeOffice'))return false;return finishCafe(s)}
 if(c.phase!=='open')return false;
 if(action==='guest')return actOnGuest(model,c.guests.find(g=>g.id===Number(arg)));
 if(action==='clean'){
  const place=Number(arg);if(!Number.isInteger(place)||place<0||place>5||!nearCafe(s,'cafeTable'+Math.floor(place/2))||!c.dirty[place]||!vacant(c,place))return false;
  clearPlace(c,place);s.needs.hygiene=Math.max(0,s.needs.hygiene-1);model.emit('Gedeck abgeräumt. Der Platz ist wieder frei.','success');return true;
 }
 if(action==='table'){
  const seat=Number(arg);if(!Number.isInteger(seat)||!nearCafe(s,'cafeTable'+seat))return false;
  const guests=c.guests.filter(g=>g.seat===seat),g=guests.find(g=>g.id===c.tray&&c.carrying)||guests.find(g=>g.stage==='paying')||guests.find(g=>g.stage==='order');
  if(g)return actOnGuest(model,g);
  const place=c.dirty.findIndex((d,i)=>d&&Math.floor(i/2)===seat&&vacant(c,i));
  if(place>=0)return cafeAction(model,'clean',place);
  return fail(guests.length?'Die Bestellungen dieses Tisches findest du in der Gästeübersicht.':'Dieser Tisch ist frei.');
 }
 if(action==='collect'){
  if(!nearCafe(s,'cafePrep')||c.tray==null||c.carrying)return false;

  c.carrying=true;const g=c.guests.find(g=>g.id===c.tray);model.emit('Tablett aufgenommen: '+guestLabel(g)+' · '+CAFE_MENU[g.recipe].name+'.');return true;
 }
 if(action==='prepare'){
  if(!nearCafe(s,'cafePrep'))return false;
  if(c.prep||c.tray!=null)return fail('Serviere zuerst die laufende Bestellung.');
  const g=c.guests.find(g=>g.id===Number(arg)&&g.stage==='accepted');if(!g)return false;
  prepareCafe(c,g,c.config.staff>0?staffWorkRate(c,0):1);model.emit(guestLabel(g)+': '+CAFE_MENU[g.recipe].name+' wird zubereitet.');return true;
 }
 return false;
}
function random(c){c.rng=(Math.imul(c.rng,1664525)+1013904223)>>>0;return c.rng/4294967296}
function arrive(c){
 const free=Array.from({length:6},(_,i)=>i).filter(i=>!c.dirty[i]&&vacant(c,i));
 if(!free.length){c.nextGuest=2;return}
 const empty=[0,1,2].filter(t=>free.includes(t*2)&&free.includes(t*2+1));
 const pair=random(c)<.35&&empty.length>0;let places;
 if(pair){const table=empty[Math.floor(random(c)*empty.length)];places=[table*2,table*2+1]}
 else{const candidates=empty.length?empty.flatMap(t=>[t*2,t*2+1]):free;places=[candidates[Math.floor(random(c)*candidates.length)]]}
 const party=c.serial+1;
 places.forEach((place,i)=>{const seat=Math.floor(place/2),side=place%2?1:-1,recipe=Object.keys(CAFE_MENU)[Math.floor(random(c)*3)];
  c.guests.push({id:++c.serial,party,seat,side,recipe,stage:'arriving',time:guestWalkDuration(seat,side)+(i?.7+random(c)*.6:0),wait:0,patience:60+c.config.quality*10,price:Math.round(CAFE_MENU[recipe].price*c.config.price),paid:false,served:false,tipPending:0,paymentWait:0,prepaid:false});
 });
 c.nextGuest=(10+random(c)*13)/cafeDemand(c.config);
}
function leaveGuest(c,g){
 g.stage='leaving';g.time=guestWalkDuration(g.seat,g.side);
 if(g.served||g.paid)c.dirty[guestPlace(g)]=.001;
 if(c.tray===g.id){c.tray=null;c.carrying=false}
 if(c.prep?.id===g.id)c.prep=null;
 if(c.staffCarry===g.id)c.staffCarry=null;
}
export function tickCafe(model,dt){
 const s=model.s,c=s.cafe;if(!c||!Number.isFinite(dt)||dt<=0)return;if(c.phase!=='open'){tickCommute(c,Math.min(dt,1));return}
 if((!s.inside||s.interior!=='cafe')&&c.config.staff<2){finishCafe(s,'Café verlassen');return}
 ensureCafeService(c);ensureCafeStaff(c);let remaining=Math.min(dt,1);
 while(remaining>1e-8&&c.phase==='open'){
  const step=Math.min(remaining,.05);remaining-=step;c.elapsed+=step;
  tickCommute(c,step);
  if(c.prep){c.prep.remaining-=step*(c.prep.rate||1);if(c.prep.remaining<=0){c.tray=c.prep.id;c.prep=null;const g=c.guests.find(g=>g.id===c.tray);model.emit(CAFE_MENU[g.recipe].name+' fertig · '+guestLabel(g)+'.','success')}}
  for(const g of c.guests){
   if(['arriving','eating','leaving','finished'].includes(g.stage)){
    g.time=Math.max(0,g.time-step);
    if(g.time===0){if(g.stage==='arriving')g.stage='order';else if(g.stage==='eating')g.stage=g.paid?'finished':'paying';else if(g.stage==='leaving')g.stage='gone'}
   }else if(['order','accepted'].includes(g.stage)){
    g.wait+=step;if(g.wait>=g.patience){leaveGuest(c,g);c.lost++;c.service.timedOut++;model.emit(guestLabel(g)+' ist ohne Bestellung gegangen.','warning')}
   }else if(g.stage==='paying'){
    g.paymentWait+=step;
    if(g.paymentWait>=20){settlePayment(c,g,'table');g.stage='finished';model.emit(guestLabel(g)+': Geld passend auf dem Tisch hinterlassen.')}
   }
  }
  // Friends finish their own meals and wait for one another before leaving.
  for(const g of c.guests)if(g.stage==='finished'&&g.time<=0&&!c.guests.some(other=>other.party===g.party&&other.id!==g.id&&(!['finished','leaving','gone'].includes(other.stage)||other.stage==='finished'&&other.time>0)))leaveGuest(c,g);
  c.guests=c.guests.filter(g=>g.stage!=='gone');
  c.dirty.forEach((d,i)=>{if(d)c.dirty[i]+=step});
  tickStaff(model,step);c.nextGuest=Math.max(0,c.nextGuest-step);
  if(c.elapsed<Math.max(0,(c.duration??CAFE_SHIFT)-60)&&c.nextGuest<=0&&!c.employees.some(e=>e.status==='entering'))arrive(c);
  if(c.elapsed>=(c.duration??CAFE_SHIFT)){finishCafe(s,'Schicht abgeschlossen');model.emit('Feierabend. Die Einzelrechnungen stehen im Betriebsbuch.','success')}
 }
}
function prepareCafe(c,g,rate=1){
 if(c.prep||c.tray!=null||g.stage!=='accepted'||c.staffCarry===g.id)return false;
 if(c.config.staff>0)ensureCafeService(c).preparedByStaff++;
 if(!g.prepaid)c.ingredients+=CAFE_MENU[g.recipe].cost;g.prepaid=false;c.prep={id:g.id,remaining:CAFE_MENU[g.recipe].seconds,rate};return true;
}
function serveCafe(model,g,byPlayer=false){
 const s=model.s,c=s.cafe;if(g.stage!=='accepted'||(byPlayer?c.tray!==g.id||!c.carrying:c.staffCarry!==g.id)||g.paid||c.receipts.some(r=>r.id===g.id))return false;
 const tip=Math.round(g.price*.15*Math.max(0,1-g.wait/g.patience)*(1+(c.config.quality-1)*.15));
 recordCafeService(c,g,byPlayer);c.served++;g.served=true;g.tipPending=tip;g.paymentWait=0;
 if(byPlayer){c.tray=null;c.carrying=false}else c.staffCarry=null;g.stage='eating';g.time=CAFE_MENU[g.recipe].dining;c.dishes[guestPlace(g)]=g.recipe;
 if(byPlayer){s.xp+=4;s.skills.business+=1.5}
 model.emit(guestLabel(g)+': '+CAFE_MENU[g.recipe].name+' serviert. Bezahlt wird nach dem Essen.','success');return true;
}
export function ensureCafeStaff(c){
 if(!Array.isArray(c.employees))c.employees=[];
 if(c.phase!=='open')return;
 for(let role=0;role<(c.config?.staff||0);role++)if(!c.employees[role])c.employees[role]={role,status:'working',...STAFF_HOME[role],task:null,route:[],wait:0};
 c.employees.length=c.config?.staff||0;c.staffCarry??=null;
}
function moveStaff(e,distance){while(e.route.length&&distance>0){const p=e.route[0],d=Math.hypot(p.x-e.x,p.z-e.z);if(d<=distance){e.x=p.x;e.z=p.z;distance-=d;e.route.shift()}else{e.x+=(p.x-e.x)*distance/d;e.z+=(p.z-e.z)*distance/d;distance=0}}}
function tickCommute(c,dt){
 for(const e of c.employees||[])if(['entering','leaving'].includes(e.status)){
  if(e.wait>0){e.wait=Math.max(0,e.wait-dt);continue}
  moveStaff(e,dt*staffWalkSpeed(c,e.role));
  if(!e.route.length)e.status=e.status==='entering'?'working':'gone';
 }
}
function assignStaff(e,kind,target,id=null,seat=null,place=null){e.task={kind,id,seat,place};e.route=staffPath(e,target).slice(1);e.wait=kind==='clean'?1.8:kind==='order'?1.4:kind==='pay'?1.1:kind==='serve'?.65:.35}
function tickStaff(model,dt){
 const c=model.s.cafe;
 if(c.employees[0]?.status!=='entering'&&c.employees[0]&&!c.prep&&c.tray==null){const g=waitingCafeOrders(c)[0];if(g)prepareCafe(c,g,staffWorkRate(c,0))}
 for(const e of c.employees.slice(1)){
  if(e.status==='entering')continue;
  if(e.task?.kind==='home'){e.task=null;e.route=[]}
  if(e.task){
   const t=e.task,g=c.guests.find(g=>g.id===t.id);
   const valid=t.kind==='home'||t.kind==='pay'&&g?.stage==='paying'||t.kind==='clean'&&c.dirty[t.place]>0&&vacant(c,t.place)||t.kind==='order'&&g?.stage==='order'||t.kind==='collect'&&c.tray===t.id&&!c.carrying&&c.staffCarry==null||t.kind==='serve'&&g?.stage==='accepted'&&c.staffCarry===t.id;
   if(!valid){e.task=null;e.route=[]}
  }
  if(!e.task){
   if(e.role===1){
    const paying=c.guests.filter(g=>g.stage==='paying').sort((a,b)=>b.paymentWait-a.paymentWait)[0],trayGuest=c.guests.find(g=>g.id===c.tray&&g.stage==='accepted'),order=c.guests.filter(g=>g.stage==='order').sort((a,b)=>b.wait-a.wait||a.id-b.id)[0];
    if(trayGuest&&!c.carrying&&c.staffCarry==null)assignStaff(e,'collect',CAFE_POINTS.cafePrep,trayGuest.id,trayGuest.seat);
    else if(order)assignStaff(e,'order',CAFE_POINTS['cafeTable'+order.seat],order.id,order.seat);
    else if(paying)assignStaff(e,'pay',CAFE_POINTS['cafeTable'+paying.seat],paying.id,paying.seat);
   }else{
    const place=c.dirty.map((d,i)=>({d,i})).filter(v=>v.d>0&&vacant(c,v.i)).sort((a,b)=>b.d-a.d)[0]?.i??-1;
    if(place>=0)assignStaff(e,'clean',CAFE_POINTS['cafeTable'+Math.floor(place/2)],null,Math.floor(place/2),place);
   }

  }
  if(!e.task)continue;moveStaff(e,dt*staffWalkSpeed(c,e.role));
  if(e.route.length)continue;e.wait=Math.max(0,e.wait-dt*staffWorkRate(c,e.role));if(e.wait>0)continue;
  const t=e.task,g=c.guests.find(g=>g.id===t.id);e.task=null;
  if(t.kind==='pay'&&g?.stage==='paying'){settlePayment(c,g,'staff');g.stage='finished'}
  else if(t.kind==='order'&&g?.stage==='order'){g.stage='accepted';c.service.ordersByStaff++}
  else if(t.kind==='collect'&&c.tray===t.id&&!c.carrying&&c.staffCarry==null){c.staffCarry=t.id;c.tray=null;assignStaff(e,'serve',CAFE_POINTS['cafeTable'+t.seat],t.id,t.seat)}
  else if(t.kind==='serve'&&g&&c.staffCarry===g.id)serveCafe(model,g);
  else if(t.kind==='clean'&&vacant(c,t.place)){clearPlace(c,t.place);c.service.clearedByStaff++}
 }
}
function migrateCafe(c){
 if(c.schema===4)return c;
 c=migrateCafePayments(c);c.schema=4;c.service=newCafeService(c.elapsed>0||c.served>0||c.lost>0,c.elapsed);return c;
}
function migrateCafePayments(c){
 if(c.schema===3)return c;
 c=migrateLegacyCafe(c);c.schema=3;
 if(c.config)c.config.staffLevels=[1,1,1];
 if(c.prep)c.prep.rate=1;
 for(const g of c.guests){g.served=g.paid;g.tipPending=0;g.paymentWait=0}
 return c;
}
function migrateLegacyCafe(c){
 if(c.schema===2)return c;
 if(c.schema!=null||!Array.isArray(c.guests)||c.guests.length>3||!Array.isArray(c.dirty)||c.dirty.length!==3)throw Error('Ungültiger alter Café-Spielstand.');
 c.schema=2;c.rng=(c.day*7919+c.serial+1)>>>0;c.receipts=[];c.openingRevenue=c.revenue;c.openingTips=c.tips;c.served*=2;c.lost*=2;
 c.dirty=c.dirty.flatMap(d=>[d,d]);c.dishes=c.dirty.map(d=>d?'espresso':null);
 const guests=[];
 for(const old of c.guests){
  const paid=['eating','leaving'].includes(old.stage)&&c.dirty[old.seat*2]>0||old.stage==='eating';
  for(const [i,side] of [-1,1].entries()){
   const g={...old,id:i?++c.serial:old.id,party:old.id,side,price:i?old.price-Math.floor(old.price/2):Math.floor(old.price/2),paid,prepaid:!!i&&(c.prep?.id===old.id||c.tray===old.id)};
   if(paid)c.dishes[guestPlace(g)]=g.recipe;guests.push(g);
  }
 }
 c.guests=guests;
 for(const e of c.employees||[])if(e.task?.kind==='clean')e.task.place=e.task.seat*2;
 if(c.lastReport)c.lastReport={...c.lastReport,served:c.lastReport.served*2,lost:c.lastReport.lost*2};
 return c;
}
function validateEmployees(c,number){
 if(c.employees==null){c.employees=[];c.staffCarry=null}
 if(!Array.isArray(c.employees)||c.employees.length>(c.config?.staff||0))throw Error('Ungültiges Café-Team.');
 const point=p=>p&&Number.isFinite(p.x)&&p.x>=292.25&&p.x<=307.75&&Number.isFinite(p.z)&&p.z>=72.25&&p.z<=87.75;
 for(const [i,e] of c.employees.entries()){
  e.status??='working';if(!['entering','working','leaving','gone'].includes(e.status))throw Error('Ungültige Anwesenheit.');
  if(e.role!==i||!point(e)||!number(e.wait,2)||!Array.isArray(e.route)||e.route.length>32||!e.route.every(point))throw Error('Ungültiger Café-Arbeitsweg.');
  const t=e.task;if(t&&(!['home','order','collect','serve','clean','pay'].includes(t.kind)||t.kind!=='home'&&(!Number.isInteger(t.seat)||t.seat<0||t.seat>2)||t.kind==='clean'&&(!Number.isInteger(t.place)||t.place<0||t.place>5||Math.floor(t.place/2)!==t.seat)||['order','collect','serve','pay'].includes(t.kind)&&(!Number.isInteger(t.id)||t.id<1||t.id>c.serial)))throw Error('Ungültige Café-Aufgabe.');
 }
 c.staffCarry??=null;if(c.staffCarry!=null&&(!c.guests.some(g=>g.id===c.staffCarry&&g.stage==='accepted')||c.staffCarry===c.tray||c.prep?.id===c.staffCarry||c.employees[1]?.task?.kind!=='serve'||c.employees[1]?.task?.id!==c.staffCarry))throw Error('Ungültiges Service-Tablett.');
 ensureCafeStaff(c);
}
export function validateCafe(raw,s){
 if(raw==null)return newCafe();const c=migrateCafe(structuredClone(raw)),number=(v,max=1e9)=>Number.isFinite(v)&&v>=0&&v<=max;
 if(c.economyModel==null&&c.staffCarry!=null&&c.staffCarry===c.tray)c.tray=null;
 c.economyModel??=1;if(![1,2].includes(c.economyModel))throw Error('Ungültige Café-Abrechnung.');
 if(c.clockRate==null){c.legacyMinutes=-c.elapsed*.5;c.clockRate=1;c.duration=210;c.closeMinute=c.phase==='open'?Math.min(CAFE_CLOSE,s.minute+210-c.elapsed):null;}
 c.legacyMinutes??=0;c.duration??=CAFE_SHIFT;c.closeMinute??=null;
 if(c.clockRate!==1||!Number.isFinite(c.legacyMinutes)||c.legacyMinutes>0||c.legacyMinutes < -106||!number(c.duration,CAFE_SHIFT)||c.closeMinute!==null&&!number(c.closeMinute,CAFE_CLOSE))throw Error('Ungültige Café-Spielzeit.');
 if(Number.isFinite(c.nextGuest)&&c.nextGuest<0&&c.nextGuest>=-65)c.nextGuest=0;
 if(!['idle','open','closed'].includes(c.phase)||typeof c.carrying!=='boolean'||!Number.isInteger(c.day)||c.day<0||c.day>s.day||!['elapsed','nextGuest','serial','served','lost','revenue','tips','ingredients','openingRevenue','openingTips'].every(k=>number(c[k]))||!Number.isInteger(c.serial)||!Number.isInteger(c.served)||!Number.isInteger(c.lost)||!Number.isInteger(c.rng)||c.rng<0||c.rng>4294967295||c.elapsed>481||!Array.isArray(c.guests)||c.guests.length>6||!Array.isArray(c.dirty)||c.dirty.length!==6||c.dirty.some(n=>!number(n))||!Array.isArray(c.dishes)||c.dishes.length!==6||c.dishes.some(r=>r!=null&&!Object.hasOwn(CAFE_MENU,r)))throw Error('Ungültiger Café-Betrieb.');
 if(c.phase!=='idle'&&(!s.businesses.cafe||!c.config||![.8,1,1.2,1.4].includes(c.config.price)||!Number.isInteger(c.config.staff)||c.config.staff<0||c.config.staff>3||!Number.isInteger(c.config.quality)||c.config.quality<1||c.config.quality>3||!validStaffLevels(c.config.staffLevels)||typeof c.config.marketing!=='boolean'||!['Normal','Aufschwung','Rezession'].includes(c.config.economy)))throw Error('Ungültige Café-Konfiguration.');
 const ids=new Set(),places=new Set();
 for(const g of c.guests){
  if(!Number.isInteger(g.id)||g.id<1||g.id>c.serial||ids.has(g.id)||!Number.isInteger(g.party)||g.party<1||g.party>c.serial||!Number.isInteger(g.seat)||g.seat<0||g.seat>2||![-1,1].includes(g.side)||places.has(guestPlace(g))||!Object.hasOwn(CAFE_MENU,g.recipe)||!['arriving','order','accepted','eating','paying','finished','leaving'].includes(g.stage)||!['time','wait','patience','price','tipPending','paymentWait'].every(k=>number(g[k]))||g.patience<1||!Number.isInteger(g.tipPending)||!Number.isInteger(g.price)||typeof g.served!=='boolean'||typeof g.paid!=='boolean'||typeof g.prepaid!=='boolean'||['eating','paying','finished'].includes(g.stage)&&!g.served||g.stage==='finished'&&!g.paid||g.stage==='paying'&&g.paid||g.paid&&!g.served||['arriving','order','accepted'].includes(g.stage)&&(g.paid||g.served))throw Error('Ungültige Café-Gäste.');
  ids.add(g.id);places.add(guestPlace(g));
 }
 const accepted=id=>c.guests.some(g=>g.id===id&&g.stage==='accepted'&&!g.paid);
 if(c.prep&&(!accepted(c.prep.id)||!number(c.prep.remaining,8)||![1,1.2,1.4,1.5,2.1].includes(c.prep.rate))||c.tray!=null&&!accepted(c.tray)||c.prep&&c.tray!=null||c.carrying&&c.tray==null)throw Error('Ungültige Café-Bestellung.');
 const billed=new Set();if(!Array.isArray(c.receipts)||c.receipts.length>128)throw Error('Ungültige Café-Rechnungen.');
 for(const r of c.receipts){if(!Number.isInteger(r.id)||r.id<1||r.id>c.serial||billed.has(r.id)||!Number.isInteger(r.seat)||r.seat<0||r.seat>2||![-1,1].includes(r.side)||!Object.hasOwn(CAFE_MENU,r.recipe)||!Number.isInteger(r.amount)||!number(r.amount)||!Number.isInteger(r.tip)||!number(r.tip))throw Error('Ungültige Café-Rechnung.');billed.add(r.id)}
 if(c.guests.some(g=>billed.has(g.id)&&!g.paid))throw Error('Ungültiger Café-Zahlungsstatus.');
 if(c.revenue!==c.openingRevenue+c.receipts.reduce((n,r)=>n+r.amount,0)||c.tips!==c.openingTips+c.receipts.reduce((n,r)=>n+r.tip,0))throw Error('Café-Kasse und Einzelrechnungen stimmen nicht überein.');
 if(c.lastReport&&(!['day','served','lost','sales','tips','costs'].every(k=>number(c.lastReport[k]))||!Number.isFinite(c.lastReport.profit)||typeof c.lastReport.reason!=='string'))throw Error('Ungültiger Café-Bericht.');
 if(c.phase==='open'&&(c.day!==s.day||(s.interior!=='cafe'&&c.config.staff<2)))throw Error('Ungültige laufende Café-Schicht.');
 validateCafeService(c.service);
 if(c.service.served>c.served||c.service.timedOut+c.service.closedUnserved>c.lost)throw Error('Ungültige Café-Servicebilanz.');
 if(c.lastReport?.service){
  const r=c.lastReport;validateCafeService(r.service);
  if(!number(r.duration,481)||!r.team||!Number.isInteger(r.team.staff)||r.team.staff<0||r.team.staff>3||!validStaffLevels(r.team.levels)||!r.breakdown||!['ingredients','wages','advertising','overhead'].every(k=>number(r.breakdown[k]))||Object.values(r.breakdown).reduce((a,b)=>a+b,0)!==r.costs)throw Error('Ungültiger Café-Schichtbericht.');
  const derived=cafeServiceSummary({service:r.service});if(r.service.averageWait!==derived.averageWait||r.service.satisfaction!==derived.satisfaction||r.service.completion!==derived.completion)throw Error('Ungültige Café-Servicebewertung.');
 }
 if(c.config?.skill!=null&&!number(c.config.skill,1e12))throw Error('Ungültige Café-Nachfrage.');
 if(c.lastReport?.breakdown?.supplies!=null&&!number(c.lastReport.breakdown.supplies))throw Error('Ungültige Café-Vorratskosten.');
 validateEmployees(c,number);return c;
}
