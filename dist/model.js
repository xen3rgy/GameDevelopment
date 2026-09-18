import {newWorkLog,recordWork,validateWorkLog} from './work-log.js?v=0.7.2';
import {tickWorkshopLife} from './workshop-life.js?v=0.7.2';
import {newWorkshop,workshopHours,workshopCommand,tickWorkshop,expireWorkshop,validateWorkshop} from './workshop.js?v=0.7.2';
import {homeLocation,outsidePosition} from './housing.js?v=0.7.2';
import {exteriorContains} from './city-layout.js?v=0.7.2';
import {newDailyLife,validateDailyLife,actionOffer,applyRest,prepareRecipe,FRIDGE_SLOTS,FRIDGE_WEIGHT,fridgeItem} from './daily-life.js?v=0.7.2';
import {deliveryPlan,deliveryLocation,transportAvailable,DELIVERY_SECONDS} from './delivery-routes.js?v=0.7.2';
import {simulationMinutes,CAFE_CLOSE} from './game-time.js?v=0.7.2';
import {contractById,newCourier,canAcceptContract,contractQuote,clockMinutes,recordCourier,validateCourier} from './contracts.js?v=0.7.2';
import {ITEMS,HOMES,BUSINESSES,QUESTS,VEHICLES,LOCATIONS,WORK,RECIPES,SHOP_POINTS,HOME_POINTS,clamp} from './data.js?v=0.7.2';
import {inventoryWeight,itemCount,insertItem,removeItem,transferItem} from './inventory.js?v=0.7.2';
import {ROOMS,canWalkRoom} from './spatial.js?v=0.7.2';
import {newCafe,validateCafe,cafeAction,tickCafe,finishCafe,cafeDayResult,STAFF_TRAINING_COST,validStaffLevels} from './cafe.js?v=0.7.2';
export const SAVE_KEY='zero-rise-save-v1';
export function newGame(sandbox=false){return {version:3,mode:sandbox?'sandbox':'story',money:sandbox?2000000:0,bank:0,debt:0,day:1,minute:8*60,needs:{health:100,hunger:78,thirst:75,energy:85,hygiene:65,stress:12},inventory:[],storage:[],fridge:[],dailyLife:newDailyLife(),position:{x:-27,z:-4},angle:0,inside:false,interior:null,basket:[],riding:false,vehicle:null,home:null,rentDue:0,job:null,courier:newCourier(),workshop:newWorkshop(),workLog:newWorkLog(),skills:{fitness:0,logistics:0,business:0,tech:0},xp:0,reputation:0,relations:{},businesses:{},properties:0,quest:0,stats:{collected:0,returned:0,consumed:0,jobs:0,slept:0,homes:0,courses:0,businesses:0,hired:0,properties:0,wealth:0,earned:0,cooked:0},collected:[],drops:[],weather:'clear',economy:'Normal',history:[],dailyIncome:0,cafe:newCafe(),settings:{sound:true,quality:'high',speed:1,sensitivity:1,brightness:1,fov:55,ambienceVolume:100,vehicleVolume:100,effectsVolume:100}}}
export class GameModel{
 constructor(state=newGame()){this.s=state.version<3?validateSave(state):state;this.s.courier??=newCourier();this.s.dailyLife??=newDailyLife();this.s.fridge??=[];this.s.workshop??=newWorkshop();this.s.workLog??=newWorkLog();this.events=[];this.onChange=()=>{}}
 emit(text,type='info'){this.events.push({text,type});this.s.history.unshift({text,day:this.s.day});this.s.history=this.s.history.slice(0,30);this.onChange()}
 get weight(){return this.s.inventory.reduce((n,v)=>n+ITEMS[v.id].weight*v.count,0)}
 get capacity(){return 16}
 get wealth(){return this.s.money+this.s.bank-this.s.debt+Object.entries(this.s.businesses).reduce((v,[id])=>v+BUSINESSES[id].cost,0)+this.s.properties*150000+(this.s.vehicle?Math.round(VEHICLES[this.s.vehicle.id].cost*.65*this.s.vehicle.condition/100):0)}
 add(id,count=1){return insertItem(this.s.inventory,id,count)}
 count(id){return itemCount(this.s.inventory,id)}
 remove(id,count=1){return removeItem(this.s.inventory,id,count)}
 transfer(index,direction,count=1){
  if(!this.s.inside||this.s.interior!=='home'||!this.s.home){this.emit('Das Wohnungslager ist nur zu Hause erreichbar.');return false}
  const deposit=direction==='deposit';if(!deposit&&direction!=='withdraw')return false;
  const source=deposit?this.s.inventory:this.s.storage,target=deposit?this.s.storage:this.s.inventory;
  if(!transferItem(source,target,index,count,deposit?32:16,deposit?100:20)){this.emit('Nicht genug Platz oder dieser Gegenstand gehört zu einem Auftrag.');return false}
  this.onChange();return true;
 }
 sortInventory(){this.s.inventory.sort((a,b)=>ITEMS[a.id].category.localeCompare(ITEMS[b.id].category,'de')||ITEMS[a.id].name.localeCompare(ITEMS[b.id].name,'de'));this.onChange()}
 cook(id,skipTime=false){
  const r=RECIPES[id];if(!Object.hasOwn(RECIPES,id)||!this.s.inside||this.s.interior!=='home'||!this.s.home)return false;
  const result=prepareRecipe(this.s,id);if(!result){this.emit('Zutaten fehlen oder es ist kein Platz für das fertige Essen.');return false;}
  this.s.inventory=result.inventory;this.s.storage=result.storage;this.s.fridge=result.fridge;
  if(!skipTime)this.advance(r.minutes);this.s.stats.cooked++;this.s.needs.stress=clamp(this.s.needs.stress-4);this.emit(r.count+' Portionen vorbereitet · '+result.where+'.','success');return true;
 }
 fridgeTransfer(index,direction,count=1){
  const s=this.s;if(s.dailyLife.action||s.interior!=='home'||!s.home||Math.hypot(s.position.x-HOME_POINTS.fridge.x,s.position.z-HOME_POINTS.fridge.z)>1.9)return false;
  const deposit=direction==='deposit';if(!deposit&&direction!=='withdraw')return false;
  const source=deposit?s.inventory:s.fridge,target=deposit?s.fridge:s.inventory;
  if(!fridgeItem(source[index]?.id)||!transferItem(source,target,index,count,deposit?FRIDGE_SLOTS:16,deposit?FRIDGE_WEIGHT:20)){this.emit('Kein Platz oder kein Lebensmittel.');return false;}
  this.onChange();return true;
 }
 beginLifeAction(kind,arg){
  if(this.s.workshop?.active?.action)return false;
  const s=this.s,offer=actionOffer(s,kind,arg);if(!offer){this.emit('Diese Aktion ist hier gerade nicht möglich. Prüfe freie Hände, Zutaten und Bargeld.');return false;}
  if(!this.spend(offer.cost))return false;
  s.dailyLife.action={...offer,elapsed:0,applied:0,interrupted:false,origin:{...s.position},yaw:s.angle+Math.PI};this.onChange();return true;
 }
 cancelLifeAction(){const a=this.s.dailyLife.action;if(!a||a.interrupted)return false;a.interrupted=true;a.elapsed=Math.max(a.elapsed,a.duration-1.5);this.onChange();return true;}
 tickLifeAction(seconds){
  const s=this.s,a=s.dailyLife.action;if(!a||!Number.isFinite(seconds)||seconds<=0)return;
  const step=Math.min(seconds,a.duration-a.elapsed);a.elapsed+=step;
  if(!a.interrupted){
   if(a.minutes){const target=a.elapsed+1e-8>=a.duration?a.minutes:Math.floor((a.minutes*a.elapsed/a.duration+1e-8)*4)/4;const delta=Math.max(0,target-a.applied);a.applied+=delta;this.advance(delta,['sleep','shelterSleep','rest'].includes(a.kind),a.kind);}
   else this.advance(simulationMinutes(step,s.settings.speed));
  }
  if(s.dailyLife.action!==a)return;
  if(a.elapsed+1e-8<a.duration)return;
  s.dailyLife.action=null;
  if(['sleep','shelterSleep','rest'].includes(a.kind)){s.dailyLife.lastRest={day:s.day,minutes:Math.round(a.applied),kind:a.kind};if(a.kind!=='rest'&&a.applied>=30){s.stats.slept++;this.checkQuests();}}
  if(a.interrupted){this.emit('Aktion beendet. Die bereits vergangene Zeit bleibt vergangen.');return;}
  if(a.kind==='consume')this.use(s.inventory.findIndex(v=>v.id===a.item));
  else if(a.kind==='cook')this.cook(a.arg,true);
  else this.emit(a.kind==='shower'||a.kind==='wash'?'Frisch gemacht.':a.kind==='rest'?'Die Pause ist vorbei.':'Aufgewacht. Hunger und Durst liefen während des Schlafs weiter.','success');
 }

