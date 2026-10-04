// Purchased per-vehicle parts. Preview drafts never enter the driving simulation.
const stages=(name,prices,effects,descriptions)=>Object.fromEntries(['stock','stage1','stage2','stage3'].map((id,i)=>[id,{name:i?name+' · Stage '+i:'Serie',level:i,price:prices[i],effect:descriptions[i],...effects[i]}]));
export const UPGRADE_CATEGORIES={
 engine:{name:'Motor',tab:'power',options:stages('Motor',[0,45000,125000,230000],[{}, {acceleration:1.09,top:1.02},{acceleration:1.18,top:1.04},{acceleration:1.27,top:1.07}],['Serienleistung','+9 % Beschleunigung · +2 % Höchstgeschwindigkeit','+18 % Beschleunigung · +4 % Höchstgeschwindigkeit','+27 % Beschleunigung · +7 % Höchstgeschwindigkeit'])},
 gearbox:{name:'Getriebe',tab:'power',options:stages('Getriebe',[0,35000,80000,145000],[{},{acceleration:1.05,top:.99},{acceleration:1.09,top:.98},{acceleration:1.13,top:.97}],['Serienübersetzung','+5 % Beschleunigung · −1 % Höchstgeschwindigkeit','+9 % Beschleunigung · −2 % Höchstgeschwindigkeit','+13 % Beschleunigung · −3 % Höchstgeschwindigkeit'])},
 brakes:{name:'Bremsen',tab:'chassis',options:stages('Bremsen',[0,30000,70000,120000],[{},{braking:1.12},{braking:1.24},{braking:1.36}],['Serienbremsen','+12 % Bremskraft','+24 % Bremskraft','+36 % Bremskraft'])},
 suspension:{name:'Fahrwerk',tab:'chassis',options:stages('Fahrwerk',[0,40000,90000,160000],[{},{handling:1.06},{handling:1.12},{handling:1.18}],['Serienabstimmung','+6 % Lenkpräzision','+12 % Lenkpräzision','+18 % Lenkpräzision'])},
 tires:{name:'Reifen',tab:'tires',options:{stock:{name:'Serie',level:0,price:0,effect:'Ausgewogenes Serienprofil'},touring:{name:'Touring',level:1,price:26000,braking:1.06,handling:.98,effect:'+6 % Bremskraft · 2 % ruhigere Lenkung'},sport:{name:'Sport',level:2,price:55000,acceleration:1.06,braking:1.10,handling:1.14,effect:'+6 % Traktion beim Anfahren · +10 % Bremskraft · +14 % Handling'},grip:{name:'Grip',level:2,price:65000,acceleration:1.03,braking:1.18,handling:1.08,effect:'+3 % Traktion beim Anfahren · +18 % Bremskraft · +8 % Handling'}}},
 exhaust:{name:'Auspuff',tab:'looks',options:{stock:{name:'Serie',level:0,price:0,effect:'Ein dezentes Endrohr · rein optisch'},dual:{name:'Doppelendrohr',level:1,price:28000,effect:'Zwei kompakte Endrohre · rein optisch'},sport:{name:'Sport-Endrohr',level:2,price:42000,effect:'Breiter ovaler Abschluss · rein optisch'}}}
};
export const UPGRADE_DEFAULTS=Object.freeze(Object.fromEntries(Object.keys(UPGRADE_CATEGORIES).map(k=>[k,'stock'])));
export function normalizeUpgrades(value){return Object.fromEntries(Object.entries(UPGRADE_CATEGORIES).map(([k,c])=>[k,Object.hasOwn(c.options,value?.[k])?value[k]:'stock']));}
export function validUpgrades(value){return !!value&&Object.entries(UPGRADE_CATEGORIES).every(([k,c])=>typeof value[k]==='string'&&Object.hasOwn(c.options,value[k]));}
export function upgradePrice(vehicle,key,choice){const option=UPGRADE_CATEGORIES[key]?.options[choice];return option?Math.round((option.price||5000)*({car:1,van:1.35,sport:2.6}[vehicle.id]||1)):0;}
export function upgradeQuote(vehicle,next){if(vehicle.id==='bike'||!validUpgrades(next))return null;const old=normalizeUpgrades(vehicle.upgrades),changes=Object.keys(UPGRADE_CATEGORIES).filter(k=>old[k]!==next[k]);return {changes,total:changes.reduce((n,k)=>n+upgradePrice(vehicle,k,next[k]),0)};}
const profiles=new Map();
export function vehiclePerformance(vehicle,preview=null){
 const u=vehicle.id==='bike'?UPGRADE_DEFAULTS:normalizeUpgrades(preview||vehicle.upgrades),key=Object.values(u).join('|');if(profiles.has(key))return profiles.get(key);
 const p={acceleration:1,top:1,braking:1,handling:1};for(const [k,c] of Object.entries(UPGRADE_CATEGORIES)){const option=c.options[u[k]];for(const stat of Object.keys(p))p[stat]*=option[stat]||1;}
 p.acceleration=Math.min(1.55,p.acceleration);p.top=Math.min(1.10,p.top);p.braking=Math.min(1.65,p.braking);p.handling=Math.min(1.35,p.handling);Object.freeze(p);profiles.set(key,p);return p;
}
export function applyUpgradeVisuals(mesh,vehicle,preview=null){if(!mesh||vehicle.id==='bike')return;const u=normalizeUpgrades(preview||vehicle.upgrades);for(const [id,g] of Object.entries(mesh.userData.exhaustGroups||{}))g.visible=id===u.exhaust;}
