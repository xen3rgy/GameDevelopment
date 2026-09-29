import {BOTTLE_CANDIDATES,NIGHT_CLUSTERS,PUBLIC_BINS} from './scavenge-layout.js?v=0.8.1';
export const SEARCH_SECONDS=2.3;
export function hash(text){let h=2166136261;for(const c of String(text)){h^=c.charCodeAt(0);h=Math.imul(h,16777619);}h^=h>>>16;h=Math.imul(h,0x7feb352d);h^=h>>>15;h=Math.imul(h,0x846ca68b);return (h^(h>>>16))>>>0;}
const roll=(seed,day,key)=>hash(seed+':'+day+':'+key)/4294967296;
export const newScavenge=(seed=globalThis.crypto?.getRandomValues(new Uint32Array(1))[0]??Math.floor(Math.random()*4294967296),day=1)=>({seed,day,searched:[],pending:[],action:null});
export function initScavenge(s){
 s.scavenge??=newScavenge(hash(JSON.stringify([s.mode,s.day,s.money,s.position,s.stats,s.collected])),s.day);
 if(s.scavenge.day<s.day){s.scavenge.day=s.day;s.scavenge.searched=[];s.scavenge.action=null;s.collected=[];}
 return s.scavenge;
}
export function dailyBottles(seed,day,story=true){
 const r=roll(seed,day,'amount'),count=Math.max(story&&day===1?5:4,r<.25?4+Math.floor(roll(seed,day,'quiet')*4):r<.85?7+Math.floor(roll(seed,day,'normal')*4):10+Math.floor(roll(seed,day,'busy')*6));
 const ranked=BOTTLE_CANDIDATES.map((p,i)=>({...p,rank:-Math.log(Math.max(1e-9,roll(seed,day,'point'+i)))/p.weight})).sort((a,b)=>a.rank-b.rank);
 const chosen=[];for(const p of ranked)if(chosen.length<count&&chosen.every(q=>Math.hypot(q.x-p.x,q.z-p.z)>7))chosen.push(p);
 return chosen.map((p,id)=>({id,x:p.x,z:p.z}));
}
export function bottleLayout(s){
 const a=initScavenge(s),day=dailyBottles(a.seed,s.day,s.mode==='story');
 if(s.minute>=1200&&roll(a.seed,s.day,'night')<.22){const cluster=NIGHT_CLUSTERS[Math.floor(roll(a.seed,s.day,'cluster')*NIGHT_CLUSTERS.length)],count=1+Math.floor(roll(a.seed,s.day,'bonus')*3);for(const [i,[x,z]] of cluster.slice(0,count).entries())day.push({id:15+i,x,z});}
 return day;
}
export function binReward(seed,day,bin){const r=roll(seed,day,'bin'+bin);return r<.4?null:r<.75?{id:'bottle',count:1}:r<.85?{id:'bottle',count:2}:{id:'roll',count:1};}
const busy=s=>s.inside||s.riding||s.transit||s.vehicleService||s.job||s.dailyLife?.action||s.workshop?.active||s.cafe?.phase==='open';
const near=(p,b)=>b&&Math.hypot(p.x-b.x,p.z-b.z)<=1.8;
function receive(model,reward){
 if(!model.add(reward.id,reward.count))return false;
 if(reward.id==='bottle'){model.s.stats.collected+=reward.count;model.s.xp+=reward.count*2;model.checkQuests();}
 return true;
}
export function beginBinSearch(model,id){
 const s=model.s,a=initScavenge(s),bin=PUBLIC_BINS.find(b=>b.id===id);
 if(a.action||busy(s)||!near(s.position,bin))return false;
 const pending=a.pending.find(p=>p.bin===id);
 if(pending){if(!receive(model,pending)){model.emit('Rucksack voll. Dein Fund bleibt am Mülleimer zur Abholung bereit.');return false;}a.pending=a.pending.filter(p=>p!==pending);model.emit('Deinen Fund eingepackt.','success');return true;}
 if(a.searched.includes(id))return false;
 a.action={bin:id,day:s.day,elapsed:0,origin:{...s.position}};model.onChange();return true;
}
export function cancelBinSearch(model){const a=model.s.scavenge;if(!a?.action)return false;a.action=null;model.onChange();return true;}
export function tickBinSearch(model,seconds){
 const s=model.s,a=initScavenge(s),action=a.action;if(!action||!Number.isFinite(seconds)||seconds<=0)return;
 const bin=PUBLIC_BINS.find(b=>b.id===action.bin);
 if(busy(s)||action.day!==s.day||!near(s.position,bin)||Math.hypot(s.position.x-action.origin.x,s.position.z-action.origin.z)>.1){cancelBinSearch(model);return;}
 action.elapsed=Math.min(SEARCH_SECONDS,action.elapsed+seconds);if(action.elapsed<SEARCH_SECONDS)return;
 a.action=null;if(a.searched.includes(action.bin))return;a.searched.push(action.bin);s.needs.hygiene=Math.max(0,s.needs.hygiene-2);
 const reward=binReward(a.seed,s.day,action.bin);
 if(!reward){model.emit('Nur Müll. Nichts Brauchbares.');return;}
 if(!receive(model,reward)){a.pending.push({...reward,bin:action.bin,day:s.day});model.emit('Rucksack voll. Dein Fund bleibt am Mülleimer zur Abholung bereit.');return;}
 model.emit(reward.id==='roll'?'Noch verpacktes Essen gefunden.':reward.count===1?'Eine Pfandflasche gefunden.':'Zwei Pfandflaschen gefunden.','success');
}
export function validateScavenge(s){
 if(s.scavenge==null){initScavenge(s);return;}
 const a=s.scavenge,validBin=id=>Number.isInteger(id)&&PUBLIC_BINS.some(b=>b.id===id),fail=()=>{throw Error('Ungültiger Suchfortschritt.');};
 if(!Number.isInteger(a.seed)||a.seed<0||a.seed>4294967295||!Number.isSafeInteger(a.day)||a.day<1||a.day>s.day||!Array.isArray(a.searched)||a.searched.length>PUBLIC_BINS.length||a.searched.some(id=>!validBin(id))||new Set(a.searched).size!==a.searched.length||!Array.isArray(a.pending)||a.pending.length>PUBLIC_BINS.length)fail();
 const seen=new Set();for(const p of a.pending){if(!p||!validBin(p.bin)||!Number.isSafeInteger(p.day)||p.day<1||p.day>a.day||seen.has(p.bin))fail();seen.add(p.bin);const reward=binReward(a.seed,p.day,p.bin);if(!reward||p.id!==reward.id||p.count!==reward.count||p.day===a.day&&!a.searched.includes(p.bin))fail();}
 if(a.action!=null){const t=a.action,bin=PUBLIC_BINS.find(b=>b.id===t.bin);if(!validBin(t.bin)||t.day!==a.day||a.searched.includes(t.bin)||seen.has(t.bin)||!Number.isFinite(t.elapsed)||t.elapsed<0||t.elapsed>=SEARCH_SECONDS||!t.origin||!Number.isFinite(t.origin.x)||!Number.isFinite(t.origin.z)||!near(t.origin,bin)||Math.hypot(s.position.x-t.origin.x,s.position.z-t.origin.z)>.1||busy(s))fail();}
 initScavenge(s);
}