 enterInterior(id){
  const s=this.s,room=ROOMS[id],location=id==='home'?homeLocation(s):LOCATIONS.find(l=>l.id===(id==='shop'?'market':id==='cafe'?'cafe':id==='workshop'?'deliveryWorkshop':'home'));
  if(s.dailyLife.action||s.workshop?.active?.action||!Object.hasOwn(ROOMS,id)||s.inside||s.riding||id==='home'&&!s.home||Math.hypot(s.position.x-location.x,s.position.z-location.z)>3.1)return false;
  if(id==='workshop'&&!workshopHours(s).open){this.emit('Werkstatt West ist von 08:00 bis 19:00 geöffnet. Neue Aufträge bis 17:00.');return false;}
  s.inside=true;s.interior=id;s.position={...room.spawn};s.angle=room.angle;s.basket=[];return true;
 }
 leaveInterior(){
  const s=this.s,room=ROOMS[s.interior];if(s.dailyLife.action||s.workshop?.active?.action||!room)return false;
  if(s.interior==='cafe'&&s.cafe?.phase==='open'){
   if(s.cafe.config.staff<2)finishCafe(s,'Café verlassen');
   else {if(s.cafe.carrying){s.cafe.carrying=false;this.emit('Das Tablett bleibt zur Abholung an der Theke.');}this.emit(s.cafe.config.staff===3?'Dein Team führt den Cafébetrieb bis zum Schichtende weiter.':'Barista und Service arbeiten weiter. Ohne Abräumkraft musst du zum Abräumen zurückkommen.');}
  }
  if(s.basket.length)this.emit('Unbezahlte Waren wurden zurück ins Regal gelegt.');s.basket=[];s.position={...outsidePosition(s)};s.angle=s.interior==='shop'?Math.PI:0;s.inside=false;s.interior=null;return true;
 }

