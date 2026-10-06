import {expansionDistrict,EXPANSION_DISTRICTS} from './expansion-layout.js?v=0.8.1';

// Presentation only. Redistribute the same twenty outer residents; no new save data.
export const DISTRICT_POPULATION={nordstadt:5,suedwerk:4,ostkontor:5,techpark:4,hoehen:2};
const profile=(id,name,style,color,power,roles,sound,activity)=>({id,name,style,light:{color,power},roles,sound,activity});
export const DISTRICT_IDENTITIES={
 station:profile('station','Bahnhofsviertel','brick',0xffbe79,250,['commuter','worker','student','commuter','casual','worker'],'rail','transit'),
 centre:profile('centre','Innenstadt','civic',0xffe8ca,270,['stylish','commuter','casual','older','student','stylish','employee','sporty'],'street','retail'),
 linden:profile('linden','Lindenhöfe','residential',0xffd5a5,220,['casual','older','casual','sporty','commuter','older'],'garden','residential'),
 harbor:profile('harbor','Westhafen','industrial',0xffd8a8,265,['worker','worker','commuter','worker','casual','worker'],'industry','shift'),
 canal:profile('canal','Kanalviertel','residential',0xffdab1,215,['sporty','older','casual','commuter'],'garden','residential'),
 nordstadt:profile('nordstadt','Nordstadt','residential',0xffd8af,235,['casual','student','sporty','commuter','older','casual'],'garden','residential'),
 suedwerk:profile('suedwerk','Südwerk','industrial',0xffdfb9,260,['worker','worker','commuter','worker','worker','casual'],'industry','shift'),
 ostkontor:profile('ostkontor','Ostkontor','office',0xf3ebdc,255,['stylish','commuter','stylish','casual','commuter','stylish'],'office','office'),
 techpark:profile('techpark','Techpark','campus',0xe8eddf,230,['student','student','casual','commuter','sporty','student'],'campus','campus'),
 hoehen:profile('hoehen','Höhenviertel','townhouse',0xffd9ad,185,['stylish','sporty','older','casual','sporty','stylish'],'garden','quiet')
};
export function districtIdentityAt(p={x:0,z:0}){
 const outer=expansionDistrict(p);
 const courtyard=p.x>=35&&p.x<=82&&p.z>=14;
 const id=outer?.id||(p.x<-120?'station':p.z<-48?'harbor':p.z>48?'canal':p.x<=15||courtyard?'linden':'centre');
 return DISTRICT_IDENTITIES[id];
}
const smooth=t=>{t=Math.max(0,Math.min(1,t));return t*t*(3-2*t);};
const regions=[...EXPANSION_DISTRICTS,
 {id:'station',minX:-300,maxX:-120,minZ:-120,maxZ:120},
 {id:'harbor',minX:-120,maxX:120,minZ:-120,maxZ:-48},
 {id:'linden',minX:-120,maxX:15,minZ:-48,maxZ:48},
 {id:'centre',minX:15,maxX:120,minZ:-48,maxZ:14},
 {id:'centre',minX:15,maxX:35,minZ:14,maxZ:48},
 {id:'linden',minX:35,maxX:82,minZ:14,maxZ:48},
 {id:'centre',minX:82,maxX:120,minZ:14,maxZ:48},
 {id:'canal',minX:-120,maxX:220,minZ:48,maxZ:120}];
// Overlapping eased boundary bands avoid abrupt changes in light and sound.
export function districtBlendAt(p){
 const band=18,out=[];let total=0;
 for(const r of regions){const weight=smooth((p.x-r.minX+band)/(band*2))*smooth((r.maxX-p.x+band)/(band*2))*smooth((p.z-r.minZ+band)/(band*2))*smooth((r.maxZ-p.z+band)/(band*2));if(weight>0){out.push({profile:DISTRICT_IDENTITIES[r.id],weight});total+=weight;}}
 return total?out.map(e=>({...e,weight:e.weight/total})):[{profile:districtIdentityAt(p),weight:1}];
}
const seed=id=>{let n=Math.imul((id+1)^0x32f7,0x45d9f3b);n=Math.imul(n^(n>>>16),0x45d9f3b);return (n^(n>>>16))>>>0;};
export function districtAppearance(look,home,id){
 const roles=districtIdentityAt(home).roles,archetype=roles[seed(id)%roles.length];
 // Clothing changes only. Skin, face, age features and body retain their original seed.
 return {...look,archetype,bag:archetype==='commuter',backpack:archetype==='student'};
}
const inTime=(m,a,b)=>a<=b?m>=a&&m<b:m>=a||m<b;
export function districtCitizenAwake(id,minute,home){
 const m=((minute%1440)+1440)%1440,slot=seed(id)%8,offset=(seed(id+61)%5)*12;
 switch(districtIdentityAt(home).activity){
  case 'transit':return slot===0?inTime(m,1170,330):slot<3?(inTime(m,300+offset,630)||inTime(m,960,1410-offset)):inTime(m,390+offset,1320-offset);
  case 'shift':return slot===0?inTime(m,1200,360):inTime(m,330+offset,1050+offset);
  case 'office':return inTime(m,450+offset,1110-offset);
  case 'campus':return inTime(m,450+offset,slot<2?1320:1170);
  case 'retail':return slot===0?inTime(m,1140,300):inTime(m,420+offset,1380-offset);
  case 'quiet':return slot<3?inTime(m,480+offset,1140-offset):inTime(m,540+offset,1020+offset);
  default:return slot===0?inTime(m,1140,330):inTime(m,390+offset,1230-offset);
 }
}
export function districtDestinationScore(home,destination,minute){
 const profile=districtIdentityAt(home),local=districtIdentityAt(destination).id===profile.id;
 let score=local?42:0;
 if(profile.activity==='transit'&&destination.cluster==='station'&&(minute<630||minute>960))score+=36;
 if(['quiet','residential'].includes(profile.activity)&&destination.kind==='seat')score+=18;
 if(['office','shift'].includes(profile.activity)&&destination.kind==='seat')score-=18;
 return score;
}
export function districtSoundMix(state){
 const result={rail:0,industry:0,street:0,office:0,campus:0,garden:0,birds:0};
 if(state.inside||!state.settings?.sound)return result;
 const night=state.minute<360||state.minute>=1200;
 for(const {profile,weight} of districtBlendAt(state.position||{x:0,z:0})){
  const quiet=profile.activity==='quiet',level=night?(['office','shift','campus'].includes(profile.activity)?.10:quiet?.12:.28):quiet?.42:1;
  result[profile.sound]+=weight*level;
  if(['garden','campus'].includes(profile.sound)&&!night&&state.weather!=='rain')result.birds+=weight*(quiet?1:.7);
 }
 return result;
}
