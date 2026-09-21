import {validateSave,SAVE_KEY} from './model.js?v=0.7.3-map1';
export const BACKUP_KEY=SAVE_KEY+'.backup';
export function readSavedGame(storage){
 let primaryError=null;
 for(const key of [SAVE_KEY,BACKUP_KEY]){
  const raw=storage.getItem(key);if(!raw)continue;
  try{const parsed=JSON.parse(raw);return {state:validateSave(parsed),recovered:key===BACKUP_KEY,migrated:parsed.version<3}}
  catch(error){primaryError=error}
 }
 if(primaryError)throw primaryError;return null;
}
export function writeSavedGame(storage,state){
 const serialized=JSON.stringify(state);validateSave(JSON.parse(serialized));
 const previous=storage.getItem(SAVE_KEY);
 if(previous){try{validateSave(JSON.parse(previous));storage.setItem(BACKUP_KEY,previous)}catch{/* Preserve an existing good backup if the primary was damaged. */}}
 storage.setItem(SAVE_KEY,serialized);
}