 get basketTotal(){return this.s.basket.reduce((sum,v)=>sum+ITEMS[v.id].price*v.count,0)}
 basketAdd(id){
  if(this.s.interior!=='shop')return false;
  const nearShelf=Object.values(SHOP_POINTS).some(p=>p.items?.includes(id)&&Math.hypot(this.s.position.x-p.x,this.s.position.z-p.z)<=2);if(!nearShelf)return false;
  let slot=this.s.basket.find(v=>v.id===id);if(slot?.count>=20){this.emit('Maximal 20 Stück pro Ware im Korb.');return false}if(slot)slot.count++;else this.s.basket.push({id,count:1});this.onChange();return true;
 }
 basketRemove(id){const i=this.s.basket.findIndex(v=>v.id===id);if(i<0)return false;if(--this.s.basket[i].count===0)this.s.basket.splice(i,1);return true}
 checkout(){
  const s=this.s,point=SHOP_POINTS.checkout;if(s.interior!=='shop'||Math.hypot(s.position.x-point.x,s.position.z-point.z)>2||!s.basket.length)return false;
  const total=this.basketTotal;if(s.money<total){this.emit('Dafür reicht dein Bargeld nicht. Lege einzelne Waren zurück.');return false}
  const inventory=structuredClone(s.inventory);for(const v of s.basket)if(!insertItem(inventory,v.id,v.count)){this.emit('Dein Rucksack hat nicht genug Platz. Lege Waren zurück oder schaffe Platz.');return false}
  s.inventory=inventory;s.money-=total;s.basket=[];this.emit('Einkauf bezahlt. Alle Waren sind jetzt in deinem Rucksack.','success');return true;
 }
 spend(amount){if(!Number.isInteger(amount)||amount<0||this.s.money<amount)return false;this.s.money-=amount;return true}
 earn(amount){this.s.money+=amount;this.s.stats.earned+=amount}
 checkQuests(){this.s.stats.wealth=this.wealth;while(this.s.quest<QUESTS.length){let q=QUESTS[this.s.quest];if(this.s.stats[q.key]<q.goal)break;this.s.quest++;this.earn(q.reward);this.s.xp+=60;this.s.reputation+=3;this.emit('Ziel erreicht: '+q.name+(q.reward?' · Belohnung erhalten.':''),'success')}this.onChange()}
 collect(id){if(this.s.collected.includes(id))return false;if(!this.add('bottle')){this.emit('Dein Rucksack ist voll.');return false}this.s.collected.push(id);this.s.stats.collected++;this.s.xp+=2;this.checkQuests();return true}
 recycle(){let n=this.count('bottle');if(!n){this.emit('Du hast keine Pfandflaschen dabei.');return}this.remove('bottle',n);this.earn(n*25);this.s.stats.returned+=n;this.emit(n+' Flaschen abgegeben.','success');this.checkQuests()}
 buy(id){let item=ITEMS[id];if(!Object.hasOwn(ITEMS,id)||!item?.price||id==='bottle'||id==='parcel')return false;if(this.s.money<item.price){this.emit('Dafür reicht dein Bargeld noch nicht.');return false}if(!this.add(id)){this.emit('Kein Platz im Rucksack.');return false}this.s.money-=item.price;this.emit(item.name+' gekauft.','success');return true}
 use(index){let slot=this.s.inventory[index],item=ITEMS[slot?.id];if(!item?.effects)return;let id=slot.id;slot.count--;if(!slot.count)this.s.inventory.splice(index,1);for(let [k,v] of Object.entries(item.effects))this.s.needs[k]=clamp(this.s.needs[k]+v);if(id==='water'&&!this.add('bottle'))this.s.drops.push({id:'bottle',count:1,x:this.s.position.x,z:this.s.position.z});this.s.stats.consumed++;this.emit(item.name+' verwendet.','success');this.checkQuests()}
 drop(index,all=false){const v=this.s.inventory[index];if(!v||v.id==='parcel')return;if(this.s.drops.length>=500){this.emit('Zu viele abgelegte Gegenstände. Nimm zuerst einige wieder auf.');return}let count=all?v.count:1;let id=v.id;v.count-=count;if(!v.count)this.s.inventory.splice(index,1);this.s.drops.push({id,count,x:this.s.position.x,z:this.s.position.z});this.onChange()}
 split(index){let slot=this.s.inventory[index];if(!slot||slot.count<2||this.s.inventory.length>=16)return;let n=Math.floor(slot.count/2);slot.count-=n;this.s.inventory.push({id:slot.id,count:n});this.onChange()}
 acceptContract(id,transport='foot'){
  const s=this.s,c=contractById(id),office=LOCATIONS.find(l=>l.id==='jobs');
  if(!c)return false;
  if(!transportAvailable(s,transport)){this.emit('Parke dein fahrbereites Fahrzeug bei Kiez & Kurier, bevor du diese Transportplanung wählst. Alternativ: Zu Fuß.');return false;}
  const plan=deliveryPlan(c,s,transport);if(!plan){this.emit('Für diese Tour ist derzeit keine sichere Route verfügbar.');return false;}
  if(s.inside||s.riding||Math.hypot(s.position.x-office.x,s.position.z-office.z)>=3.1){this.emit('Übernimm die Pakete persönlich bei Kiez & Kurier in der Lindenallee 12.');return false;}
  if(s.job||s.workshop?.active){this.emit('Schließe zuerst deinen laufenden Auftrag ab.');return false;}
  if(!canAcceptContract(c,s.courier,s.mode)){this.emit('Für diese Tour brauchst du mehr Erfahrung oder Zuverlässigkeit.');return false;}
  if(s.needs.energy<12){this.emit('Du brauchst zuerst etwas Schlaf.');return false;}
  if(!this.add('parcel',c.route.length)){this.emit('Für diese Tour brauchst du '+c.route.length+' freie Plätze und '+c.route.length*2+' kg im Rucksack.');return false;}
  s.job={protocol:2,plan,vanDistance:0,operatingCosts:0,deliveries:[],interaction:{kind:'pickup',target:'jobs',elapsed:0},type:'courier',contract:c.id,deadlineMinutes:plan.deadlineMinutes,stage:0,progress:0,carrying:false,vehicleUsed:false,target:c.route[0],started:clockMinutes(s),quotedBase:contractQuote(c,s.skills.logistics)};
  this.emit('Jonas stellt deine '+c.route.length+' Sendung'+(c.route.length>1?'en':'')+' bereit. Die Frist beginnt nach der Übernahme.','success');return true;
 }
 startJob(type){if(!['courier','cleaning','warehouse'].includes(type))return false;if(this.s.job||this.s.workshop?.active){this.emit('Schließe zuerst deinen laufenden Auftrag ab.');return false}if(this.s.needs.energy<12){this.emit('Du brauchst zuerst etwas Schlaf.');return false}
 if(type==='courier'&&!this.add('parcel')){this.emit('Du brauchst 2 kg und einen freien Platz für das Paket.');return false}
 this.s.job={type,stage:0,progress:0,carrying:false,vehicleUsed:false,target:type==='courier'?['deliveryA','deliveryB','deliveryC'][this.s.stats.jobs%3]:null,started:this.s.day*1440+this.s.minute};this.emit(type==='courier'?'Paket erhalten. Das Ziel ist auf der Karte markiert.':type==='warehouse'?'Gehe zum Westhafen. Scanne eine Kiste und bring sie zum leuchtenden Regal.':'Sammle die 6 markierten Abfallsäcke im Viertel.');return true}
 cancelJob(){const j=this.s.job;if(!j)return false;if(j.type==='courier')recordCourier(this.s,{contract:j.contract||null,status:'cancelled',stops:j.progress,duration:clockMinutes(this.s)-j.started,estimatedCosts:Math.round(j.operatingCosts||0),deliveries:j.deliveries||[]});if(j.type!=='courier')recordWork(this.s,{ref:j.type+'-cancel-'+this.s.workLog.serial,type:j.type,status:'cancelled',minutes:clockMinutes(this.s)-j.started});const parcels=this.count('parcel');if(parcels)this.remove('parcel',parcels);this.s.job=null;this.emit('Auftrag beendet. Keine Auszahlung.'+(j.type==='courier'?' Zuverlässigkeit '+this.s.courier.last.reliabilityDelta+'.':''));return true;}
 work(action,index=0){
  const s=this.s,j=s.job;if(!j||s.inside||s.riding)return false;
  const near=p=>p&&Math.hypot(s.position.x-p.x,s.position.z-p.z)<3.1;
  if(action==='deliver'&&j.type==='courier'){
   if(!near(LOCATIONS.find(l=>l.id===j.target))||!this.count('parcel'))return false;
   if(j.protocol===2){if(j.interaction)return false;const point=deliveryLocation(j.target);if(Math.hypot(s.position.x-point.x,s.position.z-point.z)>1.9){this.emit('Gehe näher zum Empfänger am markierten Eingang.');return false;}j.interaction={kind:'deliver',target:j.target,elapsed:0};this.onChange();return true;}
   if(j.contract){this.remove('parcel');j.progress++;const c=contractById(j.contract);if(j.progress<c.route.length)j.target=c.route[j.progress];}else j.progress=1;
  }else if(action==='trash'&&j.type==='cleaning'){
   const p=WORK.cleaning[j.progress];if(index!==j.progress||!near(p&&{x:p[0],z:p[1]}))return false;j.progress++;s.needs.hygiene=clamp(s.needs.hygiene-2);s.needs.energy=clamp(s.needs.energy-1);
  }else if(action==='crate'&&j.type==='warehouse'){
   if(j.carrying||!near(WORK.crate))return false;j.carrying=true;this.emit('Kiste gescannt. Bringe sie zum Regal '+['A','B','C','D'][j.progress]+'.');return true;
  }else if(action==='shelf'&&j.type==='warehouse'){
   if(!j.carrying||index!==j.progress||!near(WORK.shelves[j.progress]))return false;j.progress++;j.carrying=false;
  }else return false;
  const needed=j.contract?contractById(j.contract).route.length:{courier:1,cleaning:6,warehouse:4}[j.type];if(j.progress>=needed)this.finishJob();else this.emit(j.progress+' / '+needed+' erledigt.','success');return true;
 }
 tickCourier(seconds){
  const s=this.s,j=s.job,a=j?.interaction;if(!a||!Number.isFinite(seconds)||seconds<=0)return;
  const point=deliveryLocation(a.target);
  if(s.inside||s.riding||!point||Math.hypot(s.position.x-point.x,s.position.z-point.z)>3.1){if(a.kind==='pickup')this.cancelJob();else j.interaction=null;return;}
  a.elapsed=Math.min(DELIVERY_SECONDS[a.kind],a.elapsed+seconds);
  if(a.elapsed+1e-8<DELIVERY_SECONDS[a.kind])return;
  j.interaction=null;
  if(a.kind==='pickup'){j.started=clockMinutes(s);this.emit('Sendungen übernommen. Deine Tour beginnt jetzt.','success');return;}
  if(j.target!==a.target||!this.remove('parcel'))return;
  j.deliveries.push({index:j.progress,target:j.target,at:clockMinutes(s)});j.progress++;
  const c=contractById(j.contract);
  if(j.progress>=c.route.length)this.finishJob();else{j.target=c.route[j.progress];this.emit(j.progress+' / '+c.route.length+' persönlich übergeben. Weiter zu '+deliveryLocation(j.target).name+'.','success');}
 }
 recordCourierTravel(distance,vehicle){
  const j=this.s.job;if(j?.protocol!==2||j.interaction||!Number.isFinite(distance)||distance<=0)return;
  if(vehicle.id==='van'){j.vanDistance+=distance;j.vehicleUsed=true;}
  if(vehicle.id!=='bike')j.operatingCosts+=distance*(.012*30+.0016*120);
 }
 finishJob(){if(!this.s.job)return false;const needed=this.s.job.contract?contractById(this.s.job.contract).route.length:{courier:1,cleaning:6,warehouse:4}[this.s.job.type];if(this.s.job.progress<needed)return false;let job=this.s.job;if(job.contract){
   const c=contractById(job.contract),duration=clockMinutes(this.s)-job.started,late=!!c.minutes&&duration>(job.deadlineMinutes??c.minutes);
   const receipt=recordCourier(this.s,{contract:c.id,status:late?'late':'completed',base:job.quotedBase,estimatedCosts:Math.round(job.operatingCosts||0),missedBonus:late?c.bonus:0,deliveries:job.deliveries||[],vehicleBonus:job.protocol===2?Math.round(job.quotedBase*.25*Math.min(1,job.vanDistance/Math.max(1,job.plan.distance))):job.vehicleUsed?Math.round(job.quotedBase*.25):0,timeBonus:!late?c.bonus:0,stops:c.route.length,duration});
   if(job.contract==='workshopExpress'){this.s.workshop.expressStock=Math.min(2,(this.s.workshop.expressStock??0)+1);this.emit('Werkstatt West: Das Express-Ersatzteil liegt jetzt für einen Schlauchwechsel im Regal.','success');}
   this.earn(receipt.total);this.s.stats.jobs++;this.s.skills.logistics+=25;this.s.xp+=45;this.s.reputation+=late?0:2;this.s.needs.energy=clamp(this.s.needs.energy-8);this.s.job=null;
   this.emit(c.name+' abgeschlossen · '+(receipt.total/100).toFixed(2)+' € verdient.'+(late?' Zeitbonus verfallen.':''),'success');this.checkQuests();return true;
  }const base={courier:3200,cleaning:4200,warehouse:6500}[job.type];let bonus=1+Math.floor(this.s.skills.logistics/100)*.1;const time=this.s.day*1440+this.s.minute-job.started;let pay=Math.round(base*bonus*(job.type==='courier'&&job.vehicleUsed?1.25:1)*(job.type==='courier'&&time>360?.75:1));if(this.count('parcel'))this.remove('parcel');if(job.type==='courier')recordCourier(this.s,{status:time>360?'late':'completed',base:pay,stops:1,duration:time});if(job.type!=='courier')recordWork(this.s,{ref:job.type+'-'+this.s.stats.jobs,type:job.type,base,bonus:pay-base,total:pay,minutes:time});this.earn(pay);this.s.stats.jobs++;this.s.skills.logistics+=25;this.s.xp+=45;this.s.reputation+=2;this.s.needs.energy=clamp(this.s.needs.energy-8);this.s.job=null;this.emit('Auftrag abgeschlossen. '+(pay/100).toFixed(2)+' € verdient.','success');this.checkQuests()}
 rent(id){const s=this.s,h=HOMES.find(h=>h.id===id);if(!h||s.home===id||s.inside||s.riding||s.dailyLife.action)return false;if(!this.spend(h.price)){this.emit('Nicht genug Geld für Kaution und Einzug.');return false;}s.home=id;s.rentDue=s.day+3;s.stats.homes=1;this.emit('Willkommen in '+h.name+'. Deine Miete wird alle 3 Tage fällig.','success');this.checkQuests();return true;}
 moveHome(id){const s=this.s,h=HOMES.find(h=>h.id===id),l=LOCATIONS.find(l=>l.id===(h?.location||'home'));if(!h||s.inside||s.riding||s.dailyLife.action||s.job?.interaction||Math.hypot(s.position.x-l.x,s.position.z-l.z)>3.1){this.emit('Besuche den Eingang der neuen Unterkunft, um dort einzuziehen.');return false;}return this.rent(id);}
 sleep(home=false){let fee=home?0:500;if(home&&!this.s.home)return;if(!this.spend(fee)){this.emit('Für ein Bett brauchst du 5 €. Auf der Parkbank kannst du kostenlos ruhen.');return}this.advance(8*60,true);this.s.needs.energy=home?HOMES.find(h=>h.id===this.s.home).energy:78;this.s.needs.health=clamp(this.s.needs.health+20);this.s.needs.stress=clamp(this.s.needs.stress-35);this.s.dailyLife.awakeMinutes=Math.max(0,this.s.dailyLife.awakeMinutes-960);this.s.stats.slept++;this.emit('Ein neuer Anfang. Du bist wieder erholt.','success');this.checkQuests()}
 rest(){this.advance(120,true);this.s.dailyLife.awakeMinutes=Math.max(0,this.s.dailyLife.awakeMinutes-42);this.s.needs.energy=clamp(this.s.needs.energy+30);this.s.needs.stress=clamp(this.s.needs.stress-20);this.emit('Zwei Stunden Pause im Park.');}
 course(id){const configs={logistics:{cost:7000,hours:3},business:{cost:12000,hours:4},tech:{cost:18000,hours:4}};let c=configs[id];if(!c||!this.spend(c.cost)){this.emit('Für diesen Kurs reicht dein Geld noch nicht.');return}this.advance(c.hours*60);this.s.skills[id]+=100;this.s.xp+=100;this.s.stats.courses++;this.emit('Kurs abgeschlossen. Deine Fähigkeiten sind gestiegen.','success');this.checkQuests()}
 acquire(id){if(this.s.businesses[id])return;if(id==='agency'&&this.s.skills.tech<100){this.emit('Für das Studio brauchst du zuerst einen Technikkurs.');return}if(!this.spend(BUSINESSES[id].cost)){this.emit('Noch nicht genug Startkapital.');return}this.s.businesses[id]={stock:3,staff:0,price:1,quality:1,marketing:false,open:true,profit:0,sales:0,costs:0,...(id==='cafe'?{staffLevels:[1,1,1]}:{})};this.s.stats.businesses++;this.emit(BUSINESSES[id].name+' gehört jetzt dir. Prüfe Vorrat und Personal regelmäßig.','success');this.checkQuests()}
 workshopAction(action,arg){return workshopCommand(this,action,arg)}
 cafeAction(action,arg){if(this.s.workshop?.active&&(action==='start'||this.s.workshop.active.action)){this.emit('Beende zuerst deinen Werkstattauftrag.');return false;}return cafeAction(this,action,arg)}
 tickCafe(dt){tickCafe(this,dt)}
 manage(id,action,value){if(id==='cafe'&&this.s.cafe?.phase==='open'){this.emit('Beende zuerst deine Schicht, bevor du den Betrieb umstellst.');return}let b=this.s.businesses[id];if(!b)return;let d=BUSINESSES[id];if(action==='stock'){if(b.stock>=7){this.emit('Das Lager ist voll.');return}const delivered=Math.min(3,7-b.stock),price=Math.ceil(d.stockCost*delivered/3);if(!this.spend(price)){this.emit('Nicht genug Geld für die Bestellung.');return}b.stock+=delivered;this.emit(delivered+' Vorratstage geliefert.','success')}
 if(action==='hire'){if(b.staff>=3)return;if(!this.spend(10000)){this.emit('Die Einstellung kostet 100 €.');return}b.staff++;this.s.stats.hired++;this.checkQuests()}
 if(action==='fire'){b.staff=Math.max(0,b.staff-1);if(id==='cafe'){b.staffLevels??=[1,1,1];b.staffLevels[b.staff]=1}}
 if(action==='train'){
  const role=Number(value);if(id!=='cafe'||!Number.isInteger(role)||role<0||role>=b.staff)return;
  b.staffLevels??=[1,1,1];const level=b.staffLevels[role];if(level>=3)return;
  if(!this.spend(STAFF_TRAINING_COST[level])){this.emit('Für diese Ausbildung fehlt das Geld.');return}
  b.staffLevels[role]++;this.emit(['Barista','Service','Abräumkraft'][role]+' hat Ausbildungsstufe '+b.staffLevels[role]+' erreicht.','success');
 }
 if(action==='price')b.price=clamp(Number(value),.8,1.4);
 if(action==='marketing')b.marketing=!b.marketing;
 if(action==='open')b.open=!b.open;
 if(action==='quality'&&b.quality<3){if(!this.spend(25000*b.quality)){this.emit('Dafür fehlt das Geld.');return}b.quality++}
 this.onChange()}
 daily(){let s=this.s;finishCafe(s,'Mitternacht');s.collected=[];let states=['Normal','Normal','Aufschwung','Rezession'];s.economy=states[Math.floor(s.day/3)%4];s.weather=s.day%3===0?'rain':'clear';let income=0,cashAdjustment=0;
 for(let [id,b]of Object.entries(s.businesses)){if(id==='cafe'){const active=s.cafe?.phase!=='idle'&&s.cafe?.day===s.day-1,result=cafeDayResult(active?s.cafe:null,b,BUSINESSES[id],s.skills.business);b.sales=result.sales;b.costs=result.costs;b.profit=result.profit;b.cafeReport={day:s.day-1,...result};income+=b.profit;cashAdjustment+=result.cashAdjustment||0;continue}let d=BUSINESSES[id];b.sales=0;b.costs=Math.round(d.baseCosts*.35)+b.staff*4500+(b.marketing?1500:0);if(b.open&&b.stock>0){let demand=(s.economy==='Aufschwung'?1.22:s.economy==='Rezession'?.75:1)*(1+(b.quality-1)*.18)*(b.marketing?1.2:1)*(1+b.staff*.32)*(1+(s.skills.business/100)*.08);let staffing=b.staff===0?.65:1;demand*=Math.max(.25,1-(b.price-1)*1.8);b.sales=Math.round(d.baseSales*demand*b.price*staffing);b.stock--;b.costs+=Math.round(d.baseCosts*.65)}b.profit=b.sales-b.costs;income+=b.profit}
 income+=s.properties*7500;s.dailyIncome=income;const payout=income+cashAdjustment;if(payout>=0)this.earn(payout);else{let loss=-payout;let paid=Math.min(s.money,loss);s.money-=paid;s.debt+=loss-paid}
 if(s.debt)s.debt+=Math.ceil(s.debt*.01);
 if(s.home&&s.day>=s.rentDue){let home=HOMES.find(h=>h.id===s.home);if(this.spend(home.rent)){this.emit('Miete für '+home.name+' bezahlt.')}else{s.debt+=home.rent;this.emit('Miete offen. Der Betrag wurde als Schuld verbucht.','warning')}s.rentDue=s.day+3}
 if(Object.keys(s.businesses).length||s.properties)this.emit('Tagesabschluss: '+(income/100).toFixed(2)+' € Ergebnis.',income>=0?'success':'warning');this.checkQuests()}
 updateTime(seconds){if(this.s.dailyLife.action){this.tickLifeAction(seconds);return;}this.advance(simulationMinutes(seconds,this.s.settings.speed));this.tickCourier(seconds);tickWorkshop(this,seconds);tickWorkshopLife(this.s,seconds);}
 advance(minutes,resting=false,lifeKind=null){if(!Number.isFinite(minutes)||minutes<=0)return;let s=this.s;while(minutes>0){const c=s.cafe,busy=c?.phase==='open'||c?.employees?.some(e=>e.status==='leaving');let part=Math.min(minutes,1440-s.minute,busy?.25:lifeKind?1:Infinity);if(busy)this.tickCafe(part);s.minute+=part;minutes-=part;const n=s.needs,deprived=Math.max(0,part-n.hunger/.025,part-n.thirst/.034);n.hunger=clamp(n.hunger-part*.025);n.thirst=clamp(n.thirst-part*.034);n.energy=clamp(n.energy-part*(lifeKind&&resting?0:resting?.003:.023));if(lifeKind)applyRest(s,part,lifeKind);if(!['sleep','shelterSleep','rest'].includes(lifeKind))s.dailyLife.awakeMinutes=Math.min(2880,s.dailyLife.awakeMinutes+part);n.hygiene=clamp(n.hygiene-part*.014);n.stress=clamp(n.stress+part*(Object.keys(s.businesses).length*.0015));if(deprived>0)n.health=clamp(n.health-deprived*.035);if(c?.phase==='open'&&s.minute>=CAFE_CLOSE)finishCafe(s,'Ladenschluss um 20:00');if(s.minute>=1440){s.minute=0;s.day++;this.daily()}}
 if(s.needs.health<=0){if(s.workshop?.active)s.workshop.active.action=null;s.dailyLife.action=null;s.dailyLife.awakeMinutes=600;finishCafe(s,'Schicht wegen Erschöpfung beendet');s.needs={health:65,hunger:50,thirst:55,energy:55,hygiene:40,stress:40};s.money=Math.max(0,s.money-1500);s.position={x:-68,z:-7};s.inside=false;s.interior=null;s.basket=[];s.riding=false;this.emit('Du bist zusammengebrochen. Die Anlaufstelle hat dich versorgt. Bis zu 15 € Behandlungskosten.','warning')}expireWorkshop(this);this.checkQuests()}
 bank(action,amount){amount=Math.round(amount);if(!Number.isFinite(amount)||amount<=0)return;if(action==='deposit'&&this.spend(amount))this.s.bank+=amount;else if(action==='withdraw'&&this.s.bank>=amount){this.s.bank-=amount;this.s.money+=amount}else if(action==='loan'){if(this.s.debt+amount>100000){this.emit('Dein Kreditrahmen beträgt 1.000 €.');return}this.s.debt+=amount;this.s.money+=amount;this.emit('Kredit ausgezahlt. Zinsen: 1 % pro Spieltag.')}else if(action==='repay'){let n=Math.min(amount,this.s.debt);if(this.spend(n))this.s.debt-=n}else this.emit('Betrag nicht verfügbar.');this.checkQuests()}
 property(){if(!this.spend(150000)){this.emit('Eine Mietwohnung kostet 1.500 €.');return}this.s.properties++;this.s.stats.properties++;this.emit('Mietwohnung gekauft. Nettoertrag: 75 € pro Spieltag.','success');this.checkQuests()}
 buyVehicle(id){let v=VEHICLES[id];if(!v||this.s.vehicle)return;if(!this.spend(v.cost)){this.emit('Nicht genug Geld für dieses Fahrzeug.');return}this.s.vehicle={id,fuel:100,condition:100,x:76,z:9,angle:Math.PI,speed:0};this.emit(v.name+' gekauft. Es steht vor dem Mobilwerk.','success');this.checkQuests()}
 serviceVehicle(action){let v=this.s.vehicle;if(!v)return;if(action==='fuel'){if(v.id==='bike')return;let price=Math.ceil((100-v.fuel)*30);if(!this.spend(price)){this.emit('Nicht genug Geld zum Tanken.');return}v.fuel=100;this.emit('Vollgetankt.','success')}if(action==='repair'){let price=Math.ceil((100-v.condition)*(v.id==='bike'?10:120));if(!this.spend(price)){this.emit('Nicht genug Geld für die Reparatur.');return}v.condition=100;this.emit('Wieder in gutem Zustand.','success')}if(action==='sell'){this.earn(Math.round(VEHICLES[v.id].cost*.65*v.condition/100));this.s.vehicle=null;this.s.riding=false;this.emit('Fahrzeug verkauft.','success')}this.checkQuests()}
 talk(id,option){let r=this.s.relations[id]??{value:0,lastDay:0};if(r.lastDay===this.s.day){this.emit('Ihr habt heute schon ausführlich gesprochen. Komm morgen wieder.');return}if(option==='gift'&&!this.remove('coffee')){this.emit('Dafür brauchst du einen Kaffee im Rucksack.');return}r.value=clamp(r.value+(option==='gift'?18:10));r.lastDay=this.s.day;this.s.relations[id]=r;this.s.reputation++;this.s.needs.stress=clamp(this.s.needs.stress-8);if(r.value>=30&&r.value-(option==='gift'?18:10)<30){this.earn(4000);this.emit('Ein Kontakt hat dir einen bezahlten Tipp vermittelt: +40 €.','success')}this.emit('Ein gutes Gespräch. Eure Beziehung ist gewachsen.','success')}
}
export function validateSave(raw){
 if(!raw||![1,2,3].includes(raw.version))throw Error('Dieser Spielstand hat ein nicht unterstütztes Format.');
 const s=structuredClone(raw),base=newGame();
 if(s.version===1){s.version=2;s.storage=[];s.stats={...s.stats,cooked:0};if(s.vehicle){s.vehicle.angle=Number.isFinite(s.angle)?s.angle+Math.PI:Math.PI;s.vehicle.speed=0}if(s.job)s.job.vehicleUsed=false;}
 if(s.version===2){s.version=3;s.interior=s.inside?'home':null;s.basket=[];}
 for(let k of ['money','bank','debt','day','minute','quest','properties','xp','reputation','angle','rentDue','dailyIncome'])if(!Number.isFinite(s[k])||(!['angle','dailyIncome'].includes(k)&&s[k]<0)||Math.abs(s[k])>1e12)throw Error('Ungültiger Wert: '+k);
 if(s.day<1||s.minute>=1440||s.quest>QUESTS.length||s.properties>10000||!Number.isInteger(s.quest))throw Error('Ungültiger Fortschritt.');
 if(!Array.isArray(s.inventory)||s.inventory.length>16)throw Error('Ungültiges Inventar.');
 for(const v of s.inventory)if(!Object.hasOwn(ITEMS,v.id)||!Number.isInteger(v.count)||v.count<1||v.count>ITEMS[v.id].stack)throw Error('Ungültiger Gegenstand.');
 if(!Array.isArray(s.storage)||s.storage.length>32)throw Error('Ungültiges Wohnungslager.');
 for(const v of s.storage)if(!v||!Object.hasOwn(ITEMS,v.id)||v.id==='parcel'||!Number.isInteger(v.count)||v.count<1||v.count>ITEMS[v.id].stack)throw Error('Ungültiger Lagergegenstand.');
 if(inventoryWeight(s.storage)>100.00001)throw Error('Wohnungslager zu schwer.');
 if(s.inventory.reduce((n,v)=>n+ITEMS[v.id].weight*v.count,0)>20.00001)throw Error('Ungültiges Inventargewicht.');
 for(let k in base.needs)if(!Number.isFinite(s.needs?.[k])||s.needs[k]<0||s.needs[k]>100)throw Error('Ungültige Bedürfnisse.');
 if(!s.position||!Number.isFinite(s.position.x)||!Number.isFinite(s.position.z)||Math.abs(s.position.x)>350||Math.abs(s.position.z)>125)throw Error('Ungültige Position.');
 if(typeof s.inside!=='boolean'||s.inside!==!!s.interior||![null,'home','shop','cafe','workshop'].includes(s.interior))throw Error('Ungültiger Aufenthaltsort.');
 if(s.inside){const room=ROOMS[s.interior];if(s.position.x<room.minX||s.position.x>room.maxX||s.position.z<room.minZ||s.position.z>room.maxZ||s.interior==='home'&&!s.home)throw Error('Ungültiger Innenraum.');if(!canWalkRoom(s.interior,s.position.x,s.position.z,.35,s.home))s.position={...room.spawn};}
 else if(!exteriorContains(s.position.x,s.position.z,-1))throw Error('Ungültige Außenposition.');
 if(!Array.isArray(s.basket)||s.basket.length>10||s.interior!=='shop'&&s.basket.length)throw Error('Ungültiger Einkaufskorb.');
 const basketIds=new Set();for(const item of s.basket){if(!item||!Object.hasOwn(ITEMS,item.id)||['parcel','bottle'].includes(item.id)||!Number.isInteger(item.count)||item.count<1||item.count>20||basketIds.has(item.id))throw Error('Ungültiger Einkaufskorb.');basketIds.add(item.id)}
 if(s.home&&!HOMES.some(h=>h.id===s.home))throw Error('Unbekannte Unterkunft.');
 if(!s.businesses||Object.keys(s.businesses).some(id=>!Object.hasOwn(BUSINESSES,id)))throw Error('Unbekanntes Unternehmen.');
 for(const b of Object.values(s.businesses)){for(let k of ['stock','staff','price','quality','profit','sales','costs'])if(!Number.isFinite(b[k]))throw Error('Ungültige Unternehmensdaten.');if(b.stock<0||b.stock>7||b.staff<0||b.staff>3||b.price<.8||b.price>1.4||b.quality<1||b.quality>3)throw Error('Ungültige Unternehmenswerte.')}
 if(s.businesses.cafe?.cafeReport?.economyModel===2){const r=s.businesses.cafe.cafeReport;if(!['wages','advertising','overhead','supplies','cashAdjustment'].every(k=>Number.isInteger(r[k])&&r[k]>=0)||!Number.isInteger(r.cashMovement)||r.profit!==r.sales-r.costs||r.cashMovement!==r.profit+r.cashAdjustment||r.cashAdjustment!==r.supplies)throw Error('Ungültige Café-Bargeldabrechnung.');}
 if(s.businesses.cafe){const b=s.businesses.cafe;b.staffLevels??=[1,1,1];if(!validStaffLevels(b.staffLevels)||!Number.isInteger(b.staff))throw Error('Ungültige Café-Ausbildung.');if(b.cafeReport&&(!['day','sales','costs','manualSales','automaticSales','fixedCosts','automaticCosts','ingredients','manualMinutes'].every(k=>Number.isFinite(b.cafeReport[k])&&b.cafeReport[k]>=0)||!Number.isFinite(b.cafeReport.profit)))throw Error('Ungültige Café-Tagesabrechnung.')}
 validateCourier(s);
 if(s.job&&!s.job.contract&&(!['courier','cleaning','warehouse'].includes(s.job.type)||!Number.isFinite(s.job.progress)||!Number.isInteger(s.job.progress)||s.job.progress<0||s.job.progress>=(s.job.type==='cleaning'?6:s.job.type==='warehouse'?4:1)||!Number.isFinite(s.job.started)||s.job.type==='courier'&&!['deliveryA','deliveryB','deliveryC'].includes(s.job.target)))throw Error('Ungültiger Auftrag.');
 if(!Array.isArray(s.collected)||s.collected.some(v=>!Number.isInteger(v)||v<0||v>=64))throw Error('Ungültiger Weltzustand.');
 if(!Array.isArray(s.drops)||s.drops.length>500)throw Error('Ungültige Weltgegenstände.');
 for(const d of s.drops)if(!Object.hasOwn(ITEMS,d.id)||!Number.isInteger(d.count)||d.count<1||d.count>20||!Number.isFinite(d.x)||!Number.isFinite(d.z)||Math.abs(d.x)>350||Math.abs(d.z)>125)throw Error('Ungültiger Weltgegenstand.');
 for(let k in base.stats)if(!Number.isFinite(s.stats?.[k])||s.stats[k]<0)throw Error('Ungültige Statistik.');
 for(let k in base.skills)if(!Number.isFinite(s.skills?.[k])||s.skills[k]<0)throw Error('Ungültige Fähigkeit.');
 if(!s.relations||typeof s.relations!=='object')throw Error('Ungültige Kontakte.');
 for(let [id,r] of Object.entries(s.relations))if(!['mara','emil','leyla'].includes(id)||!Number.isFinite(r.value)||r.value<0||r.value>100||!Number.isFinite(r.lastDay))throw Error('Ungültiger Kontakt.');
 if(s.vehicle){let v=s.vehicle;if(!Object.hasOwn(VEHICLES,v.id)||!Number.isFinite(v.fuel)||v.fuel<0||v.fuel>100||!Number.isFinite(v.condition)||v.condition<0||v.condition>100||!Number.isFinite(v.x)||!Number.isFinite(v.z)||!Number.isFinite(v.angle)||!Number.isFinite(v.speed)||Math.abs(v.speed)>35||!exteriorContains(v.x,v.z,-1))throw Error('Ungültiges Fahrzeug.');}s.vehicle=s.vehicle||null;s.riding=!!s.riding;if(s.riding&&(!s.vehicle||s.inside))throw Error('Ungültiger Fahrzeugzustand.');
 s.settings={...base.settings,...s.settings};s.settings.speed=[1,4,10].includes(s.settings.speed)?s.settings.speed:1;s.settings.quality=['high','low'].includes(s.settings.quality)?s.settings.quality:'high';
 for(const [key,min,max] of [['sensitivity',.4,2],['brightness',.7,1.4],['fov',45,75],['ambienceVolume',0,100],['vehicleVolume',0,100],['effectsVolume',0,100]])s.settings[key]=Number.isFinite(s.settings[key])?clamp(s.settings[key],min,max):base.settings[key];
 s.cafe=validateCafe(s.cafe,s);
 validateDailyLife(s);
 validateWorkshop(s);
 validateWorkLog(s);
 s.history=Array.isArray(s.history)?s.history.filter(v=>typeof v.text==='string'&&Number.isFinite(v.day)).slice(0,30):[];
 return s;
}
