const schedule=['clear','cloudy','rain','cloudy','clear','fog','clear','rain','cloudy','clear','fog','clear'];
export function weatherAt(day,minute){return schedule[((Math.floor((day-1)*8+minute/180)+10)%schedule.length+schedule.length)%schedule.length];}
export const WEATHER_LABELS={clear:'Klar',cloudy:'Bewölkt',rain:'Regen',fog:'Nebel'};
export function weatherTargets(weather){return {rain:weather==='rain'?1:0,cloud:weather==='rain'?1:weather==='cloudy'?.65:weather==='fog'?.8:0,fog:weather==='fog'?1:0};}
export function blendWeather(current,weather,dt){
 const target=weatherTargets(weather);if(!current)return {...target,wet:target.rain};
 const step=1-Math.exp(-Math.max(0,dt)/8),wetStep=1-Math.exp(-Math.max(0,dt)/(target.rain?16:100));
 return {rain:current.rain+(target.rain-current.rain)*step,cloud:current.cloud+(target.cloud-current.cloud)*step,fog:current.fog+(target.fog-current.fog)*step,wet:current.wet+(target.rain-current.wet)*wetStep};
}
